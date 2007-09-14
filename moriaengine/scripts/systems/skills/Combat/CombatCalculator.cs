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

	public static class CombatCalculator {
		public static double GetResistModifier(Character resistingChar, DamageType damageType) {
			int intDamageType = (int) damageType;

			//phase 1
			double hasResistMagic =		((intDamageType & 0x0001) == 0)? 0.0 : 0.001;
			double hasResistPhysical =	((intDamageType & 0x0002) == 0)? 0.0 : 0.001;

			double modifier = 1000;
			double resistCount = hasResistMagic + hasResistPhysical;
			modifier = modifier * resistCount;

			modifier -= hasResistMagic * resistingChar.ResistMagic;
			modifier -= hasResistPhysical * resistingChar.ResistPhysical;

			//phase 2
			double hasResistFire =		((intDamageType & 0x0004) == 0)? 0.0 : 0.001;
			double hasResistElectric =	((intDamageType & 0x0008) == 0)? 0.0 : 0.001;
			double hasResistAcid =		((intDamageType & 0x0010) == 0)? 0.0 : 0.001;
			double hasResistCold =		((intDamageType & 0x0020) == 0)? 0.0 : 0.001;
			double hasResistPoison =	((intDamageType & 0x0040) == 0)? 0.0 : 0.001;
			double hasResistMystical =	((intDamageType & 0x0080) == 0)? 0.0 : 0.001;
			double hasResistSlashing =	((intDamageType & 0x0100) == 0)? 0.0 : 0.001;
			double hasResistStabbing =	((intDamageType & 0x0200) == 0)? 0.0 : 0.001;
			double hasResistBlunt =		((intDamageType & 0x0400) == 0)? 0.0 : 0.001;
			double hasResistArchery =	((intDamageType & 0x0800) == 0)? 0.0 : 0.001;
			double hasResistBleed =		((intDamageType & 0x1000) == 0)? 0.0 : 0.001;
			double hasResistSummon =	((intDamageType & 0x2000) == 0)? 0.0 : 0.001;
			double hasResistDragon =	((intDamageType & 0x4000) == 0)? 0.0 : 0.001;

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

		public static void CalculateWornArmor(Character ch, out int armorClassVsP, out int mindDefenseVsP, out int armorClassVsM, out int mindDefenseVsM) {
			int resist = SkillDef.SkillValueOfChar(ch, SkillName.MagicResist);
			double resistEffect = SkillDef.ById(SkillName.MagicResist).GetEffectForChar(ch);
			mindDefenseVsP = (int) (
				(resist * resistEffect) / 1000);
			mindDefenseVsM = mindDefenseVsP;

			armorClassVsP = ch.DefForCombat.ArmorVsP;
			armorClassVsM = ch.DefForCombat.ArmorVsM;

			//we calculate worn armor only for players
			if (ch.IsPlayerForCombat) {
				double armorVsPHead = 0;
				double armorVsPNeck = 0;
				double armorVsPBack = 0;
				double armorVsPChest = 0;
				double armorVsPArms = 0;
				double armorVsPHands = 0;
				double armorVsPLegs = 0;
				double armorVsPFeet = 0;

				double armorVsMHead = 0;
				double armorVsMNeck = 0;
				double armorVsMBack = 0;
				double armorVsMChest = 0;
				double armorVsMArms = 0;
				double armorVsMHands = 0;
				double armorVsMLegs = 0;
				double armorVsMFeet = 0;

				double mindDefVsPHead = 0;
				double mindDefVsPNeck = 0;
				double mindDefVsPBack = 0;
				double mindDefVsPChest = 0;
				double mindDefVsPArms = 0;
				double mindDefVsPHands = 0;
				double mindDefVsPLegs = 0;
				double mindDefVsPFeet = 0;

				double mindDefVsMHead = 0;
				double mindDefVsMNeck = 0;
				double mindDefVsMBack = 0;
				double mindDefVsMChest = 0;
				double mindDefVsMArms = 0;
				double mindDefVsMHands = 0;
				double mindDefVsMLegs = 0;
				double mindDefVsMFeet = 0;

				double armorVsPTotal = 0;
				double armorVsMTotal = 0;

				foreach (Item equipped in ch.GetVisibleEquip()) {
					Wearable wearable = equipped as Wearable;
					if (wearable != null) {
						Layers layer = (Layers) wearable.Z;
						int armorVsP = wearable.ArmorVsP;
						int mindDefVsP = wearable.MindDefenseVsP;
						int armorVsM = wearable.ArmorVsM;
						int mindDefVsM = wearable.MindDefenseVsM;
						if ((armorVsP > 0) || (mindDefVsP > 0) || (armorVsM > 0) || (mindDefVsM > 0)) {
							switch (layer) {
								case Layers.layer_helm:
									armorVsPHead = Math.Max(armorVsPHead, armorVsP);
									mindDefVsPHead = Math.Max(mindDefVsPHead, mindDefVsP);
									armorVsMHead = Math.Max(armorVsMHead, armorVsM);
									mindDefVsMHead = Math.Max(mindDefVsMHead, mindDefVsM); 
									break;
								case Layers.layer_collar:
									armorVsPNeck = Math.Max(armorVsPNeck, armorVsP);
									mindDefVsPNeck = Math.Max(mindDefVsPNeck, mindDefVsP);
									armorVsMNeck = Math.Max(armorVsMNeck, armorVsM);
									mindDefVsMNeck = Math.Max(mindDefVsMNeck, mindDefVsM); 
									break;
								case Layers.layer_shirt:
								case Layers.layer_chest:       // 13 = armor chest
								case Layers.layer_tunic:       // 17 = jester suit
									armorVsPBack = Math.Max(armorVsPBack, armorVsP);
									armorVsPChest = Math.Max(armorVsPChest, armorVsP);
									mindDefVsPBack = Math.Max(mindDefVsPBack, mindDefVsP);
									mindDefVsPChest = Math.Max(mindDefVsPChest, mindDefVsP);
									armorVsMBack = Math.Max(armorVsMBack, armorVsM);
									armorVsMChest = Math.Max(armorVsMChest, armorVsM);
									mindDefVsMBack = Math.Max(mindDefVsMBack, mindDefVsM);
									mindDefVsMChest = Math.Max(mindDefVsMChest, mindDefVsM); 
									break;
								case Layers.layer_arms:                // 19 = armor
									armorVsPArms = Math.Max(armorVsPArms, armorVsP);
									mindDefVsPArms = Math.Max(mindDefVsPArms, mindDefVsP);
									armorVsMArms = Math.Max(armorVsMArms, armorVsM);
									mindDefVsMArms = Math.Max(mindDefVsMArms, mindDefVsM); 
									break;
								case Layers.layer_pants:
								case Layers.layer_skirt:
								case Layers.layer_half_apron:
									armorVsPLegs = Math.Max(armorVsPLegs, armorVsP);
									mindDefVsPLegs = Math.Max(mindDefVsPLegs, mindDefVsM);
									armorVsMLegs = Math.Max(armorVsMLegs, armorVsM);
									mindDefVsMLegs = Math.Max(mindDefVsMLegs, mindDefVsM); 
									break;
								case Layers.layer_shoes:
									armorVsPFeet = Math.Max(armorVsPFeet, armorVsP);
									mindDefVsPFeet = Math.Max(mindDefVsPFeet, mindDefVsP);
									armorVsMFeet = Math.Max(armorVsMFeet, armorVsM);
									mindDefVsMFeet = Math.Max(mindDefVsMFeet, mindDefVsM); 
									break;
								case Layers.layer_gloves:      // 7
									armorVsPHands = Math.Max(armorVsPHands, armorVsP);
									mindDefVsPHands = Math.Max(mindDefVsPHands, mindDefVsP);
									armorVsMHands = Math.Max(armorVsMHands, armorVsM);
									mindDefVsMHands = Math.Max(mindDefVsMHands, mindDefVsM); 
									break;
								case Layers.layer_cape:                // 20 = cape
									armorVsPBack = Math.Max(armorVsPBack, armorVsP);
									armorVsPArms = Math.Max(armorVsPArms, armorVsP);
									mindDefVsPBack = Math.Max(mindDefVsPBack, mindDefVsP);
									mindDefVsPArms = Math.Max(mindDefVsPArms, mindDefVsP);
									armorVsMBack = Math.Max(armorVsMBack, armorVsM);
									armorVsMArms = Math.Max(armorVsMArms, armorVsM);
									mindDefVsMBack = Math.Max(mindDefVsMBack, mindDefVsM);
									mindDefVsMArms = Math.Max(mindDefVsMArms, mindDefVsM); 
									break;
								case Layers.layer_robe:                // 22 = robe over all.
									armorVsPBack = Math.Max(armorVsPBack, armorVsP);
									armorVsPChest = Math.Max(armorVsPChest, armorVsP);
									armorVsPArms = Math.Max(armorVsPArms, armorVsP);
									armorVsPLegs = Math.Max(armorVsPLegs, armorVsP); ;
									mindDefVsPBack = Math.Max(mindDefVsPBack, mindDefVsP);
									mindDefVsPChest = Math.Max(mindDefVsPChest, mindDefVsP);
									mindDefVsPArms = Math.Max(mindDefVsPArms, mindDefVsP);
									mindDefVsPLegs = Math.Max(mindDefVsPLegs, mindDefVsP);
									armorVsMBack = Math.Max(armorVsMBack, armorVsM);
									armorVsMChest = Math.Max(armorVsMChest, armorVsM);
									armorVsMArms = Math.Max(armorVsMArms, armorVsM);
									armorVsMLegs = Math.Max(armorVsMLegs, armorVsM); ;
									mindDefVsMBack = Math.Max(mindDefVsMBack, mindDefVsM);
									mindDefVsMChest = Math.Max(mindDefVsMChest, mindDefVsM);
									mindDefVsMArms = Math.Max(mindDefVsMArms, mindDefVsM);
									mindDefVsMLegs = Math.Max(mindDefVsMLegs, mindDefVsM); 
									break;
								case Layers.layer_legs:
									armorVsPLegs = Math.Max(armorVsPLegs, armorVsP);
									armorVsPFeet = Math.Max(armorVsPFeet, armorVsP);
									mindDefVsPLegs = Math.Max(mindDefVsPLegs, mindDefVsP);
									mindDefVsPFeet = Math.Max(mindDefVsPFeet, mindDefVsP);
									armorVsMLegs = Math.Max(armorVsMLegs, armorVsM);
									armorVsMFeet = Math.Max(armorVsMFeet, armorVsM);
									mindDefVsMLegs = Math.Max(mindDefVsMLegs, mindDefVsM);
									mindDefVsMFeet = Math.Max(mindDefVsMFeet, mindDefVsM); 
									break;
								case Layers.layer_hand2: //shield
									int parrying = SkillDef.SkillValueOfChar(ch, SkillName.Parry);
									armorVsPTotal = (armorVsP * parrying) / 1000;
									armorVsMTotal = (armorVsP * parrying) / 1000;
									//no mindDef with shield
									break;
							}
						}
					}
				}

				armorVsPTotal += 
					(armorVsPHead * 0.1) +
					(armorVsPNeck * 0.05) +
					(armorVsPBack * 0.1) +
					(armorVsPChest * 0.3) +
					(armorVsPArms * 0.1) +
					(armorVsPHands * 0.1) +
					(armorVsPLegs * 0.2) +
					(armorVsPFeet * 0.05);
				armorClassVsP += (int) armorVsPTotal;

				mindDefenseVsP += (int) (
					(mindDefVsPHead * 0.1) +
					(mindDefVsPNeck * 0.05) +
					(mindDefVsPBack * 0.1) +
					(mindDefVsPChest * 0.3) +
					(mindDefVsPArms * 0.1) +
					(mindDefVsPHands * 0.1) +
					(mindDefVsPLegs * 0.2) +
					(mindDefVsPFeet * 0.05));

				armorVsMTotal += 
					(armorVsMHead * 0.1) +
					(armorVsMNeck * 0.05) +
					(armorVsMBack * 0.1) +
					(armorVsMChest * 0.3) +
					(armorVsMArms * 0.1) +
					(armorVsMHands * 0.1) +
					(armorVsMLegs * 0.2) +
					(armorVsMFeet * 0.05);
				armorClassVsM += (int) armorVsMTotal;

				mindDefenseVsM += (int) (
					(mindDefVsMHead * 0.1) +
					(mindDefVsMNeck * 0.05) +
					(mindDefVsMBack * 0.1) +
					(mindDefVsMChest * 0.3) +
					(mindDefVsMArms * 0.1) +
					(mindDefVsMHands * 0.1) +
					(mindDefVsMLegs * 0.2) +
					(mindDefVsMFeet * 0.05));
			}
		}

		public static DamageType GetWeaponDamageType(WeaponType weapType) {
			switch (weapType) {
				case WeaponType.ArcheryStand:
				case WeaponType.ArcheryRunning:
					return DamageType.Archery;
				case WeaponType.OneHandBlade:
				case WeaponType.TwoHandBlade:
					return DamageType.Sharp;
				case WeaponType.BareHands:
				case WeaponType.OneHandBlunt:
				case WeaponType.TwoHandBlunt:
					return DamageType.Blunt;
				case WeaponType.OneHandSpike:
				case WeaponType.TwoHandSpike:
					return DamageType.Stabbing;
				case WeaponType.OneHandAxe:
				case WeaponType.TwoHandAxe:
					return DamageType.Slashing;
			}
			throw new ArgumentOutOfRangeException("weapType");
		}

		internal class CombatValues {
			internal int armorVsP;
			internal int mindDefenseVsP;
			internal int armorVsM;
			internal int mindDefenseVsM;
			internal Weapon weapon;
			internal WeaponType weaponType;
			internal DamageType damageType;
			internal int range;
			internal int strikeStartRange;
			internal int strikeStopRange;
			internal double delay;
			internal double attack;
			internal double piercing;
		}

		internal static CombatValues CalculateCombatValues(Character self) {
			CombatValues retVal = new CombatValues();

			CombatCalculator.CalculateWornArmor(self, out retVal.armorVsP, out retVal.mindDefenseVsP,
				out retVal.armorVsM, out retVal.mindDefenseVsM);

			int acModifier = self.ArmorClassModifier;
			int mdModifier = self.MindDefenseModifier;

			retVal.armorVsP += acModifier;
			retVal.armorVsM += acModifier;
			retVal.mindDefenseVsP += mdModifier;
			retVal.mindDefenseVsM += mdModifier;

			Weapon weapon = self.FindLayer(1) as Weapon;
			if (retVal.weapon == null) {
				retVal.weapon = self.FindLayer(2) as Weapon;
			}
			retVal.weapon = weapon;

			if (self.IsPlayerForCombat) {
				double weapSpeed;
				double weapAttack;
				if (weapon != null) {
					retVal.weaponType = weapon.WeaponType;
					retVal.range = weapon.Range;
					retVal.strikeStartRange = weapon.StrikeStartRange;
					retVal.strikeStopRange = weapon.StrikeStopRange;
					retVal.piercing = weapon.Piercing;
					weapSpeed = weapon.Speed;
					weapAttack = weapon.Attack;
				} else {
					retVal.weaponType = WeaponType.BareHands;
					retVal.range = CombatSettings.instance.bareHandsRange;
					retVal.strikeStartRange = CombatSettings.instance.bareHandsStrikeStartRange;
					retVal.strikeStopRange = CombatSettings.instance.bareHandsStrikeStopRange;
					retVal.piercing = CombatSettings.instance.bareHandsPiercing;
					weapSpeed = CombatSettings.instance.bareHandsSpeed;
					weapAttack = CombatSettings.instance.bareHandsAttack;
				}
				double delay = Math.Sqrt((double) self.Dex);
				delay *=  weapSpeed;
				delay *=  CombatSettings.instance.weaponSpeedGlobal;
				retVal.delay = (0xfffff/1000)/delay;//dedictvi z morie. funguje to tak proc to menit :)

				double tacticsAttack = SkillDef.ById(SkillName.Tactics).GetEffectForChar(self);
				double anatomyAttack = SkillDef.ById(SkillName.Anatomy).GetEffectForChar(self);
				double armsloreAttack = SkillDef.ById(SkillName.ArmsLore).GetEffectForChar(self);
				double strAttack = (self.Str * CombatSettings.instance.attackStrModifier) / 100;
				retVal.attack = (weapAttack * (tacticsAttack + anatomyAttack + armsloreAttack + strAttack)) / 1000;

				

				//TODO: upravy pro sipy (archery)

			} else {
				NPCDef npcDef = self.DefForCombat as NPCDef;
				if (npcDef != null) {
					retVal.weaponType = npcDef.WeaponType;
					if (retVal.weaponType == WeaponType.Undefined) {
						if (weapon == null) {
							retVal.weaponType = WeaponType.BareHands;
						} else {
							retVal.weaponType = weapon.WeaponType;
						}
					}
					retVal.range = npcDef.WeaponRange;
					retVal.strikeStartRange = npcDef.StrikeStartRange;
					retVal.strikeStopRange = npcDef.StrikeStopRange;
					retVal.delay = npcDef.WeaponDelay;
					retVal.attack = npcDef.WeaponAttack;
					retVal.piercing = npcDef.WeaponPiercing;
				}
				//else ?!
			}

			retVal.damageType = GetWeaponDamageType(retVal.weaponType);

			return retVal;
		}
	}
}

