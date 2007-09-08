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
using System.Collections.Generic;
using System.Reflection;
using System.Globalization;
using System.Text.RegularExpressions;
using SteamEngine.Packets;
using SteamEngine.Common;
using SteamEngine.Persistence;

namespace SteamEngine {
	public interface ITriggerGroupHolder {
		void AddTriggerGroup(TriggerGroup tg);
		void ClearTriggerGroups();
		bool HasTriggerGroup(TriggerGroup tg);
		void RemoveTriggerGroup(TriggerGroup tg);

		void Trigger(TriggerKey td, ScriptArgs sa);
		void TryTrigger(TriggerKey td, ScriptArgs sa);
		bool CancellableTrigger(TriggerKey td, ScriptArgs sa);
		bool TryCancellableTrigger(TriggerKey td, ScriptArgs sa);

		IEnumerable<TriggerGroup> AllTriggerGroups { get; }
	}

	public interface IPluginHolder : ITriggerGroupHolder {
		Plugin AddPlugin(PluginKey pg, Plugin plugin);
		Plugin AddPluginAsSimple(PluginKey pg, Plugin plugin);
		void DeletePlugins();
		Plugin GetPlugin(PluginKey pg);
		bool HasPlugin(PluginKey pg);
		bool HasPlugin(Plugin plugin);
		Plugin RemovePlugin(Plugin plugin);
		Plugin RemovePlugin(PluginKey pg);

		IEnumerable<Plugin> AllPlugins { get; }
		IEnumerable<Plugin> AllNonSimplePlugins { get; }
	}

	/*
		Class: TagHolder
		All Things (Items and Characters) and GameAccounts are TagHolders, as is Server.globals.
	*/
	public class PluginHolder : TagHolder, IPluginHolder {
		private TGListNode firstTGListNode = null;	//double-linked list of triggergroup references. We only have it for fast "foreach" operation in Trigger methods. Lookup goes thru tags Hashtable
		private Plugin firstPlugin = null;			//double-linked list of Plugins. We only have it for fast "foreach" operation in Trigger methods. Lookup goes thru tags Hashtable

		public PluginHolder() {
		}

		public PluginHolder(PluginHolder copyFrom) : base(copyFrom) { //copying constuctor
			if (copyFrom.firstTGListNode != null) {
				EnsureTagsTable();
				TGListNode curNode = new TGListNode(copyFrom.firstTGListNode.storedTG);
				firstTGListNode = curNode;
				TGListNode copiedNode = copyFrom.firstTGListNode.nextNode;
				while (copiedNode != null) {
					curNode.nextNode = new TGListNode(copiedNode.storedTG);
					tags[copiedNode.storedTG] = curNode.nextNode;
					curNode.nextNode.prevNode = curNode;
					curNode = curNode.nextNode;
					copiedNode = copiedNode.nextNode;
				}
			}
		}

		protected internal override void BeingDeleted() {
			this.DeletePlugins();
			base.BeingDeleted();
		}

		#region Triggergroups

		public void AddTriggerGroup(TriggerGroup tg) {
			if (tg == null) 
				return;
			if (tags != null) {
				if (tags.ContainsKey(tg)) {
					return;
				}
			} else {
				tags = new Hashtable();
			}

			TGListNode listNode = new TGListNode(tg);
			tags[tg] = listNode;
			if (firstTGListNode != null) {
				firstTGListNode.prevNode = listNode;
				listNode.nextNode = firstTGListNode;
			}
			firstTGListNode = listNode;
			tg.TryRun(this, TriggerKey.assign, null);
		}
		
		public void RemoveTriggerGroup(TriggerGroup tg) {
			if (tg == null) 
				return;
			if (tags != null) {
				TGListNode listNode = tags[tg] as TGListNode;
				if (listNode != null) {
					if (listNode.prevNode != null) {
						listNode.prevNode.nextNode = listNode.nextNode;
					} else {//no prev, means we were the first
						firstTGListNode = listNode.nextNode;
					}
					if (listNode.nextNode != null) {
						listNode.nextNode.prevNode = listNode.prevNode;
					}
					tg.TryRun(this, TriggerKey.unAssign, null);
				}
			}
		}
		
		public bool HasTriggerGroup(TriggerGroup tg) {
			if (tags != null) {
				return tags.ContainsKey(tg);
			}
			return false;
		}

		public IEnumerable<TriggerGroup> AllTriggerGroups {
			get {
				TGListNode curNode = firstTGListNode;
				while (curNode != null) {
					yield return curNode.storedTG;
					curNode = curNode.nextNode;
				}
			}
		}
	
		public void ClearTriggerGroups() {
			TGListNode curNode = firstTGListNode;
			while (curNode != null) {
				tags.Remove(curNode.storedTG);
				curNode = curNode.nextNode;
			}
			firstTGListNode = null;
		}

		public class TGListNode {
			public readonly TriggerGroup storedTG;
			internal TGListNode prevNode = null;
			internal TGListNode nextNode = null;
		
			internal TGListNode(TriggerGroup storedTG) {
				this.storedTG = storedTG;
			}
		}

		#endregion Triggergroups

		#region Trigger() methods
		/*
			Method: Trigger
			Triggers a trigger on this object, using the specified ScriptArgs
			
			Parameters:
				sa - The arguments (other than argv) for sphere scripts
				td - The TriggerKey for the trigger to call.
		
			Returns:
				The return value(s) from the triggers called.
		
			See also:
				<Trigger>, <CancellableTriggers>
		*/
		public virtual void Trigger(TriggerKey tk, ScriptArgs sa) {
			TGListNode curNode = firstTGListNode;
			while (curNode != null) {
				curNode.storedTG.Run(this, tk, sa);
				curNode = curNode.nextNode;
			}
			Plugin curPlugin = firstPlugin;
			while (curPlugin != null) {
				curPlugin.Run(tk, sa);
				curPlugin = curPlugin.nextInList;
			}
		}
		
		public virtual void TryTrigger(TriggerKey tk, ScriptArgs sa) {
			TGListNode curNode = firstTGListNode;
			while (curNode != null) {
				curNode.storedTG.TryRun(this, tk, sa);
				curNode = curNode.nextNode;
			}
			Plugin curPlugin = firstPlugin;
			while (curPlugin != null) {
				curPlugin.TryRun(tk, sa);
				curPlugin = curPlugin.nextInList;
			}
		}
		
		/*
			Method: CancellableTrigger
			Executes the trigger, reads return values, and returns true if anything returned 1 (returning false otherwise).
			
			Parameters:
				td - The trigger to execute
				sa - Arguments for scripts (argn, args, argo, argn1, argn2, etc). Can be null.
			
			Returns:
				True if any called trigger scripts returned 1, false otherwise.
			
			See also:
				<Trigger>, <CancellableTriggers>
		*/

		private static bool Is1(object o) {
			try {
				if (ConvertTools.ToInt32(o) == 1) {
					return true;
				}
			} catch { }
			return false;
		}
		
		public virtual bool CancellableTrigger(TriggerKey tk, ScriptArgs sa) {
			TGListNode curNode = firstTGListNode;
			while (curNode != null) {
				if (Is1(curNode.storedTG.Run(this, tk, sa))) {
					return true;
				}
				curNode = curNode.nextNode;
			}
			Plugin curPlugin = firstPlugin;
			while (curPlugin != null) {
				if (Is1(curPlugin.Run(tk, sa))) {
					return true;
				}
				curPlugin = curPlugin.nextInList;
			}
			return false;
		}

		public virtual bool TryCancellableTrigger(TriggerKey tk, ScriptArgs sa) {
			TGListNode curNode = firstTGListNode;
			while (curNode != null) {
				if (Is1(curNode.storedTG.TryRun(this, tk, sa))) {
					return true;
				}
				curNode = curNode.nextNode;
			}
			Plugin curPlugin = firstPlugin;
			while (curPlugin != null) {
				if (Is1(curPlugin.TryRun(tk, sa))) {
					return true;
				}
				curPlugin = curPlugin.nextInList;
			}
			return false;
		}
		#endregion Trigger() methods

		#region save/load
		public override void Save(SaveStream output) {
			TGListNode curNode = firstTGListNode;
			while (curNode != null) {
				output.WriteValue("TriggerGroup", curNode.storedTG);
				curNode = curNode.nextNode;
			}
			if (tags != null) {
				foreach (DictionaryEntry entry in tags) {
					object key = entry.Key;
					if (key is PluginKey) {
						Plugin value = (Plugin) entry.Value;
						if ((value == this.firstPlugin) || (value.prevInList != null) || (value.nextInList != null)) {
							output.WriteValue("@@"+key.ToString(), value);
						} else {
							output.WriteValue("@@"+key.ToString()+"*", value);
						}
					}
				}
			}
			base.Save(output);
		}

		public static Regex pluginKeyRE = new Regex(@"^\@@(?<name>.+?)(?<asterisk>\*)?\s*$", RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.IgnoreCase);

		public override void LoadLine(string filename, int line, string name, string value) {
			Match m = pluginKeyRE.Match(name);
			if (m.Success) {	//If the name begins with '@@'
				string pluginName = m.Groups["name"].Value;
				PluginKey tk = PluginKey.Get(pluginName);
				if (m.Groups["asterisk"].Value.Length > 0) {
					ObjectSaver.Load(value, DelayedLoad_SimplePlugin, filename, line, tk);
				} else {
					ObjectSaver.Load(value, DelayedLoad_Plugin, filename, line, tk);
				}
				return;
			}

			switch (name) {
				case "events":
				case "event":
				case "triggergroup":
				case "type":
					string tgName;
					m= ObjectSaver.abstractScriptRE.Match(value);
					if (m.Success) {
						tgName = m.Groups["value"].Value;
					} else {
						tgName = value;
					}
					TriggerGroup tg = TriggerGroup.Get(tgName);
					if (tg != null) {
						AddTriggerGroup(tg);
					} else {
						throw new Exception("TriggerGroup '"+tgName+"' does not exist.");
					}
					return;
			}
			base.LoadLine(filename, line, name, value);
		}

		private void DelayedLoad_Plugin(object resolvedObject, string filename, int line, object pluginKey) {
			Plugin plugin = (Plugin) resolvedObject;
			AddPluginImpl((PluginKey) pluginKey, plugin);
			if (firstPlugin != null) {
				firstPlugin.prevInList = plugin;
				plugin.nextInList = firstPlugin;
			}
			firstPlugin = plugin;
		}

		private void DelayedLoad_SimplePlugin(object resolvedObject, string filename, int line, object pluginKey) {
			AddPluginImpl((PluginKey) pluginKey, (Plugin) resolvedObject);
		}

		#endregion save/load

		public Plugin AddNewPlugin(PluginKey key, PluginDef def) {
			return AddPlugin(key, def.Create());
		}

		public Plugin AddNewPluginAsSimple(PluginKey key, PluginDef def) {
			return AddPluginAsSimple(key, def.Create());
		}

		#region IPluginHolder Members
		public Plugin AddPlugin(PluginKey pk, Plugin plugin) {
			AddPluginImpl(pk, plugin);
			if (firstPlugin != null) {
				firstPlugin.prevInList = plugin;
				plugin.nextInList = firstPlugin;
			}
			firstPlugin = plugin;
			plugin.TryRun(TriggerKey.assign, null);
			return plugin;
		}

		public Plugin AddPluginAsSimple(PluginKey pk, Plugin plugin) {
			AddPluginImpl(pk, plugin);
			plugin.TryRun(TriggerKey.assign, null);
			return plugin;
		}

		private void AddPluginImpl(PluginKey pk, Plugin plugin) {
			if (tags != null) {
				PluginKey prevKey = tags[plugin] as PluginKey;
				if (prevKey != null && prevKey != pk) {
					throw new Exception("You can't assign one Plugin to one PluginHolder under 2 different PluginKeys");
				}

				Plugin prevPlugin = tags[pk] as Plugin;
				if (prevPlugin != null && prevPlugin != plugin) {
					this.RemovePlugin(prevPlugin).Delete();
				}
			} else {
				tags = new Hashtable();
			}
			tags[pk] = plugin;
			tags[plugin] = pk;
			plugin.cont = this;
		}

		public void DeletePlugins() {
			if (tags != null) {
				List<DictionaryEntry> allPlugins = new List<DictionaryEntry>();
				foreach (DictionaryEntry entry in tags) {
					if (entry.Key is PluginKey) {
						allPlugins.Add(entry);
					}
				}
				foreach (DictionaryEntry entry in allPlugins) {
					RemovePluginImpl((PluginKey) entry.Key, (Plugin) entry.Value).Delete();
				}
			}
		}

		public bool HasPlugin(PluginKey pg) {
			if (tags != null) {
				return tags.ContainsKey(pg);
			}
			return false;
		}

		public Plugin GetPlugin(PluginKey pg) {
			if (tags != null) {
				return tags[pg] as Plugin;
			}
			return null;
		}

		public bool HasPlugin(Plugin plugin) {
			return plugin.cont == this;
		}

		public Plugin RemovePlugin(Plugin plugin) {
			if (tags != null) {
				PluginKey pg = tags[plugin] as PluginKey;
				if (pg != null) {
					return RemovePluginImpl(pg, plugin);
				}
			}
			return null;
		}

		public Plugin RemovePlugin(PluginKey pg) {
			if (tags != null) {
				Plugin plugin = tags[pg] as Plugin;
				if (plugin != null) {
					return RemovePluginImpl(pg, plugin);
				}
			}
			return null;
		}

		private Plugin RemovePluginImpl(PluginKey pg, Plugin plugin) {
			tags.Remove(pg);
			tags.Remove(plugin);
			if (plugin == firstPlugin) {
				firstPlugin = plugin.nextInList;
			}
			if (plugin.nextInList != null) {
				plugin.nextInList.prevInList = plugin.prevInList;
			}
			if (plugin.prevInList != null) {
				plugin.prevInList.nextInList = plugin.nextInList;
			}
			plugin.cont = null;
			plugin.prevInList = null;
			plugin.nextInList = null;
			plugin.TryRun(TriggerKey.unAssign, new ScriptArgs(this));
			return plugin;
		}

		public IEnumerable<Plugin> AllNonSimplePlugins {
			get {
				Plugin curPlugin = firstPlugin;
				while (curPlugin != null) {
					yield return curPlugin;
					curPlugin = curPlugin.nextInList;
				}
			}
		}

		public IEnumerable<Plugin> AllPlugins {
			get {
				if (tags != null) {
					foreach (DictionaryEntry entry in tags) {
						Plugin p = entry.Key as Plugin;
						if (p != null) {
							yield return p;
						}
					}
				}
			}
		}
		#endregion
	}
}