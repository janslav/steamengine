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
		internal static readonly TriggerKey tkNotYet = TriggerKey.Get("NotYet");
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
			//...
			useDelay = InitField_Typed("useDelay", 0, typeof(int));
		}

		[Summary("Implementation of the activating method. Used for activating/deactivating the ability")]
		internal override void Activate(Character chr) {
			///kdyz jsme zde, znamena to, ze muzeme abilitu spoustet (jiz po kontrole)
			Ability ab = chr.GetAbility(this);
			if(ab.Running) {
				Trigger_UnActivate(chr);
			} else {
				if((Globals.TimeInSeconds - ab.LastUsage) >= this.UseDelay) { //check the timing if OK
					if(!Trigger_DenyUse(new DenyAbilityArgs(chr, this))) {//check all prerequisities
						Trigger_Activate(chr);
					}
				} else {
					NotYet(chr);
				}
			}
		}

		#region triggerMethods
		[Summary("C# based @notYet trigger method")]
		protected virtual void On_UnActivate(Character ch) {
			ch.RemoveTriggerGroup(this.TriggerGroup);
			ch.RemovePlugin(this.PluginKeyInstance);
		}

		[Summary("LScript based @unactivate triggers and all unactivate trigger methods")]
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

		[Summary("This method fires the @notYet triggers. "
				+ "Gets called when user activates the ability but it is not to be fired yet")]
		internal void NotYet(Character chr) {
			if(!this.Trigger_NotYet(chr)) {
				this.On_NotYet(chr);//not cancelled (no return 1 in LScript), lets continue
			}
		}

		[Summary("C# based @notYet trigger method")]
		protected virtual void On_NotYet(Character ch) {
			ch.RedMessage("Abilitu nelze pou��t tak brzy po p�edchoz�m pou�it�");
		}

		[Summary("LScript based @notYet triggers")]
		private bool Trigger_NotYet(Character chr) {
			bool cancel = false;
			cancel = this.TryCancellableTrigger(chr, ActivableAbilityDef.tkNotYet, null);
			if(!cancel) {
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
				if(!cancel) {//still not cancelled
					cancel = On_DenyUse(args);
				}
			}
			return cancel;
		}

		[Summary("C# based @denyUse trigger method")]
		protected virtual bool On_DenyUse(DenyAbilityArgs args) {
			args.abiliter.SysMessage("Abilita " + Name + " nem� implementaci trigger metody On_DenyUse");
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
