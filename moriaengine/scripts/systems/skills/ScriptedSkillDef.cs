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
	[Dialogs.ViewableClass]
	public class ScriptedSkillDef : SkillDef {

		public ScriptedSkillDef(string defname, string filename, int headerLine) : base(defname, filename, headerLine) {
		}

		protected override bool On_Select(SkillSequenceArgs skillSeqArgs) {
			return false;
			//self.ClilocSysMessage(500014); //That skill cannot be used directly.
		}

		protected override bool On_Start(SkillSequenceArgs skillSeqArgs) {
			return false;
		}

		protected override bool On_Stroke(SkillSequenceArgs skillSeqArgs) {
			return false;
		}

		protected override bool On_Success(SkillSequenceArgs skillSeqArgs) {
			return false;
		}

		protected override bool On_Fail(SkillSequenceArgs skillSeqArgs) {
			return false;
		}

		protected override void On_Abort(SkillSequenceArgs skillSeqArgs) {
		}
	}
}


