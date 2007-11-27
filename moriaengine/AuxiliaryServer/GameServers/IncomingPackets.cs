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
				case 0x05:
					return Pool<GameServerLoginPacket>.Acquire();
			}

			return null;
		}
	}

	public abstract class GameServerIncomingPacket : IncomingPacket<NamedPipeConnection<GameServerClient>, GameServerClient, string> {

	}

	public class GameServerLoginPacket : GameServerIncomingPacket {
		string message;

		protected override ReadPacketResult Read() {
			byte len = this.DecodeByte();
			this.message = this.DecodeAsciiString(len);

			return ReadPacketResult.Success;
		}

		protected override void Handle(NamedPipeConnection<GameServerClient> conn, GameServerClient state) {
			Console.WriteLine(state+" says:"+this.message);
		}
	}
}
