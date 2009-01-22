using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

using SteamEngine.Communication;
using SteamEngine.Communication.TCP;
using SteamEngine.Common;

namespace SteamEngine.AuxiliaryServer.LoginServer {
	public class LoginServer : TCPServer<LoginClient> {
		private LoginServer()
			: base(LoginServerProtocol.instance, MainClass.globalLock) {

		}

		private static LoginServer instance = new LoginServer();

		internal static void Init() {
			instance.Bind(Settings.loginServerEndpoint);
		}

		internal static void Exit() {
			instance.Dispose();
		}
	}
}
