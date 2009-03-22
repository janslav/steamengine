using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

using SteamEngine.Communication;
using SteamEngine.Communication.NamedPipes;
using SteamEngine.Common;

namespace SteamEngine.AuxiliaryServer.GameServers {
	public sealed class GameServerServer : NamedPipeServer<GameServerClient> {

		static LinkedList<GameServerClient> clients = new LinkedList<GameServerClient>();

		private GameServerServer()
			: base(GameServerProtocol.instance, MainClass.GlobalLock) {
		}

		private static GameServerServer instance = new GameServerServer();

		internal static void Init() {
			instance.Bind(Common.Tools.commonPipeName);

		}

		internal static void Exit() {
			instance.Dispose();
		}

		public static IEnumerable<GameServerClient> AllGameServers {
			get {
				return clients;
			}
		}

		public static int GameServersCount {
			get {
				return clients.Count;
			}
		}

		public static GameServerClient GetInstanceByNumber(int number) {
			foreach (GameServerClient gsc in clients) {
				if (gsc.Setting.Number == number) {
					return gsc;
				}
			}
			return null;
		}

		public static GameServerClient GetInstanceByUid(int uid) {
			foreach (GameServerClient gsc in clients) {
				if (gsc.Uid == uid) {
					return gsc;
				}
			}
			return null;
		}

		internal static void AddClient(GameServerClient gameServerClient) {
			clients.AddFirst(gameServerClient);
		}

		internal static void RemoveClient(GameServerClient gameServerClient) {
			clients.Remove(gameServerClient);
		}

		internal static void StartSendingLogStr() {
			BroadcastRSLS(true);
		}

		internal static void StopSendingLogStr() {
			BroadcastRSLS(false);
		}

		private static void BroadcastRSLS(bool state) {
			foreach (GameServerClient gameServer in clients) {
				gameServer.RequestSendingLogStr(state);
			}
		}
	}
}
