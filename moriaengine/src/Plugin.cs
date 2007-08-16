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
	public abstract class Plugin : TagHolder {
		internal PluginHolder cont;

		internal Plugin prevInList;
		internal Plugin nextInList;

		PluginDef def;

		protected Plugin() {
			
		}

		protected Plugin(PluginDef def) {
			this.def = def;
		}

		public PluginDef Def {
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
		public abstract object Run(TriggerKey tk, ScriptArgs sa);
		
		//does not throw the exceptions - all triggers are run, regardless of their errorness
		public abstract object TryRun(TriggerKey tk, ScriptArgs sa);

		public override void Delete() {
			this.TryRun(TriggerKey.destroy, null);
			base.Delete();
		}

		protected internal override void BeingDeleted() {
			if (cont != null) {
				cont.RemovePlugin(this);
			}

			base.BeingDeleted();
		}
	}
}
