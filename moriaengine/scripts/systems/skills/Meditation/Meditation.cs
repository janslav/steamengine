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

namespace SteamEngine.CompiledScripts {
	[Dialogs.ViewableClass]
	public class MeditationSkillDef : SkillDef {

		public MeditationSkillDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		protected override bool On_Select(SkillSequenceArgs skillSeqArgs) {
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
			skillSeqArgs.Self.ClilocSysMessage(501848);//You cannot focus your concentration
			return false;
		}

		protected override void On_Abort(SkillSequenceArgs skillSeqArgs) {
			skillSeqArgs.Self.ClilocSysMessage(501848);//You cannot focus your concentration
		}

		[Remark("Check if we are alive, don't have weapons etc.... Return false if the trigger above" +
				" should be cancelled or true if we can continue")]
		private bool CheckPrerequisities(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			if (!self.CheckAliveWithMessage()) {
				return false;//no message needed, it's been already sent in the called method
			}
			if (self.Weapon != null) {
				self.ClilocSysMessage(502626);//Your hands must be free to cast spells or meditate.
				return false; //stop
			}
			if (self.Hits <= self.MaxHits / 10) {
				self.ClilocSysMessage(501849);//The mind is strong, but the body is weak.
				return false; //stop
			}
			if (self.Mana >= self.MaxMana) {
				self.ClilocSysMessage(501846);//You are at peace.
				return false; //stop
			}
			return true;
		}

		[SteamFunction]
		public static void Meditate(Character self) {
			self.SelectSkill(SkillName.Meditation);
		}
	}

	[Dialogs.ViewableClass]
	public partial class MeditationPlugin {
		public static readonly MeditationPluginDef defInstance = new MeditationPluginDef("p_meditation", "C#scripts", -1);
		internal static PluginKey meditationPluginKey = PluginKey.Acquire("_meditation_");

		public void On_Assign() {
			//add the regeneration speed to character
			((Character) Cont).ManaRegenSpeed += this.additionalManaRegenSpeed;
		}

		public void On_UnAssign(Character formerCont) {
			formerCont.ManaRegenSpeed -= this.additionalManaRegenSpeed;
			if (formerCont.Mana >= formerCont.MaxMana) {//meditation finished
				formerCont.ClilocSysMessage(501846);//You are at peace.
			} else {//meditation somehow aborted
				formerCont.ClilocSysMessage(501848);//You cannot focus your concentration
			}
		}

		public void On_Step(ScriptArgs args) {
			Delete();
		}

		public void On_SkillStart(SkillSequenceArgs skillSeqArgs) {
			Delete();
		}

		//TODO - other triggers such as ItemPickup, Speak, DClick, use another skill or ability etc...
	}
}