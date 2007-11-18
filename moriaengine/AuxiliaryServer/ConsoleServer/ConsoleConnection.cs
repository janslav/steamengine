using System;
using System.Collections.Generic;
using System.Text;

using SteamEngine.Network;
using SteamEngine.Common;

namespace SteamEngine.AuxiliaryServer.ConsoleServer {
	public class ConsoleConnection : SteamSocket {

		//public override void Handle(IncomingPacket<ConsoleConnection> packet) {
		//    ConsoleServerPacketGroup pg = Pool<ConsoleServerPacketGroup>.Acquire();

		//    pg.AddPacket(Pool<ConsoleServerOutgoingPacket>.Acquire());

		//    //MainClass.server.SendPacketGroup(this, pg);
		//}
	}
}
