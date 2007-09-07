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

	public static class DamageImpl {
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

		public static void CalculateWornArmor(Character ch, out int armorClass, out int mindDefense) {
			int resist = SkillDef.SkillValueOfChar(ch, SkillName.MagicResist);
			double resistEffect = SkillDef.ById(SkillName.MagicResist).GetEffectForChar(ch);
			mindDefense = (int) (
				(resist * resistEffect) / 1000);

			armorClass = ch.DefForCombat.Armor;

			//we calculate worn armor only for players
			if (ch.IsPlayerForCombat) {
				double armorHead = 0;
				double armorNeck = 0;
				double armorBack = 0;
				double armorChest = 0;
				double armorArms = 0;
				double armorHands = 0;
				double armorLegs = 0;
				double armorFeet = 0;

				double mindDefHead = 0;
				double mindDefNeck = 0;
				double mindDefBack = 0;
				double mindDefChest = 0;
				double mindDefArms = 0;
				double mindDefHands = 0;
				double mindDefLegs = 0;
				double mindDefFeet = 0;

				double armorTotal = 0;

				foreach (Item equipped in ch.GetVisibleEquip()) {
					Wearable wearable = equipped as Wearable;
					if (wearable != null) {
						Layers layer = (Layers) wearable.Z;
						int armor = wearable.Armor;
						int mindDef = wearable.MindDefense;
						if ((armor > 0) || (mindDef > 0)) {
							switch (layer) {
								case Layers.layer_helm:
									armorHead = Math.Max(armorHead, armor);
									mindDefHead = Math.Max(mindDefHead, mindDef); break;
								case Layers.layer_collar:
									armorNeck = Math.Max(armorNeck, armor);
									mindDefNeck = Math.Max(mindDefNeck, mindDef); break;
								case Layers.layer_shirt:
								case Layers.layer_chest:       // 13 = armor chest
								case Layers.layer_tunic:       // 17 = jester suit
									armorBack = Math.Max(armorBack, armor);
									armorChest = Math.Max(armorChest, armor);
									mindDefBack = Math.Max(mindDefBack, mindDef);
									mindDefChest = Math.Max(mindDefChest, mindDef); break;
								case Layers.layer_arms:                // 19 = armor
									armorArms = Math.Max(armorArms, armor);
									mindDefArms = Math.Max(mindDefArms, mindDef); break;
								case Layers.layer_pants:
								case Layers.layer_skirt:
								case Layers.layer_half_apron:
									armorLegs = Math.Max(armorLegs, armor);
									mindDefLegs = Math.Max(mindDefLegs, mindDef); break;
								case Layers.layer_shoes:
									armorFeet = Math.Max(armorFeet, armor);
									mindDefFeet = Math.Max(mindDefFeet, mindDef); break;
								case Layers.layer_gloves:      // 7
									armorHands = Math.Max(armorHands, armor);
									mindDefHands = Math.Max(mindDefHands, mindDef); break;
								case Layers.layer_cape:                // 20 = cape
									armorBack = Math.Max(armorBack, armor);
									armorArms = Math.Max(armorArms, armor);
									mindDefBack = Math.Max(mindDefBack, mindDef);
									mindDefArms = Math.Max(mindDefArms, mindDef); break;
								case Layers.layer_robe:                // 22 = robe over all.
									armorBack = Math.Max(armorBack, armor);
									armorChest = Math.Max(armorChest, armor);
									armorArms = Math.Max(armorArms, armor);
									armorLegs = Math.Max(armorLegs, armor); ;
									mindDefBack = Math.Max(mindDefBack, mindDef);
									mindDefChest = Math.Max(mindDefChest, mindDef);
									mindDefArms = Math.Max(mindDefArms, mindDef);
									mindDefLegs = Math.Max(mindDefLegs, mindDef); break;
								case Layers.layer_legs:
									armorLegs = Math.Max(armorLegs, armor);
									armorFeet = Math.Max(armorFeet, armor);
									mindDefLegs = Math.Max(mindDefLegs, mindDef);
									mindDefFeet = Math.Max(mindDefFeet, mindDef); break;
								case Layers.layer_hand2: //shield
									int parrying = SkillDef.SkillValueOfChar(ch, SkillName.Parry);
									armorTotal = (armor * parrying) / 1000;
									//no mindDef with shield
									break;
							}
						}
					}
				}

				armorTotal += 
					(armorHead * 0.1) +
					(armorNeck * 0.05) +
					(armorBack * 0.1) +
					(armorChest * 0.3) +
					(armorArms * 0.1) +
					(armorHands * 0.1) +
					(armorLegs * 0.2) +
					(armorFeet * 0.05);
				armorClass += (int) armorTotal;

				mindDefense += (int) (
					(mindDefHead * 0.1) +
					(mindDefNeck * 0.05) +
					(mindDefBack * 0.1) +
					(mindDefChest * 0.3) +
					(mindDefArms * 0.1) +
					(mindDefHands * 0.1) +
					(mindDefLegs * 0.2) +
					(mindDefFeet * 0.05));
			}
		}

		public static DamageType GetWeaponDamageType(WeaponType weapType) {
			switch (weapType) {
				case WeaponType.Archery:
					return DamageType.Archery;
				case WeaponType.Axe:
					return DamageType.Slashing;
				case WeaponType.Blunt:
					return DamageType.Blunt;
				case WeaponType.Stabbing:
					return DamageType.Stabbing;
				case WeaponType.Sword:
					return DamageType.Sharp;
			}
			throw new ArgumentOutOfRangeException("weapType");
		}
	}

	partial class Character {
		CombatValues combatValues;

		private class CombatValues {
			internal short armor;
			internal short mindDefense;
			internal Weapon weapon;
			internal DamageType damageType;

			internal CombatValues(short armor, short mindDefense, Weapon weapon, DamageType damageType) {
				this.armor = armor;
				this.mindDefense = mindDefense;
				this.weapon = weapon;
				this.damageType = damageType;
			}
		}

		public override short ArmorClass {
			get {
				CalculateCombatValues();
				return combatValues.armor;
			}
		}

		private static TagKey armorClassModifierTK = TagKey.Get("_armorClassModifier_");
		public int ArmorClassModifier {
			get {
				return Convert.ToInt32(GetTag(armorClassModifierTK));
			}
			set {
				InvalidateCombatValues();
				if (value != 0) {
					SetTag(armorClassModifierTK, value);
				} else {
					RemoveTag(armorClassModifierTK);
				}
			}
		}

		public override short MindDefense {
			get {
				CalculateCombatValues();
				return combatValues.mindDefense;
			}
		}

		private static TagKey mindDefenseModifierTK = TagKey.Get("_mindDefenseModifier_");
		public int MindDefenseModifier {
			get {
				return Convert.ToInt32(GetTag(mindDefenseModifierTK));
			}
			set {
				InvalidateCombatValues();
				if (value != 0) {
					SetTag(mindDefenseModifierTK, value);
				} else {
					RemoveTag(mindDefenseModifierTK);
				}
			}
		}

		public void InvalidateCombatValues() {
			if (combatValues != null) {
				Packets.NetState.AboutToChangeStats(this);
				combatValues = null;
			}
		}

		public void CalculateCombatValues() {
			if (combatValues == null) {
				int calculatedArmor, calculatedMindDef;
				DamageImpl.CalculateWornArmor(this, out calculatedArmor, out calculatedMindDef);

				Weapon weapon = this.FindLayer(1) as Weapon;
				if (weapon == null) {
					weapon = this.FindLayer(2) as Weapon;
				}
				WeaponType weaponType = WeaponType.Blunt;
				if (weapon != null) {
					weaponType = weapon.WeaponType;
				}

				combatValues = new CombatValues(
					(short) (calculatedArmor + this.ArmorClassModifier),
					(short) (calculatedMindDef + this.MindDefenseModifier),
					weapon,
					DamageImpl.GetWeaponDamageType(weaponType)
					);
			}
		}

		public override bool On_ItemEquip(AbstractCharacter droppingChar, AbstractItem i, bool forced) {
			if (i is Wearable || i is Weapon) {
				InvalidateCombatValues();
			}
			return base.On_ItemEquip(droppingChar, i, forced);
		}

		public override bool On_ItemUnEquip(AbstractCharacter pickingChar, AbstractItem i, bool forced) {
			if (i is Wearable || i is Weapon) {
				InvalidateCombatValues();
			}
			return base.On_ItemUnEquip(pickingChar, i, forced);
		}

		public bool IsPlayerForCombat {
			get {
				//TODO: false for hypnomystic
				return IsPlayer;
			}
		}

		public CharacterDef DefForCombat {
			get {
				//TODO: monster def for hypnomystic
				return this.Def;
			}
		}
	}
}

