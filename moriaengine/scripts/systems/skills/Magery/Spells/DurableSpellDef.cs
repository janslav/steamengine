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
using SteamEngine.Regions;
using SteamEngine.CompiledScripts;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	[ViewableClass]
	public class DurableSpellDef : SpellDef {

		private FieldValue effectPluginDef;
		private FieldValue duration;

		private readonly PluginKey effectFromSpellPK;
		private readonly PluginKey effectFromPotionPK;

		public DurableSpellDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {

			this.duration = this.InitField_Typed("duration", null, typeof(double[]));
			this.effectPluginDef = this.InitField_Typed("effectPluginDef", null, typeof(PluginDef));

			this.effectFromSpellPK = PluginKey.Get(string.Concat("_effectPlugin_", defname, "_fromSpell_"));
			this.effectFromPotionPK = PluginKey.Get(string.Concat("_effectPlugin_", defname, "_fromPotion_"));
		}
		
		public double[] Duration {
			get {
				return (double[]) this.duration.CurrentValue;
			}
			set {
				this.duration.CurrentValue = value;
			}
		}

		public double GetDurationForValue(double spellpower) {
			return ScriptUtil.EvalRangePermille(spellpower, this.Duration);
		}

		protected SpellEffectDurationPlugin InitDurationPlugin(Character target, double spellPower, SpellSourceType sourceType) {
			SpellEffectDurationPlugin plugin = (SpellEffectDurationPlugin) this.EffectPluginDef.Create();
			PluginKey key;
			bool dispellable;
			if (sourceType == SpellSourceType.Potion) {
				key = this.EffectFromPotionPK;
				dispellable = false; //potions are generally not dispellable. Might want some exception from this rule at some point...?
			} else {
				key = this.effectFromSpellPK;
				dispellable = true;
			}

			target.DeletePlugin(key);

			plugin.Init(this.GetEffectForValue(spellPower), this.GetDurationForValue(spellPower), dispellable);
			target.AddPlugin(key, plugin);
			return plugin;
		}

		public PluginDef EffectPluginDef {
			get {
				return (PluginDef) this.effectPluginDef.CurrentValue;
			}
			set {
				this.effectPluginDef.CurrentValue = value;
			}
		}

		public PluginKey EffectFromBookPK {
			get {
				return this.effectFromSpellPK;
			}
		}

		public PluginKey EffectFromPotionPK {
			get {
				return this.effectFromPotionPK;
			}
		}

		protected override void On_EffectChar(Character target, SpellEffectArgs spellEffectArgs) {
			base.On_EffectChar(target, spellEffectArgs);
			this.InitDurationPlugin(target, spellEffectArgs.SpellPower, spellEffectArgs.SourceType);
		}
	}
}