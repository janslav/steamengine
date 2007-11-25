using System;
using System.Collections.Generic;
using System.Text;

using SteamEngine.Communication;
using SteamEngine.Common;

namespace SteamEngine.AuxiliaryServer.LoginServer {

	public class ServersListPacket : OutgoingPacket {
		private byte[] ip;
		private List<string> names = new List<string>();
		private sbyte timezone;

		public override byte Id {
			get { return 0xA8; }
		}

		public void Prepare(byte[] ip) {
			this.names.Clear();
			foreach (LoginServerInstanceSettings server in Settings.loginSettings) {
				this.names.Add(server.name);
			}
			this.ip = ip;
			this.timezone = Settings.timeZone;
		}

		protected override void Write() {
			int serverNumber = this.names.Count;
			EncodeUShort((ushort) ((serverNumber * 40) + 6)); //length

			EncodeByte(0x5d);	//0x13; //unknown //0x5d on RunUo

			EncodeUShort((ushort) serverNumber);

			for (int i = 0; i < serverNumber; i++) {
				EncodeUShort((ushort) i);
				EncodeASCIIString(this.names[i], 32);
				EncodeByte(0);
				EncodeSByte(this.timezone);
				EncodeBytesReversed(this.ip);
			}
		}
	}

	public class LoginToServerPacket : OutgoingPacket {
		private byte[] ip;
		private ushort port;

		public override byte Id {
			get { return 0x8c; }
		}

		public void Prepare(byte[] ip, ushort port) {
			this.ip = ip;
			this.port = port;
		}

		protected override void Write() {
			EncodeBytes(this.ip);
			EncodeUShort(this.port);
			EncodeZeros(4); //new key. could be random, but who cares...
		}
	}

	//public class ConsoleServerPacketGroup : PacketGroup {
	//    public ConsoleServerPacketGroup() {
	//        base.SetType(PacketGroupType.SingleUse);
	//    }
	//}
}
