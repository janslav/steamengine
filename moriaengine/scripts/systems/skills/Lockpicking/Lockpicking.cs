using System;
using SteamEngine;
using SteamEngine.Timers;
using SteamEngine.Persistence;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {

	[Dialogs.ViewableClass]
	public class LockpickSkillDef : SkillDef {
		public LockpickSkillDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		protected override bool On_Select(SkillSequenceArgs skillSeqArgs) {
			//todo: various state checks...
			Player self = skillSeqArgs.Self as Player;
			if (self != null) {
				self.Target(SingletonScript<Targ_Lockpick>.Instance, skillSeqArgs);
			}
			return true;
		}

		protected override bool On_Start(SkillSequenceArgs skillSeqArgs) {
			return false;
		}

		protected override bool On_Stroke(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			Item targetted = (Item)skillSeqArgs.Target1;
			if (targetted == null || targetted.IsDeleted) {
				self.SysMessage(self.IsFemale ? Loc<LockpickLoc>.Get(self.Language).forgottenItemWoman : Loc<LockpickLoc>.Get(self.Language).forgottenItem); // ztrata targetu
			} else if (self.CanReachWithMessage(targetted)) {
				skillSeqArgs.Success = this.CheckSuccess(self, Globals.dice.Next(700));	// TODO pridat succes v zavislosti na obtiznosti zamku
				return false;
				// odemknuti predmetu
			}
			skillSeqArgs.PhaseAbort();
			return true;
		}

		protected override bool On_Success(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			Item targetted = (Item)skillSeqArgs.Target1;
			self.SysMessage(self.IsFemale ? Loc<LockpickLoc>.Get(self.Language).successWoman : Loc<LockpickLoc>.Get(self.Language).success);
			return false;
		}

		protected override bool On_Fail(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			skillSeqArgs.Self.SysMessage(Loc<LockpickLoc>.Get(self.Language).fail);
			return false;
		}

		protected override void On_Abort(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			skillSeqArgs.Self.SysMessage(Loc<LockpickLoc>.Get(self.Language).abort);
		}
	}


	public class t_lockpick : CompiledTriggerGroup {
		public void On_DClick(Item self, Character clicker) {
			//TODO? use resource system for consuming lockpicks

			if (self.TopObj() == clicker) {
				clicker.SelectSkill(SkillSequenceArgs.Acquire(clicker, SkillName.Lockpicking, self));
				//StartLockpick(clicker, self);
			} else {
				Item otherLockpick = null;
				foreach (Item i in clicker.Backpack) {
					if (i.Type == this) {
						otherLockpick = i;
						break;
					}
				}
				if (otherLockpick != null) {
					clicker.SelectSkill(SkillSequenceArgs.Acquire(clicker, SkillName.Lockpicking, self));
				}
			}
		}
	}


	public class Targ_Lockpick : CompiledTargetDef {

		protected override void On_Start(Player self, object parameter) {
			self.SysMessage(Loc<LockpickLoc>.Get(self.Language).whatToUseLockOn);
			base.On_Start(self, parameter);
		}

		protected override bool On_TargonItem(Player self, Item targetted, object parameter) {
			SkillSequenceArgs skillSeq = (SkillSequenceArgs) parameter;
			
			//nevidi na cil
			if (self.CanReachWithMessage(targetted)) {
				if (!ItemLockPlugin.IsLocked(targetted)) {
					skillSeq.Target1 = targetted;
					skillSeq.PhaseStart();
				}
			}
			return false;
		}

		protected override bool On_TargonChar(Player self, Character targetted, object parameter) {
			self.SysMessage(Loc<LockpickLoc>.Get(self.Language).cantUseLockpick);
			return false;
		}

		private TimerKey lockpickTimerKey = TimerKey.Get("_lockpickTimer_");
	}

	public class LockpickLoc : CompiledLocStringCollection {
		public string cantUseLockpick = "Na tohle nelze paklíè použít.";
		public string whatToUseLockOn = "Na co chceš použít paklíè?";
		public string abort = "Odemykání pøerušeno.";
		public string fail = "Nepodaøilo se ti odemknout zámek";
		public string success = "Úspìšnì jsi odemkl zámek";
		public string successWoman = "Úspìšnì jsi odemkla zámek";
		public string forgottenItem = "Zapomnìl jsi, co máš odemknout";
		public string forgottenItemWoman = "Zapomnìla jsi, co máš odemknout";
	}
}