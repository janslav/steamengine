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
	class ClassTemplateInstanceField {
		//enum AccessModifier {None, Private, Protected, Internal, Public};
		internal MemberAttributes access = MemberAttributes.Final; //AccessModifier.None;
		internal string typeString;
		internal Type type;
		internal bool needsCopying = true;//copy or not in the copying constructor?
		internal string fieldName;
		internal string propName;
		internal string defaultValue = null;
		internal bool isStatic = false;
		public bool isOnDef = false;
		//private static readonly char[] whiteSpace = new char[] {' ', '\t', '='};
		internal bool isTypeOfDef = false;

		internal CodeMemberMethod delayedLoadMethod;
		internal CodeMemberMethod delayedCopyMethod;
		//internal Codestat



		private static Regex headerRE = new Regex(
			@"^((?<access>(public)|(private)|(protected)|(internal))\s+)?(?<static>static\s+)?(?<type>[a-z][a-z0-9\.<>\[\]\,]*)\s+(?<name>[a-z][a-z0-9\.]*)\s*=\s*(?<defval>.*)\s*",
			RegexOptions.IgnoreCase|RegexOptions.CultureInvariant|RegexOptions.Compiled);

		//[] means optional, | means or, <> is something variable.
		//[public|private|protected|internal] [static] <Type> <Name> = <DefaultValue>
		public ClassTemplateInstanceField(string s, bool isOnDef) {
			this.isOnDef = isOnDef;

			//Console.WriteLine("mathing this string:\""+s+"\"");

			Match m = headerRE.Match(s);
			if (m.Success) {
				GroupCollection gc = m.Groups;
				SetType(gc["type"].Value);
				fieldName = Utility.Uncapitalize(gc["name"].Value);
				propName = Utility.Capitalize(fieldName);
				defaultValue = gc["defval"].Value;
				Group g = gc["static"];
				if (g.Length > 0) {
					isStatic = true;
				}
				g = gc["access"];
				if (g.Length > 0) {
					access = TranslateAccess(g.Value);
				}
			} else {
				throw new ScriptException(BadVarDecl(s));
			}
		}

		private void SetType(string typeName) {
			switch (typeName.ToLower()) {
				case "bool": {
						typeName="Boolean";
						break;
					}
				case "byte": {
						typeName="Byte";
						break;
					}
				case "sbyte": {
						typeName="SByte";
						break;
					}
				case "ushort": {
						typeName="UInt16";
						break;
					}
				case "short": {
						typeName="Int16";
						break;
					}
				case "uint": {
						typeName="UInt32";
						break;
					}
				case "int": {
						typeName="Int32";
						break;
					}
				case "ulong": {
						typeName="UInt64";
						break;
					}
				case "long": {
						typeName="Int64";
						break;
					}
				case "float": {
						typeName="Single";
						break;
					}
				case "double": {
						typeName="Double";
						break;
					}
				case "decimal": {
						typeName="Decimal";
						break;
					}
				case "char": {
						typeName="Char";
						break;
					}
			}
			type = SteamEngine.CompiledScripts.ClassManager.GetType(typeName);
			if (type == null) {
				type = Type.GetType(typeName, false, true);
				if (type == null) {
					type = Type.GetType("System."+typeName, false, true);
					if (type == null) {
						type = Type.GetType("SteamEngine."+typeName, false, true);
						if (type == null) {
							type = Type.GetType("System.Collections."+typeName, false, true);
						}
					}
				}
			}
			if (type != null) {
				if (typeof(Thing).IsAssignableFrom(type)) {
					needsCopying = false;
				} else {
					needsCopying = !DeepCopyFactory.IsNotCopied(type);
				}
				typeString = type.Name;
			} else {
				//Console.WriteLine("unrecognized type "+typeName);
				typeString = typeName;
			}
		}

		//private internal

		internal CodeStatement ToSaveExpression() {
			//if (this.hitpoints!=100) {
			//    output.WriteValue("hitpoints", this.hitpoints);
			//}
			CodeFieldReferenceExpression fieldExpression = new CodeFieldReferenceExpression(
				new CodeThisReferenceExpression(), this.fieldName);

			return new CodeConditionStatement(
				new CodeBinaryOperatorExpression(
					fieldExpression,
					CodeBinaryOperatorType.IdentityInequality,
					new CodeSnippetExpression(this.defaultValue)),
					new CodeExpressionStatement(
						new CodeMethodInvokeExpression(
						new CodeMethodReferenceExpression(
							new CodeArgumentReferenceExpression("output"),
							"WriteValue"),
						new CodePrimitiveExpression(this.fieldName),
						fieldExpression)));
		}

		internal CodeMemberField ToField() {
			if (isOnDef) {
				CodeMemberField field = new CodeMemberField("FieldValue", fieldName);
				field.Attributes=MemberAttributes.Final|MemberAttributes.Private;
				if (isStatic) {
					field.Attributes|=MemberAttributes.Static;
				}
				return field;
			} else {
				//CodeMemberField field = new CodeMemberField(type, TagMath.Capitalize(name));
				//field.Attributes=access;
				CodeMemberField field = new CodeMemberField(typeString, fieldName);
				field.Attributes=access;
				field.InitExpression=new CodeSnippetExpression(defaultValue);
				if (isStatic) {
					field.Attributes|=MemberAttributes.Static;
				}
				return field;
			}
		}

		internal CodeMemberProperty ToProperty() {
			CodeMemberProperty prop = new CodeMemberProperty();
			prop.Type = new CodeTypeReference(typeString);
			prop.Name = propName;
			if (access==MemberAttributes.Final) {
				access|=MemberAttributes.Public;
			}
			prop.Attributes=access;
			if (isStatic) {
				prop.Attributes|=MemberAttributes.Static;
			}
			if (isOnDef) {
				//return (type) field.CurrentValue;
				prop.GetStatements.Add(
					new CodeMethodReturnStatement(
						new CodeCastExpression(typeString,
							new CodePropertyReferenceExpression(
								new CodeFieldReferenceExpression(
									new CodeThisReferenceExpression(),
									fieldName
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
								fieldName
							),
							"CurrentValue"
						),
						new CodeArgumentReferenceExpression("value")
					)
				);
			} else { //is not used :\ we must find a way to determine if the user wants the property around the field
				//return <name>;
				prop.GetStatements.Add(
					new CodeMethodReturnStatement(
						new CodeFieldReferenceExpression(
							new CodeThisReferenceExpression(),
							fieldName
						)
					)
				);

				//<name>=value;
				prop.SetStatements.Add(
					new CodeAssignStatement(
						new CodeFieldReferenceExpression(
							new CodeThisReferenceExpression(),
							fieldName
						),
						new CodeArgumentReferenceExpression("value")
					)
				);
			}

			return prop;
		}

		internal CodeStatement ToAssign() {
			//field = SetFieldMemory("field", defaultValue, typeof(type));
			return new CodeAssignStatement(
				new CodeFieldReferenceExpression(
					new CodeThisReferenceExpression(),
					fieldName
				),
				new CodeMethodInvokeExpression(
					new CodeThisReferenceExpression(),
					"InitField_Typed",
					new CodePrimitiveExpression(fieldName),
					new CodeSnippetExpression(defaultValue),
					new CodeTypeOfExpression(typeString)
				)
			);
		}

		internal CodeStatement ToDirectLoadStatement() {
			Sanity.IfTrueThrow(type == null, "type can't be null in ToDirectLoadStatement()");

			return new CodeAssignStatement(
				new CodeFieldReferenceExpression(
					new CodeThisReferenceExpression(),
					this.fieldName),
				GeneratedCodeUtil.GenerateSimpleLoadExpression(
					this.type, 
					new CodeArgumentReferenceExpression("value")));
		}

		internal CodeMemberMethod ToDelayedLoadMethod() {
			this.delayedLoadMethod = new CodeMemberMethod();
			this.delayedLoadMethod.Name="DelayedLoad_"+Utility.Capitalize(this.fieldName);
			this.delayedLoadMethod.Attributes=MemberAttributes.Private;
			this.delayedLoadMethod.Parameters.Add(
				new CodeParameterDeclarationExpression(typeof(object), "resolvedObject"));
			this.delayedLoadMethod.Parameters.Add(
				new CodeParameterDeclarationExpression(typeof(string), "filename"));
			this.delayedLoadMethod.Parameters.Add(
				new CodeParameterDeclarationExpression(typeof(int), "line"));

			CodeExpression rightSide;
			if (type != null) {
				rightSide = GeneratedCodeUtil.GenerateDelayedLoadExpression(
					this.type,
					new CodeArgumentReferenceExpression("resolvedObject"));
			} else {
				rightSide = new CodeCastExpression(
					new CodeTypeReference(this.typeString),
					new CodeArgumentReferenceExpression("resolvedObject"));
			}
			this.delayedLoadMethod.Statements.Add(new CodeAssignStatement(
				new CodeFieldReferenceExpression(
					new CodeThisReferenceExpression(),
					this.fieldName),
				rightSide));

			return this.delayedLoadMethod;
		}

		internal CodeDelegateCreateExpression ToDelayedLoadMethodDeleg() {
			//ObjectSaver.Load(value, new LoadObject(LoadSomething_Delayed), filename, line);
			if (this.delayedLoadMethod == null) {
				ToDelayedLoadMethod();
			}
			return new CodeDelegateCreateExpression(
				new CodeTypeReference(typeof(SteamEngine.Persistence.LoadObject)),
				new CodeThisReferenceExpression(),
				this.delayedLoadMethod.Name);
		}

		internal CodeStatement ToCopyExpression() {
			Sanity.IfTrueThrow(isOnDef, "Copying def Properties? This should not happen.");
			//field = copyFrom.field;

			CodeExpression copyFrom= new CodeFieldReferenceExpression(
				new CodeArgumentReferenceExpression("copyFrom"), 
				fieldName);
			

			if (needsCopying) {
				return new CodeExpressionStatement(new CodeMethodInvokeExpression(
					new CodeTypeReferenceExpression(typeof(DeepCopyFactory)),
					"GetCopyDelayed",
					copyFrom,
					this.ToDelayedCopyMethodDeleg()
				));
			} else {
				return new CodeAssignStatement(
					new CodeFieldReferenceExpression(
						new CodeThisReferenceExpression(), 
						fieldName), 
					copyFrom);
			}
		}

		internal CodeMemberMethod ToDelayedCopyMethod() {
			if (this.delayedCopyMethod == null) {
				this.delayedCopyMethod = new CodeMemberMethod();
				this.delayedCopyMethod.Name="DelayedCopy_"+Utility.Capitalize(this.fieldName);
				this.delayedCopyMethod.Attributes=MemberAttributes.Private;
				this.delayedCopyMethod.Parameters.Add(
					new CodeParameterDeclarationExpression(typeof(object), "copy"));

				//implementace
				if (type == typeof(object)) {
					this.delayedCopyMethod.Statements.Add(new CodeAssignStatement(
						new CodeFieldReferenceExpression(
							new CodeThisReferenceExpression(),
							this.fieldName),
						new CodeArgumentReferenceExpression("copy")));
				} else {
					this.delayedCopyMethod.Statements.Add(new CodeAssignStatement(
						new CodeFieldReferenceExpression(
							new CodeThisReferenceExpression(),
							this.fieldName),
						new CodeCastExpression(
							new CodeTypeReference(this.typeString),
							new CodeArgumentReferenceExpression("copy"))));
				}
			}
			return this.delayedCopyMethod;
		}

		internal CodeDelegateCreateExpression ToDelayedCopyMethodDeleg() {
			//ObjectSaver.Load(value, new LoadObject(LoadSomething_Delayed), filename, line);
			return new CodeDelegateCreateExpression(
				new CodeTypeReference(typeof(SteamEngine.ReturnCopy)),
				new CodeThisReferenceExpression(),
				this.ToDelayedCopyMethod().Name);
		}
























		private MemberAttributes TranslateAccess(string s) {
			MemberAttributes ret = MemberAttributes.Final;
			switch (s.ToLower()) {
				case "public":
					ret |= MemberAttributes.Public;
					break;
				case "private":
					ret |= MemberAttributes.Private;
					break;
				case "protected":
					ret |= MemberAttributes.Family;
					break;
				case "internal":
					ret |= MemberAttributes.Assembly;
					break;
				default:
					break;
			}
			return ret;
		}

		private string BadVarDecl(string s) {
			return "Invalid variable declaration '"+s+"'. It should be like this:\n//[] means optional, | means or, <> is something variable.\n[public|private|protected|internal] [static] <Type> <Name> = <DefaultValue>";
		}
	}
}