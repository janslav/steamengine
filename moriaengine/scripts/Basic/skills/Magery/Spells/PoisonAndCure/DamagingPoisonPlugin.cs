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
using SteamEngine;
using SteamEngine.Common;
using SteamEngine.Regions;
using SteamEngine.Timers;
using SteamEngine.Persistence;
using SteamEngine.Networking;

namespace SteamEngine.CompiledScripts {


	[Dialogs.ViewableClass]
	public partial class DamagingPoisonEffectPlugin {

		public override void On_PoisonTick() {

			DamageType damageType = DamageType.Poison;
			if ((this.Flags & EffectFlag.FromPotion) == EffectFlag.FromPotion) {
				damageType |= DamageType.Physical; //poison in potions/on weapons is not magic, maybe...?
			} else {
				damageType |= DamageType.Magic; //poison in potions/on weapons is not magic, maybe...?
			}

			Character self = this.Cont as Character;
			if (self != null) {

				DamageManager.CauseDamage(this.SourceThing as Character, self, damageType, this.EffectPower);

				base.On_PoisonTick();
			} else {
				this.Delete();
			}			
		}

		//1042857	*You feel a bit nauseous*
		//1042858	*~1_PLAYER_NAME~ looks ill.*
		//1042859	* You feel disoriented and nauseous! *
		//1042860	* ~1_PLAYER_NAME~ looks extremely ill. *
		//1042861	* You begin to feel pain throughout your body! *
		//1042862	* ~1_PLAYER_NAME~ stumbles around in confusion and pain. *
		//1042863	* You feel extremely weak and are in severe pain! *
		//1042864	* ~1_PLAYER_NAME~ is wracked with extreme pain. *
		//1042865	* You are in extreme pain, and require immediate aid! *
		//1042866	* ~1_PLAYER_NAME~ begins to spasm uncontrollably. *
	}
}


