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
using SteamEngine.Networking;
using SteamEngine.Regions;
using SteamEngine.CompiledScripts;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	[ViewableClass]
	public class PoisonSpellDef : DurableCharEffectSpellDef {

		public static readonly TimeSpan poisonTickInterval = TimeSpan.FromSeconds(5); //or should be settable?

		public PoisonSpellDef(String defname, String filename, Int32 headerLine)
			: base(defname, filename, headerLine) {
		}

		protected override void On_EffectChar(Character target, SpellEffectArgs spellEffectArgs) {
			Character caster = spellEffectArgs.Caster;
			target.Trigger_HostileAction(caster);

			int spellPower = spellEffectArgs.SpellPower;
			double effect = (int) this.GetEffectForValue(spellPower);
			double durationInSeconds = this.GetDurationForValue(spellPower);
			int ticksCount = (int) (durationInSeconds / poisonTickInterval.TotalSeconds);

			((PoisonEffectPluginDef) this.EffectPluginDef).Apply(caster, target, spellEffectArgs.EffectFlag, 
				effect, ticksCount);
		}
	}
}