using System;
using System.Net;
using System.Collections.Generic;
using System.Text;

using SteamEngine.Communication;
using SteamEngine.Communication.NamedPipes;
using SteamEngine.Common;

namespace SteamEngine.AuxServerPipe {
	public class AuxServerPipeProtocol : IProtocol<NamedPipeConnection<AuxServerPipeClient>, AuxServerPipeClient, string> {
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Member")]
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
		public static readonly AuxServerPipeProtocol instance = new AuxServerPipeProtocol();


		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
		public IncomingPacket<NamedPipeConnection<AuxServerPipeClient>, AuxServerPipeClient, string> GetPacketImplementation(byte id, NamedPipeConnection<AuxServerPipeClient> conn, AuxServerPipeClient state, out bool discardAfterReading) {
			discardAfterReading = false;
			switch (id) {
				case 0x01:
					return Pool<RequestSendingLogStringsPacket>.Acquire();

				case 0x02:
					return Pool<RequestAccountLoginPacket>.Acquire();


				case 0x03:
					return Pool<ConsoleCommandLinePacket>.Acquire();
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
			//instead of replting right away, we use a timer. That way we ensure the reply is sent no sooner than when the server is started properly, 
			//i.e. including loading of account list.
			new AccountLoginDelayTimer(conn, this.consoleId, this.accName, this.password);
		}

		private class AccountLoginDelayTimer : Timers.Timer {
			int consoleId;
			string accName, password;
			NamedPipeConnection<AuxServerPipeClient> conn;

			public AccountLoginDelayTimer(NamedPipeConnection<AuxServerPipeClient> conn, int consoleId, string accName, string password) {
				this.conn = conn;
				this.consoleId = consoleId;
				this.accName = accName;
				this.password = password;

				this.DueInSeconds = 0;
			}

			protected override void OnTimeout() {
				if (this.conn.IsConnected) {
					AbstractAccount acc = AbstractAccount.HandleConsoleLoginAttempt(this.accName, this.password);

					ReplyAccountLoginPacket reply = Pool<ReplyAccountLoginPacket>.Acquire();
					reply.Prepare(this.consoleId, this.accName, acc != null);

					conn.SendSinglePacket(reply);
				}
				this.Delete();
			}
		}
	}

	public class ConsoleCommandLinePacket : AuxServerPipeIncomingPacket {
		int consoleId;
		string accName, password;
		private string command;

		protected override ReadPacketResult Read() {
			this.consoleId = this.DecodeInt();
			this.accName = this.DecodeUTF8String();
			this.password = this.DecodeUTF8String();
			this.command = this.DecodeUTF8String();
			return ReadPacketResult.Success;
		}

		protected override void Handle(NamedPipeConnection<AuxServerPipeClient> conn, AuxServerPipeClient state) {
			if (RunLevelManager.IsAwaitingRetry) {
				if (this.command == "exit") {//TODO? check authorisation somehow?
					MainClass.signalExit.Set();
				} else {
					MainClass.RetryRecompilingScripts();
				}
				return;
			}

			AbstractAccount acc = AbstractAccount.HandleConsoleLoginAttempt(this.accName, this.password);
			if (acc != null) {
				ConsoleDummy dummy = new ConsoleDummy(acc, this.consoleId);

				Commands.ConsoleCommand(dummy, this.command);
			} else {
				//ignore? I think so.
			}
		}
	}
}


