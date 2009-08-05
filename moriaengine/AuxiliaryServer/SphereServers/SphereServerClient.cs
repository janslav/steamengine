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
		private readonly SphereServerConnection conn;
		private SphereServerSetup setup;

		private bool loggedIn;

		static GameUID uids = GameUID.LastSphereServer;

		internal SphereServerClient(SphereServerConnection sphereServerConnection, SphereServerSetup setup) {
			this.uid = uids--;
			this.conn = sphereServerConnection;
			this.setup = setup;

			GameServersManager.AddGameServer(this);

			Console.WriteLine(this + " connected.");

			this.conn.BeginSend(" ");
			this.conn.EnqueueParser(new CallbackParser<object>(usernameRE, this.EnterUsername, null));
		}

		internal void On_Close(string reason) {
			GameServersManager.RemoveGameServer(this);

			Console.WriteLine(this + " closed: " + reason);

			SphereServerClientFactory.Connect(this.setup); //start connecting again
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

		public override string ToString() {
			return "SphereServerClient '" + this.setup.RamdiscIniPath + "'";
		}

		#region loginSequence
		static Regex usernameRE = new Regex(@"Username\?:", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);
		static Regex passwordRE = new Regex(@"Password\?:", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);
		static Regex loggedinRE = new Regex(@"(Login '(?<username>.+?)')|(?<badPass>Bad password for this account\.)", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);
		
		private bool EnterUsername(Match m, object state) {
			this.conn.BeginSend(this.setup.AdminAccount);
			this.conn.EnqueueParser(new CallbackParser<object>(passwordRE, this.EnterPassword, null));
			return true;
		}

		private bool EnterPassword(Match m, object state) {
			this.conn.BeginSend(this.setup.AdminPassword);
			this.conn.EnqueueParser(new CallbackParser<object>(loggedinRE, this.OnLoggedIn, null));
			return true;
		}

		private bool OnLoggedIn(Match m, object state) {
			if (m.Groups["badPass"].Value.Length == 0) {
				if (m.Groups["username"].Value.Equals(this.setup.AdminAccount, StringComparison.OrdinalIgnoreCase)) {
					Console.WriteLine(this + " logged in.");
					this.loggedIn = true;

					foreach (ConsoleServer.ConsoleClient console in ConsoleServer.ConsoleServer.AllConsoles) {
						console.OpenCmdWindow(this.Setup.Name, this.serverUid);
						console.TryLoginToGameServer(this);
					}

					return true;
				} else { //someone else? wtf
					return false;
				}
			} else {
				this.conn.Close("Bad admin password set in steamaux.ini");
				return true;
			}
		}

		public bool IsLoggedIn {
			get {
				return this.loggedIn;
			}
		}


		#endregion loginSequence

		#region authentising console
		public override void SendConsoleLogin(ConsoleServer.ConsoleId consoleId, string accName, string accPassword) {
			ConsoleCredentials state = new ConsoleCredentials(consoleId, accName, accPassword);

			if (accPassword.Length == 0) {
				this.DenyConsole(state, " - password must be > 0 characters");
				return;
			}

			this.conn.BeginSend("show findaccount(" + accName + ").password");
			this.conn.EnqueueParser(new CallbackParser<ConsoleCredentials>(passwordQueryRE, 
				this.OnAccountQueryResponse, state));
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

		private bool OnAccountQueryResponse(Match m, ConsoleCredentials state) {
			if (m.Groups["username"].Value.Equals(state.accName, StringComparison.OrdinalIgnoreCase)) {
				if (!m.Groups["password"].Value.Equals(state.accPassword)) {
					DenyConsole(state, "and/or it's password.");
				} else {
					this.conn.BeginSend("show findaccount(" + state.accName + ").priv");
					this.conn.EnqueueParser(new CallbackParser<ConsoleCredentials>(privQueryRE,
						this.OnPrivQueryResponse, state));
				}
				return true;
			} else {
				return false;
			}
		}

		static Regex privQueryRE = new Regex(
			@"findaccount\((?<username>.+?)\).priv' for '(.+?)' is '(?<priv>.*?)'",
			RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);

		private bool OnPrivQueryResponse(Match m, ConsoleCredentials state) {
			if (m.Groups["username"].Value.Equals(state.accName, StringComparison.OrdinalIgnoreCase)) {
				int priv = ConvertTools.ParseInt32(m.Groups["priv"].Value.Trim());
				if ((priv & 0x0200) == 0x0200) {
					DenyConsole(state, "- the acc is blocked.");
				} else {
					this.conn.BeginSend("show findaccount(" + state.accName + ").plevel");
					this.conn.EnqueueParser(new CallbackParser<ConsoleCredentials>(plevelQueryRE,
						this.OnPlevelQueryResponse, state));
				}
				return true;
			} else {
				return false;
			}
		}

		static Regex plevelQueryRE = new Regex(
			@"findaccount\((?<username>.+?)\).plevel' for '(.+?)' is '(?<plevel>.*?)'",
			RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);

		private bool OnPlevelQueryResponse(Match m, ConsoleCredentials state) {
			if (m.Groups["username"].Value.Equals(state.accName, StringComparison.OrdinalIgnoreCase)) {
				int plevel = ConvertTools.ParseInt32(m.Groups["plevel"].Value.Trim());
				if (plevel < 4) {
					DenyConsole(state, "- low plevel.");
				} else {
					ConsoleServer.ConsoleClient console = ConsoleServer.ConsoleServer.GetClientByUid(state.consoleUid);
					if (console != null) {
						console.SetLoggedInTo(this);
					}
					this.SendCommand(state.consoleUid, null, null, "i");
				}
				return true;
			} else {
				return false;
			}
		}

		private void DenyConsole(ConsoleCredentials state, string reason) {
			ConsoleServer.ConsoleClient console = ConsoleServer.ConsoleServer.GetClientByUid(state.consoleUid);
			if (console != null) {
				console.CloseCmdWindow(this.serverUid);

				ICollection<GameServer> serversLoggedIn = GameServersManager.AllServersWhereLoggedIn(console);
				if (serversLoggedIn.Count == 0) {
					Settings.ForgetUser(console.AccountName);
					console.SendLoginFailedAndClose("GameServer '" + this.Setup.Name + "' rejected username '" + state.accName + "' " + reason);
				}
			}
		}
		#endregion authentising console

		internal void ExitLater(TimeSpan timeSpan) {
			throw new Exception("The method or operation is not implemented.");
		}

		internal void On_ReceievedLine(string line) {
			if (this.loggedIn) {
				foreach (ConsoleServer.ConsoleClient console in GameServersManager.AllConsolesIn(this)) {
					console.WriteLine(this.serverUid, line);
				}
			}
		}

		public override void SendCommand(ConsoleServer.ConsoleId consoleId, string accName, string accPassword, string cmd) {
			this.conn.BeginSend(cmd);
			this.conn.EnqueueParser(new CallbackParser<ConsoleServer.ConsoleId>(commandReplyRE,
						this.OnCommandReply, consoleId));
		}

		static Regex commandReplyRE = new Regex(
			@"(?<unwanted>[\d][\d]:[\d][\d]:[0-9a-f]{3})?(?<reply>.*)",
			RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);

		private bool OnCommandReply(Match m, ConsoleServer.ConsoleId consoleId) {
			if (string.IsNullOrEmpty(m.Groups["unwanted"].Value)) {

				ConsoleServer.ConsoleClient console = ConsoleServer.ConsoleServer.GetClientByUid(consoleId);
				if (console != null) {
					console.WriteLine(this.serverUid, m.Groups["reply"].Value);
				}
					
				return true;
			} else {
				return false;
			}
		}
	}
}
