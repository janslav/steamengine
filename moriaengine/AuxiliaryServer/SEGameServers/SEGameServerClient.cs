using System;
using System.Net;
using System.Collections.Generic;
using System.Text;

using SteamEngine.Communication;
using SteamEngine.Communication.NamedPipes;
using SteamEngine.Common;

namespace SteamEngine.AuxiliaryServer.SEGameServers {
	public class SEGameServerClient : SteamEngine.AuxiliaryServer.GameServer,
		IConnectionState<NamedPipeConnection<SEGameServerClient>, SEGameServerClient, string> {

		NamedPipeConnection<SEGameServerClient> conn;

		SEGameServerSetup settings;

		private bool startupFinished;

		static GameUID uids = GameUID.FirstSEGameServer;

		public NamedPipeConnection<SEGameServerClient> Conn {
			get {
				return conn;
			}
		}

		internal void SetStartupFinished(bool p) {
			this.startupFinished = p;
		}

		public override bool StartupFinished {
			get {
				return this.startupFinished;
			}
		}

		public override IGameServerSetup Setup {
			get {
				return this.settings;
			}
		}

		public IEncryption Encryption {
			get {
				return null;
			}
		}

		public ICompression Compression {
			get {
				return null;
			}
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "conn")]
		public void On_Init(NamedPipeConnection<SEGameServerClient> conn) {
			this.uid = uids++;
			Console.WriteLine(this + " connected.");
			this.conn = conn;

			GameServersManager.AddGameServer(this);
		}

		public void On_Close(string reason) {
			Console.WriteLine(this + " closed: " + reason);

			foreach (ConsoleServer.ConsoleClient console in ConsoleServer.ConsoleServer.AllConsoles) {
				console.CloseCmdWindow(this.serverUid);
			}

			GameServersManager.RemoveGameServer(this);

			if (GameServersManager.AllIdentifiedGameServers.Count == 0) { //it was the last server, we kick nonlogged consoles
				foreach (ConsoleServer.ConsoleClient console in ConsoleServer.ConsoleServer.AllConsoles) {
					if (!console.IsLoggedInAux) {
						console.Conn.Close("Failed to identify");
					}
				}
			}
		}

		public override string ToString() {
			return "SEGameServerClient " + this.serverUid;
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "settings")]
		internal void SetIdentificationData(SEGameServerSetup settings) {
			this.settings = settings;
		}

		internal void WriteToMyConsoles(string str) {
			if (this.startupFinished) {
				foreach (ConsoleServer.ConsoleClient console in GameServersManager.AllConsolesIn(this)) {
					console.Write(this.serverUid, str);
				}
			} else {
				foreach (ConsoleServer.ConsoleClient console in ConsoleServer.ConsoleServer.AllConsoles) {
					console.Write(this.serverUid, str);
				}
			}
		}

		internal void RequestSendingLogStr(bool state) {
			RequestSendingLogStringsPacket packet = Pool<RequestSendingLogStringsPacket>.Acquire();
			packet.Prepare(state);
			this.conn.SendSinglePacket(packet);
		}

		public bool PacketGroupsJoiningAllowed {
			get {
				return false;
			}
		}

		public override void SendCommand(ConsoleServer.ConsoleId consoleId, string accName, string accPassword, string cmd) {
			ConsoleCommandLinePacket p = Pool<ConsoleCommandLinePacket>.Acquire();
			p.Prepare(consoleId, accName, accPassword, cmd);
			this.Conn.SendSinglePacket(p);
		}

		public override void SendConsoleLogin(ConsoleServer.ConsoleId consoleId, string accName, string accPassword) {
			ConsoleLoginRequestPacket loginRequest = Pool<ConsoleLoginRequestPacket>.Acquire();
			loginRequest.Prepare(consoleId, accName, accPassword);
			this.Conn.SendSinglePacket(loginRequest);
		}
	}
}
