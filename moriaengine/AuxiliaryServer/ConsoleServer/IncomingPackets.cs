using System;
using System.Net;
using System.Collections.Generic;
using System.Text;
using System.Threading;

using NAnt.Core;

using SteamEngine.Communication;
using SteamEngine.Communication.TCP;
using SteamEngine.Common;

namespace SteamEngine.AuxiliaryServer.ConsoleServer {
	public class ConsoleServerProtocol : IProtocol<TCPConnection<ConsoleClient>, ConsoleClient, IPEndPoint> {
		public static readonly ConsoleServerProtocol instance = new ConsoleServerProtocol();

		public IncomingPacket<TCPConnection<ConsoleClient>, ConsoleClient, IPEndPoint> GetPacketImplementation(byte id, TCPConnection<ConsoleClient> conn, ConsoleClient state, out bool discardAfterReading) {
			discardAfterReading = false;

			switch (id) {
				case 0:
					return Pool<RequestLoginPacket>.Acquire();
				case 1:
					return Pool<RequestServersToStartPacket>.Acquire();
				case 2:
					return Pool<RequestStartGameServer>.Acquire();
				case 3:
					return Pool<CommandLinePacket>.Acquire();
			}
			return null;
		}
	}


	public abstract class ConsoleIncomingPacket : IncomingPacket<TCPConnection<ConsoleClient>, ConsoleClient, IPEndPoint> {

	}

	public class RequestLoginPacket : ConsoleIncomingPacket {
		private string accName;
		private string password;

		protected override ReadPacketResult Read() {
			this.accName = this.DecodeUTF8String();
			this.password = this.DecodeUTF8String();
			return ReadPacketResult.Success;
		}

		protected override void Handle(TCPConnection<ConsoleClient> conn, ConsoleClient state) {
			state.SetLoginData(this.accName, this.password);

			bool failed = false;
			if (Settings.CheckUser(this.accName, this.password)) {
				state.SetLoggedInToAux(true);
			} else {
				failed = true;
			}

			if (GameServers.GameServerServer.GameServersCount > 0) {
				foreach (GameServers.GameServerClient gameServer in GameServers.GameServerServer.AllGameServers) {
					state.TryLoginToGameServer(gameServer);
				}
			} else if (failed) {
				conn.Close("Failed to identify as " + this.accName);
			}
		}
	}

	public class RequestServersToStartPacket : ConsoleIncomingPacket {
		protected override ReadPacketResult Read() {
			return ReadPacketResult.Success;
		}

		protected override void Handle(TCPConnection<ConsoleClient> conn, ConsoleClient state) {
			List<int> runningServerNumbers = new List<int>();
			foreach (GameServers.GameServerClient gsc in GameServers.GameServerServer.AllGameServers) {
				runningServerNumbers.Add(gsc.Setting.Number);
			}

			SendServersToStartPacket packet = Pool<SendServersToStartPacket>.Acquire();
			packet.Prepare(Settings.KnownGameServersList, runningServerNumbers);
			conn.SendSinglePacket(packet);
		}
	}

	public class RequestStartGameServer : ConsoleIncomingPacket {
		private byte serverNum;
		private SEBuild build;

		protected override ReadPacketResult Read() {
			this.serverNum = this.DecodeByte();
			this.build = (SEBuild) this.DecodeByte();

			return ReadPacketResult.Success;
		}

		protected override void Handle(TCPConnection<ConsoleClient> conn, ConsoleClient state) {
			GameServers.GameServerClient cli = GameServers.GameServerServer.GetInstanceByNumber(this.serverNum);
			if (cli != null) {
				state.WriteLine(0, "Server already online, ignoring start command.");
				//server online, we do nothing
			} else {
				GameServerInstanceSettings sett = Settings.KnownGameServersList[this.serverNum];
				Sanity.IfTrueThrow((this.serverNum != sett.Number), "Server setting number is different from it's index in list");
				string nantPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(sett.IniPath, NantLauncher.defaultPathInProject));
				Console.WriteLine("Compiling " + this.build + " build of server at " + sett.IniPath);

				AuxServNantLogger o = new AuxServNantLogger(this.serverNum, this.build, nantPath);
				Thread t = new Thread(o.StartThread);
				t.Start();
			}
		}

		//Nant logger class and a helper threading class combined
		internal class AuxServNantLogger : DefaultLogger {
			private byte serverNum;
			private SEBuild build;
			private string nantPath;

			internal AuxServNantLogger(byte serverNum, SEBuild build, string nantPath) {
				this.serverNum = serverNum;
				this.build = build;
				this.nantPath = nantPath;
			}

			public override void BuildFinished(object sender, BuildEventArgs e) { }
			public override void BuildStarted(object sender, BuildEventArgs e) { }
			public override void TargetFinished(object sender, BuildEventArgs e) { }
			public override void TargetStarted(object sender, BuildEventArgs e) { }
			public override void TaskFinished(object sender, BuildEventArgs e) { }
			public override void TaskStarted(object sender, BuildEventArgs e) { }

			protected override void Log(string pMessage) {
				object o = NantLauncher.GetDecoratedLogMessage(pMessage);
				if (o != null) {
					Logger.StaticWriteLine(o);
				}
				//Console.WriteLine(pMessage);
			}

			internal void StartThread() {
				NantLauncher nant = new NantLauncher(this.nantPath);
				nant.SetLogger(this);
				nant.SetPropertiesAsSelf();
				nant.SetDebugMode(this.build == SEBuild.Debug);
				nant.SetOptimizeMode(this.build == SEBuild.Optimised);

				nant.SetTarget("buildCore");
				nant.Execute();

				string file = nant.GetCompiledAssemblyName("gameCoreFileName");

				Console.WriteLine("Starting " + file);
				System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo(file);
				psi.WindowStyle = System.Diagnostics.ProcessWindowStyle.Minimized;
				System.Diagnostics.Process.Start(psi);
			}
		}
	}

	public class CommandLinePacket : ConsoleIncomingPacket {
		private int id;
		private string command;

		protected override ReadPacketResult Read() {
			this.id = this.DecodeInt();
			this.command = this.DecodeUTF8String();

			return ReadPacketResult.Success;
		}

		protected override void Handle(TCPConnection<ConsoleClient> conn, ConsoleClient state) {
			if (this.id == 0) {
				if (state.IsLoggedInAux) {
					//TODO
				}
			} else {
				GameServers.GameServerClient cli = GameServers.GameServerServer.GetInstanceByUid(this.id);
				if (cli != null) {
					if (LoggedInConsoles.IsLoggedIn(state, cli)) {
						GameServers.ConsoleCommandLinePacket p = Pool<GameServers.ConsoleCommandLinePacket>.Acquire();
						p.Prepare(state.Uid, state.AccountName, state.Password, this.command);
						cli.Conn.SendSinglePacket(p);
					}
				}
			}

			state.WriteLine(0, "Invalid (not implemented/not logged in) id to command: " + this.id);
		}
	}
}