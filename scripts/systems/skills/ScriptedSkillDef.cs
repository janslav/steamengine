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
	public class ScriptedSkillDef : SkillDef {

		public ScriptedSkillDef(string defname, string filename, int headerLine) : base(defname, filename, headerLine) {
		}

		public override void Select(AbstractCharacter ch) {
			Character self = (Character) ch;
			
			//temp code, just to see it does *something*
			if (!Trigger_Select(self)) {
				//Trigger_Fail(self);
				ch.SysMessage("I clicked the button of the skill "+this+" which has value "+ch.Skills[this.Id].RealValue+" and cap "+ch.Skills[this.Id].Cap);
				ch.ClilocSysMessage(500014); //That skill cannot be used directly.
			}
		}

		internal override void Start(Character self) {
			Trigger_Start(self);
		}
		
		public override void Stroke(Character self) {
			Trigger_Stroke(self);
		}

		public override void Success(Character self) {
			Trigger_Success(self);
		}
	    
		public override void Fail(Character self) {
			Trigger_Fail(self);
		}
		
		protected internal override void Abort(Character self) {
			Trigger_Abort(self);
		}
	}
}


