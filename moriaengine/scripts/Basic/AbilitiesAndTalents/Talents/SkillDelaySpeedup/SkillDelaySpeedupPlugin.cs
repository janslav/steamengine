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

namespace SteamEngine.CompiledScripts {
	[Dialogs.ViewableClass]
	public partial class SkillDelaySpeedupPlugin {
		public TriggerResult On_SkillStart(SkillSequenceArgs ssa) {
			if (ssa.SkillDef == this.TypeDef.Skill) {
				ssa.DelayInSeconds *= (1 - this.EffectPower);
			}
			return TriggerResult.Continue;
		}

		//public void On_ActivateAbility(AbilityDef ad, Ability a) {
		//    if (ad == this.TypeDef.Ability) {
		//        //move the lastusage time backwards
		//        a.LastUsage = Globals.TimeAsSpan - TimeSpan.FromSeconds(ad.Cooldown * this.EffectPower);
		//    }
		//}
	}

	[Dialogs.ViewableClass]
	public partial class SkillDelaySpeedupPluginDef {
	}
}
