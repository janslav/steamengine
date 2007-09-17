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
using SteamEngine.Packets;

namespace SteamEngine.CompiledScripts {
	public partial class CorpseDef : ContainerDef {

	}

	public partial class Corpse : Container {
		uint hairFakeUid = Thing.GetFakeItemUid();
		uint beardFakeUid = Thing.GetFakeItemUid();

		FreedPacketGroup equippedItemsPackets;
		bool hasEquippedItems = true;

		FreedPacketGroup hairItemsPackets;
		bool hasHairItems = true;

		CharacterDef charDef;

		public override void GetNameCliloc(out uint id, out string argument) {
			if (this.ownerName != null) {
				id = 1070702; //a corpse of ~1_CORPSENAME~
				argument = this.ownerName;
			} else {
				ItemDispidInfo idi = this.TypeDef.DispidInfo;
				string name = this.Name;
				if (idi != null) {
					if (string.Compare(name, idi.name, true) == 0) {
						argument = null;
						id = (uint) (1020000 + (this.Model & 16383));
						return;
					}
				}
				id = 1042971;
				argument = name;
			}
		}

		public override void On_Click(AbstractCharacter clicker) {
			//TODO notoriety hue stuff
			Server.SendNameFrom(clicker.Conn, this, this.Name, 0);
		}

		public override void On_AosClick(AbstractCharacter clicker) {
			//TODO notoriety hue stuff
			ObjectPropertiesContainer opc = this.GetProperties();
			Server.SendClilocNameFrom(clicker.Conn, this,
				opc.FirstId, 0, opc.FirstArgument);
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
				hairModel = hair.Model;
				hairColor = hair.Color;
			}

			Item beard = dieingChar.Beard;
			if (beard != null) {
				beardModel = beard.Model;
				beardColor = beard.Color;
			}
			AbstractItem pack = dieingChar.Backpack;
			foreach (Item inBackpack in pack) {
				if (inBackpack.CanFallToCorpse) {
					inBackpack.Cont = this;
				}
			}
			foreach (Item equipped in dieingChar.GetVisibleEquip()) {
				if (equipped.CanFallToCorpse && equipped != pack && equipped != hair && equipped != beard) {
					equipped.Cont = this;
					if (equippedItems == null) {
						equippedItems = new Dictionary<PacketSender.ICorpseEquipInfo, AbstractItem>();
					}
					equippedItems[equipped] = equipped;
				}
			}
		}

		public void ReturnStuffToChar(Character resurrectedChar) {
			if (equippedItems != null) {
				foreach (Item i in equippedItems.Values) {
					resurrectedChar.TryEquip(resurrectedChar, i);
				}
			}
			AbstractItem backpack = resurrectedChar.Backpack;
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
				SteamEngine.Packets.NetState.ItemAboutToChange(this);
				direction = value;
			}
		}

		public override void On_Destroy() {
			base.On_Destroy();
			Thing.DisposeFakeUid(hairFakeUid);
			Thing.DisposeFakeUid(beardFakeUid);
		}

		internal class CorpseEquipInfo : PacketSender.ICorpseEquipInfo {
			uint flaggedUid;
			byte layer;
			ushort color;
			ushort model;

			internal CorpseEquipInfo(uint flaggedUid, byte layer, ushort color, ushort model) {
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

			public ushort Color {
				get { return color; }
			}

			public ushort Model {
				get { return model; }
			}
		}

		public override void On_BeingSentTo(GameConn viewerConn) {
			base.On_BeingSentTo(viewerConn);

			if (IsOnGround && hasEquippedItems) {
				if (equippedItemsPackets == null) {
					CorpseEquipInfo hair = null;
					CorpseEquipInfo beard = null;

					if (equippedItems == null) {
						equippedItems = new Dictionary<PacketSender.ICorpseEquipInfo, AbstractItem>();
					} else {
						List<AbstractItem> toRemove = null;
						foreach (AbstractItem item in equippedItems.Values) {
							if (item.Cont != this) {
								if (toRemove == null) {
									toRemove = new List<AbstractItem>();
								}
								toRemove.Add(item);
							}
						}
						if (toRemove != null) {
							foreach (AbstractItem noMoreEquipped in toRemove) {
								equippedItems.Remove(noMoreEquipped);
							}
						}
					}

					if (hairModel != 0) {
						hair = new CorpseEquipInfo(
							hairFakeUid, (byte) Layers.layer_hair, hairColor, hairModel);
						equippedItems[hair] = null;
					}
					if (beardModel != 0) {
						beard = new CorpseEquipInfo(
							beardFakeUid, (byte) Layers.layer_beard, beardColor, beardModel);
						equippedItems[beard] = null;
					}

					BoundPacketGroup bpg = PacketSender.NewBoundGroup();
					PacketSender.PrepareCorpseEquip(this, equippedItems.Keys);
					if (hair != null) {
						equippedItems.Remove(hair);
					}
					if (beard != null) {
						equippedItems.Remove(beard);
					}

					if (!PacketSender.PrepareCorpseContents(this, equippedItems.Values, hair, beard)) {
						hasEquippedItems = false;
						bpg.Dispose();
						return;
					} else {
						equippedItemsPackets = bpg.Free();
					}
					if (equippedItems.Count == 0) {
						equippedItems = null;
					}
				}
				equippedItemsPackets.SendTo(viewerConn);
			}
		}

		public override void On_ContainerOpen(GameConn viewerConn) {
			base.On_ContainerOpen(viewerConn);
			if (IsOnGround && hasHairItems) {
				if (hairItemsPackets == null) {
					hasHairItems = false;
					BoundPacketGroup bpg = PacketSender.NewBoundGroup();
					if (hairModel != 0) {
						CorpseEquipInfo hair = new CorpseEquipInfo(
							hairFakeUid, (byte) Layers.layer_hair, hairColor, hairModel);
						PacketSender.PrepareItemInCorpse(this, hair);
						hasHairItems = true;
					}
					if (beardModel != 0) {
						CorpseEquipInfo beard = new CorpseEquipInfo(
							beardFakeUid, (byte) Layers.layer_beard, beardColor, beardModel);
						PacketSender.PrepareItemInCorpse(this, beard);
						hasHairItems = true;
					}
					if (hasHairItems) {
						hairItemsPackets = bpg.Free();
					} else {
						bpg.Dispose();
						return;
					}
				}
				hairItemsPackets.SendTo(viewerConn);
			}
		}

		public override bool On_PickupFrom(AbstractCharacter pickingChar, AbstractItem i, ref object amount) {
			if (equippedItems != null) {
				if (equippedItems.ContainsKey(i)) {
					equippedItemsPackets = null;
				}
			}
			return base.On_PickupFrom(pickingChar, i, ref amount);
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