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
    public class SnoopingSkillDef : SkillDef {

        public SnoopingSkillDef(string defname, string filename, int headerLine)
            : base(defname, filename, headerLine) {
        }

        internal static PluginKey snoopedPluginKey = PluginKey.Get("_snoopedBackpacks_");

        public override void Select(AbstractCharacter ch) {
            //todo: various state checks...
            Character self = (Character)ch;
            if (!this.Trigger_Select(self)) {
                self.StartSkill((int)SkillName.Snooping);
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
                Container conta = self.currentSkillTarget1 as Container;
                if (!self.CanReachWithMessage(conta)) {
                    Fail(self);
                } else {
                    if (SkillDef.CheckSuccess(self.Skills[(int)SkillName.Snooping].RealValue, 800)) {
                        Success(self);
                    } else {
                        Fail(self);
                    }
                }
                self.currentSkill = null;
                self.currentSkillTarget1 = null;
            }
        }

        public override void Success(Character self) {
            if (!this.Trigger_Success(self)) {
                Container cnt = (Container)self.currentSkillTarget1;
                self.SysMessage("Vidíš do batohu hráèe " + (cnt.TopObj()).Name+ ".");
                cnt.OpenTo(self);
                SnoopingPlugin sb = self.GetPlugin(snoopedPluginKey) as SnoopingPlugin;
                if (sb == null) {
                    sb = (SnoopingPlugin)self.AddNewPlugin(snoopedPluginKey, SnoopingPlugin.defInstance);
                }
                sb.Add(cnt);
                sb.Timer = SnoopingPlugin.duration;
            }
        }

        public override void Fail(Character self) {
            if (!this.Trigger_Fail(self)) {
                Character steal = (Character)(((Container)self.currentSkillTarget1).TopObj());
                self.ClilocSysMessage(500210);      // You failed to peek into the container. 
                int ran = (int)Globals.dice.Next(4);
                if (ran == 3) {
                    steal.SysMessage(self.Name + " se ti pokusil otevøít batoh.");
                    steal.Trigger_HostileAction(self);
                    self.ClilocSysMessage(500167);  // You are now a criminal.
                    self.DisArm();                  // Bonus for fatal fail. Can be more
                } else if ((ran == 2) || (ran == 1)) {
                    steal.SysMessage(self.Name + " se ti pokusil otevøít batoh.");
                    steal.Trigger_HostileAction(self);
                    self.ClilocSysMessage(500167);  // You are now a criminal.
                }
            }
        }

        protected internal override void Abort(Character self) {
            this.Trigger_Abort(self);
            self.SysMessage("Šacování pøedèasnì ukonèeno.");
        }
    }

    [Dialogs.ViewableClass]
    public partial class SnoopingPlugin {

        public static readonly SnoopingPluginDef defInstance = new SnoopingPluginDef("p_snoopedBackpacks", "C#scripts", -1);
        internal static PluginKey snoopedPluginKey = PluginKey.Get("_snoopedBackpacks_");
        public static int duration = 180;

        public bool On_DenyPickupItem(DenyPickupArgs args) {
            Container conta = args.manipulatedItem.Cont as Container;
            Character stealer = args.pickingChar as Character;
            stealer.currentSkillTarget2 = (Item)args.manipulatedItem;
            stealer.SelectSkill((int)SkillName.Stealing);
            if ((conta != null) && (this.Contains(conta))) {
                if ((stealer.currentSkillParam != null) && (int)stealer.currentSkillParam == 1) {          // currentSkillParam == 1 if stealing successed
                    stealer.currentSkillParam = null;
                    return false;
                } else {
                    args.Result = DenyResult.Deny_ThatDoesNotBelongToYou;
                    stealer.currentSkillParam = null;
                    return true;
                }
            }
            stealer.currentSkillParam = null;
            return false;
        }

        public void Add(Container cont) {
            if (this.snoopedBackpacks == null) {
                this.snoopedBackpacks = new LinkedList<Container>();
            }
            if (!this.snoopedBackpacks.Contains(cont)) {
                this.snoopedBackpacks.AddFirst(cont);
            }
        }

        public bool Contains(Container cont) {
            if (this.snoopedBackpacks == null) {
                return false;
            } else {
                return this.snoopedBackpacks.Contains(cont);
            }
        }

        public void On_Timer() {
            foreach (Container cont in this.snoopedBackpacks) {
                if (OpenedContainers.HasContainerOpen(((Character)this.Cont).Conn, cont) == DenyResult.Allow) {
                    this.Timer = duration;
                    return;
                }
            }
            this.Delete();
        }
    }
}