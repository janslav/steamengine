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

	[Summary("Special ability class used for implementing abilities that will be run using SteamFunction." +
			"Their effect will be immediate (e.g. warcry)")]
	[ViewableClass]
	public class ImmediateAbilityDef : AbilityDef {
		public static readonly TriggerKey tkFire = TriggerKey.Get("Fire");
		
		public ImmediateAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {			
		}

		[Summary("Implementation of the activating method. Used for running the ability")]
		public override void Activate(Character chr) {
			Ability ab = chr.GetAbility(this);			
			DenyResultAbilities retVal = 0;
			if(ab == null) {//"0th" common check - do we have the ability?
				retVal = DenyResultAbilities.Deny_DoesntHaveAbility;
			} else {
				DenyAbilityArgs args = new DenyAbilityArgs(chr, this, ab);
				bool cancel = Trigger_DenyUse(args); //return value means only that the trigger has been cancelled
				retVal = args.Result;//this value contains the info if we can or cannot run the ability
			}			
			if(retVal == DenyResultAbilities.Allow) {
				bool cancel = Trigger_Fire(chr);
				ab.LastUsage = Globals.TimeInSeconds; //set the last usage time
			} 
			SendAbilityResultMessage(chr,retVal); //send result(message) of the "activate" call to the client
		}

		#region triggerMethods
		[Summary("C# based @fire trigger method")]
		protected virtual bool On_Fire(Character chr) {
			chr.SysMessage("Abilita " + Name + " nem� implementaci trigger metody On_Fire");
			return false; //no cancelling
		}

		[Summary("LScript based @fire triggers"+
				"Gets called when every prerequisity has been fulfilled and the ability can be run now")]
		private bool Trigger_Fire(Character chr) {
			bool cancel = false;
			cancel = TryCancellableTrigger(chr, ImmediateAbilityDef.tkFire, null);
			if(!cancel) {
				cancel = chr.On_AbilityFire(this);
				if (!cancel) {//still not cancelled
					cancel = On_Fire(chr);
				}
			}
			return cancel;
		}			

		[Summary("This method fires the @denyUse triggers. "
				+ "Their purpose is to check if all requirements for running the ability have been met")]
		private bool Trigger_DenyUse(DenyAbilityArgs args) {
			bool cancel = false;
			cancel = this.TryCancellableTrigger(args.abiliter, ActivableAbilityDef.tkDenyUse, args);
			if(!cancel) {//not cancelled (no return 1 in LScript), lets continue
				cancel = args.abiliter.On_AbilityDenyUse(args);
				if(!cancel) {//still not cancelled, try the C# method
					cancel = On_DenyUse(args);
				}
			}
			return cancel;
		}

		[Summary("C# based @denyUse trigger method, implementation of common checks (ability presence, timers...)")]		
		protected virtual bool On_DenyUse(DenyAbilityArgs args) {
			Ability ab = args.runAbility;
			//common check - is the usage timer OK?
			if((Globals.TimeInSeconds - ab.LastUsage) <= this.UseDelay) { //check the timing if OK
				args.Result = DenyResultAbilities.Deny_TimerNotPassed;
				return true;//same as "return 1" from LScript - cancel trigger sequence
			} 
			return false; //continue
		}
		#endregion triggerMethods		
	}
}
