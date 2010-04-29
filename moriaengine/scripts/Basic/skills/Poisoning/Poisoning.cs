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
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using SteamEngine;
using SteamEngine.Common;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	public interface IPoisonableItem {
		int PoisoningDifficulty { get; }
		double PoisoningEfficiency { get; }
	}

	//tool = poison potion
	//target1 = item being poisoned (weapon, projectile, etc.)

	[Dialogs.ViewableClass]
	public class PoisoningSkillDef : SkillDef {

		public PoisoningSkillDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		protected override bool On_Select(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			PoisonPotion potion = skillSeqArgs.Tool as PoisonPotion;
			if (potion != null) {
				if (!self.CanReachWithMessage(potion)) {
					potion = null;
				}
			}
			if (potion == null) {
				Player selfAsPlayer = self as Player;
				if (selfAsPlayer != null) {
					selfAsPlayer.Target(SingletonScript<Targ_Poisoning_Potion>.Instance, skillSeqArgs);
				} else {
					throw new SEException("Poison potion not set for nonplayer while poisoning");
				}
				return true;
			}

			Item target = skillSeqArgs.Target1 as Item;
			if (target != null) {
				if (!CanPoisonWithMessage(self, potion, target)) {
					target = null;
				}
			}
			if (target == null) {
				Player selfAsPlayer = self as Player;
				if (selfAsPlayer != null) {
					selfAsPlayer.Target(SingletonScript<Targ_Poisoning_Target>.Instance, skillSeqArgs);
				} else {
					throw new SEException("Target not set for nonplayer while poisoning");
				}
				return true;
			}

			return false; //continue
		}

		protected override bool On_Start(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			self.Sound(0x4F);

			PoisonPotion potion = (PoisonPotion) skillSeqArgs.Tool;
			skillSeqArgs.Param1 = PoisonedItemPlugin.Acquire(potion); //we need to remember the parameters

			IPoisonableItem targetItem = (IPoisonableItem) skillSeqArgs.Target1;
			int difficulty = targetItem.PoisoningDifficulty;
			skillSeqArgs.Success = this.CheckSuccess(self, difficulty);

			if ((targetItem is Projectile) || (Globals.dice.NextDouble() <= 0.1)) { //1 potion can poison ~10 weapons or <=50 arrows
				potion.CreateEmptyFlask(self.Backpack);
				potion.Consume(1);
			}

			return false;
		}

		protected override bool On_Stroke(SkillSequenceArgs skillSeqArgs) {
			//Character self = skillSeqArgs.Self;
			return false;
		}

		protected override bool On_Success(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;

			PoisonPotion potion = (PoisonPotion) skillSeqArgs.Tool;
			Item target = (Item) skillSeqArgs.Target1;
			if (!CanPoisonWithMessage(self, potion, target)) {
				return true;
			}



			return false;
		}

		protected override bool On_Fail(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			return false;
		}

		protected override void On_Abort(SkillSequenceArgs skillSeqArgs) {
		}

		internal static bool CanPoisonWithMessage(Character self, PoisonPotion potion, Item target) {
			if (!self.CanReachWithMessage(target)) {
				return false;
			} else {
				Weapon asWeapon = target as Weapon;
				if (asWeapon != null) {
					if (asWeapon.PoisoningDifficulty < 1) {
						self.SysMessage(Loc<PoisoningLoc>.Get(self.Language).CantPoisonThisWeapon);
						return false;
					} //check for existing poison?
					return true;
				} else {
					Projectile asProjectile = target as Projectile;
					if (asProjectile != null) {
						if (asProjectile.PoisoningDifficulty < 1) {
							self.SysMessage(Loc<PoisoningLoc>.Get(self.Language).CantPoisonThisProjectile);
							return false;
						} else {
							PoisonedItemPlugin poison = PoisonedItemPlugin.GetPoisonPlugin(asProjectile);
							if (poison != null) {
								if (poison.PoisonType != potion.PoisonType) {
									self.SysMessage(Loc<PoisoningLoc>.Get(self.Language).ProjectilesHaveDifferentPoisond);
									return false;
								} else if (poison.PoisonDoses >= asProjectile.Amount) {
									self.SysMessage(Loc<PoisoningLoc>.Get(self.Language).AllProjectilesPoisoned);
								}
							}
						}
						return true;
					}
				}
				self.SysMessage(Loc<PoisoningLoc>.Get(self.Language).CantPoisonThat);
				return false;
			}
		}
	}


	public class Targ_Poisoning_Potion : CompiledTargetDef {

		protected override void On_Start(Player self, object parameter) {
			self.ClilocSysMessage(502137); //Select the poison you wish to use
			base.On_Start(self, parameter);
		}

		protected override bool On_TargonItem(Player self, Item targetted, object parameter) {
			if (!self.CanReachWithMessage(targetted)) {
				return true; //re-raise target
			} else if (targetted is PoisonPotion) {
				SkillSequenceArgs skillSeq = (SkillSequenceArgs) parameter;
				skillSeq.Tool = targetted;
				skillSeq.PhaseStart();
				return false;
			}
			return true; //re-raise target
		}
	}

	public class Targ_Poisoning_Target : CompiledTargetDef {

		protected override void On_Start(Player self, object parameter) {
			self.ClilocSysMessage(502142); //To what do you wish to apply the poison?
			base.On_Start(self, parameter);
		}

		protected override bool On_TargonItem(Player self, Item targetted, object parameter) {
			SkillSequenceArgs skillSeq = (SkillSequenceArgs) parameter;

			if (!self.CanReachWithMessage(targetted)) {
				return true;
			} else if (PoisoningSkillDef.CanPoisonWithMessage(self, (PoisonPotion) skillSeq.Tool, targetted)) {				
				skillSeq.Tool = targetted;
				skillSeq.PhaseStart();
				return false;
			}
			return true; //re-raise target
		}
	}

	public class PoisoningLoc : CompiledLocStringCollection {
		public string CantPoisonThisWeapon = "It's impossible to poison this weapon";
		public string CantPoisonThisProjectile = "It's impossible to poison this weapon projectile";
		public string CantPoisonThat = "It's impossible to poison that. You can only poison infectious weapons or weapon projectiles.";
		public string ProjectilesHaveDifferentPoisond = "These projectiles are already poisoned with a different poison.";
		public string AllProjectilesPoisoned = "All of the projectiles are already poisoned.";
	}

}

//502137	Select the poison you wish to use
//502139	That is not a poison potion.
//502142	To what do you wish to apply the poison?
//1060204	You cannot poison that! You can only poison infectious weapons, food or drink.
//502145	You cannot poison that! You can only poison bladed or piercing weapons, food or drink.
//1010517	You apply the poison
//502148	You make a grave mistake while applying the poison.
//1010516	You fail to apply a sufficient dose of poison on the blade
//1010518	You fail to apply a sufficient dose of poison