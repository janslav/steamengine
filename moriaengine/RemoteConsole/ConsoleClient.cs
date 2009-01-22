using System;
using System.Net;
using System.Collections.Generic;
using System.Text;
using SteamEngine.Common;
using SteamEngine.Communication;
using SteamEngine.Communication.TCP;

namespace SteamEngine.RemoteConsole {
	public class ConsoleClient : Poolable, IConnectionState<TCPConnection<ConsoleClient>, ConsoleClient, IPEndPoint> {
		static TCPClientFactory<ConsoleClient> factory = new TCPClientFactory<ConsoleClient>(
			ConsoleProtocol.instance, MainClass.globalLock);


		private static ConsoleClient connectedInstance;
		private TCPConnection<ConsoleClient> conn;

		protected override void On_Reset() {
			this.conn = null;

			base.On_Reset();
		}

		public static ConsoleClient ConnectedInstance {
			get {
				return connectedInstance;
			}
		}

		public TCPConnection<ConsoleClient> Conn {
			get {
				return this.conn;
			}
		}

		public IEncryption Encryption {
			get {
				return null;
			}
		}

		public ICompression Compression {
			get {
				return null;
			}
		}

		public void On_Init(TCPConnection<ConsoleClient> conn) {
			connectedInstance = this;
			this.conn = conn;
			MainClass.mainForm.SetConnected(true);

			MainClass.mainForm.SystemDisplay.WriteLine("Connected to " + conn.EndPoint);
			//IdentifyGameServerPacket packet = Pool<IdentifyGameServerPacket>.Acquire();
			//packet.Prepare();
			//conn.SendSinglePacket(packet);
		}

		private delegate void NoParamDeleg();

		public void On_Close(string reason) {
			MainClass.mainForm.SystemDisplay.WriteLine("Disconnected from " + conn.EndPoint + ": " + reason);

			connectedInstance = null;
			MainClass.mainForm.Invoke(new NoParamDeleg(MainClass.mainForm.SetConnectedFalse));
			MainClass.mainForm.Invoke(new NoParamDeleg(MainClass.mainForm.ClearCmdLineDisplays));
		}

		public static void SendCommand(int id, string command) {
			if (connectedInstance != null) {
				CommandLinePacket p = Pool<CommandLinePacket>.Acquire();
				p.Prepare(id, command);
				connectedInstance.conn.SendSinglePacket(p);
			}
		}

		public static TCPConnection<ConsoleClient> Connect(EndPointSetting connectTo) {
			try {
				TCPConnection<ConsoleClient> conn = factory.Connect(new IPEndPoint(
					Dns.GetHostAddresses(connectTo.Address)[0], connectTo.Port));

				Settings.Save();
				return conn;
			} catch (Exception e) {
				Logger.WriteError(e);
			}

			return null;
		}

		public static void Disconnect(string reason) {
			if (connectedInstance != null) {
				connectedInstance.conn.Close(reason);
			}
		}
	}
}

