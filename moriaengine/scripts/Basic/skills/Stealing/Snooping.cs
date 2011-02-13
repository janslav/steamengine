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

		internal static PluginKey snoopedPluginKey = PluginKey.Acquire("_snoopedBackpacks_");

		protected override TriggerResult On_Select(SkillSequenceArgs skillSeqArgs) {
			//todo: various state checks...
			return TriggerResult.Continue;
		}

		protected override TriggerResult On_Start(SkillSequenceArgs skillSeqArgs) {
			return TriggerResult.Continue;
		}

		protected override TriggerResult On_Stroke(SkillSequenceArgs skillSeqArgs) {
			//todo: various state checks...
			Character self = skillSeqArgs.Self;
			Container cnt = (Container) skillSeqArgs.Target1;
			if (self.CanReachWithMessage(cnt)) {
				skillSeqArgs.Success = this.CheckSuccess(self, 800);
				return TriggerResult.Continue;
			}
			return TriggerResult.Cancel;
		}

		protected override void On_Success(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			Container cnt = (Container) skillSeqArgs.Target1;
			self.SysMessage("Vid� do batohu osoby " + cnt.TopObj().Name + ".");
			cnt.OpenTo(self);
			SnoopingPlugin sb = self.GetPlugin(snoopedPluginKey) as SnoopingPlugin;
			if (sb == null) {
				sb = (SnoopingPlugin) self.AddNewPlugin(snoopedPluginKey, SnoopingPlugin.defInstance);
			}
			sb.Add(cnt);
			sb.Timer = SnoopingPlugin.duration;
		}

		protected override void On_Fail(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			Character victim = (Character) (((Container) skillSeqArgs.Target1).TopObj());
			self.ClilocSysMessage(500210); // You failed to peek into the container. 
			int ran = (int) Globals.dice.Next(4);
			switch (ran) {
				case 3: //fatal failure
					victim.SysMessage(self.Name + " se ti pokusil otev��t batoh.");
					victim.Trigger_HostileAction(self);
					self.DisArm(); // Bonus for fatal fail. Can be more
					break;
				case 2: //regular failure. Maybe no hostile trigger in this case?
				case 1:
					victim.SysMessage(self.Name + " se ti pokusil otev��t batoh.");
					victim.Trigger_HostileAction(self);
					break;
			}
		}

		protected override void On_Abort(SkillSequenceArgs skillSeqArgs) {
			skillSeqArgs.Self.SysMessage("�acov�n� p�ed�asn� ukon�eno.");
		}
	}

	[Dialogs.ViewableClass]
	public partial class SnoopingPlugin {

		public static readonly SnoopingPluginDef defInstance = new SnoopingPluginDef("p_snoopedBackpacks", "C#scripts", -1);
		internal static PluginKey snoopedPluginKey = PluginKey.Acquire("_snoopedBackpacks_");
		public static int duration = 180;

		public bool On_DenyPickupItem(DenyPickupArgs args) {
			Container conta = args.ManipulatedItem.Cont as Container;

			if (this.Contains(conta)) {
				Sanity.IfTrueThrow(conta == null, "conta == null");
				Character thief = args.PickingChar as Character;

				SkillSequenceArgs stealing = SkillSequenceArgs.Acquire(thief, SkillName.Stealing, args.ManipulatedItem, null, null, null, null);
				stealing.PhaseSelect();
				bool success = stealing.Success;
				//stealing.Dispose();

				if (!success) {
					args.Result = DenyResultMessages.Deny_ThatDoesNotBelongToYou;
					return true;
				}
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
			if (this.snoopedBackpacks == null) {
				return false;
			} else {
				return this.snoopedBackpacks.Contains(cont);
			}
		}

		public void On_Timer() {
			foreach (Container cont in this.snoopedBackpacks) {
				if (OpenedContainers.HasContainerOpen((Character) this.Cont, cont).Allow) {
					this.Timer = duration;
					return;
				}
			}
			this.Delete();
		}
	}

	[Dialogs.ViewableClass]
	public partial class SnoopingPluginDef {
	}
}