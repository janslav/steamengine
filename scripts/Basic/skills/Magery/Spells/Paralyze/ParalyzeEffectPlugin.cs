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
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Scripting;
using SteamEngine.Scripting.Compilation;

namespace SteamEngine.CompiledScripts {

	[ViewableClass]
	public partial class ParalyzeEffectPlugin {
		public void On_Assign() {
			var self = (Character) this.Cont;
			self.ClilocSysMessage(500111); //You are frozen and cannot move.
		}

		public TriggerResult On_Step(Direction dir, bool running) {
			var self = (Character) this.Cont;
			self.ClilocSysMessage(500111); //You are frozen and cannot move.
			return TriggerResult.Cancel;
		}

		protected override void EffectEndedMessage(Character cont) {
			cont.ClilocSysMessage(500007); //You are no longer frozen.
		}

		//disruption = when attacked
		protected virtual void On_Disruption() {
			this.Delete();
		}
	}

	[ViewableClass]
	public partial class ParalyzeEffectPluginDef {
		public static readonly ParalyzeEffectPluginDef Instance = (ParalyzeEffectPluginDef)
			new ParalyzeEffectPluginDef("p_paralyze", "C# scripts", -1).Register();

		public static DurableCharEffectSpellDef ParalyzeSpellDef => (DurableCharEffectSpellDef) SpellDef.GetByDefname("s_paralyze");


		//paralyze method for general usage
		public static void Paralyze(Character target, PluginKey key, TimeSpan duration, Thing source, EffectFlag type) {
			type |= EffectFlag.HarmfulEffect;
			var plugin = (ParalyzeEffectPlugin) Instance.Create();
			plugin.Init(source, type, 1, duration, ParalyzeSpellDef);
			target.AddPlugin(key, plugin);
		}

		public static void Paralyze(Character target, PluginKey key, TimeSpan duration) {
			Paralyze(target, key, duration, null, EffectFlag.HarmfulEffect | EffectFlag.FromTrap);
		}

		[SteamFunction]
		public static void Paralyze(Character target, ScriptArgs sa) {
			var key = (PluginKey) sa.Argv[0];
			TimeSpan duration;
			var argv1 = sa.Argv[1];
			if (argv1 is TimeSpan) {
				duration = (TimeSpan) argv1;
			} else {
				duration = TimeSpan.FromSeconds(ConvertTools.ToDouble(argv1));
			}
			Thing source = null;
			var type = EffectFlag.HarmfulEffect | EffectFlag.FromTrap;
			var len = sa.Argv.Length;
			if (len > 2) {
				source = (Thing) sa.Argv[2];
				if (len > 3) {
					type = (EffectFlag) ConvertTools.ToInt64(sa.Argv[3]);
				}
			}
			Paralyze(target, key, duration, source, type);
		}
	}
}

//500111	You are frozen and cannot move.
//500007	You are no longer frozen.
//1060158	You cannot mount that while you are frozen.
//1060170	You cannot use this ability while frozen.