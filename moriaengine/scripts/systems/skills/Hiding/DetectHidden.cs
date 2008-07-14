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
	public class DetectHiddenSkillDef : SkillDef {
		
		public DetectHiddenSkillDef(string defname, string filename, int headerLine) : base( defname, filename, headerLine ) {
		}

		protected override void On_Select(Character self) {
			//todo: various state checks...
			self.StartSkill(SkillName.DetectHidden);
		}

		protected override void On_Start(Character self) {
			self.currentSkill = this;
			DelaySkillStroke(self);
		}

		protected override void On_Stroke(Character self) {
			//todo: various state checks...
			Map map = self.GetMap();
			Point2D point = new Point2D(self);
			ushort pointX = point.x;
			ushort pointY = point.y;
			int s = 0;
			foreach (Character person in map.GetCharsInRange(pointX, pointY, (ushort) GetEffectForChar(self))) {
				if (CheckSuccess(self, person.GetSkill((int) SkillName.Hiding))) {
					s++;
					self.currentSkillTarget1 = person;
					this.Success(self);
				}
			}
			if (s == 0) {     //If nobody was found
				this.Fail(self);
			}
			self.currentSkill = null;
			self.currentSkillTarget1 = null;
		}

		protected override void On_Success(Character self) {
			Character person = (Character) self.currentSkillTarget1;
			HiddenHelperPlugin ssp = person.GetPlugin(HidingSkillDef.pluginKey) as HiddenHelperPlugin;
			if (ssp != null) {
				if (ssp.hadDetectedMe == null) {
					Packets.NetState.AboutToChangeVisibility(person);
					ssp.hadDetectedMe = new LinkedList<Character>();
					ssp.hadDetectedMe.AddFirst(self);
				} else if (!ssp.hadDetectedMe.Contains(self)) {
					Packets.NetState.AboutToChangeVisibility(person);
					ssp.hadDetectedMe.AddFirst(self);
				}
			}
		}

		protected override void On_Fail(Character self) {
			self.ClilocSysMessage(500817);//You can see nothing hidden there.
			self.currentSkill = null;
			self.currentSkillTarget1 = null;
		}
		
		protected override void On_Abort(Character self) {
			self.SysMessage("Detecting Hidden aborted.");
            self.currentSkill = null;
            self.currentSkillTarget1 = null;
		}
	}
}