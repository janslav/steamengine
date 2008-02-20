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

        public const int limwght = 17;

        public StealingSkillDef(string defname, string filename, int headerLine)
            : base(defname, filename, headerLine) {
        }

        public override void Select(AbstractCharacter ch) {
            //todo: various state checks...
            Character self = (Character)ch;
            if (!this.Trigger_Select(self)) {
                ((Player)self).Target(SingletonScript<Targ_Stealing>.Instance);
            }
        }

        internal override void Start(Character self) {
            if (!this.Trigger_Start(self)) {
                self.currentSkill = this;
                DelaySkillStroke(self);
            }
        }

        public override void Stroke(Character self) {
            //todo: various state checks...
            if (!this.Trigger_Stroke(self)) {
                Item item = (Item)self.currentSkillTarget2 as Item;
                if (item == null) {
                    self.SysMessage("null");
                }
                if (self.CanReach(item) == DenyResult.Allow) {
                    int diff = (int)(700 + 100 * Math.Log(((Item)self.currentSkillTarget2).Weight + 1));
                    self.SysMessage("Diff je " + diff);
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

        public override void Success(Character self) {
            self.SysMessage("Ukradeno");
            if (((Character)((Item)self.currentSkillTarget2).TopObj()) != (Character)self) {
                self.SysMessage("Ale nemam to...");
            }
            // stipnout item
            //((Item)self.currentSkillTarget2).Newitem();
        }

        public override void Fail(Character self) {
            if (!this.Trigger_Fail(self)) {
                self.SysMessage("Krádež se nezdaøila.");
                self.Trigger_HostileAction(self);
            }
        }

        protected internal override void Abort(Character self) {
            this.Trigger_Abort(self);
            self.SysMessage("Okrádání bylo pøedèasnì pøerušeno.");
        }
    }


    public class Targ_Stealing : CompiledTargetDef {

        protected override void On_Start(Character self, object parameter) {
            self.SysMessage("Co chceš ukrást?");
            base.On_Start(self, parameter);
        }

        protected override bool On_TargonItem(Character self, Item targetted, object parameter) {
            if (self.currentSkill != null) {
                self.ClilocSysMessage(500118);                    //You must wait a few moments to use another skill.
                return false;
            }
            self.currentSkillTarget2 = targetted;
            self.StartSkill((int)SkillName.Stealing);
            return false;
        }

        protected override bool On_TargonChar(Character self, Character targetted, object parameter) {
            self.SysMessage("Zameøuj pouze vìci.");
            return false;
        }
    }
}