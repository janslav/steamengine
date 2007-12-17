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
			}

			return null;
		}
	}

	public abstract class GameServerIncomingPacket : IncomingPacket<NamedPipeConnection<GameServerClient>, GameServerClient, string> {

	}

	public class IdentifyGameServerPacket : GameServerIncomingPacket {
		ushort port;
		string serverName;
		string executablePath;

		protected override ReadPacketResult Read() {
			this.port = this.DecodeUShort();
			this.serverName = this.DecodeUTF8String();
			this.executablePath = this.DecodeUTF8String();

			return ReadPacketResult.Success;
		}

		protected override void Handle(NamedPipeConnection<GameServerClient> conn, GameServerClient state) {
			Console.WriteLine(state+" identified as '"+this.serverName+"', port "+this.port+
				", executable '"+this.executablePath+"'");

			state.SetIdentificationData(this.port, this.serverName, this.executablePath);
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
					Console.WriteLine(console + " identified as " + this.accName);

					ConsoleServer.RequestOpenGameServerWindowPacket packet = Pool<ConsoleServer.RequestOpenGameServerWindowPacket>.Acquire();
					packet.Prepare(state.Name, state.Uid);
					console.Conn.SendSinglePacket(packet);

					LoggedInConsoles.AddPair(console, state);

				} else {
					console.Conn.Close("Failed to identify as " + this.accName);
				}
			}
		}
	}
}
