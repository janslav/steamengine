using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

using SteamEngine.Communication;
using SteamEngine.Communication.TCP;
using SteamEngine.Common;

namespace SteamEngine.AuxiliaryServer.ConsoleServer {
	public class ConsoleServer : TCPServer<ConsoleClient> {
		public ConsoleServer()
			: base(ConsoleServerProtocol.instance, MainClass.globalLock) {
		}

		private static ConsoleServer instance = new ConsoleServer();

		private static Dictionary<int, ConsoleClient> consoles = new Dictionary<int, ConsoleClient>();


		internal static void Init() {
			instance.Bind(Settings.consoleServerEndpoint);
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