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

//500624	You do not have enough staves to construct the keg.
//500625	You do not have enough hoops to construct the keg.
//500626	You do not have a bottom for the keg.
//500627	You fail to create the keg.
//500628	Some of your staves are no longer fit for use.
//500629	You fail the project horribly, losing most of your materials in the process.
//500630	You create the keg and put it in your backpack.
//500631	You create the keg and put it at your feet.
//500632	You are too busy with something else.
//500633	What would you like to make?
//500634	The amount of wood changed since you started working with it.
//500635	Due to your exceptional skill, it's quality is higher than average.

//502910	You don't have the resources required to make that item.
//502911	You do not have the resources required to create the keg.
//502912	You must empty the keg before you can convert it for liquid storage.
//502913	You do not have the resources required to create the keg.
//502914	You split the keg while attempting to tap it, rendering it useless.
//502915	You crack the lid while attempting to construct the keg, rendering it useless.
//502916	You damage the tap while attempting to tap the keg, rendering it useless.
//502917	You lose some of your materials, but cannot seem to line the keg correctly.
//502918	You cannot seem to assemble the keg properly.
//502919	You create the keg and place it at your feet.
//502920	You create the keg and place it in your backpack.

//502965	You must use wood to make that item.

//1044043	You failed to create the item, and some of your materials are lost.
//1044284	You need a carpentry tool to make that.

//1044351	You do not have sufficient wood to make that. 

using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using SteamEngine;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {
	[Dialogs.ViewableClass]
	public class CarpentrySkillDef : SkillDef {

		public CarpentrySkillDef(string defname, string filename, int headerLine)
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
			Character self = skillSeqArgs.Self;

			self.ClilocSysMessage(501851);//You enter a meditative trance.
			MeditationPlugin mpl = (MeditationPlugin) MeditationPlugin.defInstance.Create();
			mpl.additionalManaRegenSpeed = this.GetEffectForChar(self);
			self.AddPlugin(MeditationPlugin.meditationPluginKey, mpl);
			return false;
		}

		protected override bool On_Fail(SkillSequenceArgs skillSeqArgs) {
			//500629	You fail the project horribly, losing most of your materials in the process.
			//1044043	You failed to create the item, and some of your materials are lost.

			skillSeqArgs.Self.ClilocSysMessage(501848);//You cannot focus your concentration
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

		[SteamFunction]
		public static void Carpenter(Character self) {
			self.SelectSkill(SkillName.Carpentry);
		}
	}
}