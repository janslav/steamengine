using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using SteamEngine;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {
	[Dialogs.ViewableClass]
	public class CookingSkillDef : SkillDef {

		public CookingSkillDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		protected override bool On_Select(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;

			//todo: paralyzed state etc.
			return !CheckPrerequisities(skillSeqArgs); //F = continue to @start, T = stop
		}

		protected override bool On_Start(SkillSequenceArgs skillSeqArgs) {
			return false; //continue to delay, then @stroke
		}

		protected override bool On_Stroke(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;

			if (!CheckPrerequisities(skillSeqArgs)) {
				return true;//stop
			}
			skillSeqArgs.Success = this.CheckSuccess(self, Globals.dice.Next(700));

			return false; //continue to @success or @fail
		}

		protected override bool On_Success(SkillSequenceArgs skillSeqArgs) {
			return false;
		}

		protected override bool On_Fail(SkillSequenceArgs skillSeqArgs) {
			return false;
		}

		protected override void On_Abort(SkillSequenceArgs skillSeqArgs) {
			skillSeqArgs.Self.SysMessage("Výroba pøerušena");
		}

		[Remark("Check if we are alive, have enough stats etc.... Return false if the trigger above" +
				" should be cancelled or true if we can continue")]
		private bool CheckPrerequisities(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			if (!self.CheckAliveWithMessage()) {
				return false;//no message needed, it's been already sent in the called method
			}
			if (self.Stam <= self.MaxStam / 10) {
				self.ClilocSysMessage(501991);//You are too fatigued to even lift a finger.
				return false; //stop
			}
			return true;
		}
	}
}