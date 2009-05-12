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
using System.IO;
using System.Globalization;
using SteamEngine.Common;
using System.Configuration;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Text;
using System.Text.RegularExpressions;

namespace SteamEngine.CompiledScripts.ClassTemplates {

	internal class ClassTemplate : ClassTemplateBase {
		internal ClassTemplate(ClassTemplateSection section, ClassTemplateSubSection subSection)
			: base(section, subSection) {
		}

		protected override void Process() {
			base.Process();
			GenerateTypeDeclaration();
			Fields();
			CopyConstructor();
			Save();
			LoadLine();
			DefProperty();
		}

		private void GenerateTypeDeclaration() {
			generatedType = new CodeTypeDeclaration(section.className);
			generatedType.BaseTypes.Add(section.baseClassName);

			generatedType.CustomAttributes.Add(new CodeAttributeDeclaration(
				new CodeTypeReference(typeof(DeepCopyableClassAttribute))));
			generatedType.CustomAttributes.Add(new CodeAttributeDeclaration(
				new CodeTypeReference(typeof(Persistence.SaveableClassAttribute))));

			generatedType.IsClass = true;
			generatedType.IsPartial = true;
		}


		private void DefProperty() {
			CodeMemberProperty defProperty = new CodeMemberProperty();
			defProperty.Name = "TypeDef";
			defProperty.Attributes = MemberAttributes.Final | MemberAttributes.Public | MemberAttributes.New;
			defProperty.Type = new CodeTypeReference(section.defClassName);
			CodeMethodReturnStatement ret = new CodeMethodReturnStatement(
				new CodeCastExpression(
					section.defClassName,
					new CodePropertyReferenceExpression(
						new CodeBaseReferenceExpression(),
						"Def")));
			defProperty.GetStatements.Add(ret);
			generatedType.Members.Add(defProperty);
		}

		private void Fields() {
			if (this.subSection != null) {
				foreach (ClassTemplateInstanceField ctfi in this.subSection.fields) {
					CodeMemberField field = new CodeMemberField(ctfi.typeString, ctfi.uncapName);
					field.Attributes = ctfi.access;
					if (ctfi.isStatic) {
						field.Attributes |= MemberAttributes.Static;
					}

					field.InitExpression = new CodeSnippetExpression(ctfi.value);
					generatedType.Members.Add(field);
				}
			}
		}

		private void CopyConstructor() {
			CodeConstructor ctdCopyConstructor = new CodeConstructor();
			ctdCopyConstructor.CustomAttributes.Add(new CodeAttributeDeclaration(
				new CodeTypeReference(typeof(DeepCopyImplementationAttribute))));
			ctdCopyConstructor.Attributes = MemberAttributes.Public | MemberAttributes.Final;
			ctdCopyConstructor.Parameters.Add(new CodeParameterDeclarationExpression(section.className, "copyFrom"));
			ctdCopyConstructor.BaseConstructorArgs.Add(new CodeArgumentReferenceExpression("copyFrom"));

			if (this.subSection != null) {
				foreach (ClassTemplateInstanceField ctif in this.subSection.fields) {
					ctdCopyConstructor.Statements.Add(CopyFieldStatement(ctif));
				}
			}

			generatedType.Members.Add(ctdCopyConstructor);
		}

		private CodeStatement CopyFieldStatement(ClassTemplateInstanceField field) {
			CodeExpression copyFrom = new CodeFieldReferenceExpression(
				new CodeArgumentReferenceExpression("copyFrom"),
				field.uncapName);

			if (field.needsCopying) {
				CodeMemberMethod delayedCopyMethod = DelayedCopyMethod(field);

				return new CodeExpressionStatement(new CodeMethodInvokeExpression(
					new CodeTypeReferenceExpression(typeof(DeepCopyFactory)),
					"GetCopyDelayed",
					copyFrom,
					new CodeDelegateCreateExpression(
						new CodeTypeReference(typeof(SteamEngine.ReturnCopy)),
						new CodeThisReferenceExpression(),
						delayedCopyMethod.Name)
				));
			} else {
				return new CodeAssignStatement(
					new CodeFieldReferenceExpression(
						new CodeThisReferenceExpression(),
						field.uncapName),
					copyFrom);
			}
		}

		private CodeMemberMethod DelayedCopyMethod(ClassTemplateInstanceField field) {
			CodeMemberMethod method = new CodeMemberMethod();

			method.Name = "DelayedCopy_" + field.capName;
			method.Attributes = MemberAttributes.Private;
			method.Parameters.Add(
				new CodeParameterDeclarationExpression(typeof(object), "copy"));

			if (field.type == typeof(object)) {
				method.Statements.Add(new CodeAssignStatement(
					new CodeFieldReferenceExpression(
						new CodeThisReferenceExpression(),
						field.uncapName),
					new CodeArgumentReferenceExpression("copy")));
			} else {
				method.Statements.Add(new CodeAssignStatement(
					new CodeFieldReferenceExpression(
						new CodeThisReferenceExpression(),
						field.uncapName),
					new CodeCastExpression(
						new CodeTypeReference(field.typeString),
						new CodeArgumentReferenceExpression("copy"))));
			}

			generatedType.Members.Add(method);
			return method;
		}


		private void Save() {
			if ((this.subSection != null) && (this.subSection.fields.Count > 0)) {
				CodeMemberMethod save = new CodeMemberMethod();
				save.Name = "Save";
				save.Attributes = MemberAttributes.Public | MemberAttributes.Override;
				save.Parameters.Add(new CodeParameterDeclarationExpression("SaveStream", "output"));
				save.ReturnType = new CodeTypeReference(typeof(void));

				foreach (ClassTemplateInstanceField ctif in this.subSection.fields) {
					save.Statements.Add(SaveFieldStatement(ctif));
				}

				save.Statements.Add(new CodeMethodInvokeExpression( //"base.Save(output);\n";
					new CodeMethodReferenceExpression(
						new CodeBaseReferenceExpression(),
						"Save"),
					new CodeArgumentReferenceExpression("output")));
				generatedType.Members.Add(save);
			}
		}

		private static CodeStatement SaveFieldStatement(ClassTemplateInstanceField field) {
			CodeFieldReferenceExpression fieldExpression = new CodeFieldReferenceExpression(
				new CodeThisReferenceExpression(), field.uncapName);

			return new CodeConditionStatement(
				new CodeBinaryOperatorExpression(
					fieldExpression,
					CodeBinaryOperatorType.IdentityInequality,
					new CodeSnippetExpression(field.value)),
					new CodeExpressionStatement(
						new CodeMethodInvokeExpression(
						new CodeMethodReferenceExpression(
							new CodeArgumentReferenceExpression("output"),
							"WriteValue"),
						new CodePrimitiveExpression(field.uncapName),
						fieldExpression)));

		}

		private void LoadLine() {
			if ((this.subSection != null) && (this.subSection.fields.Count > 0)) {
				CodeMemberMethod load = new CodeMemberMethod();
				load.Name = "LoadLine";
				load.Attributes = MemberAttributes.Public | MemberAttributes.Override;
				load.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "filename"));
				load.Parameters.Add(new CodeParameterDeclarationExpression(typeof(int), "line"));
				load.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "valueName"));
				load.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "valueString"));
				load.ReturnType = new CodeTypeReference(typeof(void));

				load.Statements.Add(new CodeSnippetStatement("\t\t\tswitch (valueName) {\n"));

				foreach (ClassTemplateInstanceField ctif in this.subSection.fields) {
					load.Statements.Add(new CodeSnippetStatement("\t\t\t\tcase \"" + ctif.uncapName.ToLower(System.Globalization.CultureInfo.InvariantCulture) + "\":"));
					load.Statements.Add(LoadFieldStatement(ctif));
					load.Statements.Add(new CodeSnippetStatement("\t\t\t\t\tbreak;\n"));
				}

				load.Statements.Add(new CodeSnippetStatement("\t\t\t\tdefault:\n"));
				load.Statements.Add(new CodeMethodInvokeExpression(
						new CodeMethodReferenceExpression(
							new CodeBaseReferenceExpression(),
							"LoadLine"),
						new CodeArgumentReferenceExpression("filename"),
						new CodeArgumentReferenceExpression("line"),
						new CodeArgumentReferenceExpression("valueName"),
						new CodeArgumentReferenceExpression("valueString")));

				load.Statements.Add(new CodeSnippetStatement("\t\t\t\t\tbreak;\n\t\t\t}"));

				generatedType.Members.Add(load);
			}
		}


		private CodeStatement LoadFieldStatement(ClassTemplateInstanceField field) {
			if ((field.type != null) &&
					SteamEngine.Persistence.ObjectSaver.IsSimpleSaveableType(field.type)) {

				return new CodeAssignStatement(
					new CodeFieldReferenceExpression(
						new CodeThisReferenceExpression(),
						field.uncapName),
					GeneratedCodeUtil.GenerateSimpleLoadExpression(
						field.type,
						new CodeArgumentReferenceExpression("valueString")));

			} else {
				CodeMemberMethod delayedLoadMethod = new CodeMemberMethod();
				delayedLoadMethod.Name = "DelayedLoad_" + field.capName;
				delayedLoadMethod.Attributes = MemberAttributes.Private;
				delayedLoadMethod.Parameters.Add(
					new CodeParameterDeclarationExpression(typeof(object), "resolvedObject"));
				delayedLoadMethod.Parameters.Add(
					new CodeParameterDeclarationExpression(typeof(string), "filename"));
				delayedLoadMethod.Parameters.Add(
					new CodeParameterDeclarationExpression(typeof(int), "line"));

				CodeExpression rightSide;
				if (field.type != null) {
					rightSide = GeneratedCodeUtil.GenerateDelayedLoadExpression(
						field.type,
						new CodeArgumentReferenceExpression("resolvedObject"));
				} else {
					rightSide = new CodeCastExpression(
						new CodeTypeReference(field.typeString),
						new CodeArgumentReferenceExpression("resolvedObject"));
				}
				delayedLoadMethod.Statements.Add(new CodeAssignStatement(
					new CodeFieldReferenceExpression(
						new CodeThisReferenceExpression(),
						field.uncapName),
					rightSide));

				generatedType.Members.Add(delayedLoadMethod);

				return new CodeExpressionStatement(new CodeMethodInvokeExpression(
					//ObjectSaver.Load(value, new LoadObject(LoadSomething_Delayed), filename, line);
					new CodeMethodReferenceExpression(
						new CodeTypeReferenceExpression(typeof(SteamEngine.Persistence.ObjectSaver)), "Load"),
						new CodeArgumentReferenceExpression("valueString"),
						new CodeDelegateCreateExpression(
							new CodeTypeReference(typeof(SteamEngine.Persistence.LoadObject)),
							new CodeThisReferenceExpression(),
							delayedLoadMethod.Name),
						new CodeArgumentReferenceExpression("filename"),
						new CodeArgumentReferenceExpression("line")));
			}
		}
	}
}