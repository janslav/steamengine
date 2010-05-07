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
	public partial class PoisonEffectPlugin {
		public const double minimumPoisonEffect = 0.1;

		static TimerKey tickTimerKey = TimerKey.Acquire("_poisonTickTimer_");

		public override void Init(Thing sourceThing, EffectFlag sourceType, double power, TimeSpan duration, AbstractDef sourceDef) {
			base.Init(sourceThing, sourceType, power, duration, sourceDef);

			double tickSpan = this.TypeDef.TickInterval;
			double tickCount = duration.TotalSeconds / tickSpan;
			double differencePerTick = this.EffectPower / tickCount;
			this.AddTimer(tickTimerKey, new PoisonTickTimer(TimeSpan.FromSeconds(tickSpan), differencePerTick));
		}

		public virtual void On_Assign() {
			Character self = (Character) this.Cont;
			self.AddPoisonCounter();
		}

		public virtual void On_PoisonTick() {
			throw new NotImplementedException();
		}

		public override void On_UnAssign(Character cont) {
			PoisonSpellDef poisonSpell = SingletonScript<PoisonSpellDef>.Instance;
			cont.RemovePoisonCounter();
			base.On_UnAssign(cont);
		}

		public void AnnouncePoisonStrength(PoisonStrengthMessage strength) {
			Character self = this.Cont as Character;
			if (self != null) {
				int roundedEffect = (int) strength;
				if (roundedEffect < 0) {
					roundedEffect = 0;
				} else if (roundedEffect > 4) {
					roundedEffect = 4;
				}

				self.ClilocEmote(1042858 + (roundedEffect * 2), 0x21, self.Name); //shouldn't see one's own cliloc emote?
				self.ClilocSysMessage(1042857 + (roundedEffect * 2), 0x21);
			}
		}
	}

	//the names aren;t really important, the relevant part here is that it's 0-4
	public enum PoisonStrengthMessage {
		Zero = 0, Weakest = 0, Nauseous = 0, Ill = 0,
		One = 1, Weak = 1, DisorientedAndNauseous = 1, ExtremelyIll = 1,
		Two = 2, Normal = 2, FeelingPain = 2, StumblesAround = 2,
		Three = 3, Strong = 3, ExtremelyWeak = 3, WrackedWithPain = 3,
		Four = 4, Strongest = 4, ExtremePain = 4, SpasmingUncontrollably = 4
	}

	[Dialogs.ViewableClass]
	public partial class PoisonEffectPluginDef {

		public void Apply(Thing source, Character target, EffectFlag sourceType, double poisonPower, int tickCount) {
			//every poison type has it's pluginkey, so they're independent on each other

			double tickSeconds = this.TickInterval;
			double duration = tickSeconds * tickCount;

			PluginKey key = this.PluginKey;
			PoisonEffectPlugin previous = target.GetPlugin(key) as PoisonEffectPlugin;
			if (previous != null) {
				if ((previous.Def == this)) {
					//previous poison is of the same type, we sum them up to the max values and apply to the old one

					double maxDuration = this.MaxTicks * tickSeconds;
					double newDuration = Math.Min(previous.Timer + duration, maxDuration);
					double newPower = Math.Min(previous.EffectPower + poisonPower, this.MaxPower);

					//reinit with new duration and effect. Hope it doesn't break anything :D
					//also we change it's source, so that the new attacker is responsible now
					previous.Init(source, sourceType, newPower, TimeSpan.FromSeconds(newDuration));

					return;
				}
			}

			PoisonEffectPlugin effect = (PoisonEffectPlugin) this.Create();
			effect.Init(source, sourceType, poisonPower,
				TimeSpan.FromSeconds(duration));
			target.AddPlugin(key, effect);
		}
	}

	[SaveableClass, DeepCopyableClass]
	public class PoisonTickTimer : BoundTimer {

		[SaveableData, CopyableData]
		public TimeSpan tickSpan;
		[SaveableData, CopyableData]
		public double differencePerTick;

		[DeepCopyImplementation, LoadingInitializer]
		public PoisonTickTimer() {
		}

		public PoisonTickTimer(TimeSpan tickSpan, double differencePerTick) {
			this.DueInSpan = tickSpan;
			this.PeriodSpan = tickSpan;

			this.differencePerTick = differencePerTick;
		}

		protected override void OnTimeout(TagHolder cont) {
			PoisonEffectPlugin poison = (PoisonEffectPlugin) cont;

			poison.On_PoisonTick();
			poison.EffectPower -= this.differencePerTick;
		}
	}
}


