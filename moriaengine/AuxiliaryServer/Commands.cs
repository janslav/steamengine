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
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods"), 
		System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "conn"), 
		System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1807:AvoidUnnecessaryStringCreation", MessageId = "cmd")]
		public static void HandleCommand(TcpConnection<ConsoleServer.ConsoleClient> conn, ConsoleServer.ConsoleClient state, string cmd) {
			cmd = cmd.ToLower(System.Globalization.CultureInfo.InvariantCulture);
			switch (cmd) {
				case "restart":
					CmdRestart();
					return;
				case "svnupdate":
					VersionControl.SvnUpdateProject();
					return;
				case "svncleanup":
					VersionControl.SvnCleanUpProject();
					return;
				case "help":
					DisplayHelp(state);
					return;
			}

			state.WriteLine(0, "Unknown command '" + cmd + "'.");
		}

		private static void DisplayHelp(ConsoleServer.ConsoleClient state) {
			state.WriteLine(0, "Available commands:"
				+ "restart" + Environment.NewLine
				+ "svnupdate" + Environment.NewLine
				+ "svncleanup" + Environment.NewLine
				+ "help");
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1806:DoNotIgnoreMethodResults", MessageId = "SteamEngine.AuxiliaryServer.Commands+NantProjectReStarter")]
		public static void CmdRestart() {
			new NantProjectReStarter();
		}

		private class NantProjectReStarter : AuxServNantProjectStarter {
			internal NantProjectReStarter ()
				: base (SEBuild.Sane, ".", "buildRestarter", "restarterFileName") {
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

				MainClass.SetToExit.Set();
			}
		}
	}

	//Nant logger class and a helper threading class combined
	internal class AuxServNantProjectStarter : DefaultLogger {
		private SEBuild build;
		private string seRootPath;
		private string targetTask;
		private string filenameProperty;

		internal AuxServNantProjectStarter(SEBuild build, string seRootPath, string targetTask, string filenameProperty) {
			this.build = build;
			this.seRootPath = seRootPath;
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
			try {
				NantLauncher nant = new NantLauncher(Path.Combine(this.seRootPath, NantLauncher.defaultPathInProject));
				nant.SetLogger(this);
				nant.SetPropertiesAndSymbols(this.build);
				//nant.SetDebugMode(this.build == SEBuild.Debug);
				//nant.SetOptimizeMode(this.build == SEBuild.Optimised);

				nant.SetTarget(this.targetTask);
				nant.Execute();

				if (nant.WasSuccess()) {
					string file = nant.GetCompiledAssemblyName(this.seRootPath, filenameProperty);

					Console.WriteLine("Starting " + file);
					StartProcess(file);
				}
			} catch (Exception e) {
				Logger.WriteError(e);
			}
		}

		public virtual void StartProcess(string file) {
			System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo(file);
			psi.WindowStyle = System.Diagnostics.ProcessWindowStyle.Minimized;
			System.Diagnostics.Process.Start(psi);
		}
	}
}
