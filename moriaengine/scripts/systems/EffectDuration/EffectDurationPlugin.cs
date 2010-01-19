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
	public partial class EffectDurationPlugin {

		public void Init(Thing source, EffectFlag sourceType, double power, TimeSpan duration) {
			this.source = source;
			this.flags = sourceType;
			this.effectPower = power;
			if (duration >= TimeSpan.Zero) {
				this.Timer = duration.TotalSeconds;
			}
		}

		public void On_Timer() {
			this.Delete();
		}

		public void On_Dispell(SpellEffectArgs spell) {
			if (this.Dispellable) {
				Thing t = this.Cont as Thing;
				if (t != null) {
					DispellDef.ShowDispellEffect(t.TopObj());
				}
				this.Delete();
			}
		}

		public double EffectPower {
			get {
				return this.effectPower;
			}
			protected set {
				this.effectPower = value;
			}
		}

		public bool Dispellable {
			get {
				return (this.flags & EffectFlag.FromBook) == EffectFlag.FromBook; //potion effects are generally not dispellable. Might want some exception from this rule at some point...?
			}
		}

		public Thing Source {
			get {
				return this.source;
			}
		}

		public EffectFlag Flags {
			get {
				return this.flags;
			}
		}

		public void On_Death() {
			this.Delete();
		}
	}
}