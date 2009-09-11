using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;
using System.ComponentModel;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Text.RegularExpressions;

using SteamEngine.Common;

namespace SteamEngine.AuxiliaryServer.SphereServers {
	public class SphereServerClient : GameServer {
		static GameUid uids = GameUid.LastSphereServer;

		private readonly SphereServerConnection conn;
		private SphereServerSetup setup;

		private bool loggedIn;

		private Timer consoleAuthTimeout;


		internal SphereServerClient(SphereServerConnection sphereServerConnection, SphereServerSetup setup) {
			this.uid = uids--;
			this.conn = sphereServerConnection;
			this.setup = setup;

			GameServersManager.AddGameServer(this);

			Console.WriteLine(this + " connected.");

			this.conn.StartLoginSequence(this.setup);
		}

		internal void On_Close(string reason) {
			Console.WriteLine(this + " closed: " + reason);

			GameServersManager.RemoveGameServer(this);

			this.DisposeTimeoutTimer();

			SphereServerClientFactory.Connect(this.setup, 2000); //start connecting again
		}


		public SphereServerConnection Conn {
			get {
				return conn;
			}
		}

		public override IGameServerSetup Setup {
			get {
				return this.setup;
			}
		}

		public override bool StartupFinished {
			get {
				return true; //we wouldn't even connect otherwise
			}
		}

		public bool IsLoggedIn {
			get {
				return this.loggedIn;
			}
		}

		public override string ToString() {
			return "Sphere 0x" + ((int) this.ServerUid).ToString("X");
		}

		internal void On_LoginSequenceFinished(LoginResult result) {
			switch (result) {
				case LoginResult.Success:
					Console.WriteLine(this + " logged in.");
					this.loggedIn = true;
					foreach (ConsoleServer.ConsoleClient console in ConsoleServer.ConsoleServer.AllConsoles) {
						console.OpenCmdWindow(this.Setup.Name, this.ServerUid);
						console.TryLoginToGameServer(this);
					}
					break;
				case LoginResult.BadPassword:
					this.conn.Close("Bad admin password set in steamaux.ini");
					break;
				case LoginResult.AccountInUse:
					this.conn.Close("Account '" + this.setup.AdminAccount + "' already in use.");
					break;
			}
		}

		#region authentising console
		public override void SendConsoleLogin(ConsoleServer.ConsoleId consoleId, string accName, string accPassword) {
			ConsoleCredentials state = new ConsoleCredentials(consoleId, accName, accPassword);

			if (accPassword.Length == 0) {
				this.DenyConsole(state, " - password must be > 0 characters");
				return;
			}

			this.conn.SendCommand("show findaccount(" + accName + ").password", 
				new CallbackCommandResponse<ConsoleCredentials>(
					this.OnAccountQueryResponse, state));

			consoleAuthTimeout = new Timer(ConsoleAuthTimeout, state, 5000, Timeout.Infinite);
		}

		private void ConsoleAuthTimeout(object o) {
			ConsoleCredentials state = (ConsoleCredentials) o;

			this.Conn.Close("user auth sequence with sphere timed out.");
		}

		private class ConsoleCredentials {
			internal ConsoleServer.ConsoleId consoleUid;
			internal string accName, accPassword;

			internal ConsoleCredentials(ConsoleServer.ConsoleId consoleUid, string accName, string accPassword) {
				this.consoleUid = consoleUid;
				this.accName = accName;
				this.accPassword = accPassword;
			}
		}

		static Regex passwordQueryRE = new Regex(
			@"findaccount\((?<username>.+?)\).password' for '(.+?)' is '(?<password>.*?)'",
			RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);

		private void OnAccountQueryResponse(IList<string> lines, ConsoleCredentials state) {
			try {
				foreach (string line in lines) {
					Match m = passwordQueryRE.Match(line);
					if (m.Groups["username"].Value.Equals(state.accName, StringComparison.OrdinalIgnoreCase)) {
						if (!m.Groups["password"].Value.Equals(state.accPassword)) {
							DenyConsole(state, "and/or it's password.");
						} else {
							this.conn.SendCommand("show findaccount(" + state.accName + ").priv",
								new CallbackCommandResponse<ConsoleCredentials>(
									this.OnPrivQueryResponse, state));
						}
						return;
					}
				}
			} catch (Exception e) {
				this.DenyConsole(state, "Exception while auth sequence: " + e.Message);
				Logger.WriteDebug(e);
			}
		}

		static Regex privQueryRE = new Regex(
			@"findaccount\((?<username>.+?)\).priv' for '(.+?)' is '(?<priv>.*?)'",
			RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);

		private void OnPrivQueryResponse(IList<string> lines, ConsoleCredentials state) {
			try {
				foreach (string line in lines) {
					Match m = privQueryRE.Match(line);
					if (m.Groups["username"].Value.Equals(state.accName, StringComparison.OrdinalIgnoreCase)) {
						int priv = ConvertTools.ParseInt32(m.Groups["priv"].Value.Trim());
						if ((priv & 0x0200) == 0x0200) {
							DenyConsole(state, "- the acc is blocked.");
						} else {
							this.conn.SendCommand("show findaccount(" + state.accName + ").plevel",
								new CallbackCommandResponse<ConsoleCredentials>(
									this.OnPlevelQueryResponse, state));
						}
						return;
					}
				}
			} catch (Exception e) {
				this.DenyConsole(state, "Exception while auth sequence: " + e.Message);
				Logger.WriteDebug(e);
			}
		}

		static Regex plevelQueryRE = new Regex(
			@"findaccount\((?<username>.+?)\).plevel' for '(.+?)' is '(?<plevel>.*?)'",
			RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);

		private void OnPlevelQueryResponse(IList<string> lines, ConsoleCredentials state) {
			try {
				foreach (string line in lines) {
					Match m = plevelQueryRE.Match(line);
					if (m.Groups["username"].Value.Equals(state.accName, StringComparison.OrdinalIgnoreCase)) {
						int plevel = ConvertTools.ParseInt32(m.Groups["plevel"].Value.Trim());
						if (plevel < 4) {
							DenyConsole(state, "- low plevel.");
						} else {
							ConsoleServer.ConsoleClient console = ConsoleServer.ConsoleServer.GetClientByUid(state.consoleUid);
							if (console != null) {
								console.SetLoggedInTo(this);
							}
							this.SendCommand(console, "i");
						}
						this.DisposeTimeoutTimer();
						return;
					}
				}
			} catch (Exception e) {
				this.DenyConsole(state, "Exception while auth sequence: " + e.Message);
				Logger.WriteDebug(e);
			}
		}

		private void DenyConsole(ConsoleCredentials state, string reason) {
			ConsoleServer.ConsoleClient console = ConsoleServer.ConsoleServer.GetClientByUid(state.consoleUid);
			if (console != null) {
				console.CloseCmdWindow(this.ServerUid);

				ICollection<GameServer> serversLoggedIn = GameServersManager.AllServersWhereLoggedIn(console);
				if (serversLoggedIn.Count == 0) {
					Settings.ForgetUser(console.AccountName);
					console.SendLoginFailedAndClose("GameServer '" + this.Setup.Name + "' rejected username '" + state.accName + "' " + reason);
				}
			}

			this.DisposeTimeoutTimer();
		}

		private void DisposeTimeoutTimer() {
			if (this.consoleAuthTimeout != null) {
				this.consoleAuthTimeout.Dispose();
				this.consoleAuthTimeout = null;
			}
		}

		#endregion authentising console

		internal void On_ReceievedLine(string line) {
			line = line.Trim();
			if (!string.IsNullOrEmpty(line)) {
				if (this.loggedIn) {
					foreach (ConsoleServer.ConsoleClient console in GameServersManager.AllConsolesIn(this)) {
						if (!console.filteredGameServers.Contains(this.ServerUid)) {
							console.WriteLine(this.ServerUid, line);
						}
					}
				}
			}
		}

		public override void SendCommand(ConsoleServer.ConsoleClient console, string cmd) {
			if (cmd.StartsWith("[")) {
				SphereCommands.HandleCommand(console, this, cmd.Substring(1));
			} else {
				ConsoleServer.ConsoleId consoleId = ConsoleServer.ConsoleId.FakeConsole;
				if (console != null) {
					consoleId = console.ConsoleId;
				}
				this.conn.SendCommand(cmd, new CallbackCommandResponse<ConsoleServer.ConsoleId>(
					this.OnCommandReply, consoleId));
			}
		}

		private void OnCommandReply(IList<string> lines, ConsoleServer.ConsoleId consoleId) {
			ConsoleServer.ConsoleClient console = ConsoleServer.ConsoleServer.GetClientByUid(consoleId);
			if (console != null) {
				foreach (string line in lines) {
					string l = line.Trim();
					if (!string.IsNullOrEmpty(l)) {
						console.WriteLine(this.ServerUid, l);
					}
				}
			}
		}

		internal void ExitLater(TimeSpan timeSpan) {
			throw new Exception("The method or operation is not implemented.");
		}
	}
}