/*
	This program is free software; you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation; either version 2 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program; if not, write to the Free Software
	Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
	Or visit http://www.gnu.org/copyleft/gpl.html
*/

using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Shielded;
using SteamEngine.Common;
using SteamEngine.Scripting;
using SteamEngine.Scripting.Compilation;
using SteamEngine.Scripting.Objects;
using SteamEngine.Transactionality;

namespace SteamEngine {
	internal sealed class PluginTriggerGroupGenerator : ISteamCsCodeGenerator {
		static readonly ShieldedSeq<Type> pluginTGs = new ShieldedSeq<Type>();

		[SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
		public static void Bootstrap() {
			ClassManager.RegisterSupplySubclasses<Plugin>(AddPluginTgType);
		}

		internal static bool AddPluginTgType(Type t) {
			Transaction.AssertInTransaction();

			if (!t.IsAbstract) {
				pluginTGs.Add(t);
			}
			return false;
		}

		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		public CodeCompileUnit WriteSources() {
			return Transaction.InTransaction(() => {
				try {
					var codeCompileUnit = new CodeCompileUnit();
					if (pluginTGs.Any()) {
						Logger.WriteDebug("Generating PluginTriggergroups");

						var ns = new CodeNamespace("SteamEngine.CompiledScripts");
						codeCompileUnit.Namespaces.Add(ns);

						foreach (var decoratedClass in pluginTGs) {
							try {
								var gi = new GeneratedInstance(decoratedClass);
								if (gi.MethodsCount > 0) {
									var ctd = gi.GetGeneratedType();
									ns.Types.Add(ctd);
								}
							} catch (FatalException) {
								throw;
							} catch (TransException) {
								throw;
							} catch (Exception e) {
								Logger.WriteError(decoratedClass.Assembly.GetName().Name, decoratedClass.Name, e);
								return null;
							}
						}
						Logger.WriteDebug("Done generating " + pluginTGs.Count + " PluginTriggergroups");
					}
					return codeCompileUnit;
				} finally {
					pluginTGs.Clear();
				}
			});
		}

		public void HandleAssembly(Assembly compiledAssembly) {

		}


		public string FileName {
			get { return "PluginTriggerGroups.Generated.cs"; }
		}

		/*
			Constructor: CompiledTriggerGroup
			Creates a triggerGroup named after the class, and then finds and sets up triggers 
			defined in the script (by naming them on_whatever).
		*/
		private class GeneratedInstance : SingleThreadedClass {
			private readonly List<MethodInfo> triggerMethods = new List<MethodInfo>();
			private readonly Type pluginType;

			internal GeneratedInstance(Type pluginType) {
				this.pluginType = pluginType;
				var memberType = MemberTypes.Method;        //Only find methods.
				var bindingAttr = BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public;

				var mis = pluginType.FindMembers(memberType, bindingAttr, StartsWithString, "on_"); //Does it's name start with "on_"?
				foreach (var m in mis) {
					var mi = m as MethodInfo;
					if (mi != null) {
						this.triggerMethods.Add(mi);
					}
				}
			}

			internal int MethodsCount => this.triggerMethods.Count;

			internal CodeTypeDeclaration GetGeneratedType() {
				this.AssertCorrectThread();

				var codeTypeDeclaration = new CodeTypeDeclaration("GeneratedPluginTriggerGroup_" + this.pluginType.Name) {
					TypeAttributes = TypeAttributes.Public | TypeAttributes.Sealed
				};
				codeTypeDeclaration.BaseTypes.Add(typeof(PluginDef.PluginTriggerGroup));
				codeTypeDeclaration.IsClass = true;

				codeTypeDeclaration.Members.Add(this.GenerateRunMethod());
				codeTypeDeclaration.Members.Add(this.GenerateBootstrapMethod(codeTypeDeclaration.Name));

				return codeTypeDeclaration;
			}

			private CodeMemberMethod GenerateRunMethod() {
				var retVal = new CodeMemberMethod {
					Attributes = MemberAttributes.Public | MemberAttributes.Override,
					Name = "Run"
				};
				retVal.Parameters.Add(new CodeParameterDeclarationExpression(typeof(Plugin), "self"));
				retVal.Parameters.Add(new CodeParameterDeclarationExpression(typeof(TriggerKey), "tk"));
				retVal.Parameters.Add(new CodeParameterDeclarationExpression(typeof(ScriptArgs), "sa"));
				retVal.ReturnType = new CodeTypeReference(typeof(object));

				if (this.triggerMethods.Count > 0) {
					retVal.Statements.Add(new CodeSnippetStatement("#pragma warning disable 168"));
					retVal.Statements.Add(new CodeVariableDeclarationStatement(
						typeof(object[]),
						"argv"));
					retVal.Statements.Add(new CodeSnippetStatement("#pragma warning restore 168"));

					retVal.Statements.Add(new CodeSnippetStatement("\t\t\tswitch (tk.Uid) {"));
					foreach (var mi in this.triggerMethods) {
						var tk = TriggerKey.Acquire(mi.Name.Substring(3));
						retVal.Statements.Add(new CodeSnippetStatement("\t\t\t\tcase(" + tk.Uid + "): //" + tk.Name));
						retVal.Statements.AddRange(
							CompiledScriptHolderGenerator.GenerateMethodInvocation(mi,
							new CodeCastExpression(this.pluginType,
								new CodeArgumentReferenceExpression("self")),
							false));

					}
					retVal.Statements.Add(new CodeSnippetStatement("\t\t\t}"));
				}

				retVal.Statements.Add(
					new CodeMethodReturnStatement(
						new CodePrimitiveExpression(null)));

				return retVal;
			}


			private CodeMemberMethod GenerateBootstrapMethod(string typeName) {
				var retVal = new CodeMemberMethod();
				retVal.Attributes = MemberAttributes.Public | MemberAttributes.Static;
				retVal.Name = "Bootstrap";

				retVal.Statements.Add(
					new CodeMethodInvokeExpression(
						new CodeTypeReferenceExpression(typeof(PluginDef)),
						"RegisterPluginTg",
						new CodeTypeOfExpression(this.pluginType.Name + "Def"),
						new CodeObjectCreateExpression(typeName)
					));

				return retVal;
			}


			private static bool StartsWithString(MemberInfo m, object filterCriteria) {
				var s = ((string) filterCriteria).ToLowerInvariant();
				return m.Name.ToLowerInvariant().StartsWith(s);
			}
		}
	}
}