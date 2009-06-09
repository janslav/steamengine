using System;
using System.Net;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using SteamEngine.Communication;
using SteamEngine.Communication.TCP;
using SteamEngine.Common;

namespace SteamEngine.AuxiliaryServer.ConsoleServer {
	public class ConsoleServerProtocol : IProtocol<TcpConnection<ConsoleClient>, ConsoleClient, IPEndPoint> {
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Member")]
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
		public static readonly ConsoleServerProtocol instance = new ConsoleServerProtocol();

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
		public IncomingPacket<TcpConnection<ConsoleClient>, ConsoleClient, IPEndPoint> GetPacketImplementation(byte id, TcpConnection<ConsoleClient> conn, ConsoleClient state, out bool discardAfterReading) {
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


	public abstract class ConsoleIncomingPacket : IncomingPacket<TcpConnection<ConsoleClient>, ConsoleClient, IPEndPoint> {

	}

	public class RequestLoginPacket : ConsoleIncomingPacket {
		private string accName;
		private string password;

		protected override ReadPacketResult Read() {
			this.accName = this.DecodeUTF8String();
			this.password = this.DecodeUTF8String();
			return ReadPacketResult.Success;
		}

		protected override void Handle(TcpConnection<ConsoleClient> conn, ConsoleClient state) {
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
				state.SendLoginFailedAndClose("The username '"+this.accName+"' either isn't cached in the AuxServer or the password is wrong.");
			}
		}
	}

	public class RequestServersToStartPacket : ConsoleIncomingPacket {
		protected override ReadPacketResult Read() {
			return ReadPacketResult.Success;
		}

		protected override void Handle(TcpConnection<ConsoleClient> conn, ConsoleClient state) {
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

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1806:DoNotIgnoreMethodResults", MessageId = "SteamEngine.AuxiliaryServer.AuxServNantProjectStarter")]
		protected override void Handle(TcpConnection<ConsoleClient> conn, ConsoleClient state) {
			GameServers.GameServerClient cli = GameServers.GameServerServer.GetInstanceByNumber(this.serverNum);
			if (cli != null) {
				state.WriteLine(0, "Server already online, ignoring start command.");
				//server online, we do nothing
			} else {
				GameServerInstanceSettings sett = Settings.KnownGameServersList[this.serverNum];
				Sanity.IfTrueThrow((this.serverNum != sett.Number), "Server setting number is different from it's index in list");
				string nantPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(sett.IniPath, NantLauncher.defaultPathInProject));
				Console.WriteLine("Compiling " + this.build + " build of server at " + sett.IniPath);

				new AuxServNantProjectStarter(this.build, nantPath, "buildCore", "gameCoreFileName");
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

		protected override void Handle(TcpConnection<ConsoleClient> conn, ConsoleClient state) {
			if (this.id == 0) {
				if (state.IsLoggedInAux) {
					Commands.HandleCommand(conn, state, this.command);
					return;
				}
			} else {
				GameServers.GameServerClient cli = GameServers.GameServerServer.GetInstanceByUid(this.id);
				if (cli != null) {
					if (LoggedInConsoles.IsLoggedIn(state, cli)) {
						GameServers.ConsoleCommandLinePacket p = Pool<GameServers.ConsoleCommandLinePacket>.Acquire();
						p.Prepare(state.Uid, state.AccountName, state.Password, this.command);
						cli.Conn.SendSinglePacket(p);
						return;
					}
				}
			}

			state.WriteLine(0, "Invalid (not implemented/not logged in) id to command: " + this.id);
		}
	}
}