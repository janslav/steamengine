using System;
using System.Collections.Generic;
using System.Text;

using SteamEngine.Network;
using SteamEngine.Common;

namespace SteamEngine.AuxiliaryServer.LoginServer {
	public class LoginConnection : SteamSocket {

		static int uids;

		int uid = uids++;

		IEncryption encryption = new LoginEncryption();

		public override IEncryption Encryption {
			get {
				return encryption;
			}
		}

		public override void On_Connect() {
			Console.WriteLine(this + " connected from "+this.EndPoint);

			base.On_Connect();
		}

		public override void On_Close(LogStr reason) {
			Console.WriteLine(this + " closed: "+reason);

			base.On_Close(reason);
		}

		public override void On_Close(string reason) {
			Console.WriteLine(this + " closed: "+reason);

			base.On_Close(reason);
		}

		public override string ToString() {
			return "LoginConnection "+uid;
		}
		//public override void Handle(IncomingPacket packet) {
		//    ConsoleServerPacketGroup pg = Pool<ConsoleServerPacketGroup>.Acquire();

		//    pg.AddPacket(Pool<ConsoleServerOutgoingPacket>.Acquire());

		//    //MainClass.server.SendPacketGroup(this, pg);
		//}
	}
}
