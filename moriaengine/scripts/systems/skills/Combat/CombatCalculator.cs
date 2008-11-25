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

		internal static CombatArmorValues CalculateCombatArmorValues(Character self) {
			int armorClassVsP, mindDefenseVsP, armorClassVsM, mindDefenseVsM;
			int resist = SkillDef.SkillValueOfChar(self, SkillName.MagicResist);
			double resistEffect = SkillDef.ById(SkillName.MagicResist).GetEffectForChar(self);
			mindDefenseVsP = (int) (
				(resist * resistEffect) / 1000);
			mindDefenseVsM = mindDefenseVsP;

			armorClassVsP = self.DefForCombat.ArmorVsP;
			armorClassVsM = self.DefForCombat.ArmorVsM;

			//we calculate worn armor only for players
			if (self.IsPlayerForCombat) {
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

				foreach (Item equipped in self.GetVisibleEquip()) {
					Wearable wearable = equipped as Wearable;
					if (wearable != null) {
						LayerNames layer = (LayerNames) wearable.Z;
						int armorVsP = wearable.ArmorVsP;
						int mindDefVsP = wearable.MindDefenseVsP;
						int armorVsM = wearable.ArmorVsM;
						int mindDefVsM = wearable.MindDefenseVsM;
						if ((armorVsP > 0) || (mindDefVsP > 0) || (armorVsM > 0) || (mindDefVsM > 0)) {
							switch (layer) {
								case LayerNames.Helmet:
									armorVsPHead = Math.Max(armorVsPHead, armorVsP);
									mindDefVsPHead = Math.Max(mindDefVsPHead, mindDefVsP);
									armorVsMHead = Math.Max(armorVsMHead, armorVsM);
									mindDefVsMHead = Math.Max(mindDefVsMHead, mindDefVsM); 
									break;
								case LayerNames.Collar:
									armorVsPNeck = Math.Max(armorVsPNeck, armorVsP);
									mindDefVsPNeck = Math.Max(mindDefVsPNeck, mindDefVsP);
									armorVsMNeck = Math.Max(armorVsMNeck, armorVsM);
									mindDefVsMNeck = Math.Max(mindDefVsMNeck, mindDefVsM); 
									break;
								case LayerNames.Shirt:
								case LayerNames.Chest:       // 13 = armor chest
								case LayerNames.Tunic:       // 17 = jester suit
									armorVsPBack = Math.Max(armorVsPBack, armorVsP);
									armorVsPChest = Math.Max(armorVsPChest, armorVsP);
									mindDefVsPBack = Math.Max(mindDefVsPBack, mindDefVsP);
									mindDefVsPChest = Math.Max(mindDefVsPChest, mindDefVsP);
									armorVsMBack = Math.Max(armorVsMBack, armorVsM);
									armorVsMChest = Math.Max(armorVsMChest, armorVsM);
									mindDefVsMBack = Math.Max(mindDefVsMBack, mindDefVsM);
									mindDefVsMChest = Math.Max(mindDefVsMChest, mindDefVsM); 
									break;
								case LayerNames.Arms:                // 19 = armor
									armorVsPArms = Math.Max(armorVsPArms, armorVsP);
									mindDefVsPArms = Math.Max(mindDefVsPArms, mindDefVsP);
									armorVsMArms = Math.Max(armorVsMArms, armorVsM);
									mindDefVsMArms = Math.Max(mindDefVsMArms, mindDefVsM); 
									break;
								case LayerNames.Pants:
								case LayerNames.Skirt:
								case LayerNames.Half_apron:
									armorVsPLegs = Math.Max(armorVsPLegs, armorVsP);
									mindDefVsPLegs = Math.Max(mindDefVsPLegs, mindDefVsM);
									armorVsMLegs = Math.Max(armorVsMLegs, armorVsM);
									mindDefVsMLegs = Math.Max(mindDefVsMLegs, mindDefVsM); 
									break;
								case LayerNames.Shoes:
									armorVsPFeet = Math.Max(armorVsPFeet, armorVsP);
									mindDefVsPFeet = Math.Max(mindDefVsPFeet, mindDefVsP);
									armorVsMFeet = Math.Max(armorVsMFeet, armorVsM);
									mindDefVsMFeet = Math.Max(mindDefVsMFeet, mindDefVsM); 
									break;
								case LayerNames.Gloves:      // 7
									armorVsPHands = Math.Max(armorVsPHands, armorVsP);
									mindDefVsPHands = Math.Max(mindDefVsPHands, mindDefVsP);
									armorVsMHands = Math.Max(armorVsMHands, armorVsM);
									mindDefVsMHands = Math.Max(mindDefVsMHands, mindDefVsM); 
									break;
								case LayerNames.Cape:                // 20 = cape
									armorVsPBack = Math.Max(armorVsPBack, armorVsP);
									armorVsPArms = Math.Max(armorVsPArms, armorVsP);
									mindDefVsPBack = Math.Max(mindDefVsPBack, mindDefVsP);
									mindDefVsPArms = Math.Max(mindDefVsPArms, mindDefVsP);
									armorVsMBack = Math.Max(armorVsMBack, armorVsM);
									armorVsMArms = Math.Max(armorVsMArms, armorVsM);
									mindDefVsMBack = Math.Max(mindDefVsMBack, mindDefVsM);
									mindDefVsMArms = Math.Max(mindDefVsMArms, mindDefVsM); 
									break;
								case LayerNames.Robe:                // 22 = robe over all.
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
								case LayerNames.Leggins:
									armorVsPLegs = Math.Max(armorVsPLegs, armorVsP);
									armorVsPFeet = Math.Max(armorVsPFeet, armorVsP);
									mindDefVsPLegs = Math.Max(mindDefVsPLegs, mindDefVsP);
									mindDefVsPFeet = Math.Max(mindDefVsPFeet, mindDefVsP);
									armorVsMLegs = Math.Max(armorVsMLegs, armorVsM);
									armorVsMFeet = Math.Max(armorVsMFeet, armorVsM);
									mindDefVsMLegs = Math.Max(mindDefVsMLegs, mindDefVsM);
									mindDefVsMFeet = Math.Max(mindDefVsMFeet, mindDefVsM); 
									break;
								case LayerNames.Hand2: //shield
									int parrying = SkillDef.SkillValueOfChar(self, SkillName.Parry);
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
			CombatArmorValues retVal = new CombatArmorValues();
			retVal.armorVsP = armorClassVsP;
			retVal.armorVsM += armorClassVsM;
			retVal.mindDefenseVsP += mindDefenseVsP;
			retVal.mindDefenseVsM += mindDefenseVsM;

			int acModifier = self.ArmorClassModifier;
			int mdModifier = self.MindDefenseModifier;

			retVal.armorVsP += acModifier;
			retVal.armorVsM += acModifier;
			retVal.mindDefenseVsP += mdModifier;
			retVal.mindDefenseVsM += mdModifier;

			return retVal;
		}

		public static DamageType GetWeaponDamageType(WeaponType weapType) {
			switch (weapType) {
				case WeaponType.Bow:
				case WeaponType.XBow:
					return DamageType.Archery;
				case WeaponType.OneHandSword:
				case WeaponType.TwoHandSword:
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

		internal class CombatArmorValues {
			internal int armorVsP;
			internal int mindDefenseVsP;
			internal int armorVsM;
			internal int mindDefenseVsM;
		}

		internal class CombatWeaponValues {
			internal Weapon weapon;
			internal WeaponType weaponType;
			internal DamageType damageType;
			internal int range;
			internal int strikeStartRange;
			internal int strikeStopRange;
			internal TimeSpan delay;
			internal double attackVsP;
			internal double attackVsM;
			internal double piercing;
			internal WeaponAnimType weaponAnimType;
			internal int projectileAnim = -1;
			internal ProjectileType projectileType;
		}

		private static Projectile TryFindProjectile(Character self, ProjectileType type) {
			if (type != ProjectileType.None) {
				Projectile retVal = self.weaponProjectile;
				if ((retVal != null) &&
						(retVal.IsDeleted || 
						(retVal.TopObj() != self) || 
						(retVal.Amount < 1))) {
					retVal = null;
				}
				if ((retVal == null) || (!CheckProjectile(retVal, type))) {
					foreach (Item i in self.BackpackAsContainer.EnumShallow()) {
						retVal = i as Projectile;
						if ((retVal != null) && (CheckProjectile(retVal, type))) {
							return retVal;
						}
					}
				} else {
					return retVal;
				}
			}
			return null;
		}

		private static bool CheckProjectile(Projectile projectile, ProjectileType type) {
			ProjectileType actualType = projectile.ProjectileType;
			return ((type & actualType) == actualType);
		}

		internal static CombatWeaponValues CalculateCombatWeaponValues(Character self) {
			CombatWeaponValues retVal = new CombatWeaponValues();

			Weapon weapon = self.FindLayer(1) as Weapon;
			if (weapon == null) {
				weapon = self.FindLayer(2) as Weapon;
			}
			retVal.weapon = weapon;

			if (self.IsPlayerForCombat) {
				double weapSpeed;
				double weapAttackVsP;
				double weapAttackVsM;
				if (weapon != null) {
					retVal.weaponType = weapon.WeaponType;
					retVal.weaponAnimType = weapon.WeaponAnimType;
					retVal.range = weapon.Range;
					retVal.strikeStartRange = weapon.StrikeStartRange;
					retVal.strikeStopRange = weapon.StrikeStopRange;
					retVal.piercing = weapon.Piercing;
					retVal.projectileType = weapon.ProjectileType;
					self.weaponProjectile = TryFindProjectile(self, weapon.ProjectileType);
					if (self.weaponProjectile != null) {
						retVal.piercing += self.weaponProjectile.Piercing;
						retVal.projectileAnim = weapon.ProjectileAnim;
					}

					weapSpeed = weapon.Speed;
					weapAttackVsP = weapon.AttackVsP;
					weapAttackVsM = weapon.AttackVsM;
				} else {
					retVal.weaponType = WeaponType.BareHands;
					retVal.weaponAnimType = WeaponAnimType.BareHands;
					retVal.range = CombatSettings.instance.bareHandsRange;
					retVal.strikeStartRange = CombatSettings.instance.bareHandsStrikeStartRange;
					retVal.strikeStopRange = CombatSettings.instance.bareHandsStrikeStopRange;
					retVal.piercing = CombatSettings.instance.bareHandsPiercing;
					self.weaponProjectile = null;
					weapSpeed = CombatSettings.instance.bareHandsSpeed;
					weapAttackVsP = CombatSettings.instance.bareHandsAttackVsP;
					weapAttackVsM = CombatSettings.instance.bareHandsAttackVsM;
				}
				double delay = Math.Sqrt((double) self.Dex);
				delay *=  weapSpeed;
				delay *=  CombatSettings.instance.weaponSpeedGlobal;
				retVal.delay = TimeSpan.FromSeconds((0xfffff / 1000) / delay);//dedictvi z morie. funguje to tak proc to menit :)

				double tacticsAttack = SkillDef.ById(SkillName.Tactics).GetEffectForChar(self);
				double anatomyAttack = SkillDef.ById(SkillName.Anatomy).GetEffectForChar(self);
				double armsloreAttack = SkillDef.ById(SkillName.ArmsLore).GetEffectForChar(self);
				double strAttack = self.Str * CombatSettings.instance.attackStrModifier;
				double sum = (tacticsAttack + anatomyAttack + armsloreAttack + strAttack) / 1000;
				retVal.attackVsP = weapAttackVsP * sum;
				retVal.attackVsM = weapAttackVsM * sum;
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
					if (weapon != null) {
						retVal.weaponAnimType = weapon.WeaponAnimType;
						retVal.projectileAnim = weapon.ProjectileAnim;
					} else {
						retVal.weaponAnimType = WeaponAnimType.BareHands;
						retVal.projectileAnim = npcDef.ProjectileAnim;
					}
					retVal.range = npcDef.WeaponRange;
					retVal.strikeStartRange = npcDef.StrikeStartRange;
					retVal.strikeStopRange = npcDef.StrikeStopRange;
					retVal.delay = TimeSpan.FromSeconds(npcDef.WeaponDelay);
					retVal.attackVsM = npcDef.WeaponAttack;
					retVal.attackVsP = retVal.attackVsM;
					retVal.piercing = npcDef.WeaponPiercing;
					self.weaponProjectile = null;
				} else {
					//else ?!
					Logger.WriteError("Can't calculate combat values for '"+self+"'. It says it's a NPC but has no NPCDef.");
				}
			}

			retVal.damageType = GetWeaponDamageType(retVal.weaponType);

			return retVal;
		}
	}
}

