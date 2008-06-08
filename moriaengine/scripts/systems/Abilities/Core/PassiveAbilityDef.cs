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

	[Summary("Special abiliy class used for implementing abilities that will automatically perform their action when the "+
			"first point is assigned to them (e.g. Regenerations...). These actions can be performed using the included TriggerGroup or "+
			"Plugin that will be attached/detached when the first point is added to the ability (last point is removed from the ability)")]
	[ViewableClass]
	public class PassiveAbilityDef : TriggerBasedAbilityDef {
		public PassiveAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		#region triggerMethods
		[Summary("This method implements the unassigning of the last point from the Ability. "+
				"Remove the plugin and trigger group from the holder")]
		protected override void On_UnAssign(Character ch) {			
			ch.RemoveTriggerGroup(this.TriggerGroup);
			ch.RemovePlugin(this.PluginKeyInstance);

			ch.SysMessage("Ztratil jsi veškeré znalosti o abilitì " + Name);
		}

		[Summary("This method implements the assigning of the first point to the Ability"+        
				"Add plugin and trigger group to the holder, if they exist")]
		protected override void On_Assign(Character ch) {			
			ch.AddTriggerGroup(this.TriggerGroup);
			if(this.PluginDef != null) {
				ch.AddNewPlugin(this.PluginKeyInstance, this.PluginDef);
			}

			ch.SysMessage("Získal jsi abilitu " + Name);
		}
		#endregion triggerMethods
	}
}
