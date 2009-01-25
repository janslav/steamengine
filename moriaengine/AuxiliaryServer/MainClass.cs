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

		public static readonly ManualResetEvent setToExit = new ManualResetEvent(false);

		static void Main(string[] args) {
			//name the console window for better recognizability
			Console.Title = "SE Auxiliary Server - " + System.Reflection.Assembly.GetExecutingAssembly().Location;

			Tools.ExitBinDirectory();

			try {
				Init();

				Thread t = new Thread(delegate() {
					Console.ReadLine();
					setToExit.Set();
				});
				t.IsBackground = true;
				t.Start();

				setToExit.WaitOne();

			} catch (Exception e) {
				Logger.WriteFatal(e);
			} finally {
				Exit();
			}
		}

		private static void Init() {
			Logger.Init();
			Settings.Init();
			LoginServer.ServerUtils.Init();

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
