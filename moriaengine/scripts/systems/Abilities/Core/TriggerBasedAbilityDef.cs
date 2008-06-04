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

	[Summary("Ability class serving as a parent for special types of abilities that can assign a plugin or a "+
			"trigger group (or both) to the ability holder when activated/fired. This class specially offers only fields "+
			"for storing the plugin/trigger group info. The assigning will be managed on children classes")]
	public class TriggerBasedAbilityDef : AbilityDef {
		//fields for storing the keys (comming from LScript or set in constructor of children)
		private FieldValue triggerGroupKey;
		private FieldValue pluginKey;

		private TriggerGroup triggerGroup;
		private PluginDef pluginDef;

		public TriggerBasedAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			//we expect the values from Lscript as follows
			//[TriggerBasedAbilityDef a_bility]
			//...
			//triggerGroup=t_some_triggergroup
			//...
			//plugin=p_some_plugindef
			//these keys will be then used for assigning TG / plugin to the ability holder
			triggerGroupKey = InitField_Typed("triggerGroup", "", typeof(string));
			pluginKey = InitField_Typed("plugin", "", typeof(string));
		}

		[Summary("Triggergroup connected with this ability (can be null if no key is specified). It will be used " +
				"for appending trigger groups to the ability holder")]
		protected TriggerGroup TriggerGroup {
			get {
				if(triggerGroup == null && !triggerGroupKey.CurrentValue.Equals("")) {
					//we can load
					triggerGroup = TriggerGroup.Get(triggerGroupKey.CurrentValue.ToString());
				}
				return triggerGroup;
			}
		}

		[Summary("Plugindef connected with this ability (can be null if no key is specified). It will be used "+
				"for creating plugin instances and setting them to the ability holder")]
		protected PluginDef PluginDef {
			get {
				if(pluginDef == null && !pluginKey.CurrentValue.Equals("")) {
					//we can load
					pluginDef = PluginDef.Get(pluginKey.CurrentValue.ToString());
				}
				return pluginDef;
			}
		}

		[Summary("Return plugin key from the field value (used e.g. for removing plugins)")]
		protected PluginKey PluginKeyInstance {
			get {
				return PluginKey.Get(pluginKey.CurrentValue.ToString());
			}
		}
	}
}
