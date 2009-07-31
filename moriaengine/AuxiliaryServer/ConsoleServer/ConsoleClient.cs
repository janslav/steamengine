using System;
using System.Net;
using System.Collections.Generic;
using System.Text;

using SteamEngine.Communication;
using SteamEngine.Communication.TCP;
using SteamEngine.Common;

namespace SteamEngine.AuxiliaryServer.ConsoleServer {
	public class ConsoleClient : Disposable,
		IConnectionState<TcpConnection<ConsoleClient>, ConsoleClient, IPEndPoint> {

		TcpConnection<ConsoleClient> conn;

		static int uids;
		int uid = uids++;

		string accName;
		string password;

		private bool isLoggedInAux;

		public bool IsLoggedInAux {
			get {
				return this.isLoggedInAux;
			}
		}

		public TcpConnection<ConsoleClient> Conn {
			get {
				return this.conn;
			}
		}

		public int Uid {
			get {
				return uid;
			}
		}

		public IEncryption Encryption {
			get { return null; }
		}

		public ICompression Compression {
			get { return null; }
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "conn")]
		public void On_Init(TcpConnection<ConsoleClient> conn) {
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

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "accName"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "password")]
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

		internal void OpenCmdWindow(string name, int cmdWinUid) {
			RequestOpenCommandWindowPacket packet = Pool<RequestOpenCommandWindowPacket>.Acquire();
			packet.Prepare(name, cmdWinUid);
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

		public void SendLoginFailedAndClose(string reason) {
			TcpConnection<ConsoleClient> conn = this.Conn;

			LoginFailedPacket packet = Pool<LoginFailedPacket>.Acquire();
			packet.Prepare(reason);			
			conn.SendSinglePacket(packet);

			conn.Core.WaitForAllSent();
			conn.Close(reason);
		}

		public bool PacketGroupsJoiningAllowed {
			get {
				return true;
			}
		}
	}
}
