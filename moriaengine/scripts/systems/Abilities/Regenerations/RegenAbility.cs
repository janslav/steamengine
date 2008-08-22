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
using System.Timers;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

    [ViewableClass]
    [Summary("Class holding information about regeneration abilities (hits, mana, stamina). Slightly"+
            "different from the ancestor as it will not be deleted when zeroized (regens can go to negative values "+
            "and 0 means it simply doesnt regenerate")]
    public class RegenAbility : Ability {
        public RegenAbility(AbilityDef def, Character cont) : base(def, cont) {            
        }

        [Summary("Get or set actual ability points for this ability. But do not delete the ability object when reaching 0 "+
                "instead offer to go to negative values")]
        public override int Points {
            get {
                return points;
            }
            set {                
                points = Math.Min(value, this.MaxPoints); //we can go under zero but not over MaxPoints!                                
                //refresh the stored abilitypoints information on the RegenerationPlugin
                RegenerationPlugin regPlug = (RegenerationPlugin)cont.GetPlugin(RegenerationPlugin.regenerationsPluginKey);
                regPlug.RefreshRegenPoints();
            }
        }   
    }
}