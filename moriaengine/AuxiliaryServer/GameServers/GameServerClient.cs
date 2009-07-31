using System;
using System.Net;
using System.Collections.Generic;
using System.Text;

using SteamEngine.Communication;
using SteamEngine.Communication.NamedPipes;
using SteamEngine.Common;

namespace SteamEngine.AuxiliaryServer.GameServers {
	public class GameServerClient : Disposable,
		IConnectionState<NamedPipeConnection<GameServerClient>, GameServerClient, string> {

		NamedPipeConnection<GameServerClient> conn;

		static int uids = 1;

		int uid = uids++;

		GameServerInstanceSettings settings;

		private bool startupFinished;

		public NamedPipeConnection<GameServerClient> Conn {
			get {
				return conn;
			}
		}

		internal void SetStartupFinished(bool p) {
			this.startupFinished = p;
		}

		public bool StartupFinished {
			get {
				return this.startupFinished;
			}
		}

		public string Name {
			get {
				return this.settings.Name;
			}
		}

		public GameServerInstanceSettings Setting {
			get {
				return this.settings;
			}
		}

		public int Uid {
			get {
				return this.uid;
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
		public void On_Init(NamedPipeConnection<GameServerClient> conn) {
			Console.WriteLine(this + " connected.");
			this.conn = conn;

			GameServerServer.AddClient(this);
		}

		public void On_Close(string reason) {
			Console.WriteLine(this + " closed: " + reason);

			foreach (ConsoleServer.ConsoleClient console in ConsoleServer.ConsoleServer.AllConsoles) {
				console.CloseCmdWindow(this.uid);
			}

			LoggedInConsoles.RemoveGameServer(this);
			GameServerServer.RemoveClient(this);

			if (GameServerServer.GameServersCount == 0) {
				foreach (ConsoleServer.ConsoleClient console in ConsoleServer.ConsoleServer.AllConsoles) {
					if (!console.IsLoggedInAux) {
						console.Conn.Close("Failed to identify");
					}
				}
			}
		}

		public override string ToString() {
			return "GameServerClient " + uid;
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "settings")]
		internal void SetIdentificationData(GameServerInstanceSettings settings) {
			this.settings = settings;
		}

		internal void WriteToMyConsoles(string str) {
			if (this.startupFinished) {
				foreach (ConsoleServer.ConsoleClient console in LoggedInConsoles.AllConsolesIn(this)) {
					console.Write(this.uid, str);
				}
			} else {
				foreach (ConsoleServer.ConsoleClient console in ConsoleServer.ConsoleServer.AllConsoles) {
					console.Write(this.uid, str);
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
	}
}
