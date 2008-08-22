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
using System.Collections.Generic;
using System.Reflection;
using SteamEngine.Timers;
using SteamEngine.Common;
using SteamEngine.Packets;
using SteamEngine.Persistence;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {
	[Dialogs.ViewableClass]
	public partial class CharacterDef {
		private CharModelInfo charModelInfo;

		private CorpseDef corpseDef;
		public CorpseDef CorpseDef {
			get {
				if (corpseDef == null) {
					corpseDef = ThingDef.FindItemDef(this.CorpseModel) as CorpseDef;
				}
				return corpseDef;
			}
		}

		public CharModelInfo CharModelInfo {
			get {
				ushort model = this.Model;
				if ((this.charModelInfo == null) || (this.charModelInfo.model != model)) {
					this.charModelInfo = CharModelInfo.Get(model);
				}
				return charModelInfo;
			}
		}

		public bool IsHuman {
			get {
				return (this.CharModelInfo.charAnimType & CharAnimType.Human) == CharAnimType.Human;
			}
		}

		public bool IsAnimal {
			get {
				return (this.CharModelInfo.charAnimType & CharAnimType.Animal) == CharAnimType.Animal;
			}
		}

		public bool IsMonster {
			get {
				return (this.CharModelInfo.charAnimType & CharAnimType.Monster) == CharAnimType.Monster;
			}
		}

		public Gender Gender {
			get {
				return this.CharModelInfo.gender;
			}
		}

		public override bool IsMale {
			get {
				return this.CharModelInfo.isMale;
			}
		}

		public override bool IsFemale {
			get {
				return this.CharModelInfo.isFemale;
			}
		}

	}

	[Dialogs.ViewableClass]
	public partial class Character : AbstractCharacter {
		//removed, reworked to use the Skills togeter with abilities in one distionary
		//Skill[] skills;//this CAN be null, altough it usually isn't

		//[Summary("Dictionary of character's (typically player's) abilities")]
		//private Dictionary<AbilityDef, Ability> abilities = null;

		[Summary("Dictionary of character's skills and abilities. Key is (ability/skill)def, value is instance of " +
				"Ability/Skill object of the desired entity")]
		private Dictionary<AbstractDef, object> skillsabilities = null;

		float weight;
		private CharModelInfo charModelInfo;

		public SkillDef currentSkill;
		public IPoint3D currentSkillTarget1 = null;
		public IPoint3D currentSkillTarget2 = null;
		public Object currentSkillParam = null;

		public override sealed byte FlagsToSend {
			get {
				//We don't want to send 0x02 if it is set, so we &0xfd to get rid of it.
				int ret = 0;	//0xfd is all bits except 0x02.
				if (IsNotVisible) {
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

		public bool Flag_Dead {
			get {
				return ((flags & 0x0002) == 0x0002);
			}
			private set {
				flags = (ushort) (value ? (flags | 0x0002) : (flags & ~0x0002));
			}
		}

		public bool Flag_Insubst {
			get {
				return ((flags & 0x0004) == 0x0004);
			}
			set {
				ushort newFlags = (ushort) (value ? (flags | 0x0004) : (flags & ~0x0004));
				if (newFlags != flags) {
					NetState.AboutToChangeVisibility(this);
					flags = newFlags;
				}
			}
		}

		public bool Flag_InvisByMagic {
			get {
				return ((flags & 0x0008) == 0x0008);
			}
			set {
				ushort newFlags = (ushort) (value ? (flags | 0x0008) : (flags & ~0x0008));
				if (newFlags != flags) {
					NetState.AboutToChangeVisibility(this);
					flags = newFlags;
				}
			}
		}

		public bool Flag_Hidden {
			get {
				return ((flags & 0x0010) == 0x0010);
			}
			set {
				ushort newFlags = (ushort) (value ? (flags | 0x0010) : (flags & ~0x0010));
				if (newFlags != flags) {
					NetState.AboutToChangeVisibility(this);
					flags = newFlags;
				}
			}
		}

		public override bool Flag_WarMode {
			get {
				return ((flags & 0x0020) == 0x0020);
			}
			set {
				ushort newFlags = (ushort) (value ? (flags | 0x0020) : (flags & ~0x0020));
				if (newFlags != flags) {
					NetState.AboutToChangeFlags(this);
					flags = newFlags;
					Trigger_WarModeChange();
				}
			}
		}

		private static TriggerKey warModeChangeTK = TriggerKey.Get("warModeChange");
		public void Trigger_WarModeChange() {
			TryTrigger(warModeChangeTK, null);
			On_WarModeChange();
		}

		public virtual void On_WarModeChange() {
		}

		private static TriggerKey visibilityChangeTK = TriggerKey.Get("visibilityChange");
		public void Trigger_VisibilityChange() {
			TryTrigger(visibilityChangeTK, null);
			On_VisibilityChange();
		}

		public virtual void On_VisibilityChange() {
		}

		public override sealed bool IsNotVisible {
			get {
				return (Flag_InvisByMagic || Flag_Hidden || Flag_Insubst || Flag_Disconnected);
			}
		}

		//Flag_Riding is split into two properties because scripts should not be able to set it,
		//(they should set Mount instead) but it needs to be settable from within Character itself.
		public override bool Flag_Riding {
			get {
				return ((flags & 0x2000) == 0x2000);
			}
		}

		private void SetFlag_Riding(bool value) {
			flags = (ushort) (value ? (flags | 0x2000) : (flags & ~0x2000));
		}


		/**
			The character riding this one, or null if this character doesn't have a rider.
		*/
		public AbstractCharacter Rider {
			get {
				if (mountorrider != null) {
					if (!Flag_Riding) {
						if (mountorrider.IsDeleted) {
							NetState.AboutToChangeMount(this);
							mountorrider = null;
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
				if (mountorrider != null) {
					if (Flag_Riding) {
						if (mountorrider.IsDeleted) {
							NetState.AboutToChangeMount(this);
							SetFlag_Riding(false);
							mountorrider = null;
						}
					}
				}
				return mountorrider;
			}
			set {
				NetState.AboutToChangeMount(this);
				if (value == null) {	//automatically call Dismount if 'mount=null;' is done.
					if (mountorrider != null && !mountorrider.IsDeleted) {
						Dismount();
					} else {
						NetState.AboutToChangeMount(this);
						SetFlag_Riding(false);
						mountorrider = null;
					}
					return;
				}
				if (mountorrider != null) {
					if (!mountorrider.IsDeleted) {
						Dismount();
					}
				}
				if (value.Flag_Riding) {
					throw new ArgumentException("You can't ride something that's riding something else!");
				} else {
					mountorrider = (Character) value;
					SetFlag_Riding(true);
					mountorrider.mountorrider = this;
					mountorrider.Disconnect();
				}
			}
		}

		public void Dismount() {
			NetState.AboutToChangeMount(this);
			if (Flag_Riding && mountorrider != null) {
				if (mountorrider.mountorrider == this) {
					//mountorrider.AboutToChange();

					//move it to where we are
					mountorrider.P(this);
					mountorrider.Direction = Direction;

					//set it's rider to null
					mountorrider.mountorrider = null;
					mountorrider.Reconnect();
				} else {
					Logger.WriteCritical("Dismount(): Mounted character doesn't know who's riding it or thinks the wrong person is ([" + this + "] is, but the mount thinks [" + mountorrider.mountorrider + "] is)!");
				}
			}
			SetFlag_Riding(false);
			mountorrider = null;
		}

		public override bool CanSeeVisibility(Thing target) {
			Item targetAsItem = target as Item;
			if (targetAsItem != null) {
				return CanSeeVisibility(targetAsItem);
			}
			Character targetAsChar = target as Character;
			if (targetAsChar != null) {
				return CanSeeVisibility(targetAsChar);
			}
			return false;//it was null
		}

		public bool CanSeeVisibility(Character target) {
			if (target == null) {
				return false;
			}
			if (target.IsDeleted) {
				return false;
			}
			//character invisibility has 4 possible reasons: Flag_InvisByMagic || Flag_Hidden || Flag_Insubst || Flag_Disconnected
			if (target.Flag_Disconnected) {
				//TODO: "allshow" mode for GMs
				return false;
			}
			if (target.Flag_Insubst) {//ghosts, insubst GMs
				if (this.IsGM()) {
					return this.Plevel > target.Plevel; //can see other GMs only if they have lowe plevel
				}
				return false;
			}
			if (target.Flag_InvisByMagic) {
				return this.IsGM();
			}
			if (target.Flag_Hidden) {
				if (this.IsGM()) {
					return true;
				} else {
					HiddenHelperPlugin ssp = target.GetPlugin(HidingSkillDef.pluginKey) as HiddenHelperPlugin;
					return ((ssp != null) &&
						(ssp.hadDetectedMe != null) &&
						(ssp.hadDetectedMe.Contains(this)));
				}
			}
			return true;
		}

		public bool CanSeeVisibility(Item target) {
			if (target == null) {
				return false;
			}
			if (target.IsDeleted) {
				return false;
			}
			if (target.IsNotVisible) {
				if (!target.Flag_Disconnected) {
					return this.IsGM();
				}
				return false;
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
				return (StatLockType) ((statLockByte >> 4) & 0x3);
			}
			set {
				if (value != StrLock) {
					statLockByte = (byte) ((statLockByte & 0xCF) + ((((byte) value) & 0x3) << 4));
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
				return (StatLockType) ((statLockByte >> 2) & 0x3);
			}
			set {
				if (value != DexLock) {
					statLockByte = (byte) ((statLockByte & 0xF3) + ((((byte) value) & 0x3) << 2));
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
				return (StatLockType) ((statLockByte) & 0x3);
			}
			set {
				if (value != IntLock) {
					statLockByte = (byte) ((statLockByte & 0xFC) + ((((byte) value) & 0x3)));
					GameConn c = Conn;
					if (c != null) {
						PacketSender.PrepareStatLocks(this);
						PacketSender.SendTo(c, true);
					}
				}
			}
		}

		#region status properties
		public override short Hits {
			get {
				return hitpoints;
			}
			set {
				if (value != hitpoints) {
					if (!Flag_Dead && value < 1) {
						CauseDeath((Character) Globals.SrcCharacter);
					} else {
						NetState.AboutToChangeHitpoints(this);
						hitpoints = value;
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
					maxHitpoints = value;
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
					mana = value;
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
					maxMana = value;
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
					stamina = value;
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
					maxStamina = value;
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
					InvalidateCombatWeaponValues();
					strength = value;
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
					InvalidateCombatWeaponValues();
					dexterity = value;
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
					InvalidateCombatWeaponValues();
					intelligence = value;
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
					tithingPoints = value;
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
		#endregion status properties

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
				return dynamicPart + TypeDef.ResistMagic;
			}
			set {
				int dynamicPart = value - TypeDef.ResistMagic;
				if (dynamicPart != 0) {
					this.SetTag(resistMagicTK, dynamicPart);
				}
			}
		}

		public int ResistFire {
			get {
				int dynamicPart = Convert.ToInt32(this.GetTag(resistFireTK));
				return dynamicPart + TypeDef.ResistFire;
			}
			set {
				int dynamicPart = value - TypeDef.ResistFire;
				if (dynamicPart != 0) {
					this.SetTag(resistFireTK, dynamicPart);
				}
			}
		}

		public int ResistElectric {
			get {
				int dynamicPart = Convert.ToInt32(this.GetTag(resistElectricTK));
				return dynamicPart + TypeDef.ResistElectric;
			}
			set {
				int dynamicPart = value - TypeDef.ResistElectric;
				if (dynamicPart != 0) {
					this.SetTag(resistElectricTK, dynamicPart);
				}
			}
		}

		public int ResistAcid {
			get {
				int dynamicPart = Convert.ToInt32(this.GetTag(resistAcidTK));
				return dynamicPart + TypeDef.ResistAcid;
			}
			set {
				int dynamicPart = value - TypeDef.ResistAcid;
				if (dynamicPart != 0) {
					this.SetTag(resistAcidTK, dynamicPart);
				}
			}
		}

		public int ResistCold {
			get {
				int dynamicPart = Convert.ToInt32(this.GetTag(resistColdTK));
				return dynamicPart + TypeDef.ResistCold;
			}
			set {
				int dynamicPart = value - TypeDef.ResistCold;
				if (dynamicPart != 0) {
					this.SetTag(resistColdTK, dynamicPart);
				}
			}
		}

		public int ResistPoison {
			get {
				int dynamicPart = Convert.ToInt32(this.GetTag(resistPoisonTK));
				return dynamicPart + TypeDef.ResistPoison;
			}
			set {
				int dynamicPart = value - TypeDef.ResistPoison;
				if (dynamicPart != 0) {
					this.SetTag(resistPoisonTK, dynamicPart);
				}
			}
		}

		public int ResistMystical {
			get {
				int dynamicPart = Convert.ToInt32(this.GetTag(resistMysticalTK));
				return dynamicPart + TypeDef.ResistMystical;
			}
			set {
				int dynamicPart = value - TypeDef.ResistMystical;
				if (dynamicPart != 0) {
					this.SetTag(resistMysticalTK, dynamicPart);
				}
			}
		}

		public int ResistPhysical {
			get {
				int dynamicPart = Convert.ToInt32(this.GetTag(resistPhysicalTK));
				return dynamicPart + TypeDef.ResistPhysical;
			}
			set {
				int dynamicPart = value - TypeDef.ResistPhysical;
				if (dynamicPart != 0) {
					this.SetTag(resistPhysicalTK, dynamicPart);
				}
			}
		}

		public int ResistSlashing {
			get {
				int dynamicPart = Convert.ToInt32(this.GetTag(resistSlashingTK));
				return dynamicPart + TypeDef.ResistSlashing;
			}
			set {
				int dynamicPart = value - TypeDef.ResistSlashing;
				if (dynamicPart != 0) {
					this.SetTag(resistSlashingTK, dynamicPart);
				}
			}
		}

		public int ResistStabbing {
			get {
				int dynamicPart = Convert.ToInt32(this.GetTag(resistStabbingTK));
				return dynamicPart + TypeDef.ResistStabbing;
			}
			set {
				int dynamicPart = value - TypeDef.ResistStabbing;
				if (dynamicPart != 0) {
					this.SetTag(resistStabbingTK, dynamicPart);
				}
			}
		}

		public int ResistBlunt {
			get {
				int dynamicPart = Convert.ToInt32(this.GetTag(resistBluntTK));
				return dynamicPart + TypeDef.ResistBlunt;
			}
			set {
				int dynamicPart = value - TypeDef.ResistBlunt;
				if (dynamicPart != 0) {
					this.SetTag(resistBluntTK, dynamicPart);
				}
			}
		}

		public int ResistArchery {
			get {
				int dynamicPart = Convert.ToInt32(this.GetTag(resistArcheryTK));
				return dynamicPart + TypeDef.ResistArchery;
			}
			set {
				int dynamicPart = value - TypeDef.ResistArchery;
				if (dynamicPart != 0) {
					this.SetTag(resistArcheryTK, dynamicPart);
				}
			}
		}

		public int ResistBleed {
			get {
				int dynamicPart = Convert.ToInt32(this.GetTag(resistBleedTK));
				return dynamicPart + TypeDef.ResistBleed;
			}
			set {
				int dynamicPart = value - TypeDef.ResistBleed;
				if (dynamicPart != 0) {
					this.SetTag(resistBleedTK, dynamicPart);
				}
			}
		}

		public int ResistSummon {
			get {
				int dynamicPart = Convert.ToInt32(this.GetTag(resistSummonTK));
				return dynamicPart + TypeDef.ResistSummon;
			}
			set {
				int dynamicPart = value - TypeDef.ResistSummon;
				if (dynamicPart != 0) {
					this.SetTag(resistSummonTK, dynamicPart);
				}
			}
		}

		public int ResistDragon {
			get {
				int dynamicPart = Convert.ToInt32(this.GetTag(resistDragonTK));
				return dynamicPart + TypeDef.ResistDragon;
			}
			set {
				int dynamicPart = value - TypeDef.ResistDragon;
				if (dynamicPart != 0) {
					this.SetTag(resistDragonTK, dynamicPart);
				}
			}
		}

		public int ResistParalyse {
			get {
				int dynamicPart = Convert.ToInt32(this.GetTag(resistParalyseTK));
				return dynamicPart + TypeDef.ResistParalyse;
			}
			set {
				int dynamicPart = value - TypeDef.ResistParalyse;
				if (dynamicPart != 0) {
					this.SetTag(resistParalyseTK, dynamicPart);
				}
			}
		}
		#endregion


		public override string PaperdollName {
			get {
				if (title != null) {
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
				title = value;
			}
		}

		public void Kill() {
			//TODO effect?
			CauseDeath((Character) Globals.SrcCharacter);
		}

		public void On_Death(Character killedBy) {
		}

		private static TriggerKey deathTK = TriggerKey.Get("death");

		public void CauseDeath(Character killedBy) {
			if (!Flag_Dead) {

				this.Trigger_HostileAction(killedBy);
				this.Trigger_Disrupt();

				this.AbortSkill();
				this.Dismount();
				SoundCalculator.PlayDeathSound(this);

				TryTrigger(deathTK, new ScriptArgs(killedBy));
				On_Death(killedBy);

				NetState.AboutToChangeHitpoints(this);
				this.hitpoints = 0;

				CorpseDef cd = this.TypeDef.CorpseDef;
				Corpse corpse = null;
				if (cd != null) {
					corpse = (Corpse) cd.Create((IPoint4D) this);
					//NetState.ProcessThing(corpse);
				}

				GameConn myConn = this.Conn;
				if (myConn != null) {
					Prepared.SendYouAreDeathMessage(myConn);
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
					this.OColor = this.Color;
					this.Model = 0x192; //make me ghost
					this.Color = 0; //?
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
				NetState.AboutToChangeHitpoints(this);
				this.hitpoints = 1;
				this.Model = this.OModel;
				this.ReleaseOModelTag();
				this.Color = this.OColor;
				this.ReleaseOColorTag();
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

				GameConn myConn = this.Conn;
				if (myConn != null) {
					Prepared.SendResurrectMessage(myConn);
				}
			}
		}

		private static TagKey oColorTK = TagKey.Get("_ocolor_");
		public ushort OColor {
			get {
				object o = this.GetTag(oColorTK);
				if (o != null) {
					return Convert.ToUInt16(o);
				}
				return this.Color;
			}
			set {
				this.SetTag(oColorTK, value);
			}
		}

		private void ReleaseOColorTag() {
			this.RemoveTag(oColorTK);
		}

		private static TagKey oModelTK = TagKey.Get("_omodel_");
		public ushort OModel {
			get {
				object o = this.GetTag(oModelTK);
				if (o != null) {
					return Convert.ToUInt16(o);
				}
				return this.Model;
			}
			set {
				this.SetTag(oModelTK, value);
			}
		}

		private void ReleaseOModelTag() {
			this.RemoveTag(oModelTK);
		}

		private static TriggerKey disruptTK = TriggerKey.Get("disrupt");
		public void Trigger_Disrupt() {
			TryTrigger(disruptTK, null);
			On_Disruption();
		}

		public virtual void On_Disruption() {

		}

		private static TriggerKey hostileActionTK = TriggerKey.Get("hostileAction");
		public void Trigger_HostileAction(Character enemy) {
			ScriptArgs sa = new ScriptArgs(enemy);
			TryTrigger(hostileActionTK, sa);
			On_HostileAction(enemy);
		}

		public virtual void On_HostileAction(Character enemy) {

		}

		public void Go(Region reg) {
			P(reg.P);
			Fix();
			//Update();
		}

		public void Go(Point2D pnt) {
			P(pnt.x, pnt.y);
			Fix();
			//Update();
		}

		public void Go(Point3D pnt) {
			P(pnt.x, pnt.y, pnt.z);
			Fix();
			//Update();
		}

		public void Go(Point4D pnt) {
			P(pnt.x, pnt.y, pnt.z, pnt.m);
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

		public Container BackpackAsContainer {
			get {
				return (Container) base.Backpack;
			}
		}

		public override sealed AbstractItem NewItem(IThingFactory arg, uint amount) {
			return Backpack.NewItem(arg, amount);
		}

		public Equippable NewEquip(IThingFactory factory) {
			Thing t = factory.Create(this);
			Equippable i = t as Equippable;
			if (i != null) {
				if (i.Cont != this) {
					i.Delete();
					throw new Exception("'" + i + "' ended not equipped on the char... Wtf?");
				}
				return i;
			}
			if (t != null) {
				t.Delete();//we created something else
			}
			throw new SEException(factory + " did not create an equippable item.");
		}

		public override void On_Dupe(Thing model) {
			Character copyFrom = (Character) model;

			//rewritten using dictionary of skills and abilities
			foreach (Skill skl in copyFrom.Skills) {
				Skill newSkill = new Skill(skl, this); //create a copy
				SkillsAbilities.Add(SkillDef.ById(newSkill.Id), newSkill);//add to the duped char's storage
			}

			//if (copyFrom.skills != null) {
			//    skills = new Skill[copyFrom.skills.Length];
			//    int n = skills.Length;
			//    for (ushort i = 0; i<n; i++) {
			//        skills[i] = new Skill(copyFrom.skills[i], this);
			//    }
			//}
		}

		public override void On_Save(SteamEngine.Persistence.SaveStream output) {
			foreach (Skill s in Skills) {
				string defsKey = AbstractSkillDef.ById(s.Id).Key;
				ushort realValue = s.RealValue;
				if (realValue != 0) {
					output.WriteValue(defsKey, realValue);
				}
				//in sphere, the caps are done by Professions or some such... so this may change in the future
				if (s.Cap != 1000) {
					output.WriteValue("Cap." + defsKey, s.Cap);
				}
				if (s.Lock != SkillLockType.Increase) {
					if (s.Lock == SkillLockType.Locked) {
						output.WriteLine("SkillLock." + defsKey + "=Lock");
					} else {//down
						output.WriteLine("SkillLock." + defsKey + "=Down");
					}
				}
			}

			//now abilities
			foreach (Ability ab in Abilities) {
				int points = ab.Points;
				if (points != 0) {
					output.WriteValue(ab.AbilityDef.Defname, points);
				}
			}

			base.On_Save(output);
		}

		public override void On_Load(PropsSection input) {
			int n = AbstractSkillDef.SkillsCount;
			for (ushort i = 0; i < n; i++) {
				string skillKey = AbstractSkillDef.ById(i).Key;
				PropsLine ps = input.TryPopPropsLine(skillKey);
				if (ps != null) {
					ushort val;
					if (TagMath.TryParseUInt16(ps.value, out val)) {
						SetSkill(i, val);
					} else {
						Logger.WriteError(input.filename, ps.line, "Unrecognised value format.");
					}
				}

				ps = input.TryPopPropsLine("Cap." + skillKey);
				if (ps != null) {
					ushort val;
					if (TagMath.TryParseUInt16(ps.value, out val)) {
						SetSkillCap(i, val);
					} else {
						Logger.WriteError(input.filename, ps.line, "Unrecognised value format.");
					}
				}

				ps = input.TryPopPropsLine("SkillLock." + skillKey);
				if (ps != null) {
					if (string.Compare("Lock", ps.value, true) == 0) {
						SetSkillLockType(i, SkillLockType.Locked);
					} else if (string.Compare("Down", ps.value, true) == 0) {
						SetSkillLockType(i, SkillLockType.Down);
					} else if (string.Compare("Up", ps.value, true) == 0) {
						SetSkillLockType(i, SkillLockType.Increase);
					} else {
						Logger.WriteError(input.filename, ps.line, "Unrecognised value format.");
					}
				}
			}

			//now load abilities (they are saved by defnames)
			foreach (AbilityDef abDef in AbilityDef.AllAbilities) {
				string defName = abDef.Defname;
				PropsLine ps = input.TryPopPropsLine(defName);
				if (ps != null) {
					int val;
					if (TagMath.TryParseInt32(ps.value, out val)) {
						AddNewAbility(abDef, val);
					} else {
						Logger.WriteError(input.filename, ps.line, "Unrecognised value format.");
					}
				}
			}

			base.On_Load(input);
		}

		#region skills
		[Summary("Enumerator of all character's skills")]
		public override IEnumerable<ISkill> Skills {
			get {
				return new SkillsEnumerator(this);
			}
		}

		[Summary("Find the appropriate Skill instance by given ID (look to the dictionary)")]
		public override ISkill GetSkillObject(int id) {
			AbstractSkillDef def = SkillDef.ById(id);
			object retVal = null;
			if (SkillsAbilities.TryGetValue(def, out retVal)) {
				return (ISkill) retVal;//return either Skill or null if not present
			}
			return null;
		}

		[Summary("Check if character has the desired skill (according to the given ID) " +
				"if yes it also instantiates the returning value")]
		public bool HasSkill(int id) {
			AbstractSkillDef def = SkillDef.ById(id);
			return SkillsAbilities.ContainsKey(def);
		}

		[Summary("Find the skill by given ID and set the prescribed value. If the skill is not present " +
				"create a new instance on the character")]
		public override void SetSkill(int id, ushort value) {
			ISkill skl = GetSkillObject(id);
			if (skl != null) {
				skl.RealValue = value;
			} else if (value > 0) { //we wont create a new skill with 0 or <0 number of points!
				AddNewSkill(id, value);
			}
		}

		[Summary("Find the skill by given ID and set the prescribed lock type. If the skill is not present " +
				"create a new instance on the character")]
		public override void SetSkillLockType(int id, SkillLockType type) {
			ISkill skl = GetSkillObject(id);
			if (skl != null) {
				skl.Lock = type;
			} else if ((byte) type != 0) { //we wont create a new skill with default lock type
				AddNewSkill(id, type);
			}
		}

		[Summary("Find the skill by given ID and set the prescribed skillcap. If the skill is not present " +
				"create a new instance on the character")]
		public void SetSkillCap(int id, ushort cap) {
			ISkill skl = GetSkillObject(id);
			if (skl != null) {
				skl.Cap = cap;
			} else if (cap < 1000) { //we wont create a new skill with default cap (1000)
				AddNewSkill(id, 0, cap);
			}
		}

		[Summary("Find the skill by given ID and add the prescribed value. If the skill is not present " +
				"create a new instance on the character")]
		public void AddSkill(int id, int value) {
			ISkill skl = GetSkillObject(id);
			if (skl != null) {
				ushort resultValue = (ushort) Math.Max(0, skl.RealValue + value); //value can be negative!
				skl.RealValue = resultValue;
			} else if (value > 0) { //we wont create a new skill with 0 or <0 number of points!
				AddNewSkill(id, (ushort) value);
			}
		}

		//instantiate new skill and set the specified points, used when the skill does not exist
		private void AddNewSkill(int id, ushort value) {
			AddNewSkill(id, value, 1000); //call the same method with default cap
		}

		//instantiate new skill and set the specified lock type, used when the skill does not exist
		private void AddNewSkill(int id, SkillLockType type) {
			AbstractSkillDef newSkillDef = AbstractSkillDef.ById(id);
			ISkill skl = new Skill((ushort) id, this);
			SkillsAbilities[newSkillDef] = skl; //add to dict
			skl.Lock = type; //set lock type
		}

		//instantiate new skill and set the specified value and cap, used when the skill does not exist
		private void AddNewSkill(int id, ushort value, ushort cap) {
			AbstractSkillDef newSkillDef = AbstractSkillDef.ById(id);
			ISkill skl = new Skill((ushort) id, this);
			SkillsAbilities[newSkillDef] = skl; //add to dict
			skl.RealValue = value; //set value
			skl.Cap = cap; //set lock type
		}

		internal void RemoveSkill(ushort id) {
			AbstractSkillDef aDef = AbstractSkillDef.ById(id);
			SkillsAbilities.Remove(aDef);
		}


		[Summary("Get value of skill with given ID, if the skill is not present return 0")]
		public override ushort GetSkill(int id) {
			ISkill skl = GetSkillObject(id);
			if (skl != null) {
				return skl.RealValue;
			} else {
				return 0;
			}
		}

		[Summary("Get value of the lock type of skill with given ID, if the skill is not present return default")]
		public SkillLockType GetSkillLockType(int id) {
			ISkill skl = GetSkillObject(id);
			if (skl != null) {
				return skl.Lock;
			} else {
				return SkillLockType.Increase; //default value
			}
		}

		[SteamFunction]
		[Summary("Method for new skill storing testing purposes")]
		public static void TrySkills() {
			Character chr = (Character) Globals.SrcCharacter;
			foreach (Skill skl in chr.Skills) {
				chr.SysMessage(skl.Name + "-" + skl.RealValue);
			}
		}

		//check if there are some Skills in the skillsAbilities dictionary
		//private bool HasSomeSkills() {
		//	//try to find one skill in the dictionary (if there is one, there are all...)
		//	SkillDef oneSkillDef = SkillDef.ById(SkillName.Alchemy);
		//	object outVal = null;
		//	return SkillsAbilities.TryGetValue(oneSkillDef, out outVal);
		//}

		//private void InstantiateSkillsIfNotYetDone() {
		//	if (!HasSomeSkills()) {
		//		//not found, time to instantiate all and put them in the dict...
		//		AbstractSkillDef oneSkillDef = null;
		//		Skill oneSkill = null;
		//		for(ushort i = 0; i < AbstractSkillDef.SkillsCount; i++) {
		//			oneSkillDef = AbstractSkillDef.ById(i);
		//			oneSkill = new Skill(i, this);
		//			SkillsAbilities[oneSkillDef] = oneSkill;
		//		}
		//	}
		//if (skills == null) {
		//    skills = new Skill[AbstractSkillDef.SkillsCount];
		//    int n = skills.Length;
		//    for (ushort i = 0; i<n; i++) {
		//        skills[i] = new Skill(i, this);
		//    }
		//}
		//}

		//public override ISkill[] Skills {
		//    get {
		//        InstantiateSkillsArrayIfNull();
		//        return skills;
		//    }
		//}

		private static TriggerKey skillChangeTK = TriggerKey.Get("skillChange");
		public void Trigger_SkillChange(Skill skill, ushort oldValue) {
			ushort newValue = skill.RealValue;
			ScriptArgs sa = new ScriptArgs(skill.Id, oldValue, newValue, skill);
			this.TryTrigger(skillChangeTK, sa);
			On_SkillChange(skill, oldValue);
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

		[Summary("Start a skill.")]
		public void SelectSkill(SkillName skillName) {
			SelectSkill((int) skillName);
		}

		[Summary("Call the \"Start\" phase oi a skill. This should typically be called from within implementation of the skill's Select phase.")]
		public void StartSkill(int skillId) {
			SkillDef skillDef = (SkillDef) AbstractSkillDef.ById(skillId);
			if (skillDef != null) {
				if (currentSkill != null) {
					this.AbortSkill();
				}
				currentSkill = skillDef;
				skillDef.Start(this);
			}
		}

		[Summary("Call the \"Start\" phase oi a skill. This should typically be called from within implementation of the skill's Select phase.")]
		public void StartSkill(SkillDef skillDef) {
			if (skillDef != null) {
				if (currentSkill != null) {
					this.AbortSkill();
				}
				currentSkill = skillDef;
				skillDef.Start(this);
			}
		}

		[Summary("Call the \"Start\" phase oi a skill. This should typically be called from within implementation of the skill's Select phase.")]
		public void StartSkill(SkillName skillName) {
			StartSkill((int) skillName);
		}

		public SkillName CurrentSkillName {
			get {
				if (currentSkill == null) {
					return SkillName.None;
				}
				return (SkillName) currentSkill.Id;
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

		private static TimerKey skillTimerKey = TimerKey.Get("_skillTimer_");

		[SaveableClass]
		[DeepCopyableClass]
		public class SkillStrokeTimer : BoundTimer {
			[LoadingInitializer]
			[DeepCopyImplementation]
			public SkillStrokeTimer() {
			}

			protected sealed override void OnTimeout(TagHolder cont) {
				Logger.WriteDebug("SkillStrokeTimer OnTimeout on " + this.Cont);
				Character self = cont as Character;
				if (self != null) {
					self.DelayedSkillStroke();
				}
			}
		}

		public void DelaySkillStroke(double seconds) {
			Sanity.IfTrueThrow((currentSkill == null),
				"DelaySkillStroke called on " + this + ", which currently does no skill.");

			//this.RemoveTimer(skillTimerKey);
			this.AddTimer(skillTimerKey, new SkillStrokeTimer()).DueInSeconds = seconds;
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

		public virtual void On_SkillAbort(int id) {
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
		#endregion skills

		internal Dictionary<AbstractDef, object> SkillsAbilities {
			get {
				if (skillsabilities == null) {
					skillsabilities = new Dictionary<AbstractDef, object>();
				}
				return skillsabilities;
			}
		}

		#region abilities
		[Summary("Enumerator of all character's abilities")]
		public IEnumerable<Ability> Abilities {
			get {
				return new AbilitiesEnumerator(this);
			}
		}

		[Summary("Check if character has the desired ability (according to the ability def)")]
		public bool HasAbility(AbilityDef aDef, out Ability abil) {
			abil = null;
			object retVal = null;
			bool hasOrNot = SkillsAbilities.TryGetValue(aDef, out retVal);
			if (hasOrNot) {
				abil = (Ability) retVal; //found ability, cast the return value
			}

			return hasOrNot;
		}

		public Ability GetAbilityObject(AbilityDef aDef) {
			object retVal = null;
			SkillsAbilities.TryGetValue(aDef, out retVal);
			return (Ability) retVal; //either null or Ability instance if the player has it
		}

		[Summary("Get number of points the character has for specified AbilityDef (0 if he doesnt have it at all)")]
		public int GetAbility(AbilityDef aDef) {
			Ability retAb = GetAbilityObject(aDef);
			return (retAb == null ? 0 : retAb.Points); //either null or Ability.Points if the player has it
		}

		[Summary("Add specified number of points the character has for specified AbilityDef. If the result is" +
				"<= 0 then we will remove the ability")]
		public void AddAbilityPoints(AbilityDef aDef, int points) {
			Ability ab = GetAbilityObject(aDef);
			if (ab != null) {
				ab.Points += points;
			} else if (points > 0) { //we wont create a new ability with 0 or <0 number of points!
				AddNewAbility(aDef, points);
			}
		}

		[Summary("Set specified number of points the character has for specified AbilityDef, check for positive value afterwards.")]
		public void SetAbilityPoints(AbilityDef aDef, int points) {
			Ability ab = GetAbilityObject(aDef);
			if (ab != null) {
				ab.Points = points;
			} else if (points > 0) { //we wont create a new ability with 0 or <0 number of points!
				AddNewAbility(aDef, points);
			}
		}

		private void AddNewAbility(AbilityDef aDef, int points) {
            Ability ab = aDef.Create(this);
			SkillsAbilities.Add(aDef, ab); //first add the object to the dictionary			
			ab.Points = points; //then set points 
			aDef.Trigger_Assign(this); //then call the assign trigger
		}

		internal void RemoveAbility(AbilityDef aDef) {
			SkillsAbilities.Remove(aDef);
			aDef.Trigger_UnAssign(this); //then call the unassign trigger
		}

		internal virtual void On_AbilityAssign(AbilityDef aDef) {
		}

		internal virtual void On_AbilityUnAssign(AbilityDef aDef) {
		}

		internal virtual bool On_AbilityActivate(AbilityDef aDef) {
			return false;
		}

		internal virtual void On_AbilityUnActivate(AbilityDef aDef) {
		}

		internal virtual bool On_AbilityDenyUse(DenyAbilityArgs args) {
			//args contain DenyResultAbilities, Character, AbilityDef and Ability as parameters 
			//(Ability can be null)
			return false;
		}

		#endregion abilities

        #region roles
        [Summary("Check if character has been cast to the given role")]
        public bool HasRole(Role role) {
			HashSet<Role> myRoles = RolesManagement.charactersRoles[this];
			if (myRoles != null) {
				return RolesManagement.charactersRoles[this].Contains(role);
			}
            return false;
        }

        [Summary("Check if character has been cast to any role created by given RoleDef "+
                "e.g. useful for finding out if char is member of any house or citizen of any town etc.")]
        public bool HasRole(RoleDef roledef) {
			HashSet<Role> myRoles = RolesManagement.charactersRoles[this];
			if (myRoles != null) {
				foreach (Role role in myRoles) {
                    if (role.RoleDef == roledef) {
                        return true;
                    }
                }
            }
            return false;
        }

        [Summary("Called after the character has been cast to some role (the role is already in his assignedRoles list")]
        internal virtual void On_RoleAssign(Role role) {
        }

        [Summary("Called after the character has been cast to some role (the role is already in his assignedRoles list")]
        internal virtual void On_RoleUnAssign(Role role) {
        }
        #endregion roles

        public Character Owner {
			get {
				return owner;
			}
			set {
				if (this.IsPlayer) {
					//AboutToChange();
					owner = value;	//always Character
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
			AbstractAccount acc = Account;
			if (acc != null) {
				if (acc.PLevel < acc.MaxPLevel) {
					acc.PLevel = acc.MaxPLevel;
					Conn.WriteLine("GM mode on (Plevel " + acc.PLevel + ").");
				} else {
					acc.PLevel = 1;
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
			AbstractAccount acc = Account;
			if (acc != null) {
				if (i > 0) {
					acc.PLevel = acc.MaxPLevel;
					Conn.WriteLine("GM mode on (Plevel " + acc.PLevel + ").");
				} else {
					acc.PLevel = 1;
					Conn.WriteLine("GM mode off (Plevel 1).");
				}
			}
		}

		[Summary("Check if the current character has plevel greater than 1 (is more than player)")]
		public bool IsGM() {
			return Account.PLevel > 1;
		}

		//for pets
		public bool IsOwnerOf(Character cre) {
			return ((cre.IsPlayer) && cre.Owner != null && cre.owner.Equals(this));
		}

		public bool IsPet {
			get {
				return ((this.IsPlayer) && (this.Owner != null));
			}
		}

		//also for pets
		public bool IsPetOf(Character cre) {
			//return (IsNPC && Owner!=null && Owner.Equals(cre));
			return true;
		}

		public override bool CanEquipItemsOn(AbstractCharacter chr) {
			Character target = (Character) chr;
			return (IsPlevelAtLeast(Globals.plevelOfGM) || (target.Owner == this && CanReach(chr) == DenyResult.Allow));
		}

		//public override bool CanEquip(AbstractItem i) {
		//    return true;
		//}

		public override bool CanRename(AbstractCharacter to) {
			Character target = (Character) to;
			Character targetOwner = target.owner;
			return ((to.IsPlayer) && targetOwner != null && targetOwner.Equals(this));
		}

		public virtual bool IsMountable {
			get {
				return MountItem != 0;
			}
		}

		public virtual bool IsMountableBy(AbstractCharacter chr) {
			if (IsMountable && chr.CanReach(this) == DenyResult.Allow) {
				if (IsPetOf((Character) chr)) return true;
				if (!IsPet && chr.IsPlevelAtLeast(Globals.plevelOfGM)) return true;
			}
			return false;
		}

		//method: On_DClick
		//Character`s implementation of @Dclick trigger, 
		//paperdoll raising and mounting is/will be handled here
		public override void On_DClick(AbstractCharacter from) {
			if (from != null && from.IsPlayer) {
				//PC
				if (from == this && this.Mount != null) {
					this.Dismount();
				} else {
					GameConn conn = from.Conn;
					if (from != this && this.IsMountableBy(from)) {
						from.Mount = this;
					} else {
						if (conn != null) {
							this.ShowPaperdollTo(from.Conn);
						}
					}
				}
			}
		}

		public override void On_ItemDClick(AbstractItem dClicked) {

			//TODO? maybe not in all cases (healing?)? maybe not at all?
			this.Trigger_Disrupt();

			base.On_ItemDClick(dClicked);
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
				if (i != null) {
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
			if (mountorrider != null) {
				if (Flag_Riding) {//I am the rider
					if (!mountorrider.IsDeleted) {
						mountorrider.Delete();
					}
					SetFlag_Riding(false);
					mountorrider = null;
				} else {//I am the mount
					mountorrider.Dismount();
				}
			}
			base.On_Destroy();
		}

		[Summary("Message displayed in red - used for importatnt system or ingame messages (warnings, errors etc)")]
		public void RedMessage(string arg) {
			SysMessage(arg, (int) Hues.Red);
		}

		[Summary("Message displayed in blue - used for ingame purposes")]
		public void BlueMessage(string arg) {
			SysMessage(arg, (int) Hues.Blue);
		}

		[Summary("Message displayed in green - used for ingame purposes")]
		public void GreenMessage(string arg) {
			SysMessage(arg, (int) Hues.Green);
		}

		[Summary("Message displayed in green - used for ingame purposes")]
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
				return (Item) FindLayer((byte) LayerNames.Hair);
			}
		}

		public Item Beard {
			get {
				return (Item) FindLayer((byte) LayerNames.Beard);
			}
		}

		#region combat
		public override void AttackRequest(AbstractCharacter target) {
			if (this == target || target == null) {
				return;
			}

			WeaponSkillTargetQueuePlugin.AddTarget(this, (Character) target);
		}

		public virtual void On_SkillChange(Skill skill, ushort oldValue) {
			switch ((SkillName) skill.Id) {
				case SkillName.Parry:
					InvalidateCombatArmorValues();
					break;
				case SkillName.Tactics:
					InvalidateCombatWeaponValues();
					break;
			}
		}

		CombatCalculator.CombatWeaponValues combatWeaponValues;
		CombatCalculator.CombatArmorValues combatArmorValues;
		internal Projectile weaponProjectile;

		public int ArmorClassVsP {
			get {
				CalculateCombatArmorValues();
				return combatArmorValues.armorVsP;
			}
		}

		public int ArmorClassVsM {
			get {
				CalculateCombatArmorValues();
				return combatArmorValues.armorVsM;
			}
		}

		public override short StatusArmorClass {
			get {
				CalculateCombatArmorValues();
				return (short) ((combatArmorValues.armorVsP + combatArmorValues.armorVsM) / 2);
			}
		}

		private static TagKey armorClassModifierTK = TagKey.Get("_armorClassModifier_");
		public int ArmorClassModifier {
			get {
				return Convert.ToInt32(GetTag(armorClassModifierTK));
			}
			set {
				InvalidateCombatArmorValues();
				if (value != 0) {
					SetTag(armorClassModifierTK, value);
				} else {
					RemoveTag(armorClassModifierTK);
				}
			}
		}

		public int MindDefenseVsP {
			get {
				CalculateCombatArmorValues();
				return combatArmorValues.mindDefenseVsP;
			}
		}

		public int MindDefenseVsM {
			get {
				CalculateCombatArmorValues();
				return combatArmorValues.mindDefenseVsM;
			}
		}

		public override short StatusMindDefense {
			get {
				CalculateCombatArmorValues();
				return (short) ((combatArmorValues.mindDefenseVsP + combatArmorValues.mindDefenseVsM) / 2);
			}
		}

		private static TagKey mindDefenseModifierTK = TagKey.Get("_mindDefenseModifier_");
		public int MindDefenseModifier {
			get {
				return Convert.ToInt32(GetTag(mindDefenseModifierTK));
			}
			set {
				InvalidateCombatArmorValues();
				if (value != 0) {
					SetTag(mindDefenseModifierTK, value);
				} else {
					RemoveTag(mindDefenseModifierTK);
				}
			}
		}

		public void InvalidateCombatWeaponValues() {
			if (combatWeaponValues != null) {
				Packets.NetState.AboutToChangeStats(this);
				combatWeaponValues = null;
			}
		}

		public void InvalidateCombatArmorValues() {
			if (combatArmorValues != null) {
				Packets.NetState.AboutToChangeStats(this);
				combatArmorValues = null;
			}
		}

		private void CalculateCombatWeaponValues() {
			if (combatWeaponValues == null) {
				Packets.NetState.AboutToChangeStats(this);
				combatWeaponValues = CombatCalculator.CalculateCombatWeaponValues(this);
			}
		}

		private void CalculateCombatArmorValues() {
			if (combatArmorValues == null) {
				Packets.NetState.AboutToChangeStats(this);
				combatArmorValues = CombatCalculator.CalculateCombatArmorValues(this);
			}
		}

		public override void On_ItemEnter(ItemInCharArgs args) {
			if (args.manipulatedItem is Wearable) {
				InvalidateCombatArmorValues();
			} else if (args.manipulatedItem is Weapon) {
				InvalidateCombatWeaponValues();
			}
			base.On_ItemEnter(args);
		}

		public override void On_ItemLeave(ItemInCharArgs args) {
			if (args.manipulatedItem is Wearable) {
				InvalidateCombatArmorValues();
			} else if (args.manipulatedItem is Weapon) {
				InvalidateCombatWeaponValues();
			}
			base.On_ItemLeave(args);
		}

		public virtual bool IsPlayerForCombat {
			get {
				//TODO: false for hypnomystic
				return IsPlayer;
			}
		}

		public virtual CharacterDef DefForCombat {
			get {
				//TODO: monster def for hypnomystic
				return this.TypeDef;
			}
		}

		public Weapon Weapon {
			get {
				CalculateCombatWeaponValues();
				return combatWeaponValues.weapon;
			}
		}

		public double WeaponAttackVsP {
			get {
				CalculateCombatWeaponValues();
				return combatWeaponValues.attackVsP;
			}
		}

		public double WeaponAttackVsM {
			get {
				CalculateCombatWeaponValues();
				return combatWeaponValues.attackVsM;
			}
		}

		public double WeaponPiercing {
			get {
				CalculateCombatWeaponValues();
				return combatWeaponValues.piercing;
			}
		}

		public WeaponType WeaponType {
			get {
				CalculateCombatWeaponValues();
				return combatWeaponValues.weaponType;
			}
		}

		public DamageType WeaponDamageType {
			get {
				CalculateCombatWeaponValues();
				return combatWeaponValues.damageType;
			}
		}

		public WeaponAnimType WeaponAnimType {
			get {
				CalculateCombatWeaponValues();
				return combatWeaponValues.weaponAnimType;
			}
		}

		public Projectile WeaponProjectile {
			get {
				if ((weaponProjectile != null) &&
						(weaponProjectile.IsDeleted ||
						(weaponProjectile.TopObj() != this) ||
						(weaponProjectile.Amount < 1))) {
					weaponProjectile = null;//we had ammo but now don't have it anymore
					InvalidateCombatWeaponValues();
				} else if ((weaponProjectile == null) && (this.combatWeaponValues != null) &&
						(this.combatWeaponValues.projectileType != ProjectileType.None)) {
					InvalidateCombatWeaponValues();//we have no ammo but we should, let's look for it
				}
				CalculateCombatWeaponValues();
				return weaponProjectile;
			}
		}

		public int WeaponProjectileAnim {
			get {
				CalculateCombatWeaponValues();
				return combatWeaponValues.projectileAnim;
			}
		}

		public ProjectileType WeaponProjectileType {
			get {
				CalculateCombatWeaponValues();
				return combatWeaponValues.projectileType;
			}
		}

		public int WeaponRange {
			get {
				CalculateCombatWeaponValues();
				return combatWeaponValues.range;
			}
		}

		public int WeaponStrikeStartRange {
			get {
				CalculateCombatWeaponValues();
				return combatWeaponValues.strikeStartRange;
			}
		}

		public int WeaponStrikeStopRange {
			get {
				CalculateCombatWeaponValues();
				return combatWeaponValues.strikeStopRange;
			}
		}

		public double WeaponDelay {
			get {
				//TODO: mana-dependant for mystic
				CalculateCombatWeaponValues();
				return combatWeaponValues.delay;
			}
		}

		public bool On_BeforeSwing(WeaponSwingArgs args) {
			return false;
		}

		public bool On_BeforeGetSwing(WeaponSwingArgs args) {
			return false;
		}

		public bool On_CauseDamage(DamageArgs args) {
			return false;
		}

		public bool On_Damage(DamageArgs args) {
			return false;
		}

		public void On_AfterSwing(WeaponSwingArgs args) {
		}

		public void On_AfterGetSwing(WeaponSwingArgs args) {
		}


		[Summary("hodi zbran do batohu")]
		public void DisArm() {
			Weapon w = this.Weapon;
			if (w != null)
				w.Cont = this.BackpackAsContainer;
		}
		#endregion combat

		public override void On_LogOut() {
			AbortSkill();
			base.On_LogOut();
		}


		public CharModelInfo CharModelInfo {
			get {
				ushort model = this.Model;
				if ((this.charModelInfo == null) || (this.charModelInfo.model != model)) {
					this.charModelInfo = CharModelInfo.Get(model);
				}
				return charModelInfo;
			}
		}

		/**
			These are flags which specify what kind of model this is, and what anims it has.
		*/
		public uint AnimsAvailable {
			get {
				return this.CharModelInfo.AnimsAvailable;
			}
		}

		public bool IsHuman {
			get {
				return (this.CharModelInfo.charAnimType & CharAnimType.Human) == CharAnimType.Human;
			}
		}

		public bool IsAnimal {
			get {
				return (this.CharModelInfo.charAnimType & CharAnimType.Animal) == CharAnimType.Animal;
			}
		}

		public bool IsMonster {
			get {
				return (this.CharModelInfo.charAnimType & CharAnimType.Monster) == CharAnimType.Monster;
			}
		}

		public Gender Gender {
			get {
				return this.CharModelInfo.gender;
			}
		}

		public override bool IsMale {
			get {
				return this.CharModelInfo.isMale;
			}
		}

		public override bool IsFemale {
			get {
				return this.CharModelInfo.isFemale;
			}
		}

		public bool CanReachWithMessage(Thing target) {
			DenyResult result = CanReach(target);
			if (result == DenyResult.Allow) {
				return true;
			} else {
				GameConn conn = this.Conn;
				if (conn != null) {
					Server.SendDenyResultMessage(conn, target, result);
				}
				return false;
			}
		}

		public override DenyResult CanOpenContainer(AbstractItem targetContainer) {
			if (this.IsGM()) {
				return DenyResult.Allow;
			}

			GameConn conn = this.Conn;
			if (conn == null) {
				return DenyResult.Deny_NoMessage;
			}

			//TODO zamykani kontejneru

			DenyResult result = DenyResult.Allow;

			Thing c = targetContainer.Cont;
			if (c != null) {
				Item contAsItem = c as Item;
				if (contAsItem != null) {
					result = OpenedContainers.HasContainerOpen(conn, contAsItem);
				} else if (c != this) {
					result = this.CanReach(c);
					if (result == DenyResult.Allow) {
						Character contAsChar = (Character) c;
						if (!contAsChar.IsPetOf(this)) {//not my pet or myself
							result = DenyResult.Deny_ThatDoesNotBelongToYou;
						}
					}
				}
			} else {
				result = this.CanReachCoordinates(targetContainer);
			}

			return result;
		}


		public virtual bool On_DenyOpenDoor(DenySwitchDoorArgs args) {
			return false;
		}

		public virtual bool On_DenyCloseDoor(DenySwitchDoorArgs args) {
			return false;
		}
	}
}
