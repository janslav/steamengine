using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

using SteamEngine.Communication;
using SteamEngine.Communication.NamedPipes;
using SteamEngine.Common;

namespace SteamEngine.AuxiliaryServer {
	public abstract class GameServer {
		protected GameUid uid;


		public GameUid ServerUid {
			get {
				return this.uid;
			}
		}

		public abstract IGameServerSetup Setup {
			get;
		}

		public abstract bool StartupFinished {
			get;
		}

		public abstract void SendCommand(ConsoleServer.ConsoleClient console, string cmd);

		public abstract void SendConsoleLogin(ConsoleServer.ConsoleId consoleId, string accName, string accPassword);
	}
}
