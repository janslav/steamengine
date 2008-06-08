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
	public class ImmediateAbilityDef : TriggerBasedAbilityDef {
		public static readonly TriggerKey tkFire = TriggerKey.Get("Fire");
		
		[Summary("Field for holding the number information about the pause between another ability activation try." +
				"You can use 0 for no delay. This field will be used in children classes and will be attributed as FieldValue "+
				"in order to be settable")]
		
		protected FieldValue useDelay;

		public ImmediateAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			//initialize the field value
			//the field will be in the LScript as follows:
			//[ImmediateAbilityDef a_bility]
			//...
			//useDelay = 300
			//...
			useDelay = InitField_Typed("useDelay", 0, typeof(int));
		}

		[Summary("Implementation of the activating method. Used for running the ability")]
		internal override void Activate(Character chr) {
			//if we are here, we can use the ability
			Ability ab = chr.GetAbility(this);
			if((Globals.TimeInSeconds - ab.LastUsage) >= this.UseDelay) {//check the timing it OK
				if(!Trigger_DenyUse(new DenyAbilityArgs(chr, this))) {//check all requisities
					Fire(chr);
				}
			} else {
				NotYet(chr);
			}
		}

		#region triggerMethods
		[Summary("This method fires the @fire triggers or trigger methods. "
		+ "Gets called when every prerequisity has been fulfilled and the ability can be run now")]
		internal void Fire(Character chr) {
			if(!Trigger_Fire(chr)) {
				On_Fire(chr);//not cancelled (no return 1 in LScript), lets continue
			}
		}

		[Summary("C# based @fire trigger method")]
		protected virtual void On_Fire(Character chr) {
			chr.SysMessage("Abilita " + Name + " nemá implementaci trigger metody On_Fire");
		}

		[Summary("LScript based @fire triggers")]
		private bool Trigger_Fire(Character chr) {
			bool cancel = false;
			cancel = TryCancellableTrigger(chr, ImmediateAbilityDef.tkFire, null);
			if(!cancel) {
				cancel = chr.On_AbilityFire(this);
			}
			return cancel;
		}		

		[Summary("This method fires the @notYet triggers. "
				+ "Gets called when user activates the ability but it is not to be fired yet")]
		internal void NotYet(Character chr) {
			if(!Trigger_NotYet(chr)) {
				On_NotYet(chr);
			}
		}

		[Summary("C# based @notYet trigger method")]
		protected virtual void On_NotYet(Character ch) {
			ch.RedMessage("Abilitu nelze použít tak brzy po pøedchozím použití");
		}

		[Summary("LScript based @notYet triggers")]
		private bool Trigger_NotYet(Character chr) {
			bool cancel = false;
			cancel = this.TryCancellableTrigger(chr, ActivableAbilityDef.tkNotYet, null);
			if(!cancel) {//not cancelled (no return 1 in LScript), lets continue
				cancel = chr.On_AbilityNotYet(this);
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

		[Summary("C# based @denyUse trigger method")]		
		protected virtual bool On_DenyUse(DenyAbilityArgs args) {
			args.abiliter.SysMessage("Abilita " + Name + " nemá implementaci trigger metody On_DenyUse");
			return false; //all ok, continue
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
