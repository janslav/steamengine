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
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using SteamEngine;
using SteamEngine.Common;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {
	public class MemoryDef : AbstractDefTriggerGroupHolder {
		//private FieldValue type, name;

		private static Dictionary<string, ConstructorInfo> constructors = new Dictionary<string, ConstructorInfo>(StringComparer.OrdinalIgnoreCase);
			
		public MemoryDef(string defname, string filename, int headerLine) : base (defname, filename, headerLine) {   
			//name = InitField_Typed("name", defname, typeof(string));
			//type = InitField_Typed("type", "", typeof(TriggerGroup));
		}
		
		public Memory Create(Character cont) {
			Memory memory = Create();
			cont.Equip(memory);
			return memory;
		}
		
		public virtual Memory Create() {
			return new Memory(this);
		}
		
		public static new MemoryDef Get(string defname) {
			AbstractScript script;
			byDefname.TryGetValue(defname, out script);
			return script as MemoryDef;
		}
		
		internal static IUnloadable LoadFromScripts(PropsSection input) {
			string memoryDefClassName = input.headerType;

			Type mdType;
			ConstructorInfo constructor;
			if (!constructors.TryGetValue(memoryDefClassName, out constructor)) {
				if (!defTypesByName.TryGetValue(memoryDefClassName, out mdType)) {
					Logger.WriteError(input.filename, input.headerLine, "No MemoryDef subclass '"+memoryDefClassName+"' found.");
					return null;
				} else {
					constructor = MemoryDef.RegisterSubType(mdType);
				}
			} else {
				mdType = constructor.DeclaringType;
			}

			if (constructor == null) {
				Logger.WriteError(input.filename, input.headerLine, "There is no proper MemoryDef subclass constructor for this section...");
				return null;
			}

			

			string defname = input.headerName.ToLower();

			MemoryDef def = Get(defname);
			if (def != null) {
				if (def.unloaded) {
					if (def.GetType() != mdType) {
						throw new OverrideNotAllowedException("You can not change the class of a Memorydef while resync. You have to recompile or restart to achieve that. Ignoring.");
					}
					def.unloaded = false;
				} else {
					throw new OverrideNotAllowedException(LogStr.Ident(def.PrettyDefname)+" defined multiple times.");
				}
			} else {
				def = (MemoryDef) constructor.Invoke(BindingFlags.Default, null, new object[] { defname, input.filename, input.headerLine }, null);
			}
			def.defname = defname;
			byDefname[def.Defname]=def;
			
			def.ClearTriggerGroups();//maybe clear other things too?
					
			//header done. now we have the def instantiated.
			//now load the other fields
			def.LoadScriptLines(input);
			
			//now do load the trigger code. 
			if (input.TriggerCount>0) {
				input.headerName = "t__"+input.headerName+"__";
				TriggerGroup tg = ScriptedTriggerGroup.Load(input);
				def.AddTriggerGroup(tg);
			}
			return def;
		}
		
		public static void Bootstrap() {
			ScriptLoader.RegisterScriptType("memorydef", new LoadSection(LoadFromScripts), false);
		}

		private static Type[] memoryDefConstructorParamTypes = new Type[] { typeof(string), typeof(string), typeof(int) };

		//called by ClassManager
		internal static ConstructorInfo RegisterSubType(Type type) {
			ConstructorInfo match = type.GetConstructor(memoryDefConstructorParamTypes);

			string name = type.Name;
			if (match!=null) {
				ConstructorInfo retVal = MemberWrapper.GetWrapperFor(match);
				constructors[name]=retVal;
				return retVal;
			}
			return null;
		}

		internal void Trigger(Memory self, TriggerKey td, ScriptArgs sa) {
			foreach (TriggerGroup tg in this.AllTriggerGroups) {
				tg.Run(self, td, sa);
			}
		}

		internal void TryTrigger(Memory self, TriggerKey td, ScriptArgs sa) {
			foreach (TriggerGroup tg in this.AllTriggerGroups) {
				tg.TryRun(self, td, sa);
			}
		}

		internal bool CancellableTrigger(Memory self, TriggerKey td, ScriptArgs sa) {
			foreach (TriggerGroup tg in this.AllTriggerGroups) {
				object retVal = tg.Run(self, td, sa);
				try {
					int retInt = Convert.ToInt32(retVal);
					if (retInt == 1) {
						return true;
					}
				} catch (Exception) {
				}
			}
			return false;
		}

		internal bool TryCancellableTrigger(Memory self, TriggerKey td, ScriptArgs sa) {
			foreach (TriggerGroup tg in this.AllTriggerGroups) {
				object retVal = tg.TryRun(self, td, sa);
				try {
					int retInt = Convert.ToInt32(retVal);
					if (retInt == 1) {
						return true;
					}
				} catch (Exception) {
				}
			}
			return false;
		}
	}
}