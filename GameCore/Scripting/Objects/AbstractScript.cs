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
using System.Collections.Generic;
using System.Linq;
using Shielded;
using SteamEngine.Common;
using SteamEngine.Scripting.Compilation;
using SteamEngine.Transactionality;

namespace SteamEngine.Scripting.Objects {
	public abstract class AbstractScript : IUnloadable {

		private static readonly ShieldedDictNc<string, AbstractScript> byDefname =
			new ShieldedDictNc<string, AbstractScript>(comparer: StringComparer.OrdinalIgnoreCase);

		private readonly Shielded<string> defname = new Shielded<string>();
		private readonly Shielded<bool> unloaded = new Shielded<bool>();

		public static void Bootstrap() {
			ClassManager.RegisterSupplySubclassInstances<AbstractScript>(null, false, false);
		}

		public static AbstractScript GetByDefname(string defname) {
			AbstractScript script;
			byDefname.TryGetValue(defname, out script);
			return script;
		}

		public static void ForgetAll() {
			foreach (var gs in AllScripts) {
				Transaction.InTransaction(() =>
					gs.Unregister());
			}
			Sanity.IfTrueThrow(byDefname.Any(), "byDefname.Count > 0 after UnloadAll");
		}

		//register with static dictionaries and lists. 
		//Can be called multiple times without harm
		//Returns self for easier usage 
		public virtual AbstractScript Register() {
			Transaction.AssertInTransaction();

			var defname = this.Defname;
			if (!string.IsNullOrEmpty(defname)) {
				AbstractScript previous;
				if (byDefname.TryGetValue(defname, out previous)) {
					if (previous != this) {
						throw new SEException("previous != this when registering AbstractScript '" + defname + "'");
					}
				} else {
					byDefname.Add(defname, this);
				}
			}
			return this;
		}

		//unregister from static dictionaries and lists. 
		//Can be called multiple times without harm
		protected virtual void Unregister() {
			Transaction.AssertInTransaction();
			var defname = this.Defname;
			if (!string.IsNullOrEmpty(defname)) {
				AbstractScript previous;
				if (byDefname.TryGetValue(defname, out previous)) {
					if (previous != this) {
						throw new SEException("previous != this when registering AbstractScript '" + defname + "'");
					} else {
						byDefname.Remove(defname);
					}
				}
			}
		}

		internal static ShieldedDictNc<string, AbstractScript> AllScriptsByDefname {
			get {
				Transaction.AssertInTransaction();
				return byDefname;
			}
		}

		public static IReadOnlyCollection<AbstractScript> AllScripts {
			get {
				return Transaction.InTransaction(byDefname.Values.ToList);
			}
		}

		protected AbstractScript() {
			Transaction.AssertInTransaction();
			this.defname.Value = this.InternalFirstGetDefname();
			if (byDefname.ContainsKey(this.defname.Value)) {
				throw new SEException("AbstractScript called " + LogStr.Ident(this.defname.Value) + " already exists!");
			}
		}

		protected AbstractScript(string defname) {
			Transaction.AssertInTransaction();
			if (string.IsNullOrEmpty(defname)) {
				this.defname.Value = this.InternalFirstGetDefname();
			} else {
				this.defname.Value = defname;
			}
			if (byDefname.ContainsKey(this.defname.Value)) {
				throw new SEException("AbstractScript called " + LogStr.Ident(this.defname.Value) + " already exists!");
			}
		}

		public virtual void Unload() {
			this.unloaded.Value = true;
		}

		public virtual void UnUnload() {
			this.unloaded.Value = false;
		}

		public bool IsUnloaded => this.unloaded.Value;

		internal void InternalSetDefname(string value) {
			this.defname.Value = value;
		}

		public string Defname => this.defname.Value;

		public virtual string PrettyDefname => this.defname.Value;

		protected void ThrowIfUnloaded() {
			if (this.unloaded.Value) {
				throw new UnloadedException("The " + Tools.TypeToString(this.GetType()) + " '" + LogStr.Ident(this.Defname) + "' is unloaded.");
			}
		}

		protected virtual string InternalFirstGetDefname() {
			throw new SEException("This should not happen");
		}
	}
}