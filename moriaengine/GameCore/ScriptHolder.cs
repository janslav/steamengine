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
using System.Globalization;
using SteamEngine.Packets;
using SteamEngine.Common;
using SteamEngine.LScript;
	
namespace SteamEngine {
	public abstract class ScriptHolder {
		public readonly string name;
		internal bool unloaded = false;
		internal TriggerGroup contTriggerGroup;
		
		internal bool lastRunSuccesful = false;
		internal Exception lastRunException;
		
		private static Dictionary<string,ScriptHolder> functionsByName = new Dictionary<string,ScriptHolder>(StringComparer.OrdinalIgnoreCase);
		
		public static ScriptHolder GetFunction(string name) {
			ScriptHolder sh;
			functionsByName.TryGetValue(name, out sh);
			return sh;
		}
	
		protected ScriptHolder(string name) {
			if (String.IsNullOrEmpty(name)) {
				this.name = this.GetName();
			}
			this.name = name;
		}
		
		protected ScriptHolder() {
			this.name = this.GetName();
		}

		protected virtual string GetName() {
			throw new Exception("This should not happen");
		}

		internal protected void RegisterAsFunction() {
			if (!functionsByName.ContainsKey(name)) {
				functionsByName[name] = this;
				return;
			}
			throw new ServerException("ScriptHolder '"+name+"' already exists; Cannot create a new one with the same name.");
		}
		
		internal static void UnloadAll() {
			functionsByName.Clear();
		}
		
		public abstract object Run(object self, ScriptArgs sa);
		
		public object Run(object self, params object[] args) {
			return Run(self, new ScriptArgs(args));
		}
		
		public object TryRun(object self, ScriptArgs sa) {
			try {
				return Run(self, sa);
			} catch (FatalException) {
				throw;
			} catch (Exception e) {
				Error(e);
			}
			return null;
		}
		
		public object TryRun(object self, params object[] args) {
			return TryRun(self, new ScriptArgs(args));
		}
		protected virtual void Error(Exception e) {
			Logger.WriteError(e);
		}
		
		public string GetDecoratedName() {
			if (contTriggerGroup == null) {
				return name;
			} else {
				return contTriggerGroup.Defname+": @"+name;
			}
		}
		
	}
}		