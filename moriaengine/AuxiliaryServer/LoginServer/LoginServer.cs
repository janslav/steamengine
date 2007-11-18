using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

using SteamEngine.Network;
using SteamEngine.Common;

namespace SteamEngine.AuxiliaryServer.LoginServer {
	public class LoginServer : Server<LoginConnection> {
		public LoginServer()
			: base(Settings.loginServerPort) {

		}

		protected override IncomingPacket<LoginConnection> GetPacketImplementation(byte id) {
			return Pool<ConsoleServerIncomingPacket>.Acquire();
		}
	}

}
