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
using System.IO;
using System.Globalization;
using System.Configuration;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Text;
using System.Text.RegularExpressions;
using SteamEngine.Common;
using SteamEngine.CompiledScripts;

namespace SteamEngine.Persistence {

	[Summary("Decorate your class by this attribute if you want it to be automatically loadable and saveable.")]
	[SeeAlso(typeof(SaveableDataAttribute))]
	[SeeAlso(typeof(LoadSectionAttribute))]
	[SeeAlso(typeof(LoadingInitializerAttribute))]
	[SeeAlso(typeof(LoadLineAttribute))]
	[SeeAlso(typeof(SaveAttribute))]
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
	public sealed class SaveableClassAttribute : Attribute {
		private string description;

		public string Description {
			get {
				return description;
			}
		}

		//no params constructor
		public SaveableClassAttribute() {
		}

		//constructor allowing us to specify the name of the saveableclass displayed in settings dialog
		public SaveableClassAttribute(string description) {
			this.description = description;
		}
	}

	[Summary("Use to mark the simple initializer of loaded instances")]
	[Remark("Use this attribute to decorate the constructor or static method of your class which should"
	+ "be used for creating of an the \"empty\" instance. This instance will then be populated using the "
	+ "LoadLine- and/or SaveableData-decorated members. This method or constructor should be public and have no parameters.")]
	[SeeAlso(typeof(SaveableDataAttribute))]
	[SeeAlso(typeof(SaveableClassAttribute))]
	[SeeAlso(typeof(LoadSectionAttribute))]
	[SeeAlso(typeof(LoadLineAttribute))]
	[SeeAlso(typeof(SaveAttribute))]
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor)]
	public sealed class LoadingInitializerAttribute : Attribute {
	}

	[Summary("Use to mark the initializer that takes whole saved section as it's parameter.")]
	[Remark("Use this attribute to decorate the static method or constructor of your class which should"
	+ "be used for creating of a \"fully populated\" instance. Note that you can not use any of the other auto-loading"
	+ "(SaveableData, LoadLine, LoadingInitializer) attributes if you use this one. In other words, this"
	+ "should implement the whole loading by itself. So, it must be a public (static) member with one "
	+ "parameter of type PropsSection, returning the loaded instance.")]
	[SeeAlso(typeof(LoadingInitializerAttribute))]
	[SeeAlso(typeof(SaveableDataAttribute))]
	[SeeAlso(typeof(LoadingInitializerAttribute))]
	[SeeAlso(typeof(LoadLineAttribute))]
	[SeeAlso(typeof(SaveAttribute))]
	[SeeAlso(typeof(SaveableClassAttribute))]
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor)]
	public sealed class LoadSectionAttribute : Attribute {
		//no params
	}

	[Summary("Use to mark the instance fields and properties to be automatically saved and loaded.")]
	[Remark("Note that any properties decorated by this attribute must be simple, that means no indexers."
	+ "Also note that these fields and attributes must be public.")]
	[SeeAlso(typeof(LoadingInitializerAttribute))]
	[SeeAlso(typeof(LoadSectionAttribute))]
	[SeeAlso(typeof(LoadingInitializerAttribute))]
	[SeeAlso(typeof(LoadLineAttribute))]
	[SeeAlso(typeof(SaveAttribute))]
	[SeeAlso(typeof(SaveableClassAttribute))]
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public sealed class SaveableDataAttribute : Attribute {
		private string description;

		public string Description {
			get {
				return this.description;
			}
		}

		//no params constructor
		public SaveableDataAttribute() {
		}

		//constructor allowing us to specify the displayed name of the attribute
		public SaveableDataAttribute(string description) {
			this.description = description;
		}
	}

	[Summary("Use to mark the instance method that is repeatedly called for every line in the loaded section.")]
	[Remark("Decorate only public instance methods which have one string parameter and return void. "
	+ "Note that if there are also any SaveableDataAttribute-decorated members in the class, they"
	+ "will be loaded first.")]
	[SeeAlso(typeof(LoadingInitializerAttribute))]
	[SeeAlso(typeof(SaveableDataAttribute))]
	[SeeAlso(typeof(LoadSectionAttribute))]
	[SeeAlso(typeof(LoadingInitializerAttribute))]
	[SeeAlso(typeof(SaveAttribute))]
	[SeeAlso(typeof(SaveableClassAttribute))]
	[AttributeUsage(AttributeTargets.Method)]
	public sealed class LoadLineAttribute : Attribute {
		//no params
	}

	[Summary("Use to mark the method that writes out the save section for instances of this class.")]
	[Remark("Use this attribute to decorate the instance method that takes one SaveStream parameter"
	+ "and writes the needed saving data into it. "
	+ "Unless there is an according IBaseClassSaveCoordinator, you actually implement only saving of the \"body\" of the section, "
	+ "not it's header, because that is reserved for use by the ObjectSaver class."
	+ "Also note that there are certain format restrictions you should (or must) follow. "
	+ "For example, the obvious one is that you can't use the format of the header ;)"
	+ "Basically, you should follow the \"name = value\" way of saving."
	+ "If you use SaveableDataAttributes along with this, note that this method will be called before "
	+ "applying those attributes.")]
	[AttributeUsage(AttributeTargets.Method)]
	[SeeAlso(typeof(LoadingInitializerAttribute))]
	[SeeAlso(typeof(SaveableDataAttribute))]
	[SeeAlso(typeof(LoadSectionAttribute))]
	[SeeAlso(typeof(LoadingInitializerAttribute))]
	[SeeAlso(typeof(LoadLineAttribute))]
	[SeeAlso(typeof(SaveableClassAttribute))]
	public sealed class SaveAttribute : Attribute {
		//no params
	}

	public abstract class DecoratedClassesSaveImplementor : ISaveImplementor {
		Type handledType;
		string headerName;

		protected DecoratedClassesSaveImplementor(Type handledType, string headerName) {
			this.handledType = handledType;
			this.headerName = headerName;
		}

		public Type HandledType {
			get {
				return handledType;
			}
		}

		public string HeaderName {
			get {
				return headerName;
			}
		}

		public abstract object LoadSection(PropsSection input);
		public abstract void Save(object objToSave, SaveStream writer);

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1720:AvoidTypeNamesInParameters", MessageId = "1#")]
		protected void LoadSectionLines(PropsSection ps, object loadedObject) {
			foreach (PropsLine p in ps.PropsLines) {
				try {
					LoadLineImpl(loadedObject, ps.Filename, p.Line, p.Name.ToLower(System.Globalization.CultureInfo.InvariantCulture), p.Value);
				} catch (FatalException) {
					throw;
				} catch (Exception ex) {
					Logger.WriteWarning(ps.Filename, p.Line, ex);
				}
			}
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1720:AvoidTypeNamesInParameters", MessageId = "0#")]
		protected virtual void LoadLineImpl(object loadedObject, string filename, int line, string name, string value) {
			throw new SEException("This should not happen.");
		}

	}

	internal sealed class DecoratedClassesSaveImplementorGenerator : ISteamCSCodeGenerator {
		static List<Type> decoratedClasses = new List<Type>();

		internal static bool AddDecoratedClass(Type t, SaveableClassAttribute ignored) {
			decoratedClasses.Add(t);
			return false;
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
		public static void Bootstrap() {
			ClassManager.RegisterSupplyDecoratedClasses<SaveableClassAttribute>(AddDecoratedClass, false);
		}

		public string FileName {
			get {
				return "DecoratedClassesSaveImplementors.Generated.cs";
			}
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		public CodeCompileUnit WriteSources() {
			try {
				CodeCompileUnit codeCompileUnit = new CodeCompileUnit();


				if (decoratedClasses.Count > 0) {
					Logger.WriteDebug("Generating SaveImplementors");

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
					Logger.WriteDebug("Done generating " + decoratedClasses.Count + " SaveImplementors");
				}
				return codeCompileUnit;
			} finally {
				decoratedClasses.Clear();
			}
		}

		public void HandleAssembly(Assembly compiledAssembly) {

		}

		private class GeneratedInstance {
			Type decoratedClass;
			MethodBase loadingInitializer;
			List<FieldInfo> saveableDataFields = new List<FieldInfo>();
			List<PropertyInfo> saveableDataProperties = new List<PropertyInfo>();
			MethodBase loadSection;
			MethodInfo loadLine;
			MethodInfo saveMethod;

			CodeTypeDeclaration codeTypeDeclatarion;

			internal GeneratedInstance(Type decoratedClass) {
				this.decoratedClass = decoratedClass;
				foreach (MemberInfo mi in decoratedClass.GetMembers(
						BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)) {
					HandleLoadingInitializerAttribute(mi);
					HandleLoadLineAttribute(mi);
					HandleSaveableDataAttribute(mi);
					HandleLoadSectionAttribute(mi);
					HandleSaveAttribute(mi);
				}

				//now check if we have all attributes we need and if we don't have any evil combination of them
				if (loadSection != null) {
					if ((loadLine != null) || (loadingInitializer != null)) {
						//(saveableDataFields.Count != 0) || (saveableDataProperties.Count != 0)) {
						throw new SEException("Can not use the " + LogStr.Ident("LoadSectionAttribute") + " along with other auto-loading attributes.");
					}
				} else if (loadingInitializer == null) {
					throw new SEException("No way to instantiate this class when autoloading.");
				} else if ((loadLine == null) && (saveableDataFields.Count == 0) && (saveableDataProperties.Count == 0)) {
					throw new SEException("No way to load members of this class when autoloading (members not public?).");
				}
				if ((saveMethod == null) && (saveableDataFields.Count == 0) && (saveableDataProperties.Count == 0)) {
					throw new SEException("No way to autosave this class.");
				}
			}

			private void HandleSaveAttribute(MemberInfo mi) {
				if (mi.IsDefined(typeof(SaveAttribute), false)) {
					if (saveMethod != null) {
						throw new SEException("Can not use the " + LogStr.Ident("SaveAttribute") + " on two class members.");
					} else if ((mi.MemberType & MemberTypes.Method) == MemberTypes.Method) {
						MethodInfo meth = (MethodInfo) mi;
						if (!meth.IsStatic) {
							ParameterInfo[] pars = meth.GetParameters();
							if ((pars.Length == 1) && (pars[0].ParameterType == typeof(SaveStream))) {
								saveMethod = MemberWrapper.GetWrapperFor(meth);
							}
						}
					}
					if (saveMethod == null) {
						throw new SEException(LogStr.Ident("SaveAttribute") + " can only be placed on an instance method with one parameter of type SaveStream.");
					}
				}
			}

			private void HandleLoadSectionAttribute(MemberInfo mi) {
				if (mi.IsDefined(typeof(LoadSectionAttribute), false)) {
					if (loadSection != null) {
						throw new SEException("Can not use the " + LogStr.Ident("LoadSectionAttribute") + " on two class members.");
					} else if ((mi.MemberType & MemberTypes.Constructor) == MemberTypes.Constructor) {
						loadSection = (MethodBase) mi;
					} else if ((mi.MemberType & MemberTypes.Method) == MemberTypes.Method) {
						MethodInfo meth = (MethodInfo) mi;
						if (meth.IsStatic) {
							loadSection = meth;
						}
					}
					if (loadSection != null) {
						ParameterInfo[] pars = loadSection.GetParameters();
						if ((pars.Length != 1) || (pars[0].ParameterType != typeof(PropsSection))) {
							throw new SEException(LogStr.Ident("LoadSectionAttribute") + " can only be placed on a callable member with one parameter of type PropsSection.");
						}
					} else {
						throw new SEException(LogStr.Ident("LoadSectionAttribute") + " can only be placed on a constructor or static method.");
					}
				}
			}

			private void HandleSaveableDataAttribute(MemberInfo mi) {
				if (mi.IsDefined(typeof(SaveableDataAttribute), false)) {
					bool added = false;
					if ((mi.MemberType & MemberTypes.Property) == MemberTypes.Property) {
						PropertyInfo pi = (PropertyInfo) mi;
						MethodInfo[] accessors = pi.GetAccessors();
						if (accessors.Length == 2) {
							if (!accessors[0].IsStatic) {
								saveableDataProperties.Add(pi);
								added = true;
							}
						} else {
							throw new SEException(LogStr.Ident("SaveableDataAttribute") + " can only be placed on fields or properties with both setter and getter.");
						}
					} else if ((mi.MemberType & MemberTypes.Field) == MemberTypes.Field) {
						FieldInfo fi = (FieldInfo) mi;
						if (!fi.IsStatic) {
							saveableDataFields.Add(fi);
							added = true;
						}
					}
					if (!added) {
						throw new SEException(LogStr.Ident("SaveableDataAttribute") + " can only be placed on instance fields or properties.");
					}
				}
			}

			private void HandleLoadLineAttribute(MemberInfo mi) {
				if (mi.IsDefined(typeof(LoadLineAttribute), false)) {
					if (loadLine != null) {
						throw new SEException("Can not use the " + LogStr.Ident("LoadLineAttribute") + " on two class members.");
					} else if ((mi.MemberType & MemberTypes.Method) == MemberTypes.Method) {
						MethodInfo meth = (MethodInfo) mi;
						if (!meth.IsStatic) {
							ParameterInfo[] pars = meth.GetParameters();
							if ((pars.Length == 4) && (pars[0].ParameterType == typeof(string)) && (pars[1].ParameterType == typeof(int))
										&& (pars[2].ParameterType == typeof(string)) && (pars[3].ParameterType == typeof(string))) {
								loadLine = MemberWrapper.GetWrapperFor(meth);
							}
						}
					}
					if (loadLine == null) {
						throw new SEException(LogStr.Ident("LoadLineAttribute") + " can only be placed on an instance method with four parameters of types string, int, string, string.");
					}
				}
			}

			private void HandleLoadingInitializerAttribute(MemberInfo mi) {
				if (mi.IsDefined(typeof(LoadingInitializerAttribute), false)) {
					if (loadingInitializer != null) {
						throw new SEException("Can not use the " + LogStr.Ident("LoadingInitializerAttribute") + " on two class members.");
					} else if ((mi.MemberType & MemberTypes.Constructor) == MemberTypes.Constructor) {
						loadingInitializer = (MethodBase) mi;
					} else if ((mi.MemberType & MemberTypes.Method) == MemberTypes.Method) {
						MethodInfo meth = (MethodInfo) mi;
						if (meth.IsStatic) {
							loadingInitializer = meth;
						}
					}
					if (loadingInitializer != null) {
						if (loadingInitializer.GetParameters().Length != 0) {
							throw new SEException(LogStr.Ident("LoadingInitializerAttribute") + " can only be placed on a callable member with no parameters.");
						}
					} else {
						throw new SEException(LogStr.Ident("LoadingInitializerAttribute") + " can only be placed on a constructor or static method.");
					}
				}
			}

			internal CodeTypeDeclaration GetGeneratedType() {
				codeTypeDeclatarion = new CodeTypeDeclaration("GeneratedSaveImplementor_" + decoratedClass.Name);
				codeTypeDeclatarion.TypeAttributes = TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed;
				codeTypeDeclatarion.BaseTypes.Add(typeof(DecoratedClassesSaveImplementor));
				codeTypeDeclatarion.IsClass = true;

				codeTypeDeclatarion.Members.Add(GenerateConstructor());
				codeTypeDeclatarion.Members.Add(GenerateLoadSectionMethod());
				codeTypeDeclatarion.Members.Add(GenerateSaveMethod());

				return codeTypeDeclatarion;
			}

			private CodeConstructor GenerateConstructor() {
				CodeConstructor retVal = new CodeConstructor();
				retVal.Attributes = MemberAttributes.Public | MemberAttributes.Final;

				retVal.BaseConstructorArgs.Add(new CodeTypeOfExpression(decoratedClass));
				retVal.BaseConstructorArgs.Add(new CodePrimitiveExpression(decoratedClass.Name));

				return retVal;
			}

			private CodeMemberMethod GenerateSaveMethod() {
				CodeMemberMethod retVal = new CodeMemberMethod();
				retVal.Attributes = MemberAttributes.Public | MemberAttributes.Override;

				retVal.Name = "Save";
				retVal.Parameters.Add(new CodeParameterDeclarationExpression(typeof(object), "objToSave"));
				retVal.Parameters.Add(new CodeParameterDeclarationExpression(typeof(SaveStream), "writer"));
				retVal.ReturnType = new CodeTypeReference(typeof(void));

				//Memory castObjToSave = (Memory) objToSave;
				retVal.Statements.Add(new CodeVariableDeclarationStatement(
					this.decoratedClass, "castObjToSave",
					new CodeCastExpression(this.decoratedClass,
						new CodeArgumentReferenceExpression("objToSave"))));
				if (this.loadSection == null) {
					//writer.WriteValue("Flags", castObjToSave.Flags);
					foreach (PropertyInfo pi in saveableDataProperties) {
						retVal.Statements.Add(new CodeMethodInvokeExpression(
							new CodeArgumentReferenceExpression("writer"),
							"WriteValue",
							new CodePrimitiveExpression(pi.Name),
							new CodePropertyReferenceExpression(
								new CodeVariableReferenceExpression("castObjToSave"),
								pi.Name)));
					}
					foreach (FieldInfo fi in saveableDataFields) {
						retVal.Statements.Add(new CodeMethodInvokeExpression(
							new CodeArgumentReferenceExpression("writer"),
							"WriteValue",
							new CodePrimitiveExpression(fi.Name),
							new CodeFieldReferenceExpression(
								new CodeVariableReferenceExpression("castObjToSave"),
								fi.Name)));
					}
				}
				if (this.saveMethod != null) {
					retVal.Statements.Add(new CodeMethodInvokeExpression(
						new CodeVariableReferenceExpression("castObjToSave"),
						saveMethod.Name,
						new CodeArgumentReferenceExpression("writer")));
				}

				return retVal;
			}

			private CodeMemberMethod GenerateLoadSectionMethod() {
				CodeMemberMethod retVal = new CodeMemberMethod();
				retVal.Attributes = MemberAttributes.Public | MemberAttributes.Override;

				retVal.Name = "LoadSection";
				retVal.Parameters.Add(new CodeParameterDeclarationExpression(typeof(PropsSection), "input"));
				retVal.ReturnType = new CodeTypeReference(typeof(object));

				//int currentLineNumber = input.headerLine;
				retVal.Statements.Add(new CodeVariableDeclarationStatement(
					typeof(int), "currentLineNumber",
					new CodePropertyReferenceExpression(
						new CodeArgumentReferenceExpression("input"),
						"HeaderLine")));

				CodeTryCatchFinallyStatement trycatch = new CodeTryCatchFinallyStatement();
				retVal.Statements.Add(trycatch);

				//Memory loadedObject = new Memory();
				CodeExpression createObjectExpression;
				if (this.loadSection != null) {
					if (this.loadSection.IsConstructor) {
						createObjectExpression = new CodeObjectCreateExpression(
							this.decoratedClass, new CodeArgumentReferenceExpression("input"));
					} else {
						createObjectExpression = new CodeMethodInvokeExpression(
							new CodeTypeReferenceExpression(this.decoratedClass),
							loadSection.Name,
							new CodeArgumentReferenceExpression("input"));

						Type returnedType = ((MethodInfo) loadSection).ReturnType;
						if (this.decoratedClass != returnedType) {
							createObjectExpression = new CodeCastExpression(
								this.decoratedClass,
								createObjectExpression);
						}
					}
				} else if (this.loadingInitializer.IsConstructor) {
					createObjectExpression = new CodeObjectCreateExpression(
						this.decoratedClass);
				} else {
					createObjectExpression = new CodeMethodInvokeExpression(
						new CodeTypeReferenceExpression(this.decoratedClass),
						loadingInitializer.Name);
				}

				trycatch.TryStatements.Add(new CodeVariableDeclarationStatement(
					this.decoratedClass, "loadedObject", createObjectExpression));

				if (this.loadSection == null) {
					foreach (PropertyInfo pi in saveableDataProperties) {
						CodeAssignStatement propertyAssignment = new CodeAssignStatement();
						propertyAssignment.Left = new CodePropertyReferenceExpression(
							new CodeVariableReferenceExpression("loadedObject"),
							pi.Name);

						GenerateLoadFieldOrProperty(trycatch.TryStatements, propertyAssignment,
							pi.Name, pi.PropertyType);
					}

					foreach (FieldInfo fi in saveableDataFields) {
						CodeAssignStatement fieldAssignment = new CodeAssignStatement();
						fieldAssignment.Left = new CodeFieldReferenceExpression(
							new CodeVariableReferenceExpression("loadedObject"),
							fi.Name);
						GenerateLoadFieldOrProperty(trycatch.TryStatements, fieldAssignment,
							fi.Name, fi.FieldType);
					}
				}

				//we generate a method that only will call the real LoadLine method, and we call it by a 
				//helper method on DecoratedClassSaveImplementor. The reason is that there's no foreach codedom statement
				if (this.loadLine != null) {
					trycatch.TryStatements.Add(new CodeMethodInvokeExpression(
						new CodeThisReferenceExpression(),
						"LoadSectionLines",
						new CodeArgumentReferenceExpression("input"),
						new CodeVariableReferenceExpression("loadedObject")));
					GenerateLoadLineImplMethod();
				}

				//return
				trycatch.TryStatements.Add(new CodeMethodReturnStatement(
					new CodeVariableReferenceExpression("loadedObject")));

				//catch (SteamEngine.FatalException ) { throw; }
				trycatch.CatchClauses.Add(new CodeCatchClause("",
					new CodeTypeReference(typeof(FatalException)),
						new CodeThrowExceptionStatement()));

				//} catch (SEException sex) {sex.TryAddFileLineInfo(input.filename, currentLineNumber);throw;}
				trycatch.CatchClauses.Add(new CodeCatchClause("sex",
					new CodeTypeReference(typeof(SEException)),
						new CodeExpressionStatement(
							new CodeMethodInvokeExpression(
								new CodeVariableReferenceExpression("sex"),
								"TryAddFileLineInfo",
								new CodePropertyReferenceExpression(
									new CodeArgumentReferenceExpression("input"),
									"Filename"),
								new CodeVariableReferenceExpression("currentLineNumber"))),
						new CodeThrowExceptionStatement()));

				//throw new SEException(Common.LogStr.FileLine(input.filename, currentLineNumber)+CoreLogger.ErrText(e));
				trycatch.CatchClauses.Add(new CodeCatchClause("e",
					new CodeTypeReference(typeof(Exception)),
					new CodeThrowExceptionStatement(
						new CodeObjectCreateExpression(typeof(SEException),
							new CodePropertyReferenceExpression(
								new CodeArgumentReferenceExpression("input"),
								"Filename"),
							new CodeVariableReferenceExpression("currentLineNumber"),
							new CodeVariableReferenceExpression("e")))));

				return retVal;
			}

			private void GenerateLoadLineImplMethod() {
				CodeMemberMethod method = new CodeMemberMethod();
				method.Attributes = MemberAttributes.Family | MemberAttributes.Override;

				method.Name = "LoadLineImpl";
				method.Parameters.Add(new CodeParameterDeclarationExpression(typeof(object), "loadedObject"));
				method.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "filename"));
				method.Parameters.Add(new CodeParameterDeclarationExpression(typeof(int), "line"));
				method.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "name"));
				method.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "value"));
				method.ReturnType = new CodeTypeReference(typeof(void));

				method.Statements.Add(new CodeVariableDeclarationStatement(
					this.decoratedClass,
					"castLoadedObject",
					new CodeCastExpression(
						this.decoratedClass,
						new CodeArgumentReferenceExpression("loadedObject"))));

				method.Statements.Add(new CodeMethodInvokeExpression(
					new CodeVariableReferenceExpression("castLoadedObject"),
					loadLine.Name,
					new CodeArgumentReferenceExpression("filename"),
					new CodeArgumentReferenceExpression("line"),
					new CodeArgumentReferenceExpression("name"),
					new CodeArgumentReferenceExpression("value")));

				this.codeTypeDeclatarion.Members.Add(method);
			}

			private void GenerateLoadFieldOrProperty(CodeStatementCollection statements, CodeAssignStatement assignment, string name, Type type) {
				string propsLineName = Utility.Uncapitalize(name + "Line");

				statements.Add(new CodeVariableDeclarationStatement(
					typeof(PropsLine), propsLineName,
					new CodeMethodInvokeExpression(
						new CodeArgumentReferenceExpression("input"),
						"TryPopPropsLine",
						new CodePrimitiveExpression(name))));

				CodeConditionStatement ifStatement = new CodeConditionStatement(
					new CodeBinaryOperatorExpression(
						new CodeVariableReferenceExpression(propsLineName),
						CodeBinaryOperatorType.IdentityEquality,
						new CodePrimitiveExpression(null)),
					new CodeExpressionStatement(new CodeMethodInvokeExpression(
						new CodeTypeReferenceExpression(typeof(CoreLogger)),
						"WriteWarning",
						new CodePropertyReferenceExpression(
							new CodeArgumentReferenceExpression("input"),
							"Filename"),
						new CodePropertyReferenceExpression(
							new CodeArgumentReferenceExpression("input"),
							"HeaderLine"),
						new CodePrimitiveExpression("Missing value '" + name + "' in loaded section"))));
				statements.Add(ifStatement);

				ifStatement.FalseStatements.Add(new CodeAssignStatement(
					new CodeVariableReferenceExpression("currentLineNumber"),
					new CodePropertyReferenceExpression(
						new CodeVariableReferenceExpression(propsLineName),
						"Line")));

				if (ObjectSaver.IsSimpleSaveableType(type)) {
					//we directly assign the value to the field/property
					assignment.Right = GeneratedCodeUtil.GenerateSimpleLoadExpression(
						type,
						new CodePropertyReferenceExpression(
							new CodeVariableReferenceExpression(propsLineName),
							"Value"));

					ifStatement.FalseStatements.Add(assignment);

				} else {
					//we create a method for delayed loading of the value

					string delayedLoadMethodName = GenerateDelayedLoadMethod(assignment, name, type);

					//ObjectSaver.Load(value, new LoadObject(LoadSomething_Delayed), filename, line);
					ifStatement.FalseStatements.Add(new CodeMethodInvokeExpression(
						new CodeMethodReferenceExpression(
							new CodeTypeReferenceExpression(typeof(ObjectSaver)), "Load"),
							new CodePropertyReferenceExpression(
								new CodeVariableReferenceExpression(propsLineName),
								"Value"),
							new CodeDelegateCreateExpression(
								new CodeTypeReference(typeof(LoadObjectParam)),
								new CodeThisReferenceExpression(),
								delayedLoadMethodName),
							new CodePropertyReferenceExpression(
								new CodeArgumentReferenceExpression("input"),
								"Filename"),
							new CodePropertyReferenceExpression(
								new CodeVariableReferenceExpression(propsLineName),
								"Line"),
							new CodeVariableReferenceExpression("loadedObject")));

					//
				}
			}
			private string GenerateDelayedLoadMethod(CodeAssignStatement assignment, string name, Type type) {
				CodeMemberMethod method = new CodeMemberMethod();
				method.Attributes = MemberAttributes.Private;

				method.Name = "DelayedLoad_" + name;
				method.Parameters.Add(new CodeParameterDeclarationExpression(typeof(object), "resolved"));
				method.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "filename"));
				method.Parameters.Add(new CodeParameterDeclarationExpression(typeof(int), "line"));
				method.Parameters.Add(new CodeParameterDeclarationExpression(typeof(object), "param"));
				method.ReturnType = new CodeTypeReference(typeof(void));

				//Memory loadedObject = (Memory) param;
				method.Statements.Add(new CodeVariableDeclarationStatement(
					this.decoratedClass, "loadedObject",
					new CodeCastExpression(this.decoratedClass,
						new CodeArgumentReferenceExpression("param"))));

				assignment.Right = GeneratedCodeUtil.GenerateDelayedLoadExpression(
					type,
					new CodeArgumentReferenceExpression("resolved"));

				method.Statements.Add(assignment);

				this.codeTypeDeclatarion.Members.Add(method);

				return method.Name;
			}
		}
	}
}