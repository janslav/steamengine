using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

using SteamEngine.Communication;
using SteamEngine.Common;

namespace SteamEngine.AuxiliaryServer {
	public static class MainClass {
		//public static ConsoleServer server = new ConsoleServer();

		private static readonly object globalLock = new object();

		private static readonly ManualResetEvent setToExit = new ManualResetEvent(false);

		public static object GlobalLock {
			get { return MainClass.globalLock; }
		}

		public static ManualResetEvent SetToExit {
			get { return MainClass.setToExit; }
		} 

		static void Main() {
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
			SEGameServers.SEGameServerServer.Init();
			SphereServers.SphereServerClientFactory.Init();
		}

		private static void Exit() {
			LoginServer.LoginServer.Exit();
			ConsoleServer.ConsoleServer.Exit();
			SEGameServers.SEGameServerServer.Exit();
		}
	}
}
