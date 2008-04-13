using System;
using System.Net;
using System.Collections.Generic;
using System.Text;

using NAnt.Core;

using SteamEngine.Communication;
using SteamEngine.Communication.TCP;
using SteamEngine.Common;

namespace SteamEngine.AuxiliaryServer.ConsoleServer {
	public class ConsoleServerProtocol : IProtocol<TCPConnection<ConsoleClient>, ConsoleClient, IPEndPoint> {
		public static readonly ConsoleServerProtocol instance = new ConsoleServerProtocol();



		public IncomingPacket<TCPConnection<ConsoleClient>, ConsoleClient, IPEndPoint> GetPacketImplementation(byte id) {
			switch (id) {
				case 0:
					return Pool<RequestLoginPacket>.Acquire();
				case 1:
					return Pool<RequestServersToStartPacket>.Acquire();
				case 2:
					return Pool<RequestStartGameServer>.Acquire();
			}
			return null;
		}
	}


	public abstract class ConsoleIncomingPacket : IncomingPacket<TCPConnection<ConsoleClient>, ConsoleClient, IPEndPoint> {

	}

	public class RequestLoginPacket : ConsoleIncomingPacket {
		private string accName;
		private string password;

		protected override ReadPacketResult Read() {
			this.accName = this.DecodeUTF8String();
			this.password = this.DecodeUTF8String();
			return ReadPacketResult.Success;
		}

		protected override void Handle(TCPConnection<ConsoleClient> conn, ConsoleClient state) {
			state.SetLoginData(this.accName, this.password);

			if (GameServers.GameServerServer.GameServersCount > 0) {

				foreach (GameServers.GameServerClient gameServer in GameServers.GameServerServer.AllGameServers) {
					state.TryLoginToGameServer(gameServer);
				}
			} else {
				if (Settings.CheckUser(this.accName, this.password)) {
					state.SetLoggedInToAux(true);
				} else {
					conn.Close("Failed to identify as " + this.accName);
				}
			}
		}
	}

	public class RequestServersToStartPacket : ConsoleIncomingPacket {
		protected override ReadPacketResult Read() {
			return ReadPacketResult.Success;
		}

		protected override void Handle(TCPConnection<ConsoleClient> conn, ConsoleClient state) {
			List<int> runningServerNumbers = new List<int>();
			foreach (GameServers.GameServerClient gsc in GameServers.GameServerServer.AllGameServers) {
				runningServerNumbers.Add(gsc.Setting.Number);
			}

			SendServersToStartPacket packet = Pool<SendServersToStartPacket>.Acquire();
			packet.Prepare(Settings.KnownGameServersList, runningServerNumbers);
			conn.SendSinglePacket(packet);
		}
	}

	public class RequestStartGameServer : ConsoleIncomingPacket {
		private byte serverNum;
		private SEBuild build;

		protected override ReadPacketResult Read() {
			this.serverNum = this.DecodeByte();
			this.build = (SEBuild) this.DecodeByte();

			return ReadPacketResult.Success;
		}

		protected override void Handle(TCPConnection<ConsoleClient> conn, ConsoleClient state) {
			GameServers.GameServerClient cli = GameServers.GameServerServer.GetInstanceByNumber(this.serverNum);
			if (cli != null) {
				//server online, we do nothing
			} else {
				GameServerInstanceSettings sett = Settings.KnownGameServersList[this.serverNum];
				Sanity.IfTrueThrow((this.serverNum != sett.Number), "Server setting number is different from it's index in list");

				//string binPath = System.IO.Path.Combine(sett.IniPath, "bin");

				string nantPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(sett.IniPath, NantLauncher.defaultPathInProject));

				NantLauncher nant = new NantLauncher(nantPath);
				nant.SetLogger(new AuxServNantLogger());
				nant.SetPropertiesAsSelf();
				nant.SetDebugMode(this.build == SEBuild.Debug);
				nant.SetOptimizeMode(this.build == SEBuild.Optimised);

				nant.SetTarget("buildCore");
				nant.Execute();

				string file = nant.GetCompiledAssemblyName("gameCoreFileName");

				System.Diagnostics.Process.Start(file);
			}
		}

		internal class AuxServNantLogger : DefaultLogger {
			public override void BuildFinished(object sender, BuildEventArgs e) { }
			public override void BuildStarted(object sender, BuildEventArgs e) { }
			public override void TargetFinished(object sender, BuildEventArgs e) { }
			public override void TargetStarted(object sender, BuildEventArgs e) { }
			public override void TaskFinished(object sender, BuildEventArgs e) { }
			public override void TaskStarted(object sender, BuildEventArgs e) { }

			protected override void Log(string pMessage) {
				object o = NantLauncher.GetDecoratedLogMessage(pMessage);
				if (o != null) {
					Logger.StaticWriteLine(o);
				}
				//Console.WriteLine(pMessage);
			}
		}
	}
}
