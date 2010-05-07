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
using SteamEngine.Persistence;
using SteamEngine.Regions;
using SteamEngine.Networking;
using SteamEngine.Communication;
using SteamEngine.Communication.TCP;

namespace SteamEngine.CompiledScripts {

	//[Flags]
	//public enum CharacterFlags : short {
	//    None = 0, Zero = None,
	//    Disconnected = 0x01,
	//}

	[Dialogs.ViewableClass]
	public partial class Character : AbstractCharacter {
		//removed, reworked to use the Skills togeter with abilities in one distionary
		//Skill[] skills;//this CAN be null, altough it usually isn't

		//[Summary("Dictionary of character's (typically player's) abilities")]
		//private Dictionary<AbilityDef, Ability> abilities = null;

		[Summary("Dictionary of character's skills and abilities. Key is (ability/skill)def, value is instance of " +
				"Ability/Skill object of the desired entity")]
		private Dictionary<AbstractDef, object> skillsabilities;

		float weight;
		private CharModelInfo charModelInfo;

		#region Flags
		public override sealed byte FlagsToSend {
			get {
				int ret = 0;

				if (this.Flag_GreenHealthBar) {
					ret |= 0x04;
				}
				if (this.IsNotVisible) {
					ret |= 0x80;
				}
				if (this.Flag_WarMode) {
					ret |= 0x41; //both 0x40 (for aos clients) and 0x01 (for older clients?) hope it won't break much
				}

				//if Female
				//	|= 0x02;

				//if Blessed || YellowHealthbar
				//	|= 0x08;
				

				return (byte) ret;
			}
		}

		public bool Flag_Dead {
			get {
				return this.ProtectedFlag1;
			}
			private set {
				this.ProtectedFlag1 = value;
			}
		}

		public override bool Flag_Insubst {
			get {
				return this.ProtectedFlag2;
			}
			set {
				if (this.ProtectedFlag2 != value) {
					CharSyncQueue.AboutToChangeVisibility(this);
					this.ProtectedFlag2 = value;
				}
			}
		}

		public bool Flag_InvisByMagic {
			get {
				return this.ProtectedFlag3;
			}
			set {
				if (this.ProtectedFlag3 != value) {
					CharSyncQueue.AboutToChangeVisibility(this);
					this.ProtectedFlag3 = value;
				}
			}
		}

		public bool Flag_Hidden {
			get {
				return this.ProtectedFlag4;
			}
			set {
				if (this.ProtectedFlag4 != value) {
					CharSyncQueue.AboutToChangeVisibility(this);
					this.ProtectedFlag4 = value;
				}
			}
		}

		public override bool Flag_WarMode {
			get {
				return this.ProtectedFlag5;
			}
			set {
				if (this.ProtectedFlag5 != value) {
					CharSyncQueue.AboutToChangeFlags(this);
					this.ProtectedFlag5 = value;
					this.Trigger_WarModeChange();
				}
			}
		}

		public bool Flag_GreenHealthBar {
			get {
				return this.ProtectedFlag7;
			}
			set {
				if (this.ProtectedFlag7 != value) {
					CharSyncQueue.AboutToChangeFlags(this);
					this.ProtectedFlag7 = value;
				}
			}
		}

		//use these counter methods to properly set the greenhealthbar flag from multiple sources

		private static TagKey poisonCounterTK = TagKey.Acquire("_poison_counter_");
		public void AddPoisonCounter() {
			this.SetTag(poisonCounterTK, Convert.ToInt32(this.GetTag(poisonCounterTK)) + 1);
			this.Flag_GreenHealthBar = true;
		}

		public void RemovePoisonCounter() {
			int val = Convert.ToInt32(this.GetTag(poisonCounterTK)) - 1;
			if (val <= 0) {
				this.Flag_GreenHealthBar = false;
				this.RemoveTag(poisonCounterTK);
			} else {
				this.SetTag(poisonCounterTK, val);
			}
		}

		#endregion Flags

		#region Mount/Rider stuff
		//Flag_Riding = true, mountorrider must not be null -> I'm riding
		//Flag_Riding = false, mountorrider != null -> someone's on my back (and I'm insubst)
		//Flag_Riding = false, mountorrider == null -> I'm neither riding nor being ridden

		//Flag_Riding is readonly and has a setter method,s because scripts should not be able to set it,
		//(they should set Mount instead) but it needs to be settable from within Character itself.
		public override bool Flag_Riding {
			get {
				return this.ProtectedFlag6;
			}
		}

		private void SetFlag_Riding(bool value) {
			this.ProtectedFlag6 = value;
		}

		public virtual bool IsMountable {
			get {
				return this.MountItem > 0;
			}
		}

		public virtual bool IsMountableBy(Character chr) {
			return !this.IsPlayer && //players are never mountable
				this.IsMountable &&
				chr.CanReach(this) == DenyResult.Allow &&
				this.IsPetOf(chr);
		}

		[Summary("The character riding this one, or null if this character doesn't have a rider.")]
		public AbstractCharacter Rider {
			get {
				if (!this.Flag_Riding) { //otherwise I'm the rider
					return this.mountorrider;
				}
				return null;
			}
		}

		[Summary("The mount this character is riding, or null if this character isn't riding a mount.")]
		public override sealed AbstractCharacter Mount {
			get {
				if (this.Flag_Riding) {
					return this.mountorrider;
				}
				return null;
			}
			set {
				if ((this.mountorrider != null) && (!this.Flag_Riding)) {
					throw new SEException("You can't set Mount of something that's being ridden!");
				}
				if (value == null || value.IsDeleted) {
					//automatically call Dismount if 'mount=null;' is done.
					this.Dismount();
				} else {

					Character newMount = (Character) value;
					if ((newMount.mountorrider != null) && (!newMount.Flag_Riding)) {
						throw new SEException("You can't set Mount to something that's being ridden!");
					}
					if (newMount.IsPlayer) {
						throw new SEException("Players can't be mounted!");
					}

					if (this.mountorrider != newMount) {
						if (this.mountorrider != null) {
							this.Dismount();
						} else {
							CharSyncQueue.AboutToChangeMount(this);
						}

						newMount.Dismount();

						this.mountorrider = newMount;
						this.SetFlag_Riding(true);
						newMount.mountorrider = this;
						this.mountorrider.Disconnect();
					} //else nothing changes
				}
			}
		}

		public void Dismount() {
			if (this.mountorrider != null) {
				if (this.Flag_Riding) {
					CharSyncQueue.AboutToChangeMount(this);

					//move it to where we are
					this.mountorrider.P(this);
					this.mountorrider.Direction = Direction;

					Sanity.IfTrueThrow(this.mountorrider.mountorrider != this, "this.mountorrider.mountorrider != this");
					Sanity.IfTrueThrow(this.mountorrider.Flag_Riding, "this.mountorrider.Flag_Riding on Dismount()");

					//set it's rider to null
					this.mountorrider.mountorrider = null;
					this.mountorrider.Reconnect();

					this.SetFlag_Riding(false);
					this.mountorrider = null;
				} else {
					throw new SEException("You can't call Dismount() on a char that's being ridden. Call it on it's Rider.");
				}
			}

			Sanity.IfTrueThrow(this.Flag_Riding, "Flag_Riding and this.mountorrider == null");
		}

		public override void On_AfterLoad() {
			//try to fix mounts
			if (this.mountorrider != null) {
				if (this.mountorrider.mountorrider == this) {
					if (this.Flag_Riding == this.mountorrider.Flag_Riding) { //both riders or both mounts = wrong
						if (this.IsPlayer) { //I am the rider
							if (!this.mountorrider.IsPlayer) {
								this.SetFlag_Riding(true);
								this.mountorrider.SetFlag_Riding(false);
								this.mountorrider.Disconnect();
							} else { //mount would be a player. Not gonna happen
								this.ForgetMountValues();
							}
						} else if (!this.mountorrider.IsPlayer) { //both are NPCs
							//we use flag-disconnected as clue
							if (this.Flag_Disconnected != this.mountorrider.Flag_Disconnected) {
								Logger.WriteWarning("Fixed mount states of 2 NPCs ('" + this + "' and '" + this.mountorrider + "') according to their Flag_Disconnected");
								this.SetFlag_Riding(!this.Flag_Disconnected);
								this.mountorrider.SetFlag_Riding(!this.mountorrider.Flag_Disconnected);
							} else { //disconnected state the same, i.e. useless for us
								//this is an error
								this.ForgetMountValues(); //the other side should get fied in their own instance of this method
							}
						}
					}
				} else { //other side is not pointing to us. They might be in fact not broken so we leave them alone.
					this.ForgetMountValues();
				}
			} else if (this.Flag_Riding) {
				Logger.WriteWarning("Fixed mistakenly positive Flag_Riding of '" + this + "'.");
				this.SetFlag_Riding(false);
			}

			base.On_AfterLoad();
		}

		private void ForgetMountValues() {
			Logger.WriteError("Unfixable mount persistence error of '" + this + "'. Set as dismounted and reconnected.");
			this.SetFlag_Riding(false);
			this.mountorrider = null;
			if (!this.IsPlayer) {
				this.Reconnect();
			}
		}

		public override void On_Destroy() {
			if (this.mountorrider != null) {
				if (this.Flag_Riding) {//I am the rider, riding a NPC - delete it
					this.mountorrider.Delete();
					this.SetFlag_Riding(false);
					this.mountorrider = null;
				} else {//I am the mount
					this.mountorrider.Dismount();
				}
			}
			base.On_Destroy();
		}
		#endregion Mount/Rider stuff

		#region Visibility
		public virtual void On_VisibilityChange() {
		}

		public override sealed bool IsNotVisible {
			get {
				return (this.Flag_InvisByMagic || this.Flag_Hidden || this.Flag_Insubst || this.Flag_Disconnected);
			}
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
				if (this.IsGM) {
					return this.Plevel > target.Plevel; //can see other GMs only if they have lowe plevel
				}
				return false;
			}
			if (target.Flag_InvisByMagic) {
				return this.IsGM;
			}
			if (target.Flag_Hidden) {
				if (this.IsGM) {
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
					return this.IsGM;
				}
				return false;
			}
			return true;
		}
		#endregion Visibility

		public bool IsAliveAndValid {
			get {
				//check if char isnt deleted, disconnected, dead and (insubst without being a GM)
				return !this.IsDeleted && !this.Flag_Disconnected && !this.Flag_Dead && !(this.Flag_Insubst && !this.IsGM);
			}
		}

		public bool CheckAliveWithMessage() {
			if ((this.Flag_Disconnected) || (this.IsDeleted)) {
				return false;
			} else if (this.Flag_Dead) {
				this.ClilocSysMessage(1019048, 0x3B2); // I am dead and cannot do that.
				return false;
			} else if (this.Flag_Insubst && !this.IsGM) {
				this.ClilocSysMessage(500590, 0x3B2);  //You're a ghost, and can't do that.
				return false;
			}
			return true;
		}

		private static TriggerKey warModeChangeTK = TriggerKey.Acquire("warModeChange");
		public void Trigger_WarModeChange() {
			this.TryTrigger(warModeChangeTK, null);			
			try {
				this.On_WarModeChange();
			} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
		}

		public virtual void On_WarModeChange() {
		}

		private static TriggerKey visibilityChangeTK = TriggerKey.Acquire("visibilityChange");
		public void Trigger_VisibilityChange() {
			TryTrigger(visibilityChangeTK, null);
			On_VisibilityChange();
		}

		public bool CanInteractWith(Thing target) {
			if (this.Flag_Dead) {
				return false;
			}

			bool canSee = this.CanSeeVisibility(target);
			if (!canSee) {
				return false;
			}

			Character targetAsChar = target as Character;
			if (targetAsChar != null) {
				return !targetAsChar.Flag_Dead;
			}
			return true;
		}

		#region StatLock
		public override sealed byte StatLockByte {
			get {
				return this.statLockByte;
			}
		}

		public override StatLockType StrLock {
			get {
				return (StatLockType) ((this.statLockByte >> 4) & 0x3);
			}
			set {
				if (value != this.StrLock) {
					this.statLockByte = (byte) ((this.statLockByte & 0xCF) + ((((byte) value) & 0x3) << 4));
					PacketSequences.SendStatLocks(this);
				}
			}
		}

		public override StatLockType DexLock {
			get {
				return (StatLockType) ((this.statLockByte >> 2) & 0x3);
			}
			set {
				if (value != DexLock) {
					this.statLockByte = (byte) ((this.statLockByte & 0xF3) + ((((byte) value) & 0x3) << 2));
					PacketSequences.SendStatLocks(this);
				}
			}
		}

		public override StatLockType IntLock {
			get {
				return (StatLockType) ((this.statLockByte) & 0x3);
			}
			set {
				if (value != IntLock) {
					this.statLockByte = (byte) ((this.statLockByte & 0xFC) + ((((byte) value) & 0x3)));
					PacketSequences.SendStatLocks(this);
				}
			}
		}
		#endregion StatLock

		#region status properties
		public override short Hits {
			get {
				return hitpoints;
			}
			set {
				if (value != hitpoints) {
					if (!this.Flag_Dead && value < 1) {
						this.CauseDeath((Character) Globals.SrcCharacter);
					} else {
						CharSyncQueue.AboutToChangeHitpoints(this);
						hitpoints = value;

						//try the hitpoints regeneration
						RegenerationPlugin.TryInstallPlugin(this, this.hitpoints, this.MaxHits, hitsRegenSpeed);
					}
				}
			}
		}

		public override short MaxHits {
			get {
				throw new SEException("The method or operation is not implemented.");
			}
			set {
				throw new SEException("The method or operation is not implemented.");
			}
		}

		public override short Mana {
			get {
				return mana;
			}
			set {
				if (value != mana) {
					CharSyncQueue.AboutToChangeMana(this);
					mana = value;

					//regeneration...
					RegenerationPlugin.TryInstallPlugin(this, this.mana, this.MaxMana, manaRegenSpeed);

					//meditation finish
					if (mana >= this.MaxMana) {
						this.DeletePlugin(MeditationPlugin.meditationPluginKey);
					}
				}
			}
		}


		public override short MaxStam {
			get {
				throw new SEException("The method or operation is not implemented.");
			}
			set {
				throw new SEException("The method or operation is not implemented.");
			}
		}

		public override short Stam {
			get {
				return stamina;
			}
			set {
				if (value != stamina) {
					CharSyncQueue.AboutToChangeStamina(this);
					stamina = value;

					//regeneration...
					RegenerationPlugin.TryInstallPlugin(this, this.stamina, this.MaxStam, stamRegenSpeed);
				}
			}
		}

		public override short MaxMana {
			get {
				throw new SEException("The method or operation is not implemented.");
			}
			set {
				throw new SEException("The method or operation is not implemented.");
			}
		}

		public override short Str {
			get {
				return strength;
			}
			set {
				if (value != strength) {
					CharSyncQueue.AboutToChangeStats(this);
					this.InvalidateCombatWeaponValues();
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
					CharSyncQueue.AboutToChangeStats(this);
					this.InvalidateCombatWeaponValues();
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
					CharSyncQueue.AboutToChangeStats(this);
					this.InvalidateCombatWeaponValues();
					intelligence = value;
				}
			}
		}

		public override short TithingPoints {
			get {
				return tithingPoints;
			}
			set {
				if (value != tithingPoints) {
					CharSyncQueue.AboutToChangeStats(this);
					tithingPoints = value;
				}
			}
		}

		//public override short ExtendedStatusNum01 {
		//    get {
		//        return 0;
		//    }
		//}

		//public override short ExtendedStatusNum02 {
		//    get {
		//        return 0;
		//    }
		//}

		//public override short ExtendedStatusNum03 {
		//    get {
		//        return 0;
		//    }
		//}

		//public override short ExtendedStatusNum04 {
		//    get {
		//        return 0;
		//    }
		//}

		//public override short ExtendedStatusNum05 {
		//    get {
		//        return 0;
		//    }
		//}

		//public override short ExtendedStatusNum06 {
		//    get {
		//        return 0;
		//    }
		//}

		//public override byte ExtendedStatusNum07 {
		//    get {
		//        return 0;
		//    }
		//}

		//public override byte ExtendedStatusNum08 {
		//    get {
		//        return 0;
		//    }
		//}

		//public override short ExtendedStatusNum09 {
		//    get {
		//        return 0;
		//    }
		//}

		public override long Gold {
			get {
				return 0;
			}
		}
		#endregion status properties

		public void AddHits(int howManyHits) {
			int hits = this.Hits + howManyHits;
			this.Hits = (short) Math.Min(0, Math.Max(this.MaxHits, hits));
		}

		#region resisty
		private static TagKey resistMagicTK = TagKey.Acquire("_resistMagic_");
		private static TagKey resistFireTK = TagKey.Acquire("_resistFire_");
		private static TagKey resistElectricTK = TagKey.Acquire("_resistElectric_");
		private static TagKey resistAcidTK = TagKey.Acquire("_resistAcid_");
		private static TagKey resistColdTK = TagKey.Acquire("_resistCold_");
		private static TagKey resistPoisonTK = TagKey.Acquire("_resistPoison_");
		private static TagKey resistMysticalTK = TagKey.Acquire("_resistMystical_");
		private static TagKey resistPhysicalTK = TagKey.Acquire("_resistPhysical_");
		private static TagKey resistSlashingTK = TagKey.Acquire("_resistSlashing_");
		private static TagKey resistStabbingTK = TagKey.Acquire("_resistStabbing_");
		private static TagKey resistBluntTK = TagKey.Acquire("_resistBlunt_");
		private static TagKey resistArcheryTK = TagKey.Acquire("_resistArchery_");
		private static TagKey resistBleedTK = TagKey.Acquire("_resistBleed_");
		private static TagKey resistSummonTK = TagKey.Acquire("_resistSummon_");
		private static TagKey resistDragonTK = TagKey.Acquire("_resistDragon_");
		private static TagKey resistParalyseTK = TagKey.Acquire("_resistParalyse_");

		public int ResistMagic {
			get {
				int dynamicPart = Convert.ToInt32(this.GetTag(resistMagicTK));
				return dynamicPart + TypeDef.ResistMagic;
			}
			set {
				int dynamicPart = value - TypeDef.ResistMagic;
				if (dynamicPart != 0) {
					this.SetTag(resistMagicTK, dynamicPart);
				} else {
					this.RemoveTag(resistMagicTK);
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
				} else {
					this.RemoveTag(resistFireTK);
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
				} else {
					this.RemoveTag(resistElectricTK);
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
				} else {
					this.RemoveTag(resistAcidTK);
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
				} else {
					this.RemoveTag(resistColdTK);
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
				} else {
					this.RemoveTag(resistPoisonTK);
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
				} else {
					this.RemoveTag(resistMysticalTK);
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
				} else {
					this.RemoveTag(resistPhysicalTK);
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
				} else {
					this.RemoveTag(resistSlashingTK);
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
				} else {
					this.RemoveTag(resistStabbingTK);
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
				} else {
					this.RemoveTag(resistBluntTK);
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
				} else {
					this.RemoveTag(resistArcheryTK);
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
				} else {
					this.RemoveTag(resistBleedTK);
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
				} else {
					this.RemoveTag(resistSummonTK);
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
				} else {
					this.RemoveTag(resistDragonTK);
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
				} else {
					this.RemoveTag(resistParalyseTK);
				}
			}
		}
		#endregion

		#region regenerace
		[Summary("How many hitpoints is regenerated in one second")]
		public double HitsRegenSpeed {
			get {
				return this.hitsRegenSpeed;
			}
			set {
				this.hitsRegenSpeed = value;

				//check the regeneration
				RegenerationPlugin.TryInstallPlugin(this, this.hitpoints, this.MaxHits, this.hitsRegenSpeed);
			}
		}

		[Summary("How many stamina points is regenerated in one second")]
		public double ManaRegenSpeed {
			get {
				return this.manaRegenSpeed;
			}
			set {
				this.manaRegenSpeed = value;

				//check the regeneration
				RegenerationPlugin.TryInstallPlugin(this, this.mana, this.MaxMana, this.manaRegenSpeed);
			}
		}

		[Summary("How many mana points is regenerated in one second")]
		public double StamRegenSpeed {
			get {
				return this.stamRegenSpeed;
			}
			set {
				this.stamRegenSpeed = value;

				//check the regeneration
				RegenerationPlugin.TryInstallPlugin(this, this.stamina, this.MaxStam, this.stamRegenSpeed);
			}
		}
		#endregion regenerace

		public override string PaperdollName {
			get {
				if (!String.IsNullOrEmpty(this.title)) {
					return string.Concat(this.Name, ", ", title);
				}
				return this.Name;
			}
		}

		public string Title {
			get {
				return this.title;
			}
			set {
				this.title = value;
			}
		}

		#region Death/Resurrection
		public void Kill() {
			//TODO effect?
			this.CauseDeath((Character) Globals.SrcCharacter);
		}

        private static TagKey DeathTimeTK = TagKey.Acquire("_deathTime_");

		public virtual void On_Death(Character killedBy) {
			//stop regenerating
			this.DeletePlugin(RegenerationPlugin.regenerationsPluginKey);
            this.SetTag(DeathTimeTK, Globals.TimeAsSpan);
		}

		private static TriggerKey deathTK = TriggerKey.Acquire("death");

		public void CauseDeath(Character killedBy) {
			if (!this.Flag_Dead) {

				this.Trigger_HostileAction(killedBy);
				this.Trigger_Disrupt();

				this.AbortSkill();
				this.Dismount();
				SoundCalculator.PlayDeathSound(this);

				this.Trigger_Death(killedBy);

				CharSyncQueue.AboutToChangeHitpoints(this);
				this.hitpoints = 0;

				CorpseDef cd = this.TypeDef.CorpseDef;
				Corpse corpse = null;
				if (cd != null) {
					corpse = (Corpse) cd.Create((IPoint4D) this);
					//NetState.ProcessThing(corpse);
				}

				GameState state = this.GameState;
				TcpConnection<GameState> conn = null;
				if (state != null) {
					conn = state.Conn;
					PreparedPacketGroups.SendYouAreDeathMessage(conn);
				}

				PacketGroup pg = null;
				foreach (TcpConnection<GameState> viewerConn in this.GetMap().GetConnectionsWhoCanSee(this)) {
					if (conn != viewerConn) {
						if (pg == null) {
							pg = PacketGroup.AcquireMultiUsePG();
							pg.AcquirePacket<DisplayDeathActionOutPacket>().Prepare(this.FlaggedUid, corpse);
						}
						viewerConn.SendPacketGroup(pg);
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

				if (pg != null) {
					pg.Dispose();
				}
			}
		}

		private void Trigger_Death(Character killedBy) {
			this.TryTrigger(deathTK, new ScriptArgs(killedBy));
			try {
				this.On_Death(killedBy);
			} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
		}

        [Summary("Resne. Pokud je mrtva postava v blizkosti tela(max 1 policko), tak to lootne i telo")]
        public void Resurrect() {			
			Corpse c = null;
			foreach (Thing nearbyThing in this.GetMap().GetThingsInRange(this.X, this.Y, 1)) {
				c = nearbyThing as Corpse;
                if (c != null) {
                    if (c.Owner == this) {
                        break;
                    } else {
                        c = null;
                    }
                }
			}

			this.Resurrect(c);			
		}

        [Summary("Resne a pokud je telo tak i lootne telo.")]
        [Remark("Vzajemna pozice mezi telem a mrtve postavy neni kontrolovana")]
        [Param(0, "Telo jehoz majitel je this. Muze byt null.")]
        public void Resurrect(Corpse c) {
			if (this.Flag_Dead) {
				if (c != null && !c.IsDeleted) {
					c.ReturnStuffToChar(this);
				}

				this.Hits = 1;
				this.Model = this.OModel;
				this.ReleaseOModelTag();
				this.Color = this.OColor;
				this.ReleaseOColorTag();
				this.Flag_Insubst = false;
				this.Flag_Dead = false;

				GameState state = this.GameState;
				if (state != null) {
					PreparedPacketGroups.SendResurrectMessage(state.Conn);
				}
			}
        }

		private static TagKey oColorTK = TagKey.Acquire("_ocolor_");
		public int OColor {
			get {
				object o = this.GetTag(oColorTK);
				if (o != null) {
					return Convert.ToInt32(o);
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

		private static TagKey oModelTK = TagKey.Acquire("_omodel_");
		public int OModel {
			get {
				object o = this.GetTag(oModelTK);
				if (o != null) {
					return Convert.ToInt32(o);
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
		#endregion Death/Resurrection

		private static TriggerKey disruptionTK = TriggerKey.Acquire("disruption");
		public void Trigger_Disrupt() {
			TryTrigger(disruptionTK, null);
			On_Disruption();
		}

		public virtual void On_Disruption() {
			if (this.CurrentSkillName == SkillName.Magery) {
				this.AbortSkill();
			}
		}

		private static TriggerKey hostileActionTK = TriggerKey.Acquire("hostileAction");
		public void Trigger_HostileAction(Character enemy) {
			ScriptArgs sa = new ScriptArgs(enemy);
			TryTrigger(hostileActionTK, sa);
			On_HostileAction(enemy);
		}

		public virtual void On_HostileAction(Character enemy) {

		}

		#region Go() overrides
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

		public void Go(int x, int y) {
			P(x, y);
			Fix();
			//Update();
		}

		public void Go(int x, int y, int z) {
			P(x, y, z);
			Fix();
			//Update();
		}

		public void Go(int x, int y, int z, byte m) {
			P(x, y, z, m);
			Fix();
			//Update();
		}
		#endregion Go() overrides

		private static ContainerDef backpackDef = null;

		private AbstractItem AddBackpack() {
			ThrowIfDeleted();
			if (backpackDef == null) {
				backpackDef = ThingDef.FindItemDef(0xe75) as ContainerDef;
				if (backpackDef == null) {
					throw new SEException("Unable to find itemdef 0xe75 in scripts.");
				} else if (backpackDef.Layer != (int) LayerNames.Pack) {
					throw new SEException("Wrong layer of backpack itemdef.");
				}
			}

			AbstractItem i = (AbstractItem) backpackDef.Create(this);
			if (i == null) {
				throw new SEException("Unable to create backpack.");
			}
			return i;
		}

		public sealed override AbstractItem GetBackpack() {
			AbstractItem foundPack = this.FindLayer(LayerNames.Pack);
			if (foundPack == null) {
				foundPack = this.AddBackpack();
			}
			return foundPack;
		}

		public Container Backpack {
			get {
				return (Container) this.GetBackpack();
			}
		}

		public override sealed AbstractItem NewItem(IThingFactory arg, int amount) {
			return this.Backpack.NewItem(arg, amount);
		}

		public Equippable NewEquip(IThingFactory factory) {
			Thing t = factory.Create(this);
			Equippable i = t as Equippable;
			if (i != null) {
				if (i.Cont != this) {
					i.Delete();
					throw new SEException("'" + i + "' ended not equipped on the char... Wtf?");
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

			if (copyFrom.skillsabilities != null) {
				foreach (object o in copyFrom.skillsabilities.Values) {
					Ability a = o as Ability;
					if (a != null) {
						this.SkillsAbilities.Add(a.Def,
							new Ability(a, this)); //add copy of the Ability object to the duped char's storage
					} else {
						Skill s = (Skill) o;
						this.SkillsAbilities.Add(SkillDef.GetById(s.Id),
							new Skill(s, this)); //add copy of the Skill object to the duped char's storage
					}
				}
			}
		}

		#region Persistence
		public override void On_Save(SteamEngine.Persistence.SaveStream output) {
			if (this.skillsabilities != null) {
				foreach (object o in this.skillsabilities.Values) {
					Ability a = o as Ability;
					if (a != null) {
						output.WriteLine(String.Concat(a.Def.PrettyDefname, "=", a.GetSaveString()));
					} else {
						Skill s = (Skill) o;
						output.WriteLine(String.Concat(s.Def.PrettyDefname, "=", s.GetSaveString()));
					}
				}
			}
			base.On_Save(output);
		}

		public override void On_Load(PropsSection input) {
			List<PropsLine> linesList = new List<PropsLine>(input.PropsLines); //can't iterate over the property itself, popping the lines would break the iteration
			foreach (PropsLine line in linesList) {
				AbilityDef abDef = AbilityDef.GetByDefname(line.Name);
				if (abDef != null) {
					input.PopPropsLine(line.Name);
					Ability ab = this.AcquireAbilityObject(abDef);
					if (!ab.LoadSavedString(line.Value)) {
						Logger.WriteError(input.Filename, line.Line, "Unrecognised ability value format.");
					}
				} else {
					AbstractSkillDef skillDef = AbstractSkillDef.GetByDefname(line.Name);
					if (skillDef != null) {
						input.PopPropsLine(line.Name);
						Skill skill = this.AcquireSkillObject(skillDef);
						if (!skill.LoadSavedString(line.Value)) {
							Logger.WriteError(input.Filename, line.Line, "Unrecognised skill value format.");
						}
					}
				}
			}
			base.On_Load(input);
		}
		#endregion Persistence

		private Dictionary<AbstractDef, object> SkillsAbilities {
			get {
				if (this.skillsabilities == null) {
					this.skillsabilities = new Dictionary<AbstractDef, object>();
				}
				return this.skillsabilities;
			}
		}

		#region Skills
		[Summary("Enumerator of all character's skills")]
		public override IEnumerable<ISkill> Skills {
			get {
				return new SkillsEnumerator(this.skillsabilities);
			}
		}

		[Summary("Return the appropriate Skill instance by given ID, or null if given skill values are default (value 0 and lock up).")]
		public override ISkill GetSkillObject(int id) {
			if (this.skillsabilities != null) {
				AbstractSkillDef def = SkillDef.GetById(id);
				object retVal = null;
				if (this.skillsabilities.TryGetValue(def, out retVal)) {
					return (ISkill) retVal;
				}
			}
			return null;
		}

		[Summary("Return the appropriate Skill instance by given def, or null if given skill values are default (value 0 and lock up).")]
		public Skill GetSkillObject(AbstractSkillDef def) {
			if (this.skillsabilities != null) {
				object retVal = null;
				if (this.skillsabilities.TryGetValue(def, out retVal)) {
					return (Skill) retVal;
				}
			}
			return null;
		}

		public override void SetRealSkillValue(int id, int value) {
			AbstractSkillDef def = SkillDef.GetById(id);
			this.AcquireSkillObject(def).RealValue = value;
		}

		public override void SetSkillLockType(int id, SkillLockType type) {
			AbstractSkillDef def = SkillDef.GetById(id);
			this.AcquireSkillObject(def).Lock = type;
		}

		public void SetRealSkillValue(AbstractSkillDef def, int value) {
			this.AcquireSkillObject(def).RealValue = value;
		}

		[Summary("Change the 'modified' value of a skill.")]
		public void ModifySkillValue(int id, int difference) {
			AbstractSkillDef def = SkillDef.GetById(id);
			this.AcquireSkillObject(def).ModifyValue(difference);
		}

		[Summary("Change the 'modified' value of a skill.")]
		public void ModifySkillValue(SkillName id, int difference) {
			this.ModifySkillValue((int) id, difference);
		}

		[Summary("Change the 'modified' value of a skill.")]
		public void ModifySkillValue(AbstractSkillDef def, int difference) {
			this.AcquireSkillObject(def).ModifyValue(difference);
		}

		[Summary("Get modified value of the skill.")]
		public override int GetSkill(int id) {
			ISkill skl = this.GetSkillObject(id);
			if (skl != null) {
				return skl.ModifiedValue;
			} else {
				return 0;
			}
		}

		[Summary("Get modified value of the skill.")]
		public int GetSkill(SkillName id) {
			return this.GetSkill((int) id);
		}

		[Summary("Get modified value of the skill.")]
		public int GetSkill(AbstractSkillDef skillDef) {
			Skill skl = this.GetSkillObject(skillDef);
			if (skl != null) {
				return skl.ModifiedValue;
			} else {
				return 0;
			}
		}

		public int GetModifiedSkillValue(int id) {
			return this.GetSkill(id);
		}

		public int GetModifiedSkillValue(SkillName id) {
			return this.GetSkill(id);
		}

		public int GetModifiedSkillValue(AbstractSkillDef skillDef) {
			return this.GetSkill(skillDef);
		}

		public int GetRealSkillValue(int id) {
			ISkill skl = GetSkillObject(id);
			if (skl != null) {
				return skl.ModifiedValue;
			} else {
				return 0;
			}
		}

		public int GetRealSkillValue(SkillName id) {
			return this.GetSkill((int) id);
		}

		public int GetRealSkillValue(AbstractSkillDef skillDef) {
			Skill skl = GetSkillObject(skillDef);
			if (skl != null) {
				return skl.ModifiedValue;
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
				return SkillLockType.Up; //default value
			}
		}

		public SkillLockType GetSkillLockType(SkillName id) {
			return this.GetSkillLockType((int) id);
		}

		public SkillLockType GetSkillLockType(AbstractSkillDef skillDef) {
			Skill skl = this.GetSkillObject(skillDef);
			if (skl != null) {
				return skl.Lock;
			} else {
				return SkillLockType.Up; //default value
			}
		}

		//instantiate new skill object, if needed
		private Skill AcquireSkillObject(AbstractSkillDef skillDef) {
			Sanity.IfTrueSay(skillDef == null, "skillDef == null");
			Skill skill;
			if (!this.HasSkill(skillDef, out skill)) {
				skill = new Skill(skillDef.Id, this);
				this.SkillsAbilities.Add(skillDef, skill);
			}
			return skill;
		}

		private bool HasSkill(AbstractSkillDef skillDef, out Skill skill) {
			if (this.skillsabilities != null) {
				object o;
				if (this.skillsabilities.TryGetValue(skillDef, out o)) {
					skill = (Skill) o;
					return true;
				}
			}
			skill = null;
			return false;
		}

		internal void InternalRemoveSkill(int id) {
			AbstractSkillDef aDef = AbstractSkillDef.GetById(id);
			this.SkillsAbilities.Remove(aDef);
		}

		[Summary("Sphere's command for starting a skill")]
		public void Skill(int skillId) {
			this.SelectSkill(skillId);
		}

		[Summary("Start a skill.")]
		public void SelectSkill(SkillName skillName) {
			SelectSkill((SkillDef) AbstractSkillDef.GetById((int) skillName));
		}

		[Summary("Start a skill.")]
		public void SelectSkill(int skillId) {
			SelectSkill((SkillDef) AbstractSkillDef.GetById(skillId));
		}

		[Summary("Start a skill. "
		+ "Is also called when client does the useskill macro")]
		public override void SelectSkill(AbstractSkillDef skillDef) {
			this.SelectSkill((SkillDef) skillDef);
		}

		[Summary("Start a skill.")]
		public void SelectSkill(SkillDef skillDef) {
			if (skillDef != null) {
				SkillSequenceArgs args = SkillSequenceArgs.Acquire(this, skillDef);
				args.PhaseSelect();
			}
		}

		public void SelectSkill(SkillSequenceArgs skillSeqArgs) {
			Sanity.IfTrueThrow(skillSeqArgs.Self != this, "skillSeqArgs.Self != this");
			skillSeqArgs.PhaseSelect();
		}

		public SkillDef CurrentSkill {
			get {
				SkillSequenceArgs ssa = SkillSequenceArgs.GetSkillSequenceArgs(this);
				if (ssa != null) {
					return ssa.SkillDef;
				}
				return null;
			}
		}

		public SkillSequenceArgs CurrentSkillArgs {
			get {
				return SkillSequenceArgs.GetSkillSequenceArgs(this);
			}
		}

		public SkillSequenceArgs.SkillStrokeTimer CurrentSkillTimer {
			get {
				return SkillSequenceArgs.GetSkillSequenceTimer(this);
			}
		}

		public SkillName CurrentSkillName {
			get {
				SkillSequenceArgs ssa = SkillSequenceArgs.GetSkillSequenceArgs(this);
				if (ssa != null) {
					return (SkillName) ssa.SkillDef.Id;
				}
				return SkillName.None;
			}
		}

		public void AbortSkill() {
			SkillSequenceArgs.AbortSkill(this);
		}

		public virtual bool On_SkillSelect(SkillSequenceArgs skillSeqArgs) {
			return false;
		}

		public virtual bool On_SkillStart(SkillSequenceArgs skillSeqArgs) {
			return false;
		}

		public virtual void On_SkillAbort(SkillSequenceArgs skillSeqArgs) {
		}

		public virtual bool On_SkillFail(SkillSequenceArgs skillSeqArgs) {
			return false;
		}

		public virtual bool On_SkillStroke(SkillSequenceArgs skillSeqArgs) {
			return false;
		}

		public virtual bool On_SkillSuccess(SkillSequenceArgs skillSeqArgs) {
			return false;
		}

		//public virtual bool On_SkillGain(int id, object arg) {
		//	return false;
		//}
		//
		//public virtual bool On_SkillMakeItem(int id, AbstractItem item) {
		//	return false;
		//}
		#endregion Skills

		#region abilities
		public void ActivateAbility(AbilityDef aDef) {
			aDef.Activate(this);
		}

		[Summary("Enumerator of all character's abilities")]
		public IEnumerable<Ability> Abilities {
			get {
				return new AbilitiesEnumerator(this.skillsabilities);
			}
		}

		[Summary("Check if character has the desired ability (according to the ability def)")]
		public bool HasAbility(AbilityDef aDef, out Ability ability) {
			if (this.skillsabilities != null) {
				object o;
				if (this.skillsabilities.TryGetValue(aDef, out o)) {
					ability = (Ability) o; //found ability, cast the return value
					return true;
				}
			}
			ability = null;
			return false;
		}

		public Ability GetAbilityObject(AbilityDef aDef) {
			object retVal = null;
			if (this.skillsabilities != null) {
				this.skillsabilities.TryGetValue(aDef, out retVal);
			}
			return (Ability) retVal; //either null or Ability instance if the player has it
		}

		[Summary("Get number of modified points the character has for specified AbilityDef. Equals getting the ModifiedPoints property value on the Ability object directly.")]
		public int GetAbility(AbilityDef aDef) {
			Ability retAb = GetAbilityObject(aDef);
			return (retAb == null ? 0 : retAb.ModifiedPoints); //either null or Ability.Points if the player has it
		}

		[Summary("Modifies the points of the Ability by given diference. Equals calling ModifyPoints on the Ability object directly.")]
		public void ModifyAbilityPoints(AbilityDef aDef, int difference) {
			Ability ab = this.AcquireAbilityObject(aDef);
			ab.ModifyPoints(difference);
		}

		[Summary("Set specified number of real points the character has for specified AbilityDef. Equals setting the RealPoints property on the Ability object directly.")]
		public void SetRealAbilityPoints(AbilityDef aDef, int points) {
			Ability ab = this.AcquireAbilityObject(aDef);
			ab.RealPoints = points;
		}

		private Ability AcquireAbilityObject(AbilityDef def) {
			Sanity.IfTrueSay(def == null, "def == null");
			Ability ab;
			if (!this.HasAbility(def, out ab)) {
				ab = new Ability(def, this);
				this.SkillsAbilities.Add(def, ab);
			}
			return ab;
		}

		internal void InternalRemoveAbility(AbilityDef aDef) {
			this.SkillsAbilities.Remove(aDef);
		}

		internal virtual void On_AbilityValueChanged(AbilityDef aDef, Ability ab, int previousValue) {
		}

		internal virtual bool On_DenyActivateAbility(DenyAbilityArgs args) {
			return false;
		}

		internal virtual bool On_ActivateAbility(AbilityDef aDef, Ability ab) {
			return false;
		}

		internal virtual bool On_UnActivateAbility(ActivableAbilityDef aDef, Ability ab) {
			return false;
		}

		internal virtual void On_AbilityUnActivate(AbilityDef aDef, Ability ab) {
		}
		#endregion abilities

		#region roles
		[Summary("Check if character has been cast to the given role")]
		public bool HasRole(Role role) {
			return RolesManagement.HasRole(this, role);
		}

		//these triggers might get alive if they prove to be needed. For now I dont think so
		//[Summary("Called after the character has been cast to some role (the role is already in his assignedRoles list")]
		//internal virtual void On_RoleAssign(Role role) {
		//}

		//[Summary("Called after the character has been cast to some role (the role is already in his assignedRoles list")]
		//internal virtual void On_RoleUnAssign(Role role) {
		//}
		#endregion roles

		public override void TryCastSpellFromBook(int spellid) {
			MagerySkillDef.TryCastSpellFromBook(this, spellid);
		}

		public void TryCastSpellFromBook(SpellDef spellDef) {
			MagerySkillDef.TryCastSpellFromBook(this, spellDef);
		}

		public virtual bool On_SpellEffect(SpellEffectArgs spellEffectArgs) {
			return false;
		}

		public virtual bool On_CauseSpellEffect(IPoint3D target, SpellEffectArgs spellEffectArgs) {
			return false;
		}

		/*
			Method: GM
		
				Toggles the plevel of the account between 1 (player) and the account's max plevel.
				Has no effect on players.
		*/
		public void GM() {
			AbstractAccount acc = this.Account;
			if (acc != null) {
				GameState state = acc.GameState;
				if (acc.PLevel < acc.MaxPLevel) {
					acc.PLevel = acc.MaxPLevel;
					state.WriteLine(String.Format(
						Loc<CharacterLoc>.Get(state.Language).GMModeOn,
						acc.PLevel));
				} else {
					acc.PLevel = 1;
					state.WriteLine(Loc<CharacterLoc>.Get(state.Language).GMModeOff);
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
				GameState state = acc.GameState;
				if (i > 0) {
					acc.PLevel = acc.MaxPLevel;
					state.WriteLine(String.Format(
						Loc<CharacterLoc>.Get(state.Language).GMModeOn,
						acc.PLevel));
				} else {
					acc.PLevel = 1;
					state.WriteLine(Loc<CharacterLoc>.Get(state.Language).GMModeOff);
				}
			}
		}

		[Summary("Check if the current character has plevel greater than 1 (is more than player)")]
		public bool IsGM {
			get {
				AbstractAccount acc = this.Account;
				if (acc != null) {
					return acc.PLevel > 1;
				}
				return false;
			}
		}

		public bool IsOwnerOf(Character cre) {
			return cre.IsPetOf(this);
		}

		public bool IsPetOf(Character cre) {
			return true;
		}

		public override bool CanEquipItemsOn(AbstractCharacter chr) {
			Character target = (Character) chr;
			return (this.IsPetOf(target)) && (this.CanReach(target) == DenyResult.Allow);
		}

		//public override bool CanEquip(AbstractItem i) {
		//    return true;
		//}

		public override bool CanRename(AbstractCharacter to) {
			return (this.IsGM) || 
				((this.IsPlayer) && (to != this) && this.IsOwnerOf((Character) to)); //can't rename self
		}

		//method: On_DClick
		//Character`s implementation of @Dclick trigger, 
		//basic implementation = paperdoll sending and horse mounting
		public override void On_DClick(AbstractCharacter from) {
			if (from != null && from.IsPlayer) {
				//PC
				if (from == this && this.Mount != null) {
					this.Dismount();
				} else {
					if (from != this && this.IsMountableBy((Character) from)) {
						from.Mount = this;
					} else { //TODO? paperdoll only for humanoids? show backpack of pets?
						this.ShowPaperdollTo(from);
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

		#region Weight
		public override float Weight {
			get {
				return weight;
			}
		}

		public override void FixWeight() {
			CharSyncQueue.AboutToChangeStats(this);
			float w = Def.Weight;
			foreach (AbstractItem i in this) {
				if (i != null) {
					i.FixWeight();
					w += i.Weight;
				}
			}
			weight = w;
		}

		protected override void AdjustWeight(float adjust) {
			CharSyncQueue.AboutToChangeStats(this);
			this.weight += adjust;
		}
		#endregion Weight

		[Summary("Message displayed in red - used for importatnt system or ingame messages (warnings, errors etc)")]
		public void RedMessage(string arg) {
			this.SysMessage(arg, (int) Hues.Red);
		}

		[Summary("Message displayed in red - used for importatnt system or ingame messages (warnings, errors etc)")]
		public void RedMessageCliloc(int arg) {
			this.ClilocSysMessage(arg, (int) Hues.Red);
		}

		[Summary("Message displayed in blue - used for ingame purposes")]
		public void BlueMessage(string arg) {
			this.SysMessage(arg, (int) Hues.Blue);
		}

		[Summary("Message displayed in green - used for ingame purposes")]
		public void GreenMessage(string arg) {
			this.SysMessage(arg, (int) Hues.Green);
		}

		[Summary("Message displayed in green - used for ingame purposes")]
		public void InfoMessage(string arg) {
			this.SysMessage(arg, (int) Hues.Info);
		}

		public Item Hair {
			get {
				return (Item) this.FindLayer( LayerNames.Hair);
			}
		}

		public Item Beard {
			get {
				return (Item) this.FindLayer(LayerNames.Beard);
			}
		}

		#region combat
		public override void Trigger_PlayerAttackRequest(AbstractCharacter target) {
			if (this == target || target == null) {
				return;
			}

			//TODO? triggers

			WeaponSkillTargetQueuePlugin.AddTarget(this, (Character) target);
		}

		public virtual void On_SkillChange(Skill skill, int oldModifiedValue) {
			switch (skill.Name) {
				case SkillName.Parry: //Efficiency of shield
					this.InvalidateCombatArmorValues();
					break;
				case SkillName.Tactics: //Attack
				case SkillName.Anatomy: //Attack
				case SkillName.ArmsLore: //Attack
				case SkillName.SpiritSpeak: //MindPower
				case SkillName.EvalInt: //MindPower
					this.InvalidateCombatWeaponValues();
					break;
			}
		}

		CombatCalculator.CombatWeaponValues combatWeaponValues;
		CombatCalculator.CombatArmorValues combatArmorValues;
		internal Projectile weaponProjectile;

		public int ArmorClassVsP {
			get {
				this.AcquireCombatArmorValues();
				return this.combatArmorValues.armorVsP;
			}
		}

		public int ArmorClassVsM {
			get {
				this.AcquireCombatArmorValues();
				return this.combatArmorValues.armorVsM;
			}
		}

		public override short StatusArmorClass {
			get {
				this.AcquireCombatArmorValues();
				return (short) ((this.combatArmorValues.armorVsP + this.combatArmorValues.armorVsM) / 2);
			}
		}

		private static TagKey armorClassModifierTK = TagKey.Acquire("_armorClassModifier_");
		public int ArmorClassModifier {
			get {
				return Convert.ToInt32(this.GetTag(armorClassModifierTK));
			}
			set {
				this.InvalidateCombatArmorValues();
				if (value != 0) {
					this.SetTag(armorClassModifierTK, value);
				} else {
					this.RemoveTag(armorClassModifierTK);
				}
			}
		}

		public int MindDefenseVsP {
			get {
				this.AcquireCombatArmorValues();
				return this.combatArmorValues.mindDefenseVsP;
			}
		}

		public int MindDefenseVsM {
			get {
				this.AcquireCombatArmorValues();
				return this.combatArmorValues.mindDefenseVsM;
			}
		}

		public override short StatusMindDefense {
			get {
				this.AcquireCombatArmorValues();
				return (short) ((this.combatArmorValues.mindDefenseVsP + this.combatArmorValues.mindDefenseVsM) / 2);
			}
		}

		private static TagKey mindDefenseModifierTK = TagKey.Acquire("_mindDefenseModifier_");
		public int MindDefenseModifier {
			get {
				return Convert.ToInt32(this.GetTag(mindDefenseModifierTK));
			}
			set {
				this.InvalidateCombatArmorValues();
				if (value != 0) {
					this.SetTag(mindDefenseModifierTK, value);
				} else {
					this.RemoveTag(mindDefenseModifierTK);
				}
			}
		}

		public void InvalidateCombatWeaponValues() {
			if (this.combatWeaponValues != null) {
				CharSyncQueue.AboutToChangeStats(this);
				this.combatWeaponValues = null;
			}
		}

		public void InvalidateCombatArmorValues() {
			if (this.combatArmorValues != null) {
				CharSyncQueue.AboutToChangeStats(this);
				this.combatArmorValues = null;
			}
		}

		private void AcquireCombatWeaponValues() {
			if (this.combatWeaponValues == null) {
				CharSyncQueue.AboutToChangeStats(this);
				this.combatWeaponValues = CombatCalculator.CalculateCombatWeaponValues(this);
			}
		}

		private void AcquireCombatArmorValues() {
			if (this.combatArmorValues == null) {
				CharSyncQueue.AboutToChangeStats(this);
				this.combatArmorValues = CombatCalculator.CalculateCombatArmorValues(this);
			}
		}

		public override void On_ItemEnter(ItemInCharArgs args) {
			if (args.ManipulatedItem is Wearable) {
				this.InvalidateCombatArmorValues();
			} else if (args.ManipulatedItem is Weapon) {
				this.InvalidateCombatWeaponValues();
			}
			base.On_ItemEnter(args);
		}

		public override void On_ItemLeave(ItemInCharArgs args) {
			if (args.ManipulatedItem is Wearable) {
				this.InvalidateCombatArmorValues();
			} else if (args.ManipulatedItem is Weapon) {
				this.InvalidateCombatWeaponValues();
			}
			base.On_ItemLeave(args);
		}

		public virtual bool IsPlayerForCombat {
			get {
				//TODO: false for hypnomystic
				return this.IsPlayer;
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
				this.AcquireCombatWeaponValues();
				return this.combatWeaponValues.weapon;
			}
		}

		public int WeaponAttackVsP {
			get {
				this.AcquireCombatWeaponValues();
				return (int) this.combatWeaponValues.attackVsP;
			}
		}

		public int WeaponAttackVsM {
			get {
				this.AcquireCombatWeaponValues();
				return (int) this.combatWeaponValues.attackVsM;
			}
		}

		public int WeaponPiercing {
			get {
				this.AcquireCombatWeaponValues();
				return (int) this.combatWeaponValues.piercing;
			}
		}

		public WeaponType WeaponType {
			get {
				this.AcquireCombatWeaponValues();
				return this.combatWeaponValues.weaponType;
			}
		}

		public DamageType WeaponDamageType {
			get {
				this.AcquireCombatWeaponValues();
				return this.combatWeaponValues.damageType;
			}
		}

		public WeaponAnimType WeaponAnimType {
			get {
				this.AcquireCombatWeaponValues();
				return this.combatWeaponValues.weaponAnimType;
			}
		}

		public Projectile WeaponProjectile {
			get {
				if ((this.weaponProjectile != null) &&
						(this.weaponProjectile.IsDeleted ||
						(this.weaponProjectile.TopObj() != this) ||
						(this.weaponProjectile.Amount < 1))) {
					this.weaponProjectile = null;//we had ammo but now don't have it anymore
					this.InvalidateCombatWeaponValues();
				} else if ((weaponProjectile == null) && (this.combatWeaponValues != null) &&
						(this.combatWeaponValues.projectileType != ProjectileType.None)) {
					this.InvalidateCombatWeaponValues();//we have no ammo but we should, let's look for it
				}
				this.AcquireCombatWeaponValues();
				return this.weaponProjectile;
			}
		}

		public int WeaponProjectileAnim {
			get {
				this.AcquireCombatWeaponValues();
				return this.combatWeaponValues.projectileAnim;
			}
		}

		public ProjectileType WeaponProjectileType {
			get {
				this.AcquireCombatWeaponValues();
				return this.combatWeaponValues.projectileType;
			}
		}

		public int WeaponRangeVsM {
			get {
				this.AcquireCombatWeaponValues();
				return this.combatWeaponValues.rangeVsM;
			}
		}

		public int WeaponRangeVsP {
			get {
				this.AcquireCombatWeaponValues();
				return this.combatWeaponValues.rangeVsP;
			}
		}

		private static TagKey weaponRangeModifierTK = TagKey.Acquire("_weaponRangeModifier_");
		public int WeaponRangeModifier {
			get {
				return Convert.ToInt32(this.GetTag(weaponRangeModifierTK));
			}
			set {
				this.InvalidateCombatWeaponValues();
				if (value != 0) {
					this.SetTag(weaponRangeModifierTK, value);
				} else {
					this.RemoveTag(weaponRangeModifierTK);
				}
			}
		}

		public int WeaponStrikeStartRange {
			get {
				this.AcquireCombatWeaponValues();
				return this.combatWeaponValues.strikeStartRange;
			}
		}

		public int WeaponStrikeStopRange {
			get {
				this.AcquireCombatWeaponValues();
				return this.combatWeaponValues.strikeStopRange;
			}
		}

		public TimeSpan WeaponDelay {
			get {
				//TODO: mana-dependant for mystic
				this.AcquireCombatWeaponValues();
				return this.combatWeaponValues.delay;
			}
		}

		public int MindPowerVsP {
			get {
				this.AcquireCombatWeaponValues();
				return (int) this.combatWeaponValues.mindPowerVsP;
			}
		}

		public int MindPowerVsM {
			get {
				this.AcquireCombatWeaponValues();
				return (int) this.combatWeaponValues.mindPowerVsM;
			}
		}

		public bool On_BeforeSwing(WeaponSwingArgs args) {
			return false;
		}

		public bool On_BeforeGetSwing(WeaponSwingArgs args) {
			return false;
		}

		public void On_CauseDamage(DamageArgs args) {
		}

		public void On_Damage(DamageArgs args) {
		}

		public void On_AfterSwing(WeaponSwingArgs args) {
		}

		public void On_AfterGetSwing(WeaponSwingArgs args) {
		}

		[Summary("hodi zbran do batohu")]
		public void DisArm() {
			Weapon w = this.Weapon;
			if (w != null)
				w.Cont = this.Backpack;
		}
		#endregion combat

		public override void On_LogOut() {
			this.AbortSkill();
			Dialogs.DialogStacking.ClearDialogStack(this);
			base.On_LogOut();
		}


		public CharModelInfo CharModelInfo {
			get {
				int model = this.Model;
				if ((this.charModelInfo == null) || (this.charModelInfo.model != model)) {
					this.charModelInfo = CharModelInfo.GetByModel(model);
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
				GameState state = this.GameState;
				if (state != null) {
					PacketSequences.SendDenyResultMessage(state.Conn, target, result);
				}
				return false;
			}
		}

		public override DenyResult CanOpenContainer(AbstractItem targetContainer) {
			if (this.IsGM) {
				return DenyResult.Allow;
			}

			if (!this.IsOnline) {
				return DenyResult.Deny_NoMessage;
			}

			//TODO zamykani kontejneru

			DenyResult result = DenyResult.Allow;

			Thing c = targetContainer.Cont;
			if (c != null) {
				Item contAsItem = c as Item;
				if (contAsItem != null) {
					return OpenedContainers.HasContainerOpen(this, contAsItem);
				} else if (c != this) {
					result = this.CanReach(c);
					if (result == DenyResult.Allow) {
						Character contAsChar = (Character) c;
						if (!contAsChar.IsPetOf(this)) {//not my pet or myself
							return DenyResult.Deny_ThatDoesNotBelongToYou;
						}
					}
				}
			} else {
				return this.CanReachCoordinates(targetContainer);
			}

			//equipped in visible layer?
			if (targetContainer.Z > (int) LayerNames.Mount) {
				return DenyResult.Deny_RemoveFromView;
			}

			return result;
		}


		public virtual bool On_DenyOpenDoor(DenySwitchDoorArgs args) {
			return false;
		}

		public virtual bool On_DenyCloseDoor(DenySwitchDoorArgs args) {
			return false;
		}

		public Party Party {
			get {
				return Party.GetParty(this);
			}
		}

		public override ICollection<AbstractCharacter> PartyMembers {
			get {
				Party p = Party.GetParty(this);
				if (p != null) {
					return (ICollection<AbstractCharacter>) p.Members;
				}
				return EmptyReadOnlyGenericCollection<AbstractCharacter>.instance;
			}
		}

		public virtual void On_Dispell(SpellEffectArgs spellEffectArgs) {
		}
	}

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
				int model = this.Model;
				if ((this.charModelInfo == null) || (this.charModelInfo.model != model)) {
					this.charModelInfo = CharModelInfo.GetByModel(model);
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

	internal class CharacterLoc : CompiledLocStringCollection {
		public string GMModeOn = "GM mode on (Plevel {0}).";
		public string GMModeOff = "GM mode off (Plevel 1).";
	}
}
