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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Shielded;
using SteamEngine.Common;
using SteamEngine.Scripting.Objects;

namespace SteamEngine.Scripting.Compilation {
	internal sealed class CompiledTriggerGroupGenerator : ISteamCSCodeGenerator {
		private static readonly ShieldedSeq<Type> compiledTGs = new ShieldedSeq<Type>();

		[SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
		public static void Bootstrap() {
			ClassManager.RegisterSupplySubclasses<CompiledTriggerGroup>(AddCompiledTgType);
		}

		internal static bool AddCompiledTgType(Type t) {
			SeShield.AssertInTransaction();

			if (!t.IsAbstract && !t.IsSealed) {//they will be overriden anyway by generated code (so they _could_ be abstract), 
											   //but the abstractness means here that they're utility code and not actual TGs (like GroundTileType)
				compiledTGs.Add(t);
				return true;
			}
			return false;
		}

		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		public CodeCompileUnit WriteSources() {
			return SeShield.InTransaction(() => {
				try {
					CodeCompileUnit codeCompileUnit = new CodeCompileUnit();
					if (compiledTGs.Any()) {
						Logger.WriteDebug("Generating compiled Triggergroups");

						CodeNamespace ns = new CodeNamespace("SteamEngine.CompiledScripts");
						codeCompileUnit.Namespaces.Add(ns);

						foreach (Type decoratedClass in compiledTGs) {
							try {
								GeneratedInstance gi = new GeneratedInstance(decoratedClass);
								CodeTypeDeclaration ctd = gi.GetGeneratedType();
								ns.Types.Add(ctd);
							} catch (FatalException) {
								throw;
							} catch (TransException) {
								throw;
							} catch (Exception e) {
								Logger.WriteError(decoratedClass.Assembly.GetName().Name, decoratedClass.Name, e);
								return null;
							}
						}
						Logger.WriteDebug("Done generating " + compiledTGs.Count + " compiled Triggergroups");
					}
					return codeCompileUnit;
				} finally {
					compiledTGs.Clear();
				}
			});
		}

		public void HandleAssembly(Assembly compiledAssembly) {

		}


		public string FileName => "CompiledTriggerGroups.Generated.cs";

		/*
			Constructor: CompiledTriggerGroup
			Creates a triggerGroup named after the class, and then finds and sets up triggers 
			defined in the script (by naming them on_whatever).
		*/
		private class GeneratedInstance : SingleThreadedClass {
			private readonly List<MethodInfo> triggerMethods = new List<MethodInfo>();
			private readonly Type tgType;

			internal GeneratedInstance(Type tgType) {
				this.tgType = tgType;
				MemberTypes memberType = MemberTypes.Method; //Only find methods.
				BindingFlags bindingAttr = BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public;

				MemberInfo[] mis = tgType.FindMembers(memberType, bindingAttr, StartsWithString, "on_"); //Does its name start with "on_"?
				foreach (MemberInfo m in mis) {
					MethodInfo mi = m as MethodInfo;
					if (mi != null) {
						this.triggerMethods.Add(mi);
					}
				}
			}

			internal CodeTypeDeclaration GetGeneratedType() {
				this.AssertCorrectThread();

				CodeTypeDeclaration codeTypeDeclatarion = new CodeTypeDeclaration("GeneratedTriggerGroup_" + this.tgType.Name);
				codeTypeDeclatarion.TypeAttributes = TypeAttributes.Public | TypeAttributes.Sealed;
				codeTypeDeclatarion.BaseTypes.Add(this.tgType);
				codeTypeDeclatarion.IsClass = true;

				codeTypeDeclatarion.Members.Add(this.GenerateRunMethod());
				codeTypeDeclatarion.Members.Add(this.GenerateGetNameMethod());

				return codeTypeDeclatarion;
			}

			private CodeMemberMethod GenerateRunMethod() {
				CodeMemberMethod retVal = new CodeMemberMethod();
				retVal.Attributes = MemberAttributes.Public | MemberAttributes.Override;
				retVal.Name = "Run";
				retVal.Parameters.Add(new CodeParameterDeclarationExpression(typeof(object), "self"));
				retVal.Parameters.Add(new CodeParameterDeclarationExpression(typeof(TriggerKey), "tk"));
				retVal.Parameters.Add(new CodeParameterDeclarationExpression(typeof(ScriptArgs), "sa"));
				retVal.ReturnType = new CodeTypeReference(typeof(object));

				if (this.triggerMethods.Count > 0) {
					retVal.Statements.Add(new CodeVariableDeclarationStatement(
						typeof(object[]),
						"argv"));

					retVal.Statements.Add(new CodeSnippetStatement("\t\t\tswitch (tk.Uid) {"));
					foreach (MethodInfo mi in this.triggerMethods) {
						TriggerKey tk = TriggerKey.Acquire(mi.Name.Substring(3));
						retVal.Statements.Add(new CodeSnippetStatement("\t\t\t\tcase(" + tk.Uid + "): //" + tk.Name));
						retVal.Statements.AddRange(
							CompiledScriptHolderGenerator.GenerateMethodInvocation(mi,
								new CodeThisReferenceExpression(), true));
					}
					retVal.Statements.Add(new CodeSnippetStatement("\t\t\t}"));
				}

				retVal.Statements.Add(
					new CodeMethodReturnStatement(
						new CodePrimitiveExpression(null)));

				return retVal;
			}

			private CodeMemberMethod GenerateGetNameMethod() {
				var retVal = new CodeMemberMethod {
					Attributes = MemberAttributes.Family | MemberAttributes.Override,
					Name = "InternalFirstGetDefname",
					ReturnType = new CodeTypeReference(typeof(string))
				};

				retVal.Statements.Add(new CodeMethodReturnStatement(new CodePrimitiveExpression(this.tgType.Name)));
				return retVal;
			}

			private static bool StartsWithString(MemberInfo m, object filterCriteria) {
				string s = ((string) filterCriteria).ToLowerInvariant();
				return m.Name.ToLowerInvariant().StartsWith(s);
			}
		}
	}
}