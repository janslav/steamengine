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
using SteamEngine.Networking;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {
	[Dialogs.ViewableClass]
	public class HidingSkillDef : SkillDef {

		public HidingSkillDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		public static PluginKey pluginKey = PluginKey.Get("hiddenHelper");

		public static PluginDef p_hiddenHelper;
		public static PluginDef P_HiddenHelper {
			get {
				if (p_hiddenHelper == null) {
					p_hiddenHelper = PluginDef.Get("p_hiddenHelper");
				}
				return p_hiddenHelper;
			}
		}

		protected override bool On_Select(SkillSequenceArgs skillSeqArgs) {
			//todo: various state checks...
			return false; //continue to @start
		}

		protected override bool On_Start(SkillSequenceArgs skillSeqArgs) {
			return false; //continue to delay, then @stroke
		}

		protected override bool On_Stroke(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			//todo: various state checks...
			skillSeqArgs.Success = this.CheckSuccess(self, Globals.dice.Next(700));

			return false; //continue to @success or @fail
		}

		protected override bool On_Success(SkillSequenceArgs skillSeqArgs) {
			Hide(skillSeqArgs.Self);
			return false;
		}

		protected override bool On_Fail(SkillSequenceArgs skillSeqArgs) {
			skillSeqArgs.Self.ClilocSysMessage(501241);//You can't seem to hide here.
			return false;
		}

		protected override void On_Abort(SkillSequenceArgs skillSeqArgs) {
			GameState state = skillSeqArgs.Self.GameState;
			if (state != null) {
				state.WriteLine(CompiledLoc<HidingLoc>.Get(state.Language).HidingAborted);
			}
		}

		[SteamFunction]
		public static void Hide(Character self) {
			self.ClilocSysMessage(501240);//You have hidden yourself well.
			self.Flag_Hidden = true;
			self.AddPlugin(pluginKey, P_HiddenHelper.Create());
		}

		[SteamFunction]
		public static void UnHide(Character self) {
			if (self.Flag_Hidden) {
				self.ClilocSysMessage(501242); //You are no longer hidden.
				self.Flag_Hidden = false;
			}
			self.DeletePlugin(pluginKey);
		}
	}

	public class HidingLoc : AbstractLoc {
		internal readonly string HidingAborted = "Hiding aborted.";
	}
}