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
	public partial class ManaShieldPlugin {

		//private static DurableCharEffectSpellDef s_mana_shield;
		//private static DurableCharEffectSpellDef ManaShieldDef {
		//    get {
		//        if (s_mana_shield == null) {
		//            s_mana_shield = (DurableCharEffectSpellDef) SpellDef.GetByDefname("s_mana_shield");
		//        }
		//        return s_mana_shield;
		//    }
		//}

		public void On_Damage(DamageArgs args) {
			Character self = (Character) this.Cont;

			int damage = (int) Math.Round(args.damage);
			int newMana = self.Mana - damage;

			if (newMana > 0) {
				//do no damage at all
				self.Hits += (short) damage;
				self.WriteLine(Loc<ManaShieldLoc>.Get(self.Language).shieldSavedYou);				
			} else {
				//do some damage still (damage + newMana, but the newMana part is negative...)
				self.Hits += (short) (damage + newMana);
				self.WriteLine(Loc<ManaShieldLoc>.Get(self.Language).shieldSavedYouPartially);
				this.Delete();
			}			
		}

		//interrupt on meditation attempt
		public bool On_SelectSkill(SkillSequenceArgs skillSeq) {
			return CheckStartingSkill(skillSeq);
		}

		public bool On_StartSkill(SkillSequenceArgs skillSeq) {
			return CheckStartingSkill(skillSeq);
		}

		private bool CheckStartingSkill(SkillSequenceArgs skillSeq) {
			if (skillSeq.SkillDef.Id == (int) SkillName.Meditation) {
				Character self = (Character) this.Cont;
				self.WriteLine(Loc<ManaShieldLoc>.Get(self.Language).cantMeditateWithManaShield);
				this.Delete();
			}
			return false;
		}

		public override void On_UnAssign(Character cont) {
			//lower his mana if it's not already lowered. It's kind of weird but I got it verbatim from sphere script :o
			int maxMana = cont.MaxMana;
			if (cont.Mana > maxMana / 2) {
				cont.Mana -= (short) (maxMana / 3);
			} else {
				cont.Mana = 0;
			}

			base.On_UnAssign(cont);
		}
	}

	[Dialogs.ViewableClass]
	public partial class ManaShieldPluginDef {
	}

	public class ManaShieldLoc : CompiledLocStringCollection {
		public string cantMeditateWithManaShield = "Soust�edit se z�rove� na meditaci i mana shield nen� v lidsk�ch sil�ch.";
		public string shieldSavedYou = "Mana shield t� uchr�nil p�ed zran�n�m";
		public string shieldSavedYouPartially = "Mana shield t� ��ste�n� ochr�nil p�ed zran�n�m";
	}
}