using System;
using System.Net;
using System.Collections.Generic;
using System.Text;

using SteamEngine.Communication;
using SteamEngine.Communication.TCP;
using SteamEngine.Common;

namespace SteamEngine.AuxiliaryServer.ConsoleServer {

	public class ConsoleClient : //Disposable,
		IConnectionState<TcpConnection<ConsoleClient>, ConsoleClient, IPEndPoint> {

		TcpConnection<ConsoleClient> conn;

		static ConsoleId uids;
		ConsoleId uid = uids++;

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

		public ConsoleId ConsoleId {
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
			GameServersManager.RemoveConsole(this);
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

		public void TryLoginToGameServers() {
			foreach (GameServer gameServer in GameServersManager.AllIdentifiedGameServers) {
				gameServer.SendConsoleLogin(this.uid, this.accName, this.password);
			}			
		}

		public void TryLoginToGameServer(GameServer gameServer) {
			gameServer.SendConsoleLogin(this.uid, this.accName, this.password);
		}

		internal void SetLoggedInToAux(bool announce) {
			if (announce) {
				Console.WriteLine(this + " identified as " + this.accName);
			}

			this.isLoggedInAux = true;

			RequestOpenCommandWindowPacket openWindow = Pool<RequestOpenCommandWindowPacket>.Acquire();
			openWindow.Prepare("AuxiliaryServer", GameUID.AuxServer);
			this.Conn.SendSinglePacket(openWindow);

			EnableCommandLine(GameUID.AuxServer);
		}

		internal void EnableCommandLine(GameUID serverUid) {
			RequestEnableCommandLinePacket enableCmdLine = Pool<RequestEnableCommandLinePacket>.Acquire();
			enableCmdLine.Prepare(serverUid);
			this.Conn.SendSinglePacket(enableCmdLine);
		}

		internal void CloseCmdWindow(GameUID serverUid) {
			RequestCloseCommandWindowPacket packet = Pool<RequestCloseCommandWindowPacket>.Acquire();
			packet.Prepare(serverUid);
			this.Conn.SendSinglePacket(packet);
		}

		internal void SetLoggedInTo(GameServer gameServer) {
			if (!this.isLoggedInAux) {
				SetLoggedInToAux(false);
			}

			Console.WriteLine(this + " identified as " + this.accName + " with " + gameServer.Setup.Name);

			Settings.RememberUser(this.accName, this.password);

			OpenCmdWindow(gameServer.Setup.Name, gameServer.serverUid);
			EnableCommandLine(gameServer.serverUid);

			GameServersManager.AddLoggedIn(this, gameServer);
		}

		internal void OpenCmdWindow(string name, GameUID serverUid) {
			RequestOpenCommandWindowPacket packet = Pool<RequestOpenCommandWindowPacket>.Acquire();
			packet.Prepare(name, serverUid);
			this.Conn.SendSinglePacket(packet);
		}

		public void Write(GameUID serverUid, string str) {
			SendStringPacket packet = Pool<SendStringPacket>.Acquire();
			packet.Prepare(serverUid, str);
			this.Conn.SendSinglePacket(packet);
		}

		public void WriteLine(GameUID serverUid, string str) {
			SendStringLinePacket packet = Pool<SendStringLinePacket>.Acquire();
			packet.Prepare(serverUid, str);
			this.Conn.SendSinglePacket(packet);
		}

		public void SendLoginFailedAndClose(string reason) {
			LoginFailedPacket packet = Pool<LoginFailedPacket>.Acquire();
			packet.Prepare(reason);			
			this.conn.SendSinglePacket(packet);

			this.conn.Core.WaitForAllSent();
			this.conn.Close(reason);
		}

		public bool PacketGroupsJoiningAllowed {
			get {
				return true;
			}
		}
	}

	//just a renamed int
	public enum ConsoleId {
	}
}
