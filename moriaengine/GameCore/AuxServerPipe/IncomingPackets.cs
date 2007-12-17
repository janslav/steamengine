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
				case 0x01:
					return Pool<RequestSendingLogStringsPacket>.Acquire();

				case 0x02:
					return Pool<RequestAccountLoginPacket>.Acquire();

			}

			return null;
		}
	}

	public abstract class AuxServerPipeIncomingPacket : IncomingPacket<NamedPipeConnection<AuxServerPipeClient>, AuxServerPipeClient, string> {

	}

	public class RequestSendingLogStringsPacket : AuxServerPipeIncomingPacket {
		byte sendLogStrings;

		protected override ReadPacketResult Read() {
			this.sendLogStrings = DecodeByte();
			return ReadPacketResult.Success;
		}

		protected override void Handle(NamedPipeConnection<AuxServerPipeClient> conn, AuxServerPipeClient state) {
			state.sendLogStrings = this.sendLogStrings != 0;
		}
	}

	public class RequestAccountLoginPacket : AuxServerPipeIncomingPacket {
		int consoleId;
		string accName, password;

		protected override ReadPacketResult Read() {
			this.consoleId = this.DecodeInt();
			this.accName = this.DecodeUTF8String();
			this.password = this.DecodeUTF8String();
			return ReadPacketResult.Success;
		}

		protected override void Handle(NamedPipeConnection<AuxServerPipeClient> conn, AuxServerPipeClient state) {
			AbstractAccount acc = AbstractAccount.HandleConsoleLoginAttempt(this.accName, this.password);

			AccountLoginPacket reply = Pool<AccountLoginPacket>.Acquire();
			reply.Prepare(this.consoleId, this.accName, acc != null);

			conn.SendSinglePacket(reply);
		}
	}
}
