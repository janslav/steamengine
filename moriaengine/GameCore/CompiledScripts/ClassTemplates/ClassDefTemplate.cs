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
	internal class ClassDefTemplate : ClassTemplateBase {
		internal ClassDefTemplate(ClassTemplateSection section, ClassTemplateSubSection subSection)
			: base(section, subSection) {
		}

		protected override void Process() {
			base.Process();
			GenerateTypeDeclaration();

			Fields();
			DefConstructor();

		}

		private void GenerateTypeDeclaration() {
			generatedType = new CodeTypeDeclaration(section.defClassName);
			generatedType.BaseTypes.Add(section.baseDefClassName);

			generatedType.IsClass = true;
			generatedType.IsPartial = true;
		}


		private void Fields() {
			if (this.subSection != null) {
				foreach (ClassTemplateInstanceField ctfi in this.subSection.fields) {
					generatedType.Members.Add(CodeField(ctfi));
					generatedType.Members.Add(CodeProperty(ctfi));
				}
			}
		}

		private CodeMemberField CodeField(ClassTemplateInstanceField ctfi) {
			CodeMemberField field = new CodeMemberField("FieldValue", ctfi.uncapName);
			field.Attributes=MemberAttributes.Final|MemberAttributes.Private;
			if (ctfi.isStatic) {
				field.Attributes|=MemberAttributes.Static;
			}
			return field;
		}

		private CodeMemberProperty CodeProperty(ClassTemplateInstanceField ctfi) {
			CodeMemberProperty prop = new CodeMemberProperty();
			prop.Type = new CodeTypeReference(ctfi.typeString);
			prop.Name = ctfi.capName;
			if (ctfi.access == MemberAttributes.Final) {
				ctfi.access |= MemberAttributes.Public;
			}
			prop.Attributes=ctfi.access;
			if (ctfi.isStatic) {
				prop.Attributes|=MemberAttributes.Static;
			}

			//return (type) field.CurrentValue;
			prop.GetStatements.Add(
				new CodeMethodReturnStatement(
					new CodeCastExpression(ctfi.typeString,
						new CodePropertyReferenceExpression(
							new CodeFieldReferenceExpression(
								new CodeThisReferenceExpression(),
								ctfi.uncapName
							),
							"CurrentValue"
						)
					)
				)
			);

			//field.CurrentValue=value;
			prop.SetStatements.Add(
				new CodeAssignStatement(
					new CodePropertyReferenceExpression(
						new CodeFieldReferenceExpression(
							new CodeThisReferenceExpression(),
							ctfi.uncapName
						),
						"CurrentValue"
					),
					new CodeArgumentReferenceExpression("value")
				)
			);
			return prop;
		}

		private void DefConstructor() {
			CodeConstructor defConstructor = new CodeConstructor();
			defConstructor.Attributes=MemberAttributes.Public|MemberAttributes.Final;
			defConstructor.Parameters.Add(new CodeParameterDeclarationExpression("String", "defname"));
			defConstructor.Parameters.Add(new CodeParameterDeclarationExpression("String", "filename"));
			defConstructor.Parameters.Add(new CodeParameterDeclarationExpression("Int32", "headerLine"));

			defConstructor.BaseConstructorArgs.Add(new CodeArgumentReferenceExpression("defname"));
			defConstructor.BaseConstructorArgs.Add(new CodeArgumentReferenceExpression("filename"));
			defConstructor.BaseConstructorArgs.Add(new CodeArgumentReferenceExpression("headerLine"));

			if (this.subSection != null) {
				foreach (ClassTemplateInstanceField ctfi in this.subSection.fields) {
					defConstructor.Statements.Add(
						new CodeAssignStatement(
							new CodeFieldReferenceExpression(
							new CodeThisReferenceExpression(),
							ctfi.uncapName),
						new CodeMethodInvokeExpression(
							new CodeThisReferenceExpression(),
							"InitField_Typed",
							new CodePrimitiveExpression(ctfi.uncapName),
							new CodeSnippetExpression(ctfi.value),
							new CodeTypeOfExpression(ctfi.typeString))));
				}
			}
			generatedType.Members.Add(defConstructor);
		}
	}
}