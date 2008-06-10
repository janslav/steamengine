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
using SteamEngine.CompiledScripts;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	[Summary("Special ability class used for implementing abilities that can be activated (usually by calling some SteamFunction)."+
			"The performing of the ability ends when it is either manually deactivated or some other conditions are fulfilled"+
			"The included TriggerGroup/Plugin will be attached to the holder after activation (and removed after deactivation)")]
	[ViewableClass]
	public class ActivableAbilityDef : TriggerBasedAbilityDef {
		internal static readonly TriggerKey tkActivate = TriggerKey.Get("Activate");
		internal static readonly TriggerKey tkUnActivate = TriggerKey.Get("UnActivate");
		internal static readonly TriggerKey tkDenyUse = TriggerKey.Get("DenyUse");

		[Summary("Field for holding the number information about the pause between another ability activation try."+
				"You can use 0 for no delay")]
		private FieldValue useDelay;
		
		public ActivableAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			//initialize the field value
			//the field will be in the LScript as follows:
			//[ActivableAbilityDef a_bility]
			//...
			//useDelay = 300
			//runMessage = "Nyni muzes zabijet zlym pohledem"
			//...
			useDelay = InitField_Typed("useDelay", 0, typeof(int));
		}

		[Summary("Check the ability on the character, if he has it, chesk its state and decide what to do next."+
				"If its is running - deactivate, otherwise - activate.")]
		public void ActivateOrUnactivate(Character chr) {
			Ability ab = chr.GetAbility(this);
			DenyResultAbilities retVal = 0;
			if(ab == null || ab.Points == 0) {//"0th" common check - do we have the ability?
				if(ab.Running) {
					ab.Running = false;
					Trigger_UnActivate(chr); //ability is running, do the triggers (usually to remove triggergroup / plugin)			
					retVal = DenyResultAbilities.Deny_WasSwitchedOff; //inform about switching off
				} else {
					Activate(chr, ab);//try to activate
				}
			} else {
				retVal = DenyResultAbilities.Deny_DoesntHaveAbility;
			}
			if(retVal != 0) {
				//ability either not existed or was running - send the message now
				SendAbilityResultMessage(chr, retVal);
			}
		}

		[Summary("Different Activate method than in AbilityDef - called only privately after availability- and running- checks")]
		private void Activate(Character chr, Ability ab) {
			DenyResultAbilities retVal = 0;
			if(ab == null || ab.Points == 0) {//"0th" common check - do we have the ability?
				retVal = DenyResultAbilities.Deny_DoesntHaveAbility;				
			} else {
				DenyAbilityArgs args = new DenyAbilityArgs(chr, this, ab);
				bool cancel = Trigger_DenyUse(args); //return value means only that the trigger has been cancelled
				retVal = args.Result;//this value contains the info if we can or cannot run the ability
			}
			if(retVal == DenyResultAbilities.Allow) {
				Trigger_Activate(chr);
				//if we are here, we can use the ability
				ab.LastUsage = Globals.TimeInSeconds; //set the last usage time
				ab.Running = true;
			}
			SendAbilityResultMessage(chr, retVal); //send result(message) of the "activate" call to the client
		}

		#region triggerMethods
		[Summary("C# based @notYet trigger method")]
		protected virtual void On_UnActivate(Character ch) {
			ch.RemoveTriggerGroup(this.TriggerGroup);
			ch.RemovePlugin(this.PluginKeyInstance);
		}

		[Summary("LScript based @unactivate triggers and all unactivate trigger methods."+
				"We typically remove the triggergroups and plugins (if any) here.")]
		protected void Trigger_UnActivate(Character chr) {
			if(chr != null) {
				TryTrigger(chr, ActivableAbilityDef.tkUnActivate, null);
				chr.On_AbilityUnActivate(this);
				On_UnActivate(chr);
			}
		}

		[Summary("C# based @activate trigger method")]
		protected virtual void On_Activate(Character ch) {
			ch.AddTriggerGroup(this.TriggerGroup);
			if(this.PluginDef != null) {
				ch.AddNewPlugin(this.PluginKeyInstance, this.PluginDef);
			}
		}

		[Summary("LScript based @activate triggers and all activate trigger methods")]
		protected void Trigger_Activate(Character chr) {
			if(chr != null) {
				TryTrigger(chr, ActivableAbilityDef.tkActivate, null);
				chr.On_AbilityActivate(this);
				On_Activate(chr);
			}
		}		

		[Summary("This method fires the @denyUse triggers. "
				+ "Their purpose is to check if all requirements for running the ability have been met")]
		private bool Trigger_DenyUse(DenyAbilityArgs args) {
			bool cancel = false;
			cancel = this.TryCancellableTrigger(args.abiliter, ActivableAbilityDef.tkDenyUse, args);
			if(!cancel) {//not cancelled (no return 1 in LScript), lets continue
				cancel = args.abiliter.On_AbilityDenyUse(args);
				if(!cancel) {//still not cancelled
					cancel = On_DenyUse(args);
				}
			}
			return cancel;
		}

		[Summary("C# based @denyUse trigger method, implementation of common checks (ability presence, (not)running, timers...)")]
		protected virtual bool On_DenyUse(DenyAbilityArgs args) {
			Ability ab = args.runAbility;			
			//common check - is the usage timer OK?
			if((Globals.TimeInSeconds - ab.LastUsage) <= this.UseDelay) { //check the timing if OK
				args.Result = DenyResultAbilities.Deny_TimerNotPassed;
				return false;
			}
			return false; //continue
		}
		#endregion triggerMethods

		[InfoField("Usage delay")]
		public virtual int UseDelay {
			get {
				return (int)useDelay.CurrentValue;
			}
			set {
				useDelay.CurrentValue = value;
			}
		}		
	}
}
