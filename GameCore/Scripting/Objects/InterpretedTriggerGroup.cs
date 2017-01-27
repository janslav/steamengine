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

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Shielded;
using SteamEngine.Common;
using SteamEngine.Scripting.Interpretation;

namespace SteamEngine.Scripting.Objects {
	public sealed class InterpretedTriggerGroup : TriggerGroup {
		private readonly ShieldedDictNc<TriggerKey, ScriptHolder> triggers = new ShieldedDictNc<TriggerKey, ScriptHolder>();

		private InterpretedTriggerGroup(string defname)
			: base(defname) {
		}

		//the first trigger that throws an exceptions terminates the other ones that way
		public override object Run(object self, TriggerKey tk, ScriptArgs sa) {
			this.ThrowIfUnloaded();
			ScriptHolder sd;
			if (this.triggers.TryGetValue(tk, out sd)) {
				return sd.Run(self, sa);
			}
			return null;    //This triggerGroup does not contain that trigger
		}

		//does not throw the exceptions - all triggers are run, regardless of their errorness
		public override object TryRun(object self, TriggerKey tk, ScriptArgs sa) {
			this.ThrowIfUnloaded();
			ScriptHolder sd;
			if (this.triggers.TryGetValue(tk, out sd)) {
				return sd.TryRun(self, sa);
			}
			return null;    //This triggerGroup does not contain that trigger
		}

		private void AddTrigger(ScriptHolder scriptHolder) {
			var name = scriptHolder.Name;
			//Console.WriteLine("Adding trigger {0} to tg {1}", name, this);
			var tk = TriggerKey.Acquire(name);
			if (this.triggers.ContainsKey(tk)) {
				Logger.WriteError("Attempted to declare triggers of the same name (" + LogStr.Ident(name) + ") in trigger-group " + LogStr.Ident(this.Defname) + "!");
				return;
			}
			this.triggers[tk] = scriptHolder;
		}

		public override string ToString() {
			return "TriggerGroup " + this.Defname;
		}

		public new static TriggerGroup GetByDefname(string name) {
			return AbstractScript.GetByDefname(name) as TriggerGroup;
		}

		private static InterpretedTriggerGroup GetNewOrCleared(string defname) {
			var tg = GetByDefname(defname);
			if (tg == null) {
				var stg = new InterpretedTriggerGroup(defname);
				stg.Register();
				return stg;
			}
			if (tg.IsUnloaded) {
				return (InterpretedTriggerGroup) tg;
			}
			throw new OverrideNotAllowedException("TriggerGroup " + LogStr.Ident(defname) + " defined multiple times.");
		}

		internal static void StartingLoading() {
		}

		[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
		public static TriggerGroup Load(PropsSection input) {
			SeShield.AssertInTransaction();

			var group = GetNewOrCleared(input.HeaderName);
			for (int i = 0, n = input.TriggerCount; i < n; i++) {
				var sc = new LScriptHolder(input.GetTrigger(i), contTriggerGroupName: input.HeaderName);
				if (!sc.IsUnloaded) {//in case the compilation failed, we do not add the trigger
					group.AddTrigger(sc);
				}
			}
			group.UnUnload();
			return group;
		}

		internal static void LoadingFinished() {
			//dump the number of groups loaded?
		}

		public override void Unload() {
			SeShield.AssertInTransaction();

			this.triggers.Clear();
			base.Unload();
		}
	}
}