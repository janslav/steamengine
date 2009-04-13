using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

using SteamEngine.Communication;
using SteamEngine.Communication.TCP;
using SteamEngine.Common;

namespace SteamEngine.AuxiliaryServer.ConsoleServer {
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1724:TypeNamesShouldNotMatchNamespaces")]
	public class ConsoleServer : TcpServer<ConsoleClient> {
		public ConsoleServer()
			: base(ConsoleServerProtocol.instance, MainClass.GlobalLock) {
		}

		private static ConsoleServer instance = new ConsoleServer();

		private static Dictionary<int, ConsoleClient> consoles = new Dictionary<int, ConsoleClient>();


		internal static void Init() {
			instance.Bind(Settings.ConsoleServerEndpoint);
		}

		internal static void Exit() {
			instance.Dispose();
		}

		internal static void AddConnection(ConsoleClient client) {
			consoles.Add(client.Uid, client);

			if (consoles.Count == 1) {
				GameServers.GameServerServer.StartSendingLogStr();
			}
		}

		internal static void RemoveConnection(ConsoleClient client) {
			consoles.Remove(client.Uid);

			if (consoles.Count == 0) {
				GameServers.GameServerServer.StopSendingLogStr();
			}
		}

		public static ConsoleClient GetClientByUid(int uid) {
			ConsoleClient retVal;
			if (consoles.TryGetValue(uid, out retVal)) {
				return retVal;
			}
			return null;
		}

		public static void WriteLineAsAux(string msg) {
			foreach (ConsoleClient cc in consoles.Values) {
				if (cc.IsLoggedInAux) {
					cc.WriteLine(0, msg);
				}
			}
		}

		public static void WriteAsAux(string msg) {
			foreach (ConsoleClient cc in consoles.Values) {
				if (cc.IsLoggedInAux) {
					cc.Write(0, msg);
				}
			}
		}

		public static IEnumerable<ConsoleClient> AllConsoles {
			get {
				return consoles.Values;
			}
		}

		public static int AllConsolesCount {
			get {
				return consoles.Count;
			}
		}
	}
}