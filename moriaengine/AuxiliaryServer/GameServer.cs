using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

using SteamEngine.Communication;
using SteamEngine.Communication.NamedPipes;
using SteamEngine.Common;

namespace SteamEngine.AuxiliaryServer {
	public abstract class GameServer {
		protected GameUID uid;


		public GameUID ServerUid {
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

	//used more or less just like a renamed int. A new struct might be better but this works too :)
	//just wanted to make clear what's what
	public enum GameUID {
		AuxServer = 0,
		FirstSEGameServer = 1,
		LastSphereServer = int.MaxValue,
		//...
	}
}
