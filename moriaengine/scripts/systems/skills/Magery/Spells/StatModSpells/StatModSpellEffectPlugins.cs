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

	public static class StatModSpellsUtils {
		public const int minStat = 10;

		static StatModSpellsUtils() {
			new IntModSpellEffectPluginDef("p_intModSpellEffect", "C# scripts", -1);
		}

		public static void Bootstrap() { //ensure calling the static initialiser
		}

		public static short ModifyStat(Character ch, short statValue, short statDiff, out short resultDiff) {
			if (statDiff < 0) {
				short retVal = (short) (statValue + statDiff);
				if (retVal < minStat) { //this would decrease the stat under the boundary
					if (statValue < minStat) { //the stat is already under the boundary, we leave it as is
						resultDiff = 0;
						return statValue;
					} else {
						resultDiff = (short) (minStat - statValue);
						return minStat;
					}
				} else {
					resultDiff = statDiff;
					return retVal;
				}
			} else { //positive change has no boundary
				resultDiff = statDiff;
				return (short) (statValue + statDiff);
			}
		}
	}

	[ViewableClass]
	public partial class IntModSpellEffectPlugin {
		public void On_Assign() {
			Character self = (Character) this.Cont;
			self.Int = StatModSpellsUtils.ModifyStat(self, self.Int, (short) this.Effect, out this.intDifference);
		}

		public void On_UnAssign(Character cont) {
			cont.Int -= this.intDifference;
		}
	}
}