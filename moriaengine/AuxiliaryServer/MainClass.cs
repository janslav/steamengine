using System;
using System.Threading;

using SteamEngine.Common;

namespace SteamEngine.AuxiliaryServer {
	public static class MainClass {
		//public static ConsoleServer server = new ConsoleServer();

		private static readonly object globalLock = new object();

		private static readonly CancellationTokenSource exitTokenSource = new CancellationTokenSource();

		public static object GlobalLock {
			get { return globalLock; }
		}

		public static CancellationToken ExitSignalToken {
			get { return exitTokenSource.Token; }
		}

		static void Main() {
			//name the console window for better recognizability
			Console.Title = "SE Auxiliary Server - " + System.Reflection.Assembly.GetExecutingAssembly().Location;

			Tools.ExitBinDirectory();

			try {
				Init();

				Thread t = new Thread(delegate() {
					Console.ReadLine();
					exitTokenSource.Cancel();
				});
				t.IsBackground = true;
				t.Start();

				exitTokenSource.Token.WaitHandle.WaitOne();

			} catch (Exception e) {
				Common.Logger.WriteFatal(e);
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

		internal static void CommandExit() {
			exitTokenSource.Cancel();
		}
	}
}
