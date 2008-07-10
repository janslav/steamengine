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

		protected override void On_Select(Character self) {
            //todo: various state checks...
            self.StartSkill(SkillName.Stealing);
        }

		protected override void On_Start(Character self) {
			self.currentSkill = this;
			Item item = (Item) self.currentSkillTarget2 as Item;
			if (self.CanReach(item) == DenyResult.Allow) {
				int diff = (int) (700 + 100 * Math.Log(item.Weight + 1));
				if (SkillDef.CheckSuccess(self.SkillById((int) SkillName.Stealing).RealValue, diff)) {
					this.Success(self);
				} else {
					this.Fail(self);
				}
			}
			self.currentSkill = null;
			self.currentSkillTarget2 = null;
		}

		protected override void On_Stroke(Character self) {
            throw new Exception("The method or operation is not implemented.");
        }

		protected override void On_Success(Character self) {
			self.ClilocSysMessage(500174);      // You successfully steal the item!
			self.currentSkillParam = 1;         // for On_DenyPickupItem in snooping skill
		}

		protected override void On_Fail(Character self) {
			self.ClilocSysMessage(500172);	    // I failed to steal.
			((Character) ((Item) self.currentSkillTarget2).TopObj()).Trigger_HostileAction(self);
			self.ClilocSysMessage(500167);	    // You are now a criminal.
			self.currentSkillParam = 0;
		}

        protected override void On_Abort(Character self) {
            self.SysMessage("Okrádání bylo pøedèasnì ukonèeno.");
        }
    }
}