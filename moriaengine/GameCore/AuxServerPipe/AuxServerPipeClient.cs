using System;
using System.Threading;
using System.Net;
using System.Collections.Generic;
using System.Text;

using SteamEngine.Communication;
using SteamEngine.Communication.NamedPipes;
using SteamEngine.Common;

namespace SteamEngine.AuxServerPipe {
	public class AuxServerPipeClient : 
		IConnectionState<NamedPipeConnection<AuxServerPipeClient>, AuxServerPipeClient, string> {

		private static NamedPipeClientFactory<AuxServerPipeClient> clientFactory;
		private static AuxServerPipeClient connectedInstance;


		private NamedPipeConnection<AuxServerPipeClient> pipe;
		internal bool sendLogStrings = false;

		private StringToSend onConsoleWrite;
		private StringToSend onConsoleWriteLine;

		public AuxServerPipeClient() {
			this.onConsoleWrite = Logger_OnConsoleWrite;
			this.onConsoleWriteLine = Logger_OnConsoleWriteLine;
		}

		internal static void Init() {
			clientFactory = new NamedPipeClientFactory<AuxServerPipeClient>(
				AuxServerPipeProtocol.instance,
				MainClass.globalLock);

			StartTryingToConnect();

		}

		private static void StartTryingToConnect() {
			Thread connectingThread = new Thread(delegate() {
				while (true) {
					NamedPipeConnection<AuxServerPipeClient> c = 
						clientFactory.Connect(Common.Tools.commonPipeName);

					if (c != null) {
						break;
					}
					Thread.Sleep(1000);
				}
			});

			connectingThread.IsBackground = true;
			connectingThread.Name = "AuxServerPipeClient_Connecting";
			connectingThread.Start();
		}

		public void On_Init(NamedPipeConnection<AuxServerPipeClient> conn) {
			connectedInstance = this;
			this.pipe = conn;
			Logger.OnConsoleWrite += this.onConsoleWrite;
			Logger.OnConsoleWriteLine += this.onConsoleWriteLine;
		}

		private void Logger_OnConsoleWriteLine(string data) {
			SendLogString(data+Environment.NewLine);
		}

		private void Logger_OnConsoleWrite(string data) {
			SendLogString(data);
		}

		private void SendLogString(string data) {
			if (this.sendLogStrings && this.pipe.IsConnected) {
				LogStringPacket packet = Pool<LogStringPacket>.Acquire();
				packet.Prepare(data);
				pipe.SendSinglePacket(packet);
			}
		}

		public void On_Close(string reason) {
			connectedInstance = null;
			Logger.OnConsoleWrite -= this.onConsoleWrite;
			Logger.OnConsoleWriteLine -= this.onConsoleWriteLine;
			this.sendLogStrings = false;

			StartTryingToConnect();
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
	}


}