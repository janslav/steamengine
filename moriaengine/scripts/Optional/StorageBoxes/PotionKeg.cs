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
	public partial class PotionKeg {

		public override void On_DClick (AbstractCharacter ac) {
            ((Player)ac.Cont).Target(SingletonScript<Targ_PotionKeg>.Instance, this);
			base.On_DClick (ac);
		}

	}
	public class Targ_PotionKeg: CompiledTargetDef {

		protected override void On_Start (Player self, object parameter) {
			self.SysMessage ("Zaměř potiony, které chceš vylít do kegu");
			base.On_Start (self, parameter);
		}

		protected override TargetResult On_TargonItem(Player self, Item targetted, object parameter) {
			PotionKeg focus = parameter as PotionKeg;
			if (!self.CanReachWithMessage (focus)) {
                self.SysMessage ("CanReachWithMessage");
				return TargetResult.RestartTargetting;
			}
			if (targetted.Type.Defname == "t_allpotions") {
              if ((focus.TypeDef.Capacity - focus.potionsCount) < (int)targetted.Amount) {	// poresime prekroceni nosnosti kegu -> do kegu se prida jen tolik potionu, kolik skutecne lze pridat
                int potionsToTake = focus.TypeDef.Capacity - focus.potionsCount;
                targetted.Amount -= potionsToTake;
                focus.potionsCount += potionsToTake;
              } else {
                focus.potionsCount += (int)targetted.Amount;
                targetted.Delete();
              }
			} else if(targetted.Type.Defname == "t_bottle_empty"){
                self.SysMessage("Láhev je prázdná.");
            } else {
				self.SysMessage ("Můžeš nalít jenom potiony.");
			}
         
			return TargetResult.Done;
		}

	}

	//[Dialogs.ViewableClass]
	//public partial class PotionKegDef {}
	//
}
