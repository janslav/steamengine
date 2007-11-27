using System;
using System.Net;
using System.Collections.Generic;
using System.Text;

using SteamEngine.Communication;
using SteamEngine.Communication.NamedPipes;
using SteamEngine.Common;

namespace SteamEngine.AuxServerPipe {
	public class AuxServerPipeProtocol : IProtocol<NamedPipeConnection<AuxServerPipeClient>, AuxServerPipeClient, string> {
		public static readonly AuxServerPipeProtocol instance = new AuxServerPipeProtocol();


		public IncomingPacket<NamedPipeConnection<AuxServerPipeClient>, AuxServerPipeClient, string> GetPacketImplementation(byte id) {
			switch (id) {
				case 0x03:
					return Pool<RequestSendingLogStringsPacket>.Acquire();

				//case 0x04:
				//    return Pool<CommandPacket>.Acquire();
			}

			return null;
		}
	}

	public abstract class AuxServerPipeIncomingPacket : IncomingPacket<NamedPipeConnection<AuxServerPipeClient>, AuxServerPipeClient, string> {

	}

	public class RequestSendingLogStringsPacket : AuxServerPipeIncomingPacket {
		byte sendLogStrings;

		protected override ReadPacketResult Read() {
			sendLogStrings = DecodeByte();
			return ReadPacketResult.Success;
		}

		protected override void Handle(NamedPipeConnection<AuxServerPipeClient> conn, AuxServerPipeClient state) {
			state.sendLogStrings = this.sendLogStrings != 0;
		}
	}

	//public class CommandPacketPacket : AuxServerPipeIncomingPacket {
	//    string cmd;

	//    protected override ReadPacketResult Read() {
	//        this.cmd = DecodeUTF8String();
	//        return ReadPacketResult.Success;
	//    }

	//    protected override void Handle(NamedPipeConnection<AuxServerPipeClient> conn, AuxServerPipeClient state) {
			
	//    }
	//}

	
}
