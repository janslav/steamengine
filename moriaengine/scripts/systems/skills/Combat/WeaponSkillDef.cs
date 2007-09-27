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

		public override void Select(AbstractCharacter ch) {
			Character self = (Character) ch;
			if (!Trigger_Select(self)) {
				//self.SysMessage(this.Key+" selected");
				Character target = self.currentSkillTarget1 as Character;
				if (target != null) {
					self.StartSkill(this);
				}
			}
		}

		internal override void Start(Character self) {
			if (!Trigger_Start(self)) {
				Character target = self.currentSkillTarget1 as Character;
				if (target != null) {
					WeaponSkillTargetTrackerPlugin.InstallTargetTracker(target, self);
					self.currentSkillParam = new WeaponSkillParam(
						WeaponSkillPhase.Drawing,
						0);

					self.DelayedSkillStroke();
				} else {
					self.AbortSkill();
				}
			}
		}
		
		public override void Stroke(Character self) {
			if (!Trigger_Stroke(self)) {
				//self.SysMessage(this.Key+" stroking");
				WeaponSkillParam param = (WeaponSkillParam) self.currentSkillParam;
				Character target = self.currentSkillTarget1 as Character;
				if (target.IsDeleted || target.Flag_Dead) {
					WeaponSkillTargetQueuePlugin.RemoveTarget(self, target);
					return;
				}
				int distance = Point2D.GetSimpleDistance(self, target);
				if (param.phase == WeaponSkillPhase.Drawing) {
					if (distance <= self.WeaponStrikeStartRange) {
						double delay = self.WeaponDelay;
						self.DelaySkillStroke(delay);
						param.dueAt = Globals.TimeInSeconds+delay;
						param.phase = WeaponSkillPhase.Striking;
						AnimCalculator.PerformAttackAnim(self);
					}
				} else {
					if (distance > self.WeaponStrikeStopRange) {
						self.AbortSkill();
					} else if ((param.dueAt <= Globals.TimeInSeconds) && 
							(distance <= self.WeaponRange)) {
						if (CheckSuccess(self, Globals.dice.Next(700))) {
							Success(self);
						} else {
							Fail(self);
						}

						self.Sound((SoundFX) 567);
						if (!self.Flag_Moving) {
							self.Direction = Point2D.GetDirFromTo(self, target);
						}

						self.currentSkill = null;
						if (!target.IsDeleted && !target.Flag_Dead) {
							WeaponSkillTargetQueuePlugin.AddTarget(self, target);//we're not really adding the target, just restarting the attack, most probably
						} else {
							WeaponSkillTargetQueuePlugin.FightCurrentTarget(self);
						}
					}
				}
			}
		}

		public override void Success(Character self) {
			if (!Trigger_Success(self)) {
				//self.SysMessage(this.Key+" succeeded");

			}
		}

		public override void Fail(Character self) {
			if (!Trigger_Fail(self)) {
				//self.SysMessage(this.Key+" failed");
			}
		}

		protected internal override void Abort(Character self) {
			if (!Trigger_Abort(self)) {
				//self.SysMessage(this.Key+" aborted");
				Character target = self.currentSkillTarget1 as Character;
				if (target != null) {
					//WeaponSkillTargetQueuePlugin.RemoveTarget(self, target);
					WeaponSkillTargetTrackerPlugin.UnInstallTargetTracker(target, self);
				}
			}
		}
	}
}