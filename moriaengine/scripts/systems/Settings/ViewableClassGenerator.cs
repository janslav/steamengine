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
	internal sealed class ViewableClassGenerator : ISteamCSCodeGenerator {
		static List<Type> viewableClasses = new List<Type>();

		//key is type, value is its descriptor
		static Dictionary<Type, Type> viewableDescriptorsForTypes = new Dictionary<Type, Type>();

		internal static int classCounter = 0;

		public CodeCompileUnit WriteSources() {
			try {
				CodeCompileUnit codeCompileUnit = new CodeCompileUnit();				
				if (viewableClasses.Count > 0) {
					Logger.WriteDebug("Generating DataViews and View Descriptors");

					CodeNamespace ns = new CodeNamespace("SteamEngine.CompiledScripts.Dialogs");
					codeCompileUnit.Namespaces.Add(ns);
					
					foreach (Type viewableClass in viewableClasses) {
						try {
							GeneratedInstance gi = new GeneratedInstance(viewableClass);
							if(gi.buttonMethods.Count + gi.fields.Count + gi.properties.Count > 0) {//we have at least one MemberInfo
							    //CodeTypeDeclaration ctd = gi.GetGeneratedType();
							    //ns.Types.Add(ctd);
							}
						} catch (FatalException) {
							throw;
						} catch (Exception e) {
							Logger.WriteError(viewableClass.Assembly.GetName().Name, viewableClass.Name, e);
							return null;
						}
					}
					Logger.WriteDebug("Done generating "+viewableClasses.Count+" DataViews and View Descriptors");
				}

				if(viewableDescriptors.Count > 0) {
					Logger.WriteDebug("Generating View Descriptors");

					if(ns == null) { //add namespace only if it not yet present...
						ns = new CodeNamespace("SteamEngine.CompiledScripts.Dialogs");
						codeCompileUnit.Namespaces.Add(ns);
					}

					foreach(Type descriptorClass in viewableDescriptors) {
						try {
							GeneratedInstanceDesc gi = new GeneratedInstanceDesc(descriptorClass);
							if(gi.buttonMethods.Count + gi.describedFields.Count > 0) {//we have at least one Button method or field to be described
								CodeTypeDeclaration ctd = gi.GetGeneratedType();
								ns.Types.Add(ctd);
							}
						} catch(FatalException) {
							throw;
						} catch(Exception e) {
							Logger.WriteError(descriptorClass.Assembly.GetName().Name, descriptorClass.Name, e);
							return null;
						}
					}
					Logger.WriteDebug("Done generating " + viewableDescriptors.Count + " viewable descriptors");
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
			} else if(Attribute.IsDefined(type, typeof(ViewDescriptorAttribute), false)) {
				Type descHandledType = ((ViewDescriptorAttribute)type.GetCustomAttributes(typeof(ViewDescriptorAttribute), false)[0]).HandledType;
				viewableDescriptorsForTypes.Add(descHandledType,type); //we have found some descriptor class
			}
		}

		[Remark("Go through all descriptors and look if their handled type is assignable from the just generated one")]
		private List<Type> FindDescriptorsForType(Type infoizedType) {
			List<Type> retList = new List<Type>(new TypeHierarchyComparer());
			foreach (Type describedType in viewableDescriptorsForTypes.Keys) {
				if (describedType.IsAssignableFrom(infoizedType)) {
					//a describedType is some parent class of the infoized type
					retList.Add(describedType);//the described types list will be sorted and then used for getting and generating all descriptors
				}
			}
			return retList;
		}

		private class GeneratedInstance {
			//all fields except marked as NoShow
			internal Dictionary<string, FieldInfo> fields = new Dictionary<string, FieldInfo>(StringComparer.OrdinalIgnoreCase);
			internal Dictionary<string, PropertyInfo> properties = new Dictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);
			internal List<MethodInfo> buttonMethods = new List<MethodInfo>();

			//this will be the list containing all descriptor methods from which the dialog action buttons will be created
			internal List<MethodInfo> descButtonMethods = new List<MethodInfo>();
			//in this dictionary will be stored all described fields. we will find out whether the field has Getter or Setter (or both) methods
			internal Dictionary<string, PropertyMethods> describedFields = new Dictionary<string, PropertyMethods>(StringComparer.OrdinalIgnoreCase);
			
			Type type;
			string typeLabel; //label for info dialogs - obtained from ViewableClassAttribute
			CodeTypeDeclaration codeTypeDeclaration;

			internal GeneratedInstance(Type type) {
				this.type = type;
				ViewableClassAttribute vca = (ViewableClassAttribute)type.GetCustomAttributes(typeof(ViewableClassAttribute), false)[0];
				typeLabel = (vca.Name == null ? type.Name : vca.Name); //label will be either the name of the type or specified label
				//binding flags for members of the actual type - do not consider members from the possible parent classes
				BindingFlags forActualMembers = BindingFlags.IgnoreCase|BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly;
				BindingFlags forInheritedMembers = BindingFlags.IgnoreCase|BindingFlags.Instance|BindingFlags.Public;

				//get all fields from the actual Type except for those marked as "NoShow"
				MemberInfo[] fldsMine = type.FindMembers(MemberTypes.Field | MemberTypes.Property, forActualMembers, HasntAttribute, typeof(NoShowAttribute));
				foreach (MemberInfo fld in fldsMine) {
					PropertyInfo propinf = fld as PropertyInfo;
					if(propinf != null) {
						properties.Add(propinf.Name,propinf);
					}
					FieldInfo fldinf = fld as FieldInfo;
					if(fldinf != null) {
						fields.Add(fldinf.Name,fldinf);						
					}
				}
				//now get the fields from the whole hierarchy, but before adding them, check if they are not present in the set (which would mean
				//that we dont want to add it (this would overwrite the membere from the actual Type by the member from parent class with the same name)
				MemberInfo[] fldsAll = type.FindMembers(MemberTypes.Field | MemberTypes.Property, forInheritedMembers, HasntAttribute, typeof(NoShowAttribute));
				foreach (MemberInfo fld in fldsAll) {
					PropertyInfo propinf = fld as PropertyInfo;
					if (propinf != null) {
						if(!properties.ContainsKey(propinf.Name))//isnt this property already in the dictionary?
							properties.Add(propinf.Name,propinf);
					}
					FieldInfo fldinf = fld as FieldInfo;
					if (fldinf != null) {
						if (!fields.ContainsKey(fldinf.Name))//isnt this property already in the dictionary?
							fields.Add(fldinf.Name,fldinf);
					}
				}

				//get all methods from the Type that have the "Button" attribute
				MemberInfo[] mths = type.FindMembers(MemberTypes.Method, forInheritedMembers, HasAttribute, typeof(ButtonAttribute));
				foreach(MemberInfo mi in mths) {
					MethodInfo minf = mi as MethodInfo;
					if(minf != null) {
						buttonMethods.Add(minf);
					}
				}

				//now find any possible descriptors, we will have to add them too...
				List<Type> typeDescriptors = FindDescriptorsForType(type);
			}
			
			internal CodeTypeDeclaration GetGeneratedType() {

				codeTypeDeclaration = new CodeTypeDeclaration("GeneratedDataView_" + type.Name + "_" + ViewableClassGenerator.classCounter++);
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
				foreach(MethodInfo buttonMethod in buttonMethods) {
					CodeTypeDeclaration oneButtonIDFW = GenerateButtonIDFW(buttonMethod);
					buttonDataFieldViews.Add(oneButtonIDFW.Name); //store the name of the new generated type
					codeTypeDeclaration.Members.Add(oneButtonIDFW);
				}

				//then classes for datafields
				List<string> fieldsDataFieldViews = new List<string>();
				foreach(FieldInfo oneField in fields.Values) {
					CodeTypeDeclaration oneFieldIDFW = GenerateFieldIDFW(oneField);
					fieldsDataFieldViews.Add(oneFieldIDFW.Name); //store the name of the new generated type
					codeTypeDeclaration.Members.Add(oneFieldIDFW);
				}
				foreach(PropertyInfo oneProperty in properties.Values) {
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
												buttonMethods.Count
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
				string newClassName = "GeneratedButtonDataFieldView_" + type.Name + "_" + minf.Name + "_" + ViewableClassGenerator.classCounter++;
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
					isReadOnly = !((PropertyInfo)minf).CanWrite;
					MethodInfo setMethod = ((PropertyInfo)minf).GetSetMethod(true);
					if(setMethod != null) //we have the set Method - check if it is public...
						isReadOnly = !((setMethod.Attributes & MethodAttributes.Public) == MethodAttributes.Public);
				} else { //field
					isReadOnly = ((FieldInfo)minf).IsInitOnly;
				}

				Type baseAbstractClass;
				if (isReadOnly) {
					baseAbstractClass = typeof(ReadOnlyDataFieldView);
				} else {
					baseAbstractClass = typeof(ReadWriteDataFieldView);
				}

				string newClassName = "Generated" + baseAbstractClass.Name + "_" + type.Name + "_" + minf.Name + "_" + ViewableClassGenerator.classCounter++;
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
				string newClassName = type.Name + "ActionButtonsPage";
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

		[Remark("Special class used similiarly as GeneratedInstance but only for ViewableDescriptors")]
		private class GeneratedInstanceDesc {
			Type handledType;
			Type descriptorType;
			string descLabel; //label for info dialogs - it will be used unless the ViewableClass for the Type is found
			CodeTypeDeclaration ctd;

			
			internal GeneratedInstanceDesc(Type descriptorType) {
				this.descriptorType = descriptorType;
				ViewDescriptorAttribute vda = (ViewDescriptorAttribute)descriptorType.GetCustomAttributes(typeof(ViewDescriptorAttribute), false)[0];
				descLabel = (vda.Name == null ? handledType.Name : vda.Name); //label will be either the name of the type or specified label
				handledType = vda.HandledType;
				//binding flags for members of the actual type - do not consider members from the possible parent classes, everything is static here!
				BindingFlags forMembers = BindingFlags.IgnoreCase|BindingFlags.Static|BindingFlags.Public;

				//get all methods from the Type that have the "Button" attribute
				MemberInfo[] mths = descriptorType.FindMembers(MemberTypes.Method, forMembers, HasAttribute, typeof(ButtonAttribute));
				foreach(MemberInfo mi in mths) {
					MethodInfo minf = mi as MethodInfo;
					if(minf != null) {
						buttonMethods.Add(minf);
					}
				}

				//get all methods from the Type that have the "GetMethod" attribute
				mths = descriptorType.FindMembers(MemberTypes.Method, forMembers, HasAttribute, typeof(GetMethodAttribute));
				foreach(MemberInfo mi in mths) {
					MethodInfo minf = mi as MethodInfo;
					if(minf != null) {
						//get the name from the attribute
						GetMethodAttribute gmAtt = (GetMethodAttribute)Attribute.GetCustomAttribute(minf, typeof(GetMethodAttribute));
						string fieldName = gmAtt.Name;
						Type fieldType = gmAtt.FieldType;

						PropertyMethods pm;
						if(describedFields.ContainsKey(fieldName)) {
							pm = describedFields[fieldName];//get it from the dictionary
						} else {
							pm = new PropertyMethods(fieldName,fieldType);
							describedFields.Add(fieldName, pm);//store it newly to the dictionary
						}
						pm.GetMethod = minf;
					}
				}

				//get all methods from the Type that have the "SetMethod" attribute
				mths = descriptorType.FindMembers(MemberTypes.Method, forMembers, HasAttribute, typeof(SetMethodAttribute));
				foreach(MemberInfo mi in mths) {
					MethodInfo minf = mi as MethodInfo;
					if(minf != null) {
						//get the name and fieldtype from the attribute
						SetMethodAttribute smAtt = (SetMethodAttribute)Attribute.GetCustomAttribute(minf, typeof(SetMethodAttribute));
						string fieldName = smAtt.Name;
						Type fieldType = smAtt.FieldType;

						PropertyMethods pm;
						if(describedFields.ContainsKey(fieldName)) {
							pm = describedFields[fieldName];//get it from the dictionary
						} else {
							pm = new PropertyMethods(fieldName,fieldType);
							describedFields.Add(fieldName, pm);//store it newly to the dictionary
						}
						pm.SetMethod = minf;
					}
				}
			}

			internal CodeTypeDeclaration GetGeneratedType() {

				ctd = new CodeTypeDeclaration("GeneratedViewDescriptor_" + handledType.Name + "_" + ViewableClassGenerator.classCounter++);
				ctd.TypeAttributes = TypeAttributes.Public | TypeAttributes.Sealed;
				ctd.BaseTypes.Add(typeof(AbstractDataView));
				ctd.IsClass = true;

				//first add two methods for getting the ActionsButtonsPage and DataFieldsPage
				ctd.Members.Add(GenerateGetActionButtonsPageMethod());
				ctd.Members.Add(GenerateGetDataFieldsPageMethod());

				ctd.Members.Add(GenerateGetActionButtonsCountMethod());
				ctd.Members.Add(GenerateGetFieldsCountMethod());
				ctd.Members.Add(GenerateGetNameMethod());
				ctd.Members.Add(GenerateHandledTypeProperty());

				//now create the inner classes of all IDataFieldViews for this class
				//first classes for buttons
				List<string> buttonDataFieldViews = new List<string>();
				foreach(MethodInfo buttonMethod in buttonMethods) {
					CodeTypeDeclaration oneButtonIDFW = GenerateButtonIDFW(buttonMethod);
					buttonDataFieldViews.Add(oneButtonIDFW.Name); //store the name of the new generated type
					ctd.Members.Add(oneButtonIDFW);
				}

				//then classes for datafields
				Dictionary<string,string> describedFieldViews = new Dictionary<string,string>();
				foreach(KeyValuePair<string,PropertyMethods> oneKVP in describedFields) {
					CodeTypeDeclaration oneFieldIDFW = GenerateFieldIDFW(oneKVP.Value);
					describedFieldViews.Add(oneKVP.Key,oneFieldIDFW.Name); //store the name of the newly generated type. use the field name as key
					ctd.Members.Add(oneFieldIDFW);
				}

				//now add a Class representing one Page of action buttons
				ctd.Members.Add(GenerateActionButtonsPage(buttonDataFieldViews));
				//and a Class representing one Page of other datafields
				ctd.Members.Add(GenerateDataFieldsPage(describedFieldViews));

				return ctd;
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
										new CodeObjectCreateExpression(handledType.Name + "DescActionButtonsPage",
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
										new CodeObjectCreateExpression(handledType.Name + "DescDataFieldsPage",
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
												buttonMethods.Count
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
												describedFields.Count
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
												descLabel
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
											new CodeTypeOfExpression(handledType)
										));
				return retVal;
			}

			[Remark("Generate an inner class for handling Action Buttons")]
			private CodeTypeDeclaration GenerateButtonIDFW(MethodInfo minf) {
				ButtonAttribute bat = (ButtonAttribute)minf.GetCustomAttributes(typeof(ButtonAttribute), false)[0];
				string buttonLabel = (bat.Name == null ? minf.Name : bat.Name); //label of the button (could have been specified in ButtonAttribute)
				string newClassName = "GeneratedButtonDataFieldView_" + handledType.Name + "_" + minf.Name + "_" + ViewableClassGenerator.classCounter++;
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
						//call the static method from the Descriptor referenced by MethodInfo
												new CodeTypeReferenceExpression(descriptorType),
												minf.Name,
												new CodeExpression[] {
													new CodeVariableReferenceExpression("target")
												}
											));
				retVal.Members.Add(onButtonMeth);
				return retVal;
			}

			[Remark("Generate an inner class for handling ReadWriteData fields")]
			private CodeTypeDeclaration GenerateFieldIDFW(PropertyMethods propMeth) {				
				bool isReadOnly = !propMeth.HasSetMethod; //if we dont have Set method, it is readonly
				
				//check if we have GetMethod (if not then it is time to throw an exception...)
				//setmethod can miss but get method no!
				if(!propMeth.HasGetMethod) {
					throw new SEException("Missing GetMethod for field "+propMeth.FieldLabel+" in descriptor "+descriptorType.Name);
				}

				Type baseAbstractClass;
				if(isReadOnly) {
					baseAbstractClass = typeof(ReadOnlyDataFieldView);
				} else {
					baseAbstractClass = typeof(ReadWriteDataFieldView);
				}

				string newClassName = "Generated" + baseAbstractClass.Name + "_" + descriptorType.Name + "_" + propMeth.FieldLabel + "_" + ViewableClassGenerator.classCounter++;
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
												propMeth.FieldLabel
										)));
				retVal.Members.Add(getNameMethod);

				//GetValue method
				CodeMemberMethod getValueMeth = new CodeMemberMethod();
				getValueMeth.Attributes = MemberAttributes.Public | MemberAttributes.Override;
				getValueMeth.Name = "GetValue";
				getValueMeth.ReturnType = new CodeTypeReference(typeof(object));
				getValueMeth.Parameters.Add(new CodeParameterDeclarationExpression(typeof(object), "target"));
				getValueMeth.Statements.Add(new CodeMethodReturnStatement(
					//call the static method from the Descriptor referenced by PropMethods
												new CodeMethodInvokeExpression(
													new CodeTypeReferenceExpression(descriptorType),
														propMeth.GetMethod.Name,
														new CodeExpression[] {
															new CodeVariableReferenceExpression("target") }
											)));											
				retVal.Members.Add(getValueMeth);

				//GetStringValue method
				CodeMemberMethod getStringValueMeth = new CodeMemberMethod();
				getStringValueMeth.Attributes = MemberAttributes.Public | MemberAttributes.Override;
				getStringValueMeth.Name = "GetStringValue";
				getStringValueMeth.ReturnType = new CodeTypeReference(typeof(string));
				getStringValueMeth.Parameters.Add(new CodeParameterDeclarationExpression(typeof(object), "target"));
				//add something like this: return ObjectSaver.Save(<returned_value_from_the_descriptor_getmethod>);
				getStringValueMeth.Statements.Add(new CodeMethodReturnStatement(
													new CodeMethodInvokeExpression(
														new CodeTypeReferenceExpression(typeof(ObjectSaver)),
														"Save",
														new CodeMethodInvokeExpression(
																new CodeTypeReferenceExpression(descriptorType),
																propMeth.GetMethod.Name, 
																new CodeExpression[] {
																	new CodeVariableReferenceExpression("target") }
												 ))));
				retVal.Members.Add(getStringValueMeth);
				
				if(!isReadOnly) {
					//SetValue method
					CodeMemberMethod setValueMeth = new CodeMemberMethod();
					setValueMeth.Attributes = MemberAttributes.Public | MemberAttributes.Override;
					setValueMeth.Name = "SetValue";
					setValueMeth.ReturnType = new CodeTypeReference(typeof(void));
					setValueMeth.Parameters.Add(new CodeParameterDeclarationExpression(typeof(object), "target"));
					setValueMeth.Parameters.Add(new CodeParameterDeclarationExpression(typeof(object), "value"));
					//add something like this: DescriptorClass.SetSomeField(target,value);
					setValueMeth.Statements.Add(new CodeMethodInvokeExpression(
														new CodeTypeReferenceExpression(descriptorType),
														propMeth.SetMethod.Name,	
														new CodeExpression[] {
															new CodeVariableReferenceExpression("target"),
															new CodeVariableReferenceExpression("value") }
												));
					retVal.Members.Add(setValueMeth);

					//SetStringValue method
					CodeMemberMethod setStringValueMeth = new CodeMemberMethod();
					setStringValueMeth.Attributes = MemberAttributes.Public | MemberAttributes.Override;
					setStringValueMeth.Name = "SetStringValue";
					setStringValueMeth.ReturnType = new CodeTypeReference(typeof(void));
					setStringValueMeth.Parameters.Add(new CodeParameterDeclarationExpression(typeof(object), "target"));
					setStringValueMeth.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "value"));
					//add something like this: DescriptorClass.SetSomeFiel(target,<stringified_value>)
					setStringValueMeth.Statements.Add(new CodeMethodInvokeExpression(
														new CodeTypeReferenceExpression(descriptorType),
														propMeth.SetMethod.Name,	
														new CodeExpression[] {
															new CodeVariableReferenceExpression("target"),
															//value parameter will be stringified
															GeneratedCodeUtil.GenerateSimpleLoadExpression(propMeth.FieldType, new CodeVariableReferenceExpression("value"))
														}
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
											new CodeTypeOfExpression(propMeth.FieldType)
										   ));
				retVal.Members.Add(fieldTypeProp);

				return retVal;				
			}

			[Remark("Generate a class representing an ActionButtonsPage")]
			private CodeTypeDeclaration GenerateActionButtonsPage(List<string> buttonDFVs) {
				string newClassName = handledType.Name + "DescActionButtonsPage";
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

				if(buttonDFVs.Count > 0) { //if no buttons found, only "default part" od switch - dont show any other coe
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
			private CodeTypeDeclaration GenerateDataFieldsPage(Dictionary<string,string> describedFieldViews) {
				string newClassName = handledType.Name + "DescDataFieldsPage";
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
				int i = 0;
				foreach(string generatedRWDF in describedFieldViews.Values) { //for every Read(Write/Only)DataFieldView add one "case"
					moveNextMeth.Statements.Add(new CodeSnippetStatement("\t\t\t\t\tcase " + i + ":"));
					moveNextMeth.Statements.Add(new CodeAssignStatement(
									new CodeVariableReferenceExpression("current"),
									new CodeFieldReferenceExpression(
										new CodeTypeReferenceExpression(generatedRWDF), "instance")
								));
					moveNextMeth.Statements.Add(new CodeSnippetStatement("\t\t\t\t\t\tbreak;"));
					i++;
				}
				//default section returns false (it means we have no more cases - no more fields to display 
				moveNextMeth.Statements.Add(new CodeSnippetStatement("\t\t\t\t\tdefault:"));
				moveNextMeth.Statements.Add(new CodeMethodReturnStatement(
								new CodePrimitiveExpression(false)
							));
				moveNextMeth.Statements.Add(new CodeSnippetStatement("\t\t\t\t}"));

				if(i > 0) { //if no fields found, only "default part" from switch - dont show anything else
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

			[Remark("Utility class used for holding MethodInfos of Get and Set methods from the descriptor."+
					"Both Get and Set methods are connected with single described field")]
			internal class PropertyMethods {
				private MethodInfo getMethod;
				private MethodInfo setMethod;
				private string fieldLabel;
				private Type fieldType;

				internal PropertyMethods(string fieldLabel, Type fieldType) {
					this.fieldLabel = fieldLabel;
					this.fieldType = fieldType;
				}

				internal bool HasGetMethod {
					get {
						return (getMethod != null);
					}
				}

				internal bool HasSetMethod {
					get {
						return (setMethod != null);
					}
				}

				internal string FieldLabel {
					get {
						return fieldLabel;
					}
					set {
						fieldLabel = value;
					}
				}

				internal Type FieldType {
					get {
						return fieldType;
					}
					set {
						FieldType = value;
					}
				}

				internal MethodInfo GetMethod {
					get {
						return getMethod;
					}
					set {
						getMethod = value;
					}
				}

				internal MethodInfo SetMethod {
					get {
						return setMethod;
					}
					set {
						setMethod = value;
					}
				}
			}

			[Remark("Used in constructor for filtering - obtain all members with given attribute")]
			private static bool HasAttribute(MemberInfo m, object attributeType) {
				Type attType = (Type)attributeType;
				return Attribute.IsDefined(m, attType, false);
			}
		}
	}
}