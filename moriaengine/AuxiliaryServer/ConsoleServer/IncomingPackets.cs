using System;
using System.Net;
using System.Collections.Generic;
using System.Text;

using SteamEngine.Communication;
using SteamEngine.Communication.TCP;
using SteamEngine.Common;

namespace SteamEngine.AuxiliaryServer.ConsoleServer {
	public class ConsoleServerProtocol : IProtocol<TCPConnection<ConsoleClient>, ConsoleClient, IPEndPoint> {
		public static readonly ConsoleServerProtocol instance = new ConsoleServerProtocol();



		public IncomingPacket<TCPConnection<ConsoleClient>, ConsoleClient, IPEndPoint> GetPacketImplementation(byte id) {
			switch (id) {
				case 0:
					return Pool<LoginRequestPacket>.Acquire();
			}
			return null;
		}
	}


	public abstract class ConsoleIncomingPacket : IncomingPacket<TCPConnection<ConsoleClient>, ConsoleClient, IPEndPoint> {

	}

	public class LoginRequestPacket : ConsoleIncomingPacket {
		private string accName;
		private string password;

		protected override ReadPacketResult Read() {
			this.accName = this.DecodeUTF8String();
			this.password = this.DecodeUTF8String();
			return ReadPacketResult.Success;
		}

		protected override void Handle(TCPConnection<ConsoleClient> conn, ConsoleClient state) {
			state.SetLoginData(this.accName, this.password);

			if (GameServers.GameServerServer.GameServersCount > 0) {

				foreach (GameServers.GameServerClient gameServer in GameServers.GameServerServer.AllGameServers) {
					state.TryLoginToGameServer(gameServer);
				}
			} else {
				if (Settings.CheckUser(this.accName, this.password)) {
					state.SetLoggedInToAux(true);
				} else {
					conn.Close("Failed to identify as " + this.accName);
				}
			}
		}
	}
}
