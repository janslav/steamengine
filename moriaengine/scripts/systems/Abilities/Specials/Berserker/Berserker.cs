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
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	[Summary("War's special - berserker")]
	[ViewableClass]
	public class BerserkerDef : ActivableAbilityDef {
		private FieldValue damageModifier;

		public BerserkerDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			damageModifier = InitField_Typed("damageModifier", 1.0, typeof(double));
		}

		#region triggerMethods
		protected override bool On_DenyUse(DenyAbilityArgs args) {
			bool retVal = false;
			//TODO - zde ještì implementovat to jestli dotyènej žije/nežije atd.

			retVal = base.On_DenyUse(args); //call superclass for common checks - including resources consuming etc
			return retVal;
		}
		#endregion triggerMethods

		[InfoField("Dmg modifier")]
		[Summary("Coefficient of the damage on hit")]
		public double DamageModifier {
			get {
				return (double) damageModifier.CurrentValue;
			}
			set {
				damageModifier.CurrentValue = value;
			}
		}		

		[SteamFunction("Berserker")]
		[Summary("Switching on the berserker ability (if the player has the ability)")]
		public static void BerserkerFunction(Character chr, ScriptArgs args) {
			BerserkerDef bskrDef = SingletonScript<BerserkerDef>.Instance;
			bskrDef.ActivateOrUnactivate(chr);
		}
	}

	[ViewableClass]
	public partial class BerserkerPlugin {
		//public static readonly BerserkerPluginDef defInstance = new BerserkerPluginDef("p_berserker", "C#scripts", -1);
		
		public void On_AfterSwing(WeaponSwingArgs args) {
			Character attacker = args.attacker;
			if (attacker.CurrentSkillName != SkillName.Archery) { //archery is unmodified (we have sniper ability for rangers)
				//if (attacker.Stam >= staminaConsumed) {
				//	attacker.SysMessage("Kritický zásah!");
					//taken from the moria berserker script
				//	args.FinalDamage = args.FinalDamage * damageModifier;
				//}
			}
		}
	}
}
