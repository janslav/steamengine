using System;
using System.Collections.Generic;
using System.Text;

using SteamEngine.Communication;

namespace SteamEngine.AuxiliaryServer.ConsoleServer {

	public class RequestOpenCommandWindowPacket : OutgoingPacket {
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

	public class RequestEnableCommandLinePacket : OutgoingPacket {
		int uid;

		public void Prepare(int uid) {
			this.uid = uid;
		}

		public override byte Id {
			get { return 3; }
		}

		protected override void Write() {
			this.EncodeInt(this.uid);
		}
	}

	public class RequestCloseCommandWindowPacket : OutgoingPacket {
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

	public class SendStringPacket : OutgoingPacket {
		string str;
		int uid;

		public void Prepare(int uid, string str) {
			this.uid = uid;
			this.str = str;
		}

		public override byte Id {
			get { return 4; }
		}

		protected override void Write() {
			this.EncodeInt(this.uid);
			this.EncodeUTF8String(this.str);
		}
	}
}
