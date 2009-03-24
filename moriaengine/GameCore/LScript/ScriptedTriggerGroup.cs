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
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Globalization;
using SteamEngine.Packets;
using SteamEngine.LScript;
using SteamEngine.Common;
using SteamEngine.CompiledScripts;

namespace SteamEngine {
	public sealed class ScriptedTriggerGroup : TriggerGroup {
		private Dictionary<TriggerKey, ScriptHolder> triggers = new Dictionary<TriggerKey, ScriptHolder>();

		private ScriptedTriggerGroup(string defname)
			: base(defname) {
		}

		//the first trigger that throws an exceptions terminates the other ones that way
		public override sealed object Run(object self, TriggerKey tk, ScriptArgs sa) {
			ThrowIfUnloaded();
			ScriptHolder sd;
			if (triggers.TryGetValue(tk, out sd)) {
				return sd.Run(self, sa);
			} else {
				return null;	//This triggerGroup does not contain that trigger
			}
		}

		//does not throw the exceptions - all triggers are run, regardless of their errorness
		public override sealed object TryRun(object self, TriggerKey tk, ScriptArgs sa) {
			ThrowIfUnloaded();
			ScriptHolder sd;
			if (triggers.TryGetValue(tk, out sd)) {
				return sd.TryRun(self, sa);
			} else {
				return null;	//This triggerGroup does not contain that trigger
			}
		}

		private void AddTrigger(ScriptHolder sd) {
			string name = sd.name;
			//Console.WriteLine("Adding trigger {0} to tg {1}", name, this);
			TriggerKey tk = TriggerKey.Get(name);
			if (triggers.ContainsKey(tk)) {
				Logger.WriteError("Attempted to declare triggers of the same name (" + LogStr.Ident(name) + ") in trigger-group " + LogStr.Ident(this.Defname) + "!");
				return;
			}
			sd.contTriggerGroup = this;
			triggers[tk] = sd;
		}

		public override string ToString() {
			return "TriggerGroup " + Defname;
		}

		public static new TriggerGroup Get(string name) {
			AbstractScript script;
			byDefname.TryGetValue(name, out script);
			return script as TriggerGroup;
		}

		private static ScriptedTriggerGroup GetNewOrCleared(string defname) {
			TriggerGroup tg = Get(defname);
			if (tg == null) {
				return new ScriptedTriggerGroup(defname);
			} else if (tg.IsUnloaded) {
				return (ScriptedTriggerGroup) tg;
			}
			throw new OverrideNotAllowedException("TriggerGroup " + LogStr.Ident(defname) + " defined multiple times.");
		}

		internal static void StartingLoading() {
		}

		public static TriggerGroup Load(PropsSection input) {
			ScriptedTriggerGroup group = GetNewOrCleared(input.headerName);
			for (int i = 0, n = input.TriggerCount; i < n; i++) {
				ScriptHolder sc = new LScriptHolder(input.GetTrigger(i));
				if (!sc.unloaded) {//in case the compilation failed, we do not add the trigger
					group.AddTrigger(sc);
				}
			}
			group.unloaded = false;
			return group;
		}

		internal static void LoadingFinished() {
			//dump the number of groups loaded?
		}

		public override void Unload() {
			triggers.Clear();
			base.Unload();
		}
	}
}