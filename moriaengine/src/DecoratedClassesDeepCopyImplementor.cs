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
	public class DeepCopyableClassAttribute : Attribute {
	}

	[Summary("In classes that have [DeepCopyableClass], use this attribute to decorate public instance fields and properties"
	+ " that are supposed to be copied by the deepcopy framework along with the main object.")]
	[SeeAlso(typeof(LoadingInitializerAttribute))]
	[AttributeUsage(AttributeTargets.Field|AttributeTargets.Property)]
	public class CopyableDataAttribute : Attribute {
	}

	[Summary("Use this to decorate a static method or constructor that implements deep copying of instances of the given class. "
	+ "It must have one parameter of it's type and a return value of assignable type.")]
	[AttributeUsage(AttributeTargets.Method|AttributeTargets.Constructor)]
	public class DeepCopyImplementationAttribute : Attribute {
	}

	//public abstract class DecoratedClassesDeepCopyImplementor : IDeepCopyImplementor {
	//}

	internal sealed class DeepCopyImplementorGenerator : ISteamCSCodeGenerator {
		static List<Type> decoratedClasses = new List<Type>();

		internal static bool AddDecoratedClass(Type t, DeepCopyableClassAttribute ignored) {
			decoratedClasses.Add(t);
			return false;
		}

		public string FileName {
			get {
				return "DeepCopyImplementor.Generated.cs";
			}
		}

		public static void Bootstrap() {
			ClassManager.RegisterSupplyDecoratedClasses<DeepCopyableClassAttribute>(AddDecoratedClass, false);
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
			MethodBase initializer = null;

			List<FieldInfo> copyableDataFields = new List<FieldInfo>();
			List<PropertyInfo> copyableDataProperties = new List<PropertyInfo>();

			CodeTypeDeclaration codeTypeDeclatarion;

			internal GeneratedInstance(Type decoratedClass) {
				this.decoratedClass = decoratedClass;
				foreach (MemberInfo mi in decoratedClass.GetMembers()) {
					HandleDeepCopyImplementationAttribute(decoratedClass, mi);
					HandleCopyableDataAttribute(mi);
				}

				if (initializer == null) {
					throw new SEException("No proper member with [DeepCopyImplementationAttribute].");
				}
			}

			private void HandleDeepCopyImplementationAttribute(Type decoratedClass, MemberInfo mi) {
				if (mi.IsDefined(typeof(DeepCopyImplementationAttribute), false)) {
					if (initializer != null) {
						throw new SEException("Can not use the "+LogStr.Ident("DeepCopyImplementationAttribute")+" on two class members.");
					} else {
						if ((mi.MemberType&MemberTypes.Constructor) == MemberTypes.Constructor) {
							initializer = (MethodBase) mi;
						} else if ((mi.MemberType&MemberTypes.Method) == MemberTypes.Method) {
							MethodInfo meth = (MethodInfo) mi;
							if (!meth.ReturnType.IsAssignableFrom(decoratedClass)) {
								throw new SEException("Incompatible return type of method "+meth);
							}
							if (meth.IsStatic) {
								initializer = meth;
							}
						}
						if (initializer != null) {
							ParameterInfo[] pars = initializer.GetParameters();
							if (pars.Length == 0) {
							} else if ((pars.Length == 1) && (pars[0].ParameterType == decoratedClass)) {
							} else {
								throw new SEException(LogStr.Ident("DeepCopyImplementationAttribute")+" can only be placed on a callable member with one parameter of the same type as is the declaring class, or with zero parameters.");
							}
						} else {
							throw new SEException(LogStr.Ident("DeepCopyImplementationAttribute")+" can only be placed on a constructor or static method.");
						}
					}
				}
			}

			private void HandleCopyableDataAttribute(MemberInfo mi) {
				if (mi.IsDefined(typeof(CopyableDataAttribute), false)) {
					bool added = false;
					if ((mi.MemberType&MemberTypes.Property) == MemberTypes.Property) {
						PropertyInfo pi = (PropertyInfo) mi;
						MethodInfo[] accessors = pi.GetAccessors();
						if (accessors.Length == 2) {
							if (!accessors[0].IsStatic) {
								copyableDataProperties.Add(pi);
								added = true;
							}
						} else {
							throw new SEException(LogStr.Ident("CopyableDataAttribute")+" can only be placed on fields or properties with both setter and getter.");
						}
					} else if ((mi.MemberType&MemberTypes.Field) == MemberTypes.Field) {
						FieldInfo fi = (FieldInfo) mi;
						if (!fi.IsStatic) {
							copyableDataFields.Add(fi);
							added = true;
						}
					}
					if (!added) {
						throw new SEException(LogStr.Ident("CopyableDataAttribute")+" can only be placed on instance fields or properties.");
					}
				}
			}

			private CodeMemberMethod GenerateDeepCopyMethod() {
				CodeMemberMethod retVal = new CodeMemberMethod();
				retVal.Attributes = MemberAttributes.Public | MemberAttributes.Final;
				retVal.Name = "DeepCopy";
				retVal.Parameters.Add(new CodeParameterDeclarationExpression(typeof(object), "copyFrom"));
				retVal.ReturnType = new CodeTypeReference(typeof(object));

				retVal.Statements.Add(new CodeVariableDeclarationStatement(
					decoratedClass,
					"copyFromConverted",
					new CodeCastExpression(decoratedClass,
						new CodeArgumentReferenceExpression("copyFrom"))));


				CodeExpression[] parameters;
				if (initializer.GetParameters().Length > 0) {
					parameters = new CodeExpression[] { 
						new CodeArgumentReferenceExpression("copyFromConverted") };
				} else {
					parameters = new CodeExpression[0];
				}

				CodeExpression initExpression;
				if (initializer is ConstructorInfo) {
					initExpression = new CodeObjectCreateExpression(
						decoratedClass,
						parameters);
				} else {
					initExpression = new CodeMethodInvokeExpression(
						new CodeTypeReferenceExpression(decoratedClass),
						initializer.Name,
						parameters);
				}

				retVal.Statements.Add(new CodeVariableDeclarationStatement(
					decoratedClass,
					"copy",
					initExpression));

				foreach (FieldInfo fi in copyableDataFields) {
					string name = fi.Name;
					retVal.Statements.Add(GenerateCopyOperation(name, fi.FieldType, false));
				}
				foreach (PropertyInfo pi in copyableDataProperties) {
					string name = pi.Name;
					retVal.Statements.Add(GenerateCopyOperation(name, pi.PropertyType, true));
				}

				retVal.Statements.Add(
					new CodeMethodReturnStatement(
						new CodeVariableReferenceExpression("copy")));
				return retVal;
			}

			private CodeStatement GenerateCopyOperation(string name, Type type, bool isProperty) {
				if (DeepCopyFactory.IsNotCopied(type)) {
					CodeExpression from, to;
					if (isProperty) {
						to = new CodePropertyReferenceExpression(
							new CodeVariableReferenceExpression("copy"),
							name);
						from = new CodePropertyReferenceExpression(
							new CodeVariableReferenceExpression("copyFromConverted"),
							name);
					} else {
						to = new CodeFieldReferenceExpression(
							new CodeVariableReferenceExpression("copy"),
							name);
						from = new CodeFieldReferenceExpression(
							new CodeVariableReferenceExpression("copyFromConverted"),
							name);
					}
					return new CodeAssignStatement(to, from);
				} else {
					string methodName = "DelayedGetCopy_"+name;

					CodeMemberMethod method = new CodeMemberMethod();
					method.Attributes = MemberAttributes.Public | MemberAttributes.Final;
					method.Name = methodName;
					method.Parameters.Add(new CodeParameterDeclarationExpression(typeof(object), "copiedValue"));
					method.Parameters.Add(new CodeParameterDeclarationExpression(typeof(object), "self"));

					method.Statements.Add(new CodeVariableDeclarationStatement(
						decoratedClass,
						"copyConverted",
						new CodeCastExpression(decoratedClass,
							new CodeArgumentReferenceExpression("self"))));

					CodeExpression to;
					if (isProperty) {
						to = new CodePropertyReferenceExpression(
							new CodeVariableReferenceExpression("copyConverted"),
							name);
					} else {
						to = new CodeFieldReferenceExpression(
							new CodeVariableReferenceExpression("copyConverted"),
							name);
					}

					method.Statements.Add(new CodeAssignStatement(to, 
						new CodeCastExpression(type,
							new CodeArgumentReferenceExpression("copiedValue"))));
					this.codeTypeDeclatarion.Members.Add(method);

					return new CodeExpressionStatement(new CodeMethodInvokeExpression(
						new CodeTypeReferenceExpression(typeof(DeepCopyFactory)),
						"GetCopyDelayed",
						new CodeVariableReferenceExpression("copyFromConverted"),
						new CodeDelegateCreateExpression(
							new CodeTypeReference(typeof(ReturnCopyParam)),
							new CodeThisReferenceExpression(),
							methodName),
						new CodeVariableReferenceExpression("copy")));

				}
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
				codeTypeDeclatarion = new CodeTypeDeclaration("GeneratedDeepCopyImplementor_"+decoratedClass.Name);
				codeTypeDeclatarion.TypeAttributes = TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed;
				codeTypeDeclatarion.BaseTypes.Add(typeof(IDeepCopyImplementor));
				codeTypeDeclatarion.IsClass = true;

				codeTypeDeclatarion.Members.Add(GenerateDeepCopyMethod());
				codeTypeDeclatarion.Members.Add(GenerateHandledTypeProperty());

				return codeTypeDeclatarion;
			}
		}
	}
}