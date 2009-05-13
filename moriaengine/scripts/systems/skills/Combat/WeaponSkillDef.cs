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
		//param1 = WeaponSkillPhase
		//param2 = duein TimeSpan

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

		protected override bool On_Select(SkillSequenceArgs skillSeqArgs) {
			if (skillSeqArgs.Target1 == null || skillSeqArgs.Target1 == skillSeqArgs.Self) {
				return true;
			}
			return false;
		}

		protected override bool On_Start(SkillSequenceArgs skillSeqArgs) {
			Character self = (Character) skillSeqArgs.Self;
			Character target = (Character) skillSeqArgs.Target1;
			self.AbortSkill(); //abort previous skill

			WeaponSkillTargetTrackerPlugin.InstallTargetTracker(target, self);
			skillSeqArgs.Param1 = WeaponSkillPhase.Drawing;
			skillSeqArgs.DelayInSeconds = 0;
			skillSeqArgs.DelayStroke();

			return true;//cancel because we're not using the skill's delay just yet
		}

		static TimerKey animTk = TimerKey.Get("_weaponAnimDelay_");

		protected override bool On_Stroke(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			Character target = (Character) skillSeqArgs.Target1;
			if (!self.CanInteractWith(target)) {
				WeaponSkillTargetQueuePlugin.RemoveTarget(self, target);
				self.AbortSkill();
				return true;
			}
			int distance = Point2D.GetSimpleDistance(self, target);
			WeaponSkillPhase phase = (WeaponSkillPhase) Convert.ToInt64(skillSeqArgs.Param1);
			if (phase == WeaponSkillPhase.Drawing) {
				if (distance <= self.WeaponStrikeStartRange) {
					skillSeqArgs.Param1 = WeaponSkillPhase.Striking;
					TimeSpan delay = self.WeaponDelay;
					skillSeqArgs.Param2 = Globals.TimeAsSpan + delay;
					skillSeqArgs.DelaySpan = delay;
					skillSeqArgs.DelayStroke();
					self.AddTimer(animTk, new WeaponAnimTimer()).DueInSeconds = delay.TotalSeconds / 2;
					return true;
				}
			} else {
				if (distance > self.WeaponStrikeStopRange) {
					self.AbortSkill();
				} else if ((((TimeSpan) skillSeqArgs.Param2) <= Globals.TimeAsSpan) &&
						(distance <= self.WeaponRange)) {

					Projectile projectile = self.WeaponProjectile;
					if (projectile != null) {
						switch (Globals.dice.Next(3)) {
							case 0://arrow appears on ground
								Projectile onGround = (Projectile) projectile.Dupe();
								onGround.P(target);
								onGround.Amount = 1;
								break;
							case 1://arrow appears in targets backpack
								Projectile inPack = (Projectile) projectile.Dupe();
								inPack.Cont = target.Backpack;
								inPack.Amount = 1;
								break;
							//else arrow disappears
						}
						int amount = projectile.Amount;
						if (amount < 2) {
							projectile.Delete();
						} else {
							projectile.Amount = amount - 1;
						}
					} else if (self.WeaponProjectileType != ProjectileType.None) {
						self.SysMessage(CompiledLoc<WeaponSkillDefLoc>.Get(self.Language).YouHaveNoAmmo);
						self.AbortSkill();
						return true;
					}

					if (!self.Flag_Moving) {
						self.Direction = Point2D.GetDirFromTo(self, target);
					}

					int projectileAnim = self.WeaponProjectileAnim;
					if (projectileAnim >= 0) {
						EffectFactory.EffectFromTo(self, target, projectileAnim, 10, 1, false, false, 0, 0);
					}

					skillSeqArgs.Success = this.CheckSuccess(self, Globals.dice.Next(700));

					if (self.CanInteractWith(target)) {
						WeaponSkillTargetQueuePlugin.AddTarget(self, target);//we're not really adding the target, just restarting the attack, most probably
					} else {
						WeaponSkillTargetQueuePlugin.FightCurrentTarget(self);
					}

					return false;
				}
			}

			skillSeqArgs.DelaySpan = TimeSpan.FromSeconds(1);
			skillSeqArgs.DelayStroke();//we keep stroking, this needs to be the current skill...
			return true;
		}

		[Dialogs.ViewableClass]
		[Persistence.SaveableClass]
		[DeepCopyableClass]
		public class WeaponAnimTimer : BoundTimer {
			[Persistence.LoadingInitializer]
			[DeepCopyImplementation]
			public WeaponAnimTimer() {
			}

			protected override void OnTimeout(TagHolder cont) {
				AnimCalculator.PerformAttackAnim((Character) cont);
			}
		}

		protected override bool On_Success(SkillSequenceArgs skillSeqArgs) {
			Character self = (Character) skillSeqArgs.Self;
			Character target = (Character) skillSeqArgs.Target1;

			DamageManager.ProcessSwing(self, target);
			return false;
		}

		protected override bool On_Fail(SkillSequenceArgs skillSeqArgs) {
			Character self = (Character) skillSeqArgs.Self;
			Character target = (Character) skillSeqArgs.Target1;

			target.Trigger_HostileAction(self);
			SoundCalculator.PlayMissSound(self);
			return false;
		}

		protected override void On_Abort(SkillSequenceArgs skillSeqArgs) {
			Character target = (Character) skillSeqArgs.Target1;
			if (target != null) {
				Character self = (Character) skillSeqArgs.Self;
				//WeaponSkillTargetQueuePlugin.RemoveTarget(self, target);
				WeaponSkillTargetTrackerPlugin.UnInstallTargetTracker(target, self);
			}
		}
	}

	public class WeaponSkillDefLoc : AbstractLoc {
		internal string YouHaveNoAmmo = "Nemáš støelivo.";
	}
}