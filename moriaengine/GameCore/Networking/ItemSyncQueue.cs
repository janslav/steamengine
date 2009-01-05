/*
	This program is free software; you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation; either version 2 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program; if not, write to the Free Software
	Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
	Or visit http://www.gnu.org/copyleft/gpl.html
*/

using System;
using System.Collections.Generic;
using System.Text;
using SteamEngine.Communication;
using SteamEngine.Communication.TCP;
using SteamEngine.Common;
using System.IO;
using System.Net;
using SteamEngine.Regions;

namespace SteamEngine.Networking {
	public sealed class ItemSyncQueue : SyncQueue {
		internal static ItemSyncQueue instance = new ItemSyncQueue();

		private SimpleQueue<AbstractItem> queue = new SimpleQueue<AbstractItem>();

		[Summary("Call when a thing is about to be created/changed")]
		public static void Resend(AbstractItem item) {
			if (enabled) {
				Logger.WriteInfo(Globals.netSyncingTracingOn, "Resend(" + item + ") called");
				instance.SetFlagsOnItem(item, SyncFlags.Resend);
			}
		}

		[Summary("Call when an item is about to be changed")]
		public static void AboutToChange(AbstractItem item) {
			ItemOnGroundUpdater.RemoveFromCache(item);
			if (enabled) {
				Logger.WriteInfo(Globals.netSyncingTracingOn, "ItemAboutToChange(" + item + ") called");
				instance.SetFlagsOnItem(item, SyncFlags.ItemUpdate);
			}
		}

		[Summary("Call when an item is about to be changed")]
		public static void PropertiesChanged(AbstractItem item) {
			if (enabled) {
				Logger.WriteInfo(Globals.netSyncingTracingOn, "ItemPropertiesChanged(" + item + ") called");
				instance.SetFlagsOnItem(item, SyncFlags.Property);
			}
		}

		private ItemSyncQueue() {
		}

		protected override void ProcessQueue() {
			while (this.queue.Count > 0) {
				AbstractItem item = this.queue.Dequeue();
				if ((item != null) && (!item.IsDeleted)) {
					SyncFlags syncFlags = item.syncFlags;
					item.syncFlags = SyncFlags.None;

					if ((syncFlags & (SyncFlags.Resend | SyncFlags.ItemUpdate)) != SyncFlags.None) { //no difference between update and resend. Maybe one day we will discover something :)
						this.UpdateItemAndProperties(item);
					} else if (Globals.aosToolTips) {//only new properties
						this.SendItemPropertiesOnly(item);
					}
				}
			}
		}

		private void SetFlagsOnItem(AbstractItem item, SyncFlags flags) {
			Sanity.IfTrueThrow(flags == SyncFlags.None, "flags == SyncFlags.None");

			if (item.syncFlags == SyncFlags.None) {
				this.queue.Enqueue(item);
				this.autoResetEvent.Set();
			}
			item.syncFlags |= flags;
		}

		[Flags]
		internal enum SyncFlags : byte {
			None = 0x00,
			Resend = 0x01,	//complete update - after creation, or on demand
			ItemUpdate = 0x02,
			//update properties
			Property = 0x04
		}

		private void SendItemPropertiesOnly(AbstractItem item) {
			Logger.WriteInfo(Globals.netSyncingTracingOn, "ProcessItemProperties " + item);
			IEnumerable<AbstractCharacter> enumerator;
			AbstractItem contAsItem = item.Cont as AbstractItem;
			if (contAsItem != null) {
				enumerator = OpenedContainers.GetViewers(contAsItem);
			} else {
				Thing top = item.TopObj();
				enumerator = top.GetMap().GetPlayersInRange(top.X, top.Y, Globals.MaxUpdateRange);
			}

			AOSToolTips toolTips = null;
			foreach (AbstractCharacter player in enumerator) {
				GameState state = player.GameState;
				if (state != null) {
					TCPConnection<GameState> conn = state.Conn;
					if (state.Version.aosToolTips) {
						if (toolTips == null) {
							toolTips = item.GetAOSToolTips();
							if (toolTips == null) {
								break;
							}
						}
						toolTips.SendIdPacket(state, conn);
					}
				}
			}
		}

		//oldMapPoint can be null if checkPreviousVisibility is false
		private void UpdateItemAndProperties(AbstractItem item) {
			Logger.WriteInfo(Globals.netSyncingTracingOn, "ProcessItem " + item);

			bool propertiesExist = true;
			bool isOnGround = item.IsOnGround;
			bool isEquippedAndVisible = false;
			bool isInContainer = false;
			if (!isOnGround) {
				isEquippedAndVisible = item.IsEquipped;
				if (isEquippedAndVisible) {
					if (item.Z >= AbstractCharacter.sentLayers) {
						isEquippedAndVisible = false;
					}
				} else {
					isInContainer = item.IsInContainer;
				}
			}

			if (isOnGround || isEquippedAndVisible || isInContainer) {
				PacketGroup pg = null;//iteminfo or paperdollinfo or itemincontainer
				PacketGroup allmoveItemInfo = null;
				AOSToolTips toolTips = null;

				IEnumerable<AbstractCharacter> enumerator;
				AbstractItem contAsItem = item.Cont as AbstractItem;
				if (contAsItem != null) {
					enumerator = OpenedContainers.GetViewers(contAsItem);
					//checkPreviousVisibility = false;
				} else {
					Thing newMapPoint = item.TopObj();
					Map newMap = newMapPoint.GetMap();
					enumerator = newMap.GetPlayersInRange(newMapPoint.X, newMapPoint.Y, Globals.MaxUpdateRange);
				}

				foreach (AbstractCharacter viewer in enumerator) {
					GameState state = viewer.GameState;
					if (state != null) {
						TCPConnection<GameState> conn = state.Conn;

						if (viewer.CanSeeForUpdate(item)) {
							if (isOnGround) {
								if (viewer.IsPlevelAtLeast(Globals.plevelOfGM)) {
									if (allmoveItemInfo == null) {
										allmoveItemInfo = PacketGroup.AcquireMultiUsePG();
										allmoveItemInfo.AcquirePacket<ObjectInfoOutPacket>().Prepare(item, MoveRestriction.Movable); //0x1a
									}
									conn.SendPacketGroup(allmoveItemInfo);
								} else {
									if (pg == null) {
										pg = PacketGroup.AcquireMultiUsePG();
										pg.AcquirePacket<ObjectInfoOutPacket>().Prepare(item, MoveRestriction.Normal); //0x1a
									}
									conn.SendPacketGroup(pg);
								}
							} else if (isEquippedAndVisible) {
								if (pg == null) {
									pg = PacketGroup.AcquireMultiUsePG();
									pg.AcquirePacket<WornItemOutPacket>().PrepareItem(item.Cont.FlaggedUid, item);//0x2e
								}
								conn.SendPacketGroup(pg);
							} else { //isInContainer
								if (pg == null) {
									pg = PacketGroup.AcquireMultiUsePG();
									pg.AcquirePacket<AddItemToContainerOutPacket>().Prepare(item.Cont.FlaggedUid, item);//0x25
								}
								conn.SendPacketGroup(pg);
							}

							if (propertiesExist) {
								if (Globals.aosToolTips && state.Version.aosToolTips) {
									if (toolTips == null) {
										toolTips = item.GetAOSToolTips();
										if (toolTips == null) {
											propertiesExist = false;
											continue;
										}
									}
									toolTips.SendIdPacket(state, conn);
								}
							}
						}
					}
				}

				if (pg != null) {
					pg.Dispose();
				}
				if (allmoveItemInfo != null) {
					allmoveItemInfo.Dispose();
				}
			}
		}
	}
}