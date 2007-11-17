using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using SteamEngine.Network;
using SteamEngine.Common;

namespace SteamEngine.AuxiliaryServer {
	public static class MainClass {
		//public static ConsoleServer server = new ConsoleServer();

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
		}


		private static void Cycle() {

			while (true) {
				Thread.Sleep(1);
				//server.Cycle();
			}
		}

		private static void Dispose() {

		}

	}
}
