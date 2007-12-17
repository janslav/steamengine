using System;
using System.Net;
using System.Collections.Generic;
using System.Text;

using SteamEngine.Communication;
using SteamEngine.Communication.TCP;

namespace SteamEngine.RemoteConsole {

	public class LoginRequestPacket : OutgoingPacket {
		private string accName;
		private string password;

		public void Prepare(string accName, string password) {
			this.accName = accName;
			this.password = password;
		}

		public override byte Id {
			get { return 0; }
		}

		protected override void Write() {
			this.EncodeUTF8String(this.accName);
			this.EncodeUTF8String(this.password);
		}
	}

}