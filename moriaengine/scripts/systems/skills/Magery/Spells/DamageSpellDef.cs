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
using System.Text.RegularExpressions;
using SteamEngine.Common;
using SteamEngine.Regions;
using SteamEngine.CompiledScripts;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	[ViewableClass]
	public class DamageSpellDef : SpellDef {

		#region FieldValues
		private FieldValue damageType;

		public DamageSpellDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {

			this.damageType = this.InitField_Typed("damageType", DamageType.Magic, typeof(DamageType));
		}

		public DamageType DamageType {
			get {
				return (DamageType) this.damageType.CurrentValue;
			}
			set {
				this.damageType.CurrentValue = value;
			}
		}
		#endregion FieldValues


		public override void On_EffectChar(Character target, SpellEffectArgs spellEffectArgs) {
			if (target != spellEffectArgs.MainTarget) { //if not the main target, we only want to hit the bad guys (applies ofc to mass versions of this spells only)
				CharRelation relation = Notoriety.GetCharRelation(target, spellEffectArgs.Caster);
				if (relation > spellEffectArgs.CasterToMainTargetRelation) { //if the relation is actually better, we do not proceed with damage
					return;
				}
			}

			double dam = this.GetEffectForValue(spellEffectArgs.SpellPower);
			DamageManager.CauseDamage(spellEffectArgs.Caster, target, this.DamageType, dam);
		}
	}
}