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

	[Summary("Ability class serving as a parent for special types of abilities that can assign a plugin or a "+
			"trigger group (or both) to the ability holder when activated/fired. This class specially offers only fields "+
			"for storing the plugin/trigger group info. The assigning will be managed on children classes")]
	[ViewableClass]
	public class TriggerBasedAbilityDef : AbilityDef {
		//fields for storing the keys (comming from LScript or set in constructor of children)
		private FieldValue triggerGroup;
		private FieldValue pluginDef;
		private FieldValue pluginKey;

		public TriggerBasedAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			//we expect the values from Lscript as follows
			//[TriggerBasedAbilityDef a_bility]
			//...
			//triggerGroup=t_some_triggergroup
			//...
			//pluginDef=p_some_plugindef
			//...
			//pluginKey=p_some_pluginkey
			//these values will be then used for assigning TG / plugin to the ability holder
			triggerGroup = InitField_Typed("triggerGroup", null, typeof(TriggerGroup));
			pluginDef = InitField_Typed("pluginDef", null, typeof(PluginDef));
			pluginKey = InitField_Typed("pluginKey", null, typeof(PluginKey));
		}

		[Summary("Triggergroup connected with this ability (can be null if no key is specified). It will be used " +
				"for appending trigger groups to the ability holder")]
		public TriggerGroup TriggerGroup {
			get {
				return triggerGroup.CurrentValue as TriggerGroup;
			}
		}

		[Summary("Plugindef connected with this ability (can be null if no key is specified). It will be used "+
				"for creating plugin instances and setting them to the ability holder")]
		public PluginDef PluginDef {
			get {
				return pluginDef.CurrentValue as PluginDef;
			}
		}

		[Summary("Return plugin key from the field value (used e.g. for removing plugins)")]
		public PluginKey PluginKeyInstance {
			get {
				return pluginKey.CurrentValue as PluginKey;				
			}
		}
	}
}
