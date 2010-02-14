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
	//the similarity to Poison is not random :)
	public partial class ManaSavingPlugin {

		private static ActivableAbilityDef a_mana_saving;
		private static ActivableAbilityDef ManaSavingDef {
			get {
				if (a_mana_saving == null) {
					a_mana_saving = (ActivableAbilityDef) AbilityDef.GetByDefname("a_mana_saving");
				}
				return a_mana_saving;
			}
		}

		public void On_SkillSuccess(SkillSequenceArgs skillSeqArgs) {
			if (skillSeqArgs.SkillDef.Id == (int) SkillName.Magery) {
				Character self = (Character) this.Cont;
				ActivableAbilityDef abilityDef = ManaSavingDef;
				int aPoints = self.GetAbility(abilityDef);

				if (abilityDef.CheckSuccess(aPoints)) {
					if (self.Mana < self.MaxMana) {
						SpellDef spell = (SpellDef) skillSeqArgs.Param1;
						double manause = spell.GetManaUse(skillSeqArgs.Tool is SpellScroll);
						int giveback = (int) Math.Round(manause * abilityDef.EffectPower * aPoints);
					
						self.Mana = (short) Math.Min(self.Mana + giveback, self.MaxMana);
					}
				}
			}
		}
	}
}