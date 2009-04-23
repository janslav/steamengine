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
using System.Reflection;
using System.Text.RegularExpressions;
using System.Globalization;
using SteamEngine.Persistence;
using SteamEngine.Common;

namespace SteamEngine {
	public abstract class Plugin : TagHolder {
		internal PluginHolder cont;
		internal Plugin prevInList;
		internal Plugin nextInList;

		internal PluginDef def;

		private bool isDeleted;

		protected Plugin(Plugin copyFrom)
			: base(copyFrom) {
			this.def = copyFrom.def;
		}

		protected Plugin() {
		}

		public PluginDef Def {
			get {
				return def;
			}
		}

		public PluginDef TypeDef {
			get {
				return def;
			}
		}

		public PluginHolder Cont {
			get {
				return cont;
			}
		}

		//the first trigger that throws an exceptions terminates the other ones that way
		public object Run(TriggerKey tk, ScriptArgs sa) {
			if (!isDeleted) {
				object retVal = null;
				TriggerGroup scriptedTriggers = this.def.scriptedTriggers;
				if (scriptedTriggers != null) {
					retVal = scriptedTriggers.Run(this, tk, sa);
				}

				PluginDef.PluginTriggerGroup compiledTriggers = def.compiledTriggers;
				if (compiledTriggers != null) {
					retVal = compiledTriggers.Run(this, tk, sa);
				}
				return retVal;
			}
			return null;
		}

		//does not throw the exceptions - all triggers are run, regardless of their errorness
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		public object TryRun(TriggerKey tk, ScriptArgs sa) {
			object retVal = null;
			if (!isDeleted) {
				TriggerGroup scriptedTriggers = this.def.scriptedTriggers;
				if (scriptedTriggers != null) {
					retVal = scriptedTriggers.TryRun(this, tk, sa);
				}
				PluginDef.PluginTriggerGroup compiledTriggers = def.compiledTriggers;
				if (compiledTriggers != null) {
					try {
						retVal = compiledTriggers.Run(this, tk, sa);
					} catch (FatalException) {
						throw;
					} catch (Exception e) {
						Logger.WriteError(e);
					}
				}
			}
			return retVal;
		}

		public override void Delete() {
			if (cont != null) {
				cont.RemovePlugin(this);
			}
			if (!isDeleted) {
				this.TryRun(TriggerKey.destroy, null);
				isDeleted = true;
			}
			base.Delete();
		}

		#region save/load
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods"), Save]
		public override void Save(SaveStream output) {
			output.WriteValue("def", this.def);
			base.Save(output);
		}

		[LoadLine]
		public override void LoadLine(string filename, int line, string valueName, string valueString) {
			if ("def".Equals(valueName, StringComparison.OrdinalIgnoreCase)) {
				this.def = (PluginDef) ObjectSaver.OptimizedLoad_Script(valueString);
			} else {
				base.LoadLine(filename, line, valueName, valueString);
			}
		}
		#endregion save/load
	}
}
