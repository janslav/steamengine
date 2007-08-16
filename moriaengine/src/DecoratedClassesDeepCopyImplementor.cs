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
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Reflection;
using SteamEngine.Common;
using SteamEngine.Persistence;
using SteamEngine.CompiledScripts;

namespace SteamEngine {
	[Summary("Decorate your class by this attribute if you want it to be cloneable using the DeepCopyFactory framework."
	+ "When this attribute is used, LoadingInitializerAttribute is expected to be found on a corresponding member, and will be used to initialize a new instance."
	+ "Members with SaveableData attribute will be considered the members to be copied.")]
	[SeeAlso(typeof(LoadingInitializerAttribute))]
	[AttributeUsage(AttributeTargets.Class|AttributeTargets.Struct)]
	public class DeepCopySaveableClassAttribute : SaveableClassAttribute {
	}

	[Summary("Decorate your class by this attribute if you want it to be cloneable using the DeepCopyFactory framework."
	+ "When this attribute is used, DeepCopyImplementationAttribute is expected to be found on a corresponding member, and will be used to deep copy a new instance.")]
	[SeeAlso(typeof(LoadingInitializerAttribute))]
	[AttributeUsage(AttributeTargets.Class|AttributeTargets.Struct)]
	public class ManualDeepCopyClassAttribute : Attribute{
	}

	[Summary("Use this to decorate a static method or constructor that implements deep copying of instances of the given class. "
	+ "It must have one parameter of it's type and a return value of assignable type.")]
	[AttributeUsage(AttributeTargets.Method|AttributeTargets.Constructor)]
	public class DeepCopyImplementationAttribute : Attribute {
	}

	//public abstract class DecoratedClassesDeepCopyImplementor : IDeepCopyImplementor {
	//}

	internal class ManualDeepCopyImplementorGenerator : ISteamCSCodeGenerator {
		static List<Type> decoratedClasses = new List<Type>();

		internal static void AddDecoratedClass(Type t) {
			decoratedClasses.Add(t);
		}

		public string FileName {
			get {
				return "ManualDeepCopyImplementor.Generated.cs";
			}
		}

		public System.CodeDom.CodeCompileUnit WriteSources() {
			try {
				CodeCompileUnit codeCompileUnit = new CodeCompileUnit();
				if (decoratedClasses.Count > 0) {
					Logger.WriteDebug("Generating Manual DeepCopy Implementors");

					CodeNamespace ns = new CodeNamespace("SteamEngine.CompiledScripts");
					codeCompileUnit.Namespaces.Add(ns);

					foreach (Type decoratedClass in decoratedClasses) {
						try {
							GeneratedInstance gi = new GeneratedInstance(decoratedClass);
							CodeTypeDeclaration ctd = gi.GetGeneratedType();
							ns.Types.Add(ctd);
						} catch (FatalException) {
							throw;
						} catch (Exception e) {
							Logger.WriteError(decoratedClass.Assembly.GetName().Name, decoratedClass.Name, e);
							return null;
						}
					}
					Logger.WriteDebug("Done generating "+decoratedClasses.Count+" Manual DeepCopy Implementors");
				}
				return codeCompileUnit;
			} finally {
				decoratedClasses.Clear();
			}
		}

		public void HandleAssembly(System.Reflection.Assembly compiledAssembly) {
			
		}

		private class GeneratedInstance {
			Type decoratedClass;
			MethodBase memberToRun = null;

			CodeTypeDeclaration codeTypeDeclatarion;

			internal GeneratedInstance(Type decoratedClass) {
				this.decoratedClass = decoratedClass;
				foreach (MemberInfo mi in decoratedClass.GetMembers()) {
					if (mi.IsDefined(typeof(DeepCopyImplementationAttribute), false)) {
						if (memberToRun != null) {
							throw new SEException("Can not use the "+LogStr.Ident("DeepCopyImplementationAttribute")+" on two class members.");
						} else {
							if ((mi.MemberType&MemberTypes.Constructor) == MemberTypes.Constructor) {
								memberToRun = (MethodBase) mi;
							} else if ((mi.MemberType&MemberTypes.Method) == MemberTypes.Method) {
								MethodInfo meth = (MethodInfo) mi;
								if (!meth.ReturnType.IsAssignableFrom(decoratedClass)) {
									throw new SEException("Incompatible return type of method "+meth);
								}
								if (meth.IsStatic) {
									memberToRun = meth;
								}
							}
							if (memberToRun != null) {
								ParameterInfo[] pars = memberToRun.GetParameters();
								if ((pars.Length != 1) || (pars[0].ParameterType != decoratedClass)) {
									throw new SEException(LogStr.Ident("DeepCopyImplementationAttribute")+" can only be placed on a callable member with one parameter of the same type as is the declaring class.");
								}
							} else {
								throw new SEException(LogStr.Ident("DeepCopyImplementationAttribute")+" can only be placed on a constructor or static method.");
							}
						}
					}					
				}

				if (memberToRun == null) {
					throw new SEException("No proper member with [DeepCopyImplementationAttribute].");
				}
			}


			private void HandleLoadingInitializerAttribute(MemberInfo mi) {

			}

			private CodeMemberMethod GenerateDeepCopyMethod() {
				CodeMemberMethod retVal = new CodeMemberMethod();
				retVal.Attributes = MemberAttributes.Public | MemberAttributes.Final;
				retVal.Name = "DeepCopy";
				retVal.Parameters.Add(new CodeParameterDeclarationExpression(typeof(object), "copyFrom"));
				retVal.ReturnType = new CodeTypeReference(typeof(object));

				CodeExpression convertedParam = new CodeCastExpression(decoratedClass,
					new CodeArgumentReferenceExpression("copyFrom"));

				if (memberToRun is ConstructorInfo) {
					retVal.Statements.Add(
						new CodeMethodReturnStatement(
							new CodeObjectCreateExpression(
								decoratedClass,
								convertedParam)));
				} else {
					retVal.Statements.Add(
						new CodeMethodReturnStatement(
							new CodeMethodInvokeExpression(
								new CodeTypeReferenceExpression(decoratedClass),
								memberToRun.Name,
								convertedParam)));
				}

				return retVal;
			}

			private CodeMemberProperty GenerateHandledTypeProperty() {
				CodeMemberProperty retVal = new CodeMemberProperty();
				retVal.Attributes = MemberAttributes.Public | MemberAttributes.Final; ;
				retVal.Name = "HandledType";
				retVal.Type = new CodeTypeReference(typeof(Type));
				retVal.HasGet = true;
				retVal.HasSet = false;

				retVal.GetStatements.Add(
					new CodeMethodReturnStatement(
						new CodeTypeOfExpression(decoratedClass)));


				return retVal;
			}

			internal CodeTypeDeclaration GetGeneratedType() {
				codeTypeDeclatarion = new CodeTypeDeclaration("GeneratedManualDeepCopyImplementor_"+decoratedClass.Name);
				codeTypeDeclatarion.TypeAttributes = TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed;
				codeTypeDeclatarion.BaseTypes.Add(typeof(IDeepCopyImplementor));
				codeTypeDeclatarion.IsClass = true;

				codeTypeDeclatarion.Members.Add(GenerateDeepCopyMethod());
				codeTypeDeclatarion.Members.Add(GenerateHandledTypeProperty());

				return codeTypeDeclatarion;
			}
		}
	}

	internal class AutoDeepCopyImplementorGenerator /*: ISteamCSCodeGenerator */ {
	    static List<Type> decoratedClasses = new List<Type>();

	    internal static void AddDecoratedClass(Type t) {
	        decoratedClasses.Add(t);
	    }

	//    public string FileName {
	//        get {
	//            return "AutoDeepCopyImplementors.Generated.cs";
	//        }
	//    }

	//    public System.CodeDom.CodeCompileUnit WriteSources() {
	//        try {
	//            CodeCompileUnit codeCompileUnit = new CodeCompileUnit();
	//            if (decoratedClasses.Count > 0) {
	//                Logger.WriteDebug("Generating SaveImplementors");

	//                CodeNamespace ns = new CodeNamespace("SteamEngine.CompiledScripts");
	//                codeCompileUnit.Namespaces.Add(ns);

	//                foreach (Type decoratedClass in decoratedClasses) {
	//                    try {
	//                        GeneratedInstance gi = new GeneratedInstance(decoratedClass);
	//                        CodeTypeDeclaration ctd = gi.GetGeneratedType();
	//                        ns.Types.Add(ctd);
	//                    } catch (FatalException) {
	//                        throw;
	//                    } catch (Exception e) {
	//                        Logger.WriteError(decoratedClass.Assembly.GetName().Name, decoratedClass.Name, e);
	//                        return null;
	//                    }
	//                }
	//                Logger.WriteDebug("Done generating "+decoratedClasses.Count+" SaveImplementors");
	//            }
	//            return codeCompileUnit;
	//        } finally {
	//            decoratedClasses.Clear();
	//        }
	//    }

	//    public void HandleAssembly(System.Reflection.Assembly compiledAssembly) {
	//    }

	//    private class GeneratedInstance {
	//        Type decoratedClass;
	//        MethodBase loadingInitializer = null;
	//        List<FieldInfo> saveableDataFields = new List<FieldInfo>();
	//        List<PropertyInfo> saveableDataProperties = new List<PropertyInfo>();

	//        CodeTypeDeclaration codeTypeDeclatarion;

	//        internal GeneratedInstance(Type decoratedClass) {
	//            this.decoratedClass = decoratedClass;
	//            foreach (MemberInfo mi in decoratedClass.GetMembers()) {
	//                HandleLoadingInitializerAttribute(mi);
	//                HandleSaveableDataAttribute(mi);
	//            }

	//            //now check if we have all attributes we need and if we don't have any evil combination of them
	//            if (loadSection != null) {
	//                if ((loadLine != null) || (loadingInitializer != null)) {
	//                    //(saveableDataFields.Count != 0) || (saveableDataProperties.Count != 0)) {
	//                    throw new SEException("Can not use the "+LogStr.Ident("LoadSectionAttribute")+" along with other auto-loading attributes.");
	//                }
	//            } else if (loadingInitializer == null) {
	//                throw new SEException("No way to instantiate this class when autoloading.");
	//            } else if ((loadLine == null) && (saveableDataFields.Count == 0) && (saveableDataProperties.Count == 0)) {
	//                throw new SEException("No way to load members of this class when autoloading (members not public?).");
	//            }
	//            if ((saveMethod == null) && (saveableDataFields.Count == 0) && (saveableDataProperties.Count == 0)) {
	//                throw new SEException("No way to autosave this class.");
	//            }
	//        }

	//        private void HandleSaveableDataAttribute(MemberInfo mi) {
	//            if (mi.IsDefined(typeof(SaveableDataAttribute), false)) {
	//                bool added = false;
	//                if ((mi.MemberType&MemberTypes.Property) == MemberTypes.Property) {
	//                    PropertyInfo pi = (PropertyInfo) mi;
	//                    MethodInfo[] accessors = pi.GetAccessors();
	//                    if (accessors.Length == 2) {
	//                        if (!accessors[0].IsStatic) {
	//                            saveableDataProperties.Add(pi);
	//                            added = true;
	//                        }
	//                    } else {
	//                        throw new SEException(LogStr.Ident("SaveableDataAttribute")+" can only be placed on fields or properties with both setter and getter.");
	//                    }
	//                } else if ((mi.MemberType&MemberTypes.Field) == MemberTypes.Field) {
	//                    FieldInfo fi = (FieldInfo) mi;
	//                    if (!fi.IsStatic) {
	//                        saveableDataFields.Add(fi);
	//                        added = true;
	//                    }
	//                }
	//                if (!added) {
	//                    throw new SEException(LogStr.Ident("SaveableDataAttribute")+" can only be placed on instance fields or properties.");
	//                }
	//            }
	//        }

	//        private void HandleLoadingInitializerAttribute(MemberInfo mi) {
	//            if (mi.IsDefined(typeof(LoadingInitializerAttribute), false)) {
	//                if (loadingInitializer != null) {
	//                    throw new SEException("Can not use the "+LogStr.Ident("LoadingInitializerAttribute")+" on two class members.");
	//                } else if ((mi.MemberType&MemberTypes.Constructor) == MemberTypes.Constructor) {
	//                    loadingInitializer = (MethodBase) mi;
	//                } else if ((mi.MemberType&MemberTypes.Method) == MemberTypes.Method) {
	//                    MethodInfo meth = (MethodInfo) mi;
	//                    if (meth.IsStatic) {
	//                        loadingInitializer = meth;
	//                    }
	//                }
	//                if (loadingInitializer != null) {
	//                    if (loadingInitializer.GetParameters().Length != 0) {
	//                        throw new SEException(LogStr.Ident("LoadingInitializerAttribute")+" can only be placed on a callable member with no parameters.");
	//                    }
	//                } else {
	//                    throw new SEException(LogStr.Ident("LoadingInitializerAttribute")+" can only be placed on a constructor or static method.");
	//                }
	//            }
	//        }

	//        internal CodeTypeDeclaration GetGeneratedType() {
	//            codeTypeDeclatarion = new CodeTypeDeclaration(decoratedClass.Name+"SaveImplementor");
		//            				codeTypeDeclatarion.TypeAttributes = TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed;
	//            codeTypeDeclatarion.BaseTypes.Add(typeof(DecoratedClassesSaveImplementor));
	//            codeTypeDeclatarion.IsClass = true;

	//            codeTypeDeclatarion.Members.Add(GenerateConstructor());
	//            codeTypeDeclatarion.Members.Add(GenerateLoadSectionMethod());
	//            codeTypeDeclatarion.Members.Add(GenerateSaveMethod());

	//            return codeTypeDeclatarion;
	//        }

	//        private CodeConstructor GenerateConstructor() {
	//            CodeConstructor retVal = new CodeConstructor();
	//            retVal.Attributes = MemberAttributes.Public | MemberAttributes.Final;

	//            retVal.BaseConstructorArgs.Add(new CodeTypeOfExpression(decoratedClass));
	//            retVal.BaseConstructorArgs.Add(new CodePrimitiveExpression(decoratedClass.Name));

	//            return retVal;
	//        }

	//        private CodeMemberMethod GenerateSaveMethod() {
	//            CodeMemberMethod retVal = new CodeMemberMethod();
	//            retVal.Attributes = MemberAttributes.Public | MemberAttributes.Override;

	//            retVal.Name = "Save";
	//            retVal.Parameters.Add(new CodeParameterDeclarationExpression(typeof(object), "objToSave"));
	//            retVal.Parameters.Add(new CodeParameterDeclarationExpression(typeof(SaveStream), "writer"));
	//            retVal.ReturnType = new CodeTypeReference(typeof(void));

	//            //Memory castObjToSave = (Memory) objToSave;
	//            retVal.Statements.Add(new CodeVariableDeclarationStatement(
	//                this.decoratedClass, "castObjToSave",
	//                new CodeCastExpression(this.decoratedClass,
	//                    new CodeArgumentReferenceExpression("objToSave"))));
	//            if (this.loadSection == null) {
	//                //writer.WriteValue("Flags", castObjToSave.Flags);
	//                foreach (PropertyInfo pi in saveableDataProperties) {
	//                    retVal.Statements.Add(new CodeMethodInvokeExpression(
	//                        new CodeArgumentReferenceExpression("writer"),
	//                        "WriteValue",
	//                        new CodePrimitiveExpression(pi.Name),
	//                        new CodePropertyReferenceExpression(
	//                            new CodeVariableReferenceExpression("castObjToSave"),
	//                            pi.Name)));
	//                }
	//                foreach (FieldInfo fi in saveableDataFields) {
	//                    retVal.Statements.Add(new CodeMethodInvokeExpression(
	//                        new CodeArgumentReferenceExpression("writer"),
	//                        "WriteValue",
	//                        new CodePrimitiveExpression(fi.Name),
	//                        new CodeFieldReferenceExpression(
	//                            new CodeVariableReferenceExpression("castObjToSave"),
	//                            fi.Name)));
	//                }
	//            }
	//            if (this.saveMethod != null) {
	//                retVal.Statements.Add(new CodeMethodInvokeExpression(
	//                    new CodeVariableReferenceExpression("castObjToSave"),
	//                    saveMethod.Name,
	//                    new CodeArgumentReferenceExpression("writer")));
	//            }

	//            return retVal;
	//        }

	//        private CodeMemberMethod GenerateLoadSectionMethod() {
	//            CodeMemberMethod retVal = new CodeMemberMethod();
	//            retVal.Attributes = MemberAttributes.Public | MemberAttributes.Override;

	//            retVal.Name = "LoadSection";
	//            retVal.Parameters.Add(new CodeParameterDeclarationExpression(typeof(PropsSection), "input"));
	//            retVal.ReturnType = new CodeTypeReference(typeof(object));

	//            //int currentLineNumber = input.headerLine;
	//            retVal.Statements.Add(new CodeVariableDeclarationStatement(
	//                typeof(int), "currentLineNumber",
	//                new CodeFieldReferenceExpression(
	//                    new CodeArgumentReferenceExpression("input"),
	//                    "headerLine")));

	//            CodeTryCatchFinallyStatement trycatch = new CodeTryCatchFinallyStatement();
	//            retVal.Statements.Add(trycatch);

	//            //Memory loadedObject = new Memory();
	//            CodeExpression createObjectExpression;
	//            if (this.loadSection != null) {
	//                if (this.loadSection.IsConstructor) {
	//                    createObjectExpression = new CodeObjectCreateExpression(
	//                        this.decoratedClass, new CodeArgumentReferenceExpression("input"));
	//                } else {
	//                    createObjectExpression = new CodeMethodInvokeExpression(
	//                        new CodeTypeReferenceExpression(this.decoratedClass),
	//                        loadSection.Name,
	//                        new CodeArgumentReferenceExpression("input"));
	//                }
	//            } else if (this.loadingInitializer.IsConstructor) {
	//                createObjectExpression = new CodeObjectCreateExpression(
	//                    this.decoratedClass);
	//            } else {
	//                createObjectExpression = new CodeMethodInvokeExpression(
	//                    new CodeTypeReferenceExpression(this.decoratedClass),
	//                    loadingInitializer.Name);
	//            }

	//            trycatch.TryStatements.Add(new CodeVariableDeclarationStatement(
	//                this.decoratedClass, "loadedObject", createObjectExpression));

	//            if (this.loadSection == null) {
	//                foreach (PropertyInfo pi in saveableDataProperties) {
	//                    CodeAssignStatement propertyAssignment = new CodeAssignStatement();
	//                    propertyAssignment.Left = new CodePropertyReferenceExpression(
	//                        new CodeVariableReferenceExpression("loadedObject"),
	//                        pi.Name);

	//                    GenerateLoadFieldOrProperty(trycatch.TryStatements, propertyAssignment,
	//                        pi.Name, pi.PropertyType);
	//                }

	//                foreach (FieldInfo fi in saveableDataFields) {
	//                    CodeAssignStatement fieldAssignment = new CodeAssignStatement();
	//                    fieldAssignment.Left = new CodeFieldReferenceExpression(
	//                        new CodeVariableReferenceExpression("loadedObject"),
	//                        fi.Name);
	//                    GenerateLoadFieldOrProperty(trycatch.TryStatements, fieldAssignment,
	//                        fi.Name, fi.FieldType);
	//                }
	//            }

	//            //we generate a method that only will call the real LoadLine method, and we call it by a 
	//            //helper method on DecoratedClassSaveImplementor. The reason is that there's no foreach codedom statement
	//            if (this.loadLine != null) {
	//                trycatch.TryStatements.Add(new CodeMethodInvokeExpression(
	//                    new CodeThisReferenceExpression(),
	//                    "LoadSectionLines",
	//                    new CodeArgumentReferenceExpression("input"),
	//                    new CodeVariableReferenceExpression("loadedObject")));
	//                GenerateLoadLineImplMethod();
	//            }

	//            //return
	//            trycatch.TryStatements.Add(new CodeMethodReturnStatement(
	//                new CodeVariableReferenceExpression("loadedObject")));

	//            //catch (SteamEngine.FatalException ) { throw; }
	//            trycatch.CatchClauses.Add(new CodeCatchClause("",
	//                new CodeTypeReference(typeof(FatalException)),
	//                    new CodeThrowExceptionStatement()));

	//            //} catch (SEException sex) {sex.TryAddFileLineInfo(input.filename, currentLineNumber);throw;}
	//            trycatch.CatchClauses.Add(new CodeCatchClause("sex",
	//                new CodeTypeReference(typeof(SEException)),
	//                    new CodeExpressionStatement(
	//                        new CodeMethodInvokeExpression(
	//                            new CodeVariableReferenceExpression("sex"),
	//                            "TryAddFileLineInfo",
	//                            new CodeFieldReferenceExpression(
	//                                new CodeArgumentReferenceExpression("input"),
	//                                "filename"),
	//                            new CodeVariableReferenceExpression("currentLineNumber"))),
	//                    new CodeThrowExceptionStatement()));

	//            //throw new SEException(Common.LogStr.FileLine(input.filename, currentLineNumber)+CoreLogger.ErrText(e));
	//            trycatch.CatchClauses.Add(new CodeCatchClause("e",
	//                new CodeTypeReference(typeof(Exception)),
	//                new CodeThrowExceptionStatement(
	//                    new CodeObjectCreateExpression(typeof(SEException),
	//                        new CodeFieldReferenceExpression(
	//                            new CodeArgumentReferenceExpression("input"),
	//                            "filename"),
	//                        new CodeVariableReferenceExpression("currentLineNumber"),
	//                        new CodeVariableReferenceExpression("e")))));

	//            return retVal;
	//        }

	//        private void GenerateLoadLineImplMethod() {
	//            CodeMemberMethod method = new CodeMemberMethod();
	//            method.Attributes = MemberAttributes.Family | MemberAttributes.Override;

	//            method.Name = "LoadLineImpl";
	//            method.Parameters.Add(new CodeParameterDeclarationExpression(typeof(object), "loadedObject"));
	//            method.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "filename"));
	//            method.Parameters.Add(new CodeParameterDeclarationExpression(typeof(int), "line"));
	//            method.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "name"));
	//            method.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "value"));
	//            method.ReturnType = new CodeTypeReference(typeof(void));

	//            method.Statements.Add(new CodeVariableDeclarationStatement(
	//                this.decoratedClass,
	//                "castLoadedObject",
	//                new CodeCastExpression(
	//                    this.decoratedClass,
	//                    new CodeArgumentReferenceExpression("loadedObject"))));

	//            method.Statements.Add(new CodeMethodInvokeExpression(
	//                new CodeVariableReferenceExpression("castLoadedObject"),
	//                loadLine.Name,
	//                new CodeArgumentReferenceExpression("filename"),
	//                new CodeArgumentReferenceExpression("line"),
	//                new CodeArgumentReferenceExpression("name"),
	//                new CodeArgumentReferenceExpression("value")));

	//            this.codeTypeDeclatarion.Members.Add(method);
	//        }

	//        private void GenerateLoadFieldOrProperty(CodeStatementCollection statements, CodeAssignStatement assignment, string name, Type type) {
	//            string propsLineName = Utility.Uncapitalize(name+"Line");

	//            statements.Add(new CodeVariableDeclarationStatement(
	//                typeof(PropsLine), propsLineName,
	//                new CodeMethodInvokeExpression(
	//                    new CodeArgumentReferenceExpression("input"),
	//                    "TryPopPropsLine",
	//                    new CodePrimitiveExpression(name))));

	//            CodeConditionStatement ifStatement = new CodeConditionStatement(
	//                new CodeBinaryOperatorExpression(
	//                    new CodeVariableReferenceExpression(propsLineName),
	//                    CodeBinaryOperatorType.IdentityEquality,
	//                    new CodePrimitiveExpression(null)),
	//                new CodeExpressionStatement(new CodeMethodInvokeExpression(
	//                    new CodeTypeReferenceExpression(typeof(CoreLogger)),
	//                    "WriteWarning",
	//                    new CodeFieldReferenceExpression(
	//                        new CodeArgumentReferenceExpression("input"),
	//                        "filename"),
	//                    new CodeFieldReferenceExpression(
	//                        new CodeArgumentReferenceExpression("input"),
	//                        "headerLine"),
	//                    new CodePrimitiveExpression("Missing value '"+name+"' in loaded section"))));
	//            statements.Add(ifStatement);

	//            ifStatement.FalseStatements.Add(new CodeAssignStatement(
	//                new CodeVariableReferenceExpression("currentLineNumber"),
	//                new CodeFieldReferenceExpression(
	//                    new CodeVariableReferenceExpression(propsLineName),
	//                    "line")));

	//            if (ObjectSaver.IsSimpleSaveableType(type)) {
	//                //we directly assign the value to the field/property

	//                MethodInfo convertMethod = typeof(Convert).GetMethod("To"+type.Name, BindingFlags.Static|BindingFlags.Public, null,
	//                    new Type[] { typeof(object) }, null);
	//                if (convertMethod != null) {
	//                    assignment.Right = new CodeMethodInvokeExpression(
	//                        new CodeTypeReferenceExpression(typeof(Convert)),
	//                        convertMethod.Name,
	//                        new CodeMethodInvokeExpression(
	//                            new CodeTypeReferenceExpression(typeof(ObjectSaver)),
	//                            "Load",
	//                            new CodeFieldReferenceExpression(
	//                                new CodeVariableReferenceExpression(propsLineName),
	//                                "value")));
	//                } else {
	//                    assignment.Right = new CodeCastExpression(
	//                        type,
	//                        new CodeMethodInvokeExpression(
	//                            new CodeTypeReferenceExpression(typeof(ObjectSaver)),
	//                            "Load",
	//                            new CodeFieldReferenceExpression(
	//                                new CodeVariableReferenceExpression(propsLineName),
	//                                "value")));
	//                }

	//                ifStatement.FalseStatements.Add(assignment);

	//            } else {
	//                //we create a method for delayed loading of the value

	//                string delayedLoadMethodName = GenerateDelayedCopyMethod(assignment, name, type);

	//                //ObjectSaver.Load(value, new LoadObject(LoadSomething_Delayed), filename, line);
	//                ifStatement.FalseStatements.Add(new CodeMethodInvokeExpression(
	//                    new CodeMethodReferenceExpression(
	//                        new CodeTypeReferenceExpression(typeof(ObjectSaver)), "Load"),
	//                        new CodeFieldReferenceExpression(
	//                            new CodeVariableReferenceExpression(propsLineName),
	//                            "value"),
	//                        new CodeDelegateCreateExpression(
	//                            new CodeTypeReference(typeof(LoadObjectParam)),
	//                            new CodeThisReferenceExpression(),
	//                            delayedLoadMethodName),
	//                        new CodeFieldReferenceExpression(
	//                            new CodeArgumentReferenceExpression("input"),
	//                            "filename"),
	//                        new CodeFieldReferenceExpression(
	//                            new CodeVariableReferenceExpression(propsLineName),
	//                            "line"),
	//                        new CodeVariableReferenceExpression("loadedObject")));

	//                //
	//            }
	//        }
	//        private string GenerateDelayedCopyMethod(CodeAssignStatement assignment, string name, Type type) {
	//            CodeMemberMethod method = new CodeMemberMethod();
	//            method.Attributes = MemberAttributes.Private;

	//            method.Name = "Load"+name+"_Delayed";
	//            method.Parameters.Add(new CodeParameterDeclarationExpression(typeof(object), "resolved"));
	//            method.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "filename"));
	//            method.Parameters.Add(new CodeParameterDeclarationExpression(typeof(int), "line"));
	//            method.Parameters.Add(new CodeParameterDeclarationExpression(typeof(object), "param"));
	//            method.ReturnType = new CodeTypeReference(typeof(void));

	//            //Memory loadedObject = (Memory) param;
	//            method.Statements.Add(new CodeVariableDeclarationStatement(
	//                this.decoratedClass, "loadedObject",
	//                new CodeCastExpression(this.decoratedClass,
	//                    new CodeArgumentReferenceExpression("param"))));

	//            assignment.Right = new CodeCastExpression(
	//                type,
	//                new CodeArgumentReferenceExpression("resolved"));

	//            method.Statements.Add(assignment);

	//            this.codeTypeDeclatarion.Members.Add(method);

	//            return method.Name;
	//        }
	//    }
	}
}