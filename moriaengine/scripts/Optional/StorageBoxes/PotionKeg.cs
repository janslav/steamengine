﻿/*
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
	public partial class PotionKeg {

		public override void On_DClick (AbstractCharacter ac) {
			base.On_DClick (ac);
		}

	}
	public class Targ_PotionKeg: CompiledTargetDef {
		protected override void On_Start (Player self, object parameter) {
			self.SysMessage ("Zaměř potiony, které chceš vylít do kegu");
			base.On_Start (self, parameter);
		}
		protected override bool On_TargonItem (Player self, Item targetted, object parameter) {
			PotionKeg focus = parameter as PotionKeg;
			if (!self.CanReachWithMessage (focus)) {
				return false;
			}
			if (targetted.Type.Defname == "t_potion") {
				int previousCount;

			} else {
				self.SysMessage ("Muzes nalit jenom potiony");
			}
			return true;
		}

	}

	[Dialogs.ViewableClass]
	public partial class PotionKegDef {
	}
}
