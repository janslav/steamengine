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
using System.Text;
using System.Collections;
using System.Collections.Generic;
using SteamEngine.Common;

namespace SteamEngine {
	public abstract class AbstractScript : IUnloadable {

		protected readonly static Dictionary<string, AbstractScript> byDefname = new Dictionary<string, AbstractScript>(StringComparer.OrdinalIgnoreCase);

		private string defname;
		protected bool unloaded = false;


		public static void Bootstrap() {
			CompiledScripts.ClassManager.RegisterSupplySubclassInstances<AbstractScript>(null, false, false);
		}

		public static AbstractScript Get(string defname) {
			AbstractScript script;
			byDefname.TryGetValue(defname, out script);
			return script;
		}

		public static void UnloadAll() {
			foreach (AbstractScript gs in byDefname.Values) {
				gs.Unload();
			}
			byDefname.Clear();
		}

		public static IEnumerable<AbstractScript> AllScripts {
			get {
				return byDefname.Values;
			}
		}

		protected AbstractScript() {
			this.defname = this.InternalFirstGetDefname();
			if (byDefname.ContainsKey(this.defname)) {
				throw new SEException("AbstractScript called " + LogStr.Ident(this.defname) + " already exists!");
			}
			byDefname[this.defname] = this;
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
			byDefname[this.defname] = this;
		}

		public virtual void Unload() {
			this.unloaded = true;
		}

		public bool IsUnloaded {
			get {
				return this.unloaded;
			}
		}

		public string Defname {
			get { 
				return this.defname; 
			}
			internal set { 
				this.defname = value; 
			}
		}

		public virtual string PrettyDefname {
			get {
				return this.defname;
			}
		}

		protected void ThrowIfUnloaded() {
			if (unloaded) {
				throw new UnloadedException("The " + this.GetType().Name + " '" + LogStr.Ident(defname) + "' is unloaded.");
			}
		}

		protected virtual string InternalFirstGetDefname() {
			throw new SEException("This should not happen");
		}
	}
}