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


        }
    }*/

    /*public class SnoopingDeny : CompiledTriggerGroup {
        [Summary("Someone is trying to pick up item that is contained in me")]
        public bool On_DenyPickupItemFrom(DenyPickupArgs args) {
            if (((Character)args.pickingChar).currentSkillParam != null) {
                if ((Item)((Character)args.pickingChar).currentSkillParam == (Item)args.manipulatedItem) {
                    //muze zvednout
                    args.Result = DenyResult.Allow;
                    return true;
                } else {
                    //nemuze zevednout
                    args.Result = DenyResult.Deny_ThatDoesNotBelongToYou;
                    return true;
                }
            }
            args.Result = DenyResult.Deny_ThatDoesNotBelongToYou;
            return true;
        }
    }*/

    /*[Dialogs.ViewableClass]
    public partial class SnoopingPlugin {

        //public static readonly SnoopingPluginDef defInstance = new SnoopingPluginDef("p_snoopedBackpacks_", "C#scripts", -1);

        public void On_Assign() {

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

namespace SteamEngine.CompiledScripts {
    [Dialogs.ViewableClass]
    public class SnoopingSkillDef : SkillDef {

        public SnoopingSkillDef(string defname, string filename, int headerLine)
            : base(defname, filename, headerLine) {
        }

        internal static PluginKey snoopedPluginKey = PluginKey.Get("_snoopedBackpacks_");
        private static PluginDef p_snooping;
        private static PluginDef P_Snooping {
            get {
                if (p_snooping == null) {
                    p_snooping = PluginDef.Get("p_snoopedBackpacks_");
                }
                return p_snooping;
            }
        }

        public override void Select(AbstractCharacter ch) {
            //todo: various state checks...
            Character self = (Character)ch;
            if (!this.Trigger_Select(self)) {
                self.SysMessage("Select");
                self.StartSkill((int)SkillName.Snooping);
            }
        }

        internal override void Start(Character self) {
            if (!this.Trigger_Start(self)) {
                self.SysMessage("Start");
                self.currentSkill = this;
                DelaySkillStroke(self);
            }
        }

        public override void Stroke(Character self) {
            //todo: various state checks...
            if (!this.Trigger_Stroke(self)) {
                self.SysMessage("Stroke");
                Character snooped = (Character)self.currentSkillTarget1;
                if (self.GetMap() != snooped.GetMap() || !self.GetMap().CanSeeLOSFromTo(self, snooped) || (Point2D.GetSimpleDistance(self, snooped) > 2)) {
                    self.SysMessage(snooped.Name + " je od tebe na prohlížení batohu pøíliš daleko.");
                    Fail(self);
                } else {
                    self.SysMessage("else");
                    if (SkillDef.CheckSuccess(self.Skills[(int)SkillName.Snooping].RealValue, 800)) {
                        self.currentSkillTarget2 = (Container)snooped.BackpackAsContainer;
                        self.SysMessage("Succ");
                        Success(self);
                    } else {
                        Fail(self);
                    }
                }
                self.currentSkill = null;
                self.currentSkillTarget1 = null;
                self.currentSkillTarget2 = null;
            }
        }

        public override void Success(Character self) {
            if (!this.Trigger_Success(self)) {
                self.SysMessage("Vidis do batohu hrace " + ((Character)self.currentSkillTarget1).Name);
                ((Character)self.currentSkillTarget1).BackpackAsContainer.OpenTo(self);
                SnoopingPlugin sb = self.GetPlugin(snoopedPluginKey) as SnoopingPlugin;
                if (self.HasPlugin(snoopedPluginKey)) {
                    if (!sb.snoopedBackpacks.Contains((Container)self.currentSkillTarget2)) {
                        sb.snoopedBackpacks.AddFirst((Container)self.currentSkillTarget2);
                    }
                } else {
                    self.AddPlugin(snoopedPluginKey, P_Snooping.Create());
                    sb.snoopedBackpacks.AddFirst((Container)self.currentSkillTarget2);
                }
            }
        }

        public override void Fail(Character self) {
            if (!this.Trigger_Fail(self)) {
                self.SysMessage("Nepovedlo se ti nepozorovanì otevøít batoh.");
                //z zlodeje udelame crima
                ((Character)self.currentSkillTarget1).SysMessage(self.Name + " se ti pokusil otevøít batoh.");
                self.currentSkill = null;
                self.currentSkillTarget1 = null;
                self.currentSkillTarget2 = null;
            }
        }

        protected internal override void Abort(Character self) {
            this.Trigger_Abort(self);
            self.SysMessage("Snooping aborted.");
            self.currentSkill = null;
            self.currentSkillTarget1 = null;
            self.currentSkillTarget2 = null;
        }
    }
}