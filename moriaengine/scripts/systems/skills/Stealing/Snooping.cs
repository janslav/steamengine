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

		protected override void On_Select(Character self) {
			//todo: various state checks...
			self.StartSkill(SkillName.Snooping);
		}

		protected override void On_Start(Character self) {
			self.currentSkill = this;
			DelaySkillStroke(self);
		}

		protected override void On_Stroke(Character self) {
			//todo: various state checks...
			Container conta = self.currentSkillTarget1 as Container;
			if (!self.CanReachWithMessage(conta)) {
				this.Fail(self);
			} else {
				if (SkillDef.CheckSuccess(self.GetSkill((int) SkillName.Snooping), 800)) {
					this.Success(self);
				} else {
					this.Fail(self);
				}
			}
			self.currentSkill = null;
			self.currentSkillTarget1 = null;
		}

		protected override void On_Success(Character self) {
			Container cnt = (Container) self.currentSkillTarget1;
			self.SysMessage("Vidíš do batohu hráèe " + (cnt.TopObj()).Name + ".");
			cnt.OpenTo(self);
			SnoopingPlugin sb = self.GetPlugin(snoopedPluginKey) as SnoopingPlugin;
			if (sb == null) {
				sb = (SnoopingPlugin) self.AddNewPlugin(snoopedPluginKey, SnoopingPlugin.defInstance);
			}
			sb.Add(cnt);
			sb.Timer = SnoopingPlugin.duration;
		}

		protected override void On_Fail(Character self) {
			Character steal = (Character) (((Container) self.currentSkillTarget1).TopObj());
			self.ClilocSysMessage(500210);      // You failed to peek into the container. 
			int ran = (int) Globals.dice.Next(4);
			if (ran == 3) {
				steal.SysMessage(self.Name + " se ti pokusil otevøít batoh.");
				steal.Trigger_HostileAction(self);
				self.DisArm();                  // Bonus for fatal fail. Can be more
			} else if ((ran == 2) || (ran == 1)) {
				steal.SysMessage(self.Name + " se ti pokusil otevøít batoh.");
				steal.Trigger_HostileAction(self);
			}
		}

		protected override void On_Abort(Character self) {
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
			stealer.currentSkillTarget2 = (Item) args.manipulatedItem;
			stealer.SelectSkill((int) SkillName.Stealing);
			if ((conta != null) && (this.Contains(conta))) {
				if ((stealer.currentSkillParam1 != null) && (int) stealer.currentSkillParam1 == 1) {          // currentSkillParam == 1 if stealing successed
					stealer.currentSkillParam1 = null;
					return false;
				} else {
					args.Result = DenyResult.Deny_ThatDoesNotBelongToYou;
					stealer.currentSkillParam1 = null;
					return true;
				}
			}
			stealer.currentSkillParam1 = null;
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
				if (OpenedContainers.HasContainerOpen((Character) this.Cont, cont) == DenyResult.Allow) {
					this.Timer = duration;
					return;
				}
			}
			this.Delete();
		}
	}
}