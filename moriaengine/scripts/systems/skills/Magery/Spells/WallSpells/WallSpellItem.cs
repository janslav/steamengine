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
using System.Text.RegularExpressions;
using SteamEngine.Timers;
using SteamEngine.Persistence;
using SteamEngine.Common;
using SteamEngine.Regions;
using SteamEngine.CompiledScripts;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {
	public partial class WallSpellItem {

		static TimerKey timerKey = TimerKey.Get("_spellEffectTimer_");

		public int SpellPower {
			get {
				return this.spellPower;
			}
		}

		internal void Init(int spellPower, TimeSpan duration, WallDirection wallDir) {
			this.spellPower = spellPower;

			this.DeleteTimer(timerKey);

			BoundTimer timer = new SpellEffectTimer();
			this.AddTimer(timerKey, timer);
			timer.DueInSpan = duration;
		}

		[SaveableClass]
		[DeepCopyableClass]
		public class SpellEffectTimer : BoundTimer {
			[DeepCopyImplementation]
			[LoadingInitializer]
			public SpellEffectTimer() {
			}

			protected override void OnTimeout(TagHolder cont) {
				this.Cont.Delete();
			}
		}

		public override bool BlocksFit {
			get {
				return true;
			}
		}
	}
}