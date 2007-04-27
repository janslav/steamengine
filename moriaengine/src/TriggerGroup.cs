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
using System.Text.RegularExpressions;
using System.Globalization;
using SteamEngine.Packets;
using SteamEngine.LScript;
using SteamEngine.Common;
using SteamEngine.CompiledScripts;
	
namespace SteamEngine {
	public class TriggerGroup : AbstractScript {
		//private static Hashtable byDefname = new Hashtable(StringComparer.OrdinalIgnoreCase);
		
		//public readonly string defname;
		
		private	Hashtable triggers = new Hashtable();

		private TriggerGroup(string defname) : base(defname) {
			this.remover = new TGRemover(this);
		}
		
		//the first trigger that throws an exceptions terminates the other ones that way
		public object Run(object self, TriggerKey tk, ScriptArgs sa) {
			ScriptHolder sd = (ScriptHolder) (triggers[tk]);
			if (sd != null) {
				return sd.Run(self, sa);
			} else {
				return null;	//This triggerGroup does not contain that trigger
			}
		}
		
		//does not throw the exceptions - all triggers are run, regardless of their errorness
		public object TryRun(object self, TriggerKey tk, ScriptArgs sa) {
			ScriptHolder sd = (ScriptHolder) (triggers[tk]);
			if (sd != null) {
				return sd.TryRun(self, sa);
			} else {
				return null;	//This triggerGroup does not contain that trigger
			}
		}
		
		internal void AddTrigger(ScriptHolder sd) {
			string name=sd.name;
			//Console.WriteLine("Adding trigger {0} to tg {1}", name, this);
			TriggerKey tk = TriggerKey.Get(name);
			if (triggers[tk]!=null) {
				Logger.WriteError("Attempted to declare triggers of the same name ("+LogStr.Ident(name)+") in trigger-group "+LogStr.Ident(this.Defname)+"!");
				return;
			}
			sd.myTriggerGroup = this;
			triggers[tk]=sd;
		}
		
		public override string ToString() {
			return "TriggerGroup "+defname;
		}
		
		public static new TriggerGroup Get(string name) {
			AbstractScript script;
			byDefname.TryGetValue(name, out script);
			return script as TriggerGroup;
		}
	
		private static Regex globalNameRE = new Regex(@"^.*_all(?<value>[a-z][0-9a-z]+)s\s*$",
			RegexOptions.IgnoreCase|RegexOptions.CultureInvariant|RegexOptions.Compiled);
			
		internal static TriggerGroup GetNewOrCleared(string defname) {
			TriggerGroup tg = Get(defname);
			if (tg == null) {
				tg = new TriggerGroup(defname);
				byDefname[defname]=tg;
				
				//register it as 'global' triggergroup for some class, like *_allitems gets sent to Item.RegisterTriggerGroup()
				Match m= globalNameRE.Match(defname);
				if (m.Success) {
					
					string typeName = m.Groups["value"].Value;
					if (string.Compare(typeName, "account", true) == 0) {
						typeName = "GameAccount";
					}
					
					MethodInfo wrapper = ClassManager.GetRegisterTGmethod(typeName);
					if (wrapper != null) {
						wrapper.Invoke(null, new object[] {tg});
					}
				}
				if (defname.ToLower().EndsWith("_global")) {
					Globals.instance.AddTriggerGroup(tg);
				}
				return tg;
			} else if (tg.unloaded) {
				return tg;
			}
			throw new OverrideNotAllowedException("TriggerGroup "+LogStr.Ident(defname)+" defined multiple times.");
		}
		
		internal static void StartingLoading() {
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
		
		public static TriggerGroup Load(PropsSection input) {
			TriggerGroup group = GetNewOrCleared(input.headerName);
			for (int i = 0, n = input.TriggerCount; i<n; i++) {
				ScriptHolder sc = new LScriptHolder(input.GetTrigger(i));
				if (!sc.unloaded) {//in case the compilation failed, we do not add the trigger
					group.AddTrigger(sc);
				}
			}
			return group;
		}
		
		internal static void LoadingFinished() {
			//dump the number of groups loaded?
		}
		
		public override void Unload() {
			triggers.Clear();

			base.Unload();
		}
		
		internal static new void UnloadAll() {
			byDefname.Clear();
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
		internal TriggerGroup tg;
		internal TGRemover(TriggerGroup tg) {
			this.tg = tg;
		}
	}
}