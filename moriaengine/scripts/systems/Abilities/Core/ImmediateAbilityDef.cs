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

namespace SteamEngine {

	[Summary("Special ability class used for implementing abilities that will be run using SteamFunction." +
			"Their effect will be immediate (e.g. warcry)")]
	public class ImmediateAbilityDef : TriggerBasedAbilityDef {
		public static readonly TriggerKey tkFire = TriggerKey.Get("Fire");
		
		[Summary("Field for holding the number information about the pause between another ability activation try." +
				"You can use 0 for no delay")]
		private FieldValue runDelay;

		public ImmediateAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			//initialize the field value
			//the field will be in the LScript as follows:
			//[ImmediateAbilityDef a_bility]
			//...
			//runDelay = 300
			//...
			runDelay = InitField_Typed("runDelay", 0, typeof(int));
		}

		[Summary("Correct implementation of the activating method")]
		public new void Activate(Ability ab) {
			///TODO - dodelat nejakou kontrolu treba resourcu zejo...
			if((Globals.TimeInSeconds - ab.LastUsage) >= this.RunDelay) {
				Trigger_Fire(ab.Cont,ab); 
			} else {
				Trigger_NotYet(ab.Cont, ab);
			}
		}

		#region triggerMethods
		[Summary("This method implements firing the ability. Will be implemened ond special children")]
		protected virtual void On_Fire(Character chr) {

		}

		internal void Trigger_Fire(Character chr, Ability ab) {
			if(chr != null) {
				ScriptArgs sa = new ScriptArgs(ab);
				//call the trigger @fire with arguments "ability"-the Ability class on character (it has its own reference to the char)
				TryTrigger(chr, ImmediateAbilityDef.tkFire, sa);
				chr.On_AbilityFire(ab);
				On_Fire(chr);
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
				//call the trigger @notYet with arguments "ability"-the Ability class on character (it has its own reference to the char)
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
