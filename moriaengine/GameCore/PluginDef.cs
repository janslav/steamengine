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
	public abstract class PluginDef : AbstractDef {

		private static Dictionary<Type, Type> pluginDefTypesByPluginType = new Dictionary<Type, Type>();
		private static Dictionary<Type, Type> pluginTypesByPluginDefType = new Dictionary<Type, Type>();
		private static Dictionary<string, Type> pluginDefTypesByName = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
		private static Dictionary<Type, ConstructorInfo> pluginDefCtors = new Dictionary<Type, ConstructorInfo>();

		private static Dictionary<Type, PluginTriggerGroup> triggerGroupsByType = new Dictionary<Type, PluginTriggerGroup>();


		protected PluginDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			{
				TryActivateCompiledTriggers();
			}
		}

		private void TryActivateCompiledTriggers() {
			PluginTriggerGroup ptg;
			if (triggerGroupsByType.TryGetValue(this.GetType(), out ptg)) {
				this.compiledTriggers = ptg;
			}
		}

		internal TriggerGroup scriptedTriggers;
		internal protected PluginTriggerGroup compiledTriggers;

		public abstract class PluginTriggerGroup {
			public abstract object Run(Plugin self, TriggerKey tk, ScriptArgs sa);
		}

		protected abstract Plugin CreateImpl();

		public Plugin Create() {
			Plugin p = this.CreateImpl();
			p.def = this;
			return p;
		}

		public static new PluginDef Get(string defname) {
			AbstractScript script;
			byDefname.TryGetValue(defname, out script);
			return script as PluginDef;
		}

		public static Type GetDefTypeByName(string name) {
			Type defType;
			pluginDefTypesByName.TryGetValue(name, out defType);
			return defType;
		}

		public static new bool ExistsDefType(string name) {
			return pluginDefTypesByName.ContainsKey(name);
		}

		//checking type when loading...
		public static Type GetDefTypeByPluginType(Type pluginType) {//
			Type defType;
			pluginDefTypesByPluginType.TryGetValue(pluginType, out defType);
			return defType;
		}

		public static void RegisterPluginTG(Type defType, PluginTriggerGroup tg) {
			triggerGroupsByType[defType] = tg;
		}

		private static Type[] pluginDefConstructorParamTypes = new Type[] { typeof(string), typeof(string), typeof(int) };

		//this should be typically called by the Bootstrap methods of scripted PluginDef
		public static void RegisterPluginDef(Type pluginDefType, Type pluginType) {
			Type t;
			if (pluginDefTypesByPluginType.TryGetValue(pluginDefType, out t)) {
				throw new OverrideNotAllowedException("PluginDef type " + LogStr.Ident(pluginDefType.FullName) + " already has it's Plugin type -" + t.FullName + ".");
			}
			if (pluginTypesByPluginDefType.TryGetValue(pluginType, out t)) {
				throw new OverrideNotAllowedException("Plugin type " + LogStr.Ident(pluginType.FullName) + " already has it's PluginDef type -" + t.FullName + ".");
			}

			ConstructorInfo ci = pluginDefType.GetConstructor(pluginDefConstructorParamTypes);
			if (ci == null) {
				throw new SEException("Proper constructor not found.");
			}
			pluginDefTypesByPluginType[pluginType] = pluginDefType;
			pluginTypesByPluginDefType[pluginDefType] = pluginType;
			pluginDefTypesByName[pluginDefType.Name] = pluginDefType;
			pluginDefCtors[pluginDefType] = MemberWrapper.GetWrapperFor(ci);
		}

		internal static IUnloadable LoadFromScripts(PropsSection input) {
			Type pluginDefType = null;
			string typeName = input.headerType.ToLower();
			string defname = input.headerName.ToLower();
			//Console.WriteLine("loading section "+input.HeadToString());
			//[typeName defname]

			pluginDefType = PluginDef.GetDefTypeByName(typeName);
			if (pluginDefType == null) {
				throw new SEException("Type " + LogStr.Ident(typeName) + " does not exist.");
			}

			if (pluginDefType.IsAbstract) {
				throw new SEException("The PluginDef Type " + LogStr.Ident(pluginDefType) + " is abstract, a.e. not meant to be directly used in scripts this way. Ignoring.");
			}

			AbstractScript def;
			byDefname.TryGetValue(defname, out def);
			PluginDef pluginDef = def as PluginDef;

			if (pluginDef == null) {
				if (def != null) {//it isnt pluginDef
					throw new OverrideNotAllowedException("PluginDef " + LogStr.Ident(defname) + " has the same name as " + LogStr.Ident(def));
				} else {
					ConstructorInfo cw = pluginDefCtors[pluginDefType];
					pluginDef = (PluginDef) cw.Invoke(BindingFlags.Default, null, new object[] { defname, input.filename, input.headerLine }, null);
				}
			} else if (pluginDef.unloaded) {
				if (pluginDef.GetType() != pluginDefType) {
					throw new OverrideNotAllowedException("You can not change the class of a Plugindef while resync. You have to recompile or restart to achieve that. Ignoring.");
				}
				pluginDef.unloaded = false;
				byDefname.Remove(pluginDef.Defname);//will be put back again
			} else {
				throw new OverrideNotAllowedException("PluginDef " + LogStr.Ident(defname) + " defined multiple times. Ignoring.");
			}

			pluginDef.defname = defname;
			byDefname[defname] = pluginDef;

			//header done. now we have the def instantiated.
			//now load the other fields
			pluginDef.LoadScriptLines(input);

			//now do load the trigger code. 
			if (input.TriggerCount > 0) {
				input.headerName = "t__" + input.headerName + "__";
				pluginDef.scriptedTriggers = ScriptedTriggerGroup.Load(input);
			}
			if (pluginDef.scriptedTriggers == null) {
				return pluginDef;
			} else {
				return new UnloadableGroup(pluginDef, pluginDef.scriptedTriggers);
			}
		}

		internal static void ClearAll() {
			pluginDefTypesByPluginType.Clear();
			pluginTypesByPluginDefType.Clear();//we can assume that inside core there are no non-abstract thingdefs
			pluginDefTypesByName.Clear();
			pluginDefCtors.Clear();
			triggerGroupsByType.Clear();
		}

		internal static void Init() {
			foreach (AbstractScript script in AbstractScript.byDefname.Values) {
				PluginDef pd = script as PluginDef;
				if (pd != null) {
					pd.TryActivateCompiledTriggers();
				}
			}
		}
	}
}
