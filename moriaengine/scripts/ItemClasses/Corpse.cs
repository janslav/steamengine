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
using SteamEngine.Common;
using SteamEngine.Networking;
using SteamEngine.Communication;
using SteamEngine.Communication.TCP;

namespace SteamEngine.CompiledScripts {
	[Dialogs.ViewableClass]
	public partial class CorpseDef : ContainerDef {

	}

	[Dialogs.ViewableClass]
	public partial class Corpse : Container {
		uint hairFakeUid = Thing.GetFakeItemUid();
		uint beardFakeUid = Thing.GetFakeItemUid();

		bool hasEquippedItems = true;

		PacketGroup hairItemsPackets;
		bool hasHairItems = true;

		CharacterDef charDef;

		public override void GetNameCliloc(out int id, out string argument) {
			if (this.ownerName != null) {
				id = 1070702; //a corpse of ~1_CORPSENAME~
				argument = this.ownerName;
			} else {
				ItemDispidInfo idi = this.TypeDef.DispidInfo;
				string name = this.Name;
				if (idi != null) {
					if (string.Compare(name, idi.singularName, true) == 0) {
						argument = null;
						id = (1020000 + (this.Model & 16383));
						return;
					}
				}
				id = 1042971;
				argument = name;
			}
		}

		public override void On_Click(AbstractCharacter clicker, GameState clickerState, TCPConnection<GameState> clickerConn) {
			//TODO notoriety hue stuff
			PacketSequences.SendNameFrom(clickerConn, this, this.Name, 0);
		}

		public override void On_AosClick(AbstractCharacter clicker, GameState clickerState, TCPConnection<GameState> clickerConn) {
			//TODO notoriety hue stuff
			AOSToolTips toolTips = this.GetAOSToolTips();
			PacketSequences.SendClilocNameFrom(clickerConn, this,
				toolTips.FirstId, 0, toolTips.FirstArgument);
		}

		public void InitFromChar(Character dieingChar) {
			this.charDef = dieingChar.Def as CharacterDef;
			this.Amount = dieingChar.Model;
			this.Direction = dieingChar.Direction;
			this.Color = dieingChar.Color;
			string name = dieingChar.Name;
			this.Name = string.Concat("a corpse of ", name);
			if (dieingChar.IsPlayer) {
				this.owner = dieingChar;
				this.ownerName = name;
			}
			Item hair = dieingChar.Hair;
			if (hair != null) {
				hairModel = hair.ShortModel;
				hairColor = hair.ShortColor;
			}

			Item beard = dieingChar.Beard;
			if (beard != null) {
				beardModel = beard.ShortModel;
				beardColor = beard.ShortColor;
			}
			AbstractItem pack = dieingChar.BackpackAsContainer;
			foreach (Item inBackpack in pack) {
				if (inBackpack.CanFallToCorpse) {
					inBackpack.Cont = this;
				}
			}
			foreach (Item equipped in dieingChar.GetVisibleEquip()) {
				if (equipped.CanFallToCorpse && equipped != pack && equipped != hair && equipped != beard) {
					equipped.Cont = this;
					if (equippedItems == null) {
						equippedItems = new Dictionary<ICorpseEquipInfo, AbstractItem>();
					}
					equippedItems[equipped] = equipped;
				}
			}
		}

		public void ReturnStuffToChar(Character resurrectedChar) {
			if (equippedItems != null) {
				foreach (Item i in equippedItems.Values) {
					i.Cont = resurrectedChar;
					//resurrectedChar.TryEquip(resurrectedChar, i);
				}
			}
			AbstractItem backpack = resurrectedChar.BackpackAsContainer;
			foreach (Item i in this) {
				i.Cont = backpack; //TODO pickup/dropon backpack triggers...?
			}
			this.Delete(); //TODO anim?
		}

		public override Direction Direction {
			get {
				return direction;
			}
			set {
				SteamEngine.Networking.ItemSyncQueue.AboutToChange(this);
				direction = value;
			}
		}

		public override void On_Destroy() {
			base.On_Destroy();
			Thing.DisposeFakeUid(hairFakeUid);
			Thing.DisposeFakeUid(beardFakeUid);

			if (this.hairItemsPackets != null) {
				this.hairItemsPackets.Dispose();
				this.hairItemsPackets = null;
			}
		}

		internal class CorpseEquipInfo : ICorpseEquipInfo {
			uint flaggedUid;
			byte layer;
			int color;
			int model;

			internal CorpseEquipInfo(uint flaggedUid, byte layer, int color, int model) {
				this.flaggedUid = flaggedUid;
				this.layer = layer;
				this.color = color;
				this.model = model;
			}

			public uint FlaggedUid {
				get { return flaggedUid; }
			}

			public byte Layer {
				get { return layer; }
			}

			public int Color {
				get { return color; }
			}

			public int Model {
				get { return model; }
			}
		}

		public override ItemOnGroundUpdater GetOnGroundUpdater() {
			ItemOnGroundUpdater iogu = ItemOnGroundUpdater.GetFromCache(this);
			if (iogu == null) {
				if (this.hasEquippedItems) {
					iogu = new CorpseOnGroundUpdater(this);
				} else {
					iogu = new ItemOnGroundUpdater(this);
				}
			}
			return iogu;
		}

		public class CorpseOnGroundUpdater : ItemOnGroundUpdater {
			PacketGroup equippedItemsPackets;

			public CorpseOnGroundUpdater(Corpse corpse)
				: base(corpse) {
			}

			public override void SendTo(AbstractCharacter viewer, GameState viewerState, SteamEngine.Communication.TCP.TCPConnection<GameState> viewerConn) {
				base.SendTo(viewer, viewerState, viewerConn);

				Corpse corpse = (Corpse) this.item;
				if (corpse.hasEquippedItems) {
					if (this.equippedItemsPackets == null) {
						CorpseEquipInfo hair = null;
						CorpseEquipInfo beard = null;

						if (corpse.equippedItems != null) {
							List<AbstractItem> toRemove = null;
							foreach (AbstractItem item in corpse.equippedItems.Values) {
								if (item.Cont != corpse) {
									if (toRemove == null) {
										toRemove = new List<AbstractItem>();
									}
									toRemove.Add(item);
								}
							}
							if (toRemove != null) {
								foreach (AbstractItem noMoreEquipped in toRemove) {
									corpse.equippedItems.Remove(noMoreEquipped);
								}
							}
						}

						if (corpse.hairModel != 0) {
							hair = new CorpseEquipInfo(
								corpse.hairFakeUid, (byte) LayerNames.Hair, corpse.hairColor, corpse.hairModel);
							if (corpse.equippedItems == null) {
								corpse.equippedItems = new Dictionary<ICorpseEquipInfo, AbstractItem>();
							}
							corpse.equippedItems[hair] = null;
						}
						if (corpse.beardModel != 0) {
							beard = new CorpseEquipInfo(
								corpse.beardFakeUid, (byte) LayerNames.Beard, corpse.beardColor, corpse.beardModel);
							if (corpse.equippedItems == null) {
								corpse.equippedItems = new Dictionary<ICorpseEquipInfo, AbstractItem>();
							}
							corpse.equippedItems[beard] = null;
						}

						if ((corpse.equippedItems != null) && (corpse.equippedItems.Count > 0)) {
							this.equippedItemsPackets = PacketGroup.CreateFreePG();
							this.equippedItemsPackets.AcquirePacket<CorpseClothingOutPacket>().Prepare(corpse.FlaggedUid, corpse.equippedItems.Keys);//0x89
							this.equippedItemsPackets.AcquirePacket<ItemsInContainerOutPacket>().PrepareCorpse(corpse.FlaggedUid, corpse.equippedItems.Keys);//0x3c

							if (hair != null) {
								corpse.equippedItems.Remove(hair);
							}
							if (beard != null) {
								corpse.equippedItems.Remove(beard);
							}

						} else {
							corpse.equippedItems = null;
							corpse.hasEquippedItems = false;
						}
					}

					viewerConn.SendPacketGroup(this.equippedItemsPackets);
				}
			}

			public override void Dispose() {
				base.Dispose();

				if (this.equippedItemsPackets != null) {
					this.equippedItemsPackets.Dispose();
					this.equippedItemsPackets = null;
				}
			}
		}

		public override void On_ContainerOpen(AbstractCharacter viewer, GameState viewerState, SteamEngine.Communication.TCP.TCPConnection<GameState> viewerConn) {
			base.On_ContainerOpen(viewer, viewerState, viewerConn);

			if (this.IsOnGround && this.hasHairItems) {
				if (this.hairItemsPackets == null) {
					this.hairItemsPackets = PacketGroup.CreateFreePG();
					if (hairModel != 0) {
						CorpseEquipInfo hair = new CorpseEquipInfo(
							hairFakeUid, (byte) LayerNames.Hair, hairColor, hairModel);
						this.hairItemsPackets.AcquirePacket<AddItemToContainerOutPacket>().PrepareItemInCorpse(this.FlaggedUid, hair);
						hasHairItems = true;
					}
					if (beardModel != 0) {
						CorpseEquipInfo beard = new CorpseEquipInfo(
							beardFakeUid, (byte) LayerNames.Beard, beardColor, beardModel);

						this.hairItemsPackets.AcquirePacket<AddItemToContainerOutPacket>().PrepareItemInCorpse(this.FlaggedUid, beard);
						hasHairItems = true;
					}

					if (!this.hasHairItems) {
						this.hairItemsPackets.Dispose();
						this.hairItemsPackets = null;
						return;
					}
				}
				viewerConn.SendPacketGroup(this.hairItemsPackets);
			}
		}

		public override void On_ItemLeave(ItemInItemArgs args) {
			if (equippedItems != null) {
				if (equippedItems.ContainsKey(args.manipulatedItem)) {
					ItemOnGroundUpdater.RemoveFromCache(this);
				}
			}
		}

		public override sealed Thing Link {
			get {
				return owner;
			}
			set {
				owner = (Character) value;
			}
		}

		public Character Owner {
			get {
				return owner;
			}
			set {
				owner = value;
			}
		}

		public string OwnerName {
			get {
				return ownerName;
			}
			set {
				ownerName = value;
			}
		}

		public ushort HairColor {
			get {
				return hairColor;
			}
		}

		public ushort HairModel {
			get {
				return hairModel;
			}
		}

		public ushort BeardModel {
			get {
				return beardModel;
			}
		}

		public ushort BeardColor {
			get {
				return beardColor;
			}
		}

		public CharacterDef CharDef {
			get {
				return charDef;
			}
		}
	}

	public class T_Corpse : CompiledTriggerGroup {

	}
}