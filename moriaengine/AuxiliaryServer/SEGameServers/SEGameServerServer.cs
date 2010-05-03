using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

using SteamEngine.Communication;
using SteamEngine.Communication.NamedPipes;
using SteamEngine.Common;

namespace SteamEngine.AuxiliaryServer.SEGameServers {
	public sealed class SEGameServerServer : NamedPipeServer<SEGameServerClient> {
		private static SEGameServerServer instance = new SEGameServerServer();

		private SEGameServerServer()
			: base(SEGameServerProtocol.instance, MainClass.GlobalLock) {

		}

		internal static void Init() {
#if MSWIN
			instance.Bind(Common.Tools.commonPipeName);
#else
			instance.Bind(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, Common.Tools.commonPort));
#endif

		}

		internal static void Exit() {
			instance.Dispose();
		}

		internal static void StartSendingLogStr() {
			BroadcastRSLS(true);
		}

		internal static void StopSendingLogStr() {
			BroadcastRSLS(false);
		}

		private static void BroadcastRSLS(bool state) {
			foreach (GameServer gameServer in GameServersManager.AllRunningGameServers) {
				SEGameServerClient segs = gameServer as SEGameServerClient;
				if (segs != null) {
					segs.RequestSendingLogStr(state);
				}
			}
		}
	}
}
