//502767	What would you like to make?
//502768	You create the item and put it in your backpack.
//502769	You create the item and put it at your feet.
//502770	Please select the cloth you would like to use.
//502771	You cannot reach that.
//502772	That cloth belongs to someone else.
//502773	Someone else is using that cloth.
//502774	There's not enough material on that.
//502775	That cloth belongs to someone else.
//502776	Someone else is using that cloth.
//502777	There's not enough of the right type of material on that.
//502778	That's not the proper material.
//502779	You do not have enough leather to make this item.
//502780	You don't have room for the item in your pack, so you stop working on it.
//502781	You don't have room for the item and leftovers in your pack, so you stop working on it.
//502782	You place the left-over cloth pieces into your backpack
//502783	You place the left-over cloth pieces into your backpack
//502784	Due to your exceptional skill, its quality is higher than average.
//502785	You were barely able to make this item.  It's quality is below average.
//502786	You create the item and put it in your backpack.
//502787	You create the item and put it at your feet.
//502788	You throw the useless pieces away.

//1044043	You failed to create the item, and some of your materials are lost.

//1044454	You don’t have a bolt of cloth.
//1044456	You don't have any ready cloth.
//1044463	You do not have sufficient leather to make that item.
using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using SteamEngine;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {
	[Dialogs.ViewableClass]
	public class TailoringSkillDef : SkillDef {

		public TailoringSkillDef(string defname, string filename, int headerLine)
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
			skillSeqArgs.Self.SysMessage("Výroba přerušena");
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