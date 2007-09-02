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
	public partial class ClassTemplateInstance {
		string fileName;
		//string ctPath;
		//string outFileName;
		int line;
		CodeCompileUnit codeCompileUnit=null;

		string name = null;
		string baseName = null;
		ArrayList implements = new ArrayList();
		enum SSType { None, DefVars, Vars/*, CS, JS, VB */}
		SSType ssType = SSType.None;

		ArrayList defVars = new ArrayList();
		ArrayList vars = new ArrayList();
		StringBuilder csCode = new StringBuilder();

		public ClassTemplateInstance(string fileName, int line, string sectionName) {
			this.fileName=fileName;
			this.line=line;
			//this.ctPath=ctPath;
			//this.outFileName=outFileName;
			int colon = sectionName.IndexOf(":");
			if (colon>-1) {
				name = Utility.Capitalize(sectionName.Substring(0, colon).Trim());
				baseName = Utility.Capitalize(sectionName.Substring(colon+1).Trim());
			} else {
				name = Utility.Capitalize(sectionName.Trim());
			}
		}


		public void SubSection(string sectionName, int line) {
			//A subsection declaration looks like this: "foobar:"
			switch (sectionName.ToLower()) {
				case "def":
				case "defvars":
				case "defs": {
						ssType = SSType.DefVars;
						break;
					}
				case "var":
				case "vars":
				case "tags": {
						ssType = SSType.Vars;
						break;
					}
				default: {
						throw new ScriptException("Unknown section type '"+sectionName+"' in ClassTemplate script '"+fileName+" near line "+line+". Valid section types: (def, defvars, or defs), (var, vars, or tags), (cs or container#), (vb or vb.net), (js, jscript, jscript.net, js#, or j#)");
					}

			}
		}
		public void Line(string origS, string s, int line) {
			if (s.Length==0) return;
			int equals = s.IndexOf('=');
			string key = "";
			string value = "";
			if (equals>-1) {
				key = s.Substring(0, equals);
				if (equals<s.Length-1) {
					value=s.Substring(equals+1);
				}
			} else {
				key=s;
			}
			if (key.Length==0 && value.Length==0) return;
			if (key.Length==0) {
				Warning(line, "Invalid line '"+s+"': There's nothing on the left side of the '='!");
			} else {
				switch (key.ToLower()) {
					case "base":
					case "baseclass":
					case "parent":
					case "parentclass":
					case "extends": {
							baseName=Utility.Capitalize(value);
							break;
						}
					case "implements": {
							implements.Add(Utility.Capitalize(value));
							break;
						}
					default: {
							Warning(line, "Unknown key '"+key+"' in '"+s+"'. Valid keys are (base, baseclass, parent, parentclass, or extends) for specifying the base/parent class, and (implements) for implementing interfaces.");
							break;
						}
				}
			}
		}

		public void Warning(int line, string s) {
			Logger.WriteWarning(fileName, line, s);
		}
		public void Warning(int line, LogStr s) {
			Logger.WriteWarning(fileName, line, s);
		}

		//SubSectionLine is only called if we're really still in the subsection.
		public void SubSectionLine(string origS, string s, int line) {
			if (s.ToLower()=="none" || s.ToLower()=="nothing") {
				return;
			}
			try {
				switch (ssType) {
					case SSType.DefVars: {
							defVars.Add(new ClassTemplateInstanceField(s, true));
							break;
						}
					case SSType.Vars: {
							vars.Add(new ClassTemplateInstanceField(s, false));
							break;
							//} case SSType.CS: {
							//    csCode.Append("\t"+origS+"\n");
							//    break;
							//} case SSType.JS: {
							//    jsCode.Append("\t"+origS+"\n");
							//    break;
							//} case SSType.VB: {
							//    vbCode.Append("\t"+origS+"\n");
							//    break;
						}
					default: {
							Warning(line, "Unknown section type "+ssType);
							break;
						}
				}
			} catch (ScriptException se) {
				Warning(line, se.Message);
			}
		}

		public void ToCode(out CodeTypeDeclaration ctd, out CodeTypeDeclaration def) {
			ctd = new CodeTypeDeclaration(name);
			def = new CodeTypeDeclaration(name+"Def");
			if (baseName=="") {
				Warning(line, "This ClassTemplate doesn't specify its parent class. You can do that by putting 'base=Item' (or whatever type you want) in the def, preferably at the top of it. (Valid synonyms for 'base' are baseclass, parent, parentclass, and extends)");
				baseName="Item";
			}
			ctd.BaseTypes.Add(baseName);
			bool initNeedsNew = true;
			if ((String.Compare(baseName, "AbstractCharacter", true)==0)||
					(String.Compare(baseName, "AbstractItem", true)==0)) {
				initNeedsNew = false;
			}

			def.BaseTypes.Add(baseName+"Def");

			foreach (string imp in implements) {
				ctd.BaseTypes.Add(imp);
			}
			ctd.CustomAttributes.Add(new CodeAttributeDeclaration(
				new CodeTypeReference(typeof(DeepCopyableClassAttribute))));
			ctd.CustomAttributes.Add(new CodeAttributeDeclaration(
				new CodeTypeReference(typeof(Persistence.SaveableClassAttribute))));

			ctd.IsClass = true;
			ctd.IsPartial = true;
			def.IsClass = true;
			def.IsPartial = true;
			ctd.TypeAttributes|=TypeAttributes.Public;
			def.TypeAttributes|=TypeAttributes.Public;

			CodeMemberProperty defProperty = new CodeMemberProperty();
			defProperty.Name="Def";
			defProperty.Attributes=MemberAttributes.Final|MemberAttributes.Private; //That's the default, but we'll set it anyways in case that changes in a later version of .NET.
			defProperty.Type = new CodeTypeReference(name+"Def");
			CodeMethodReturnStatement ret = new CodeMethodReturnStatement();
			CodeCastExpression cast = new CodeCastExpression();
			cast.TargetType = new CodeTypeReference(name+"Def");
			CodePropertyReferenceExpression refex = new CodePropertyReferenceExpression();
			refex.PropertyName="def";
			refex.TargetObject=new CodeThisReferenceExpression();
			cast.Expression = refex;
			ret.Expression = cast;
			defProperty.GetStatements.Add(ret);
			ctd.Members.Add(defProperty);

			//CodeMemberMethod dupeImpl = new CodeMemberMethod();
			//dupeImpl.Name="DupeImpl";
			//dupeImpl.Attributes=MemberAttributes.Family|MemberAttributes.Override;
			//dupeImpl.ReturnType=new CodeTypeReference(typeof(Thing));
			////I got tired of using CodeDOM.
			////dupeImpl.Statements.Add(new CodeSnippetStatement("Sanity.IfTrueThrow(GetType()!=typeof("+name+"), \"DupeImpl() needs to be overriden by subclasses - \"+GetType()+\" did not override it.\");"));
			//dupeImpl.Statements.Add(
			//    new CodeMethodInvokeExpression(
			//        new CodeMethodReferenceExpression(
			//            new CodeTypeReferenceExpression(typeof(Sanity)),
			//            "IfTrueThrow"),
			//        new CodeBinaryOperatorExpression(
			//            new CodeMethodInvokeExpression(
			//                new CodeMethodReferenceExpression(
			//                    new CodeThisReferenceExpression(),
			//                    "GetType")),
			//            CodeBinaryOperatorType.IdentityInequality,
			//            new CodeTypeOfExpression(name)),
			//        new CodePrimitiveExpression("DupeImpl() needs to be overriden by subclasses")));

			////dupeImpl.Statements.Add(new CodeSnippetStatement("return new "+name+"(this);"));
			//dupeImpl.Statements.Add(
			//    new CodeMethodReturnStatement(
			//        new CodeObjectCreateExpression(
			//            name,
			//            new CodeThisReferenceExpression())));
			//ctd.Members.Add(dupeImpl);

			//That writes this:
			//Sanity.IfTrueThrow(GetType()!=typeof(Item), "DupeImpl() needs to be overridden by subclasses - "+GetType()+" did not override it.");
			//return new Item(this);

			//public FoobarDef(string defname, string filename, int headerLine, string typeName) : base(defname, filename, headerLine, typeName)
			CodeConstructor defConstructor = new CodeConstructor();
			defConstructor.Attributes=MemberAttributes.Public|MemberAttributes.Final;
			defConstructor.Parameters.Add(new CodeParameterDeclarationExpression("String", "defname"));
			defConstructor.Parameters.Add(new CodeParameterDeclarationExpression("String", "filename"));
			defConstructor.Parameters.Add(new CodeParameterDeclarationExpression("Int32", "headerLine"));

			defConstructor.BaseConstructorArgs.Add(new CodeArgumentReferenceExpression("defname"));
			defConstructor.BaseConstructorArgs.Add(new CodeArgumentReferenceExpression("filename"));
			defConstructor.BaseConstructorArgs.Add(new CodeArgumentReferenceExpression("headerLine"));
			def.Members.Add(defConstructor);

			CodeConstructor ctdConstructor = new CodeConstructor();
			ctdConstructor.Attributes=MemberAttributes.Public|MemberAttributes.Final;
			ctdConstructor.Parameters.Add(new CodeParameterDeclarationExpression("ThingDef", "myDef"));
			ctdConstructor.BaseConstructorArgs.Add(new CodeArgumentReferenceExpression("myDef"));
			ctd.Members.Add(ctdConstructor);

			CodeConstructor ctdCopyConstructor = new CodeConstructor();
			ctdCopyConstructor.CustomAttributes.Add(new CodeAttributeDeclaration(
				new CodeTypeReference(typeof(DeepCopyImplementationAttribute))));
			ctdCopyConstructor.Attributes=MemberAttributes.Public|MemberAttributes.Final;
			ctdCopyConstructor.Parameters.Add(new CodeParameterDeclarationExpression(name, "copyFrom"));
			ctdCopyConstructor.BaseConstructorArgs.Add(new CodeArgumentReferenceExpression("copyFrom"));
			ctd.Members.Add(ctdCopyConstructor);

			CodeMemberMethod save = new CodeMemberMethod();
			save.Name="Save";
			save.Attributes=MemberAttributes.Public|MemberAttributes.Override;
			save.Parameters.Add(new CodeParameterDeclarationExpression("SaveStream", "output"));
			save.ReturnType=new CodeTypeReference(typeof(void));

			//string saveStatement = "";
			CodeMemberMethod load = new CodeMemberMethod();
			load.Name="LoadLine";
			load.Attributes=MemberAttributes.Family|MemberAttributes.Override;
			load.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "filename"));
			load.Parameters.Add(new CodeParameterDeclarationExpression(typeof(int), "line"));
			load.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "prop"));
			load.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "value"));
			load.ReturnType=new CodeTypeReference(typeof(void));
			load.Statements.Add(new CodeSnippetStatement("\t\t\tswitch (prop) {\n"));

			foreach (ClassTemplateInstanceField ct in defVars) {
				def.Members.Add(ct.ToField());
				def.Members.Add(ct.ToProperty());
				defConstructor.Statements.Add(ct.ToAssign());
			}
			foreach (ClassTemplateInstanceField ct in vars) {
				ctd.Members.Add(ct.ToField());
				//ctd.Members.Add(ct.ToProperty());
				ctdCopyConstructor.Statements.Add(ct.ToCopyExpression());
				if (ct.needsCopying) {
					ctd.Members.Add(ct.ToDelayedCopyMethod());
				}
				save.Statements.Add(ct.ToSaveExpression());

				load.Statements.Add(new CodeSnippetStatement("\t\t\t\tcase \""+ct.fieldName.ToLower()+"\":"));
				if ((ct.type != null) && 
							SteamEngine.Persistence.ObjectSaver.IsSimpleSaveableType(ct.type)) {
					load.Statements.Add(ct.ToDirectLoadStatement());
				} else {
					load.Statements.Add(new CodeMethodInvokeExpression(//ObjectSaver.Load(value, new LoadObject(LoadSomething_Delayed), filename, line);
						new CodeMethodReferenceExpression(
							new CodeTypeReferenceExpression(typeof(SteamEngine.Persistence.ObjectSaver)), "Load"),
							new CodeArgumentReferenceExpression("value"),
							ct.ToDelayedLoadMethodDeleg(),
							new CodeArgumentReferenceExpression("filename"),
							new CodeArgumentReferenceExpression("line")));
					ctd.Members.Add(ct.ToDelayedLoadMethod());
				}
				load.Statements.Add(new CodeSnippetStatement("\t\t\t\t\tbreak;\n"));
			}

			load.Statements.Add(new CodeSnippetStatement("\t\t\t\tdefault:\n"));
			load.Statements.Add(new CodeMethodInvokeExpression(
					new CodeMethodReferenceExpression(
						new CodeBaseReferenceExpression(),
						"LoadLine"),
					new CodeArgumentReferenceExpression("filename"),
					new CodeArgumentReferenceExpression("line"),
					new CodeArgumentReferenceExpression("prop"),
					new CodeArgumentReferenceExpression("value")));

			load.Statements.Add(new CodeSnippetStatement("\t\t\t\t\tbreak;\n\t\t\t}"));

			save.Statements.Add(new CodeMethodInvokeExpression( //"base.Save(output);\n";
				new CodeMethodReferenceExpression(
					new CodeBaseReferenceExpression(),
					"Save"),
				new CodeArgumentReferenceExpression("output")));
			ctd.Members.Add(save);
			ctd.Members.Add(load);


			CodeMemberMethod createPoint = new CodeMemberMethod();
			createPoint.Name="CreateImpl";
			createPoint.Attributes=MemberAttributes.Family|MemberAttributes.Override; ;
			createPoint.ReturnType=new CodeTypeReference(typeof(Thing));
			createPoint.Statements.Add(//return new "+name+"(x, y, z, m)
				new CodeMethodReturnStatement(
					new CodeObjectCreateExpression(
						name,
						new CodeThisReferenceExpression()
					)
				)
			);
			def.Members.Add(createPoint);

			CodeMemberMethod init = new CodeMemberMethod();
			init.Name="Bootstrap";
			init.Attributes=MemberAttributes.Public|MemberAttributes.Static;
			if (initNeedsNew) {
				init.Attributes |= MemberAttributes.New;
			}
			//init.Statements.Add(new CodeSnippetStatement("ThingDef.RegisterThingDef(typeof("+name+"Def), \""+name+"\");"));
			init.Statements.Add(
				new CodeMethodInvokeExpression(
					new CodeMethodReferenceExpression(
						new CodeTypeReferenceExpression(typeof(ThingDef)),
						"RegisterThingDef"),
					new CodeTypeOfExpression(name+"Def"),
					new CodeTypeOfExpression(name)));
			def.Members.Add(init);

		}

		public void RememberCCU(ref CodeCompileUnit codeCompileUnit) {
			//If we open a new one, we have to set the ref param to it too.
			if (codeCompileUnit == null) {
				codeCompileUnit = new CodeCompileUnit();
				CodeNamespace ns = NameSpace();
				codeCompileUnit.Namespaces.Add(ns);
			}
			this.codeCompileUnit=codeCompileUnit;
		}

		public CodeNamespace NameSpace() {
			CodeNamespace codeNamespace = new CodeNamespace("SteamEngine.CompiledScripts");
			codeNamespace.Imports.Add(new CodeNamespaceImport("SteamEngine"));
			codeNamespace.Imports.Add(new CodeNamespaceImport("System"));
			codeNamespace.Imports.Add(new CodeNamespaceImport("System.Reflection"));
			codeNamespace.Imports.Add(new CodeNamespaceImport("System.Collections"));
			codeNamespace.Imports.Add(new CodeNamespaceImport("System.Collections.Generic"));
			codeNamespace.Imports.Add(new CodeNamespaceImport("System.IO"));
			codeNamespace.Imports.Add(new CodeNamespaceImport("System.Globalization"));
			codeNamespace.Imports.Add(new CodeNamespaceImport("SteamEngine.Common"));
			codeNamespace.Imports.Add(new CodeNamespaceImport("System.Configuration"));
			codeNamespace.Imports.Add(new CodeNamespaceImport("System.Text"));
			codeNamespace.Imports.Add(new CodeNamespaceImport("SteamEngine.Timers"));
			codeNamespace.Imports.Add(new CodeNamespaceImport("SteamEngine.Packets"));
			codeNamespace.Imports.Add(new CodeNamespaceImport("SteamEngine.Persistence"));
			return codeNamespace;
		}

		public void Dump() {
			//mf.WriteLine("//Automatically generated by SteamEngine's ClassTemplateReader");
			string cs = csCode.ToString();
			//string js = jsCode.ToString();
			//string vb = vbCode.ToString();
			CodeTypeDeclaration ctd, def;
			ToCode(out ctd, out def);

			codeCompileUnit.Namespaces[0].Types.Add(def);
			codeCompileUnit.Namespaces[0].Types.Add(ctd);
		}
	}
}