using System;
using System.Collections.Generic;
using System.Text;

using SteamEngine.Network;
using SteamEngine.Common;

namespace SteamEngine.AuxiliaryServer.LoginServer {

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
