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

/*namespace SteamEngine.CompiledScripts {
    [Dialogs.ViewableClass]
    public class t_backpack : CompiledTriggerGroup {
        public void On_Dclick(Player self, Player snooped) {
            if (snooped.Backpack == self.Backpack) {    //nelze se vloupat sam sobe do batohu
            } else {
                self.currentSkillParam = Point2D.GetSimpleDistance(self, snooped);
                if (self.GetMap() != snooped.GetMap() || !self.GetMap().CanSeeLOSFromTo(self, snooped) || Point2D.GetSimpleDistance(self, snooped) > 2) {
                    self.SysMessage(snooped.Name + " je od tebe na prohlížení batohu pøíliš daleko.");
                } else {
                    if (SkillDef.CheckSuccess(self, self.Skills[(int)SkillName.Snooping].RealValue)) {
                        //uspech
                        //spustit stealing
                        self.SysMessage("Vidis do batohu hrace " + snooped.Name);
                        snooped.BackpackAsContainer.OpenTo(self);
                        snooped.BackpackAsContainer.On_DenyPickupItemFrom(snooped.BackpackAsContainer);
                    } else {
                        self.SysMessage("Nepovedlo se ti nepozorovanì otevøít batoh.");
                        //z zlodeje udelame crima
                        snooped.SysMessage(snooped.Name + " se ti pokusil otevøít batoh.");
                    }
                }
            }
        }
    }
}*/
        /*    public class Targ_Snooping : CompiledTargetDef {
                protected override void On_Start(Character self, object parameter) {
                    self.SysMessage("Komu se chceš podívat do batohu?");
                    base.On_Start(self, parameter);

                }

                protected override bool On_TargonItem(Character self, Item targetted, object parameter) {
                    self.SysMessage("Zameøuj pouze hráèe.");
                    return false;
                }

                protected override bool On_TargonChar(Character self, Character targetted, object parameter) {
                    if (self.currentSkill != null) {
                        self.ClilocSysMessage(500118);//You must wait a few moments to use another skill.
                        return false;
                    }

                   //cil neni hrac
                   // if (targetted != Player) {
                   //     self.SysMessage("Monstrùm se nelze podívat do batohu.");
                   //}

                    //chce se vloupat k sobe do batohu
                    if (targetted == self) {
                        self.SysMessage("Vyber nìkoho jiného než sebe.");
                        return false;
                    }

                    //nevidi na cil
                    if (targetted != self) {
                        if (self.GetMap() != targetted.GetMap() || !self.GetMap().CanSeeLOSFromTo(self, targetted) || Point2D.GetSimpleDistance(self, targetted) > 3) {
                            self.SysMessage(targetted.Name + " je od tebe pøíliš daleko.");
                            return false;
                        }
                    }
                    self.currentSkillTarget1 = (Character)targetted;
                    return false;
                }
            }*/
        /*public class SnoopingSkillDef : SkillDef {

            public SnoopingSkillDef(string defname, string filename, int headerLine)
                : base(defname, filename, headerLine) {
            }

            public override void Select(AbstractCharacter ch) {
                throw new Exception("The method or operation is not implemented.");
            }

            internal override void Start(Character self) {
                throw new Exception("The method or operation is not implemented.");
            }

            public override void Stroke(Character self) {
                throw new Exception("The method or operation is not implemented.");
            }

            public override void Success(Character self) {
                throw new Exception("The method or operation is not implemented.");
            }

            public override void Fail(Character self) {
                throw new Exception("The method or operation is not implemented.");
            }

            protected internal override void Abort(Character self) {
                throw new Exception("The method or operation is not implemented.");
            }
        }*/
