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
using SteamEngine.Timers;
using SteamEngine.Persistence;
using SteamEngine.Networking;

namespace SteamEngine.CompiledScripts {


	[Dialogs.ViewableClass]
	public partial class DamagingPoisonEffectPlugin {
		public const double minimumPoisonEffect = 0.1; //plugin gets removed when the regen modifier goes below this


		static TimerKey tickTimerKey = TimerKey.Acquire("_poisonTickTimer_");

		public void On_Assign() {
			Character self = (Character) this.Cont;

			double effect = this.EffectPower;
			this.AddTimer(tickTimerKey, new PoisonTickTimer(this.TimerObject.DueInSpan, effect));
			self.HitsRegenSpeed -= effect;
			self.Flag_GreenHealthBar = true;
		}

		public override void On_UnAssign(Character cont) {
			cont.HitsRegenSpeed += this.EffectPower;

			PoisonSpellDef poisonSpell = SingletonScript<PoisonSpellDef>.Instance;
			cont.Flag_GreenHealthBar = //
				cont.HasPlugin(poisonSpell.EffectPluginKey_Potion) || cont.HasPlugin(poisonSpell.EffectPluginKey_Spell);
			base.On_UnAssign(cont);
		}

		public void ModifyEffect(double difference) {
			Character self = (Character) this.Cont;
			if (self != null) {
				double newEffect = this.EffectPower + difference;
				if (newEffect < minimumPoisonEffect) {
					this.Delete();
				} else {
					self.HitsRegenSpeed -= difference;
					this.EffectPower = newEffect;
				}
			}
		}

		public void AnnouncePoisonStrength() {
			Character self = this.Cont as Character;
			if (self != null) {
				int roundedEffect = (int) this.EffectPower;
				if (roundedEffect < 0) {
					roundedEffect = 0;
				} else if (roundedEffect > 4) {
					roundedEffect = 4;
				}

				self.ClilocEmote(1042858 + (roundedEffect * 2), 0x21, self.Name); //shouldn't see one's own cliloc emote?
				self.ClilocSysMessage(1042857 + (roundedEffect * 2), 0x21);
			}
		}

		//1042857	*You feel a bit nauseous*
		//1042858	*~1_PLAYER_NAME~ looks ill.*
		//1042859	* You feel disoriented and nauseous! *
		//1042860	* ~1_PLAYER_NAME~ looks extremely ill. *
		//1042861	* You begin to feel pain throughout your body! *
		//1042862	* ~1_PLAYER_NAME~ stumbles around in confusion and pain. *
		//1042863	* You feel extremely weak and are in severe pain! *
		//1042864	* ~1_PLAYER_NAME~ is wracked with extreme pain. *
		//1042865	* You are in extreme pain, and require immediate aid! *
		//1042866	* ~1_PLAYER_NAME~ begins to spasm uncontrollably. *
	}

	//[SaveableClass, DeepCopyableClass]
	//public class PoisonTickTimer : BoundTimer {
	//    private static TimeSpan tickSpan = TimeSpan.FromSeconds(5);

	//    [SaveableData, CopyableData]
	//    public double differencePerTick;

	//    [DeepCopyImplementation, LoadingInitializer]
	//    public PoisonTickTimer() {
	//    }

	//    public PoisonTickTimer(TimeSpan totalTime, double totalEffect) {
	//        this.DueInSpan = tickSpan;
	//        this.PeriodSpan = tickSpan;

	//        this.differencePerTick = -((tickSpan.Ticks * totalEffect) / totalTime.Ticks);
	//    }

	//    protected override void OnTimeout(TagHolder cont) {
	//        PoisonEffectPlugin poison = (PoisonEffectPlugin) cont;
	//        poison.ModifyEffect(differencePerTick);
	//        poison.AnnouncePoisonStrength();
	//    }
	//}
}


