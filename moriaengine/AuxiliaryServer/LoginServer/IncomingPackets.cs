using System;
using System.Net;
using System.Collections.Generic;
using System.Text;

using SteamEngine.Network;
using SteamEngine.Common;

namespace SteamEngine.AuxiliaryServer.LoginServer {
	public static class PacketHandlers {

		internal static IncomingPacket<LoginConnection> GetPacketImplementation(byte id) {
			switch (id) {
				case 0x80:
					return Pool<GameLoginPacket>.Acquire();

				case 0xa4:
					return Pool<GameSpyPacket>.Acquire();

				case 0xa0:
					return Pool<ServerSelectPacket>.Acquire();
			}

			return null;
		}

		public class GameSpyPacket : IncomingPacket<LoginConnection> {
			protected override ReadPacketResult Read() {
				SeekFromCurrent(148);
				return ReadPacketResult.DiscardSingle;
			}

			public override void Handle(LoginConnection packet) {
				throw new Exception("The method or operation is not implemented.");
			}
		}

		public class GameLoginPacket : IncomingPacket<LoginConnection> {
			string accname;

			protected override ReadPacketResult Read() {
				accname = this.DecodeAsciiString(30);
				this.SeekFromCurrent(31);
				return ReadPacketResult.Success;
			}

			public override void Handle(LoginConnection conn) {
				Console.WriteLine(conn+" identified as "+this.accname);

				ServersListPacket serverList = Pool<ServersListPacket>.Acquire();
				byte[] remoteAddress = conn.EndPoint.Address.GetAddressBytes();
				if (GameLoginPacket.ByteArraysEquals(remoteAddress, Settings.lanIP)) {
					serverList.Prepare(Settings.lanIP);
				} else {
					serverList.Prepare(Settings.wanIP);
				}

				conn.SendSinglePacket(serverList);
			}

			internal static bool ByteArraysEquals(byte[] a, byte[] b) {
				int len = a.Length;
				if (len != b.Length) {
					return false;
				}
				for (int i = 0; i<len; i++) {
					if (a[i] != b[i]) {
						return false;
					}
				}
				return true;
			}
		}

		public class ServerSelectPacket : IncomingPacket<LoginConnection> {
			int chosenServer;

			protected override ReadPacketResult Read() {
				chosenServer = DecodeUShort();
				return ReadPacketResult.Success;
			}

			public override void Handle(LoginConnection conn) {
				LoginToServerPacket packet = Pool<LoginToServerPacket>.Acquire();
				byte[] remoteAddress = conn.EndPoint.Address.GetAddressBytes();
				if (GameLoginPacket.ByteArraysEquals(remoteAddress, Settings.lanIP)) {
					packet.Prepare(Settings.lanIP, Settings.loginSettings[this.chosenServer].port);
				} else {
					packet.Prepare(Settings.wanIP, Settings.loginSettings[this.chosenServer].port);
				}

				//packet.Prepare(new byte[] { 89, 185, 242, 165 }, 2593); //moria 

				conn.SendSinglePacket(packet);
			}
		}
	}
}
