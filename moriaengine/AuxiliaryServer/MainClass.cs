using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

using SteamEngine.Communication;
using SteamEngine.Common;

namespace SteamEngine.AuxiliaryServer {
	public static class MainClass {
		//public static ConsoleServer server = new ConsoleServer();

		public static LoginServer.LoginServer loginServer;

		static void Main(string[] args) {
			Tools.ExitBinDirectory();

			try {
				Init();

				Cycle();
			} catch (Exception e) {
				Logger.WriteFatal(e);
			} finally {
				Dispose();
			}

		}

		private static void Init() {
			Logger.Init();
			Settings.Init();

			loginServer = new LoginServer.LoginServer();
		}



		private static void Cycle() {

			while (true) {
				Thread.Sleep(1);
				loginServer.Cycle();
			}
		}

		private static void Dispose() {
			Console.ReadLine();
		}

	}
}
