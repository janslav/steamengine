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
                if (!self.CanReachWithMessage((Container)self.currentSkillTarget1)) {
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
                self.SysMessage("Vidíš do batohu hráèe " + (((Container)self.currentSkillTarget1).TopObj()).Name);
                ((Container)self.currentSkillTarget1).OpenTo(self);
                SnoopingPlugin sb = self.GetPlugin(snoopedPluginKey) as SnoopingPlugin;
                if (sb == null) {
                    sb = (SnoopingPlugin)self.AddNewPlugin(snoopedPluginKey, SnoopingPlugin.defInstance);
                }
                sb.Add(cnt);
            }
            self.currentSkill = null;
            self.currentSkillTarget1 = null;
        }

        public override void Fail(Character self) {
            if (!this.Trigger_Fail(self)) {
                self.SysMessage("Nepovedlo se ti nepozorovanì otevøít batoh.");
                //z zlodeje udelame crima
                ((Character)(((Container)self.currentSkillTarget1).TopObj())).SysMessage(self.Name + " se ti pokusil otevøít batoh.");
                self.currentSkill = null;
                self.currentSkillTarget1 = null;
            }
        }

        protected internal override void Abort(Character self) {
            this.Trigger_Abort(self);
            self.SysMessage("Snooping aborted.");
            self.currentSkill = null;
            self.currentSkillTarget1 = null;
        }
    }

    [Dialogs.ViewableClass]
    public partial class SnoopingPlugin {

        public static readonly SnoopingPluginDef defInstance = new SnoopingPluginDef("p_snoopedBackpacks", "C#scripts", -1);
        internal static PluginKey snoopedPluginKey = PluginKey.Get("_snoopedBackpacks_");

        public bool On_DenyPickupItem(DenyPickupArgs args) {
            SnoopingPlugin sb = (args.pickingChar).GetPlugin(SnoopingSkillDef.snoopedPluginKey) as SnoopingPlugin;
            Container conta = args.manipulatedItem.Cont as Container;
            if ((conta != null) && (sb.Contains(conta))) {
                args.Result = DenyResult.Deny_ThatDoesNotBelongToYou;
                return true;
            }
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
            if (this.snoopedBackpacks.Contains(cont)) {
                return true;
            } else {
                return false;
            }
        }
    }
}