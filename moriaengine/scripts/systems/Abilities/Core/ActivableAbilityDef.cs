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
			"trigger group (or both) to the ability holder when activated." +
			"The performing of the ability ends when it is either manually deactivated or some other conditions are fulfilled" +
			"The included TriggerGroup/Plugin will be attached to the holder after activation (and removed after deactivation)")]
	[ViewableClass]
	public class ActivableAbilityDef : AbilityDef {
		//internal static readonly TriggerKey tkUnActivate = TriggerKey.Acquire("unActivate");
		//internal static readonly TriggerKey tkUnActivateAbility = TriggerKey.Acquire("unActivateAbility");

		//fields for storing the keys (comming from LScript or set in constructor of children)
		private FieldValue pluginDef;
		private FieldValue pluginKey;

		public ActivableAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			//initialize the field value
			//the field will be in the LScript as follows:
			//[ActivableAbilityDef a_bility]
			//...
			//pluginDef=p_some_plugindef
			//pluginKey=some_pluginkey
			//these values will be then used for assigning plugin to the ability holder
			//...
			//we expect the values from Lscript as follows
			//triggerGroup = InitTypedField("triggerGroup", null, typeof(TriggerGroup)); //which trigger group will be stored on ability holder
			pluginDef = InitTypedField("pluginDef", null, typeof(PluginDef)); //which plugin will be stored on ability holder
			pluginKey = InitTypedField("pluginKey", null, typeof(PluginKey)); //how the plugin will be stored on ability holder
		}

		public bool IsActive(Character ch) {
			Plugin plugin = ch.GetPlugin(this.PluginKey);
			if (plugin != null) {
				return plugin.Def == this.PluginDef;
			}
			return false;
		}

		[Summary("If its is running - deactivate, otherwise - activate.")]
		public void Switch(Character chr) {
			Plugin plugin = chr.GetPlugin(this.PluginKey);
			if (plugin != null) {
				plugin.Delete();
			} else {
				this.Activate(chr);//try to activate
			}
		}

		public override void Activate(Character chr) {
			Plugin plugin = chr.GetPlugin(this.PluginKey);
			if (plugin == null) {
				base.Activate(chr);//try to activate
			}
		}

		[Summary("C# based @activate trigger method, Overriden from parent - add Plugin to ability holder")]
		protected override bool On_Activate(Character chr, Ability ab) {
			PluginDef def = this.PluginDef;
			if (def != null) {
				Plugin plugin = def.Create();
				EffectDurationPlugin durationPlugin = plugin as EffectDurationPlugin;
				if (durationPlugin != null) {
					durationPlugin.Init(chr, EffectFlag.BeneficialEffect | EffectFlag.FromAbility,
						this.EffectPower * ab.ModifiedPoints, TimeSpan.MinValue); //the "power" formula is somewhat arbitrary, but this particular combination (points * effect) seems to be quite popular
				}
				chr.AddPlugin(this.PluginKey, plugin);
			}
			return base.On_Activate(chr, ab);
		}

		public void UnActivate(Character chr) {
			PluginKey key = this.PluginKey;
			if (key != null) {
				chr.DeletePlugin(key);
			}
		}

		#region triggerMethods
		[Summary("When unassigning, do not forget to deactivate the ability")]
		protected override void On_UnAssign(Character ch, Ability ab) {
			this.UnActivate(ch); //unactivate the ability automatically
		}
		#endregion triggerMethods

		[Summary("Plugindef connected with this ability (can be null if no key is specified). It will be used " +
				"for creating plugin instances and setting them to the ability holder")]
		public PluginDef PluginDef {
			get {
				return (PluginDef) pluginDef.CurrentValue;
			}
			set {
				this.pluginDef.CurrentValue = value;
			}
		}

		[Summary("Return plugin key from the field value (used e.g. for adding/removing plugins to the character)")]
		public PluginKey PluginKey {
			get {
				return (PluginKey) pluginKey.CurrentValue;
			}
			set {
				this.pluginKey.CurrentValue = value;
			}
		}
	}
}
