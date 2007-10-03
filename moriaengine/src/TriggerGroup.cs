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
	public abstract class TriggerGroup : AbstractScript {
		protected TriggerGroup() : base() {
			Init(this.defname);
		}

		protected TriggerGroup(string defname)
			: base(defname) {
			Init(defname);
		}
	
		private static Regex globalNameRE = new Regex(@"^.*_all(?<value>[a-z][0-9a-z]+)s\s*$",
			RegexOptions.IgnoreCase|RegexOptions.CultureInvariant|RegexOptions.Compiled);

		private void Init(string defname) {
			this.remover = new TGRemover(this);

			//register it as 'global' triggergroup for some class, like *_allitems gets sent to Item.RegisterTriggerGroup()
			Match m = globalNameRE.Match(defname);
			if (m.Success) {
				string typeName = m.Groups["value"].Value;
				if (string.Compare(typeName, "account", true) == 0) {
					typeName = "GameAccount";
				}

				RegisterTGDeleg wrapper = ClassManager.GetRegisterTGmethod(typeName);
				if (wrapper != null) {
					wrapper(this);
				}
			}
			if (defname.ToLower().EndsWith("_global")) {
				Globals.instance.AddTriggerGroup(this);
			}
		}
		
		//the first trigger that throws an exceptions terminates the other ones that way
		public abstract object Run(object self, TriggerKey tk, ScriptArgs sa);
		
		//does not throw the exceptions - all triggers are run, regardless of their errorness
		public abstract object TryRun(object self, TriggerKey tk, ScriptArgs sa);

		public override string ToString() {
			return "TriggerGroup "+defname;
		}
		
		public static new TriggerGroup Get(string name) {
			AbstractScript script;
			byDefname.TryGetValue(name, out script);
			return script as TriggerGroup;
		}
		
		internal static void ReAddGlobals() {
			foreach (AbstractScript script in byDefname.Values) {
				TriggerGroup tg = script as TriggerGroup;
				if (tg != null) {
					if (tg.Defname.ToLower().EndsWith("_global")) {
						Globals.instance.AddTriggerGroup(tg);
					}
				}
			}
		}

		//operators for simulation of spherescipt-like addition/removing of events. 
		//Look at TagHolder`s Events() overloads
		public static TriggerGroup operator +(TriggerGroup tg) {
			return tg;
		}
    	
    	private TGRemover remover;
    	
		public static TGRemover operator -(TriggerGroup tg) {
			return tg.remover;
		}
	}
	
	public class TGRemover {
		public readonly TriggerGroup tg;
		public TGRemover(TriggerGroup tg) {
			this.tg = tg;
		}
	}
}