using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using SteamEngine.Common;
using SteamEngine.Communication.TCP;


namespace SteamEngine.AuxiliaryServer {
	public static class Commands {
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods"),
		System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "conn"),
		System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1807:AvoidUnnecessaryStringCreation", MessageId = "cmd")]
		public static void HandleCommand(TcpConnection<ConsoleServer.ConsoleClient> conn, ConsoleServer.ConsoleClient state, string cmd) {
			cmd = cmd.ToLowerInvariant();
			switch (cmd) {
				case "restart":
					CmdRestart();
					return;
				case "svnupdate":
					VersionControl.SvnUpdateProject(".");
					foreach (IGameServerSetup game in Settings.KnownGameServersList) {
						game.SvnUpdate(state);
					}
					return;
				case "svncleanup":
					VersionControl.SvnCleanUpProject(".");
					foreach (IGameServerSetup game in Settings.KnownGameServersList) {
						game.SvnCleanup(state);
					}
					return;
				case "help":
					DisplayHelp(state);
					return;
				case "processes":
					DisplayProcesses(state);
					return;
			}

			state.WriteLine(GameUid.AuxServer, "Unknown command '" + cmd + "'.");
		}

		private static void DisplayHelp(ConsoleServer.ConsoleClient state) {
			state.WriteLine(GameUid.AuxServer, "Available commands:"
				+ "restart" + Environment.NewLine
				+ "svnupdate" + Environment.NewLine
				+ "svncleanup" + Environment.NewLine
				+ "processes" + Environment.NewLine
				+ "help");
		}

		private static void DisplayProcesses(SteamEngine.AuxiliaryServer.ConsoleServer.ConsoleClient state) {
			StringBuilder message = new StringBuilder("Relevant running processes on ").AppendLine(Environment.MachineName);
			foreach (Process prc in Process.GetProcesses()) {
				try {
					string file = prc.MainModule.FileName;

					if (file.Contains(SphereServerSetup.sphereExeName) ||
							file.ToLowerInvariant().Contains("steamengine")) {

						message.Append(file)
							.Append(" - running since ").Append(prc.StartTime.ToString(System.Globalization.CultureInfo.InvariantCulture))
							.Append(", PID ").AppendLine(prc.Id.ToString(System.Globalization.CultureInfo.InvariantCulture));
					}
				} catch { }
			}

			state.WriteLine(GameUid.AuxServer, message.ToString());
		}

		public static void CmdRestart() {
			new MsBuildProjectReStarter();
		}

		private class MsBuildProjectReStarter : AuxServMsBuildProjectStarter {
			internal MsBuildProjectReStarter()
				: base(SEBuild.Sane, ".", "buildRestarter", "restarterFileName") {
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

				MainClass.CommandExit();
			}
		}
	}

	//Nant logger class and a helper threading class combined
}
