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

namespace SteamEngine.CompiledScripts {

	[Summary("Special ability class used for implementing abilities that can be activated (usually by calling some SteamFunction)."+
			"The performing of the ability ends when it is either manually deactivated or some other conditions are fulfilled"+
			"The included TriggerGroup/Plugin will be attached to the holder after activation (and removed after deactivation)")]
	public class ActivableAbilityDef : TriggerBasedAbilityDef {
		public static readonly TriggerKey tkActivate = TriggerKey.Get("Activate");
		public static readonly TriggerKey tkUnActivate = TriggerKey.Get("UnActivate");
		public static readonly TriggerKey tkNotYet = TriggerKey.Get("NotYet");

		[Summary("Field for holding the number information about the pause between another ability activation try."+
				"You can use 0 for no delay")]
		private FieldValue runDelay;

		public ActivableAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			//initialize the field value
			//the field will be in the LScript as follows:
			//[ActivableAbilityDef a_bility]
			//...
			//runDelay = 300
			//...
			runDelay = InitField_Typed("runDelay", 0, typeof(int));
		}

		[Summary("Correct implementation of the activating method")]
		public new void Activate(Ability ab) {
			///TODO - dodelat nejakou kontrolu treba resourcu zejo...
			if(ab.Running) {
				Trigger_UnActivate(ab.Cont,ab);
			} else {
				if((Globals.TimeInSeconds - ab.LastUsage) >= this.RunDelay) {
					Trigger_Activate(ab.Cont,ab);
				} else {
					Trigger_NotYet(ab.Cont,ab);
				}
			}
		}

		#region triggerMethods
		[Summary("This method implements the deactivating of the ability")]
		protected virtual void On_UnActivate(Character ch) {
			ch.RemoveTriggerGroup(this.TriggerGroup);
			ch.RemovePlugin(this.PluginKeyInstance);
		}

		[Summary("Trigger method called when the ability is unactivated")]
		protected void Trigger_UnActivate(Character chr, Ability ab) {
			if(chr != null) {
				ScriptArgs sa = new ScriptArgs(ab);
				//call the trigger @unactivate with argument "ability" (containing also info about its holder)
				TryTrigger(chr, ActivableAbilityDef.tkUnActivate, sa);
				chr.On_AbilityUnActivate(ab);
				On_UnActivate(chr);
			}
		}

		[Summary("This method implements the activating of the ability")]
		protected virtual void On_Activate(Character ch) {
			ch.AddTriggerGroup(this.TriggerGroup);
			if(this.PluginDef != null) {
				ch.AddNewPlugin(this.PluginKeyInstance, this.PluginDef);
			}
		}

		[Summary("Trigger method called when the ability is activated")]
		protected void Trigger_Activate(Character chr, Ability ab) {
			if(chr != null) {
				ScriptArgs sa = new ScriptArgs(ab);
				//call the trigger @activate with argument "ability" (containing also info about its holder)
				TryTrigger(chr, ActivableAbilityDef.tkActivate, sa);
				chr.On_AbilityActivate(ab);
				On_Activate(chr);
			}
		}

		[Summary("This method implements the behavior when the ability is not yet allowed to be activated")]
		protected virtual void On_NotYet(Character ch) {
			//ch.RedMessage("Abilitu nelze použít tak brzy po pøedchozím použití");
		}

		[Summary("Trigger method called when the ability is activated but it is not allowed yet to do it")]
		protected void Trigger_NotYet(Character chr, Ability ab) {
			if(chr != null) {
				ScriptArgs sa = new ScriptArgs(ab);
				//call the trigger @activate with argument "ability" (containing also info about its holder)
				TryTrigger(chr, ActivableAbilityDef.tkNotYet, sa);
				On_NotYet(chr);
			}
		}
		#endregion triggerMethods

		public int RunDelay {
			get {
				return (int)runDelay.CurrentValue;
			}
		}
	}
}
