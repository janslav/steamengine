using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

using SteamEngine.Communication;
using SteamEngine.Communication.NamedPipes;
using SteamEngine.Common;

namespace SteamEngine.AuxiliaryServer.GameServers {
	public class GameServerServer : NamedPipeServer<GameServerClient> {

		public GameServerServer()
			: base(@"\\.\pipe\myNamedPipe", GameServerProtocol.instance, MainClass.globalLock) {

		}
	}
}
