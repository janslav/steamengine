using System;
using System.Net;
using System.Collections.Generic;
using System.Text;

using SteamEngine.Communication;
using SteamEngine.Communication.TCP;
using SteamEngine.Common;

namespace SteamEngine.AuxiliaryServer.ConsoleServer {
	public class ConsoleClient : Poolable,
		IConnectionState<TCPConnection<ConsoleClient>, ConsoleClient, IPEndPoint> {

		TCPConnection<ConsoleClient> conn;

		static int uids;
		int uid;

		string accName;
		string password;

		private bool isLoggedInAux;

		public bool IsLoggedInAux {
			get {
				return this.isLoggedInAux;
			}
		}

		public TCPConnection<ConsoleClient> Conn {
			get {
				return this.conn;
			}
		}

		public int Uid {
			get {
				return uid;
			}
		}

		protected override void On_Reset() {
			this.uid = uids++;

			this.isLoggedInAux = false;
			this.accName = null;
			this.password = null;

			base.On_Reset();
		}

		public IEncryption Encryption {
			get { return null; }
		}

		public ICompression Compression {
			get { return null; }
		}

		public void On_Init(TCPConnection<ConsoleClient> conn) {
			this.conn = conn;
			Console.WriteLine(this + " connected.");

			ConsoleServer.AddConnection(this);
		}

		public void On_Close(string reason) {
			Console.WriteLine(this + " closed: " + reason);

			ConsoleServer.RemoveConnection(this);
			LoggedInConsoles.RemoveConsole(this);
		}

		public override string ToString() {
			return "ConsoleClient " + uid;
		}

		internal void SetLoginData(string accName, string password) {
			this.accName = accName;
			this.password = password;
		}

		public string AccountName {
			get {
				return this.accName;
			}
		}

		internal string Password {
			get {
				return this.password;
			}
		}

		public void TryLoginToGameServer(GameServers.GameServerClient gameServer) {
			GameServers.ConsoleLoginRequestPacket loginRequest = Pool<GameServers.ConsoleLoginRequestPacket>.Acquire();
			loginRequest.Prepare(this.uid, this.accName, this.password);
			gameServer.Conn.SendSinglePacket(loginRequest);
		}

		internal void SetLoggedInToAux(bool announce) {
			if (announce) {
				Console.WriteLine(this + " identified as " + this.accName);
			}

			this.isLoggedInAux = true;

			RequestOpenCommandWindowPacket openWindow = Pool<RequestOpenCommandWindowPacket>.Acquire();
			openWindow.Prepare("AuxiliaryServer", 0);
			this.Conn.SendSinglePacket(openWindow);

			EnableCommandLine(0);
		}

		internal void EnableCommandLine(int serverUid) {
			RequestEnableCommandLinePacket enableCmdLine = Pool<RequestEnableCommandLinePacket>.Acquire();
			enableCmdLine.Prepare(serverUid);
			this.Conn.SendSinglePacket(enableCmdLine);
		}

		internal void CloseCmdWindow(int serverUid) {
			RequestCloseCommandWindowPacket packet = Pool<RequestCloseCommandWindowPacket>.Acquire();
			packet.Prepare(serverUid);
			this.Conn.SendSinglePacket(packet);
		}

		internal void SetLoggedInTo(GameServers.GameServerClient state) {
			if (!this.isLoggedInAux) {
				SetLoggedInToAux(false);
			}

			Console.WriteLine(this + " identified as " + this.accName + " with " + state.Name);

			Settings.RememberUser(this.accName, this.password);

			OpenCmdWindow(state.Name, state.Uid);
			EnableCommandLine(state.Uid);

			LoggedInConsoles.AddPair(this, state);
		}

		internal void OpenCmdWindow(string name, int uid) {
			RequestOpenCommandWindowPacket packet = Pool<RequestOpenCommandWindowPacket>.Acquire();
			packet.Prepare(name, uid);
			this.Conn.SendSinglePacket(packet);
		}

		public void Write(int serverUid, string str) {
			SendStringPacket packet = Pool<SendStringPacket>.Acquire();
			packet.Prepare(serverUid, str);
			this.Conn.SendSinglePacket(packet);
		}

		public void WriteLine(int serverUid, string str) {
			SendStringLinePacket packet = Pool<SendStringLinePacket>.Acquire();
			packet.Prepare(serverUid, str);
			this.Conn.SendSinglePacket(packet);
		}
	}
}
