using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using SteamEngine.Common;
using SteamEngine.Networking;
using SteamEngine.Communication;
using SteamEngine.Communication.TCP;

namespace SteamEngine.Networking {
	public class ItemOnGroundUpdater : IDisposable {
		protected readonly AbstractItem item;
		private bool isInCache = false;

		private PacketGroup packetGroupNormal;
		private PacketGroup packetGroupMovable;

		private static CacheDictionary<AbstractItem, ItemOnGroundUpdater> cache =
			new CacheDictionary<AbstractItem, ItemOnGroundUpdater>(50000, true);//znate nekdo nejaky lepsi cislo? :)


		public ItemOnGroundUpdater(AbstractItem item) {
			this.item = item;
			cache[item] = this;
			this.isInCache = true;
		}

		public static ItemOnGroundUpdater GetFromCache(AbstractItem item) {
			ItemOnGroundUpdater iogu;
			cache.TryGetValue(item, out iogu);
			return iogu;
		}

		public static void RemoveFromCache(AbstractItem item) {
			cache.Remove(item);
		}

		public virtual void Dispose() {
			if (this.packetGroupNormal != null) {
				this.packetGroupNormal.Dispose();
			}
			if (this.packetGroupMovable != null) {
				this.packetGroupMovable.Dispose();
			}
			if (this.isInCache) {
				cache.Remove(this.item);
				this.isInCache = false;
			}
		}

		public void SendTo(AbstractCharacter viewer) {
			GameState state = viewer.GameState;
			if (state != null) {
				this.SendTo(viewer, state, state.Conn);
			}
		}

		public virtual void SendTo(AbstractCharacter viewer, GameState viewerState, TcpConnection<GameState> viewerConn) {
			if (viewer.IsPlevelAtLeast(Globals.PlevelOfGM)) {
				if (this.packetGroupMovable == null) {
					this.packetGroupMovable = PacketGroup.CreateFreePG();
					this.packetGroupMovable.AcquirePacket<ObjectInfoOutPacket>().Prepare(this.item, MoveRestriction.Movable);
				}
				viewerConn.SendPacketGroup(this.packetGroupMovable);
			} else {
				if (this.packetGroupNormal == null) {
					this.packetGroupNormal = PacketGroup.CreateFreePG();
					this.packetGroupNormal.AcquirePacket<ObjectInfoOutPacket>().Prepare(this.item, MoveRestriction.Normal);
				}
				viewerConn.SendPacketGroup(this.packetGroupNormal);
			}
		}

		public static void ClearCache() {
			cache.Clear();
		}
	}
}
