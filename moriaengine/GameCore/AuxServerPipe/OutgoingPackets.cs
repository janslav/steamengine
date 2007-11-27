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
				return 1; 
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
				return 2;
			}
		}

		protected override void Write() {
			this.EncodeUTF8String(this.str);
		}
	}

}
