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
using SteamEngine.Timers;
using SteamEngine.Common;
using SteamEngine.Persistence;

namespace SteamEngine {
	public interface IDeletable {
		bool IsDeleted { get; }
		void Delete();
	}

	public interface ITagHolder {
		object GetTag(TagKey td);
		void SetTag(TagKey tk, object value);
		bool HasTag(TagKey td);
		void RemoveTag(TagKey td);
		void ClearTags();
	}

	public interface ITriggerGroupHolder {
		void AddTriggerGroup(TriggerGroup tg);
		void ClearTriggerGroups();
		bool HasTriggerGroup(TriggerGroup tg);
		void RemoveTriggerGroup(TriggerGroup tg);

		void Trigger(TriggerKey td, ScriptArgs sa);
		void TryTrigger(TriggerKey td, ScriptArgs sa);
		bool CancellableTrigger(TriggerKey td, ScriptArgs sa);
		bool TryCancellableTrigger(TriggerKey td, ScriptArgs sa);

		void Trigger(TriggerKey td, params object[] scriptArguments);
		bool CancellableTrigger(TriggerKey td, params object[] scriptArguments);
	}

	/*
		Class: TagHolder
		All Things (Items and Characters) and GameAccounts are TagHolders, as is Server.globals.
	*/
	public class TagHolder : IDeletable, ITagHolder, ITriggerGroupHolder {
		private Hashtable tags = null; //in this tagholder are stored tags and Timers
		protected TGStoreNode triggerGroups = null; //linked list of triggergroup references
		//used also in ThingDef
		
		public virtual string Name { get {
			return "<tagholder instance>";
		} set {
		
		} }

		[Remark("Return enumerable containing all tags")]
		public IEnumerable AllTags {
			get {
				Hashtable onlyTags = new Hashtable();
				if(tags != null) {
					foreach(DictionaryEntry entry in tags) {
						if(entry.Key is TagKey) {
							onlyTags.Add(entry.Key, entry.Value);
						}
					}
				}
				return onlyTags;
			}
		}

		[Remark("Return enumerable containing all timers")]
		public IEnumerable AllTimers {
			get {
				Hashtable onlyTimers = new Hashtable();
				if(tags != null) {
					foreach(DictionaryEntry entry in tags) {
						if(entry.Key is TimerKey) {
							onlyTimers.Add(entry.Key,entry.Value);
						}
					}
				}
				return onlyTimers;
			}
		}

		public TagHolder() {
		}
		
		public TagHolder(TagHolder copyFrom) { //copying constuctor
			if (copyFrom.tags!=null) {
				tags = new Hashtable();
				foreach (DictionaryEntry entry in copyFrom.tags) {
					tags[entry.Key] = Utility.CopyTagValue(entry.Value);
				}
			}
			if (copyFrom.triggerGroups != null) {
				triggerGroups = new TGStoreNode(copyFrom.triggerGroups.storedTG);
				TGStoreNode curNode = triggerGroups;
				TGStoreNode copiedNode = copyFrom.triggerGroups.nextNode;
				while (copiedNode != null) {
					curNode.nextNode = new TGStoreNode(copiedNode.storedTG);
					curNode = curNode.nextNode;
					copiedNode = copiedNode.nextNode;
				}
			}
		}
		
		//called by Timer after load, do not use otherwise.
		internal Timer AddTimer(Timer timer) {
			if (tags == null) {
				tags = new Hashtable();
			}
			TimerKey tg = timer.name;
			if (tags[tg] == null) {
				tags[tg] = timer;
				return timer;
			} else {
				throw new Exception("Unable to add timer '"+timer+"' to '"+this+"' - it is already added.");
			}
		}

		//this should be called by Timer, do not use otherwise.
		//it does not(!) invalidate the timer
		internal void RemoveTimer(Timer timer) {
			tags.Remove(timer.name);
		}

		/*
			Method: RemoveTimer
			Remove an timer from this tagHolder.
			
			Parameters:
				tk - The TimerKey you passed to Timer constructor when you created it.
		*/
		public void RemoveTimer(TimerKey tk) {
			if (tags != null) {
				Timer t = tags[tk] as Timer;
				if (t != null) {
					t.Remove();
				}
			}
		}
		
		public void ClearTimers() {
			if (tags==null) {
				return;
			}
			List<TimerKey> toBeRemoved = new List<TimerKey>();
			foreach (object keyObj in tags.Keys) {
				TimerKey key = keyObj as TimerKey;
				if (key != null) {
					toBeRemoved.Add(key);
				}
			}
			
			foreach (TimerKey key in toBeRemoved) {
				RemoveTimer(key);
			}
		}
		
		/*
			Method: HasTimer
			Returns true if the timer is on the tagholder, and still exists.
			
			Parameters:
				td - The TimerKey you passed to Timer constructor when you created it.
			
			Returns:
				True if we have this timer, false if not.		
		*/
		
		public bool HasTimer(TimerKey tk) {
			if (tags!=null) {
				return (tags[tk] != null);
			}
			return false;
		}
		
		/*
			Method: GetTimer
			Return the timer pointed to by the specified TimerKey.
			
			Parameters:
				td - The TimerKey specified when you called AddTimer.
		*/
		
		public Timer GetTimer(TimerKey tk) {
			if (tags != null) {
				return (Timer) tags[tk];
			}
			return null;
		}
		
		/*
			Method: AddTriggerGroup
			Returns false if we already have the triggerGroup, true if not.
			
			Parameters:
				tg - The TtriggerGroup to add. 
		*/
		public void AddTriggerGroup(TriggerGroup tg) {
			if (tg == null) return;
			if (triggerGroups == null) {
				triggerGroups = new TGStoreNode(tg);
			} else {
				TGStoreNode curNode = triggerGroups;
				while (true) {
					if (curNode.storedTG == tg) {
						return;// false;//we already have it
					} else if (curNode.nextNode == null) {
						curNode.nextNode = new TGStoreNode(tg);
						return;
					}
					curNode = curNode.nextNode;
				}
				//return true;//we had to add it
			}
		}
		
		public void AddEvent(TriggerGroup tg) {
			AddTriggerGroup(tg);
		}
		
		/*
			Method: RemoveTriggerGroup
			Removes the triggerGroup if we have it.
			
			Parameters:
				tg - The triggerGroup to remove.
		*/
		public void RemoveTriggerGroup(TriggerGroup tg) {
			if (tg == null) return;
			if (triggerGroups != null) {
				if (triggerGroups.storedTG == tg) {
					triggerGroups = triggerGroups.nextNode;
					return;
				}
				TGStoreNode lastNode = triggerGroups;
				TGStoreNode curNode = lastNode.nextNode;
				while (curNode != null) {
					if (curNode.storedTG == tg) {
						lastNode.nextNode = curNode.nextNode;
						return;
					}
					lastNode = curNode;
					curNode = curNode.nextNode;
				}
				//return false;//we didnt have it, so we didnt do anything
			}
		}
		
		public void RemoveEvent(TriggerGroup tg) {
			RemoveTriggerGroup(tg);
		}
		
		/*
			Method: HasTriggerGroup
			Determines if we have this TriggerGroup on us.
			
			Parameters:
				tg - The triggergroup in question.
			
			Returns:
				True if we have it, false if we do not.
		*/
		
		public bool HasTriggerGroup(TriggerGroup tg) {
			if (tg == null) return false;
			TGStoreNode curNode = triggerGroups;
			do {
				if (curNode.storedTG == tg) {
					return true;
				}
				curNode = curNode.nextNode;
			} while (curNode != null);
			return false;
		}
		
		public bool HasEvent(TriggerGroup tg) {
			return HasTriggerGroup(tg);
		}
		
		public void ClearTriggerGroups() {
			triggerGroups = null;
		}
		
		public class TGStoreNode {
			public TriggerGroup storedTG;
			public TGStoreNode nextNode = null;
		
			internal TGStoreNode(TriggerGroup storedTG) {
				this.storedTG	= storedTG;
			}
		}
		
		public void Events(TriggerGroup tg) {//applies to spherescript-like "events(+e_blah)"
			AddTriggerGroup(tg);
		}
		
		public void Events(TGRemover remover) {
			RemoveTriggerGroup(remover.tg);
		}
		
		public void Events(int i) {
			if (i == 0) {
				triggerGroups = null;
			}
		}
		
		public string Events() {
			if (triggerGroups != null) {
				StringBuilder toreturn= new StringBuilder();
				TGStoreNode curNode = triggerGroups;
				bool first=true;
				do {
					if (first) {
						first=false;
					} else {
						toreturn.Append(", ");
					}
					toreturn.Append(curNode.storedTG.ToString());
					curNode = curNode.nextNode;
				} while (curNode != null);
				return toreturn.ToString();
			} else {
				return "";
			}
		}
	
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
		public virtual void Trigger(TriggerKey td, ScriptArgs sa) {
			if (triggerGroups != null) {
				TGStoreNode curNode = triggerGroups;
				do {
					curNode.storedTG.Run(this, td, sa);
					curNode = curNode.nextNode;
				} while (curNode != null);
			}
		}
		
		public virtual void TryTrigger(TriggerKey td, ScriptArgs sa) {
			if (triggerGroups != null) {
				TGStoreNode curNode = triggerGroups;
				do {
					curNode.storedTG.TryRun(this, td, sa);
					curNode = curNode.nextNode;
				} while (curNode != null);
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
		
		public virtual bool CancellableTrigger(TriggerKey td, ScriptArgs sa) {
			if (triggerGroups != null) {
				TGStoreNode curNode = triggerGroups;
				do {
					object retVal = curNode.storedTG.Run(this, td, sa);
					try {
						int retInt = Convert.ToInt32(retVal);
						if (retInt == 1) {
							return true;
						}
					} catch (Exception) {
					}
					curNode = curNode.nextNode;
				} while (curNode != null);
			}
			return false;
		}
		
		public virtual bool TryCancellableTrigger(TriggerKey td, ScriptArgs sa) {
			if (triggerGroups != null) {
				TGStoreNode curNode = triggerGroups;
				do {
					object retVal = curNode.storedTG.TryRun(this, td, sa);
					try {
						int retInt = Convert.ToInt32(retVal);
						if (retInt == 1) {
							return true;
						}
					} catch (Exception) {
					}
					curNode = curNode.nextNode;
				} while (curNode != null);
			}
			return false;
		}

		public void Trigger(TriggerKey td, params object[] scriptArguments) {
			if ((scriptArguments != null) && (scriptArguments.Length > 0)) {
				Trigger(td, new ScriptArgs(scriptArguments));
			} else {
				Trigger(td, (ScriptArgs) null);
			}
		}

		public bool CancellableTrigger(TriggerKey td, params object[] scriptArguments) {
			if ((scriptArguments != null) && (scriptArguments.Length > 0)) {
				return CancellableTrigger(td, new ScriptArgs(scriptArguments));
			} else {
				return CancellableTrigger(td, (ScriptArgs) null);
			}
		}
		
		internal protected virtual void BeingDeleted() {
			ClearTimers();
		}
		
		public void SetTag(TagKey tk, object value) {
			if (tags==null) tags=new Hashtable();
			//Console.WriteLine("TagKey["+tk+"]="+value);
			tags[tk]=value;
		}
		
		public object GetTag(TagKey td) {
			if (tags==null) return null;
			return tags[td];
		}
		
		public double GetTag0(TagKey td) {
			return TagMath.ToDouble(GetTag(td));
		}
		
		public bool HasTag(TagKey td) {
			if (tags==null) {
				return false;
			}
			return (tags.ContainsKey(td));
		}
		
		public void RemoveTag(TagKey td) {
			if (tags==null) return;
			tags.Remove(td);
		}
		
		public void ClearTags() {
			if (tags==null) {
				return;
			}
			foreach (object keyObj in tags.Keys) {
				if (keyObj is TagKey) {
					tags.Remove(keyObj);
				}
			}
		}
		
		public string ListTags() {
			int tagcount = 0;
			StringBuilder sb = null;
			if (tags != null) {
				sb = new StringBuilder("Tags of the object '").Append(this).Append("' :").Append(Environment.NewLine);
				foreach (DictionaryEntry entry in tags) {
					if (entry.Key is TagKey) {
						sb.Append(entry.Key ).Append(" = ").Append(entry.Value).Append(Environment.NewLine);
						tagcount++;
					}
				}
				sb.Length -= Environment.NewLine.Length;
			}
			if (tagcount > 0) {
				return sb.ToString();
			} else {
				return "Object '"+this+"' has no tags";
			}
		}
		
		
		public object CallFunc(ScriptHolder script, ScriptArgs sa) {
			if (script == null) {
				throw new CallFuncException("Attempted to call null function.");
			}
			return script.TryRun(this, sa);
		}
		
		public object CallFunc(ScriptHolder script, params object[] args) {
			if (script == null) {
				throw new CallFuncException("Attempted to call null function.");
			}
			ScriptArgs sa = new ScriptArgs(args);
			return script.TryRun(this, sa);
		}
		
		private static string ListProperties(Type type) {
			return ListProperties(type, null);
		}
		private static string ListProperties(Type type, string name) {
			PropertyInfo[] props=type.GetProperties(BindingFlags.Public|BindingFlags.Instance);
			StringBuilder propNames=new StringBuilder("(");
			foreach (PropertyInfo propertyInfo in props) {
				if (name==null || String.Compare(propertyInfo.Name, name, true)==0) {
					if (propNames.Length>1) {
						propNames.Append(", ");
					}
					propNames.Append(propertyInfo.Name);
				}
			}
			if (propNames.Length==1) {
				propNames.Append("[Sorry, no properties found])");
			} else {
				propNames.Append(")");
			}
			return propNames.ToString();
		}
		private static string ListMethods(Type type) {
			return ListMethods(type, null);
		}
		private static string ListMethods(Type type, string name) {
			MethodInfo[] meths=type.GetMethods(BindingFlags.Public|BindingFlags.Instance);
			StringBuilder methNames=new StringBuilder("(");
			foreach (MethodInfo methodInfo in meths) {
				if (name==null || String.Compare(methodInfo.Name, name, true)==0) {
					if (methNames.Length>1) {
						methNames.Append(", ");
					}
					methNames.Append(methodInfo.Name);
				}
			}
			if (methNames.Length==1) {
				methNames.Append("[Sorry, no properties found])");
			} else {
				methNames.Append(")");
			}
			return methNames.ToString();
		}
		
		public void Help() {
			Globals.Src.WriteLine("Properties: "+ListProperties(GetType()));
			Globals.Src.WriteLine("Methods: "+ListMethods(GetType()));
		}

		public virtual void Save(SaveStream output) {
			TGStoreNode curNode = triggerGroups;
			while (curNode != null) {
				output.WriteValue("events", curNode.storedTG.Defname);
				curNode = curNode.nextNode;
			}
			if (tags != null) {
				ArrayList timersList = new ArrayList();
				ArrayList forDeleting = new ArrayList();
				foreach (DictionaryEntry entry in tags) {
					object key = entry.Key;
					object value = entry.Value;
					if (key is TagKey) {
						IDeletable deletableValue = value as IDeletable;
						if (deletableValue != null) {
							if (deletableValue.IsDeleted) {
								forDeleting.Add(entry.Key);
								continue;//we don't save deleted values
							}
						}
						output.WriteValue("tag."+key, value);
					} else if (key is TimerKey) {
						timersList.Add(value);
					} else {
						Logger.WriteError(string.Format("This should not happen. Unknown key-value pair: {0} - {1}", key, value));
					}
				}
				foreach (object key in forDeleting) {
					tags.Remove(key);
				}
				foreach (Timer timer in timersList) {
					output.WriteLine();
					Timer.SaveThis(output, timer);
				}
			}
		}
		
		//regular expressions for textual loading
		//tag.name
		internal static Regex tagRE= new Regex(@"tag\.(?<name>\w+)\s*",
			RegexOptions.IgnoreCase|RegexOptions.CultureInvariant|RegexOptions.Compiled);

		protected virtual void LoadLine(string filename, int line, string name, string value) {
			Match m = tagRE.Match(name);
			if (m.Success) {	//If the name begins with 'tag.'
				if (tags==null) {
					tags=new Hashtable();
				}
				string tagName=m.Groups["name"].Value;
				TagKey td = TagKey.Get(tagName);
				ObjectSaver.Load(value, new LoadObjectParam(LoadTag_Delayed), filename, line, td);
				return;
			}
			if ((name=="events") || (name=="event") || (name=="triggergroup") || (name=="type")) {
				string eventName;
				m= TagMath.stringRE.Match(value);
				if (m.Success) {
					eventName = m.Groups["value"].Value;
				} else {
					eventName = value;
				}
				TriggerGroup tg=TriggerGroup.Get(eventName);
				if (tg!=null) {
					AddTriggerGroup(tg);
				} else {
					throw new Exception("TriggerGroup '"+eventName+"' does not exist.");
				}
				return;
			}
			throw new ScriptException("Invalid data '"+LogStr.Ident(name)+"' = '"+LogStr.Number(value)+"'.");
		}
		
		//used by loaders (Thing, GameAccount...)
		internal void LoadSectionLines(PropsSection ps) {
			foreach (PropsLine p in ps.props.Values) {
				try {
					LoadLine(ps.filename, p.line, p.name.ToLower(), p.value);
				} catch (FatalException) {
					throw;
				} catch (Exception ex) {
					Logger.WriteWarning(ps.filename,p.line,ex);
				}
			}
		}
		
		private void LoadTag_Delayed(object resolvedObject, string filename, int line, object tagKey) {
			//throw new Exception("LoadTag_Delayed");
			SetTag((TagKey) tagKey, resolvedObject);
		}

		public void ThrowIfDeleted() {
			if (this.IsDeleted) {
				throw new Exception("You can not manipulate a deleted item ("+this+")");
			}
		}

		public virtual bool IsDeleted { get { return false; } }

		public virtual void Delete() {
			BeingDeleted();
		}
	}
}