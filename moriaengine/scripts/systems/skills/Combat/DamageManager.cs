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
using SteamEngine.Common;
using SteamEngine;

namespace SteamEngine.CompiledScripts {

	public static class DamageManager {
		public static double GetResistModifier(Character resistingChar, DamageType damageType) {
			int intDamageType = (int) damageType;

			//phase 1
			double isMagic = ((intDamageType & 0x0001) == 0) ? 0.0 : 1;
			double isPhysical = ((intDamageType & 0x0002) == 0) ? 0.0 : 1;

			double modifier = 1;
			double resistCount = isMagic + isPhysical;
			modifier = modifier * resistCount;
			Sanity.IfTrueThrow(resistCount == 0, "Attack of type '" + damageType + "' - neither magical nor physical");

			modifier -= isMagic * resistingChar.ResistMagic;
			modifier -= isPhysical * resistingChar.ResistPhysical;

			//phase 2
			double isFire = ((intDamageType & (int) DamageType.Fire) == 0) ? 0.0 : 1;
			double isElectric = ((intDamageType & (int) DamageType.Electric) == 0) ? 0.0 : 1;
			double isAcid = ((intDamageType & (int) DamageType.Acid) == 0) ? 0.0 : 1;
			double isCold = ((intDamageType & (int) DamageType.Cold) == 0) ? 0.0 : 1;
			double isPoison = ((intDamageType & (int) DamageType.Poison) == 0) ? 0.0 : 1;
			double isMystical = ((intDamageType & (int) DamageType.Mystical) == 0) ? 0.0 : 1;
			double isSlashing = ((intDamageType & (int) DamageType.Slashing) == 0) ? 0.0 : 1;
			double isStabbing = ((intDamageType & (int) DamageType.Stabbing) == 0) ? 0.0 : 1;
			double isBlunt = ((intDamageType & (int) DamageType.Blunt) == 0) ? 0.0 : 1;
			double isArchery = ((intDamageType & (int) DamageType.Archery) == 0) ? 0.0 : 1;
			double isBleed = ((intDamageType & (int) DamageType.Bleed) == 0) ? 0.0 : 1;
			double isSummon = ((intDamageType & (int) DamageType.Summon) == 0) ? 0.0 : 1;
			double isDragon = ((intDamageType & (int) DamageType.Dragon) == 0) ? 0.0 : 1;

			resistCount = isFire + isElectric + isAcid + isCold +
				isPoison + isMystical + isSlashing + isStabbing +
				isBlunt + isArchery + isBleed + isSummon + isDragon;
			Sanity.IfTrueThrow(resistCount == 0, "Attack of type '" + damageType + "' - has no resist subtype");
			
			modifier = modifier * resistCount;

			modifier -= isFire * resistingChar.ResistFire;
			modifier -= isElectric * resistingChar.ResistElectric;
			modifier -= isAcid * resistingChar.ResistAcid;
			modifier -= isCold * resistingChar.ResistCold;
			modifier -= isPoison * resistingChar.ResistPoison;
			modifier -= isMystical * resistingChar.ResistMystical;
			modifier -= isSlashing * resistingChar.ResistSlashing;
			modifier -= isStabbing * resistingChar.ResistStabbing;
			modifier -= isBlunt * resistingChar.ResistBlunt;
			modifier -= isArchery * resistingChar.ResistArchery;
			modifier -= isBleed * resistingChar.ResistBleed;
			modifier -= isSummon * resistingChar.ResistSummon;
			modifier -= isDragon * resistingChar.ResistDragon;

			return modifier;
		}

		public static WeaponSwingArgs GetWeaponSwingArgs(Character attacker, Character defender) {
			CombatSettings settings = CombatSettings.instance;
			double attack = settings.weapAttack;
			double piercing = attacker.WeaponPiercing;

			bool defenderIsPlayer = defender.IsPlayerForCombat;
			bool attackerIsPlayer = attacker.IsPlayerForCombat;

			double armorClass;
			if (attackerIsPlayer) {
				if (defenderIsPlayer) {
					attack *= settings.weapAttackPvP;
				}
				armorClass = defender.ArmorClassVsP;
			} else {
				attack *= settings.weapAttackM;
				armorClass = defender.ArmorClassVsM;
			}
			if (defenderIsPlayer) {
				attack *= attacker.WeaponAttackVsP;
				armorClass *= settings.armorClassP;
			} else {
				attack *= attacker.WeaponAttackVsM;
				attack *= settings.weapAttackVsM;
				armorClass *= settings.armorClassM;
			}


			return new WeaponSwingArgs(attacker, defender, attack, piercing, armorClass);
		}


		static TriggerKey beforeSwingTK = TriggerKey.Acquire("beforeSwing");
		static TriggerKey beforeGetSwingTK = TriggerKey.Acquire("beforeGetSwing");

		[Summary("Happens before applying armor, can be cancelled.")]
		public static bool Trigger_BeforeSwing(WeaponSwingArgs swingArgs) {
			if (!swingArgs.attacker.TryCancellableTrigger(beforeSwingTK, swingArgs)) {
				try {
					if (swingArgs.attacker.On_BeforeSwing(swingArgs)) {
						return true;
					}
				} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
				if (!swingArgs.defender.TryCancellableTrigger(beforeGetSwingTK, swingArgs)) {
					try {
						if (swingArgs.defender.On_BeforeGetSwing(swingArgs)) {
							return true;
						}
					} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
					return false;
				}
			}
			return true;
		}

		static TriggerKey causeDamageTK = TriggerKey.Acquire("causeDamage");
		static TriggerKey damageTK = TriggerKey.Acquire("damage");

		[Summary("Happens before applying armor, can be cancelled.")]
		public static bool Trigger_Damage(DamageArgs damageArgs) {
			if (!damageArgs.attacker.TryCancellableTrigger(causeDamageTK, damageArgs)) {
				try {
					if (damageArgs.attacker.On_CauseDamage(damageArgs)) {
						return true;
					}
				} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
				if (!damageArgs.defender.TryCancellableTrigger(damageTK, damageArgs)) {
					try {
						if (damageArgs.defender.On_Damage(damageArgs)) {
							return true;
						}
					} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
					return false;
				}
			}
			return true;
		}

		static TriggerKey afterSwingTK = TriggerKey.Acquire("afterSwing");
		static TriggerKey afterGetSwingTK = TriggerKey.Acquire("afterGetSwing");

		[Summary("Happens after applying armor, can be cancelled.")]
		public static void Trigger_AfterSwing(WeaponSwingArgs swingArgs) {
			swingArgs.attacker.TryTrigger(afterSwingTK, swingArgs);

			try {
				swingArgs.attacker.On_AfterSwing(swingArgs);
			} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }

			if (!swingArgs.defender.IsDeleted) {
				swingArgs.defender.TryTrigger(afterGetSwingTK, swingArgs);

				try {
					swingArgs.defender.On_AfterGetSwing(swingArgs);
				} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
			}
		}

		public static void ApplyArmorClass(WeaponSwingArgs swingArgs) {
			bool defenderIsPlayer = swingArgs.defender.IsPlayerForCombat;
			bool attackerIsPlayer = swingArgs.attacker.IsPlayerForCombat;
			CombatSettings settings = CombatSettings.instance;

			double damageMod = 1.0;

			bool isMvP = false;
			if (defenderIsPlayer) {
				if (attackerIsPlayer) {
					damageMod = settings.swingDamagePvP;
				} else {
					damageMod = ScriptUtil.GetRandInRange(settings.swingDamageRandMvPMin, settings.swingDamageRandMvPMax);
					isMvP = true;
				}
			} else {
				damageMod = settings.swingDamageM;
			}

			double armor = swingArgs.ArmorClass * (1000 - swingArgs.Piercing) / 1000;
			if (!isMvP) {
				armor = ScriptUtil.GetRandInRange(settings.armorRandEffectMin, settings.armorRandEffectMax);
			}

			armor = Math.Max(0, armor);
			swingArgs.Attack = Math.Max(0, swingArgs.Attack);
			damageMod = Math.Max(0, damageMod);

			//!!TODO!! ruznej vzorec pro pvm a pvm
			//if (defenderIsPlayer && !attackerIsPlayer) { //damagecust_MvP 
			//	armorClass *= settings.armorClassMvP; 
			//}
			swingArgs.DamageAfterAC = (swingArgs.Attack - armor) * damageMod;
		}

		public static double CauseDamage(Character attacker, Character defender, DamageType flags, double damage) {
			if (!defender.IsDeleted && !defender.Flag_Dead && !defender.Flag_Insubst) {
				defender.Trigger_HostileAction(attacker);
				defender.Trigger_Disrupt();//nebo jen pri damage > 0 ?

				damage *= GetResistModifier(defender, flags);

				DamageArgs damageArgs = new DamageArgs(attacker, defender, flags, damage);
				if (!Trigger_Damage(damageArgs)) {

					damage = damageArgs.Damage;
					if (damage > 0.5) {//0.5 gets rounded to 0...


						defender.Hits = (short) (defender.Hits - damage);

						//TODO create blood?
						SoundCalculator.PlayHurtSound(defender);
						AnimCalculator.PerformAnim(defender, GenericAnim.GetHit);

						return damage;
					}
				}
			}

			return 0;
		}

		public static double ProcessSwing(Character attacker, Character defender) {
			WeaponSwingArgs swingArgs = GetWeaponSwingArgs(attacker, defender);
			SoundCalculator.PlayAttackSound(attacker);
			double retVal = 0;

			if (!Trigger_BeforeSwing(swingArgs)) {
				ApplyArmorClass(swingArgs);

				retVal = CauseDamage(attacker, defender, attacker.WeaponDamageType, swingArgs.DamageAfterAC);

				swingArgs.InternalSetFinalDamage(retVal);

				Trigger_AfterSwing(swingArgs);
			}

			return retVal;
		}
	}

	public class DamageArgs : ScriptArgs {
		public readonly Character defender;
		public readonly Character attacker;
		public readonly DamageType flags;

		public DamageArgs(Character attacker, Character defender, DamageType flags, double damage)
			: base(attacker, defender, flags, damage) {
			this.defender = defender;
			this.attacker = attacker;
			this.flags = flags;
		}

		public double Damage {
			get {
				return Convert.ToDouble(this.Argv[3]);
			}
			set {
				this.Argv[3] = value;
			}
		}
	}

	public class WeaponSwingArgs : ScriptArgs {
		public readonly Character defender;
		public readonly Character attacker;

		public WeaponSwingArgs(Character attacker, Character defender, double attack, double piercing, double armorClass)
			: base(attacker, defender, attack, piercing, armorClass, -1, -1) {
			this.attacker = attacker;
			this.defender = defender;
		}

		public double Attack {
			get {
				return Convert.ToDouble(this.Argv[2]);
			}
			set {
				this.Argv[2] = value;
			}
		}

		public double Piercing {
			get {
				return Convert.ToDouble(this.Argv[3]);
			}
			set {
				this.Argv[3] = value;
			}
		}

		public double ArmorClass {
			get {
				return Convert.ToDouble(this.Argv[4]);
			}
			set {
				this.Argv[4] = value;
			}
		}

		public double DamageAfterAC {
			get {
				return Convert.ToDouble(this.Argv[5]);
			}
			set {
				this.Argv[5] = value;
			}
		}

		public double FinalDamage {
			get {
				return Convert.ToDouble(this.Argv[6]);
			}
		}

		internal  void InternalSetFinalDamage(double value) {
			this.Argv[6] = value;
		}
	}
}

