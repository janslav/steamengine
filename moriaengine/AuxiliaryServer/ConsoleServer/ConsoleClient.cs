using System;
using System.Net;
using System.Collections.Generic;
using System.Text;

using SteamEngine.Communication;
using SteamEngine.Communication.TCP;
using SteamEngine.Common;

namespace SteamEngine.AuxiliaryServer.ConsoleServer {
	public class ConsoleClient : Poolable,
		IConnectionState<TCPConnection<ConsoleClient>, ConsoleClient, IPEndPoint> {

		TCPConnection<ConsoleClient> conn;

		static int uids;
		int uid;

		string accName;
		string password;

		public TCPConnection<ConsoleClient> Conn {
			get {
				return this.conn;
			}
		}

		public int Uid {
			get {
				return uid;
			}
		}

		protected override void On_Reset() {
			uid = uids++;

			base.On_Reset();
		}

		public IEncryption Encryption {
			get { return null; }
		}

		public ICompression Compression {
			get { return null; }
		}

		public void On_Init(TCPConnection<ConsoleClient> conn) {
			this.conn = conn;
			Console.WriteLine(this + " connected.");

			ConsoleServer.AddConnection(this);
		}

		public void On_Close(string reason) {
			//if (

			Console.WriteLine(this + " closed: " + reason);

			ConsoleServer.RemoveConnection(this);
			LoggedInConsoles.RemoveConsole(this);
		}

		public override string ToString() {
			return "ConsoleClient " + uid;
		}

		internal void SetLoginData(string accName, string password) {
			this.accName = accName;
			this.password = password;
		}

		public void TryLoginToGameServer(GameServers.GameServerClient gameServer) {
			GameServers.ConsoleLoginRequestPacket loginRequest = Pool<GameServers.ConsoleLoginRequestPacket>.Acquire();
			loginRequest.Prepare(this.uid, this.accName, this.password);
			gameServer.Conn.SendSinglePacket(loginRequest);
		}

	}
}
