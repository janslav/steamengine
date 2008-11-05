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
using SteamEngine;
using SteamEngine.Common;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {
	[Dialogs.ViewableClass]
	public class WeaponSkillDef : SkillDef {
		public enum WeaponSkillPhase {
			Drawing,//first phase, the target must get in under startsrike range
			Striking//second phase, can strike after delay if target close <= range
		}

		public class WeaponSkillParam {
			public WeaponSkillPhase phase;
			public double dueAt;

			public WeaponSkillParam(WeaponSkillPhase phase, double dueAt) {
				this.phase = phase;
				this.dueAt = dueAt;
			}
		}

		public WeaponSkillDef(string defname, string filename, int headerLine)
				: base(defname, filename, headerLine) {
		}

		protected override void On_Select(Character self) {
			//self.SysMessage(this.Key+" selected");
			Character target = self.currentSkillTarget1 as Character;
			if (target != null) {
				self.StartSkill(this);
			}
		}

		protected override void On_Start(Character self) {
			Character target = self.currentSkillTarget1 as Character;
			if (target != null) {
				WeaponSkillTargetTrackerPlugin.InstallTargetTracker(target, self);
				self.currentSkillParam1 = new WeaponSkillParam(
					WeaponSkillPhase.Drawing,
					0);
					self.DelayedSkillStroke();
			} else {
				self.AbortSkill();
			}
		}

		static TimerKey animTk = TimerKey.Get("_weaponAnimDelay_");

		protected override void On_Stroke(Character self) {
			//self.SysMessage(this.Key+" stroking");
			WeaponSkillParam param = (WeaponSkillParam) self.currentSkillParam1;
			Character target = self.currentSkillTarget1 as Character;
			if (target.IsDeleted || target.Flag_Dead || target.Flag_Insubst) {
				WeaponSkillTargetQueuePlugin.RemoveTarget(self, target);
				return;
			}
			int distance = Point2D.GetSimpleDistance(self, target);
			if (param.phase == WeaponSkillPhase.Drawing) {
				if (distance <= self.WeaponStrikeStartRange) {
					double delay = self.WeaponDelay;
					self.DelaySkillStroke(delay);
					param.dueAt = Globals.TimeInSeconds + delay;
					param.phase = WeaponSkillPhase.Striking;
					self.AddTimer(animTk, new WeaponAnimTimer()).DueInSeconds = delay / 2;
				}
			} else {
				if (distance > self.WeaponStrikeStopRange) {
					self.AbortSkill();
				} else if ((param.dueAt <= Globals.TimeInSeconds) &&
						(distance <= self.WeaponRange)) {

					Projectile projectile = self.WeaponProjectile;
					if (projectile != null) {
						switch (Globals.dice.Next(3)) {
							case 0://arrow appears on ground
								Item onGround = (Item) projectile.Dupe();
								onGround.P(target);
								onGround.Amount = 1;
								break;
							case 1://arrow appears in targets backpack
								Item inPack = (Item) projectile.Dupe();
								inPack.Cont = target.BackpackAsContainer;
								inPack.Amount = 1;
								break;
							//else arrow disappears
						}
						uint amount = projectile.Amount;
						if (amount < 2) {
							projectile.Delete();
						} else {
							projectile.Amount = amount - 1;
						}
					} else if (self.WeaponProjectileType != ProjectileType.None) {
						self.AbortSkill();
						self.SysMessage("Nemáš støelivo.");
						return;
					}

					if (!self.Flag_Moving) {
						self.Direction = Point2D.GetDirFromTo(self, target);
					}

					int projectileAnim = self.WeaponProjectileAnim;
					if (projectileAnim >= 0) {
						EffectFactory.EffectFromTo(self, target, (ushort) projectileAnim, 10, 1, 0, 0, 0, 0);
					}

					if (CheckSuccess(self, Globals.dice.Next(700))) {
						this.Success(self);
					} else {
						this.Fail(self);
					}

					self.currentSkill = null;
					if (!target.IsDeleted && !(target.Flag_Dead || target.Flag_Insubst)) {
						WeaponSkillTargetQueuePlugin.AddTarget(self, target);//we're not really adding the target, just restarting the attack, most probably
					} else {
						WeaponSkillTargetQueuePlugin.FightCurrentTarget(self);
					}
				}
			}
		}

		[Dialogs.ViewableClass][Persistence.SaveableClass][DeepCopyableClass]
		public class WeaponAnimTimer : BoundTimer {
			[Persistence.LoadingInitializer][DeepCopyImplementation]
			public WeaponAnimTimer() {
			}

			protected override void OnTimeout(TagHolder cont) {
				AnimCalculator.PerformAttackAnim((Character) cont);
			}
		}

		protected override void On_Success(Character self) {
			//self.SysMessage(this.Key+" succeeded");
			Character target = (Character) self.currentSkillTarget1;
			DamageManager.ProcessSwing(self, target);
		}

		protected override void On_Fail(Character self) {
			//self.SysMessage(this.Key+" failed");
			Character target = (Character) self.currentSkillTarget1;
			target.Trigger_HostileAction(self);
			SoundCalculator.PlayMissSound(self);
		}

		protected override void On_Abort(Character self) {
			Character target = self.currentSkillTarget1 as Character;
			if (target != null) {
				//WeaponSkillTargetQueuePlugin.RemoveTarget(self, target);
				WeaponSkillTargetTrackerPlugin.UnInstallTargetTracker(target, self);
			}
		}
	}
}