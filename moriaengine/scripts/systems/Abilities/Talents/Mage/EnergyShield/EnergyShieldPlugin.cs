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
	public partial class EnergyShieldPlugin {
		private static ActivableAbilityDef a_energy_shield;
		private static ActivableAbilityDef EnergyShieldDef {
			get {
				if (a_energy_shield == null) {
					a_energy_shield = (ActivableAbilityDef) AbilityDef.GetByDefname("a_energy_shield");
				}
				return a_energy_shield;
			}
		}

		public bool On_Damage(DamageArgs args) {
			if (!args.attacker.IsPlayer) {
				this.EffectPower -= args.Damage;
				if (this.EffectPower > 0) {
					return true; //cancel damage effects
				} else if (this.EffectPower == 0) {
					this.Delete();
					return true; //cancel damage effects
				} else {
					args.Damage = -this.EffectPower;
					this.Delete();					
				}
			}
			return false; //do not cancel damage effects
		}

		public bool On_SelectSkill(SkillSequenceArgs skillSeq) {
			return CheckAllowedSpells(skillSeq);
		}

		public bool On_StartSkill(SkillSequenceArgs skillSeq) {
			return CheckAllowedSpells(skillSeq);
		}

		private static bool CheckAllowedSpells(SkillSequenceArgs skillSeq) {
			if (skillSeq.SkillDef.Id == (int) SkillName.Magery) {
				SpellDef spell = (SpellDef) skillSeq.Param1;
				switch (spell.Id) {
					case 22: //teleport
					case 32: //recall
					case 52: //gate
						Character self = skillSeq.Self;
						self.WriteLine(Loc<EnergyShieldLoc>.Get(self.Language).cantTeleport);
						return true; //cancel
				}
			}
			return false;
		}

		public bool On_Step(Direction direction, bool running) {
			Character self = (Character) this.Cont;
			self.WriteLine(Loc<EnergyShieldLoc>.Get(self.Language).cantMove);
			return true;
		}
	}

	public class EnergyShieldLoc : CompiledLocStringCollection {
		public string cantTeleport = "Bìhem trvání kouzla Energy shield nemùžeš používat teleportaèní kouzla";
		public string cantMove = "Bìhem trvání kouzla Energy shield se nemùžeš pohybovat";
	}
}