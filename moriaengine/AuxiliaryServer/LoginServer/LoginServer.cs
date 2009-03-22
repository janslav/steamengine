using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

using SteamEngine.Communication;
using SteamEngine.Communication.TCP;
using SteamEngine.Common;

namespace SteamEngine.AuxiliaryServer.LoginServer {
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1724:TypeNamesShouldNotMatchNamespaces")]
	public sealed class LoginServer : TCPServer<LoginClient> {
		private LoginServer()
			: base(LoginServerProtocol.instance, MainClass.GlobalLock) {

		}

		private static LoginServer instance = new LoginServer();

		internal static void Init() {
			instance.Bind(Settings.LoginServerEndpoint);
		}

		internal static void Exit() {
			instance.Dispose();
		}
	}
}
