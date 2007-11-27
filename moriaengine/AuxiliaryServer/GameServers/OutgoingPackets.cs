using System;
using System.Collections.Generic;
using System.Text;

using SteamEngine.Communication;
using SteamEngine.Common;

namespace SteamEngine.AuxiliaryServer.GameServers {
	public class RequestSendingLogStringsPacket : OutgoingPacket {
		bool sendLogStrings;

		public void Prepare(bool sendLogStrings) {
			this.sendLogStrings = sendLogStrings;
		}

		public override byte Id {
			get { 
				return 0x03; 
			}
		}

		protected override void Write() {
			EncodeBool(this.sendLogStrings);
		}
	}
}
