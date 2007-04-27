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

namespace SteamEngine.CompiledScripts {
	public static partial class ClassTemplateReader {
		public static bool ClassTemplateMessages = TagMath.ParseBoolean(ConfigurationManager.AppSettings["ClassTemplate Trace Messages"]);

		static int curLineNumber;
		static ScriptFileCollection allFiles;
		static CodeGeneratorOptions options;
		static int numFilesRead;

		static ClassTemplateReader() {
			options = new CodeGeneratorOptions();
			options.IndentString="\t";
		}

		public static void Init() {
			allFiles = new ScriptFileCollection(Globals.scriptsPath, ".ct");

			Console.WriteLine("Processing ClassTemplates");
			numFilesRead=0;
			foreach (ScriptFile file in allFiles.GetAllFiles()) {
				ProcessFile(file);
				numFilesRead++;
			}
			Console.WriteLine("Processed "+numFilesRead+" ClassTemplates scripts.");
		}

		public static void Resync() {
			foreach (ScriptFile file in allFiles.GetChangedFiles()) {
				ProcessFile(file);
			}
		}

		private static void ProcessFile(ScriptFile scriptFile) {
			string fileName = Path.GetFileNameWithoutExtension(scriptFile.FullName);
			string dirName = Path.GetDirectoryName(scriptFile.FullName);

			CodeCompileUnit codeCompileUnit=null;

			ClassTemplateInstance curDef=null;
			if (fileName.ToLower().StartsWith("sphere_d_")) {
				fileName=fileName.Substring(9);
			}
			if (fileName.ToLower().StartsWith("sphere")) {
				fileName=fileName.Substring(6);
			}
			//trim every whitespace and '_'
			fileName = fileName.Trim('_', '\t', '\n', '\v', '\f', '\r', ' ', '\x00a0', '\u2000', '\u2001', '\u2002', '\u2003', '\u2004', '\u2005', '\u2006', '\u2007', '\u2008', '\u2009', '\u200a', '\u200b', '\u3000', '\ufeff');
			fileName = fileName+".Generated.cs";
			string outFileName = Path.Combine(dirName, fileName);

			StreamReader input = scriptFile.OpenText();
			int line = 0;
			string s="";
			int ssTabs = 0;
			bool inTrig=false;
			try {
				while (true) {
					string origS = input.ReadLine();
					line++;
					curLineNumber=line;
					if (origS!=null) {
						//s = s.Trim();
						int commentPos=origS.IndexOf("//");
						if (commentPos>-1) {
							s=origS.Substring(0, commentPos);
							s=s.Trim();
						} else {
							s=origS.Trim();
						}
						if (s.Length>0) {
							int lB = s.IndexOf("[");
							int rB = s.IndexOf("]");
							int trig = s.ToLower().LastIndexOf(":");
							int tabs = 0;
							for (int idx = 0; idx<origS.Length; idx++) {
								if (origS[idx]=='\t') {
									tabs++;
								} else {
									break;
								}
							}
							if (tabs==0 && lB==0 && rB>lB) {
								if (curDef!=null) {
									curDef.RememberCCU(ref codeCompileUnit);
									curDef.Dump();
								}
								curDef=null;
								inTrig=false;
								//Console.WriteLine("lB="+lB+" rB="+rB);
								string sectionName=s.Substring(lB+1, rB-lB-1);
								int space=sectionName.IndexOf(" ");
								string param=null;
								if (space>-1) {
									param=sectionName.Substring(space+1).Trim();
									sectionName=sectionName.Substring(0, space);
								} else {
									throw new ScriptException("["+sectionName+"] is not a valid classtemplate def.");
								}
								//string ss=sectionName+(param!=null?"*":"");
								curDef=new ClassTemplateInstance(scriptFile.Name, line, param);
							} else if (trig==s.Length-1) {
								if (curDef!=null) {
									curDef.SubSection(s.Substring(0, s.Length-1), line);
									ssTabs=tabs;
									inTrig=true;
								} else {
									throw new ScriptException("Encountered subsection '"+s+"' before any [] blocks!");
								}
							} else if (!inTrig) {
								if (curDef!=null) {
									curDef.Line(origS, s, line);
								}
							} else if (inTrig) {
								if (curDef!=null) {
									if (tabs<=ssTabs) {
										inTrig=false;
										curDef.Line(origS, s, line);
									} else {
										curDef.SubSectionLine(origS, s, line);
									}
								}
							}
						}
					} else {
						break;
					}
				}
			} catch (IOException) {
				throw new Exception("Convert CT scripts interrupted.");
			}
			if (curDef!=null) {
				curDef.RememberCCU(ref codeCompileUnit);
				curDef.Dump();
			}
			input.Close();

			CodeDomProvider provider = null;

			StreamWriter outFile = new StreamWriter(outFileName);
			provider = new Microsoft.CSharp.CSharpCodeProvider();
			//generator = provider.CreateGenerator();
			provider.GenerateCodeFromCompileUnit(codeCompileUnit, outFile, options);
			outFile.Close();
		}
	}
}
