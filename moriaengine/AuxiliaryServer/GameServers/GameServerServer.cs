using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

using SteamEngine.Communication;
using SteamEngine.Communication.NamedPipes;
using SteamEngine.Common;

namespace SteamEngine.AuxiliaryServer.GameServers {
	public class GameServerServer : NamedPipeServer<GameServerClient> {

		static LinkedList<GameServerClient> clients = new LinkedList<GameServerClient>();

		private GameServerServer()
			: base(GameServerProtocol.instance, MainClass.globalLock) {
		}

		private static GameServerServer instance = new GameServerServer();

		internal static void Init() {
			instance.Bind(Common.Tools.commonPipeName);
			
		}

		public static IEnumerable<GameServerClient> AllGameServers {
			get {
				return clients;
			}
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
			RequestSendingLogStringsPacket packet = Pool<RequestSendingLogStringsPacket>.Acquire();
			packet.Prepare(state);

			PacketGroup group = Pool<PacketGroup>.Acquire();
			group.AddPacket(packet);

			foreach (GameServerClient gameServer in clients) {
				gameServer.Conn.SendPacketGroup(group);
			}
		}
	}
}
