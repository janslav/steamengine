using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

using SteamEngine.Communication;
using SteamEngine.Communication.TCP;
using SteamEngine.Common;

namespace SteamEngine.AuxiliaryServer.LoginServer {
	public class LoginServer : TCPServer<LoginClient> {
		public LoginServer()
				: base(PacketHandlers.instance, MainClass.globalLock) {

			this.Bind(Settings.loginServerEndpoint);
		}
	}
}
