using System;
using System.Net;
using System.Collections.Generic;
using System.Text;
using SteamEngine.Common;
using SteamEngine.Communication;
using SteamEngine.Communication.TCP;

namespace SteamEngine.RemoteConsole {

	public class ConsoleProtocol : IProtocol<TcpConnection<ConsoleClient>, ConsoleClient, IPEndPoint> {
		public static readonly ConsoleProtocol instance = new ConsoleProtocol();


		public IncomingPacket<TcpConnection<ConsoleClient>, ConsoleClient, IPEndPoint> GetPacketImplementation(byte id, TcpConnection<ConsoleClient> conn, ConsoleClient state, out bool discardAfterReading) {
			discardAfterReading = false;
			switch (id) {
				case 1:
					return Pool<RequestOpenGameServerWindowPacket>.Acquire();
				case 2:
					return Pool<RequestCloseGameServerWindowPacket>.Acquire();
				case 3:
					return Pool<RequestEnableCommandLinePacket>.Acquire();
				case 4:
					return Pool<WriteStringPacket>.Acquire();
				case 5:
					return Pool<WriteLinePacket>.Acquire();
				case 6:
					return Pool<SendServersToStartPacket>.Acquire();
			}

			return null;
		}
	}


	public abstract class ConsoleIncomingPacket : IncomingPacket<TcpConnection<ConsoleClient>, ConsoleClient, IPEndPoint> {

	}

	public class RequestOpenGameServerWindowPacket : ConsoleIncomingPacket {
		int uid;
		string name;

		private delegate void IntAndStrDeleg(int i, string s);
		private static IntAndStrDeleg deleg = AddCmdLineDisplay;

		protected override void Handle(TcpConnection<ConsoleClient> conn, ConsoleClient state) {
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
		private static IntDeleg deleg = CloseGameServerWindow;

		protected override void Handle(TcpConnection<ConsoleClient> conn, ConsoleClient state) {
			MainClass.mainForm.Invoke(deleg, this.uid);
		}

		private static void CloseGameServerWindow(int uid) {
			MainClass.mainForm.RemoveCmdLineDisplay(uid);
		}

		protected override ReadPacketResult Read() {
			this.uid = this.DecodeInt();
			return ReadPacketResult.Success;
		}
	}

	public class RequestEnableCommandLinePacket : ConsoleIncomingPacket {
		int uid;

		private static Action<int> deleg = EnableCommandLine;

		protected override void Handle(TcpConnection<ConsoleClient> conn, ConsoleClient state) {
			MainClass.mainForm.Invoke(deleg, this.uid);
		}

		private static void EnableCommandLine(int uid) {
			MainClass.mainForm.EnableCommandLineOnDisplay(uid);
		}

		protected override ReadPacketResult Read() {
			this.uid = this.DecodeInt();
			return ReadPacketResult.Success;
		}
	}

	public class WriteStringPacket : ConsoleIncomingPacket {
		int uid;
		string str;

		private delegate void IntAndStrDeleg(int i, string s);
		private static IntAndStrDeleg deleg = Write;

		protected override void Handle(TcpConnection<ConsoleClient> conn, ConsoleClient state) {
			MainClass.mainForm.Invoke(deleg, this.uid, this.str);
		}

		private static void Write(int uid, string str) {
			MainClass.mainForm.Write(uid, str);
		}

		protected override ReadPacketResult Read() {
			this.uid = this.DecodeInt();
			this.str = this.DecodeUTF8String();
			return ReadPacketResult.Success;
		}
	}

	public class WriteLinePacket : ConsoleIncomingPacket {
		int uid;
		string str;

		private delegate void IntAndStrDeleg(int i, string s);
		private static IntAndStrDeleg deleg = WriteLine;

		protected override void Handle(TcpConnection<ConsoleClient> conn, ConsoleClient state) {
			MainClass.mainForm.Invoke(deleg, this.uid, this.str);
		}

		private static void WriteLine(int uid, string str) {
			MainClass.mainForm.WriteLine(uid, str);
		}

		protected override ReadPacketResult Read() {
			this.uid = this.DecodeInt();
			this.str = this.DecodeUTF8String();
			return ReadPacketResult.Success;
		}
	}

	public class SendServersToStartPacket : ConsoleIncomingPacket {
		GameServerEntry[] entries;

		private delegate void EntriesArrayDeleg(GameServerEntry[] entries);
		private static EntriesArrayDeleg deleg = OpenStartGameForm;

		protected override void Handle(TcpConnection<ConsoleClient> conn, ConsoleClient state) {
			MainClass.mainForm.Invoke(deleg, new object[] { this.entries });
		}

		private static void OpenStartGameForm(GameServerEntry[] entries) {
			new StartGameForm(entries).ShowDialog();
		}

		protected override ReadPacketResult Read() {
			int count = this.DecodeInt();
			this.entries = new GameServerEntry[count];

			for (int i = 0; i < count; i++) {
				int number = this.DecodeInt();
				string iniPath = this.DecodeUTF8String();
				string name = this.DecodeUTF8String();
				ushort port = this.DecodeUShort();
				bool running = this.DecodeBool();

				this.entries[i] = new GameServerEntry(number, iniPath, name, port, running);
			}

			return ReadPacketResult.Success;
		}

		public class GameServerEntry {
			private readonly int number;
			private readonly string iniPath;
			private readonly string name;
			private readonly ushort port;
			private readonly bool running;

			internal GameServerEntry(int number, string iniPath, string name, ushort port, bool running) {
				this.iniPath = iniPath;
				this.number = number;
				this.name = name;
				this.port = port;
				this.running = running;
			}

			public string DisplayText {
				get {
					if (this.running) {
						return this.name + " (on)";
					} else {
						return this.name + " (off)";
					}
				}
			}

			public int Number {
				get {
					return this.number;
				}
			}

			public string IniPath {
				get {
					return this.iniPath;
				}
			}

			public string Name {
				get {
					return this.name;
				}
			}

			public ushort Port {
				get {
					return this.port;
				}
			}

			public bool Running {
				get {
					return this.running;
				}
			}
		}
	}
}