using System;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Threading;

namespace SteamEngine.ProcessRestarter {
	public static class MainClass {
		static ManualResetEvent setForExit = new ManualResetEvent(false);

		static void Main(string[] args) {
			//name the console window for better recognizability
			Console.Title = "SE AuxRestarter - " + System.Reflection.Assembly.GetExecutingAssembly().Location;

			//Tools.ExitBinDirectory(); //we don't want any dependency, not even with Common
			ExitBinDirectory();

			if (args.Length != 2) {
				DisplayHelp();
				return;
			}

			string waitForPath = Path.GetFullPath(args[0]);
			if (!CheckFile(waitForPath)) {
				return;
			}

			string executePath = Path.GetFullPath(args[1]);
			if (!CheckFile(executePath)) {
				return;
			}

			Process runningProcess = FindRunningProcess(waitForPath);
			if (runningProcess == null) {
				Console.WriteLine("B��c� proces '" + waitForPath + "' nenalezen.");
				RunProcess(executePath);
			} else {
				Console.WriteLine("B��c� proces '" + waitForPath + "' nalezen. �ek�m na ukon�en�...");
				runningProcess.EnableRaisingEvents = true;
				runningProcess.Exited += new EventHandler(delegate(object ignored, EventArgs ignored2) {
					RunProcess(executePath);
				});

				setForExit.WaitOne();
			}
		}

		private static void RunProcess(string path) {
			Console.WriteLine("Pou�t�m '" + path + "'.");
			Process.Start(path);
			setForExit.Set();
		}

		private static Process FindRunningProcess(string waitForPath) {
			string processName = Path.GetFileNameWithoutExtension(waitForPath);
			Process[] processes = Process.GetProcessesByName(processName);

			Process correctProcess = null;
			foreach (Process p in processes) {
				string pPath = Path.GetFullPath(p.MainModule.FileName);
				if (0 == string.Compare(waitForPath, pPath, StringComparison.OrdinalIgnoreCase)) {
					correctProcess = p;
					break;
				}
			}
			return correctProcess;
		}

		private static bool CheckFile(string waitForPath) {
			if (!File.Exists(waitForPath)) {
				Console.WriteLine("Soubor '" + waitForPath + "' neexistuje.");
				DisplayHelp();
				return false;
			}
			return true;
		}

		private static void DisplayHelp() {
			Console.WriteLine("Tento program o�ek�v� 2 parametry: prvn� je cesta k souboru procesu na kter� se �ek� a� skon��, druh� je cesta k souboru kter� se m� pak pustit.");
		}

		private static void ExitBinDirectory() {
			string path = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
			if (path.ToLower().EndsWith("bin")) {
				Directory.SetCurrentDirectory(Path.GetDirectoryName(path));
			} else {
				Directory.SetCurrentDirectory(path);
			}
		}
	}
}
