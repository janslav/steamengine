using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

using SteamEngine.Communication;
using SteamEngine.Common;

namespace SteamEngine.AuxiliaryServer {

	public static class LoggedInConsoles {
		static Dictionary<GameServers.GameServerClient, LinkedList<ConsoleServer.ConsoleClient>> consoles = 
			new Dictionary<GameServers.GameServerClient,LinkedList<ConsoleServer.ConsoleClient>>();
		static Dictionary<ConsoleServer.ConsoleClient, LinkedList<GameServers.GameServerClient>> gameServers = 
			new Dictionary<ConsoleServer.ConsoleClient,LinkedList<GameServers.GameServerClient>>();


		internal static void AddPair(ConsoleServer.ConsoleClient console, GameServers.GameServerClient gameServer) {
			LinkedList<ConsoleServer.ConsoleClient> consoleList;
			if (!consoles.TryGetValue(gameServer, out consoleList)) {
				consoleList = new LinkedList<ConsoleServer.ConsoleClient>();
				consoles.Add(gameServer, consoleList);
			}
			if (!consoleList.Contains(console)) {
				consoleList.AddFirst(console);
			}

			LinkedList<GameServers.GameServerClient> gameServerList;
			if (!gameServers.TryGetValue(console, out gameServerList)) {
				gameServerList = new LinkedList<GameServers.GameServerClient>();
				gameServers.Add(console, gameServerList);
			}
			if (!gameServerList.Contains(gameServer)) {
				gameServerList.AddFirst(gameServer);
			}
		}

		internal static void RemoveConsole(ConsoleServer.ConsoleClient console) {
			LinkedList<GameServers.GameServerClient> gameServerList;
			if (gameServers.TryGetValue(console, out gameServerList)) {
				foreach (GameServers.GameServerClient gameServer in gameServerList) {
					consoles[gameServer].Remove(console);
				}

				gameServers.Remove(console);
			}
		}

		internal static void RemoveGameServer(GameServers.GameServerClient gameServer) {
			LinkedList<ConsoleServer.ConsoleClient> consoleList;
			if (consoles.TryGetValue(gameServer, out consoleList)) {
				foreach (ConsoleServer.ConsoleClient console in consoleList) {
					gameServers[console].Remove(gameServer);
				}

				consoles.Remove(gameServer);
			}
		}

		public static bool IsLoggedIn(ConsoleServer.ConsoleClient console, GameServers.GameServerClient gameServer) {
			LinkedList<ConsoleServer.ConsoleClient> consoleList;
			if (consoles.TryGetValue(gameServer, out consoleList)) {
				return consoleList.Contains(console);
			}
			return false;
		}

		public static IEnumerable<ConsoleServer.ConsoleClient> AllConsolesIn(GameServers.GameServerClient gameServer) {
			LinkedList<ConsoleServer.ConsoleClient> consoleList;
			if (consoles.TryGetValue(gameServer, out consoleList)) {
				return consoleList;
			}
			return EmptyReadOnlyGenericCollection<ConsoleServer.ConsoleClient>.instance;
		}

		public static ICollection<GameServers.GameServerClient> AllServersWhereLoggedIn(ConsoleServer.ConsoleClient console) {
			LinkedList<GameServers.GameServerClient> serversList;
			if (gameServers.TryGetValue(console, out serversList)) {
				return serversList;
			}
			return EmptyReadOnlyGenericCollection<GameServers.GameServerClient>.instance;
		}
	}
}
