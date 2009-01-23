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
	[Summary("Plugin managing all characters regenerations, on timer periodically checks all regens" +
			"and adds correct number of points to be regenerated")]
	public partial class RegenerationPlugin {
		//this is to initialize (create the instance) of the Regen.Plug.Def. it must be here as bellow or 
		//somewhere in the LScript as simple [RegenerationPluginDef p_regenerations]
		//if the instance is not pre-created in either way it will crash during attempt to assign this plugin...
		public static readonly RegenerationPluginDef defInstance = new RegenerationPluginDef("p_regenerations", "C#scripts", -1);
		internal static PluginKey regenerationsPluginKey = PluginKey.Get("_regenerations_");

		private double lastServerTime; //last time the holder obtained some stats by regen

		internal const double MIN_TIMER = 1.0d; //minimal timer usable
		private const double ALLOWED_TIMER_DIFF = 1.5d; //allowed difference between the ideal and counted mean timer
		private const double MAX_TIMER = 1.0d; //maximal timer usable

		public void On_Assign() {
			lastServerTime = Globals.TimeInSeconds; //set the first time to be used for regeneration
			Timer = MIN_TIMER; //set the basic timer for the first regen round
		}

		[Summary("Periodically check stats and regenerate computed amount of points (if any)")]
		public void On_Timer() {
			Character holder = (Character) this.Cont;
			//fields set once everytime the On_Timer method gets fired
			double hitsRegenSpeed = holder.HitsRegenSpeed;
			double stamRegenSpeed = holder.StamRegenSpeed;
			double manaRegenSpeed = holder.ManaRegenSpeed;
			int hits = holder.Hits;
			int stam = holder.Stam;
			int mana = holder.Mana;
			int maxHits = holder.MaxHits;
			int maxStam = holder.MaxStam;
			int maxMana = holder.MaxMana;

			double timeElapsed = Globals.TimeInSeconds - lastServerTime;

			//count the number of modified stats points (if any!)
			int hitsChange = CheckStatChange(hitsRegenSpeed, hits, maxHits, timeElapsed, ref residuumHits);
			int stamChange = CheckStatChange(stamRegenSpeed, stam, maxStam, timeElapsed, ref residuumStam);
			int manaChange = CheckStatChange(manaRegenSpeed, mana, maxMana, timeElapsed, ref residuumMana);

			if ((hitsChange == 0) && (stamChange == 0) && (manaChange == 0)) {
				//delete the plugin for now. nothing is modified. it will be renewed when hits/mana/stamina lowers
				//or when the regenerations get some point...
				Delete();
				return;
			}

			//now count the ideal timer for the next round (ideal means that there would be an integer change 
			//immediately without any residuum next round)
			double usedTimer;
			if ((hitsChange == 0) || (stamChange == 0) || (manaChange == 0)) { // some stat is unmodified, use the fastest regeneration
				double fastestRegen = Math.Max(hitsRegenSpeed, Math.Max(stamRegenSpeed, manaRegenSpeed));
				double fastestStatsResiduum = ((fastestRegen == hitsRegenSpeed) ? residuumHits : //fastest are hits - use them
												((fastestRegen == stamRegenSpeed) ? residuumStam : //fastest is stamina - use it
													manaRegenSpeed)); //use mana  
				//count the timer for the stat with the fastest regen speed
				usedTimer = CountIdealTimer(fastestRegen, fastestStatsResiduum);
			} else { //count the ideal timer for the next round
				//we are using the newly counted residuum here (see CheckStatChange) method...
				double hitsIdealTimer = CountIdealTimer(hitsRegenSpeed, residuumHits);
				double stamIdealTimer = CountIdealTimer(stamRegenSpeed, residuumStam);
				double manaIdealTimer = CountIdealTimer(manaRegenSpeed, residuumMana);
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
			//modify stats
			holder.Hits += (short) hitsChange;
			holder.Stam += (short) stamChange;
			holder.Mana += (short) manaChange;

			this.Timer = usedTimer; //use the count timer
			lastServerTime = Globals.TimeInSeconds; //remember the last usage
		}

		private int CheckStatChange(double regenSpeed, int stat, int maxStat, double timeElapsed, ref double residuumStat) {
			int statChange = 0;
			//when does the stat get modified?
			if ((regenSpeed < 0 && (stat > 0)) ||  //negative regeneration
					(regenSpeed > 0 && (stat < maxStat))) { //positive regeneration
				int countedChange = CountStatChange(regenSpeed, ref residuumStat, timeElapsed);
				//do not overgo the maxhits or undergo the 0
				if (countedChange < 0) {
					//we are substracting - do not go below zero!
					statChange = Math.Max(-stat, countedChange);
				} else {
					statChange = Math.Min(maxStat - stat, countedChange);
				}
			} else {
				residuumStat = 0.0; //nothing should be left for the next round!
			}
			return statChange;
		}

		[Summary("Count and return the ideal timer for the given regenSpeed - this means " +
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


		[Summary("From the regeneration speed (statpoints/sec), elapsed time and the residuum from the last round " +
				 "count the integer value to be added (substracted) to the stat, remember the new residuum for the next round")]
		private int CountStatChange(double regenSpeed, ref double lastResiduum, double timeElapsed) {
			//the number of regenerated points (x) is as follows: 
			//x = (lastResiduum) + (regenSpeed * timer);
			double absoluteChange = lastResiduum + (regenSpeed * timeElapsed);
			int retVal = (int)absoluteChange; //the stat value added - truncated
			lastResiduum = absoluteChange - retVal; //this is the new residuum for the next round

			return retVal; //it is already truncated, the cast is OK
		}

		[Summary("Check if character can have this plugin and if true, add it")]
		public static void TryInstallPlugin(Character futureCont, int stat, int maxStat, double regenSpeed) {
			//check if adept pluginholder is not dead
			if (!futureCont.Flag_Dead) {
				//check if he doesn't have the plugin already
				if (!futureCont.HasPlugin(regenerationsPluginKey)) {
					//check if the stat can be regenerated
					if ((regenSpeed < 0 && (stat > 0)) ||  //negative regeneration
							(regenSpeed > 0 && (stat < maxStat))) { //positive regeneration
						futureCont.AddNewPlugin(regenerationsPluginKey, SingletonScript<RegenerationPluginDef>.Instance);
					}
				}
			}
		}
	}
}
