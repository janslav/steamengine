using System;
using System.Collections.Generic;
using System.Text;

using SteamEngine.Communication;
using SteamEngine.Common;

namespace SteamEngine.AuxiliaryServer.GameServers {
	public class RequestSendingLogStringsPacket : OutgoingPacket {
		bool sendLogStrings;

		public void Prepare(bool sendLogStrings) {
			this.sendLogStrings = sendLogStrings;
		}

		public override byte Id {
			get {
				return 0x01;
			}
		}

		protected override void Write() {
			EncodeBool(this.sendLogStrings);
		}
	}

	public class ConsoleLoginRequestPacket : OutgoingPacket {
		int consoleId;
		string accName, password;

		public void Prepare(int consoleId, string accName, string password) {
			this.consoleId = consoleId;
			this.accName = accName;
			this.password = password;
		}

		public override byte Id {
			get {
				return 0x02;
			}
		}

		protected override void Write() {
			EncodeInt(this.consoleId);
			EncodeUTF8String(this.accName);
			EncodeUTF8String(this.password);
		}
	}

	public class ConsoleCommandLinePacket : OutgoingPacket {
		int consoleId;
		string accName, password;
		private string command;

		public void Prepare(int consoleId, string accName, string password, string command) {
			this.consoleId = consoleId;
			this.accName = accName;
			this.password = password;
			this.command = command;
		}

		public override byte Id {
			get { return 3; }
		}

		protected override void Write() {
			this.EncodeInt(this.consoleId);
			this.EncodeUTF8String(this.accName);
			this.EncodeUTF8String(this.password);
			this.EncodeUTF8String(this.command);
		}
	}
}
