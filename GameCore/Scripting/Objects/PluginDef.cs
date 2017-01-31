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
using System.Diagnostics.CodeAnalysis;
using Shielded;
using SteamEngine.Common;
using SteamEngine.Parsing;

namespace SteamEngine.Scripting.Objects {
	public abstract class PluginDef : AbstractDef {

		private static readonly ShieldedDictNc<Type, Type> pluginDefTypesByPluginType = new ShieldedDictNc<Type, Type>();
		private static readonly ShieldedDictNc<Type, Type> pluginTypesByPluginDefType = new ShieldedDictNc<Type, Type>();

		private static readonly ShieldedDictNc<Type, PluginTriggerGroup> triggerGroupsByType = new ShieldedDictNc<Type, PluginTriggerGroup>();

		private readonly Shielded<TriggerGroup> scriptedTriggers = new Shielded<TriggerGroup>();
		private readonly Shielded<PluginTriggerGroup> compiledTriggers = new Shielded<PluginTriggerGroup>();

		protected PluginDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			{
				this.TryActivateCompiledTriggers();
			}
		}

		public TriggerGroup ScriptedTriggers => this.scriptedTriggers.Value;

		public PluginTriggerGroup CompiledTriggers => this.compiledTriggers.Value;

		private void TryActivateCompiledTriggers() {
			PluginTriggerGroup ptg;
			if (triggerGroupsByType.TryGetValue(this.GetType(), out ptg)) {
				this.compiledTriggers.Value = ptg;
			}
		}

		[SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
		public abstract class PluginTriggerGroup {
			public abstract object Run(Plugin self, TriggerKey tk, ScriptArgs sa);
		}

		protected abstract Plugin CreateImpl();

		public Plugin Create() {
			var p = this.CreateImpl();
			p.def = this;
			return p;
		}

		public new static PluginDef GetByDefname(string defname) {
			return AbstractScript.GetByDefname(defname) as PluginDef;
		}

		public static void RegisterPluginTg(Type defType, PluginTriggerGroup tg) {
			triggerGroupsByType[defType] = tg;
		}

		//this should be typically called by the Bootstrap methods of scripted PluginDef
		public static void RegisterPluginDef(Type pluginDefType, Type pluginType) {
			SeShield.AssertInTransaction();

			Type t;
			if (pluginDefTypesByPluginType.TryGetValue(pluginDefType, out t)) {
				throw new OverrideNotAllowedException("PluginDef type " + LogStr.Ident(pluginDefType.FullName) + " already has it's Plugin type -" + t.FullName + ".");
			}
			if (pluginTypesByPluginDefType.TryGetValue(pluginType, out t)) {
				throw new OverrideNotAllowedException("Plugin type " + LogStr.Ident(pluginType.FullName) + " already has it's PluginDef type -" + t.FullName + ".");
			}

			pluginDefTypesByPluginType[pluginType] = pluginDefType;
			pluginTypesByPluginDefType[pluginDefType] = pluginType;
		}

		public override void LoadScriptLines(PropsSection ps) {
			SeShield.AssertInTransaction();

			base.LoadScriptLines(ps);

			//now do load the trigger code. 
			if (ps.TriggerCount > 0) {
				ps.HeaderName = "t__" + this.Defname + "__";
				this.scriptedTriggers.Value = InterpretedTriggerGroup.Load(ps);
			}
		}

		public override void Unload() {
			SeShield.AssertInTransaction();

			this.scriptedTriggers.Value?.Unload();
			base.Unload();
		}

		internal new static void ForgetAll() {
			SeShield.AssertInTransaction();

			pluginDefTypesByPluginType.Clear();
			pluginTypesByPluginDefType.Clear();//we can assume that inside core there are no non-abstract plugindefs
											   //pluginDefTypesByName.Clear();
											   //pluginDefCtors.Clear();
			triggerGroupsByType.Clear();
		}

		internal static void Init() {
			foreach (var script in AllScripts) {
				var pd = script as PluginDef;
				if (pd != null) {
					SeShield.InTransaction(pd.TryActivateCompiledTriggers);
				}
			}
		}
	}
}
