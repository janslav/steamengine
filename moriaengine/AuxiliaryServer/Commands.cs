using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Diagnostics;
using SteamEngine.Communication;
using SteamEngine.Communication.TCP;
using SteamEngine.Common;
using NAnt.Core;


namespace SteamEngine.AuxiliaryServer {
	public static class Commands {
		public static void HandleCommand(TCPConnection<ConsoleServer.ConsoleClient> conn, ConsoleServer.ConsoleClient state, string cmd) {
			cmd = cmd.ToLower();
			switch (cmd) {
				case "restart":
					CmdRestart();
					return;
				case "svnupdate":
					VersionControl.SVNUpdateProject();
					return;
			}

			state.WriteLine(0, "Unknown command '" + cmd + "'.");
		}

		public static void CmdRestart() {
			new NantProjectReStarter();
		}

		private class NantProjectReStarter : AuxServNantProjectStarter {
			internal NantProjectReStarter()
				: base(0, SEBuild.Sane, NantLauncher.defaultPathInProject, "buildRestarter", "restarterFileName") {
			}

			public override void StartProcess(string file) {
				System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo(file);
#if DEBUG
				psi.Arguments = string.Concat("\"", Process.GetCurrentProcess().MainModule.FileName, "\" \"", "Debug_AuxiliaryServer.bat", "\"");
#elif SANE
				psi.Arguments = string.Concat("\"", Process.GetCurrentProcess().MainModule.FileName, "\" \"", "Sane_AuxiliaryServer.bat", "\"");
#else
#error Optimized_AuxiliaryServer.bat not defined (?)
#endif
				System.Diagnostics.Process.Start(psi);

				MainClass.setToExit.Set();
			}
		}
	}

	//Nant logger class and a helper threading class combined
	internal class AuxServNantProjectStarter : DefaultLogger {
		private byte serverNum;
		private SEBuild build;
		private string nantPath;
		private string targetTask;
		private string filenameProperty;

		internal AuxServNantProjectStarter(byte serverNum, SEBuild build, string nantPath, string targetTask, string filenameProperty) {
			this.serverNum = serverNum;
			this.build = build;
			this.nantPath = nantPath;
			this.targetTask = targetTask;
			this.filenameProperty = filenameProperty;

			Thread t = new Thread(this.CompileAndStart);
			t.Start();
		}

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

		public virtual void CompileAndStart() {
			NantLauncher nant = new NantLauncher(this.nantPath);
			nant.SetLogger(this);
			nant.SetPropertiesAsSelf();
			nant.SetDebugMode(this.build == SEBuild.Debug);
			nant.SetOptimizeMode(this.build == SEBuild.Optimised);

			nant.SetTarget(this.targetTask);
			nant.Execute();

			string file = nant.GetCompiledAssemblyName(filenameProperty);

			Console.WriteLine("Starting " + file);
			StartProcess(file);
		}

		public virtual void StartProcess(string file) {
			System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo(file);
			psi.WindowStyle = System.Diagnostics.ProcessWindowStyle.Minimized;
			System.Diagnostics.Process.Start(psi);
		}
	}
}
