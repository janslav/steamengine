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
using SteamEngine;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {
	public class HidingSkillDef : SkillDef {
		
		public HidingSkillDef(string defname, string filename, int headerLine) : base( defname, filename, headerLine ) {
		}

		private static PluginKey pluginKey = PluginKey.Get("stealthstep");

		private static PluginDef p_StealthStep;
		public static PluginDef P_StealthStep {
			get {
				if (p_StealthStep == null) {
					p_StealthStep = PluginDef.Get("p_StealthStep");
				}
				return p_StealthStep;
			}
		}

		public override void Select(AbstractCharacter ch) {
			//todo: various state checks...
			Character self = (Character) ch;
			
			if (!this.Trigger_Select(self)) {
				self.StartSkill((int) SkillName.Hiding);
			}
		}

		internal override void Start(Character self) {
			if (!this.Trigger_Start(self)) {
				self.CurrentSkill = this;
				DelaySkillStroke(self);
			}
		}
		
		public override void Stroke(Character self) {
			//todo: various state checks...
			if (!this.Trigger_Stroke(self)) {
				if (CheckSuccess(self, Globals.dice.Next(700))) {
					Success(self);
				} else {
					Fail(self);
				}
			}
			self.CurrentSkill = null;
		}
		
		public override void Success(Character self) {
			if (!this.Trigger_Success(self)) {
				self.Flag_Hidden = true;
				self.AddPlugin(pluginKey, P_StealthStep.Create());
				self.ClilocSysMessage(501240);//You have hidden yourself well.
				//todo: gain
			}
		}
		
		public override void Fail(Character self) {
			if (!this.Trigger_Fail(self)) {
				self.ClilocSysMessage(501241);//You can't seem to hide here.
			}
		}
		
		protected internal override void Abort(Character self) {
			if (!this.Trigger_Abort(self)) {
				self.SysMessage("Hiding aborted.");
			}
			self.CurrentSkill = null;
		}

		[SteamFunction]
		public static void Hide(Character self) {
			self.SelectSkill(SkillName.Hiding);
		}

		[SteamFunction]
		public static void UnHide(Character self) {
			if (self.Flag_Hidden) {
				self.ClilocSysMessage(501242); //You are no longer hidden.
				self.Flag_Hidden = false;
			}
			self.RemovePlugin(pluginKey).Delete();
		}
	}
}