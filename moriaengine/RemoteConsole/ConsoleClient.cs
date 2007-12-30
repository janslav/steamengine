using System;
using System.Net;
using System.Collections.Generic;
using System.Text;

using SteamEngine.Communication;
using SteamEngine.Communication.TCP;

namespace SteamEngine.RemoteConsole {
	public class ConsoleClient : IConnectionState<TCPConnection<ConsoleClient>, ConsoleClient, IPEndPoint> {
		static TCPClientFactory<ConsoleClient> factory = new TCPClientFactory<ConsoleClient>(
			ConsoleProtocol.instance, MainClass.globalLock);


		private static ConsoleClient connectedInstance;

		public ConsoleClient ConnectedInstance {
			get {
				return connectedInstance;
			}
		}

		private TCPConnection<ConsoleClient> conn;


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
			MainClass.mainForm.SystemDisplay.WriteLine("Disconnected from " + conn.EndPoint);

			connectedInstance = null;
			MainClass.mainForm.SetConnected(false);


			MainClass.mainForm.Invoke(new NoParamDeleg(MainClass.mainForm.ClearCmdLineDisplays));
		}

		public static void SendCommand(int id, string command) {

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


	//    protected override void Handle(IncomingPacket p) {
	//        ConsoleIncomingPacket packet = (ConsoleIncomingPacket) p;
	//        packet.Handle();
	//    }

	//    protected override IncomingPacket GetPacketImplementation(byte id) {
	//        return Pool<ConsoleIncomingPacket>.Acquire();
	//    }

	//    protected override void On_Close(SteamEngine.Common.LogStr reason) {
	//        base.On_Close(reason);
	//    }

	//    protected override void On_Close(string reason) {
	//        base.On_Close(reason);
	//    }
	//}

}

