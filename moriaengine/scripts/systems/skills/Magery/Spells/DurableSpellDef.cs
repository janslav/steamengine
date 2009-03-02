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
		private FieldValue duration;

		public DurableSpellDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {

			this.duration = this.InitField_Typed("duration", null, typeof(double[]));
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
	}

	[ViewableClass]
	public class DurableCharEffectSpellDef : DurableSpellDef {

		private FieldValue effectPluginDef;
		private FieldValue effectPluginKey_Spell;
		private FieldValue effectPluginKey_Potion;

		public DurableCharEffectSpellDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.effectPluginDef = this.InitField_Typed("effectPluginDef", null, typeof(PluginDef));

			this.effectPluginKey_Spell = this.InitField_Typed("effectPluginKey_Spell", PluginKey.Get(string.Concat("_spellEffect_", defname, "_")), typeof(PluginDef));
			this.effectPluginKey_Potion = this.InitField_Typed("effectPluginKey_Potion", PluginKey.Get(string.Concat("_potionEffect_", defname, "_")), typeof(PluginDef));
		}

		public PluginDef EffectPluginDef {
			get {
				return (PluginDef) this.effectPluginDef.CurrentValue;
			}
			set {
				this.effectPluginDef.CurrentValue = value;
			}
		}

		public PluginKey EffectPluginKey_Spell {
			get {
				return (PluginKey) this.effectPluginKey_Spell.CurrentValue;
			}
			set {
				this.effectPluginKey_Spell.CurrentValue = value;
			}
		}

		public PluginKey EffectPluginKey_Potion {
			get {
				return (PluginKey) this.effectPluginKey_Potion.CurrentValue;
			}
			set {
				this.effectPluginKey_Potion.CurrentValue = value;
			}
		}

		protected override void On_EffectChar(Character target, SpellEffectArgs spellEffectArgs) {
			base.On_EffectChar(target, spellEffectArgs);

			SpellSourceType sourceType = spellEffectArgs.SourceType;
			PluginKey key;
			if (sourceType == SpellSourceType.Potion) {
				key = this.EffectPluginKey_Potion;
			} else {
				key = this.EffectPluginKey_Spell;
			}

			target.DeletePlugin(key);

			Plugin plugin = this.EffectPluginDef.Create();

			SpellEffectDurationPlugin durationPlugin = plugin as SpellEffectDurationPlugin;
			if (durationPlugin != null) {
				int spellPower = spellEffectArgs.SpellPower;
				durationPlugin.Init(spellEffectArgs.Caster, sourceType, this.GetEffectForValue(spellPower), TimeSpan.FromSeconds(this.GetDurationForValue(spellPower)));
			}
			target.AddPlugin(key, plugin);
		}
	}
}