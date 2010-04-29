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
using System.Collections;

namespace SteamEngine.CompiledScripts {
	[Dialogs.ViewableClass]
	public partial class PoisonPotion {

		public PoisonEffectPluginDef PoisonType {
			get { return this.TypeDef.PoisonType; }
		}

		public int PoisonPower {
			get {
				int[] arr = this.TypeDef.PoisonPower;
				if (arr == null) {
					return 0;
				} else {
					switch (arr.Length) {
						case 0:
							return 0;
						case 1:
							return arr[0];
						case 2:
							return Globals.dice.Next(arr[0], arr[1]);
						default:
							Common.Logger.WriteWarning("Poison potion " + this.TypeDef.PrettyDefname +
								" has > 2 numbers set as PoisonPower. Only 2 are supported, for randomization");
							goto case 2;
					}
				}
			}
		}

		public TimeSpan PoisonTickInterval {
			get { return this.TypeDef.PoisonTickInterval; }
		}

		public int PoisonDuration {
			get { return this.TypeDef.PoisonDuration; }
		}
	}

	[Dialogs.ViewableClass]
	public partial class PoisonPotionDef {

	}
}