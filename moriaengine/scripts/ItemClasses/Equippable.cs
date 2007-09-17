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
	public partial class Equippable : Item {
		public override byte Layer {
			get {
				return TypeDef.Layer;
			}
		}

		public override sealed bool IsEquippable {
			get {
				return true;
			}
		}

		public void Equip() {
			AbstractCharacter src = Globals.SrcCharacter;
			src.Equip(src, this);
		}

		public override void On_DClick(AbstractCharacter from) {
			//(TODO): check ownership(?), trigger snooping...
			if (this.Cont != from) {
				from.TryEquip(from, this);
			}
		}
		public override bool TwoHanded {
			get {
				if (Def!=null) return TypeDef.TwoHanded;
				return false;
			}
		}
	}
}