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
	//represents the activated ignition ability
	public partial class IgnitionActivatedPlugin {
		private static ActivableAbilityDef a_ignition;
		private static ActivableAbilityDef IgnitionDef {
			get {
				if (a_ignition == null) {
					a_ignition = (ActivableAbilityDef) AbilityDef.GetByDefname("a_ignition");
				}
				return a_ignition;
			}
		}

		static PluginKey pkIgnitionEffect = PluginKey.Acquire("_ignition_effect_");

		public void On_CauseSpellEffect(SpellEffectArgs spellEffectArgs) {
			DamageSpellDef damageSpell = spellEffectArgs.SpellDef as DamageSpellDef;
			if (damageSpell != null) {
				if ((damageSpell.DamageType & DamageType.Fire) == DamageType.Fire) {
					Character target = spellEffectArgs.CurrentTarget as Character;
					if (target != null) {
						ActivableAbilityDef def = IgnitionDef;

						int points = spellEffectArgs.Caster.GetAbility(def);
						if (points > 0) {
							if (def.CheckSuccess(points)) {								

								double power = points * def.EffectPower;
								power *= DamageManager.GetResistModifier(target, DamageType.MagicFire); //apply fire resist

								IgnitionEffectPlugin ignitionPlugin = target.GetPlugin(pkIgnitionEffect) as IgnitionEffectPlugin;
								if (ignitionPlugin != null) {
									power += Math.Max(0, ignitionPlugin.EffectPower); //we add the power of previous ignition instance, if any
								}

								if (power > IgnitionEffectPlugin.minimumIgnitionEffect) { //else it does nothing to this target, so it's effectively immune
									ignitionPlugin = (IgnitionEffectPlugin) IgnitionEffectPluginDef.instance.Create();
									ignitionPlugin.Init(spellEffectArgs.Caster, EffectFlag.FromAbility | EffectFlag.HarmfulEffect,
										power, TimeSpan.FromSeconds(def.EffectDuration));
									target.AddPlugin(pkIgnitionEffect, ignitionPlugin);
								}
							}
						}
					}
				}
			}
		}
	}
}