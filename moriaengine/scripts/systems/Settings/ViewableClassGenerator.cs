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
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.CodeDom;
using System.CodeDom.Compiler;
using SteamEngine.Common;
using SteamEngine.Persistence;
using SteamEngine.CompiledScripts;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts { 
	internal class ViewableClassGenerator : ISteamCSCodeGenerator {
		static List<Type> viewableClasses = new List<Type>();
		static Hashtable typesViewablesTable = new Hashtable(); 
		
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
		public static void CheckViewabilityClass(Type type) {
			//look if the type has this attribute, don't look to parent classes 
			//(if the type has not the attribute but some parent has, we dont care - if we want
			//it to be infoized, we must add a ViewableClass attribute to it)
			if(Attribute.IsDefined(type, typeof(ViewableClassAttribute),false)) {
				viewableClasses.Add(type);				
			}
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
				BindingFlags bindingAttr = BindingFlags.IgnoreCase|BindingFlags.Instance|BindingFlags.Public; //|BindingFlags.Static - co chces jako delat se statickejma memberama omg? -tar

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
				codeTypeDeclaration.Members.Add(GenerateGetActionButtonsPageMethod());
				codeTypeDeclaration.Members.Add(GenerateGetDataFieldsPageMethod());
				
				codeTypeDeclaration.Members.Add(GenerateGetActionButtonsCountMethod());
				codeTypeDeclaration.Members.Add(GenerateGetFieldsCountMethod());
				codeTypeDeclaration.Members.Add(GenerateGetNameMethod());
				codeTypeDeclaration.Members.Add(GenerateHandledTypeProperty());

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
				//and a Class representing one Page of other datafields
				codeTypeDeclaration.Members.Add(GenerateDataFieldsPage(fieldsDataFieldViews));

				return codeTypeDeclaration;
			}

			[Remark("Method for getting one page of action buttons")]
			private CodeMemberMethod GenerateGetActionButtonsPageMethod() {
				CodeMemberMethod retVal = new CodeMemberMethod();
				retVal.Attributes = MemberAttributes.Public | MemberAttributes.Override;
				retVal.Name = "GetActionButtonsPage";
				retVal.Parameters.Add(new CodeParameterDeclarationExpression(typeof(int), "firstLineIndex"));
				retVal.Parameters.Add(new CodeParameterDeclarationExpression(typeof(object), "target"));
				retVal.ReturnType = new CodeTypeReference(typeof(IEnumerable<ButtonDataFieldView>));

				retVal.Statements.Add(new CodeMethodReturnStatement(
										//makes the "new ..." section
										new CodeObjectCreateExpression(type.Name+"ActionButtonsPage",
											//two parameters
											new CodeVariableReferenceExpression("firstLineIndex"),
											new CodeVariableReferenceExpression("target"))
										));				
				return retVal;
			}

			[Remark("Method for getting one page of data fields buttons")]
			private CodeMemberMethod GenerateGetDataFieldsPageMethod() {
				CodeMemberMethod retVal = new CodeMemberMethod();
				retVal.Attributes = MemberAttributes.Public | MemberAttributes.Override;
				retVal.Name = "GetDataFieldsPage";
				retVal.Parameters.Add(new CodeParameterDeclarationExpression(typeof(int), "firstLineIndex"));
				retVal.Parameters.Add(new CodeParameterDeclarationExpression(typeof(object), "target"));
				retVal.ReturnType = new CodeTypeReference(typeof(IEnumerable<IDataFieldView>));

				retVal.Statements.Add(new CodeMethodReturnStatement(
										new CodeObjectCreateExpression(type.Name + "DataFieldsPage",
											new CodeVariableReferenceExpression("firstLineIndex"),
											new CodeVariableReferenceExpression("target"))
										));
				return retVal;
			}

			[Remark("The GetActionButtonsCount method - says how many buttons will there be")]
			private CodeMemberMethod GenerateGetActionButtonsCountMethod() {
				CodeMemberMethod retVal = new CodeMemberMethod();
				retVal.Attributes = MemberAttributes.Public | MemberAttributes.Override;
				retVal.Name = "GetActionButtonsCount";
				retVal.Parameters.Add(new CodeParameterDeclarationExpression(typeof(object), "instance"));
				retVal.ReturnType = new CodeTypeReference(typeof(int));
				retVal.Statements.Add(new CodeMethodReturnStatement(
											new CodePrimitiveExpression(
												butonMethods.Count
										)));
				return retVal;
			}

			[Remark("The GetActionButtonsCount method - says how many rows will there be")]
			private CodeMemberMethod GenerateGetFieldsCountMethod() {
				CodeMemberMethod retVal = new CodeMemberMethod();
				retVal.Attributes = MemberAttributes.Public | MemberAttributes.Override;
				retVal.Name = "GetFieldsCount";
				retVal.Parameters.Add(new CodeParameterDeclarationExpression(typeof(object), "instance"));
				retVal.ReturnType = new CodeTypeReference(typeof(int));
				retVal.Statements.Add(new CodeMethodReturnStatement(
											new CodePrimitiveExpression(
												fields.Count+properties.Count
										)));
				return retVal;
			}

			[Remark("The GetName method")]
			private CodeMemberMethod GenerateGetNameMethod() {
				CodeMemberMethod retVal = new CodeMemberMethod();
				retVal.Attributes = MemberAttributes.Public | MemberAttributes.Override;
				retVal.Name = "GetName";
				retVal.Parameters.Add(new CodeParameterDeclarationExpression(typeof(object), "instance"));
				retVal.ReturnType = new CodeTypeReference(typeof(string));
				retVal.Statements.Add(new CodeMethodReturnStatement(
											new CodePrimitiveExpression(
												typeLabel
										)));
				return retVal;
			}

			[Remark("The HandledType getter")]
			private CodeMemberProperty GenerateHandledTypeProperty() {
				CodeMemberProperty retVal = new CodeMemberProperty();
				retVal.HasGet = true;
				retVal.Attributes = MemberAttributes.Public | MemberAttributes.Override;
				retVal.Name = "HandledType";
				retVal.Type = new CodeTypeReference(typeof(Type));
				retVal.GetStatements.Add(new CodeMethodReturnStatement(
											new CodeTypeOfExpression(type)
										));
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
				CodeMemberField instField = new CodeMemberField(newClassName, "instance");
				instField.Attributes = MemberAttributes.Public | MemberAttributes.Static;
				retVal.Members.Add(instField);
				//constructor
				CodeTypeConstructor constr = new CodeTypeConstructor();
				constr.Attributes = MemberAttributes.Static;
				//add initialization of instance variable to the constructor
				constr.Statements.Add(new CodeAssignStatement(
										new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(newClassName), "instance"),
										new CodeObjectCreateExpression(newClassName)));
				retVal.Members.Add(constr);

				//now override the Name method
				CodeMemberMethod getNameMethod = new CodeMemberMethod();
				getNameMethod.Attributes = MemberAttributes.Public | MemberAttributes.Override;
				getNameMethod.Name = "GetName";
				getNameMethod.Parameters.Add(new CodeParameterDeclarationExpression(typeof(object), "target"));
				getNameMethod.ReturnType = new CodeTypeReference(typeof(string));
				getNameMethod.Statements.Add(new CodeMethodReturnStatement(
											new CodePrimitiveExpression(
												buttonLabel
										)));
				retVal.Members.Add(getNameMethod);

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
				bool isReadOnly = false;
				if (minf is PropertyInfo) {
					isReadOnly = !((PropertyInfo) minf).CanWrite;
				}

				Type baseAbstractClass;
				if (isReadOnly) {
					baseAbstractClass = typeof(ReadOnlyDataFieldView);
				} else {
					baseAbstractClass = typeof(ReadWriteDataFieldView);
				}

				string newClassName = "Generated" + baseAbstractClass.Name + "_" + type.Name + "_" + minf.Name;
				CodeTypeDeclaration retVal = new CodeTypeDeclaration(newClassName);
				retVal.TypeAttributes = TypeAttributes.Public | TypeAttributes.Sealed;
				retVal.BaseTypes.Add(baseAbstractClass);
				retVal.IsClass = true;

				//reference to instance
				CodeMemberField instField = new CodeMemberField(newClassName, "instance");
				instField.Attributes = MemberAttributes.Public | MemberAttributes.Static;
				retVal.Members.Add(instField);
				//constructor
				CodeTypeConstructor constr = new CodeTypeConstructor();
				constr.Attributes = MemberAttributes.Static;
				//add initialization of instance variable to the constructor
				constr.Statements.Add(new CodeAssignStatement(
										new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(newClassName), "instance"),
										new CodeObjectCreateExpression(newClassName)
									  ));
				retVal.Members.Add(constr);

				//now override the Name method
				CodeMemberMethod getNameMethod = new CodeMemberMethod();
				getNameMethod.Attributes = MemberAttributes.Public | MemberAttributes.Override;
				getNameMethod.Name = "GetName";
				getNameMethod.Parameters.Add(new CodeParameterDeclarationExpression(typeof(object), "target"));
				getNameMethod.ReturnType = new CodeTypeReference(typeof(string));
				getNameMethod.Statements.Add(new CodeMethodReturnStatement(
											new CodePrimitiveExpression(
												minf.Name
										)));
				retVal.Members.Add(getNameMethod);

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
														new CodeTypeReferenceExpression(typeof(ObjectSaver)),
														"Save",
														new CodeFieldReferenceExpression(
															new CodeCastExpression(type,new CodeVariableReferenceExpression("target")),
															minf.Name)
												 )));
				retVal.Members.Add(getStringValueMeth);

				Type memberType = null;
				if (minf is PropertyInfo)
					memberType = ((PropertyInfo) minf).PropertyType;
				else if (minf is FieldInfo)
					memberType = ((FieldInfo) minf).FieldType;

				if (!isReadOnly) {
					//SetValue method
					CodeMemberMethod setValueMeth = new CodeMemberMethod();
					setValueMeth.Attributes = MemberAttributes.Public | MemberAttributes.Override;
					setValueMeth.Name = "SetValue";
					setValueMeth.ReturnType = new CodeTypeReference(typeof(void));
					setValueMeth.Parameters.Add(new CodeParameterDeclarationExpression(typeof(object), "target"));
					setValueMeth.Parameters.Add(new CodeParameterDeclarationExpression(typeof(object), "value"));
					//add something like this: ((SimpleClass)target).foo = (string)value;
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
														GeneratedCodeUtil.GenerateSimpleLoadExpression(memberType, new CodeVariableReferenceExpression("value"))
														));
					retVal.Members.Add(setStringValueMeth);
				}

				//override the FieldType property
				CodeMemberProperty fieldTypeProp = new CodeMemberProperty();
				fieldTypeProp.HasGet = true;
				fieldTypeProp.Attributes = MemberAttributes.Public | MemberAttributes.Override;
				fieldTypeProp.Name = "FieldType";
				fieldTypeProp.Type = new CodeTypeReference(typeof(Type));
				fieldTypeProp.GetStatements.Add(new CodeMethodReturnStatement(
											new CodeTypeOfExpression(memberType)
										   ));
				retVal.Members.Add(fieldTypeProp);

				return retVal;
			}

			[Remark("Generate a class representing an ActionButtonsPage")]
			private CodeTypeDeclaration GenerateActionButtonsPage(List<string> buttonDFVs) {
				string newClassName = type.Name+"ActionButtonsPage";
				CodeTypeDeclaration retVal = new CodeTypeDeclaration(newClassName);
				retVal.TypeAttributes = TypeAttributes.Public | TypeAttributes.Sealed;				
				retVal.BaseTypes.Add(typeof(AbstractDataView.AbstractPage<ButtonDataFieldView>));
				retVal.IsClass = true;

				//constructor with two parameters (calling the base(,))
				CodeConstructor constr = new CodeConstructor();
				constr.Attributes = MemberAttributes.Public;
				constr.Parameters.Add(new CodeParameterDeclarationExpression(typeof(int), "firstLineIndex"));
				constr.Parameters.Add(new CodeParameterDeclarationExpression(typeof(object), "target"));
				constr.BaseConstructorArgs.Add(new CodeVariableReferenceExpression("firstLineIndex"));
				constr.BaseConstructorArgs.Add(new CodeVariableReferenceExpression("target"));
				retVal.Members.Add(constr);

				//add the MoveNext method for iterating over the buttons
				CodeMemberMethod moveNextMeth = new CodeMemberMethod();
				moveNextMeth.Attributes = MemberAttributes.Public | MemberAttributes.Override;
				moveNextMeth.Name = "MoveNext";
				moveNextMeth.ReturnType = new CodeTypeReference(typeof(bool));
				//add something like this: 							
						/*switch(nextIndex) {						
							case 0:
								current = GeneratedButtonDataFieldView_SimpleClass_SomeMethod.instance;
								break;
							default:
								//this happens when there are not enough lines to fill the whole page
								//or if we are beginning with the index larger then the overall LinesCount 
								//(which results in the empty page and should not happen)
								return false;
						}
						++nextIndex;//prepare the index for the next round of iteration
						*/				
				moveNextMeth.Statements.Add(new CodeSnippetStatement("\t\t\t\tswitch(nextIndex) {"));
				for(int i = 0; i < buttonDFVs.Count; i++) { //for every ReadWriteDataFieldView add one "case"
					moveNextMeth.Statements.Add(new CodeSnippetStatement("\t\t\t\t\tcase " + i + ":"));
					moveNextMeth.Statements.Add(new CodeAssignStatement(
									new CodeVariableReferenceExpression("current"),
									new CodeFieldReferenceExpression(
										new CodeTypeReferenceExpression(buttonDFVs[i]), "instance")
								));
					moveNextMeth.Statements.Add(new CodeSnippetStatement("\t\t\t\t\t\tbreak;"));
				}
				//default section returns false (it means we have no more cases - no more fields to display 
				moveNextMeth.Statements.Add(new CodeSnippetStatement("\t\t\t\t\tdefault:"));
				moveNextMeth.Statements.Add(new CodeMethodReturnStatement(
								new CodePrimitiveExpression(false)
							));
				moveNextMeth.Statements.Add(new CodeSnippetStatement("\t\t\t\t}"));

				if (buttonDFVs.Count > 0) { //if no buttons found, only "default part" od switch - dont show any other coe
					//increase indexer for the next step
					moveNextMeth.Statements.Add(new CodeAssignStatement(
									new CodeVariableReferenceExpression("nextIndex"),
									new CodeBinaryOperatorExpression(
										new CodeVariableReferenceExpression("nextIndex"),
										CodeBinaryOperatorType.Add,
										new CodePrimitiveExpression(1)
								)));
					//return true -we will continue in iterating
					moveNextMeth.Statements.Add(new CodeMethodReturnStatement(
									new CodePrimitiveExpression(true)
								));
				}
						
				retVal.Members.Add(moveNextMeth);

				return retVal;
			}

			[Remark("Generate a class representing an DataFieldsPage")]
			private CodeTypeDeclaration GenerateDataFieldsPage(List<string> fieldsDFVs) {
				string newClassName = type.Name + "DataFieldsPage";
				CodeTypeDeclaration retVal = new CodeTypeDeclaration(newClassName);
				retVal.TypeAttributes = TypeAttributes.Public | TypeAttributes.Sealed;
				retVal.BaseTypes.Add(typeof(AbstractDataView.AbstractPage<IDataFieldView>));
				retVal.IsClass = true;

				//constructor with two parameters (calling the base(,))
				CodeConstructor constr = new CodeConstructor();
				constr.Attributes = MemberAttributes.Public;
				constr.Parameters.Add(new CodeParameterDeclarationExpression(typeof(int), "firstLineIndex"));
				constr.Parameters.Add(new CodeParameterDeclarationExpression(typeof(object), "target"));
				constr.BaseConstructorArgs.Add(new CodeVariableReferenceExpression("firstLineIndex"));
				constr.BaseConstructorArgs.Add(new CodeVariableReferenceExpression("target"));
				retVal.Members.Add(constr);

				//add the MoveNext method for iterating over the fields
				CodeMemberMethod moveNextMeth = new CodeMemberMethod();
				moveNextMeth.Attributes = MemberAttributes.Public | MemberAttributes.Override;
				moveNextMeth.Name = "MoveNext";
				moveNextMeth.ReturnType = new CodeTypeReference(typeof(bool));
				//add something like this: 
					/*	switch(nextIndex) {						
							case 0:
								current = GeneratedButtonDataFieldView_SimpleClass_SomeMethod.instance;
								break;
							default:
								//this happens when there are not enough lines to fill the whole page
								//or if we are beginning with the index larger then the overall LinesCount 
								//(which results in the empty page and should not happen)
								return false;
						}
						++nextIndex;//prepare the index for the next round of iteration
						return true;
					*/
				//prepare the iteration block
				//List<CodeStatement> iterationBlock = new List<CodeStatement>();

				moveNextMeth.Statements.Add(new CodeSnippetStatement("\t\t\t\tswitch(nextIndex) {"));
				for(int i = 0; i < fieldsDFVs.Count; i++) { //for every ReadWriteDataFieldView add one "case"
					moveNextMeth.Statements.Add(new CodeSnippetStatement("\t\t\t\t\tcase " + i + ":"));
					moveNextMeth.Statements.Add(new CodeAssignStatement(
									new CodeVariableReferenceExpression("current"),
									new CodeFieldReferenceExpression(
										new CodeTypeReferenceExpression(fieldsDFVs[i]), "instance")
								));
					moveNextMeth.Statements.Add(new CodeSnippetStatement("\t\t\t\t\t\tbreak;"));
				}
				//default section returns false (it means we have no more cases - no more fields to display 
				moveNextMeth.Statements.Add(new CodeSnippetStatement("\t\t\t\t\tdefault:"));
				moveNextMeth.Statements.Add(new CodeMethodReturnStatement(
								new CodePrimitiveExpression(false)
							));
				moveNextMeth.Statements.Add(new CodeSnippetStatement("\t\t\t\t}"));

				if (fieldsDFVs.Count > 0) { //if no fields found, only "default part" od switch - dont show anything else

					//increase indexer for the next step
					moveNextMeth.Statements.Add(new CodeAssignStatement(
									new CodeVariableReferenceExpression("nextIndex"),
									new CodeBinaryOperatorExpression(
										new CodeVariableReferenceExpression("nextIndex"),
										CodeBinaryOperatorType.Add,
										new CodePrimitiveExpression(1)
								)));

					//return true -we will continue in iterating				
					moveNextMeth.Statements.Add(new CodeMethodReturnStatement(
									new CodePrimitiveExpression(true)
								));
				}
				retVal.Members.Add(moveNextMeth);

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