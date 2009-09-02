using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

using SteamEngine.Communication;
using SteamEngine.Common;

namespace SteamEngine.AuxiliaryServer {

	public static class GameServersManager {
		static Dictionary<GameServer, LinkedList<ConsoleServer.ConsoleClient>> consoles =
			new Dictionary<GameServer, LinkedList<ConsoleServer.ConsoleClient>>();
		static Dictionary<ConsoleServer.ConsoleClient, LinkedList<GameServer>> gameServers =
			new Dictionary<ConsoleServer.ConsoleClient, LinkedList<GameServer>>();



		static SortedList<int, GameServer> gameServersByIniID = new SortedList<int, GameServer>();
		static SortedList<GameUid, GameServer> gameServersByUid = new SortedList<GameUid, GameServer>();


		public static ICollection<GameServer> AllRunningGameServers {
			get {
				return gameServersByUid.Values;
			}
		}

		public static ICollection<GameServer> AllIdentifiedGameServers {
			get {
				return gameServersByIniID.Values;
			}
		}

		public static GameServer GetInstanceByIniID(int iniID) {
			GameServer gsc;
			gameServersByIniID.TryGetValue(iniID, out gsc);
			return gsc;
		}

		public static GameServer GetInstanceByUid(GameUid uid) {
			GameServer gsc;
			gameServersByUid.TryGetValue(uid, out gsc);
			return gsc;
		}

		internal static void AddGameServer(GameServer gameServer) {
			gameServersByUid[gameServer.ServerUid] = gameServer;
			if (gameServer.Setup != null) {
				gameServersByIniID[gameServer.Setup.IniID] = gameServer;
			}
		}

		#region logged in state tracking
		internal static void AddLoggedIn(ConsoleServer.ConsoleClient console, GameServer gameServer) {
			LinkedList<ConsoleServer.ConsoleClient> consoleList;
			if (!consoles.TryGetValue(gameServer, out consoleList)) {
				consoleList = new LinkedList<ConsoleServer.ConsoleClient>();
				consoles.Add(gameServer, consoleList);
			}
			if (!consoleList.Contains(console)) {
				consoleList.AddFirst(console);
			}

			LinkedList<GameServer> gameServerList;
			if (!gameServers.TryGetValue(console, out gameServerList)) {
				gameServerList = new LinkedList<GameServer>();
				gameServers.Add(console, gameServerList);
			}
			if (!gameServerList.Contains(gameServer)) {
				gameServerList.AddFirst(gameServer);
			}
		}

		internal static void RemoveConsole(ConsoleServer.ConsoleClient console) {
			LinkedList<GameServer> gameServerList;
			if (gameServers.TryGetValue(console, out gameServerList)) {
				foreach (GameServer gameServer in gameServerList) {
					consoles[gameServer].Remove(console);
				}

				gameServers.Remove(console);
			}
		}

		internal static void RemoveGameServer(GameServer gameServer) {
			LinkedList<ConsoleServer.ConsoleClient> consoleList;
			if (consoles.TryGetValue(gameServer, out consoleList)) {
				

				foreach (ConsoleServer.ConsoleClient console in consoleList) {
					gameServers[console].Remove(gameServer);

					console.CloseCmdWindow(gameServer.ServerUid);
				}

				consoles.Remove(gameServer);
			}

			if (gameServer.Setup != null) {
				gameServersByIniID.Remove(gameServer.Setup.IniID);
			}
			gameServersByUid.Remove(gameServer.ServerUid);


			if (AllIdentifiedGameServers.Count == 0) { //it was the last server, we kick nonlogged consoles
				List<ConsoleServer.ConsoleClient> toKick = new List<SteamEngine.AuxiliaryServer.ConsoleServer.ConsoleClient>();
				foreach (ConsoleServer.ConsoleClient console in ConsoleServer.ConsoleServer.AllConsoles) {
					if (!console.IsLoggedInAux) {
						toKick.Add(console);
					}
				}
				foreach (ConsoleServer.ConsoleClient console in toKick) {
					console.Conn.Close("Failed to identify");
				}
			}
		}

		public static bool IsLoggedIn(ConsoleServer.ConsoleClient console, GameServer gameServer) {
			LinkedList<ConsoleServer.ConsoleClient> consoleList;
			if (consoles.TryGetValue(gameServer, out consoleList)) {
				return consoleList.Contains(console);
			}
			return false;
		}

		public static IEnumerable<ConsoleServer.ConsoleClient> AllConsolesIn(GameServer gameServer) {
			LinkedList<ConsoleServer.ConsoleClient> consoleList;
			if (consoles.TryGetValue(gameServer, out consoleList)) {
				return consoleList;
			}
			return EmptyReadOnlyGenericCollection<ConsoleServer.ConsoleClient>.instance;
		}

		public static ICollection<GameServer> AllServersWhereLoggedIn(ConsoleServer.ConsoleClient console) {
			LinkedList<GameServer> serversList;
			if (gameServers.TryGetValue(console, out serversList)) {
				return serversList;
			}
			return EmptyReadOnlyGenericCollection<GameServer>.instance;
		}
		#endregion logged in state tracking

	}
}
