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
using System.Collections.Generic;
using System.Reflection;
using System.CodeDom;
using System.CodeDom.Compiler;
using SteamEngine.Common;
using SteamEngine.CompiledScripts;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts { 
	internal class ViewableClassGenerator : ISteamCSCodeGenerator {
		static List<Type> viewableClasses = new List<Type>();
		
		public CodeCompileUnit WriteSources() {
			///TODO - pøepsat pochopitelnì
			try {
				CodeCompileUnit codeCompileUnit = new CodeCompileUnit();
				if (pluginTGs.Count > 0) {
					Logger.WriteDebug("Generating PluginTriggergroups");

					CodeNamespace ns = new CodeNamespace("SteamEngine.CompiledScripts");
					codeCompileUnit.Namespaces.Add(ns);

					foreach (Type decoratedClass in pluginTGs) {
						try {
							GeneratedInstance gi = new GeneratedInstance(decoratedClass);
							if (gi.triggerMethods.Count > 0) {
								CodeTypeDeclaration ctd = gi.GetGeneratedType();
								ns.Types.Add(ctd);
							}
						} catch (FatalException) {
							throw;
						} catch (Exception e) {
							Logger.WriteError(decoratedClass.Assembly.GetName().Name, decoratedClass.Name, e);
							return null;
						}
					}
					Logger.WriteDebug("Done generating "+pluginTGs.Count+" PluginTriggergroups");
				}
				return codeCompileUnit;
			} finally {
				pluginTGs.Clear();
			}
		}

		public void HandleAssembly(Assembly compiledAssembly) {

		}

		public string FileName {
			get { return "ViewableClasses.Generated.cs"; }
		}

		[Remark("While loading this class, make some registrations in the ClassManager for handling ViewableClasses")]
		public static void Bootstrap() {			
			ClassManager.PrepareForViewables(IsViewableClass,RegisterViewableClass);
		}

		[Remark("Method for checking if the given Type is Viewable. Used as delegate in ClassManager")]
		public static bool IsViewableClass(Type type) {
			//look if the type has this attribute, don't look to parent classes 
			//(if the type has not the attribute but some parent has, we dont care - if we want
			//it to be infoized, we must add a ViewableClass attribute to it)
			if(Attribute.IsDefined(type, typeof(ViewableClassAttribute),false) {
				return true;
			} 
		}

		[Remark("Method for checking the Viewable types. Used as delegate in ClassManager")]
		public static void RegisterViewableClass(Type type) {
			viewableClasses.Add(type);
		}


		/*
			Constructor: CompiledTriggerGroup
			Creates a triggerGroup named after the class, and then finds and sets up triggers 
			defined in the script (by naming them on_whatever).
		*/
		private class GeneratedInstance {
			///TODO - pøepsat pochopitelnì
			internal List<MethodInfo> triggerMethods = new List<MethodInfo>();
			Type pluginType;
			CodeTypeDeclaration codeTypeDeclatarion;

			internal CodeTypeDeclaration GetGeneratedType() {
				codeTypeDeclatarion = new CodeTypeDeclaration("GeneratedPluginTriggerGroup_"+pluginType.Name);
				codeTypeDeclatarion.TypeAttributes = TypeAttributes.Public | TypeAttributes.Sealed;
				codeTypeDeclatarion.BaseTypes.Add(typeof(PluginDef.PluginTriggerGroup));
				codeTypeDeclatarion.IsClass = true;

				codeTypeDeclatarion.Members.Add(GenerateRunMethod());
				codeTypeDeclatarion.Members.Add(GenerateBootstrapMethod());

				return codeTypeDeclatarion;
			}

			internal GeneratedInstance(Type pluginType) {
				this.pluginType = pluginType;
				MemberTypes memberType=MemberTypes.Method;		//Only find methods.
				BindingFlags bindingAttr = BindingFlags.IgnoreCase|BindingFlags.Instance|BindingFlags.Public;

				MemberInfo[] mis = pluginType.FindMembers(memberType, bindingAttr, StartsWithString, "on_");	//Does it's name start with "on_"?
				foreach (MemberInfo m in mis) {
					MethodInfo mi = m as MethodInfo;
					if (mi != null) {
						triggerMethods.Add(mi);
					}
				}
			}

			private CodeMemberMethod GenerateRunMethod() {
				CodeMemberMethod retVal = new CodeMemberMethod();
				retVal.Attributes = MemberAttributes.Public | MemberAttributes.Override;
				retVal.Name = "Run";
				retVal.Parameters.Add(new CodeParameterDeclarationExpression(typeof(Plugin), "self"));
				retVal.Parameters.Add(new CodeParameterDeclarationExpression(typeof(TriggerKey), "tk"));
				retVal.Parameters.Add(new CodeParameterDeclarationExpression(typeof(ScriptArgs), "sa"));
				retVal.ReturnType = new CodeTypeReference(typeof(object));

				if (triggerMethods.Count > 0) {
					retVal.Statements.Add(new CodeSnippetStatement("#pragma warning disable 168"));
					retVal.Statements.Add(new CodeVariableDeclarationStatement(
						typeof(object[]),
						"argv"));
					retVal.Statements.Add(new CodeSnippetStatement("#pragma warning restore 168"));

					retVal.Statements.Add(new CodeSnippetStatement("\t\t\tswitch (tk.uid) {"));
					foreach (MethodInfo mi in triggerMethods) {
						TriggerKey tk = TriggerKey.Get(mi.Name.Substring(3));
						retVal.Statements.Add(new CodeSnippetStatement("\t\t\t\tcase("+tk.uid+"): //"+tk.name));
						retVal.Statements.AddRange(
							CompiledScriptHolderGenerator.GenerateMethodInvocation(mi, 
							new CodeCastExpression(
								pluginType,
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


			private CodeMemberMethod GenerateBootstrapMethod() {
				CodeMemberMethod retVal = new CodeMemberMethod();
				retVal.Attributes = MemberAttributes.Public | MemberAttributes.Static;
				retVal.Name = "Bootstrap";

				retVal.Statements.Add(
					new CodeMethodInvokeExpression(
						new CodeTypeReferenceExpression(typeof(PluginDef)),
						"RegisterPluginTG",
						new CodeTypeOfExpression(pluginType.Name+"Def"),
						new CodeObjectCreateExpression(this.codeTypeDeclatarion.Name)
					));

				return retVal;
			}


			private static bool StartsWithString(MemberInfo m, object filterCriteria) {
				string s=((string) filterCriteria).ToLower();
				return m.Name.ToLower().StartsWith(s);
			}
		}
	}
}