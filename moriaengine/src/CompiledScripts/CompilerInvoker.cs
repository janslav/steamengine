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
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Reflection;
using System.CodeDom;
using System.CodeDom.Compiler;
using Microsoft.CSharp; 
//#if !MONO
//using Microsoft.JScript;
//#endif
using Microsoft.VisualBasic;
using System.Text;
using System.Globalization; 
using SteamEngine.Timers;
using SteamEngine.Common;
using NAnt.Core;

namespace SteamEngine.CompiledScripts { 
	
	internal class CompilerInvoker {
		internal static CompScriptFileCollection compiledScripts;//CompScriptFileCollection instances
		//all types of Steamengine namespace, regardless if from scripts or core. 

		//removes all non-core references
		internal static void UnLoadScripts() {
			compiledScripts = null;
		}
	
		internal static bool SourcesHaveChanged { get {
			ScriptFile[] changedFiles = compiledScripts.GetChangedFiles();
			if (changedFiles.Length > 0) {
				return true;
			}
			return false;
		} }

		internal static uint compilenumber = 0;

		internal static bool CompileScripts(bool firstCompiling) {
			bool success = true;

			SteamEngine.Persistence.DecoratedClassesSaveImplementorGenerator.Init();

			if (firstCompiling) {
				success = ClassManager.InitClasses(ClassManager.CoreAssembly);
			}
			//then try to compile scripts
			compilenumber++;
			
			//these calls are now deprecated, we use building with NAnt now
			//success=success && CompileScripts(new CSharpCodeProvider(), "C#", ".cs", firstCompiling);
			//success=success && CompileScripts(new VBCodeProvider(), "VB.NET", ".vb", firstCompiling);
			////#if !MONO	//the jscript does some weird errors, and I dont care enough to try to satisfy it... 
			////			//if anyone wants JS support, try to uncomment this and see what happens
			////			success=success && CompileScripts(new JScriptCodeProvider(), "JScript", ".js", firstCompiling);
			////#endif

			success = success && CompileScriptsUsingNAnt();

			success = success && ClassManager.InitClasses(compiledScripts.assembly);

			success = success && SteamEngine.Persistence.DecoratedClassesSaveImplementorGenerator.DumpAndCompileClasses();
			return success;
		}


		private static bool CompileScriptsUsingNAnt() {
			CompScriptFileCollection fileCollection = new CompScriptFileCollection(Globals.scriptsPath, ".cs");

			NantLauncher nant = new NantLauncher();
			nant.SetLogger(new CoreNantLogger());
			nant.SetPropertiesAsSelf();
#if SANE
			nant.SetProperty("cmdLineParams", "/debug+"); //in sane builds, scripts should still have debug info
#endif
			nant.SetTarget("buildScripts");

			nant.SetProperty("scriptsNumber", compilenumber.ToString());
			nant.SetProperty("scriptsReferencesListPath", 
				Path.Combine(Globals.scriptsPath, "referencedAssemblies.txt"));
			nant.SetSourceFileNames(fileCollection.GetAllFileNames());

			Logger.StopListeningConsole();//stupid defaultlogger writes to Console.Out
			nant.Execute();
			Logger.ResumeListeningConsole();

			if (nant.WasSuccess()) {
				Console.WriteLine("Done compiling C# scripts.");
				fileCollection.assembly = nant.GetCompiledAssembly("scriptsFileName");
				Logger.scriptsAssembly = fileCollection.assembly;
				compiledScripts = fileCollection;
				return true;
			} else {
				return false;
			}
		}

		private class CoreNantLogger : DefaultLogger {
			public override void BuildFinished(object sender, BuildEventArgs e) { }
			public override void BuildStarted(object sender, BuildEventArgs e) { }
			public override void TargetFinished(object sender, BuildEventArgs e) { }
			public override void TargetStarted(object sender, BuildEventArgs e) { }
			public override void TaskFinished(object sender, BuildEventArgs e) { }
			public override void TaskStarted(object sender, BuildEventArgs e) { }

			protected override void Log(string pMessage) {
				object o = NantLauncher.GetDecoratedLogMessage(pMessage);
				if (o != null) {
					Logger.StaticWriteLine(o);
				}
				//Console.WriteLine(pMessage);
			}
		}

		internal static void InitScripts() {
			if (compiledScripts != null) {
				Logger.WriteDebug("Initializing Scripts.");
				Assembly scripts = compiledScripts.assembly;
				if (scripts != null) {
					Type[] types = scripts.GetTypes();
					for (int i=0; i<types.Length; i++) {
						MethodInfo m = types[i].GetMethod("Init", BindingFlags.Static|BindingFlags.Public|BindingFlags.DeclaredOnly);
						if (m!=null) {
							m.Invoke(null, null);
						}
					}
				}
				Logger.WriteDebug("Initializing Scripts done.");
			}
		}
	}
}