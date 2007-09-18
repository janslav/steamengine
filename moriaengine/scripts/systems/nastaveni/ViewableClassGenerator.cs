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
			try {
				CodeCompileUnit codeCompileUnit = new CodeCompileUnit();
				if (viewableClasses.Count > 0) {
					Logger.WriteDebug("Generating DataViews");

					CodeNamespace ns = new CodeNamespace("SteamEngine.CompiledScripts.Dialogs");
					codeCompileUnit.Namespaces.Add(ns);

					foreach (Type viewableClass in viewableClasses) {
						try {
							GeneratedInstance gi = new GeneratedInstance(viewableClass);
							if(gi.butonMethods.Count + gi.fields.Count + gi.properties.Count > 0) {//we have at least one MemberInfo
								CodeTypeDeclaration ctd = gi.GetGeneratedType();
								ns.Types.Add(ctd);
							}
						} catch (FatalException) {
							throw;
						} catch (Exception e) {
							Logger.WriteError(viewableClass.Assembly.GetName().Name, viewableClass.Name, e);
							return null;
						}
					}
					Logger.WriteDebug("Done generating "+viewableClasses.Count+" DataViews");
				}
				return codeCompileUnit;
			} finally {
				viewableClasses.Clear();
			}
		}

		public void HandleAssembly(Assembly compiledAssembly) {

		}

		public string FileName {
			get { return "ViewableClasses.Generated.cs"; }
		}

		[Remark("While loading this class, make some registrations in the ClassManager for handling ViewableClasses")]
		public static void Bootstrap() {			
			//register the CheckViewabilityClass method so every ClassManager managed Type will be
			//checked here for its Viewability...
		    ClassManager.RegisterHook(CheckViewabilityClass);
		}

		[Remark("Method for checking if the given Type is Viewable. If so, put it to the list."+
				"Used as hooked delegate in ClassManager")]
		public static bool CheckViewabilityClass(Type type) {
			//look if the type has this attribute, don't look to parent classes 
			//(if the type has not the attribute but some parent has, we dont care - if we want
			//it to be infoized, we must add a ViewableClass attribute to it)
			if(Attribute.IsDefined(type, typeof(ViewableClassAttribute),false)) {
				viewableClasses.Add(type);
				return true;
			}
			return false;
		}

		private class GeneratedInstance {

			//all fields except marked as NoShow
			internal List<FieldInfo> fields = new List<FieldInfo>();
			internal List<PropertyInfo> properties = new List<PropertyInfo>();
			internal List<MethodInfo> butonMethods = new List<MethodInfo>();
			Type type;
			CodeTypeDeclaration codeTypeDeclatarion;

			internal GeneratedInstance(Type type) {
				this.type = type;
				BindingFlags bindingAttr = BindingFlags.IgnoreCase|BindingFlags.Instance|BindingFlags.Public;

				//get all fields from the Type except for those marked as "NoShow"
				MemberInfo[] flds = type.FindMembers(MemberTypes.Field|MemberTypes.Property, bindingAttr, HasntAttribute, typeof(NoShowAttribute)); 
				foreach (MemberInfo fld in flds) {
					PropertyInfo propinf = fld as PropertyInfo;
					if(propinf != null) {
						properties.Add(propinf);
					}
					FieldInfo fldinf = fld as FieldInfo;
					if(fldinf != null) {
						fields.Add(fldinf);
					}
				}

				//get all methods from the Type that have the "Button" attribute
				MemberInfo[] mths = type.FindMembers(MemberTypes.Method, bindingAttr, HasAttribute, typeof(ButtonAttribute));
				foreach(MemberInfo mi in mths) {
					MethodInfo minf = mi as MethodInfo;
					if(minf != null) {
						butonMethods.Add(minf);
					}
				}
			}

			internal CodeTypeDeclaration GetGeneratedType() {
				codeTypeDeclatarion = new CodeTypeDeclaration("GeneratedDataView_"+type.Name);
				codeTypeDeclatarion.TypeAttributes = TypeAttributes.Public | TypeAttributes.Sealed;
				codeTypeDeclatarion.BaseTypes.Add(typeof(PluginDef.PluginTriggerGroup));
				codeTypeDeclatarion.IsClass = true;

				codeTypeDeclatarion.Members.Add(GenerateRunMethod());
				codeTypeDeclatarion.Members.Add(GenerateBootstrapMethod());

				return codeTypeDeclatarion;
			}


			private CodeMemberMethod GenerateRunMethod() {
				CodeMemberMethod retVal = new CodeMemberMethod();
				retVal.Attributes = MemberAttributes.Public | MemberAttributes.Override;
				retVal.Name = "Run";
				retVal.Parameters.Add(new CodeParameterDeclarationExpression(typeof(Plugin), "self"));
				retVal.Parameters.Add(new CodeParameterDeclarationExpression(typeof(TriggerKey), "tk"));
				retVal.Parameters.Add(new CodeParameterDeclarationExpression(typeof(ScriptArgs), "sa"));
				retVal.ReturnType = new CodeTypeReference(typeof(object));

				if(butonMethods.Count > 0) {
					retVal.Statements.Add(new CodeSnippetStatement("#pragma warning disable 168"));
					retVal.Statements.Add(new CodeVariableDeclarationStatement(
						typeof(object[]),
						"argv"));
					retVal.Statements.Add(new CodeSnippetStatement("#pragma warning restore 168"));

					retVal.Statements.Add(new CodeSnippetStatement("\t\t\tswitch (tk.uid) {"));
					foreach(MethodInfo mi in butonMethods) {
						TriggerKey tk = TriggerKey.Get(mi.Name.Substring(3));
						retVal.Statements.Add(new CodeSnippetStatement("\t\t\t\tcase("+tk.uid+"): //"+tk.name));
						//rozatim vyhozen\o, generuje to kod co nechceme
						 
						//retVal.Statements.AddRange(
						//    CompiledScriptHolderGenerator.GenerateMethodInvocation(mi, 
						//    new CodeCastExpression(
						//        type,
						//        new CodeArgumentReferenceExpression("self")), 
						//    false));
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
						new CodeTypeOfExpression(type.Name+"Def"),
						new CodeObjectCreateExpression(this.codeTypeDeclatarion.Name)
					));

				return retVal;
			}

			[Remark("Used in constructor for filtering - obtain all members with given attribute")]
			private static bool HasAttribute(MemberInfo m, object attributeType) {
				Type attType = (Type)attributeType;
				return Attribute.IsDefined(m,attType, false);
			}

			[Remark("Used in constructor for filtering- obtain all members except those with given attribute")]
			private static bool HasntAttribute(MemberInfo m, object attributeType) {
				Type attType = (Type)attributeType;
				return !Attribute.IsDefined(m, attType, false);
			}
		}
	}
}