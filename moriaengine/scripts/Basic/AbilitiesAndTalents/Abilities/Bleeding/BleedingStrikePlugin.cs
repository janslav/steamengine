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

namespace SteamEngine.CompiledScripts {
	[Dialogs.ViewableClass]
	//represents the activated bleeding strike ability
	public partial class BleedingStrikePlugin {
		private static ActivableAbilityDef a_bleeding_strike;
		private static ActivableAbilityDef BleedingStrikeDef {
			get {
				if (a_bleeding_strike == null) {
					a_bleeding_strike = (ActivableAbilityDef) AbilityDef.GetByDefname("a_bleeding_strike");
				}
				return a_bleeding_strike;
			}
		}

		//public void On_Assign() {
		//    //apply effect of the bonus talent, if present
		//    Character self = (Character) this.Cont;
		//    this.EffectPower *= 1 + (self.GetAbility(IgnitionBonusDef) * IgnitionBonusDef.EffectPower);
		//    base.On_Assign();
		//}

		public void On_AfterSwing(WeaponSwingArgs swingArgs) {
			if (swingArgs.FinalDamage > 0) {
				BleedingEffectPluginDef bleedingDef = SingletonScript<BleedingEffectPluginDef>.Instance;
				Character attacker = swingArgs.attacker;

				double bleedDamage = swingArgs.FinalDamage * attacker.Weapon.BleedingEfficiency * BleedingStrikeDef.EffectPower;
				double durationInSeconds = BleedingStrikeDef.EffectDuration;
				int ticksCount = (int) (durationInSeconds / bleedingDef.TickInterval);

				bleedingDef.Apply(attacker, swingArgs.defender, EffectFlag.HarmfulEffect | EffectFlag.FromAbility,
					bleedDamage, ticksCount);
			}
		}
	}

	[Dialogs.ViewableClass]
	public partial class BleedingStrikePluginDef {
	}
}