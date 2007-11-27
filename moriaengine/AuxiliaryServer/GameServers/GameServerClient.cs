using System;
using System.Net;
using System.Collections.Generic;
using System.Text;

using SteamEngine.Communication;
using SteamEngine.Communication.NamedPipes;
using SteamEngine.Common;

namespace SteamEngine.AuxiliaryServer.GameServers {
	public class GameServerClient : Poolable,
		IConnectionState<NamedPipeConnection<GameServerClient>, GameServerClient, string> {

		static int uids;

		int uid;

		protected override void On_Reset() {
			uid = uids++;

			base.On_Reset();
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

		public void On_Init(NamedPipeConnection<GameServerClient> conn) {
			Console.WriteLine(this + " connected.");
		}

		public void On_Close(string reason) {
			Console.WriteLine(this + " closed: "+reason);
		}

		public override string ToString() {
			return "GameServerClient "+uid;
		}
	}
}
