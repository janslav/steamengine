using System;
using System.Net;
using System.Collections.Generic;
using System.Text;

using SteamEngine.Communication;
using SteamEngine.Communication.NamedPipes;
using SteamEngine.Common;

namespace SteamEngine.AuxiliaryServer.GameServers {
	public class GameServerProtocol : IProtocol<NamedPipeConnection<GameServerClient>, GameServerClient, string> {
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Member")]
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
		public static readonly GameServerProtocol instance = new GameServerProtocol();


		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
		public IncomingPacket<NamedPipeConnection<GameServerClient>, GameServerClient, string> GetPacketImplementation(byte id, NamedPipeConnection<GameServerClient> conn, GameServerClient state, out bool discardAfterReading) {
			discardAfterReading = false;
			switch (id) {
				case 0x01:
					return Pool<IdentifyGameServerPacket>.Acquire();

				case 0x02:
					return Pool<LogStringPacket>.Acquire();

				case 0x03:
					return Pool<ConsoleLoginReplyPacket>.Acquire();

				case 0x04:
					return Pool<StartupFinishedPacket>.Acquire();

				case 0x05:
					return Pool<ConsoleWriteLinePacket>.Acquire();
			}

			return null;
		}
	}

	public abstract class GameServerIncomingPacket : IncomingPacket<NamedPipeConnection<GameServerClient>, GameServerClient, string> {

	}

	public class IdentifyGameServerPacket : GameServerIncomingPacket {
		string steamengineIniPath;

		protected override ReadPacketResult Read() {
			this.steamengineIniPath = this.DecodeUTF8String();

			return ReadPacketResult.Success;
		}

		protected override void Handle(NamedPipeConnection<GameServerClient> conn, GameServerClient state) {
			Console.WriteLine(state + " identified (steamengine.ini at '" + this.steamengineIniPath + "')");
			GameServerInstanceSettings game = Settings.RememberGameServer(this.steamengineIniPath);
			state.SetIdentificationData(game);

			if (ConsoleServer.ConsoleServer.AllConsolesCount > 0) {
				foreach (ConsoleServer.ConsoleClient console in ConsoleServer.ConsoleServer.AllConsoles) {
					//console.TryLoginToGameServer(this);
					console.OpenCmdWindow(state.Name, state.Uid);
					console.TryLoginToGameServer(state);
				}
				state.RequestSendingLogStr(true);
			}
		}
	}

	public class LogStringPacket : GameServerIncomingPacket {
		string str;

		protected override ReadPacketResult Read() {
			str = this.DecodeUTF8String();
			return ReadPacketResult.Success;
		}

		protected override void Handle(NamedPipeConnection<GameServerClient> conn, GameServerClient state) {
			//Console.WriteLine(state+": "+str);
			state.WriteToMyConsoles(this.str);
		}
	}

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
	internal class ConsoleLoginReplyPacket : GameServerIncomingPacket {
		int consoleId;
		string accName;
		bool loginSuccessful;

		protected override ReadPacketResult Read() {
			this.consoleId = this.DecodeInt();
			this.accName = this.DecodeUTF8String();
			this.loginSuccessful = this.DecodeBool();
			return ReadPacketResult.Success;
		}

		protected override void Handle(NamedPipeConnection<GameServerClient> conn, GameServerClient state) {
			ConsoleServer.ConsoleClient console = ConsoleServer.ConsoleServer.GetClientByUid(this.consoleId);
			if (console != null) {
				if (this.loginSuccessful) {
					console.SetLoggedInTo(state);
				} else {
					console.CloseCmdWindow(state.Uid);

					ICollection<GameServerClient> serversLoggedIn = LoggedInConsoles.AllServersWhereLoggedIn(console);
					if (serversLoggedIn.Count == 0) {
						Settings.ForgetUser(console.AccountName);
						console.SendLoginFailedAndClose("GameServer '" + state.Name + "' rejected username '" + this.accName + "' and/or it's password.");
					}
				}
			}
		}
	}


	public class StartupFinishedPacket : GameServerIncomingPacket {
		protected override ReadPacketResult Read() {
			return ReadPacketResult.Success;
		}

		protected override void Handle(NamedPipeConnection<GameServerClient> conn, GameServerClient state) {
			state.SetStartupFinished(true);
		}
	}

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
	internal class ConsoleWriteLinePacket : GameServerIncomingPacket {
		int consoleId;
		string line;

		protected override ReadPacketResult Read() {
			this.consoleId = this.DecodeInt();
			this.line = this.DecodeUTF8String();
			return ReadPacketResult.Success;
		}

		protected override void Handle(NamedPipeConnection<GameServerClient> conn, GameServerClient state) {
			ConsoleServer.ConsoleClient console = ConsoleServer.ConsoleServer.GetClientByUid(this.consoleId);
			if (console != null) {
				console.WriteLine(state.Uid, this.line);
			}
		}
	}
}
