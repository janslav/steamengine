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
	[Dialogs.ViewableClass]
	public partial class WallSpellItem {

		public override bool BlocksFit {
			get {
				return true;
			}
		}

		public override void On_Dispell(SpellEffectArgs spellEffectArgs) {
			base.On_Dispell(spellEffectArgs);
			DispellDef.ShowDispellEffect(this);
			this.Delete();
		}
	}

	[Dialogs.ViewableClass]
	public partial class WallSpellItemDef {

	}
}