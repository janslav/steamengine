using System;
using System.Collections.Generic;
using System.Text;

using SteamEngine.Communication;

namespace SteamEngine.AuxiliaryServer.ConsoleServer {

	public class RequestOpenGameServerWindowPacket : OutgoingPacket {
		string name;
		int uid;

		public void Prepare(string name, int uid) {
			this.name = name;
			this.uid = uid;
		}

		public override byte Id {
			get { return 1; }
		}

		protected override void Write() {
			this.EncodeInt(this.uid);
			this.EncodeUTF8String(this.name);
		}
	}

	public class RequestCloseGameServerWindowPacket : OutgoingPacket {
		int uid;

		public void Prepare(int uid) {
			this.uid = uid;
		}

		public override byte Id {
			get { return 2; }
		}

		protected override void Write() {
			this.EncodeInt(this.uid);
		}
	}
}
