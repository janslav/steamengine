using System;
using System.Net;
using System.Collections.Generic;
using System.Text;

using SteamEngine.Communication;
using SteamEngine.Communication.TCP;

namespace SteamEngine.RemoteConsole {

	public class ConsoleProtocol : IProtocol<TCPConnection<ConsoleClient>, ConsoleClient, IPEndPoint>  {
		public static readonly ConsoleProtocol instance = new ConsoleProtocol();

		public IncomingPacket<TCPConnection<ConsoleClient>, ConsoleClient, IPEndPoint> GetPacketImplementation(byte id) {
			switch (id) {
				case 1:
					return Pool<RequestOpenGameServerWindowPacket>.Acquire();
				case 2:
					return Pool<RequestCloseGameServerWindowPacket>.Acquire();
			}

			return null;
		}
	}


	public abstract class ConsoleIncomingPacket : IncomingPacket<TCPConnection<ConsoleClient>, ConsoleClient, IPEndPoint> {

	}

	public class RequestOpenGameServerWindowPacket : ConsoleIncomingPacket {
		int uid;
		string name;

		private delegate void IntAndStrDeleg(int i, string s);
		private static IntAndStrDeleg deleg = AddCmdLineDisplay;

		protected override void Handle(TCPConnection<ConsoleClient> conn, ConsoleClient state) {
			MainClass.mainForm.Invoke(deleg, this.uid, this.name);
		}

		private static void AddCmdLineDisplay(int uid, string name) {
			MainClass.mainForm.AddCmdLineDisplay(uid, name);
		}

		protected override ReadPacketResult Read() {
			this.uid = this.DecodeInt();
			this.name = this.DecodeUTF8String();
			return ReadPacketResult.Success;
		}
	}

	public class RequestCloseGameServerWindowPacket : ConsoleIncomingPacket {
		int uid;

		private delegate void IntDeleg(int i);
		private static IntDeleg deleg = RemoveCmdLineDisplay;

		protected override void Handle(TCPConnection<ConsoleClient> conn, ConsoleClient state) {
			MainClass.mainForm.Invoke(deleg, this.uid);
		}

		private static void RemoveCmdLineDisplay(int uid) {
			MainClass.mainForm.RemoveCmdLineDisplay(uid);
		}

		protected override ReadPacketResult Read() {
			this.uid = this.DecodeInt();
			return ReadPacketResult.Success;
		}
	}
}