//502924	Which gem will you use to make the jewelry?
//502925	You don't have the resources required to make that item.
//502926	That is not proper material for tinkering items.
//502927	You already have a tinkering menu.
//502928	What materials would you like to work with?
//502929	There is not enough room in your backpack!  You do not assemble the sextant.
//502930	Use that on an axle to make an axle with gears.
//502931	Use that on an axle with gears to make clock parts.
//502932	Use that on an axle with gears to make sextant parts.
//502933	Use that on gears to make an axle with gears.
//502934	Use that on clock parts to make a clock.
//502935	Use that on springs to make clock parts, or a hinge to make sextant parts.
//502936	Use that on a clock frame to make a clock.

//502957	You don't have the resources required to make that item.
//502958	Use this on only one gem.
//502959	You don't have room for that item.
//502960	You fail to make the jewelry properly.
//502961	That's not a gem or jewel of the proper type.
//502962	Use raw material.
//502963	You decide you don't want to make anything.
//502964	You didn't select anything.

//1044039	You need a tinker's toolkit to make that.

//1044043	You failed to create the item, and some of your materials are lost.

//1044627	You don't have enough sand to make that.
//1044628	You must be near a forge to blow glass.


//1044633	You haven't learned masonry.  Perhaps you need to study a book!
//1044634	You haven't learned glassblowing.  Perhaps studying a book would help!
//1044635	Requires masonry (carpentry specialization)
//1044636	Requires glassblowing (alchemy specialization)
using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using SteamEngine;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {
	[Dialogs.ViewableClass]
	public class TinkeringSkillDef : SkillDef {

		public TinkeringSkillDef(string defname, string filename, int headerLine)
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