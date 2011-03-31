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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using SteamEngine.Common;

namespace SteamEngine {
	public abstract class AbstractScript : IUnloadable {

		private readonly static ConcurrentDictionary<string, AbstractScript> byDefname = new ConcurrentDictionary<string, AbstractScript>(StringComparer.OrdinalIgnoreCase);

		private string defname;
		private bool unloaded = false;

		public static void Bootstrap() {
			CompiledScripts.ClassManager.RegisterSupplySubclassInstances<AbstractScript>(null, false, false);
		}

		public static AbstractScript GetByDefname(string defname) {
			AbstractScript script;
			byDefname.TryGetValue(defname, out script);
			return script;
		}

		public static void ForgetAll() {
			foreach (AbstractScript gs in byDefname.Values.ToList()) {
				gs.Unregister();
			}
			Contract.Ensures(byDefname.Count == 0, "byDefname.Count > 0 after UnloadAll");
		}

		//register with static dictionaries and lists. 
		//Can be called multiple times without harm
		//Returns self for easier usage 
		virtual public AbstractScript Register() {
			if (!string.IsNullOrEmpty(this.defname)) {
				var previous = byDefname.GetOrAdd(this.defname, this);
				if (previous != this) {
					throw new SEException("previous != this when registering AbstractScript '" + this.defname + "'");
				}
			}
			return this;
		}

		//unregister from static dictionaries and lists. 
		//Can be called multiple times without harm
		virtual protected void Unregister() {
			if (!string.IsNullOrEmpty(this.defname)) {
				AbstractScript previous;
				if (byDefname.TryRemove(this.defname, out previous)) {
					if (previous != this) {
						if (!byDefname.TryAdd(this.defname, previous)) {
							throw new FatalException("Parallel loading fucked up.");
						}
						throw new SEException("previous != this when unregistering AbstractScript '" + this.defname + "'");
					}
				}
			}
		}

		internal static ConcurrentDictionary<string, AbstractScript> AllScriptsByDefname {
			get {
				return byDefname;
			}
		}

		public static ICollection<AbstractScript> AllScripts {
			get {
				return byDefname.Values;
			}
		}

		protected AbstractScript() {
			this.defname = this.InternalFirstGetDefname();
			if (byDefname.ContainsKey(this.defname)) {
				throw new SEException("AbstractScript called " + LogStr.Ident(this.defname) + " already exists!");
			}
		}

		protected AbstractScript(string defname) {
			if (String.IsNullOrEmpty(defname)) {
				this.defname = this.InternalFirstGetDefname();
			} else {
				this.defname = defname;
			}
			if (byDefname.ContainsKey(this.defname)) {
				throw new SEException("AbstractScript called " + LogStr.Ident(this.defname) + " already exists!");
			}
		}

		public virtual void Unload() {
			this.unloaded = true;
		}

		public virtual void UnUnload() {
			this.unloaded = false;
		}

		public bool IsUnloaded {
			get {
				return this.unloaded;
			}
		}

		internal void InternalSetDefname(string value) {
			this.defname = value;
		}

		public string Defname {
			get {
				return this.defname;
			}
		}

		public virtual string PrettyDefname {
			get {
				return this.defname;
			}
		}

		protected void ThrowIfUnloaded() {
			if (this.unloaded) {
				throw new UnloadedException("The " + Tools.TypeToString(this.GetType()) + " '" + LogStr.Ident(defname) + "' is unloaded.");
			}
		}

		protected virtual string InternalFirstGetDefname() {
			throw new SEException("This should not happen");
		}
	}
}