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
using System.Collections;
using System.Reflection;
using SteamEngine.Timers;
using SteamEngine.Common;
using SteamEngine.Packets;

namespace SteamEngine.CompiledScripts {
	public partial class Character : AbstractCharacter {
		MemoryCollection memories = null;
		Skill[] skills;//this CAN be null, altough it usually isn't
		float weight;

		public override sealed byte FlagsToSend {
			get {
				//We don't want to send 0x02 if it is set, so we &0xfd to get rid of it.
				int ret = 0;	//0xfd is all bits except 0x02.
				if (IsInvisible) {
					ret |= 0x80;
				}
				if (Flag_WarMode) {
					ret |= 0x40;
				}
				return (byte) ret;
			}
		}

		/**
			You can't set this, use the Flag_* properties to set flags.
		*/
		public ushort Flags {
			get {
				return flags;
			}
		}

		//TODO: Make this do something
		public bool Flag_Dead {
			get {
				return ((flags&0x0002)==0x0002);
			}
			private set {
				flags = (ushort) (value?(flags|0x0002):(flags&~0x0002));
			}
		}

		public bool Flag_Insubst {
			get {
				return ((flags&0x0004)==0x0004);
			}
			set {
				ushort newFlags = (ushort) (value?(flags|0x0004):(flags&~0x0004));
				if (newFlags != flags) {
					NetState.AboutToChangeFlags(this);
					flags = newFlags;
				}
			}
		}

		public bool Flag_InvisByMagic {
			get {
				return ((flags&0x0008)==0x0008);
			}
			set {
				ushort newFlags = (ushort) (value?(flags|0x0008):(flags&~0x0008));
				if (newFlags != flags) {
					NetState.AboutToChangeFlags(this);
					flags = newFlags;
				}
			}
		}

		public bool Flag_Hidden {
			get {
				return ((flags&0x0010)==0x0010);
			}
			set {
				ushort newFlags = (ushort) (value?(flags|0x0010):(flags&~0x0010));
				if (newFlags != flags) {
					NetState.AboutToChangeFlags(this);
					flags = newFlags;
				}
			}
		}

		public override bool Flag_WarMode {
			get {
				return ((flags&0x0020)==0x0020);
			}
			set {
				if (Flag_WarMode!=value) {
					Trigger_WarModeChange(value);
				}
			}
		}

		public override sealed bool IsInvisible {
			get {
				return (Flag_InvisByMagic || Flag_Hidden || Flag_Insubst || Flag_Disconnected);
			}
		}

		//Flag_Riding is split into two properties because scripts should not be able to set it,
		//(they should set Mount instead) but it needs to be settable from within Character itself.
		public override bool Flag_Riding {
			get {
				return ((flags&0x2000)==0x2000);
			}
		}

		//method: Trigger_WarModeChange
		//this method fires the @warmodechange trigger
		public void Trigger_WarModeChange(bool changeTo) {
			ThrowIfDeleted();

			bool cancel=false;
			ScriptArgs sa;
			if (changeTo) {
				sa = new ScriptArgs(this, 1);
			} else {
				sa = new ScriptArgs(this, 0);
			}
			cancel=TryCancellableTrigger(TKwarModeChange, sa);
			if (!cancel) {
				//@warModeChange did not return 1
				On_WarModeChange(changeTo);
			}
		}

		public static readonly TriggerKey TKwarModeChange=TriggerKey.Get("warModeChange");

		public virtual void On_WarModeChange(bool changeTo) {
			if ((flags&0x40)==0x40) {
				//we had warmode on
				if (!changeTo) {
					NetState.AboutToChangeFlags(this);
					flags=(ushort) (flags&~0x40);
					if (IsPlayer && Conn!=null) {
						//PacketSender.PrepareWarMode(this);
						//PacketSender.SendTo(Conn, true);

						Packets.Prepared.SendWarMode(Conn, this);
					}
				}//else no change
			} else {
				if (changeTo) {
					//change it
					NetState.AboutToChangeFlags(this);
					flags=(ushort) (flags|0x40);
					if (IsPlayer && Conn!=null) {
						//PacketSender.PrepareWarMode(this);
						//PacketSender.SendTo(Conn, true);
						Packets.Prepared.SendWarMode(Conn, this);
					}
				}
			}
		}

		private void SetFlag_Riding(bool value) {
			flags=(ushort) (value?(flags|0x2000):(flags&~0x2000));
		}


		/**
			The character riding this one, or null if this character doesn't have a rider.
		*/
		public AbstractCharacter Rider {
			get {
				if (mountorrider!=null) {
					if (!Flag_Riding) {
						if (mountorrider.IsDeleted) {
							NetState.AboutToChangeMount(this);
							mountorrider=null;
						} else {
							return mountorrider;
						}
					}
				}
				return null;
			}
		}

		/**
			The mount this character is riding, or null if this character isn't riding a mount.
		*/
		public override sealed AbstractCharacter Mount {
			get {
				if (mountorrider!=null) {
					if (Flag_Riding) {
						if (mountorrider.IsDeleted) {
							NetState.AboutToChangeMount(this);
							SetFlag_Riding(false);
							mountorrider=null;
						}
					}
				}
				return mountorrider;
			}
			set {
				NetState.AboutToChangeMount(this);
				if (value==null) {	//automatically call Dismount if 'mount=null;' is done.
					if (mountorrider!=null && !mountorrider.IsDeleted) {
						Dismount();
					} else {
						NetState.AboutToChangeMount(this);
						SetFlag_Riding(false);
						mountorrider=null;
					}
					return;
				}
				if (mountorrider!=null) {
					if (!mountorrider.IsDeleted) {
						Dismount();
					}
				}
				if (value.Flag_Riding) {
					throw new ArgumentException("You can't ride something that's riding something else!");
				} else {
					mountorrider=(Character) value;
					SetFlag_Riding(true);
					mountorrider.mountorrider=this;
					mountorrider.Disconnect();
				}
			}
		}

		public void Dismount() {
			NetState.AboutToChangeMount(this);
			if (Flag_Riding && mountorrider!=null) {
				if (mountorrider.mountorrider==this) {
					//mountorrider.AboutToChange();

					//move it to where we are
					mountorrider.P(this);
					mountorrider.Direction=Direction;

					//set it's rider to null
					mountorrider.mountorrider=null;
					mountorrider.Reconnect();
				} else {
					Logger.WriteCritical("Dismount(): Mounted character doesn't know who's riding it or thinks the wrong person is (["+this+"] is, but the mount thinks ["+mountorrider.mountorrider+"] is)!");
				}
			}
			SetFlag_Riding(false);
			mountorrider=null;
		}

		public override bool CanSeeVisibility(Thing target) {
			if (target == null) {
				return false;
			}
			if (target.IsDeleted) {
				return false;
			}
			if (target.IsInvisible) {
				if (!target.Flag_Disconnected) {
					return this.IsGM();
				} else {
					return false;
				}
			}
			return true;
		}

		public override byte StatLockByte {
			get {
				return statLockByte;
			}
		}
		public override StatLockType StrLock {
			get {
				return (StatLockType) ((statLockByte>>4)&0x3);
			}
			set {
				if (value != StrLock) {
					statLockByte=(byte) ((statLockByte&0xCF)+((((byte) value)&0x3)<<4));
					GameConn c = Conn;
					if (c != null) {
						PacketSender.PrepareStatLocks(this);
						PacketSender.SendTo(c, true);
					}
				}
			}
		}
		public override StatLockType DexLock {
			get {
				return (StatLockType) ((statLockByte>>2)&0x3);
			}
			set {
				if (value != DexLock) {
					statLockByte=(byte) ((statLockByte&0xF3)+((((byte) value)&0x3)<<2));
					GameConn c = Conn;
					if (c != null) {
						PacketSender.PrepareStatLocks(this);
						PacketSender.SendTo(c, true);
					}
				}
			}
		}
		public override StatLockType IntLock {
			get {
				return (StatLockType) ((statLockByte)&0x3);
			}
			set {
				if (value != IntLock) {
					statLockByte=(byte) ((statLockByte&0xFC)+((((byte) value)&0x3)));
					GameConn c = Conn;
					if (c != null) {
						PacketSender.PrepareStatLocks(this);
						PacketSender.SendTo(c, true);
					}
				}
			}
		}

		public override short Hits {
			get {
				return hitpoints;
			}
			set {
				if (value != hitpoints) {
					if (!Flag_Dead && value == 0) {
						CauseDeath((Character) Globals.SrcCharacter);
					} else {
						NetState.AboutToChangeHitpoints(this);
						hitpoints=value;
					}
				}
			}
		}

		public override short MaxHits {
			get {
				return maxHitpoints;
			}
			set {
				if (value != maxHitpoints) {
					NetState.AboutToChangeHitpoints(this);
					maxHitpoints=value;
				}
			}
		}

		public override short Mana {
			get {
				return mana;
			}
			set {
				if (value != mana) {
					NetState.AboutToChangeMana(this);
					mana=value;
				}
			}
		}

		public override short MaxMana {
			get {
				return maxMana;
			}
			set {
				if (value != maxMana) {
					NetState.AboutToChangeMana(this);
					maxMana=value;
				}
			}
		}

		public override short Stam {
			get {
				return stamina;
			}
			set {
				if (value != stamina) {
					NetState.AboutToChangeStamina(this);
					stamina=value;
				}
			}
		}

		public override short MaxStam {
			get {
				return maxStamina;
			}
			set {
				if (value != maxStamina) {
					NetState.AboutToChangeStamina(this);
					maxStamina=value;
				}
			}
		}

		public override short Str {
			get {
				return strength;
			}
			set {
				if (value != strength) {
					NetState.AboutToChangeStats(this);
					strength=value;
				}
			}
		}


		public override short Dex {
			get {
				return dexterity;
			}
			set {
				if (value != dexterity) {
					NetState.AboutToChangeStats(this);
					dexterity=value;
				}
			}
		}

		public override short Int {
			get {
				return intelligence;
			}
			set {
				if (value != intelligence) {
					NetState.AboutToChangeStats(this);
					intelligence=value;
				}
			}
		}

		public override short ExtendedStatusNum1 {
			get {
				return 0;
			}
		}

		public override short ExtendedStatusNum2 {
			get {
				return 0;
			}
		}

		public override short ExtendedStatusNum3 {
			get {
				return 0;
			}
		}

		public override short ExtendedStatusNum5 {
			get {
				return 0;
			}
		}

		public override long TithingPoints {
			get {
				return tithingPoints;
			}
			set {
				if (value != tithingPoints) {
					NetState.AboutToChangeStats(this);
					tithingPoints=value;
				}
			}
		}

		public override short ExtendedStatusNum6 {
			get {
				return 0;
			}
		}

		public override short ExtendedStatusNum7 {
			get {
				return 0;
			}
		}

		public override ulong Gold {
			get {
				return 0;
			}
		}

		#region resisty
		private static TagKey resistMagicTK = TagKey.Get("_resistMagic_");
		private static TagKey resistFireTK = TagKey.Get("_resistFire_");
		private static TagKey resistElectricTK = TagKey.Get("_resistElectric_");
		private static TagKey resistAcidTK = TagKey.Get("_resistAcid_");
		private static TagKey resistColdTK = TagKey.Get("_resistCold_");
		private static TagKey resistPoisonTK = TagKey.Get("_resistPoison_");
		private static TagKey resistMysticalTK = TagKey.Get("_resistMystical_");
		private static TagKey resistPhysicalTK = TagKey.Get("_resistPhysical_");
		private static TagKey resistSlashingTK = TagKey.Get("_resistSlashing_");
		private static TagKey resistStabbingTK = TagKey.Get("_resistStabbing_");
		private static TagKey resistBluntTK = TagKey.Get("_resistBlunt_");
		private static TagKey resistArcheryTK = TagKey.Get("_resistArchery_");
		private static TagKey resistBleedTK = TagKey.Get("_resistBleed_");
		private static TagKey resistSummonTK = TagKey.Get("_resistSummon_");
		private static TagKey resistDragonTK = TagKey.Get("_resistDragon_");
		private static TagKey resistParalyseTK = TagKey.Get("_resistParalyse_");

		public int ResistMagic {
			get {
				int dynamicPart = Convert.ToInt32(this.GetTag(resistMagicTK));
				return dynamicPart + Def.ResistMagic;
			}
			set {
				int dynamicPart = value - Def.ResistMagic;
				if (dynamicPart != 0) {
					this.SetTag(resistMagicTK, dynamicPart);
				}
			}
		}

		public int ResistFire {
			get {
				int dynamicPart = Convert.ToInt32(this.GetTag(resistFireTK));
				return dynamicPart + Def.ResistFire;
			}
			set {
				int dynamicPart = value - Def.ResistFire;
				if (dynamicPart != 0) {
					this.SetTag(resistFireTK, dynamicPart);
				}
			}
		}

		public int ResistElectric {
			get {
				int dynamicPart = Convert.ToInt32(this.GetTag(resistElectricTK));
				return dynamicPart + Def.ResistElectric;
			}
			set {
				int dynamicPart = value - Def.ResistElectric;
				if (dynamicPart != 0) {
					this.SetTag(resistElectricTK, dynamicPart);
				}
			}
		}

		public int ResistAcid {
			get {
				int dynamicPart = Convert.ToInt32(this.GetTag(resistAcidTK));
				return dynamicPart + Def.ResistAcid;
			}
			set {
				int dynamicPart = value - Def.ResistAcid;
				if (dynamicPart != 0) {
					this.SetTag(resistAcidTK, dynamicPart);
				}
			}
		}

		public int ResistCold {
			get {
				int dynamicPart = Convert.ToInt32(this.GetTag(resistColdTK));
				return dynamicPart + Def.ResistCold;
			}
			set {
				int dynamicPart = value - Def.ResistCold;
				if (dynamicPart != 0) {
					this.SetTag(resistColdTK, dynamicPart);
				}
			}
		}

		public int ResistPoison {
			get {
				int dynamicPart = Convert.ToInt32(this.GetTag(resistPoisonTK));
				return dynamicPart + Def.ResistPoison;
			}
			set {
				int dynamicPart = value - Def.ResistPoison;
				if (dynamicPart != 0) {
					this.SetTag(resistPoisonTK, dynamicPart);
				}
			}
		}

		public int ResistMystical {
			get {
				int dynamicPart = Convert.ToInt32(this.GetTag(resistMysticalTK));
				return dynamicPart + Def.ResistMystical;
			}
			set {
				int dynamicPart = value - Def.ResistMystical;
				if (dynamicPart != 0) {
					this.SetTag(resistMysticalTK, dynamicPart);
				}
			}
		}

		public int ResistPhysical {
			get {
				int dynamicPart = Convert.ToInt32(this.GetTag(resistPhysicalTK));
				return dynamicPart + Def.ResistPhysical;
			}
			set {
				int dynamicPart = value - Def.ResistPhysical;
				if (dynamicPart != 0) {
					this.SetTag(resistPhysicalTK, dynamicPart);
				}
			}
		}

		public int ResistSlashing {
			get {
				int dynamicPart = Convert.ToInt32(this.GetTag(resistSlashingTK));
				return dynamicPart + Def.ResistSlashing;
			}
			set {
				int dynamicPart = value - Def.ResistSlashing;
				if (dynamicPart != 0) {
					this.SetTag(resistSlashingTK, dynamicPart);
				}
			}
		}

		public int ResistStabbing {
			get {
				int dynamicPart = Convert.ToInt32(this.GetTag(resistStabbingTK));
				return dynamicPart + Def.ResistStabbing;
			}
			set {
				int dynamicPart = value - Def.ResistStabbing;
				if (dynamicPart != 0) {
					this.SetTag(resistStabbingTK, dynamicPart);
				}
			}
		}

		public int ResistBlunt {
			get {
				int dynamicPart = Convert.ToInt32(this.GetTag(resistBluntTK));
				return dynamicPart + Def.ResistBlunt;
			}
			set {
				int dynamicPart = value - Def.ResistBlunt;
				if (dynamicPart != 0) {
					this.SetTag(resistBluntTK, dynamicPart);
				}
			}
		}

		public int ResistArchery {
			get {
				int dynamicPart = Convert.ToInt32(this.GetTag(resistArcheryTK));
				return dynamicPart + Def.ResistArchery;
			}
			set {
				int dynamicPart = value - Def.ResistArchery;
				if (dynamicPart != 0) {
					this.SetTag(resistArcheryTK, dynamicPart);
				}
			}
		}

		public int ResistBleed {
			get {
				int dynamicPart = Convert.ToInt32(this.GetTag(resistBleedTK));
				return dynamicPart + Def.ResistBleed;
			}
			set {
				int dynamicPart = value - Def.ResistBleed;
				if (dynamicPart != 0) {
					this.SetTag(resistBleedTK, dynamicPart);
				}
			}
		}

		public int ResistSummon {
			get {
				int dynamicPart = Convert.ToInt32(this.GetTag(resistSummonTK));
				return dynamicPart + Def.ResistSummon;
			}
			set {
				int dynamicPart = value - Def.ResistSummon;
				if (dynamicPart != 0) {
					this.SetTag(resistSummonTK, dynamicPart);
				}
			}
		}

		public int ResistDragon {
			get {
				int dynamicPart = Convert.ToInt32(this.GetTag(resistDragonTK));
				return dynamicPart + Def.ResistDragon;
			}
			set {
				int dynamicPart = value - Def.ResistDragon;
				if (dynamicPart != 0) {
					this.SetTag(resistDragonTK, dynamicPart);
				}
			}
		}

		public int ResistParalyse {
			get {
				int dynamicPart = Convert.ToInt32(this.GetTag(resistParalyseTK));
				return dynamicPart + Def.ResistDragon;
			}
			set {
				int dynamicPart = value - Def.ResistParalyse;
				if (dynamicPart != 0) {
					this.SetTag(resistParalyseTK, dynamicPart);
				}
			}
		}
		#endregion


		public override string PaperdollName {
			get {
				if (title!=null) {
					return string.Concat(Name, ", ", title);
				}
				return Name;
			}
		}

		public string Title {
			get {
				return title;
			}
			set {
				title=value;
			}
		}

		public void Kill() {
			//TODO effect?
			CauseDeath((Character) Globals.SrcCharacter);
		}

		public void CauseDeath(Character killedBy) {
			if (!Flag_Dead) {
				NetState.AboutToChangeHitpoints(this);
				this.hitpoints = 0;

				this.Dismount();

				CorpseDef cd = this.Def.CorpseDef;
				Corpse corpse = null;
				if (cd != null) {
					corpse = (Corpse) cd.Create((IPoint4D) this);
					//NetState.ProcessThing(corpse);
				}

				GameConn myConn = this.Conn;
				if (myConn != null) {
					Prepared.SendYoureDeathMessage(myConn);
				}

				BoundPacketGroup bpg = null;
				foreach (GameConn viewerConn in this.GetMap().GetClientsWhoCanSee(this)) {
					if (myConn != viewerConn) {
						if (bpg == null) {
							bpg = PacketSender.NewBoundGroup();
							PacketSender.PrepareDeathAnim(this, corpse);
						}
						bpg.SendTo(viewerConn);
					}
				}

				if (corpse != null) {
					corpse.InitFromChar(this);
				}

				if (this.IsPlayer) {
					this.OModel = this.Model;
					this.Model = 0x192; //make me ghost
					this.Flag_Insubst = true;
					this.Flag_Dead = true;
				} else {
					this.Delete();
				}

				//NetState.ProcessThing(this);

				if (bpg != null) {
					bpg.Dispose();
				}


			}
		}

		public void Resurrect() {
			if (Flag_Dead) {
				hitpoints = 1;
				this.Model = this.OModel;
				this.Flag_Insubst = false;
				this.Flag_Dead = false;

				Corpse c = null;
				foreach (Thing nearbyThing in this.GetMap().GetThingsInRange(this.X, this.Y, 1)) {
					c = nearbyThing as Corpse;
					if (c.Owner == this) {
						break;
					} else {
						c = null;
					}
				}
				if (c != null) {
					c.ReturnStuffToChar(this);
				}
			}
		}

		private static TagKey oModelTK = TagKey.Get("_omodel_");
		public ushort OModel {
			get {
				return Convert.ToUInt16(this.GetTag(oModelTK));
			}
			set {
				this.SetTag(oModelTK, value);
			}
		}

		public void Go(Region reg) {
			P(reg.P);
			Fix();
			//Update();
		}

		public void Go(Point2D pnt) {
			P(pnt.X, pnt.Y);
			Fix();
			//Update();
		}

		public void Go(Point3D pnt) {
			P(pnt.X, pnt.Y, pnt.Z);
			Fix();
			//Update();
		}

		public void Go(Point4D pnt) {
			P(pnt.X, pnt.Y, pnt.Z, pnt.M);
			Fix();
			//Update();
		}

		public void Go(IPoint2D pnt) {
			P(pnt.X, pnt.Y);
			Fix();
			//Update();
		}

		public void Go(IPoint3D pnt) {
			P(pnt.X, pnt.Y, pnt.Z);
			Fix();
			//Update();
		}

		public void Go(IPoint4D pnt) {
			P(pnt.X, pnt.Y, pnt.Z, pnt.M);
			Fix();
			//Update();
		}

		public void Go(string s) {
			Region reg = Region.Get(s);
			if (reg != null) {
				P(reg.P);
				return;
			}

			//translate s to coordinates
			bool parse=true;
			string constant=null;
			while (parse) {
				parse=false;
				string[] args = Utility.SplitSphereString(s);
				switch (args.Length) {
					case 1: {
							if (constant==null) {
								object o = Constant.GetValue(s);
								if (o is string) {
									Logger.WriteDebug("Resolved constant '"+s+"' to "+o);
									constant=s;
									s=(string) o;
									parse=true;
								} else {
									throw new SanityCheckException("We found a constant named '"+s+"', but it was a "+o.GetType()+" -- we expected a string.");
								}
							} else {
								throw new SanityCheckException("We found a constant named '"+s+"', but it didn't resolve to anything meaningful.");
							}
							break;
						}
					case 2: {
							Go(TagMath.ParseUInt16(args[0]), TagMath.ParseUInt16(args[1]));
							break;
						}
					case 3: {
							Go(TagMath.ParseUInt16(args[0]), TagMath.ParseUInt16(args[1]), TagMath.ParseSByte(args[3]), TagMath.ParseByte(args[4]));
							break;
						}
					case 4: {
							Go(TagMath.ParseUInt16(args[0]), TagMath.ParseUInt16(args[1]), TagMath.ParseSByte(args[3]));
							return;
						}
					default: {
							if (args.Length>4) {
								throw new SanityCheckException("Too many args ("+args.Length+") to Go(\""+s+"\"), expected no more than 4.");
							} else { //if (args.Length<2) {
								throw new SanityCheckException("Too few args ("+args.Length+") to Go(\""+s+"\"), expected at least 2.");
							}
						}
				}
			}
			//Update();
		}

		public void Go(ushort x, ushort y) {
			P(x, y);
			Fix();
			//Update();
		}

		public void Go(ushort x, ushort y, sbyte z) {
			P(x, y, z);
			Fix();
			//Update();
		}

		public void Go(ushort x, ushort y, sbyte z, byte m) {
			P(x, y, z, m);
			Fix();
			//Update();
		}

		public virtual void On_MemoryEquip(Memory memory) {
		}

		public virtual void On_MemoryUnEquip(Memory memory) {
		}

		private static AbstractItemDef backpackDef = null;
		public override sealed AbstractItem AddBackpack() {
			ThrowIfDeleted();
			if (backpackDef == null) {
				backpackDef = ThingDef.Get("i_backpack") as AbstractItemDef;
				if (backpackDef == null) {
					throw new Exception("Unable to find itemdef i_backpack in scripts.");
				}
			}
			AbstractItem i = (AbstractItem) backpackDef.Create(this);
			if (i == null) {
				throw new Exception("Unable to create item i_backpack.");
			}
			return i;
		}

		public AbstractItem NewEquip(AbstractItemDef itemDef) {
			AbstractItem i = Newitem(itemDef);
			if (i.IsEquippable) {
				TryEquip(this, i);
			} else {
				i.Delete();
				throw new Exception("'"+i+"' is not equippable.");
			}
			return i;
		}

		public Memory NewMemory(MemoryDef memoryDef) {
			return memoryDef.Create(this);
		}

		public void AddMemory(Memory memory) {
			if (memories == null) {
				memories = new MemoryCollection(this);
			}
			memories.Add(memory);

			ScriptArgs sa = new ScriptArgs(memory, this);

			TryTrigger(TriggerKey.memoryEquip, sa);
			if (memory.cont == memories) {
				this.On_MemoryEquip(memory);
				if (memory.cont == memories) {
					memory.TryTrigger(TriggerKey.Equip, sa);
					if (memory.cont == memories) {
						memory.On_Equip(this);
					}
				}
			}
		}

		public void RemoveMemory(Memory memory) {
			if (memories != null) {
				ScriptArgs sa = new ScriptArgs(memory, this);
				TryTrigger(TriggerKey.memoryUnEquip, sa);
				On_MemoryUnEquip(memory);
				memory.TryTrigger(TriggerKey.unEquip, sa);
				memory.On_UnEquip(this);
				memories.Remove(memory);
			}
		}

		public void AddLoadedMemory(Memory memory) {
			if (memories == null) {
				memories = new MemoryCollection(this);
			}
			memories.Add(memory);
		}

		public int MemoryCount {
			get {
				if (memories != null) {
					return memories.count;
				}
				return 0;
			}
		}

		//public void Memory SrcFindMemoryByLink() {;
		//	return FindMemoryByLink(Globals.src);
		//}

		public Memory FindMemory(MemoryDef def) {
			if (memories != null) {
				return memories.FindByDef(def);
			}
			return null;
		}

		public Memory FindMemoryByLink(Thing link) {
			if (memories != null) {
				return memories.FindByLink(link);
			}
			return null;
		}

		public Memory FindMemoryByFlag(int flag) {
			if (memories != null) {
				return memories.FindByFlag(flag);
			}
			return null;
		}

		public IEnumerable Memories {
			get {
				if (memories != null) {
					return memories;
				}
				return EmptyEnumerator<Memory>.instance;
			}
		}

		//for sphere compatibility
		public void Equip(Memory memory) {
			AddMemory(memory);
		}

		public void Unequip(Memory memory) {
			RemoveMemory(memory);
		}

		public Memory Memory() {
			return FindMemoryByLink(Globals.SrcCharacter);
		}

		public Memory MemoryFind(Thing link) {
			return FindMemoryByLink(link);
		}

		public Memory MemoryFind(int uid) {
			return FindMemoryByLink(Thing.UidGetThing(uid));
		}

		public Memory MemoryFindType(int flag) {
			return FindMemoryByFlag(flag);
		}

		public override void On_Dupe(Thing t) {
			Character copyFrom = (Character) t;
			if (copyFrom.memories != null) {
				memories = new MemoryCollection(copyFrom.memories);
			}

			if (copyFrom.skills != null) {
				skills = new Skill[copyFrom.skills.Length];
				int n = skills.Length;
				for (ushort i = 0; i<n; i++) {
					skills[i] = new Skill(copyFrom.skills[i], this);
				}
			}
		}

		public override void On_Save(SteamEngine.Persistence.SaveStream output) {
			if (memories != null) {
				memories.SaveMemories();
			}
			if (skills != null) {
				int n = skills.Length;
				for (ushort i = 0; i<n; i++) {
					Skill s = skills[i];
					if (s.RealValue != 0) {
						output.WriteValue(AbstractSkillDef.ById(i).Key, s.RealValue);
					}
					//in sphere, the caps are done by Professions or some such... so this may change in the future
					if (s.Cap != 1000) {
						output.WriteValue("Cap."+AbstractSkillDef.ById(i).Key, s.Cap);
					}
					if (s.Lock != SkillLockType.Increase) {
						if (s.Lock == SkillLockType.Locked) {
							output.WriteLine("SkillLock."+AbstractSkillDef.ById(i).Key+"=Lock");
						} else {//down
							output.WriteLine("SkillLock."+AbstractSkillDef.ById(i).Key+"=Down");
						}
					}
				}
			}

			if (currentSkill != null) {
				output.WriteLine("CurrentSkill="+currentSkill.Defname);
			}

			base.On_Save(output);
		}

		public override void On_Load(PropsSection input) {
			int n = AbstractSkillDef.SkillsCount;
			for (ushort i = 0; i<n; i++) {
				string skillKey = AbstractSkillDef.ById(i).Key;
				PropsLine ps = input.TryPopPropsLine(skillKey);
				if (ps != null) {
					ushort val;
					if (TagMath.TryParseUInt16(ps.value, out val)) {
						InstantiateSkillsArrayIfNull();
						skills[i].RealValue = val;
					} else {
						Logger.WriteError(input.filename, ps.line, "Unrecognised value format.");
					}
				}

				ps = input.TryPopPropsLine("Cap."+skillKey);
				if (ps != null) {
					ushort val;
					if (TagMath.TryParseUInt16(ps.value, out val)) {
						InstantiateSkillsArrayIfNull();
						skills[i].Cap = val;
					} else {
						Logger.WriteError(input.filename, ps.line, "Unrecognised value format.");
					}
				}

				ps = input.TryPopPropsLine("SkillLock."+skillKey);
				if (ps != null) {
					InstantiateSkillsArrayIfNull();
					if (string.Compare("Lock", ps.value, true)==0) {
						InstantiateSkillsArrayIfNull();
						skills[i].Lock = SkillLockType.Locked;
					} else if (string.Compare("Down", ps.value, true)==0) {
						InstantiateSkillsArrayIfNull();
						skills[i].Lock = SkillLockType.Down;
					} else if (string.Compare("Up", ps.value, true)==0) {
						InstantiateSkillsArrayIfNull();
						skills[i].Lock = SkillLockType.Increase;
					} else {
						Logger.WriteError(input.filename, ps.line, "Unrecognised value format.");
					}
				}
			}

			PropsLine pl = input.TryPopPropsLine("CurrentSkill");
			if (pl != null) {
				currentSkill = AbstractSkillDef.ByDefname(pl.value) as SkillDef;
			}

			base.On_Load(input);
		}

		private void InstantiateSkillsArrayIfNull() {
			if (skills == null) {
				skills = new Skill[AbstractSkillDef.SkillsCount];
				int n = skills.Length;
				for (ushort i = 0; i<n; i++) {
					skills[i] = new Skill(i, this);
				}
			}
		}

		public override ISkill[] Skills {
			get {
				InstantiateSkillsArrayIfNull();
				return skills;
			}
		}

		private static TriggerKey skillChangeTK = TriggerKey.Get("skillChange");
		public void Trigger_SkillChange(Skill skill, ushort oldValue) {
			ushort newValue = skill.RealValue;
			ScriptArgs sa = new ScriptArgs(skill.Id, oldValue, newValue, skill);
			this.TryTrigger(skillChangeTK, sa);
			On_SkillChange(skill, oldValue);
		}

		public virtual void On_SkillChange(Skill skill, ushort oldValue) {
			switch ((SkillName) skill.Id) {
				case SkillName.Parry:
				case SkillName.Tactics:
					InvalidateCombatValues();
					break;
			}
		}

		public override void On_Create() {
			base.On_Create();
		}

		[Summary("Sphere's command for starting a skill")]
		public void Skill(int skillId) {
			SelectSkill(skillId);
		}

		[Summary("Start a skill. "
		+ "Is also called when client does the useskill macro")]
		public override void SelectSkill(int skillId) {
			SkillDef skillDef = (SkillDef) AbstractSkillDef.ById(skillId);
			if (skillDef != null) {
				skillDef.Select(this);
			}
		}

		[Summary("Start a skill.")]
		public void SelectSkill(SkillDef skillDef) {
			if (skillDef != null) {
				skillDef.Select(this);
			}
		}

		[Summary("Call the \"Start\" phase oi a skill. This should typically be called from within implementation of the skill's Select phase.")]
		public void StartSkill(int skillId) {
			SkillDef skillDef = (SkillDef) AbstractSkillDef.ById(skillId);
			if (skillDef != null) {
				if (currentSkill != null) {
					currentSkill.Abort(this);
				}
				currentSkill = skillDef;
				skillDef.Start(this);
			}
		}

		[Summary("Call the \"Start\" phase oi a skill. This should typically be called from within implementation of the skill's Select phase.")]
		public void StartSkill(SkillDef skillDef) {
			if (skillDef != null) {
				if (currentSkill != null) {
					currentSkill.Abort(this);
				}
				currentSkill = skillDef;
				skillDef.Start(this);
			}
		}

		private SkillDef currentSkill;

		public SkillDef CurrentSkill {
			get {
				return currentSkill;
			}
			set {
				currentSkill = value;
			}
		}

		//the same as currentskill, only backward compatible with sphere
		public int Action {
			get {
				if (currentSkill == null) {
					return -1;
				}
				return currentSkill.Id;
			}
			set {
				if ((value != currentSkill.Id) || (value < 0) || (value >= AbstractSkillDef.SkillsCount)) {
					AbortSkill();
				}
				//else...? 
			}
		}

		public void AbortSkill() {
			if (currentSkill != null) {
				currentSkill.Abort(this);
				currentSkill = null;
			}
			this.currentSkillParam = null;
			this.currentSkillTarget1 = null;
			this.currentSkillTarget2 = null;
			this.RemoveTimer(skillTimerKey);
		}

		private static TimerKey skillTimerKey = TimerKey.Get("__skillTimer__");

		public class SkillStrokeTimer : Timer {
			public SkillStrokeTimer(TimerKey name)
				: base(name) {
			}

			protected SkillStrokeTimer(SkillStrokeTimer copyFrom, TagHolder assignTo)
				: base(copyFrom, assignTo) {
			}

			protected sealed override Timer Dupe(TagHolder assignTo) {
				return new SkillStrokeTimer(this, assignTo);
			}

			public SkillStrokeTimer(Character obj, TimeSpan time)
				: base(obj, skillTimerKey, time, null) {
			}

			protected sealed override void OnTimeout() {
				Logger.WriteDebug("SkillStrokeTimer OnTimeout on "+this.Obj);
				Character self = this.Obj as Character;
				if (self != null) {
					self.DelayedSkillStroke();
				}
			}
		}

		public void DelaySkillStroke(double seconds, SkillDef skill) {
			Sanity.IfTrueThrow((currentSkill == null)||(currentSkill != skill),
				"DelaySkillStroke of skill "+skill+" called on "+this+", which currently does skill "+this.Action);

			this.RemoveTimer(skillTimerKey);
			new SkillStrokeTimer(this, TimeSpan.FromSeconds(seconds))
				.Enqueue();
		}

		public void DelayedSkillStroke() {
			if (currentSkill != null) {
				currentSkill.Stroke(this);
			}
		}

		public virtual bool On_SkillSelect(int id) {
			return false;
		}

		public virtual bool On_SkillStart(int id) {
			return false;
		}

		public virtual bool On_SkillAbort(int id) {
			return false;
		}

		public virtual bool On_SkillFail(int id) {
			return false;
		}

		public virtual bool On_SkillStroke(int id) {
			return false;
		}

		public virtual bool On_SkillSuccess(int id) {
			return false;
		}

		//public virtual bool On_SkillGain(int id, object arg) {
		//	return false;
		//}
		//
		//public virtual bool On_SkillMakeItem(int id, AbstractItem item) {
		//	return false;
		//}

		private static TagKey tkStealthStepsLeft = TagKey.Get("StealthStepsLeft");

		public int StealthStepsLeft {
			get {
				return Convert.ToInt32(this.GetTag(tkStealthStepsLeft));
			}
			set {
				this.SetTag(tkStealthStepsLeft, value);
			}
		}

		public Character Owner {
			get {
				return owner;
			}
			set {
				if (IsNPC) {
					//AboutToChange();
					owner=value;	//always Character
				} else {
					throw new ScriptException("You cannot give a player an owner.");
				}
			}
		}

		/*
			Method: GM
		
				Toggles the plevel of the account between 1 (player) and the account's max plevel.
				Has no effect on players.
		*/
		public void GM() {
			GameAccount acc=Account;
			if (acc!=null) {
				if (acc.PLevel<acc.MaxPLevel) {
					acc.PLevel=acc.MaxPLevel;
					Conn.WriteLine("GM mode on (Plevel "+acc.PLevel+").");
				} else {
					acc.PLevel=1;
					Conn.WriteLine("GM mode off (Plevel 1).");
				}
			}
		}


		//Method: GM
		//
		//	Sets GM mode to on or off, changing the plevel of the account to either the account's max plevel or 1.
		//	Has no effect on players. Has no effect if GM mode is already at the requested state.
		//
		//Parameters:
		//	i - 1 to turn GM mode on, 0 to turn it off off.
		public void GM(int i) {
			GameAccount acc=Account;
			if (acc!=null) {
				if (i>0) {
					acc.PLevel=acc.MaxPLevel;
					Conn.WriteLine("GM mode on (Plevel "+acc.PLevel+").");
				} else {
					acc.PLevel=1;
					Conn.WriteLine("GM mode off (Plevel 1).");
				}
			}
		}

		[Remark("Check if the current character has plevel greater than 1 (is more than player)")]
		public bool IsGM() {
			return Account.PLevel > 1;
		}

		//for pets
		public bool IsOwnerOf(Character cre) {
			return (cre.IsNPC && cre.Owner!=null && cre.owner.Equals(this));
		}

		public bool IsPet {
			get {
				return (IsNPC && Owner!=null);
			}
		}

		//also for pets
		public bool IsPetOf(Character cre) {
			//return (IsNPC && Owner!=null && Owner.Equals(cre));
			return true;
		}

		public override bool CanEquipItemsOn(AbstractCharacter chr) {
			Character target = (Character) chr;
			return (IsPlevelAtLeast(Globals.plevelOfGM) || (target.Owner==this && CanReach(chr)));
		}

		public override bool CanEquip(AbstractItem i) {
			return true;
		}

		public override bool CanRename(AbstractCharacter to) {
			Character target = (Character) to;
			Character targetOwner = target.owner;
			return (to.IsNPC && targetOwner!=null && targetOwner.Equals(this));
		}

		public virtual bool IsMountable {
			get {
				return MountItem!=0;
			}
		}

		public virtual bool IsMountableBy(AbstractCharacter chr) {
			if (IsMountable && chr.CanReach(this)) {
				if (IsPetOf((Character) chr)) return true;
				if (!IsPet && chr.IsPlevelAtLeast(Globals.plevelOfGM)) return true;
			}
			return false;
		}

		//method: On_DClick
		//Character`s implementation of @Dclick trigger, 
		//paperdoll raising and mounting is/will be handled here
		public override void On_DClick(AbstractCharacter from) {
			ThrowIfDeleted();
			if (from!=null && from.IsPlayer) {
				//PC
				if (from==this && Mount!=null) {
					Dismount();
				} else {
					GameConn conn = from.Conn;
					if (from!=this && IsMountableBy(from)) {
						from.Mount=this;
					} else {
						if (conn!=null) {
							ShowPaperdollTo(from.Conn);
						}
					}
				}
			}
		}

		public override HighlightColor GetHighlightColorFor(AbstractCharacter viewer) {
			//TODO
			return HighlightColor.NoColor;
		}

		public override float Weight {
			get {
				return weight;
			}
		}

		public override void FixWeight() {
			NetState.AboutToChangeStats(this);
			float w = Def.Weight;
			foreach (AbstractItem i in this) {
				if (i!=null) {
					i.FixWeight();
					w += i.Weight;
				}
			}
			weight = w;
		}

		public override void AdjustWeight(float adjust) {
			NetState.AboutToChangeStats(this);
			weight += adjust;
		}

		public override void On_Destroy() {
			if (mountorrider!=null) {
				if (Flag_Riding) {//I am the rider
					if (!mountorrider.IsDeleted) {
						mountorrider.Delete();
					}
					SetFlag_Riding(false);
					mountorrider=null;
				} else {//I am the mount
					mountorrider.Dismount();
				}
			}
			base.On_Destroy();
		}

		[Remark("Message displayed in red - used for importatnt system or ingame messages (warnings, errors etc)")]
		public void RedMessage(string arg) {
			SysMessage(arg, (int) Hues.Red);
		}

		[Remark("Message displayed in blue - used for ingame purposes")]
		public void BlueMessage(string arg) {
			SysMessage(arg, (int) Hues.Blue);
		}

		[Remark("Message displayed in green - used for ingame purposes")]
		public void GreenMessage(string arg) {
			SysMessage(arg, (int) Hues.Green);
		}

		[Remark("Message displayed in green - used for ingame purposes")]
		public void InfoMessage(string arg) {
			SysMessage(arg, (int) Hues.Info);
		}


		public override short Experience {
			get {
				return experience;
			}
			set {
				experience = value;
			}
		}

		public Item Hair {
			get {
				return (Item) FindLayer((byte) Layers.layer_hair);
			}
		}

		public Item Beard {
			get {
				return (Item) FindLayer((byte) Layers.layer_beard);
			}
		}

		//this is to be moved to some separate class
		////Standard sphere effect, for sphere script compatibility
		//public void Effect(byte type, ushort effect, byte speed, byte duration, byte fixedDirection) {
		//    switch (type) {
		//        case 0: {
		//                EffectFrom(Globals.SrcCharacter,
		//                    effect, speed, duration, fixedDirection, 0, 0, 0);
		//                break;
		//            }
		//        case 1: {
		//                LightningEffect();
		//                break;
		//            }
		//        case 2: {
		//                StationaryEffectAt(this.P(), effect, speed, duration, fixedDirection, 0, 0, 0);
		//                break;
		//            }
		//        case 3: {
		//                StationaryEffect(effect, speed, duration, fixedDirection, 0, 0, 0);
		//                break;
		//            }
		//        default: {
		//                Logger.WriteWarning("Unknown effect type '"+type+"'. Sending it anyways.");
		//                PacketSender.PrepareEffect(Globals.SrcCharacter,
		//                    this, type, effect, speed, duration, 0, fixedDirection, 0, 0, 0);
		//                PacketSender.SendToClientsWhoCanSee(this);
		//                break;
		//            }
		//    }
		//}

		////More detailed effects.
		//public void LightningEffectAt(IPoint3D point) {
		//    PacketSender.PrepareEffect(point, point, 1, 0, 0, 0, 0, 0, 0, 0, 0);
		//    PacketSender.SendToClientsWhoCanSee(this);
		//}
		//public void LightningEffect() {
		//    PacketSender.PrepareEffect(this, this, 1, 0, 0, 0, 0, 0, 0, 0, 0);
		//    PacketSender.SendToClientsWhoCanSee(this);
		//}
		//public void StationaryEffect(ushort effect, byte speed, byte duration, byte fixedDirection, byte explodes, uint hue, uint renderMode) {
		//    PacketSender.PrepareEffect(this, this, 3, effect, speed, duration, 0, fixedDirection, explodes, hue, renderMode);
		//    PacketSender.SendToClientsWhoCanSee(this);
		//}
		//public void StationaryEffectAt(IPoint3D point, ushort effect, byte speed, byte duration, byte fixedDirection, byte explodes, uint hue, uint renderMode) {
		//    PacketSender.PrepareEffect(point, point, 2, effect, speed, duration, 0, fixedDirection, explodes, hue, renderMode);
		//    PacketSender.SendToClientsWhoCanSee(this);
		//}
		//public void EffectTo(IPoint3D target, ushort effect, byte speed, byte duration, byte fixedDirection, byte explodes, uint hue, uint renderMode) {
		//    PacketSender.PrepareEffect(this, target, 0, effect, speed, duration, 0, fixedDirection, explodes, hue, renderMode);
		//    PacketSender.SendToClientsWhoCanSee(this);
		//}
		//public void EffectTo(IPoint4D target, ushort effect, byte speed, byte duration, byte fixedDirection, byte explodes, uint hue, uint renderMode) {
		//    PacketSender.PrepareEffect(this, target, 0, effect, speed, duration, 0, fixedDirection, explodes, hue, renderMode);
		//    PacketSender.SendToClientsWhoCanSee(this);
		//}
		//public void EffectFrom(IPoint3D source, ushort effect, byte speed, byte duration, byte fixedDirection, byte explodes, uint hue, uint renderMode) {
		//    PacketSender.PrepareEffect(source, this, 0, effect, speed, duration, 0, fixedDirection, explodes, hue, renderMode);
		//    PacketSender.SendToClientsWhoCanSee(this);
		//}
		//public void EffectFrom(IPoint4D source, ushort effect, byte speed, byte duration, byte fixedDirection, byte explodes, uint hue, uint renderMode) {
		//    PacketSender.PrepareEffect(source, this, 0, effect, speed, duration, 0, fixedDirection, explodes, hue, renderMode);
		//    PacketSender.SendToClientsWhoCanSee(this);
		//}
	}
}