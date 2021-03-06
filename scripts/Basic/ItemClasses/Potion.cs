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

using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {
	[ViewableClass]
	public partial class Potion {
		public override void On_DClick(AbstractCharacter dclicker) {
			this.Consume(1);
			this.CreateEmptyFlask(((Character) dclicker).Backpack);
			//TODO? some sound and/or visual effect?

			base.On_DClick(dclicker);
		}

		public Item CreateEmptyFlask(Container container) {
			var emptyFlaskDef = this.TypeDef.EmptyFlask;
			if (emptyFlaskDef != null) {
				return (Item) container.NewItem(emptyFlaskDef);
			}
			return null;
		}
	}

	[ViewableClass]
	public partial class PotionDef {
	}
}