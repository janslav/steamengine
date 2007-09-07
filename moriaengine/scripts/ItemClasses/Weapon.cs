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
	public partial class WeaponDef : DestroyableDef {

	}

	public partial class Weapon : Destroyable {

		//at durability 0, attacknumber is halved
		public int AttackNumber {
			get {
				return (int) (Def.AttackNumber - (((Def.AttackNumber * this.Durability) / this.MaxDurability))/2);
			}
		}
		
		public WeaponType WeaponType {
			get {
				return Def.WeaponType;
			}
		}

		public SkillName CombatSkill {
			get {
				return Def.CombatSkill;
			}
		}

		public int Range {
			get {
				return Def.Range;
			}
		}

		public int StrikeStartRange {
			get {
				return Def.StrikeStartRange;
			}
		}

		public int StrikeStopRange {
			get {
				return Def.StrikeStopRange;
			}
		}
	}
}