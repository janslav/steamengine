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
	[Dialogs.ViewableClass]
	public class StealingSkillDef : SkillDef {

		public StealingSkillDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		protected override bool On_Select(SkillSequenceArgs skillSeqArgs) {
			//todo: various state checks...
			return false;
		}

		protected override bool On_Start(SkillSequenceArgs skillSeqArgs) {
			skillSeqArgs.PhaseStroke();
			return true;//cancel - don't delay
		}

		protected override bool On_Stroke(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			Item item = (Item) skillSeqArgs.Target1;

			if (self.CanReach(item) == DenyResult.Allow) {
				int diff = (int) (700 + 100 * Math.Log(item.Weight + 1));
				skillSeqArgs.Success = SkillDef.CheckSuccess(self.GetSkill(SkillName.Stealing), diff);
			} else {
				skillSeqArgs.Success = false;
			}
			return false;
		}

		protected override bool On_Success(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			self.ClilocSysMessage(500174);      // You successfully steal the item!
			return true; //true = no default disposing of skillSeqArgs, Snooping implementation does the rest
		}

		protected override bool On_Fail(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			Item item = (Item) skillSeqArgs.Target1;
			self.ClilocSysMessage(500172);	    // I failed to steal.
			((Character) item.TopObj()).Trigger_HostileAction(self);
			//self.ClilocSysMessage(500167);	    // You are now a criminal.
			return true; //true = no default disposing of skillSeqArgs, Snooping implementation does the rest
		}

		protected override void On_Abort(SkillSequenceArgs skillSeqArgs) {
			skillSeqArgs.Self.SysMessage("Okrádání bylo pøedèasnì ukonèeno.");
		}
	}
}