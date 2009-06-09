using System;
using System.Collections.Generic;
using System.Text;

using SteamEngine.Communication;

namespace SteamEngine.AuxiliaryServer.ConsoleServer {

	public class RequestOpenCommandWindowPacket : OutgoingPacket {
		string name;
		int cmdWinUid;

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "name"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "cmdWinUid")]
		public void Prepare(string name, int cmdWinUid) {
			this.name = name;
			this.cmdWinUid = cmdWinUid;
		}

		public override byte Id {
			get { return 1; }
		}

		protected override void Write() {
			this.EncodeInt(this.cmdWinUid);
			this.EncodeUTF8String(this.name);
		}
	}

	public class RequestEnableCommandLinePacket : OutgoingPacket {
		int uid;

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "uid")]
		public void Prepare(int uid) {
			this.uid = uid;
		}

		public override byte Id {
			get { return 3; }
		}

		protected override void Write() {
			this.EncodeInt(this.uid);
		}
	}

	public class RequestCloseCommandWindowPacket : OutgoingPacket {
		int uid;

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "uid")]
		public void Prepare(int uid) {
			this.uid = uid;
		}

		public override byte Id {
			get { return 2; }
		}

		protected override void Write() {
			this.EncodeInt(this.uid);
		}
	}

	public class SendStringPacket : OutgoingPacket {
		string str;
		int uid;

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "str"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "uid")]
		public void Prepare(int uid, string str) {
			this.uid = uid;
			this.str = str;
		}

		public override byte Id {
			get { return 4; }
		}

		protected override void Write() {
			this.EncodeInt(this.uid);
			this.EncodeUTF8String(this.str);
		}
	}

	public class SendStringLinePacket : OutgoingPacket {
		string str;
		int uid;

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "uid"), 
		System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "str")]
		public void Prepare(int uid, string str) {
			this.uid = uid;
			this.str = str;
		}

		public override byte Id {
			get { return 5; }
		}

		protected override void Write() {
			this.EncodeInt(this.uid);
			this.EncodeUTF8String(this.str);
		}
	}

	public class SendServersToStartPacket : OutgoingPacket {
		private int count;
		private List<int> numbers = new List<int>();
		private List<string> iniPaths = new List<string>();
		private List<string> names = new List<string>();
		private List<ushort> ports = new List<ushort>();
		private List<bool> runnings = new List<bool>();

		public void Prepare(IList<GameServerInstanceSettings> servers, IList<int> runningServerNumbers) {
			this.numbers.Clear();
			this.iniPaths.Clear();
			this.names.Clear();
			this.ports.Clear();
			this.runnings.Clear();

			this.count = servers.Count;
			foreach (GameServerInstanceSettings gsis in servers) {
				this.numbers.Add(gsis.Number);
				this.iniPaths.Add(gsis.IniPath);
				this.names.Add(gsis.Name);
				this.ports.Add((ushort) gsis.Port);
				this.runnings.Add(runningServerNumbers.Contains(gsis.Number));
			}
		}

		public override byte Id {
			get { return 6; }
		}

		protected override void Write() {
			this.EncodeInt(this.count);
			for (int i = 0; i < this.count; i++) {
				this.EncodeInt(this.numbers[i]);
				this.EncodeUTF8String(this.iniPaths[i]);
				this.EncodeUTF8String(this.names[i]);
				this.EncodeUShort(this.ports[i]);
				this.EncodeBool(this.runnings[i]);
			}
		}
	}

	public class LoginFailedPacket : OutgoingPacket {
		string reason;

		public void Prepare(string reason) {
			this.reason = reason;
		}

		public override byte Id {
			get { return 7; }
		}

		protected override void Write() {
			this.EncodeUTF8String(this.reason);
		}
	}
}
