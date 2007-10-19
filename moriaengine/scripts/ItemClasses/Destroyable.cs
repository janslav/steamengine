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
	public partial class DestroyableDef : EquippableDef {

	}

	[Dialogs.ViewableClass]
	public partial class Destroyable : Equippable {

		public ushort Durability {
			get {
				return durability;
			}
			set {
				if (value != durability) {
					durability = value;
					Character cont = this.Cont as Character;
					if (cont != null) {
						if (this is Weapon) {
							cont.InvalidateCombatWeaponValues();
						} else if (this is Wearable) {
							cont.InvalidateCombatArmorValues();
						}
					}
				}
			}
		}

		public ushort MaxDurability {
			get {
				return maxDurability;
			}
			set {
				if (value != maxDurability) {
					maxDurability = value;
					Character cont = this.Cont as Character;
					if (cont != null) {
						if (this is Weapon) {
							cont.InvalidateCombatWeaponValues();
						} else if (this is Wearable) {
							cont.InvalidateCombatArmorValues();
						}
					}
				}
			}
		}
	}
}