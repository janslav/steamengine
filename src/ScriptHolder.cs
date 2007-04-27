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
using System.Collections;
using System.Reflection;
using System.Globalization;
using SteamEngine.Packets;
using SteamEngine.Common;
using SteamEngine.LScript;
	
namespace SteamEngine {
	public abstract class ScriptHolder {
		public readonly string name;
		internal bool unloaded = false;
		internal TriggerGroup myTriggerGroup;
		
		internal bool lastRunSuccesful = false;
		internal Exception lastRunException;
		
		private static Hashtable functionsByName = new Hashtable(StringComparer.OrdinalIgnoreCase);
		
		public static ScriptHolder GetFunction(string name) {
			return (ScriptHolder) functionsByName[name];
		}
	
		protected ScriptHolder(string name) {
			this.name=name;
		}
		
		internal void RegisterAsFunction() {
			if (functionsByName[name] == null) {
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
			if (myTriggerGroup == null) {
				return name;
			} else {
				return myTriggerGroup.Defname+": @"+name;
			}
		}
		
	}
}		