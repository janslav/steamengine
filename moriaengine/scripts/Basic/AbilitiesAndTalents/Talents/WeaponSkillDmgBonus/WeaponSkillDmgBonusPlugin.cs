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
using SteamEngine.Timers;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {
	[Dialogs.ViewableClass]
	public partial class WeaponSkillDmgBonusPlugin {

		public void On_BeforeSwing(WeaponSwingArgs swingArgs) {
			var attackerSkill = swingArgs.attacker.CurrentSkill as WeaponSkillDef;
			if (attackerSkill == this.TypeDef.Skill) {
				swingArgs.DamageAfterAC *= (1 + this.EffectPower);
			}
		}
	}

	[Dialogs.ViewableClass]
	public partial class WeaponSkillDmgBonusPluginDef {
	}
}