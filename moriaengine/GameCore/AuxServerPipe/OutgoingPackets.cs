using System;
using System.Net;
using System.Collections.Generic;
using System.Text;

using SteamEngine.Communication;
using SteamEngine.Communication.NamedPipes;
using SteamEngine.Common;

namespace SteamEngine.AuxServerPipe {

	internal class IdentifyGameServerPacket : OutgoingPacket {
		string steamengineIniPath;

		public void Prepare() {
			this.steamengineIniPath = System.IO.Path.GetFullPath(".");
		}

		public override byte Id {
			get {
				return 0x01; 
			}
		}

		protected override void Write() {
			this.EncodeUTF8String(this.steamengineIniPath);
		}
	}


	internal class LogStringPacket : OutgoingPacket {
		string str;

		public void Prepare(string str) {
			this.str = str;
		}

		public override byte Id {
			get {
				return 0x02;
			}
		}

		protected override void Write() {
			this.EncodeUTF8String(this.str);
		}
	}

	internal class ReplyAccountLoginPacket : OutgoingPacket {
		int consoleId;
		string accName;
		bool loginSuccessful;

		public void Prepare(int consoleId, string accName, bool loginSuccessful) {
			this.consoleId = consoleId;
			this.accName = accName;
			this.loginSuccessful = loginSuccessful;
		}

		public override byte Id {
			get {
				return 0x03;
			}
		}

		protected override void Write() {
			this.EncodeInt(this.consoleId);
			this.EncodeUTF8String(this.accName);
			this.EncodeBool(this.loginSuccessful);
		}
	}
}