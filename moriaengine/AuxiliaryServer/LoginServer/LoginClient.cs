using System;
using System.Net;
using System.Collections.Generic;
using System.Text;

using SteamEngine.Communication;
using SteamEngine.Communication.TCP;
using SteamEngine.Common;

namespace SteamEngine.AuxiliaryServer.LoginServer {
	public class LoginClient : Poolable,
		IConnectionState<TCPConnection<LoginClient>, LoginClient, IPEndPoint> {

		static int uids;

		int uid;

		IEncryption encryption;

		protected override void On_Reset() {
			this.encryption = new LoginEncryption();
			uid = uids++;

			base.On_Reset();
		}

		public IEncryption Encryption {
			get {
				return encryption;
			}
		}

		public ICompression Compression {
			get {
				return null;
			}
		}

		public void On_Init(TCPConnection<LoginClient> conn) {
			Console.WriteLine(this + " connected from " + conn.EndPoint);
		}

		public void On_Close(string reason) {
			Console.WriteLine(this + " closed: " + reason);
		}

		public override string ToString() {
			return "LoginClient " + uid;
		}

		//public override void Handle(IncomingPacket packet) {
		//    ConsoleServerPacketGroup pg = Pool<ConsoleServerPacketGroup>.Acquire();

		//    pg.AddPacket(Pool<ConsoleServerOutgoingPacket>.Acquire());

		//    //MainClass.server.SendPacketGroup(this, pg);
		//}
	}
}
