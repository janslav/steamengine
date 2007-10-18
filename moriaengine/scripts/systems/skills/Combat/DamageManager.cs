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
			double hasResistMagic =		((intDamageType & 0x0001) == 0)? 0.0 : 1;
			double hasResistPhysical =	((intDamageType & 0x0002) == 0)? 0.0 : 1;

			double modifier = 1;
			double resistCount = hasResistMagic + hasResistPhysical;
			modifier = modifier * resistCount;

			modifier -= hasResistMagic * resistingChar.ResistMagic;
			modifier -= hasResistPhysical * resistingChar.ResistPhysical;

			//phase 2
			double hasResistFire =		((intDamageType & 0x0004) == 0)? 0.0 : 1;
			double hasResistElectric =	((intDamageType & 0x0008) == 0)? 0.0 : 1;
			double hasResistAcid =		((intDamageType & 0x0010) == 0)? 0.0 : 1;
			double hasResistCold =		((intDamageType & 0x0020) == 0)? 0.0 : 1;
			double hasResistPoison =	((intDamageType & 0x0040) == 0)? 0.0 : 1;
			double hasResistMystical =	((intDamageType & 0x0080) == 0)? 0.0 : 1;
			double hasResistSlashing =	((intDamageType & 0x0100) == 0)? 0.0 : 1;
			double hasResistStabbing =	((intDamageType & 0x0200) == 0)? 0.0 : 1;
			double hasResistBlunt =		((intDamageType & 0x0400) == 0)? 0.0 : 1;
			double hasResistArchery =	((intDamageType & 0x0800) == 0)? 0.0 : 1;
			double hasResistBleed =		((intDamageType & 0x1000) == 0)? 0.0 : 1;
			double hasResistSummon =	((intDamageType & 0x2000) == 0)? 0.0 : 1;
			double hasResistDragon =	((intDamageType & 0x4000) == 0)? 0.0 : 1;

			resistCount = hasResistFire + hasResistElectric + hasResistAcid + hasResistCold +
				hasResistPoison + hasResistMystical + hasResistSlashing + hasResistStabbing +
				hasResistBlunt + hasResistArchery + hasResistBleed + hasResistSummon + hasResistDragon;
			modifier = modifier * resistCount;

			modifier -= hasResistFire * resistingChar.ResistFire;
			modifier -= hasResistElectric * resistingChar.ResistElectric;
			modifier -= hasResistAcid * resistingChar.ResistAcid;
			modifier -= hasResistCold * resistingChar.ResistCold;
			modifier -= hasResistPoison * resistingChar.ResistPoison;
			modifier -= hasResistMystical * resistingChar.ResistMystical;
			modifier -= hasResistSlashing * resistingChar.ResistSlashing;
			modifier -= hasResistStabbing * resistingChar.ResistStabbing;
			modifier -= hasResistBlunt * resistingChar.ResistBlunt;
			modifier -= hasResistArchery * resistingChar.ResistArchery;
			modifier -= hasResistBleed * resistingChar.ResistBleed;
			modifier -= hasResistSummon * resistingChar.ResistSummon;
			modifier -= hasResistDragon * resistingChar.ResistDragon;

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
				if (!defenderIsPlayer) {
					armorClass *= settings.armorClassMvP;//damagecust_MvP
				}
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


		static TriggerKey beforeSwingTK = TriggerKey.Get("beforeSwing");
		static TriggerKey beforeGetSwingTK = TriggerKey.Get("beforeGetSwing");

		[Remark("Happens before applying armor, can be cancelled.")]
		public static bool Trigger_BeforeSwing(WeaponSwingArgs swingArgs) {
			ScriptArgs sa = new ScriptArgs(swingArgs);

			if (!swingArgs.attacker.TryCancellableTrigger(beforeSwingTK, sa)) {
				try {
					if (swingArgs.attacker.On_BeforeSwing(swingArgs)) {
						return true;
					}
				} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
				if (!swingArgs.defender.TryCancellableTrigger(beforeGetSwingTK, sa)) {
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

		static TriggerKey causeDamageTK = TriggerKey.Get("causeDamage");
		static TriggerKey damageTK = TriggerKey.Get("damage");

		[Remark("Happens before applying armor, can be cancelled.")]
		public static bool Trigger_Damage(DamageArgs damageArgs) {
			ScriptArgs sa = new ScriptArgs(damageArgs);

			if (!damageArgs.attacker.TryCancellableTrigger(causeDamageTK, sa)) {
				try {
					if (damageArgs.attacker.On_CauseDamage(damageArgs)) {
						return true;
					}
				} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
				if (!damageArgs.defender.TryCancellableTrigger(damageTK, sa)) {
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

		static TriggerKey afterSwingTK = TriggerKey.Get("afterSwing");
		static TriggerKey afterGetSwingTK = TriggerKey.Get("afterGetSwing");

		[Remark("Happens after applying armor, can be cancelled.")]
		public static void Trigger_AfterSwing(WeaponSwingArgs swingArgs) {
			ScriptArgs sa = new ScriptArgs(swingArgs);

			swingArgs.attacker.TryTrigger(afterSwingTK, sa);

			try {
				swingArgs.attacker.On_AfterSwing(swingArgs);
			} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }

			if (!swingArgs.defender.IsDeleted) {
				swingArgs.defender.TryTrigger(afterGetSwingTK, sa);

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

			double armor = swingArgs.armorClass * (1000-swingArgs.piercing) / 1000;
			if (!isMvP) {
				armor = ScriptUtil.GetRandInRange(settings.armorRandEffectMin, settings.armorRandEffectMin);
			}

			armor = Math.Max(0, armor);
			swingArgs.attack = Math.Max(0, swingArgs.attack);
			damageMod = Math.Max(0, damageMod);

			swingArgs.damageAfterAC = (swingArgs.attack - armor) * damageMod;
		}

		public static double CauseDamage(Character attacker, Character defender, DamageType flags, double damage) {
			defender.Trigger_HostileAction(attacker);
			defender.Trigger_Disrupt();//nebo jen pri damage > 0 ?

			damage *= GetResistModifier(defender, flags);

			DamageArgs damageArgs = new DamageArgs(attacker, defender, flags, damage);
			if (!Trigger_Damage(damageArgs)) {

				if (damageArgs.damage > 0.5) {//0.5 gets rounded to 0...


					defender.Hits = (short) (defender.Hits - damageArgs.damage);

					if (!defender.IsDeleted && !defender.Flag_Dead) {
						//TODO create blood, sound?

						SoundCalculator.PlayHurtSound(defender);
						AnimCalculator.PerformAnim(defender, GenericAnim.GetHit);
					}

					return damageArgs.damage;
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

				retVal = CauseDamage(attacker, defender, attacker.WeaponDamageType, swingArgs.damageAfterAC);

				swingArgs.finalDamage = retVal;

				Trigger_AfterSwing(swingArgs);
			}

			return retVal;
		}
	}

	public class DamageArgs {
		public readonly Character defender;
		public readonly Character attacker;

		public readonly DamageType flags;
		public double damage;

		public DamageArgs(Character attacker, Character defender, DamageType flags, double damage) {
			this.defender = defender;
			this.attacker = attacker;
			this.flags = flags;
			this.damage = damage;
		}
	}

	public class WeaponSwingArgs {
		public readonly Character defender;
		public readonly Character attacker;
		public double attack;
		public double piercing;
		public double armorClass;

		public double damageAfterAC = -1;
		public double finalDamage = -1;

		public WeaponSwingArgs(Character attacker, Character defender, double attack, double piercing, double armorClass) {
			this.defender = defender;
			this.attacker = attacker;
			this.attack = attack;
			this.piercing = piercing;
			this.armorClass = armorClass;
		}
	}
}

