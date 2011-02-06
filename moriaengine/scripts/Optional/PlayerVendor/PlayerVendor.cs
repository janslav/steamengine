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

using SteamEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.LScript;


namespace SteamEngine.CompiledScripts {
	[Dialogs.ViewableClass]
	public partial class PlayerVendor {
	}

	[Dialogs.ViewableClass]
	public partial class PlayerVendorDef {
	}
}

namespace SteamEngine.CompiledScripts.Dialogs {

	[Summary("Surprisingly the dialog that will display the RegBox guts")]
	public class D_PV : CompiledGumpDef {

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
		}
	}

	public class Targ_PV : CompiledTargetDef {

		protected override void On_Start(Player self, object parameter) {
			base.On_Start(self, parameter);
		}
	}
}