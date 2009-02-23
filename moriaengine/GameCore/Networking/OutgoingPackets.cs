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

namespace SteamEngine.Networking {

	//ushort size at the beginning of the packet


	public abstract class GameOutgoingPacket : OutgoingPacket {
		protected void EncodeStatVals(short curval, short maxval, bool showReal) {
			if (showReal) {
				this.EncodeShort(maxval);
				this.EncodeShort(curval);
			} else {
				this.EncodeShort(255);
				this.EncodeShort((short) (((int) curval << 8) / maxval));
			}
		}
	}

	public abstract class DynamicLengthOutPacket : GameOutgoingPacket {

		protected override sealed void Write() {
			this.SeekFromCurrent(2);
			this.WriteDynamicPart();
			this.SeekFromStart(1);
			this.EncodeUShort((ushort) this.CurrentSize);
		}

		protected abstract void WriteDynamicPart();

	}

	public abstract class GeneralInformationOutPacket : DynamicLengthOutPacket {
		public override sealed byte Id {
			get { return 0xBF; }
		}

		public abstract ushort SubCmdId {
			get;
		}

		protected override sealed void WriteDynamicPart() {
			this.EncodeUShort(this.SubCmdId);
			this.WriteSubCmd();
		}

		protected abstract void WriteSubCmd();

		public override string FullName {
			get {
				return string.Concat(this.Name, " ( 0x", this.Id.ToString("X"), "-0x", this.SubCmdId.ToString("X"), " )");
			}
		}

		public override string ToString() {
			return string.Concat("0x", this.Id.ToString("X"), "-0x", this.SubCmdId.ToString("X"));
		}
	}

	public sealed class LoginDeniedOutPacket : GameOutgoingPacket {
		byte why;

		public void Prepare(LoginDeniedReason why) {
			this.why = (byte) why;
		}

		public override byte Id {
			get { return 0x82; }
		}

		protected override void Write() {
			this.EncodeByte(this.why);
		}
	}

	public sealed class EnableLockedClientFeaturesOutPacket : GameOutgoingPacket {
		ushort features;

		public void Prepare(ushort features) {
			this.features = features;
		}

		public override byte Id {
			get { return 0xB9; }
		}

		protected override void Write() {
			this.EncodeUShort(this.features);
		}
	}

	public sealed class CharactersListOutPacket : DynamicLengthOutPacket {
		string[] charNames = new string[AbstractAccount.maxCharactersPerGameAccount];
		uint loginFlags;

		public void Prepare(AbstractAccount charsSource, uint loginFlags) {
			for (int i = 0; i < AbstractAccount.maxCharactersPerGameAccount; i++) {
				AbstractCharacter ch = charsSource.GetCharacterInSlot(i);
				if (ch != null) {
					this.charNames[i] = ch.Name;
				} else {
					this.charNames[i] = null;
				}
			}
			this.loginFlags = loginFlags;
		}

		public override byte Id {
			get { return 0xA9; }
		}

		protected override void WriteDynamicPart() {
			this.SeekFromCurrent(1); //skip numOfCharacters byte

			int numOfCharacters = 0;
			for (int i = 0; i < AbstractAccount.maxCharactersPerGameAccount; i++) {
				string name = this.charNames[i];
				if (name != null) {
					this.EncodeASCIIString(name, 30);
					this.EncodeZeros(30);
					numOfCharacters++;
				} else {
					this.EncodeZeros(60);
				}
			}

			this.EncodeByte(1);//just 1 location, no meaning anyway
			this.EncodeByte(0);//index 0
			this.EncodeASCIIString("London", 31);
			this.EncodeASCIIString("London", 31);

			//TODO: Login Flags as bools in globals.
			//Login Flags:
			//0x14 = One character only
			//0x08 = Right-click menus
			//0x20 = AOS features
			//0x40 = Six characters instead of five
			this.EncodeUInt(this.loginFlags);

			this.SeekFromStart(3);
			this.EncodeByte((byte) numOfCharacters);
		}
	}

	public sealed class LoginConfirmationOutPacket : GameOutgoingPacket {
		uint flaggedUid;
		ushort model, x, y, mapSizeX, mapSizeY;
		sbyte z;
		byte direction;

		public void Prepare(AbstractCharacter chr, int mapSizeX, int mapSizeY) {
			this.flaggedUid = chr.FlaggedUid;
			this.model = chr.Model;
			this.x = chr.X;
			this.y = chr.Y;
			this.z = chr.Z;
			this.direction = chr.Dir;

			this.mapSizeX = (ushort) (mapSizeX - 8);
			this.mapSizeY = (ushort) mapSizeY;
		}

		public override byte Id {
			get { return 0x1B; }
		}

		protected override void Write() {
			this.EncodeUInt(this.flaggedUid);
			this.EncodeZeros(4);
			this.EncodeUShort(this.model);
			this.EncodeUShort(this.x);
			this.EncodeUShort(this.y);
			this.EncodeByte(0);
			this.EncodeSByte(this.z);
			this.EncodeByte(this.direction);
			this.EncodeZeros(9);
			//EncodeByte(0xff, 19); //old SE code was sending 0xff, dunno why
			//EncodeByte(0xff, 20);
			//EncodeByte(0xff, 21);
			//EncodeByte(0xff, 22);
			this.EncodeUShort(this.mapSizeX);
			this.EncodeUShort(this.mapSizeY);
			this.EncodeZeros(6);
		}
	}

	public sealed class SetFacetOutPacket : GeneralInformationOutPacket {
		byte facet;

		public void Prepare(byte facet) {
			this.facet = facet;
		}

		public override ushort SubCmdId {
			get { return 0x08; }
		}

		protected override void WriteSubCmd() {
			this.EncodeByte(this.facet);
		}
	}

	public sealed class SeasonalInformationOutPacket : GameOutgoingPacket {
		byte season, cursor;

		public void Prepare(Season season, CursorType cursor) {
			this.season = (byte) season;
			this.cursor = (byte) cursor;
		}

		public override byte Id {
			get { return 0xBC; }
		}

		protected override void Write() {
			this.EncodeByte(this.season);
			this.EncodeByte(this.cursor);
		}
	}

	public sealed class DrawGamePlayerOutPacket : GameOutgoingPacket {
		uint flaggedUid;
		ushort model, color, x, y;
		sbyte z;
		byte flagsToSend, direction;

		public void Prepare(GameState state, AbstractCharacter ch) {
			this.flaggedUid = ch.FlaggedUid;
			this.model = ch.Model;
			this.color = ch.Color;
			MutablePoint4D p = ch.point4d;
			this.x = p.x;
			this.y = p.y;
			this.z = p.z;
			this.flagsToSend = ch.FlagsToSend;
			this.direction = ch.Dir;

			state.movementState.ResetMovementSequence();
		}

		public override byte Id {
			get { return 0x20; }
		}

		protected override void Write() {
			this.EncodeUInt(this.flaggedUid);
			this.EncodeUShort(this.model);
			this.EncodeByte(0);
			this.EncodeUShort(this.color);
			this.EncodeByte(this.flagsToSend);
			this.EncodeUShort(this.x);
			this.EncodeUShort(this.y);
			this.EncodeZeros(2);
			this.EncodeByte(this.direction);
			this.EncodeSByte(this.z);
		}
	}

	public sealed class SetWarModeOutPacket : GameOutgoingPacket {
		bool warModeEnabled;

		public void Prepare(bool warModeEnabled) {
			this.warModeEnabled = warModeEnabled;
		}

		public override byte Id {
			get { return 0x72; }
		}

		protected override void Write() {
			this.EncodeBool(this.warModeEnabled);
			this.EncodeByte(0);
			this.EncodeByte(32);
			this.EncodeByte(0);
		}
	}

	public sealed class ClientViewRangeOutPacket : GameOutgoingPacket {
		byte range;

		public void Prepare(byte range) {
			this.range = range;
		}

		public override byte Id {
			get { return 0xC8; }
		}

		protected override void Write() {
			this.EncodeByte(this.range);
		}
	}

	public sealed class LoginCompleteOutPacket : GameOutgoingPacket {
		public override byte Id {
			get { return 0x55; }
		}

		protected override void Write() {
		}
	}

	public sealed class DrawObjectOutPacket : DynamicLengthOutPacket {
		uint flaggedUid;
		ushort model, x, y, color;
		sbyte z;
		byte dir, flagsToSend, highlight;
		List<ItemInfo> items = new List<ItemInfo>();


		private struct ItemInfo {
			internal readonly uint flaggedUid;
			internal readonly ushort color, model;
			internal readonly byte layer;

			internal ItemInfo(uint flaggedUid, ushort color, ushort model, byte layer) {
				this.flaggedUid = flaggedUid;
				this.color = color;
				this.model = model;
				this.layer = layer;
			}
		}

		public void Prepare(AbstractCharacter ch, HighlightColor highlight) {
			this.flaggedUid = ch.FlaggedUid;
			this.model = ch.Model;
			MutablePoint4D point = ch.point4d;
			this.x = point.x;
			this.y = point.y;
			this.z = point.z;
			this.dir = ch.Dir;
			this.color = ch.Color;
			this.flagsToSend = ch.FlagsToSend;
			this.highlight = (byte) highlight;

			items.Clear();
			foreach (AbstractItem i in ch.visibleLayers) {
				items.Add(new ItemInfo(i.FlaggedUid, i.Color, i.Model, i.Layer));
			}

			AbstractCharacter mount = ch.Mount;
			if (mount != null) {
				items.Add(new ItemInfo(mount.FlaggedUid | 0x40000000, mount.Color, mount.MountItem, (byte) LayerNames.Mount));
			}
		}

		public override byte Id {
			get { return 0x78; }
		}

		protected override void WriteDynamicPart() {
			this.EncodeUInt(this.flaggedUid);
			this.EncodeUShort(this.model);
			this.EncodeUShort(this.x);
			this.EncodeUShort(this.y);
			this.EncodeSByte(this.z);
			this.EncodeByte(this.dir);
			this.EncodeUShort(this.color);
			this.EncodeByte(this.flagsToSend);
			this.EncodeByte(this.highlight);

			foreach (ItemInfo i in this.items) {
				this.EncodeUInt(i.flaggedUid);
				if (i.color == 0) {
					this.EncodeUShort(i.model);
					this.EncodeByte(i.layer);
				} else {
					this.EncodeUShort((ushort) (i.model | 0x8000));
					this.EncodeByte(i.layer);
					this.EncodeUShort(i.color);
				}
			}

			this.EncodeZeros(4);
		}
	}

	public sealed class UpdatePlayerPacket : GameOutgoingPacket {
		uint flaggedUid;
		ushort model, x, y, color;
		sbyte z;
		byte dir, flagsToSend, highlight;

		public void Prepare(AbstractCharacter chr, bool running, HighlightColor highlight) {
			this.flaggedUid = chr.FlaggedUid;
			this.model = chr.Model;
			this.x = chr.X;
			this.y = chr.Y;
			this.z = chr.Z;
			this.color = chr.Color;
			this.flagsToSend = chr.FlagsToSend;
			this.highlight = (byte) highlight;

			this.dir = chr.Dir;
			if (running) {
				this.dir |= 0x80;
			}
		}

		public override byte Id {
			get { return 0x77; }
		}

		protected override void Write() {
			this.EncodeUInt(this.flaggedUid);
			this.EncodeUShort(this.model);
			this.EncodeUShort(this.x);
			this.EncodeUShort(this.y);
			this.EncodeSByte(this.z);
			this.EncodeByte(this.dir);
			this.EncodeUShort(this.color);
			this.EncodeByte(this.flagsToSend);
			this.EncodeByte(this.highlight);
		}
	}

	public sealed class DrawContainerOutPacket : GameOutgoingPacket {
		uint flaggedUid;
		ushort gump;

		public void PrepareContainer(uint flaggedUid, ushort gump) {
			this.flaggedUid = flaggedUid;
			this.gump = gump;
		}

		public void PrepareSpellbook(uint flaggedUid) {
			this.flaggedUid = flaggedUid;
			this.gump = 0xffff;
		}

		public override byte Id {
			get { return 0x24; }
		}

		protected override void Write() {
			this.EncodeUInt(this.flaggedUid);
			this.EncodeUShort(this.gump);
		}
	}

	public sealed class AddItemToContainerOutPacket : GameOutgoingPacket {
		uint flaggedUid, contFlaggedUid;
		ushort model, x, y, color, amount;

		public void Prepare(uint contFlaggedUid, AbstractItem i) {
			this.flaggedUid = i.FlaggedUid;
			this.contFlaggedUid = i.Cont.FlaggedUid;
			this.model = i.Model;
			this.x = i.X;
			this.y = i.Y;
			this.color = i.Color;
			this.amount = i.ShortAmount;
		}

		public void PrepareItemInCorpse(uint corpseUid, ICorpseEquipInfo i) {
			this.flaggedUid = i.FlaggedUid;
			this.contFlaggedUid = corpseUid;
			this.model = i.Model;
			this.x = 0;
			this.y = 0;
			this.color = i.Color;
			this.amount = 1;
		}

		public override byte Id {
			get { return 0x25; }
		}

		protected override void Write() {
			this.EncodeUInt(this.flaggedUid);
			this.EncodeUShort(this.model);
			this.EncodeByte(0); //unknown
			this.EncodeUShort(this.amount);
			this.EncodeUShort(this.x);
			this.EncodeUShort(this.y);
			this.EncodeUInt(this.contFlaggedUid);
			this.EncodeUShort(this.color);
		}
	}

	public sealed class CorpseClothingOutPacket : DynamicLengthOutPacket {
		List<ItemInfo> items = new List<ItemInfo>();
		uint corpseUid;

		public void Prepare(uint corpseUid, IEnumerable<ICorpseEquipInfo> equippedItems) {
			this.corpseUid = corpseUid;

			this.items.Clear();
			foreach (ICorpseEquipInfo iulp in equippedItems) {
				this.items.Add(new ItemInfo(iulp.FlaggedUid, iulp.Layer));
			}
		}

		private struct ItemInfo {
			internal readonly uint flaggedUid;
			internal readonly byte layer;

			internal ItemInfo(uint flaggedUid, byte layer) {
				this.flaggedUid = flaggedUid;
				this.layer = layer;
			}
		}

		public override byte Id {
			get { return 0x89; }
		}

		protected override void WriteDynamicPart() {
			this.EncodeUInt(this.corpseUid);
			foreach (ItemInfo i in this.items) {
				this.EncodeByte(i.layer);
				this.EncodeUInt(i.flaggedUid);
			}
			this.EncodeByte(0); //terminator
		}
	}

	public sealed class ItemsInContainerOutPacket : DynamicLengthOutPacket {
		uint flaggedUid;
		private List<ItemInfo> items = new List<ItemInfo>();

		private struct ItemInfo {
			internal readonly uint flaggedUid;
			internal readonly ushort color, model, amount, x, y;

			internal ItemInfo(AbstractItem i) {
				this.flaggedUid = i.FlaggedUid;
				this.color = i.Color;
				this.model = i.Model;
				this.amount = i.ShortAmount;
				this.x = i.X;
				this.y = i.Y;
			}

			internal ItemInfo(ICorpseEquipInfo i) {
				this.flaggedUid = i.FlaggedUid;
				this.color = i.Color;
				this.model = i.Model;
				this.amount = 1;
				this.x = 0;
				this.y = 0;
			}

			internal ItemInfo(uint flaggedUid, ushort amount) {
				this.flaggedUid = flaggedUid;
				this.color = 0;
				this.model = 0;
				this.amount = amount;
				this.x = 0;
				this.y = 0;
			}
		}

		public bool PrepareContainer(AbstractItem cont, AbstractCharacter viewer, IList<AbstractItem> visibleItems) {
			this.flaggedUid = cont.FlaggedUid;

			items.Clear();

			foreach (AbstractItem i in cont) {
				if (viewer.CanSeeVisibility(i)) {
					items.Add(new ItemInfo(i));
					visibleItems.Add(i);
				}
			}

			return items.Count > 0;
		}

		public bool PrepareCorpse(uint corpseUid, IEnumerable<ICorpseEquipInfo> equippedItems) {
			this.flaggedUid = corpseUid;

			this.items.Clear();
			foreach (ICorpseEquipInfo i in equippedItems) {
				this.items.Add(new ItemInfo(i));
			}

			return this.items.Count > 0;
		}

		public void PrepareSpellbook(uint flaggedUid, int offset, ulong content) {
			this.flaggedUid = flaggedUid;

			items.Clear();

			ulong mask = 1;
			for (int i = 0; i < 64; i++, mask <<= 1) {
				if ((content & mask) != 0) {
					this.items.Add(new ItemInfo((uint) (0x7FFFFFFF - i), (ushort) (i + offset)));
				}
			}
		}

		public override byte Id {
			get { return 0x3c; }
		}

		protected override void WriteDynamicPart() {
			this.EncodeUShort((ushort) this.items.Count);

			foreach (ItemInfo i in this.items) {
				this.EncodeUInt(i.flaggedUid);
				this.EncodeUShort(i.model);
				this.EncodeByte(0);
				this.EncodeUShort(i.amount);
				this.EncodeUShort(i.x);
				this.EncodeUShort(i.y);
				//TODO? post 6.0.1.7: byte gridindex
				this.EncodeUInt(this.flaggedUid);
				this.EncodeUShort(i.color);
			}
		}
	}

	public sealed class NewSpellbookOutPacket : GeneralInformationOutPacket {
		uint flaggedUid;
		ushort bookModel;
		short firstSpellId;
		ulong content;

		public void Prepare(uint bookUid, ushort bookModel, short firstSpellId, ulong content) {
			this.flaggedUid = bookUid;
			this.bookModel = bookModel;
			this.firstSpellId = firstSpellId;
			this.content = content;
		}

		public override ushort SubCmdId {
			get { return 0x1b; }
		}

		protected override void WriteSubCmd() {
			this.EncodeShort(1);
			this.EncodeUInt(this.flaggedUid);
			this.EncodeUShort(this.bookModel);
			this.EncodeShort(this.firstSpellId);

			for (int i = 0; i < 8; i++) {
				this.EncodeByte((byte) (this.content >> (i * 8)));
			}
		}
	}

	public sealed class OldPropertiesRefreshOutPacket : GeneralInformationOutPacket {
		uint flaggedUid;
		int propertiesUid;

		public void Prepare(uint flaggedUid, int propertiesUid) {
			this.flaggedUid = flaggedUid;
			this.propertiesUid = propertiesUid;
		}

		public override ushort SubCmdId {
			get { return 0x10; }
		}

		protected override void WriteSubCmd() {
			this.EncodeUInt(this.flaggedUid);
			this.EncodeInt(this.propertiesUid);
		}
	}

	public sealed class PropertiesRefreshOutPacket : GameOutgoingPacket {
		uint flaggedUid;
		int propertiesUid;

		public void Prepare(uint flaggedUid, int propertiesUid) {
			this.flaggedUid = flaggedUid;
			this.propertiesUid = propertiesUid;
		}

		public override byte Id {
			get { return 0xdc; }
		}

		protected override void Write() {
			this.EncodeUInt(this.flaggedUid);
			this.EncodeInt(this.propertiesUid);
		}
	}

	public sealed class ObjectInfoOutPacket : DynamicLengthOutPacket {
		uint flaggedUid;
		ushort amount, model, x, y, color;
		sbyte z;
		byte dir, flagsToSend;
		MoveRestriction restrict;

		public void Prepare(AbstractItem item, MoveRestriction restrict) {
			this.flaggedUid = item.FlaggedUid;
			this.amount = item.ShortAmount;
			this.model = item.Model;
			MutablePoint4D point = item.point4d;
			this.x = point.x;
			this.y = point.y;
			this.z = point.z;
			this.dir = (byte) item.Direction;
			this.flagsToSend = item.FlagsToSend;
			this.restrict = restrict;
			this.color = item.Color;
		}

		[Summary("Prepare method for creating the 'fake item' packets")]
		public void PrepareFakeItem(uint itemUid, ushort model, IPoint4D point4D, ushort amount, Direction dir, ushort color) {
			//this must be the item UID (containing 0x40000000)
			this.flaggedUid = itemUid | 0x40000000;
			this.amount = amount;
			this.model = model;
			this.x = point4D.X;
			this.y = point4D.Y;
			this.z = point4D.Z;
			this.dir = (byte) dir;
			this.flagsToSend = 0x00;//Normal
			this.restrict = MoveRestriction.Normal; //this means Not Movable?
			this.color = color;
		}

		public override byte Id {
			get { return 0x1A; }
		}

		protected override void WriteDynamicPart() {
			if (this.amount != 1) {
				this.EncodeUInt(this.flaggedUid | 0x80000000);
				this.EncodeUShort(this.model);
				this.EncodeUShort(this.amount);
			} else {
				this.EncodeUInt(this.flaggedUid);
				this.EncodeUShort(this.model);
			}

			if (this.dir != 0) {
				this.x |= 0x8000;
			}
			this.EncodeUShort(this.x);
			if (this.color != 0) {
				this.y |= 0x8000;
			}
			if (this.flagsToSend != 0) {
				this.y |= 0x4000;
			}
			this.EncodeUShort(this.y);
			if (this.dir != 0) {
				this.EncodeByte(this.dir);
			}
			this.EncodeSByte(this.z);

			if (this.color != 0) {
				this.EncodeUShort(this.color);
			}
			if (this.flagsToSend != 0) {
				this.EncodeByte(this.flagsToSend);
			}
		}
	}

	public sealed class MobAttributesOutPacket : GameOutgoingPacket {
		bool showReal;
		short mana, maxMana, hits, maxHits, stam, maxStam;
		uint flaggedUid;

		public void Prepare(AbstractCharacter cre, bool showReal) {
			this.flaggedUid = cre.FlaggedUid;
			this.mana = cre.Mana;
			this.maxMana = cre.MaxMana;
			this.hits = cre.Hits;
			this.maxHits = cre.MaxHits;
			this.stam = cre.Stam;
			this.maxStam = cre.MaxStam;
			this.showReal = showReal;
		}

		public override byte Id {
			get { return 0x2d; }
		}

		protected override void Write() {
			this.EncodeUInt(this.flaggedUid);
			this.EncodeStatVals(this.hits, this.maxHits, this.showReal);
			this.EncodeStatVals(this.mana, this.maxMana, this.showReal);
			this.EncodeStatVals(this.stam, this.maxStam, this.showReal);
		}
	}

	public sealed class UpdateCurrentHealthOutPacket : GameOutgoingPacket {
		bool showReal;
		short hits, maxHits;
		uint flaggedUid;

		public void Prepare(uint flaggedUid, short hits, short maxHits, bool showReal) {
			this.flaggedUid = flaggedUid;
			this.hits = hits;
			this.maxHits = maxHits;
			this.showReal = showReal;
		}

		public override byte Id {
			get { return 0xa1; }
		}

		protected override void Write() {
			this.EncodeUInt(this.flaggedUid);
			this.EncodeStatVals(this.hits, this.maxHits, this.showReal);
		}
	}

	public sealed class UpdateCurrentManaOutPacket : GameOutgoingPacket {
		bool showReal;
		short mana, maxMana;
		uint flaggedUid;

		public void Prepare(uint flaggedUid, short mana, short maxMana, bool showReal) {
			this.flaggedUid = flaggedUid;
			this.mana = mana;
			this.maxMana = maxMana;
			this.showReal = showReal;
		}

		public override byte Id {
			get { return 0xa2; }
		}

		protected override void Write() {
			this.EncodeUInt(this.flaggedUid);
			this.EncodeStatVals(this.mana, this.maxMana, this.showReal);
		}
	}

	public sealed class UpdateCurrentStaminaOutPacket : GameOutgoingPacket {
		bool showReal;
		short stam, maxStam;
		uint flaggedUid;

		public void Prepare(uint flaggedUid, short stam, short maxStam, bool showReal) {
			this.flaggedUid = flaggedUid;
			this.stam = stam;
			this.maxStam = maxStam;
			this.showReal = showReal;
		}

		public override byte Id {
			get { return 0xa3; }
		}

		protected override void Write() {
			this.EncodeUInt(this.flaggedUid);
			this.EncodeStatVals(this.stam, this.maxStam, this.showReal);
		}
	}

	public sealed class StatusBarInfoOutPacket : DynamicLengthOutPacket {
		StatusBarType type;
		uint flaggedUid, gold;
		string charName;
		short mana, maxMana, hits, maxHits, stam, maxStam, strength, dexterity, intelligence, armor,
			fireResist, coldResist, poisonResist, energyResist, luck, minDamage, maxDamage, tithingPoints;
		byte currentPets, maxPets;
		ushort statCap, weight;
		bool isFemale;
		bool canRenameSelf;

		public void Prepare(AbstractCharacter ch, StatusBarType type) {
			Sanity.IfTrueThrow(ch == null, "PrepareStatusBar called with a null character.");
			Sanity.IfTrueThrow(!Enum.IsDefined(typeof(StatusBarType), type), "Invalid value " + type + " for StatusBarType in PrepareStatusBar.");

			this.type = type;
			this.charName = ch.Name;

			this.hits = ch.Hits;
			this.maxHits = ch.MaxHits;
			this.flaggedUid = ch.FlaggedUid;

			if (type == StatusBarType.Me) {
				this.mana = ch.Mana;
				this.maxMana = ch.MaxMana;
				this.stam = ch.Stam;
				this.maxStam = ch.MaxStam;

				this.strength = ch.Str;
				this.dexterity = ch.Dex;
				this.intelligence = ch.Int;

				ulong lgold = ch.Gold;
				this.gold = (uint) (lgold > 0xffffffff ? 0xffffffff : lgold);
				this.armor = ch.StatusArmorClass;
				this.weight = (ushort) ch.Weight;

				this.currentPets = ch.ExtendedStatusNum07;
				this.maxPets = ch.ExtendedStatusNum08;
				this.statCap = ch.ExtendedStatusNum09;

				this.fireResist = ch.ExtendedStatusNum01;
				this.coldResist = ch.ExtendedStatusNum02;
				this.poisonResist = ch.ExtendedStatusNum03;
				this.energyResist = ch.StatusMindDefense;
				this.luck = ch.ExtendedStatusNum04;
				this.minDamage = ch.ExtendedStatusNum05;
				this.maxDamage = ch.ExtendedStatusNum06;
				this.tithingPoints = ch.TithingPoints;

				this.isFemale = ch.IsFemale;

				this.canRenameSelf = ch.CanRename(ch);
			}
		}

		public override byte Id {
			get { return 0x11; }
		}

		protected override void WriteDynamicPart() {
			this.EncodeUInt(this.flaggedUid);
			this.EncodeASCIIString(this.charName, 30);

			if (this.type == StatusBarType.Me) {
				this.EncodeShort(this.hits);
				this.EncodeShort(this.maxHits);
				this.EncodeBool(this.canRenameSelf);
				this.EncodeByte(4); //more data following
			} else {
				this.EncodeShort((short) (((int) this.hits << 8) / this.maxHits));
				this.EncodeShort(256);
				if (this.type == StatusBarType.Pet) {
					this.EncodeByte(1);
				} else {
					this.EncodeByte(0);
				}
				this.EncodeByte(0); //no data following
				return;
			}

			this.EncodeBool(this.isFemale);

			this.EncodeShort(this.strength);
			this.EncodeShort(this.dexterity);
			this.EncodeShort(this.intelligence);
			this.EncodeShort(this.stam);
			this.EncodeShort(this.maxStam);
			this.EncodeShort(this.mana);
			this.EncodeShort(this.maxMana);
			this.EncodeUInt(this.gold);
			this.EncodeShort(this.armor);
			this.EncodeUShort(this.weight);

			this.EncodeByte(0); //unknown

			this.EncodeUShort(this.statCap);
			this.EncodeByte(this.currentPets);
			this.EncodeByte(this.maxPets);

			this.EncodeShort(this.fireResist);
			this.EncodeShort(this.coldResist);
			this.EncodeShort(this.poisonResist);
			this.EncodeShort(this.energyResist);
			this.EncodeShort(this.luck);
			this.EncodeShort(this.minDamage);
			this.EncodeShort(this.maxDamage);
			this.EncodeInt(this.tithingPoints);

		}
	}


	public sealed class SendSkillsOutPacket : DynamicLengthOutPacket {
		List<SkillInfo> skillList = new List<SkillInfo>();
		bool displaySkillCaps, singleSkill;
		byte type;
		//0x00= full list, 0xFF = single skill update, 
		//0x02 full list with skillcap, 0xDF single skill update with cap

		public void PrepareAllSkillsUpdate(IEnumerable<ISkill> skills, bool displaySkillCaps) {
			this.singleSkill = false;
			this.displaySkillCaps = displaySkillCaps;
			if (displaySkillCaps) {
				this.type = 0x02;//full list with skillcaps
			} else {
				this.type = 0x00;//full list without skillcaps
			}

			skillList.Clear();
			foreach (ISkill s in skills) {
				skillList.Add(new SkillInfo(s.Id, s.RealValue, s.ModifiedValue, s.Cap, s.Lock));
			}
		}

		public void PrepareSingleSkillUpdate(ushort skillId, ISkill skill, bool displaySkillCaps) {
			ushort realValue, modifiedValue, cap;
			SkillLockType skillLock;
			if (skill == null) {
				realValue = 0;
				modifiedValue = 0;
				cap = 1000;
				skillLock = SkillLockType.Increase;
			} else {
				realValue = skill.RealValue;
				modifiedValue = skill.ModifiedValue;
				cap = skill.Cap;
				skillLock = skill.Lock;
			}

			if (displaySkillCaps) {
				this.PrepareSingleSkillUpdate(skillId, realValue, modifiedValue, skillLock, cap);
			} else {
				this.PrepareSingleSkillUpdate(skillId, realValue, modifiedValue, skillLock);
			}
		}

		public void PrepareSingleSkillUpdate(ushort skillId, ushort realValue, ushort modifiedValue, SkillLockType skillLock) {
			this.displaySkillCaps = false;
			this.singleSkill = true;
			this.type = 0xFF; //partial list without caps
			skillList.Clear();
			skillList.Add(new SkillInfo(skillId, realValue, modifiedValue, 0, skillLock));
		}

		public void PrepareSingleSkillUpdate(ushort skillId, ushort realValue, ushort modifiedValue, SkillLockType skillLock, ushort cap) {
			this.displaySkillCaps = true;
			this.singleSkill = true;
			this.type = 0xDF; //partial list with caps
			skillList.Clear();
			skillList.Add(new SkillInfo(skillId, realValue, modifiedValue, cap, skillLock));
		}

		private struct SkillInfo {
			public readonly ushort realValue, modifiedValue, cap, id;
			public readonly SkillLockType skillLock;

			public SkillInfo(ushort id, ushort realValue, ushort modifiedValue, ushort cap, SkillLockType skillLock) {
				this.realValue = realValue;
				this.modifiedValue = modifiedValue;
				this.cap = cap;
				this.id = id;
				this.skillLock = skillLock;
			}
		}

		public override byte Id {
			get { return 0x3A; }
		}

		protected override void WriteDynamicPart() {
			this.EncodeByte(this.type);

			foreach (SkillInfo s in this.skillList) {
				if (this.singleSkill) {
					this.EncodeUShort(s.id);
				} else {
					this.EncodeUShort((ushort) (s.id + 1));
				}
				this.EncodeUShort(s.modifiedValue);
				this.EncodeUShort(s.realValue);
				this.EncodeByte((byte) s.skillLock);
				if (this.displaySkillCaps) {
					this.EncodeUShort(s.cap);
				}
			}

			//if (this.type == 0x00) {
			//    this.EncodeUShort(0);
			//}
		}
	}


	public sealed class DeleteObjectOutPacket : GameOutgoingPacket {
		uint flaggedUid;

		public void Prepare(uint flaggedUid) {
			this.flaggedUid = flaggedUid;
		}

		public void Prepare(int uid) {
			this.flaggedUid = (uint) uid;
		}

		public void Prepare(Thing thing) {
			this.flaggedUid = thing.FlaggedUid;
		}

		public override byte Id {
			get { return 0x1d; }
		}

		protected override void Write() {
			this.EncodeUInt(this.flaggedUid);
		}
	}

	public sealed class MegaClilocOutPacket : DynamicLengthOutPacket {
		uint flaggedUid;
		int propertiesUid;
		IList<uint> ids;
		IList<string> strings;

		public void Prepare(uint flaggedUid, int propertiesUid, IList<uint> ids, IList<string> strings) {
			this.flaggedUid = flaggedUid;
			this.propertiesUid = propertiesUid;
			this.ids = ids;
			this.strings = strings;
		}

		public override byte Id {
			get { return 0xd6; }
		}

		protected override void WriteDynamicPart() {
			this.EncodeShort(0x0001);
			this.EncodeUInt(this.flaggedUid);
			this.EncodeZeros(2);
			this.EncodeInt(this.propertiesUid);

			for (int i = 0, n = ids.Count; i < n; i++) {
				this.EncodeUInt(ids[i]);
				string msg = strings[i];
				if (msg == null) {
					msg = "";
				}
				this.EncodeLittleEndianUnicodeStringWithLen(msg);
			}

			this.EncodeZeros(4);
		}
	}

	public sealed class SendSpeechOutPacket : DynamicLengthOutPacket {
		uint flaggedUid;
		ushort model, color, font;
		string sourceName, message;
		byte type;

		//from can be null
		public void Prepare(Thing from, string message, string sourceName, SpeechType type, ushort font, int color) {
			if (from == null) {
				this.flaggedUid = 0xffffffff;
				this.model = 0xffff;
			} else {
				this.flaggedUid = from.FlaggedUid;
				this.model = from.Model;
			}

			this.sourceName = sourceName;
			this.message = message;
			this.type = (byte) type;
			this.color = Utility.NormalizeDyedColor(color, Globals.defaultASCIIMessageColor);
			this.font = font;
		}

		public override byte Id {
			get { return 0x1c; }
		}

		protected override void WriteDynamicPart() {
			this.EncodeUInt(this.flaggedUid);
			this.EncodeUShort(this.model);
			this.EncodeByte(this.type);
			this.EncodeUShort(this.color);
			this.EncodeUShort(this.font);
			this.EncodeASCIIString(this.sourceName, 30);
			this.EncodeASCIIString(this.message);
		}
	}

	public sealed class UnicodeSpeechOutPacket : DynamicLengthOutPacket {
		uint flaggedUid;
		ushort model, color, font;
		string sourceName, message, language;
		byte type;

		//from can be null
		public void Prepare(Thing from, string message, string sourceName, SpeechType type, ushort font, int color, string language) {
			if (from == null) {
				this.flaggedUid = 0xffffffff;
				this.model = 0xffff;
			} else {
				this.flaggedUid = from.FlaggedUid;
				this.model = from.Model;
			}

			this.sourceName = sourceName;
			this.message = message;
			this.language = language;
			this.type = (byte) type;
			this.color = Utility.NormalizeDyedColor(color, Globals.defaultUnicodeMessageColor);
			this.font = font;
		}

		public override byte Id {
			get { return 0xAE; }
		}

		protected override void WriteDynamicPart() {
			this.EncodeUInt(this.flaggedUid);
			this.EncodeUShort(this.model);
			this.EncodeByte(this.type);
			this.EncodeUShort(this.color);
			this.EncodeUShort(this.font);
			this.EncodeASCIIString(this.language, 4);
			this.EncodeASCIIString(this.sourceName, 30);
			this.EncodeBigEndianUnicodeString(this.message);
			this.EncodeZeros(2);
		}
	}

	public sealed class ClilocMessageOutPacket : DynamicLengthOutPacket {
		uint flaggedUid;
		ushort model, color, font;
		string sourceName, args;
		uint message;
		byte type;

		//from can be null
		public void Prepare(Thing from, uint message, string sourceName, SpeechType type, ushort font, int color, string args) {
			if (from == null) {
				this.flaggedUid = 0xffffffff;
				this.model = 0xffff;
			} else {
				this.flaggedUid = from.FlaggedUid;
				this.model = from.Model;
			}

			this.sourceName = sourceName;
			this.message = message;
			this.args = args;
			this.type = (byte) type;
			this.color = Utility.NormalizeDyedColor(color, Globals.defaultUnicodeMessageColor); ;
			this.font = font;
		}

		public override byte Id {
			get { return 0xC1; }
		}

		protected override void WriteDynamicPart() {
			this.EncodeUInt(this.flaggedUid);
			this.EncodeUShort(this.model);
			this.EncodeByte(this.type);
			this.EncodeUShort(this.color);
			this.EncodeUShort(this.font);
			this.EncodeUInt(this.message);
			this.EncodeASCIIString(this.sourceName, 30);
			this.EncodeLittleEndianUnicodeString(this.args);
			this.EncodeZeros(2);//msg terminator
		}
	}

	public enum AffixType : byte {
		Append = 0x00,
		Prepend = 0x01,
		//System = 0x02
	}

	public sealed class ClilocMessageAffixOutPacket : DynamicLengthOutPacket {
		uint flaggedUid;
		ushort model, color, font;
		string sourceName, args, affix;
		uint message;
		byte type, flags;

		//from can be null
		public void Prepare(Thing from, uint message, string sourceName, SpeechType type, ushort font, int color, AffixType flags, string affix, string args) {
			if (from == null) {
				this.flaggedUid = 0xffffffff;
				this.model = 0xffff;
				this.flags = 0x02;
			} else {
				this.flaggedUid = from.FlaggedUid;
				this.model = from.Model;
				this.flags = 0x00;
			}

			this.flags |= (byte) flags;
			this.sourceName = sourceName;
			this.message = message;
			this.args = args;
			this.type = (byte) type;
			this.color = Utility.NormalizeDyedColor(color, Globals.defaultUnicodeMessageColor); ;
			this.font = font;
			this.affix = affix;
		}

		public override byte Id {
			get { return 0xCC; }
		}

		protected override void WriteDynamicPart() {
			this.EncodeUInt(this.flaggedUid);
			this.EncodeUShort(this.model);
			this.EncodeByte(this.type);
			this.EncodeUShort(this.color);
			this.EncodeUShort(this.font);
			this.EncodeUInt(this.message);
			this.EncodeByte(this.flags);
			this.EncodeASCIIString(this.sourceName, 30);
			this.EncodeASCIIString(this.affix);
			this.EncodeBigEndianUnicodeString(this.args);
			this.EncodeZeros(2);//msg terminator
		}
	}

	public sealed class TargetCursorCommandsOutPacket : GameOutgoingPacket {
		byte type;
		byte cursorType;//1 = Harmful, 2 = Beneficial 3 = cancel

		public void Prepare(bool ground) {
			this.type = ground ? (byte) 1 : (byte) 0;
			this.cursorType = 0;
		}

		public void PrepareAsCancel() {
			this.type = 0;
			this.cursorType = 3;
		}

		public override byte Id {
			get { return 0x6c; }
		}

		protected override void Write() {
			this.EncodeByte(this.type);
			this.EncodeZeros(4);
			this.EncodeByte(this.cursorType);
			this.EncodeZeros(12);
		}
	}

	public sealed class GiveBoatOrHousePlacementViewOutPacket : GameOutgoingPacket {
		ushort model;

		public void Prepare(ushort model) {
			this.model = model;
		}

		public override byte Id {
			get { return 0x99; }
		}

		protected override void Write() {
			this.EncodeByte(1);
			this.EncodeZeros(16);
			this.EncodeShort((short) (this.model & 0x8fff));
			this.EncodeZeros(6);
		}
	}

	public sealed class OpenPaperdollOutPacket : GameOutgoingPacket {
		uint flaggedUid;
		string text;
		byte flagsToSend;

		public void Prepare(AbstractCharacter character, bool canEquip) {
			this.flaggedUid = character.FlaggedUid;
			this.text = character.PaperdollName;
			this.flagsToSend = character.FlagsToSend;

			Sanity.IfTrueThrow((this.flagsToSend & 0x02) > 0, character + "'s FlagsToSend included 0x02, which is the 'can equip items on' flag for paperdoll packets - FlagsToSend should never include it.");
			if (canEquip) {	//include 0x02 if we can equip stuff on them.
				this.flagsToSend |= 0x02;
			}
		}

		public override byte Id {
			get { return 0x88; }
		}

		protected override void Write() {
			this.EncodeUInt(this.flaggedUid);
			this.EncodeASCIIString(this.text, 60);
			this.EncodeByte(this.flagsToSend);
		}
	}

	public sealed class WornItemOutPacket : GameOutgoingPacket {
		uint itemFlaggedUid, charUid;
		sbyte layer;
		ushort model, color;

		public void PrepareItem(uint charUid, AbstractItem item) {
			this.charUid = charUid;
			this.itemFlaggedUid = item.FlaggedUid;
			this.layer = item.Z;
			this.model = item.Model;
			this.color = item.Color;
		}

		public void PrepareMount(uint charUid, AbstractCharacter mount) {
			this.charUid = charUid;
			this.itemFlaggedUid = (uint) (mount.Uid | 0x40000000);
			this.layer = (sbyte) LayerNames.Mount;
			this.model = mount.MountItem;
			this.color = mount.Color;
		}

		public override byte Id {
			get { return 0x2e; }
		}

		protected override void Write() {
			this.EncodeUInt(this.itemFlaggedUid);
			this.EncodeUShort(this.model);
			this.EncodeByte(0);
			this.EncodeSByte(this.layer);
			this.EncodeUInt(this.charUid);
			this.EncodeUShort(this.color);
		}
	}

	public sealed class CharacterMoveAcknowledgeOutPacket : GameOutgoingPacket {
		byte sequence, highlight;

		public void Prepare(byte sequence, HighlightColor highlight) {
			this.sequence = sequence;
			this.highlight = (byte) highlight;
		}

		public override byte Id {
			get { return 0x22; }
		}

		protected override void Write() {
			this.EncodeByte(this.sequence);
			this.EncodeByte(this.highlight);
		}
	}

	public sealed class CharMoveRejectionOutPacket : GameOutgoingPacket {
		byte sequence, direction;
		ushort x, y;
		sbyte z;

		public void Prepare(byte sequence, AbstractCharacter ch) {
			this.sequence = sequence;
			this.direction = ch.Dir;
			this.x = ch.X;
			this.y = ch.Y;
			this.z = ch.Z;
		}

		public override byte Id {
			get { return 0x21; }
		}

		protected override void Write() {
			this.EncodeByte(this.sequence);
			this.EncodeUShort(this.x);
			this.EncodeUShort(this.y);
			this.EncodeByte(this.direction);
			this.EncodeSByte(this.z);
		}
	}

	public sealed class RejectMoveItemRequestOutPacket : GameOutgoingPacket {
		byte denyResult;

		public void Prepare(DenyResult denyResult) {
			this.denyResult = (byte) denyResult;
		}

		public override byte Id {
			get { return 0x27; }
		}

		protected override void Write() {
			this.EncodeByte(this.denyResult);
		}
	}

	public sealed class RejectDeleteCharacterOutPacket : GameOutgoingPacket {
		byte reason;

		public void Prepare(DeleteCharacterResult reason) {
			this.reason = (byte) reason;
		}

		public override byte Id {
			get { return 0x85; }
		}

		protected override void Write() {
			this.EncodeByte(this.reason);
		}
	}

	public sealed class ResendCharactersAfterDeleteOutPacket : DynamicLengthOutPacket {
		string[] names = new string[AbstractAccount.maxCharactersPerGameAccount];

		public void Prepare(IList<AbstractCharacter> chars) {
			for (int i = 0; i < AbstractAccount.maxCharactersPerGameAccount; i++) {
				AbstractCharacter ch = chars[i];
				if (ch != null) {
					names[i] = ch.Name;
				} else {
					names[i] = string.Empty;
				}
			}
		}

		public override byte Id {
			get { return 0x86; }
		}

		protected override void WriteDynamicPart() {
			this.EncodeByte((byte) AbstractAccount.maxCharactersPerGameAccount);

			for (int i = 0; i < AbstractAccount.maxCharactersPerGameAccount; i++) {
				this.EncodeASCIIString(this.names[i], 30);
				this.EncodeZeros(30);
			}
		}
	}

	public sealed class AllNamesOutPacket : DynamicLengthOutPacket {
		int flaggedUid;
		string name;

		public void Prepare(int flaggedUid, string name) {
			this.flaggedUid = flaggedUid;
			this.name = name;
		}

		public override byte Id {
			get { return 0x98; }
		}

		protected override void WriteDynamicPart() {
			this.EncodeInt(this.flaggedUid);
			this.EncodeASCIIString(this.name);
		}
	}

	public sealed class SendGumpMenuDialogPacket : DynamicLengthOutPacket {
		uint focusFlaggedUid, gumpUid;
		int x, y;
		string layoutText;
		List<string> strings = new List<string>();

		public void Prepare(Gump gump) {
			this.focusFlaggedUid = gump.Focus.FlaggedUid;
			this.gumpUid = gump.uid;
			this.x = gump.X;
			this.y = gump.Y;
			this.layoutText = gump.layout.ToString();

			this.strings.Clear();
			if (gump.textsList != null) {
				this.strings.AddRange(gump.textsList);
			}
		}

		public override byte Id {
			get { return 0xb0; }
		}

		protected override void WriteDynamicPart() {
			this.EncodeUInt(this.focusFlaggedUid);
			this.EncodeUInt(this.gumpUid);
			this.EncodeInt(this.x);
			this.EncodeInt(this.y);

			this.EncodeUShort((ushort) (this.layoutText.Length + 1)); //+1 cos of null terminator
			this.EncodeASCIIString(this.layoutText);

			this.EncodeUShort((ushort) this.strings.Count);
			foreach (string s in this.strings) {
				this.EncodeUShort((ushort) s.Length);
				this.EncodeBigEndianUnicodeString(s);
			}
		}
	}

	public sealed class CloseGenericGumpOutPacket : GeneralInformationOutPacket {
		uint gumpUid;
		int buttonId;

		public void Prepare(uint gumpUid, int buttonId) {
			this.gumpUid = gumpUid;
			this.buttonId = buttonId;
		}

		public override ushort SubCmdId {
			get { return 0x04; }
		}

		protected override void WriteSubCmd() {
			this.EncodeUInt(this.gumpUid);
			this.EncodeInt(this.buttonId);
		}
	}

	public sealed class PlaySoundEffectOutPacket : GameOutgoingPacket {
		ushort sound;
		ushort x, y;
		sbyte z;

		public void Prepare(IPoint3D source, ushort sound) {
			source = source.TopPoint;
			this.sound = sound;
			this.x = source.X;
			this.y = source.Y;
			this.z = source.Z;
		}

		public override byte Id {
			get { return 0x54; }
		}

		protected override void Write() {
			this.EncodeByte(0x01); //mode (0x00=quiet, repeating, 0x01=single normally played sound effect) . TODO? enable the 0 mode
			this.EncodeUShort(this.sound);
			this.EncodeZeros(2); //unknown3 (speed/volume modifier? Line of sight stuff?) 
			this.EncodeUShort(this.x);
			this.EncodeUShort(this.y);
			this.EncodeByte(0); //z as short, again?
			this.EncodeSByte(this.z);
		}
	}


	/**
		Shows the character doing a particular animation.
			
		[ I did quite a lot of testing of this packet, since none of the packet guides seemed to have correct information about it.
			All testing on this packet was done with the AOS 2D client.
			Things that Jerrith's has wrong about this packet:
				Anim is a byte, not a ushort, and it starts at pos 6. If anim is greater than the # of valid anims for that character's model (It's out-of-range), then anim 0 is drawn. The byte at pos 5 doesn't seem to do anything (If it were a ushort, the client would display out-of-range-anim# behavior if you set byte 5 to something; It doesn't, it only looks at byte 6 for the anim#.). However, there are also anims which may not exist - for human (and equippables) models, these draw as some other anim instead. There are anims in the 3d client which don't exist in the 2d, too.
				Byte 5 is ignored by the client (But I said that already).
				Jerrith's "direction" variable isn't direction at all; The client knows the direction the character is facing already. It isn't a byte either. Read the next line:
				Bytes 7 and 8, which Jerrith's calls "unknown" and "direction," are actually a ushort which I call numBackwardsFrames. What it does is really weird - it's the number of frames to draw when the anim is drawn backwards, IF 'backwards' is true. If you send 0, it draws a blank frame (i.e. the character vanishes, but reappears after the frame is over). If you send something greater than the number of frames in the anim, then it draws a blank frame for both forwards AND backwards! It's beyond me how that behavior could have been useful for anything, but that's what it does...
				What Jerrith's calls "repeat" isn't the number of times to repeat the anim, it is the number of anims to draw, starting with 'anim'. If you specify 0 for this, though, it will continue drawing anims until the cows come home (and it apparently goes back to 0 after it draws the last one!).
				Jerrith's has "forward/backward" correct, although it doesn't mention what happens if you have both this and 'repeat' (undo) set to true (In that case it runs in reverse, and then runs forward).
				What Jerrith's calls "repeat flag" doesn't make the anim repeat. What it REALLY does is make the anim run once, and then run in reverse immediately after (so I call it "undo" :P). If 'backwards' is true, then it's going to run backwards and then forwards, but it still looks like it's undo'ing the anim it just drew, so :).
				Jerrith's has 'frame delay' correct.
		]
			
			
			
		@param anim The animation the character should perform.
		@param numBackwardsFrames If backwards is true, this is the number of frames to do when going backwards. 0 makes the character blink invisible for the first frame of the backwards anim. Anything above the number of frames in that particular anim makes the character blink invisible for both forwards and backwards, but ONLY if backwards is true. This is rather bizarre, considering how numAnims works, and how various anims have different frame amounts.
		@param numAnims The number of anims to do, starting with 'anim' and counting up from there. 0 means 'keep going forever'. If this exceeds the number of valid anims, it wraps back around to anim 0 and keeps going. So if you specify 0, it really WILL run forever, or at least until another anim is drawn for that character, including if it isn't through an anim packet (like if it moves or turns or something).
		@param backwards If true, perform the animation in reverse.
		@param undo If true, the animation is run, and then the opposite of the animation is run, and this is done for each anim which would be drawn (according to numAnims). If backwards is also true, then you will see the animation performed backwards and then forwards. Conversely, if backwards is false, then with undo true you will see the animation performed forwards and then backwards.
		@param frameDelay The delay time between animations: 0 makes the animation fastest, higher values make it proportionally slower (0xff is like watching glaciers drift).
			I timed some different values with a normal anim 14, which has 7 frames. (Using only my eyes and the windows clock, mind you, so it isn't super-accurate or anything.
				(~ means approximately)
				0: ~2s
				1: ~3s
				5: ~5s (4.5s?)
				10: ~8s
				50: ~36s
					
				What I gathered from those results:
				.25-.3 second delay between frames by default.
				.65-.7 seconds * frameDelay extra delay (for all 7 frames, so if it were .7 then it would be .1*frameDelay per frame)
					
				I did some more math and decided .25 and .7->.1 were probably pretty accurate estimates, and so:
					
				(.25*7)+(.7*frameDelay)=how many seconds UO should spend showing the anim
				(.25*7)+(.7*50)=36.75
				(.25*7)+(.7*1)=2.45
				(.25*7)+(.7*0)=1.75
					
				Or for anims without 7 frames:
				(.25*numFrames)+(.1*numFrames*frameDelay)=how many seconds UO should spend showing the anim
	*/
	public sealed class CharacterAnimationOutPacket : GameOutgoingPacket {
		uint charUid;
		byte dir, frameDelay;
		ushort anim, numAnims;
		bool backwards, undo;

		public void Prepare(AbstractCharacter cre, ushort anim, ushort numAnims, bool backwards, bool undo, byte frameDelay) {
			this.charUid = cre.FlaggedUid;
			this.dir = cre.Dir;
			this.anim = anim;
			this.numAnims = numAnims;
			this.backwards = backwards;
			this.undo = undo;
			this.frameDelay = frameDelay;
		}

		public override byte Id {
			get { return 0x6E; }
		}

		protected override void Write() {
			this.EncodeUInt(this.charUid);
			this.EncodeUShort(this.anim);
			this.EncodeByte(1);
			this.EncodeByte((byte) ((this.dir - 4) & 0x7)); //-4? huh ?
			this.EncodeUShort(this.numAnims);
			this.EncodeBool(this.backwards);
			this.EncodeBool(this.undo);
			this.EncodeByte(this.frameDelay);
		}
	}

	public sealed class GraphicalEffectOutPacket : GameOutgoingPacket {
		uint sourceUid, targetUid, renderMode, hue;
		byte type, speed, duration;
		bool fixedDirection, explodes;
		ushort effect, unk, sourceX, sourceY, targetX, targetY;
		sbyte sourceZ, targetZ;

		public void Prepare(IPoint4D source, IPoint4D target, byte type, ushort effect, byte speed, byte duration, ushort unk, bool fixedDirection, bool explodes, uint hue, RenderModes renderMode) {
			source = source.TopPoint;
			target = target.TopPoint;
			Thing sourceAsThing = source as Thing;
			if (sourceAsThing != null) {
				this.sourceUid = sourceAsThing.FlaggedUid;
				MutablePoint4D p = sourceAsThing.point4d;
				this.sourceX = p.x;
				this.sourceY = p.y;
				this.sourceZ = p.z;
			} else {
				this.sourceUid = 0xffffffff;
				this.sourceX = source.X;
				this.sourceY = source.Y;
				this.sourceZ = source.Z;
			}
			Thing targetAsThing = target as Thing;
			if (targetAsThing != null) {
				this.targetUid = targetAsThing.FlaggedUid;
				MutablePoint4D p = targetAsThing.point4d;
				this.targetX = p.x;
				this.targetY = p.y;
				this.targetZ = p.z;
			} else {
				this.targetUid = 0xffffffff;
				this.targetX = target.X;
				this.targetY = target.Y;
				this.targetZ = target.Z;
			}
			this.type = type;
			this.effect = effect;
			this.speed = speed;
			this.duration = duration;
			this.unk = unk;
			this.fixedDirection = fixedDirection;
			this.explodes = explodes;
			this.hue = hue;
			this.renderMode = (uint) renderMode;
		}

		public override byte Id {
			get { return 0xC0; }
		}

		protected override void Write() {
			this.EncodeByte(this.type);
			this.EncodeUInt(this.sourceUid);
			this.EncodeUInt(this.targetUid);
			this.EncodeUShort(this.effect);
			this.EncodeUShort(this.sourceX);
			this.EncodeUShort(this.sourceY);
			this.EncodeSByte(this.sourceZ);
			this.EncodeUShort(this.targetX);
			this.EncodeUShort(this.targetY);
			this.EncodeSByte(this.targetZ);
			this.EncodeByte(this.speed);
			this.EncodeByte(this.duration);
			this.EncodeUShort(this.unk);
			this.EncodeBool(this.fixedDirection);
			this.EncodeBool(this.explodes);
			this.EncodeUInt(this.hue);
			this.EncodeUInt(this.renderMode);
		}
	}

	public sealed class DraggingOfItemOutPacket : GameOutgoingPacket {
		uint sourceUid, targetUid;
		ushort model, amount, sourceX, sourceY, targetX, targetY;
		sbyte sourceZ, targetZ;

		public void Prepare(IPoint4D source, IPoint4D target, AbstractItem i) {
			source = source.TopPoint;
			target = target.TopPoint;
			Thing sourceAsThing = source as Thing;
			if (sourceAsThing != null) {
				this.sourceUid = sourceAsThing.FlaggedUid;
				MutablePoint4D p = sourceAsThing.point4d;
				this.sourceX = p.x;
				this.sourceY = p.y;
				this.sourceZ = p.z;
			} else {
				this.sourceUid = 0xffffffff;
				this.sourceX = source.X;
				this.sourceY = source.Y;
				this.sourceZ = source.Z;
			}
			Thing targetAsThing = target as Thing;
			if (targetAsThing != null) {
				this.targetUid = targetAsThing.FlaggedUid;
				MutablePoint4D p = targetAsThing.point4d;
				this.targetX = p.x;
				this.targetY = p.y;
				this.targetZ = p.z;
			} else {
				this.targetUid = 0xffffffff;
				this.targetX = target.X;
				this.targetY = target.Y;
				this.targetZ = target.Z;
			}
			this.model = i.Model;
			this.amount = i.ShortAmount;
		}

		public override byte Id {
			get { return 0x23; }
		}

		protected override void Write() {
			this.EncodeUShort(this.model);
			this.EncodeZeros(3);
			this.EncodeUShort(this.amount);
			this.EncodeUInt(this.sourceUid);
			this.EncodeUShort(this.sourceX);
			this.EncodeUShort(this.sourceY);
			this.EncodeSByte(this.sourceZ);
			this.EncodeUInt(this.targetUid);
			this.EncodeUShort(this.targetX);
			this.EncodeUShort(this.targetY);
			this.EncodeSByte(this.targetZ);
		}
	}

	public sealed class ExtendedStatsOutPacket : GeneralInformationOutPacket {
		byte statLockByte;
		uint flaggedUid;

		public void Prepare(uint flaggedUid, byte statLockByte) {
			this.flaggedUid = flaggedUid;
			this.statLockByte = statLockByte;
		}

		public override ushort SubCmdId {
			get { return 0x19; }
		}

		protected override void WriteSubCmd() {
			this.EncodeByte(0x02);
			this.EncodeUInt(this.flaggedUid);
			this.EncodeByte(0);
			this.EncodeByte(this.statLockByte);
		}
	}

	public sealed class ResurrectionMenuOutPacket : GameOutgoingPacket {
		byte action;

		public void Prepare(byte action) {
			this.action = action;
		}

		public override byte Id {
			get { return 0x2c; }
		}

		protected override void Write() {
			this.EncodeByte(this.action);
		}
	}

	public sealed class DisplayDeathActionOutPacket : GameOutgoingPacket {
		uint charUid, corpseUid;

		public void Prepare(uint charUid, AbstractItem corpse) {
			this.charUid = charUid;
			if (corpse == null) {
				this.corpseUid = 0xffffffff;
			} else {
				this.corpseUid = corpse.FlaggedUid;
			}
		}

		public override byte Id {
			get { return 0xAF; }
		}

		protected override void Write() {
			this.EncodeUInt(this.charUid);
			this.EncodeUInt(this.corpseUid);
			this.EncodeZeros(4);
		}
	}

	public sealed class AddPartyMembersOutPacket : GeneralInformationOutPacket {
		List<uint> members = new List<uint>();

		public void Prepare(IEnumerable<AbstractCharacter> members) {
			this.members.Clear();
			foreach (AbstractCharacter ch in members) {
				this.members.Add(ch.FlaggedUid);
			}
		}

		public override ushort SubCmdId {
			get { return 0x6; }
		}

		protected override void WriteSubCmd() {
			this.EncodeByte(0x01);
			int n = this.members.Count;
			this.EncodeByte((byte) n);
			for (int i = 0; i < n; i++) {
				this.EncodeUInt(this.members[i]);
			}
		}
	}

	public sealed class RemoveAPartyMemberOutPacket : GeneralInformationOutPacket {
		List<uint> members = new List<uint>();

		public void PrepareEmpty(AbstractCharacter self) {
			this.members.Clear();
			this.members.Add(self.FlaggedUid);
		}

		public void Prepare(AbstractCharacter removedMember, IEnumerable<AbstractCharacter> members) {
			this.members.Clear();
			this.members.Add(removedMember.FlaggedUid);
			if (members != null) {
				foreach (AbstractCharacter ch in members) {
					this.members.Add(ch.FlaggedUid);
				}
			}
		}

		public override ushort SubCmdId {
			get { return 0x6; }
		}

		protected override void WriteSubCmd() {
			this.EncodeByte(0x02);
			int n = this.members.Count;
			this.EncodeByte((byte) (n - 1));
			for (int i = 0; i < n; i++) {
				this.EncodeUInt(this.members[i]);
			}
		}
	}

	public sealed class TellPartyMemberAMessageOutPacket : GeneralInformationOutPacket {
		uint sourceUid;
		string message;

		public void Prepare(uint sourceUid, string message) {
			this.sourceUid = sourceUid;
			this.message = message;
		}

		public override ushort SubCmdId {
			get { return 0x6; }
		}

		protected override void WriteSubCmd() {
			this.EncodeByte(0x03);
			this.EncodeUInt(this.sourceUid);
			this.EncodeBigEndianUnicodeString(this.message);
		}
	}

	public sealed class TellFullPartyAMessageOutPacket : GeneralInformationOutPacket {
		uint sourceUid;
		string message;

		public void Prepare(uint sourceUid, string message) {
			this.sourceUid = sourceUid;
			this.message = message;
		}

		public override ushort SubCmdId {
			get { return 0x6; }
		}

		protected override void WriteSubCmd() {
			this.EncodeByte(0x04);
			this.EncodeUInt(this.sourceUid);
			this.EncodeBigEndianUnicodeString(this.message);
		}
	}

	public sealed class PartyInvitationOutPacket : GeneralInformationOutPacket {
		uint leaderUid;

		public void Prepare(uint leaderUid) {
			this.leaderUid = leaderUid;
		}

		public override ushort SubCmdId {
			get { return 0x6; }
		}

		protected override void WriteSubCmd() {
			this.EncodeByte(0x07);
			this.EncodeUInt(this.leaderUid);
		}
	}

	public sealed class EnableMapDiffFilesOutPacket : GeneralInformationOutPacket {
		byte facetsCount;
		List<int> mapPatches = new List<int>();
		List<int> staticsPatches = new List<int>();

		public void Prepare() {
			this.facetsCount = (byte) Regions.Map.GetFacetCount();
			this.mapPatches.Clear();
			this.staticsPatches.Clear();
			for (int i = 0; i < this.facetsCount; i++) {
				this.mapPatches.Add(Regions.Map.GetFacetPatchesMapCount(i));
				this.staticsPatches.Add(Regions.Map.GetFacetPatchesStaticsCount(i));
			}
		}

		public override ushort SubCmdId {
			get { return 0x18; }
		}

		protected override void WriteSubCmd() {
			this.EncodeByte(this.facetsCount);
			for (int i = 0; i < this.facetsCount; i++) {
				this.EncodeInt(this.mapPatches[i]);
				this.EncodeInt(this.staticsPatches[i]);
			}
		}
	}

	public sealed class ClientVersionOutPacket : DynamicLengthOutPacket {
		public override byte Id {
			get { return 0xBD; }
		}

		protected override void WriteDynamicPart() {
		}
	}

	public sealed class PersonalLightLevelOutPacket : GameOutgoingPacket {
		uint charUid;
		byte lightLevel;

		public void Prepare(uint charUid, byte lightLevel) {
			this.charUid = charUid;
			this.lightLevel = lightLevel;
		}

		public override byte Id {
			get { return 0x4E; }
		}

		protected override void Write() {
			this.EncodeUInt(this.charUid);
			this.EncodeByte(this.lightLevel);
		}
	}

	public sealed class OverallLightLevelOutPacket : GameOutgoingPacket {
		byte lightLevel;

		public void Prepare(byte lightLevel) {
			this.lightLevel = lightLevel;
		}

		public override byte Id {
			get { return 0x4f; }
		}

		protected override void Write() {
			this.EncodeByte(this.lightLevel);
		}
	}

	public sealed class QuestArrowOutPacket : GameOutgoingPacket {
		bool active;
		ushort xPos, yPos;

		public void Prepare(bool active, ushort xPos, ushort yPos) {
			this.active = active;
			this.xPos = xPos;
			this.yPos = yPos;
		}

		public void Prepare(bool active, IPoint2D position) {
			this.active = active;
			this.xPos = position.X;
			this.yPos = position.Y;
		}

		public override byte Id {
			get {
				return 0xba;
			}
		}

		protected override void Write() {
			this.EncodeBool(this.active);
			this.EncodeUShort(this.xPos);
			this.EncodeUShort(this.yPos);
		}
	}
}