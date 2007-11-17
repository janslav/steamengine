using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using SteamEngine.Network;
using SteamEngine.Common;

namespace SteamEngine.AuxiliaryServer.LoginServer {
	public class LoginServer : Server<ConsoleServerSocket> {
		public LoginServer()
			: base(12345) {

		}

		protected override IncomingPacket GetPacketImplementation(byte id) {
			return Pool<ConsoleServerIncomingPacket>.Acquire();
		}
	}

	public class ConsoleServerSocket : SteamSocket {

		public override void Handle(IncomingPacket packet) {
			ConsoleServerPacketGroup pg = Pool<ConsoleServerPacketGroup>.Acquire();

			pg.AddPacket(Pool<ConsoleServerOutgoingPacket>.Acquire());

			//MainClass.server.SendPacketGroup(this, pg);
		}
	}

	public class ConsoleServerIncomingPacket : IncomingPacket {

		protected override bool Read(int count) {
			this.position += count;

			return true;
		}
	}


	public class ConsoleServerOutgoingPacket : OutgoingPacket {

		public override byte Id {
			get { return 0; }
		}

		public override string Name {
			get { return "ConsoleOutgoingPacket"; }
		}

		protected override void Write() {
			this.position++;
		}
	}

	public class ConsoleServerPacketGroup : PacketGroup {
		public ConsoleServerPacketGroup() {
			base.SetType(PacketGroupType.SingleUse);
		}
	}
}
