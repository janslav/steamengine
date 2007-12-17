using System;
using System.Net;
using System.Collections.Generic;
using System.Text;

using SteamEngine.Communication;
using SteamEngine.Communication.NamedPipes;
using SteamEngine.Common;

namespace SteamEngine.AuxServerPipe {

	internal class IdentifyGameServerPacket : OutgoingPacket {
		ushort port;
		string serverName;
		string executablePath;

		public void Prepare() {
			this.port = Globals.port;
			this.serverName = Globals.serverName;
			this.executablePath = System.IO.Path.GetFullPath(CompiledScripts.ClassManager.CoreAssembly.Location);
		}

		public override byte Id {
			get {
				return 0x01; 
			}
		}

		protected override void Write() {
			this.EncodeUShort(this.port);
			this.EncodeUTF8String(this.serverName);
			this.EncodeUTF8String(this.executablePath);
		}
	}


	internal class LogStringPacket : OutgoingPacket {
		string str;

		public void Prepare(string str) {
			this.str = str;
		}

		public override byte Id {
			get {
				return 0x02;
			}
		}

		protected override void Write() {
			this.EncodeUTF8String(this.str);
		}
	}

	internal class AccountLoginPacket : OutgoingPacket {
		int consoleId;
		string accName;
		bool loginSuccessful;

		public void Prepare(int consoleId, string accName, bool loginSuccessful) {
			this.consoleId = consoleId;
			this.accName = accName;
			this.loginSuccessful = loginSuccessful;
		}

		public override byte Id {
			get {
				return 0x03;
			}
		}

		protected override void Write() {
			this.EncodeInt(this.consoleId);
			this.EncodeUTF8String(this.accName);
			this.EncodeBool(this.loginSuccessful);
		}
	}
}
