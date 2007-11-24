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

		public override void Select(AbstractCharacter ch) {
			//todo: various state checks...
            Character self = (Character)ch;
			if (!this.Trigger_Select(self)) {
                self.SysMessage("Select");
				self.StartSkill((int) SkillName.DetectHidden);
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
                Map map = self.GetMap();
                Point2D point = new Point2D(self);
                ushort pointX = point.x;
                ushort pointY = point.y;
                int s = 0;
                self.SysMessage("Stroke");
                foreach (Character person in map.GetCharsInRange(pointX, pointY, (ushort) GetEffectForChar(self))) {
		    		if (CheckSuccess(self, person.Skills[(int) SkillName.Hiding].RealValue)) {
                        s++;
                        self.SysMessage("Suceeees");
                        self.currentSkillTarget1 = person;
		    			Success(self);
                    }
				}
                if (s==0) {
                    self.SysMessage("s = 0");
                    Fail(self);
                }
                self.SysMessage("s je "+s);
			}
			self.currentSkill = null;
            self.currentSkillTarget1 = null;
		}

        public override void Success(Character self) {
            if (!this.Trigger_Success(self)) {
                Character person = (Character)self.currentSkillTarget1;
                StealthStepPlugin ssp = person.GetPlugin(HidingSkillDef.pluginKey) as StealthStepPlugin;
                if (ssp != null) {
                    if (ssp.hadDetectedMe == null) {
                        self.SysMessage("Stvoren");
                        ssp.hadDetectedMe = new LinkedList<Character>();
                        ssp.hadDetectedMe.AddFirst(self);
                    } else if (!ssp.hadDetectedMe.Contains(self)) {
                        self.SysMessage("Pridan");
                        ssp.hadDetectedMe.AddFirst(self);
                    }
                }
            }
        }
		
		public override void Fail(Character self) {
			if (!this.Trigger_Fail(self)) {
                self.ClilocSysMessage(500817);//You can see nothing hidden there.
                self.currentSkill = null;
                self.currentSkillTarget1 = null;
			}
		}
		
		protected internal override void Abort(Character self) {
			this.Trigger_Abort(self);
			self.SysMessage("Detecting Hidden aborted.");
            self.currentSkill = null;
            self.currentSkillTarget1 = null;
		}
	}
}