using System;
using System.Collections.Generic;
using System.Text;

using SteamEngine.Network;
using SteamEngine.Common;

namespace SteamEngine.AuxiliaryServer.LoginServer {
	public class ConsoleServerIncomingPacket : IncomingPacket<LoginConnection> {

		//protected override bool Read(int count) {
		//    this.position += count;

		//    return true;
		//}
		protected override bool Read(int count) {
			throw new Exception("The method or operation is not implemented.");
		}

		public override void Handle(LoginConnection packet) {
			throw new Exception("The method or operation is not implemented.");
		}
	}
}
