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
	internal class ThingTemplate : ClassTemplate {
		internal ThingTemplate(ClassTemplateSection section, ClassTemplateSubSection subSection)
			: base(section, subSection) {
		}

		protected override void Process() {
			base.Process();
			DefProperty();
			Constructor();
			
		}

		private void DefProperty() {
			CodeMemberProperty defProperty = new CodeMemberProperty();
			defProperty.Name = "TypeDef";
			defProperty.Attributes = MemberAttributes.Final|MemberAttributes.Public|MemberAttributes.New;
			defProperty.Type = new CodeTypeReference(section.defClassName);
			CodeMethodReturnStatement ret = new CodeMethodReturnStatement(
				new CodeCastExpression(
					section.defClassName,
					new CodePropertyReferenceExpression(
						new CodeThisReferenceExpression(),
						"Def")));
			defProperty.GetStatements.Add(ret);
			generatedType.Members.Add(defProperty);
		}

		private void Constructor() {
			CodeConstructor ctdConstructor = new CodeConstructor();
			ctdConstructor.Attributes=MemberAttributes.Public|MemberAttributes.Final;
			ctdConstructor.Parameters.Add(new CodeParameterDeclarationExpression("ThingDef", "myDef"));
			ctdConstructor.BaseConstructorArgs.Add(new CodeArgumentReferenceExpression("myDef"));
			generatedType.Members.Add(ctdConstructor);
		}
	}
}