using System;
using System.Net;
using System.Collections.Generic;
using System.Text;

using SteamEngine.Communication;
using SteamEngine.Communication.NamedPipes;
using SteamEngine.Common;

namespace SteamEngine.AuxiliaryServer.GameServers {
	public class GameServerClient : Poolable,
		IConnectionState<NamedPipeConnection<GameServerClient>, GameServerClient, string> {

		NamedPipeConnection<GameServerClient> conn;

		static int uids;

		int uid;

		int port;
		string serverName;
		string executablePath;

		public NamedPipeConnection<GameServerClient> Conn {
			get {
				return conn;
			}
		}

		public string Name {
			get {
				return this.serverName;
			}
		}

		public int Uid {
			get {
				return this.uid;
			}
		}

		protected override void On_Reset() {
			uid = uids++;

			base.On_Reset();
		}

		public IEncryption Encryption {
			get {
				return null;
			}
		}

		public ICompression Compression {
			get {
				return null;
			}
		}

		public void On_Init(NamedPipeConnection<GameServerClient> conn) {
			Console.WriteLine(this + " connected.");
			this.conn = conn;

			GameServerServer.AddClient(this);

			foreach (ConsoleServer.ConsoleClient console in ConsoleServer.ConsoleServer.AllConsoles) {
				console.TryLoginToGameServer(this);
			}
		}

		public void On_Close(string reason) {
			Console.WriteLine(this + " closed: "+reason);

			ConsoleServer.RequestCloseGameServerWindowPacket packet = Pool<ConsoleServer.RequestCloseGameServerWindowPacket>.Acquire();
			packet.Prepare(this.uid);
			PacketGroup group = Pool<PacketGroup>.Acquire();
			group.AddPacket(packet);

			bool usedGroup = false;

			foreach (ConsoleServer.ConsoleClient console in LoggedInConsoles.AllConsolesIn(this)) {
				console.Conn.SendPacketGroup(group);
				usedGroup = true;
			}

			if (!usedGroup) {
				group.Dispose();
			}

			LoggedInConsoles.RemoveGameServer(this);
			GameServerServer.RemoveClient(this);
		}

		public override string ToString() {
			return "GameServerClient "+uid;
		}

		internal void SetIdentificationData(ushort port, string serverName, string executablePath) {
			this.port = port;
			this.serverName = serverName;
			this.executablePath = executablePath;
		}
	}
}
