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

		static void Main(string[] args) {
			Tools.ExitBinDirectory();

			try {
				Init();

				Console.ReadLine(); //exit by pressing Enter :D

			} catch (Exception e) {
				Logger.WriteFatal(e);
			} finally {
				Exit();
			}
		}

		private static void Init() {
			Logger.Init();
			Settings.Init();

			LoginServer.LoginServer.Init();
			ConsoleServer.ConsoleServer.Init();
			GameServers.GameServerServer.Init();
		}

		private static void Exit() {
			LoginServer.LoginServer.Exit();
			ConsoleServer.ConsoleServer.Exit();
			GameServers.GameServerServer.Exit();
		}
	}
}
