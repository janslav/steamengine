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
	public partial class WeaponDef : DestroyableDef {

	}

	[Dialogs.ViewableClass]
	public partial class Weapon : Destroyable {

		//at durability 0, attacknumber is halved
		public int AttackVsM {
			get {
				double attackHalved = TypeDef.AttackVsM/2;
				return (int) (attackHalved + (((attackHalved * this.Durability) / this.MaxDurability)));
			}
		}

		public int AttackVsP {
			get {
				double attackHalved = TypeDef.AttackVsP/2;
				return (int) (attackHalved + (((attackHalved * this.Durability) / this.MaxDurability)));
			}
		}

		public double Piercing {
			get {
				return TypeDef.Piercing;
			}
		}
		
		public WeaponType WeaponType {
			get {
				return TypeDef.WeaponType;
			}
		}

		public int Range {
			get {
				return TypeDef.Range;
			}
		}

		public int StrikeStartRange {
			get {
				return TypeDef.StrikeStartRange;
			}
		}

		public int StrikeStopRange {
			get {
				return TypeDef.StrikeStopRange;
			}
		}

		public double Speed {
			get {
				return TypeDef.Speed;
			}
		}

		public WeaponAnimType WeaponAnimType {
			get {
				return TypeDef.WeaponAnimType;
			}
		}
	}
}