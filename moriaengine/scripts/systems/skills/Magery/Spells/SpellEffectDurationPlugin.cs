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
	public partial class SpellEffectDurationPlugin {

		public void Init(Thing source, SpellSourceType sourceType, double effect, TimeSpan duration) {
			this.source = source;
			this.sourceType = sourceType;
			this.effect = effect;
			this.Timer = duration.TotalSeconds;
		}

		public void On_Timer() {
			this.Delete();
		}

		public void On_DispellEffect() {
			if (this.Dispellable) {
				this.Delete();
			}
		}

		public double Effect {
			get {
				return this.effect;
			}
			protected set {
				this.effect = value;
			}
		}

		public bool Dispellable {
			get {
				return !(this.sourceType == SpellSourceType.Potion); //potion effects are generally not dispellable. Might want some exception from this rule at some point...?
			}
		}

		public Thing Source {
			get {
				return this.source;
			}
		}

		public SpellSourceType SourceType {
			get {
				return this.sourceType;
			}
		}
	}
}