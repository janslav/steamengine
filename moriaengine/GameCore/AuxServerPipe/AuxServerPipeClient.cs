using System;
using System.Threading;
using System.Net;
using System.Collections.Generic;
using System.Text;

using SteamEngine.Communication;
using SteamEngine.Communication.NamedPipes;
using SteamEngine.Common;

namespace SteamEngine.AuxServerPipe {
	public class AuxServerPipeClient : Poolable,
		IConnectionState<NamedPipeConnection<AuxServerPipeClient>, AuxServerPipeClient, string> {

		private static NamedPipeClientFactory<AuxServerPipeClient> clientFactory;
		private static AuxServerPipeClient connectedInstance;


		private NamedPipeConnection<AuxServerPipeClient> pipe;
		internal bool sendLogStrings = false;

		private StringToSendEventHandler onConsoleWrite;
		private StringToSendEventHandler onConsoleWriteLine;

		public AuxServerPipeClient() {
			this.onConsoleWrite = Logger_OnConsoleWrite;
			this.onConsoleWriteLine = Logger_OnConsoleWriteLine;
		}

		protected override void On_Reset() {
			this.pipe = null;
			this.sendLogStrings = false;

			base.On_Reset();
		}

		internal static void Init() {
			clientFactory = new NamedPipeClientFactory<AuxServerPipeClient>(
				AuxServerPipeProtocol.instance,
				MainClass.globalLock);

			connectingTimer.Change(TimeSpan.Zero, TimeSpan.Zero);

		}

		public static AuxServerPipeClient ConnectedInstance {
			get {
				return connectedInstance;
			}
		}

		static Timer connectingTimer = new Timer(new TimerCallback(delegate(object ignored) {
			NamedPipeConnection<AuxServerPipeClient> c =
				clientFactory.Connect(Common.Tools.commonPipeName);

			if (c == null) {
				StartTryingToConnect();
			}
		}));

		private static void StartTryingToConnect() {
			connectingTimer.Change(TimeSpan.FromSeconds(1), TimeSpan.Zero);
		}

		public void On_Init(NamedPipeConnection<AuxServerPipeClient> conn) {
			connectedInstance = this;
			this.pipe = conn;
			Logger.OnConsoleWrite += this.onConsoleWrite;
			Logger.OnConsoleWriteLine += this.onConsoleWriteLine;

			IdentifyGameServerPacket packet = Pool<IdentifyGameServerPacket>.Acquire();
			packet.Prepare();
			conn.SendSinglePacket(packet);

			new AnnounceStartupFinishedTimer(conn);
		}

		private class AnnounceStartupFinishedTimer : Timers.Timer {
			NamedPipeConnection<AuxServerPipeClient> conn;

			public AnnounceStartupFinishedTimer(NamedPipeConnection<AuxServerPipeClient> conn) {
				this.conn = conn;
				this.DueInSeconds = 0;
			}

			protected override void OnTimeout() {
				if (this.conn.IsConnected) {
					this.conn.SendPacketGroup(StartupFinishedPacket.group);
				}
				this.Delete();
			}
		}

		private void Logger_OnConsoleWriteLine(string data) {
			SendLogString(data + Environment.NewLine);
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

		public NamedPipeConnection<AuxServerPipeClient> PipeConnection {
			get {
				return this.pipe;
			}
		}
	}


}