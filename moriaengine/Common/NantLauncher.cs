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
using System.Reflection;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Text.RegularExpressions;
using NAnt.Core;


namespace SteamEngine.Common {

	
	[Summary("Use this class to run tasks from the ./distrib/nant/default.build and/or other NAnt build files.")]
	public class NantLauncher {
		
		Project nantProject;
		string target = "build";
		Exception exception;
		IBuildLogger logger;
		string[] sourceFileNames;
		string sourceFileNamesFile;
	
		public NantLauncher() : 
				this("./distrib/nant/default.build") {
			
		}
		
		public NantLauncher(string buildFilename) {
			nantProject = new NAnt.Core.Project(buildFilename, Level.Info, 0);
		}
	
		public void SetOutputFilePrefix(string prefix) {
			nantProject.Properties["outputFilePrefix"] = prefix;
		}
		
		public void SetDebugMode(bool debug) {
			nantProject.Properties["debug"] = debug.ToString();
		}
		
		public void SetOptimizeMode(bool optimize) {
			nantProject.Properties["optimize"] = optimize.ToString();
		}
		
		public void SetDefineSymbols(string symbols) {
			nantProject.Properties["defineSymbols"] = symbols;
		}
		
		public void SetLogger(IBuildLogger logger) {
			this.logger = logger;
		}

		public void SetSourceFileNames(string[] sourceFileNames, string sourceFileNamesFile) {
			this.sourceFileNames = sourceFileNames;
			this.sourceFileNamesFile = sourceFileNamesFile;
		}

		public void SetProperty(string name, string value) {
			nantProject.Properties[name] = value;
		}
		
		public void SetPropertiesAsSelf() {
#if DEBUG
			SetDebugMode(true);
			SetOptimizeMode(false);
			string symbols = "DEBUG";
#elif SANE
			SetDebugMode(false);
			SetOptimizeMode(false);
			string symbols = "SANE";

#elif OPTIMIZED
			SetDebugMode(false);
			SetOptimizeMode(true);
			string symbols = "OPTIMIZED";
#endif

//not all of these are used. Maybe. Anyway, thay can't harm.
#if TRACE
			symbols = symbols+",TRACE";
#endif
#if MSWIN
			symbols = symbols+",MSWIN";
#endif
#if TESTRUNUO
			symbols = symbols+",TESTRUNUO";
#endif
#if MSVS
			symbols = symbols+",MSVS";
#endif
#if USEFASTDLL
			symbols = symbols+",USEFASTDLL";
#endif
#if MONO
			symbols = symbols+",MONO";
#endif


			SetDefineSymbols(symbols);
		}
		
		public void SetTarget(string target) {
			this.target = target;
		}
		
		private void OnBuildFinished(object sender, BuildEventArgs e) {
			exception = e.Exception;
		}

		
		public void Execute() {
			FileInfo fileListFile = null;
			if (sourceFileNames != null) {
				fileListFile = new FileInfo(sourceFileNamesFile);
				TextWriter writer = new StreamWriter(fileListFile.OpenWrite(), System.Text.Encoding.UTF8);
				foreach (string sourcefile in sourceFileNames) {
					writer.WriteLine(sourcefile);
				}
				nantProject.Properties["sourcesListPath"] = fileListFile.FullName;
				writer.Flush();
				writer.Close();
			}

			BuildListenerCollection lListeners = new BuildListenerCollection();
			IBuildLogger lBuildLogger = this.logger;
			lBuildLogger.Threshold = Level.Info;
			lListeners.Add(lBuildLogger);
			nantProject.AttachBuildListeners(lListeners);

			nantProject.BuildFinished += new BuildEventHandler(OnBuildFinished);
			
			nantProject.BuildTargets.Add(target);

			nantProject.Run();
		}
		
		public bool WasSuccess() {
			return exception == null;
		}

		
		public Assembly GetCompiledAssembly(string filenameproperty) {
			string filename = System.IO.Path.GetFullPath(nantProject.Properties[filenameproperty]);
			
		
			return Assembly.LoadFile(filename);
		}
		
		public Exception Exception {
			get {
				return exception;
			}
		}
		
		
		private static Regex compileErrorRE = new Regex(@"^\[csc\] (?<filename>.+)\((?<linenumber>\d+),(?<colnumber>\d+)\):(?<errtext>.*)$",                   
		//private static Regex compileErrorRE = new Regex(@"^\[csc\](?<filename>.+)$",                   
			RegexOptions.IgnoreCase|RegexOptions.CultureInvariant|RegexOptions.Compiled);
			
		public static object GetDecoratedLogMessage(string msg) {
			msg = msg.Trim();
			
			if (msg.Length == 0) {
				return null;
			}

			if (msg.StartsWith("Buildfile:") || msg.StartsWith("Target framework:") || msg.StartsWith("Target(s) specified:")) {
				return null;
			}
			
			Match m = compileErrorRE.Match(msg);
			if (m.Success) {
				LogStr logstr;

				if (msg.IndexOf("warning", StringComparison.OrdinalIgnoreCase) > -1) {
					logstr = LogStr.Warning("WARNING: ");
				} else {
					logstr = LogStr.Error("ERROR: ");
				}

				logstr += LogStr.FileLine(m.Groups["filename"].Value, int.Parse(m.Groups["linenumber"].Value))
					+ ": " + m.Groups["errtext"].Value;

				return logstr;
			}

			if (msg.StartsWith("[csc] ")) {
				return msg.Substring(6);
			}

			return null;//msg - should we want anything else?
		}
		
//		public void Execute() {
//		
//		Assembly.LoadFrom(exePath);
//		}
	}
}