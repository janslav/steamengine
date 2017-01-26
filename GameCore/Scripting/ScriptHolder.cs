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
using System.Diagnostics.CodeAnalysis;
using Shielded;
using SteamEngine.Common;
using SteamEngine.Scripting.Objects;

namespace SteamEngine.Scripting {
	public abstract class ScriptHolder {
		private static readonly ShieldedDictNc<string, ScriptHolder> functionsByName =
			new ShieldedDictNc<string, ScriptHolder>(comparer: StringComparer.OrdinalIgnoreCase);

		private readonly string name;

		public static ScriptHolder GetFunction(string name) {
			ScriptHolder sh;
			functionsByName.TryGetValue(name, out sh);
			return sh;
		}

		protected ScriptHolder(string name) {
			if (string.IsNullOrEmpty(name)) {
				this.name = this.InternalFirstGetName();
			}
			this.name = name;
		}

		protected ScriptHolder() {
			this.name = this.InternalFirstGetName();
		}

		public string Name => this.name;

		protected virtual string InternalFirstGetName() {
			throw new SEException("This should not happen");
		}

		protected internal void RegisterAsFunction() {
			Shield.AssertInTransaction();
			if (functionsByName.ContainsKey(this.name)) {
				throw new ServerException("ScriptHolder '" + this.name +
										  "' already exists; Cannot create a new one with the same name.");
			}
			functionsByName.Add(this.name, this);
		}

		internal static void ForgetAllFunctions() {
			Shield.AssertInTransaction();
			functionsByName.Clear();
		}

		/// <summary>Return enumerable containing all functions</summary>
		public static IEnumerable<ScriptHolder> AllFunctions {
			get {
				return functionsByName.Values;
			}
		}

		public abstract string Description {
			get;
		}

		public abstract object Run(object self, ScriptArgs sa);

		public object Run(object self, params object[] args) {
			return this.Run(self, new ScriptArgs(args));
		}

		public object TryRun(object self, ScriptArgs sa) {
			try {
				return this.Run(self, sa);
			} catch (FatalException) {
				throw;
			} catch (TransException) {
				throw;
			} catch (Exception e) {
				this.Error(e);
				return null;
			}
		}

		public object TryRun(object self, ScriptArgs sa, out Exception exception) {
			try {
				exception = null;
				return this.Run(self, sa);
			} catch (FatalException) {
				throw;
			} catch (TransException) {
				throw;
			} catch (Exception e) {
				this.Error(e);
				exception = e;
				return null;
			}
		}

		public object TryRun(object self, params object[] args) {
			return this.TryRun(self, new ScriptArgs(args));
		}
		protected virtual void Error(Exception e) {
			Logger.WriteError(e);
		}

		[SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
		public virtual string GetDecoratedName() {
			return this.name;
		}

		public virtual bool IsUnloaded {
			get {
				return false;
			}
		}
	}
}