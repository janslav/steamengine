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

        public override void Select(AbstractCharacter ch) {
            //todo: various state checks...
            Character self = (Character)ch;
            if (!this.Trigger_Select(self)) {
                self.StartSkill((int)SkillName.Stealing);
            }
        }

        internal override void Start(Character self) {
            if (!this.Trigger_Start(self)) {
                self.currentSkill = this;
                Item item = (Item)self.currentSkillTarget2 as Item;
                if (self.CanReach(item) == DenyResult.Allow) {
                    int diff = (int)(700 + 100 * Math.Log(item.Weight + 1));
                    if (SkillDef.CheckSuccess(self.Skills[(int)SkillName.Stealing].RealValue, diff)) {
                        Success(self);
                    } else {
                        Fail(self);
                    }
                }
                self.currentSkill = null;
                self.currentSkillTarget2 = null;
            }
        }

        public override void Stroke(Character self) {
            throw new Exception("The method or operation is not implemented.");
        }

        public override void Success(Character self) {
            self.SysMessage("Pøedmìt ukraden.");
            self.currentSkillParam = 1;        // for On_DenyPickupItem in snooping skill
        }

        public override void Fail(Character self) {
            if (!this.Trigger_Fail(self)) {
                self.SysMessage("Krádež se nezdaøila.");
                self.Trigger_HostileAction(self);
            }
        }

        protected internal override void Abort(Character self) {
            this.Trigger_Abort(self);
            self.SysMessage("Okrádání bylo pøedèasnì ukonèeno.");
        }
    }
}