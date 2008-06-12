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
	[Summary("Ability class serving as a parent for special types of abilities that can assign a plugin or a " +
			"trigger group (or both) to the ability holder when activated/fired. This class specially offers only fields " +
			"for storing the plugin/trigger group info it can be also activated (usually by calling some SteamFunction)."+
			"The performing of the ability ends when it is either manually deactivated or some other conditions are fulfilled"+
			"The included TriggerGroup/Plugin will be attached to the holder after activation (and removed after deactivation)")]
	[ViewableClass]
	public class ActivableAbilityDef : AbilityDef {
		internal static readonly TriggerKey tkUnActivate = TriggerKey.Get("UnActivate");
		
		//fields for storing the keys (comming from LScript or set in constructor of children)
		private FieldValue triggerGroup;
		private FieldValue pluginDef;
		private FieldValue pluginKey;
		
		public ActivableAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			//initialize the field value
			//the field will be in the LScript as follows:
			//[ActivableAbilityDef a_bility]
			//...
			//triggerGroup=t_some_triggergroup
			//pluginDef=p_some_plugindef
			//pluginKey=p_some_pluginkey
			//these values will be then used for assigning TG / plugin to the ability holder
			//...
			//we expect the values from Lscript as follows
			triggerGroup = InitField_Typed("triggerGroup", null, typeof(TriggerGroup));
			pluginDef = InitField_Typed("pluginDef", null, typeof(PluginDef));
			pluginKey = InitField_Typed("pluginKey", null, typeof(PluginKey));
		}

		[Summary("Check the ability on the character, if he has it, chesk its state and decide what to do next."+
				"If its is running - deactivate, otherwise - activate.")]
		public void ActivateOrUnactivate(Character chr) {
			Ability ab = chr.GetAbility(this);
			if(ab != null || ab.Points > 0) {//"0th" common check - do we have the ability?
				if(ab.Running) {
					UnActivate(chr, ab);			
				} else {
					Activate(chr, ab);//try to activate
				}
			} else {
				chr.RedMessage("O abilitì " + Name + " nevíš vùbec nic");
			}			
		}

		[Summary("Common method for simple switching the ability off")]		
		public void UnActivate(Character chr) {
			Ability ab = chr.GetAbility(this);
			if (ab == null || ab.Points == 0) {
				chr.RedMessage("O abilitì " + Name + " nevíš vùbec nic, není co vypínat.");
			} else {
				UnActivate(chr, ab);
			}
		}

		private void UnActivate(Character chr, Ability ab) {
			if (ab.Running) { //do it only if running
				ab.Running = false;
				Trigger_UnActivate(chr); //ability is running, do the triggers (usually to remove triggergroup / plugin)
				chr.SysMessage("Abilita " + Name + " byla vypnuta");//inform about switching off
			}
		}

		[Summary("Common method for simple switching the ability on")]
		public override void Activate(Character chr) {
			Ability ab = chr.GetAbility(this);
			if (ab == null || ab.Points == 0) {
				chr.RedMessage("O abilitì " + Name + " nevíš vùbec nic.");
			} else {
				Activate(chr, ab);
			}
		}

		private void Activate(Character chr, Ability ab) {
			DenyAbilityArgs args = new DenyAbilityArgs(chr, this, ab);
			bool cancelDeny = Trigger_DenyUse(args); //return value means only that the trigger has been cancelled
			DenyResultAbilities retVal = args.Result;//this value contains the info if we can or cannot run the ability
			
			if(retVal == DenyResultAbilities.Allow) {
				bool cancelActivate = Trigger_Activate(chr);
				//if we are here, we can use the ability
				ab.LastUsage = Globals.TimeInSeconds; //set the last usage time
				ab.Running = true;
			}
			SendAbilityResultMessage(chr, retVal); //send result(message) of the "activate" call to the client
		}

		#region triggerMethods
		[Summary("When unassigning, do not forget to deactivate the ability (and additionally remove TGs and plugins)")]
		protected override void On_UnAssign(Character ch) {
			UnActivate(ch); //unactivate the ability automatically
		}

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
		#endregion triggerMethods
		[Summary("C# based @activate trigger method, Overriden from parent - add TG / Plugin to ability holder")]
		protected override bool On_Activate(Character ch) {
			ch.AddTriggerGroup(this.TriggerGroup);
			if (this.PluginDef != null) {
				ch.AddNewPlugin(this.PluginKeyInstance, this.PluginDef);
			}
			return false;//no cancelling, correctly return
		}

		[Summary("Triggergroup connected with this ability (can be null if no key is specified). It will be used " +
				"for appending trigger groups to the ability holder")]
		public TriggerGroup TriggerGroup {
			get {
				return (TriggerGroup) triggerGroup.CurrentValue;
			}
		}

		[Summary("Plugindef connected with this ability (can be null if no key is specified). It will be used " +
				"for creating plugin instances and setting them to the ability holder")]
		public PluginDef PluginDef {
			get {
				return (PluginDef) pluginDef.CurrentValue;
			}
		}

		[Summary("Return plugin key from the field value (used e.g. for removing plugins)")]
		public PluginKey PluginKeyInstance {
			get {
				return (PluginKey) pluginKey.CurrentValue;
			}
		}
	}
}
