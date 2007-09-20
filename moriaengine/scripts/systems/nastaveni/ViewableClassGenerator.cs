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
			return new CodeCompileUnit();
			try {
				CodeCompileUnit codeCompileUnit = new CodeCompileUnit();
				if (viewableClasses.Count > 0) {
					Logger.WriteDebug("Generating DataViews");

					CodeNamespace ns = new CodeNamespace("SteamEngine.CompiledScripts.Dialogs");
					codeCompileUnit.Namespaces.Add(ns);

					foreach (Type viewableClass in viewableClasses) {
						try {
							GeneratedInstance gi = new GeneratedInstance(viewableClass);
							///TODO-dodìlat 
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
			string typeLabel; //label for info dialogs - obtained from ViewableClassAttribute
			CodeTypeDeclaration codeTypeDeclaration;

			internal GeneratedInstance(Type type) {
				this.type = type;
				ViewableClassAttribute vca = (ViewableClassAttribute)type.GetCustomAttributes(typeof(ViewableClassAttribute), false)[0];
				typeLabel = (vca.Name == null ? type.Name : vca.Name); //label will be either the name of the type or specified label
				BindingFlags bindingAttr = BindingFlags.IgnoreCase|BindingFlags.Instance|BindingFlags.Public|BindingFlags.Static;

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
				codeTypeDeclaration = new CodeTypeDeclaration("GeneratedDataView_" + type.Name);
				codeTypeDeclaration.TypeAttributes = TypeAttributes.Public | TypeAttributes.Sealed;
				codeTypeDeclaration.BaseTypes.Add(typeof(AbstractDataView));
				codeTypeDeclaration.IsClass = true;
				
				//first add two methods for getting the ActionsButtonsPage and DataFieldsPage
				codeTypeDeclaration.Members.Add(GenerateActionButtonsPageMethod());
				codeTypeDeclaration.Members.Add(GenerateDataFieldsPageMethod());
				//now two overriden properties - LineCount and Name
				codeTypeDeclaration.Members.Add(GenerateLineCountProperty());
				codeTypeDeclaration.Members.Add(GenerateNameProperty());

				//now create the inner classes of all IDataFieldViews for this class
				//first classes for buttons
				List<string> buttonDataFieldViews = new List<string>();
				foreach(MethodInfo buttonMethod in butonMethods) {
					CodeTypeDeclaration oneButtonIDFW = GenerateButtonIDFW(buttonMethod);
					buttonDataFieldViews.Add(oneButtonIDFW.Name); //store the name of the new generated type
					codeTypeDeclaration.Members.Add(oneButtonIDFW);
				}

				//then classes for datafields
				List<string> fieldsDataFieldViews = new List<string>();
				foreach(FieldInfo oneField in fields) {
					CodeTypeDeclaration oneFieldIDFW = GenerateFieldIDFW(oneField);
					fieldsDataFieldViews.Add(oneFieldIDFW.Name); //store the name of the new generated type
					codeTypeDeclaration.Members.Add(oneFieldIDFW);
				}
				foreach(PropertyInfo oneProperty in properties) {
					CodeTypeDeclaration onePropertyIDFW = GenerateFieldIDFW(oneProperty);
					fieldsDataFieldViews.Add(onePropertyIDFW.Name); //store the name of the new generated type
					codeTypeDeclaration.Members.Add(onePropertyIDFW);
				}

				//now add a Class representing one Page of action buttons
				codeTypeDeclaration.Members.Add(GenerateActionButtonsPage(buttonDataFieldViews));

				return codeTypeDeclaration;
			}

			[Remark("Method for getting one page of action buttons")]
			private CodeMemberMethod GenerateActionButtonsPageMethod() {
				CodeMemberMethod retVal = new CodeMemberMethod();
				retVal.Attributes = MemberAttributes.Family | MemberAttributes.Override;
				retVal.Name = "ActionButtonsPage";
				retVal.Parameters.Add(new CodeParameterDeclarationExpression(typeof(int), "firstLineIndex"));
				retVal.Parameters.Add(new CodeParameterDeclarationExpression(typeof(int), "maxButtonsOnPage"));
				retVal.ReturnType = new CodeTypeReference(typeof(IEnumerable<ButtonDataFieldView>));

				retVal.Statements.Add(new CodeMethodReturnStatement(
										//makes the "new ..." section
										new CodeObjectCreateExpression(type.Name+"ActionButtonsPage",
											//two parameters
											new CodeVariableReferenceExpression("firstLineIndex"),
											new CodeVariableReferenceExpression("maxButtonsOnPage"))
										));
				/*
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
				}*/
				return retVal;
			}

			[Remark("Method for getting one page of data fields buttons")]
			private CodeMemberMethod GenerateDataFieldsPageMethod() {
				CodeMemberMethod retVal = new CodeMemberMethod();
				retVal.Attributes = MemberAttributes.Family | MemberAttributes.Override;
				retVal.Name = "DataFieldsPage";
				retVal.Parameters.Add(new CodeParameterDeclarationExpression(typeof(int), "firstLineIndex"));
				retVal.Parameters.Add(new CodeParameterDeclarationExpression(typeof(int), "maxLinesOnPage"));
				retVal.ReturnType = new CodeTypeReference(typeof(IEnumerable<IDataFieldView>));

				retVal.Statements.Add(new CodeMethodReturnStatement(
										new CodeObjectCreateExpression(type.Name + "DataFieldsPage",
											new CodeVariableReferenceExpression("firstLineIndex"),
											new CodeVariableReferenceExpression("maxLinesOnPage"))
										));
				return retVal;
			}

			[Remark("The get-property LineCount - says how many rows will there be")]
			private CodeMemberProperty GenerateLineCountProperty() {
				CodeMemberProperty retVal = new CodeMemberProperty();
				retVal.HasGet = true;
				retVal.Attributes = MemberAttributes.Public | MemberAttributes.Override;
				retVal.Name = "LineCount";
				retVal.Type = new CodeTypeReference(typeof(int));
				retVal.GetStatements.Add(new CodeMethodReturnStatement(
											new CodePrimitiveExpression(
												Math.Max(butonMethods.Count,fields.Count+properties.Count)
										)));
				return retVal;
			}

			[Remark("The get-property Name")]
			private CodeMemberProperty GenerateNameProperty() {
				CodeMemberProperty retVal = new CodeMemberProperty();
				retVal.HasGet = true;
				retVal.Attributes = MemberAttributes.Public | MemberAttributes.Override;
				retVal.Name = "Name";
				retVal.Type = new CodeTypeReference(typeof(string));
				retVal.GetStatements.Add(new CodeMethodReturnStatement(
											new CodePrimitiveExpression(
												typeLabel
										)));
				return retVal;
			}

			[Remark("Generate an inner class for handling Action Buttons")]
			private CodeTypeDeclaration GenerateButtonIDFW(MethodInfo minf) {
				ButtonAttribute bat = (ButtonAttribute)minf.GetCustomAttributes(typeof(ButtonAttribute),false)[0];
				string buttonLabel = (bat.Name == null ? minf.Name : bat.Name); //label of the button (could have been specified in ButtonAttribute)
				string newClassName = "GeneratedButtonDataFieldView_" + type.Name + "_" + minf.Name;
				CodeTypeDeclaration retVal = new CodeTypeDeclaration(newClassName);
				retVal.TypeAttributes = TypeAttributes.Public | TypeAttributes.Sealed;
				retVal.BaseTypes.Add(typeof(ButtonDataFieldView));				
				retVal.IsClass = true;

				//reference to instance
				retVal.Members.Add(new CodeMemberField(newClassName, "instance"));
				//constructor
				CodeConstructor constr = new CodeConstructor();
				constr.Attributes = MemberAttributes.Static;
				//add initialization of instance variable to the constructor
				constr.Statements.Add(new CodeAssignStatement(
										new CodeFieldReferenceExpression(new CodeThisReferenceExpression(),"instance"), 
										new CodeObjectCreateExpression(newClassName)));
				retVal.Members.Add(constr);

				//now override the Name property
				CodeMemberProperty nameProp = new CodeMemberProperty();
				nameProp.HasGet = true;
				nameProp.Attributes = MemberAttributes.Public | MemberAttributes.Override;
				nameProp.Name = "Name";
				nameProp.Type = new CodeTypeReference(typeof(string));
				nameProp.GetStatements.Add(new CodeMethodReturnStatement(
											new CodePrimitiveExpression(
												buttonLabel
										)));
				retVal.Members.Add(nameProp);

				//and finally add the implementation of OnButton method
				CodeMemberMethod onButtonMeth = new CodeMemberMethod();
				onButtonMeth.Attributes = MemberAttributes.Public | MemberAttributes.Override;
				onButtonMeth.Name = "OnButton";
				onButtonMeth.ReturnType = new CodeTypeReference(typeof(void));
				onButtonMeth.Parameters.Add(new CodeParameterDeclarationExpression(typeof(object), "target"));
				onButtonMeth.Statements.Add(new CodeMethodInvokeExpression(
												//cast the "target" to the "type" and call the method referenced by the MethodMember
												new CodeCastExpression(type, new CodeVariableReferenceExpression("target")),
												minf.Name
											));
				retVal.Members.Add(onButtonMeth);
				return retVal;
			}

			[Remark("Generate an inner class for handling ReadWriteData fields")]
			private CodeTypeDeclaration GenerateFieldIDFW(MemberInfo minf) {
				string newClassName = "GeneratedReadWriteDataFieldView_" + type.Name + "_" + minf.Name;
				CodeTypeDeclaration retVal = new CodeTypeDeclaration(newClassName);
				retVal.TypeAttributes = TypeAttributes.Public | TypeAttributes.Sealed;
				retVal.BaseTypes.Add(typeof(ReadWriteDataFieldView));
				retVal.IsClass = true;

				//reference to instance
				retVal.Members.Add(new CodeMemberField(newClassName, "instance"));
				//constructor
				CodeConstructor constr = new CodeConstructor();
				constr.Attributes = MemberAttributes.Static;
				//add initialization of instance variable to the constructor
				constr.Statements.Add(new CodeAssignStatement(
										new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), "instance"),
										new CodeObjectCreateExpression(newClassName)));
				retVal.Members.Add(constr);

				//now override the Name property
				CodeMemberProperty nameProp = new CodeMemberProperty();
				nameProp.HasGet = true;
				nameProp.Attributes = MemberAttributes.Public | MemberAttributes.Override;
				nameProp.Name = "Name";
				nameProp.Type = new CodeTypeReference(typeof(string));
				nameProp.GetStatements.Add(new CodeMethodReturnStatement(
											new CodePrimitiveExpression(
												minf.Name
										)));
				retVal.Members.Add(nameProp);

				//GetValue method
				CodeMemberMethod getValueMeth = new CodeMemberMethod();
				getValueMeth.Attributes = MemberAttributes.Public | MemberAttributes.Override;
				getValueMeth.Name = "GetValue";
				getValueMeth.ReturnType = new CodeTypeReference(typeof(object));
				getValueMeth.Parameters.Add(new CodeParameterDeclarationExpression(typeof(object), "target"));
				getValueMeth.Statements.Add(new CodeMethodReturnStatement(
												new CodeFieldReferenceExpression(
													//cast the "target" to the "type" and call the method referenced by the MethodMember
													new CodeCastExpression(type, new CodeVariableReferenceExpression("target")),
													minf.Name
											)));
				retVal.Members.Add(getValueMeth);

				//GetStringValue method
				CodeMemberMethod getStringValueMeth = new CodeMemberMethod();
				getStringValueMeth.Attributes = MemberAttributes.Public | MemberAttributes.Override;
				getStringValueMeth.Name = "GetStringValue";
				getStringValueMeth.ReturnType = new CodeTypeReference(typeof(string));
				getStringValueMeth.Parameters.Add(new CodeParameterDeclarationExpression(typeof(object), "target"));
				//add something like this: return ObjectSaver.Save(((SimpleClass)target).foo);
				getStringValueMeth.Statements.Add(new CodeMethodReturnStatement(
													new CodeMethodInvokeExpression(
														new CodeTypeReferenceExpression(typeof(Persistence.ObjectSaver)),
														"Save",
														new CodeFieldReferenceExpression(
															new CodeCastExpression(type,new CodeVariableReferenceExpression("target")),
															minf.Name)
												 )));
				retVal.Members.Add(getStringValueMeth);

				//SetValue method
				CodeMemberMethod setValueMeth = new CodeMemberMethod();
				setValueMeth.Attributes = MemberAttributes.Public | MemberAttributes.Override;
				setValueMeth.Name = "SetValue";
				setValueMeth.ReturnType = new CodeTypeReference(typeof(void));
				setValueMeth.Parameters.Add(new CodeParameterDeclarationExpression(typeof(object), "target"));
				setValueMeth.Parameters.Add(new CodeParameterDeclarationExpression(typeof(object), "value"));
				//add something like this: ((SimpleClass)target).foo = (string)value;
				Type memberType = null;
				if(minf is PropertyInfo)
					memberType = ((PropertyInfo)minf).PropertyType;
				else if(minf is FieldInfo)
					memberType = ((FieldInfo)minf).FieldType;
				setValueMeth.Statements.Add(new CodeAssignStatement(
												new CodeFieldReferenceExpression(
													new CodeCastExpression(type, new CodeVariableReferenceExpression("target")),
													minf.Name),
												new CodeCastExpression(memberType, new CodeVariableReferenceExpression("value"))
											));				
				retVal.Members.Add(setValueMeth);

				//SetStringValue method
				CodeMemberMethod setStringValueMeth = new CodeMemberMethod();
				setStringValueMeth.Attributes = MemberAttributes.Public | MemberAttributes.Override;
				setStringValueMeth.Name = "SetStringValue";
				setStringValueMeth.ReturnType = new CodeTypeReference(typeof(void));
				setStringValueMeth.Parameters.Add(new CodeParameterDeclarationExpression(typeof(object), "target"));
				setStringValueMeth.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "value"));
				//add something like this: ((SimpleClass)target).foo = (string)ObjectSaver.OptimizedLoad_String(value);
				setStringValueMeth.Statements.Add(new CodeAssignStatement(
													new CodeFieldReferenceExpression(
														new CodeCastExpression(type, new CodeVariableReferenceExpression("target")),
														minf.Name),
													new CodeCastExpression(
														typeof(string), 
														new CodeMethodInvokeExpression(
															new CodeTypeReferenceExpression(typeof(Persistence.ObjectSaver)),
															"OptimizedLoad_String",
															new CodeVariableReferenceExpression("value")
													))));
				retVal.Members.Add(setStringValueMeth);

				return retVal;
			}

			[Remark("Generate a class representing an ActionButtonsPage")]
			private CodeTypeDeclaration GenerateActionButtonsPage(List<string> buttonDFVs) {
				string newClassName = type.Name+"ActionButtonsPage";
				CodeTypeDeclaration retVal = new CodeTypeDeclaration(newClassName);
				retVal.TypeAttributes = TypeAttributes.Public | TypeAttributes.Sealed;				
				//retVal.BaseTypes.Add(typeof(AbstractPage<ButtonDataFieldView>));
				retVal.IsClass = true;

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