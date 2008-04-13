using System;
using System.Net;
using System.Collections.Generic;
using System.Text;

using SteamEngine.Communication;
using SteamEngine.Communication.NamedPipes;
using SteamEngine.Common;

namespace SteamEngine.AuxiliaryServer.GameServers {
	public class GameServerProtocol : IProtocol<NamedPipeConnection<GameServerClient>, GameServerClient, string> {
		public static readonly GameServerProtocol instance = new GameServerProtocol();


		public IncomingPacket<NamedPipeConnection<GameServerClient>, GameServerClient, string> GetPacketImplementation(byte id) {
			switch (id) {
				case 0x01:
					return Pool<IdentifyGameServerPacket>.Acquire();

				case 0x02:
					return Pool<LogStringPacket>.Acquire();

				case 0x03:
					return Pool<ConsoleLoginReplyPacket>.Acquire();

				case 0x04:
					return Pool<StartupFinishedPacket>.Acquire();
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
					Console.WriteLine(state + " identified as " + this.accName + " with " + state.Name);

					console.SetLoggedInTo(state);
				} else {
					console.CloseCmdWindow(state.Uid);

					ICollection<GameServerClient> serversLoggedIn = LoggedInConsoles.AllServersWhereLoggedIn(console);
					if (serversLoggedIn.Count == 0) {
						Settings.ForgetUser(console.AccountName);

						string msg = "Failed to identify as " + this.accName;
						console.WriteStringLine(0, msg);
						console.Conn.Close(msg);
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
}
