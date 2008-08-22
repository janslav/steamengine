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
	[ViewableClass]
	[Summary("Plugin managing all characters regenerations, on timer periodically checks all regens"+
			"and adds correct number of points to be regenerated")]
	public partial class RegenerationPlugin {		
		public static readonly RegenerationPluginDef defInstance = new RegenerationPluginDef("p_regenerations", "C#scripts", -1);
		internal static PluginKey regenerationsPluginKey = PluginKey.Get("_regenerations_");

        //fields holding the rest of points that should be regenereated in the next regen round
        private double residuumHits, residuumStam, residuumMana;
        //fields holding the number of regenerated stat points per second
        private double holderHitsRegenSpeed, holderStamRegenSpeed, holderManaRegenSpeed;

        private const double MIN_TIMER = 1.0d; //minimal timer usable
        private const double ALLOWED_TIMER_DIFF = 1.5d; //allowed difference between the ideal and counted mean timer
        private const double DEFAULT_TIMER = 2d; //timer used when no regenerations are applied at all
        private const double MAX_TIMER = 1.0d; //maximal timer usable

        private double lastServerTime; //last time the holder obtained some stats by regen

		//periodically checking and regenerating
		public void On_Timer() {            
            if (!ModifyAnything()) {
                //use the default timer and do nothing else
                this.Timer = DEFAULT_TIMER;
                lastServerTime = Globals.TimeInSeconds;
                return;
            }

            double timeElapsed = Globals.TimeInSeconds - lastServerTime;
            Character holder = (Character)this.Cont;            
            
            bool modifyAllStats = ModifyAllStats();//first check if we will modify all three stats
            
            int hitsChange = CountStatChange(holderHitsRegenSpeed, ref residuumHits, timeElapsed);
            int stamChange = CountStatChange(holderStamRegenSpeed, ref residuumStam, timeElapsed);
            int manaChange = CountStatChange(holderManaRegenSpeed, ref residuumMana, timeElapsed);
            
            //now count the ideal timer for the next round (ideal means that there would be an integer change 
            //immediately without any residuum next round
            double usedTimer = 0.0d;
            if (!modifyAllStats) { // use the fastest regeneration
                double fastestRegen = Math.Max(holderHitsRegenSpeed, Math.Max(holderStamRegenSpeed, holderManaRegenSpeed));
                double fastestStatsResiduum = ((fastestRegen == holderHitsRegenSpeed) ? residuumHits : //fastest are hits - use them
                                                ((fastestRegen == holderStamRegenSpeed) ? residuumStam : //fastest is stamine - use it
                                                    residuumMana)); //use mana  
                //count the timer for the stat with the fastest regen speed
                usedTimer = CountIdealTimer(fastestRegen, fastestStatsResiduum);
            } else { //count the ideal timer
                double hitsIdealTimer = CountIdealTimer(holderHitsRegenSpeed, residuumHits);
                double stamIdealTimer = CountIdealTimer(holderStamRegenSpeed, residuumStam);
                double manaIdealTimer = CountIdealTimer(holderManaRegenSpeed, residuumMana);
                double midTimer = Utility.ArithmeticMean(hitsIdealTimer, stamIdealTimer, manaIdealTimer);
                double hitsTmrDiff = Math.Abs(hitsIdealTimer - midTimer);
                double manaTmrDiff = Math.Abs(stamIdealTimer - midTimer);
                double stamTmrDiff = Math.Abs(manaIdealTimer - midTimer);

                usedTimer = midTimer; //defaultly we want to use the mean value of the count timers
                if (Math.Max(stamTmrDiff, Math.Max(hitsTmrDiff, manaTmrDiff)) > ALLOWED_TIMER_DIFF) {
                    //longest ideal timer exceeded the allowed difference from the mid value - we will use the shortest timer
                    usedTimer = Math.Min(stamTmrDiff, Math.Min(hitsTmrDiff, manaTmrDiff));
                }
            }
            this.Timer = usedTimer; //use the count timer
            lastServerTime = Globals.TimeInSeconds; //remember the last usage
		}

        [Summary("Method for re-reading all regeneration points and storing them in the private fields "+
                "the purpose if this method is to spare time reading the points every timer round "+
                "- it will be run only when the points change on any of the regenerating abilities")]
        internal void RefreshRegenPoints() {
            Character holder = (Character)this.Cont;
            ushort regenSpeed = SingletonScript<RegenerationDef>.Instance.RegenerationSpeed;
            holderHitsRegenSpeed = holder.GetAbility(SingletonScript<HitsRegenDef>.Instance) / regenSpeed;
            holderStamRegenSpeed = holder.GetAbility(SingletonScript<StaminaRegenDef>.Instance) / regenSpeed;
            holderManaRegenSpeed = holder.GetAbility(SingletonScript<ManaRegenDef>.Instance) / regenSpeed;
        }

        //check if we are to modify all three stats or just one or two
        private bool ModifyAllStats() {
            Character holder = (Character)this.Cont;
            if (holder.Hits == holder.MaxHits || holder.Mana == holder.MaxMana || holder.Stam == holder.MaxStam) {
                //some stat is on its maximum
                return false;
            }
            if (!isHitsRegen || !isManaRegen || !isStamRegen || //not regenerating at all
                holderHitsRegenSpeed == 0 || holderStamRegenSpeed == 0 || holderManaRegenSpeed == 0) { //or some regen is 0
                return false;
            }
            return true;
        }

        private bool ModifyAnything() {
            Character holder = (Character)this.Cont;
            //either all stats are full
            if (holder.Hits == holder.MaxHits && holder.Mana == holder.MaxMana && holder.Stam == holder.MaxStam) {
                return false;
            }
            //or all abilities are zeroized
            if (holderHitsRegenSpeed == 0 && holderStamRegenSpeed == 0 && holderManaRegenSpeed == 0) {
                return false;
            }
            return true;
        }

        [Summary("Count and return the ideal timer for the given regenSpeed - this means "+
                "number of seconds after which there will be an integer regeneration (de/in)crease")]
        private double CountIdealTimer(double regenSpeed, double currentResiduum) {
            //the number of regenerated points (x) is as follows: 
            //x = (lastResiduum) + (regenSpeed * timer);
            //we want the timer to be greater than MIN_TIMER and the number of regenerated points
            //to be integer            
            double retTmr = 0.0d;
            int x = 1; //we expect to gain at least 1 point ideally :-)
            while (retTmr < MIN_TIMER) {
                retTmr = (x - currentResiduum) / regenSpeed;
               x++; 
            }
            return retTmr;
        }

        
        [Summary("From the regeneration speed (stapoints/sec), elapsed time and the residuum from the last round "+
                 "count the integer value to be added (substracted) to the stat, remember the new residuum for the next round")]        
        private int CountStatChange(double regenSpeed, ref double lastResiduum, double timeElapsed) {
            //the number of regenerated points (x) is as follows: 
            //x = (lastResiduum) + (regenSpeed * timer);
            double absoluteChange = lastResiduum + (regenSpeed * timeElapsed);
            int retVal = (int)Math.Truncate(absoluteChange); //the stat value added
            lastResiduum = absoluteChange - retVal; //this is the new residuum for the next round

            return retVal;
        }
	}
}
