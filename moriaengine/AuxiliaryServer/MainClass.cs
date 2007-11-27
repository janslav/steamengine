using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

using SteamEngine.Communication;
using SteamEngine.Common;

namespace SteamEngine.AuxiliaryServer {
	public static class MainClass {
		//public static ConsoleServer server = new ConsoleServer();

		public static readonly object globalLock = new object();

		public static LoginServer.LoginServer loginServer;
		public static GameServers.GameServerServer gameServerServer;


		static void Main(string[] args) {
			Tools.ExitBinDirectory();

			try {
				Init();

				Console.ReadLine(); //exit by pressing Enter :D

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
			gameServerServer = new GameServers.GameServerServer();
		}

		private static void Dispose() {
		}

	}
}
