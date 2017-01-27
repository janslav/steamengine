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

using System.Collections.Generic;
using Shielded;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	[ViewableClass]
	public partial class SpellScrollDef {
		private readonly Shielded<SpellDef> spellDef = new Shielded<SpellDef>();

		public SpellDef SpellDef {
			get {
				SeShield.AssertInTransaction();

				if (this.spellDef.Value != null) {
					if (this.spellDef.Value.ScrollItem != this) {
						this.spellDef.Value = null;
					}
				}

				if (this.spellDef.Value == null) {
					var dict = new Dictionary<SpellScrollDef, SpellDef>();
					foreach (var spell in SpellDef.AllSpellDefs) {
						var ssd = spell.ScrollItem;
						if (ssd != null) {
							dict.Add(ssd, spell); //if there was more than 1 spells using 1 scroll, this line would throw an exception. 
												  //Which is good. That's why we use a dict here. So leave it alone.
						}
					}
					SpellDef def;
					dict.TryGetValue(this, out def);
					this.spellDef.Value = def;
				}
				return this.spellDef.Value;
			}
		}

		public int SpellId {
			get {
				var def = this.SpellDef;
				if (def != null) {
					return def.Id;
				}
				return -1;
			}
		}
	}

	[ViewableClass]
	public partial class SpellScroll {
		public SpellDef SpellDef => this.TypeDef.SpellDef;

		public int SpellId => this.TypeDef.SpellId;
	}
}