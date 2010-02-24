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
using SteamEngine.Timers;
using SteamEngine.Common;
using SteamEngine.Persistence;

namespace SteamEngine {
	public interface IDeletable {
		bool IsDeleted { get; }
		void Delete();
	}

	public interface ITagHolder {
		object GetTag(TagKey tk);
		void SetTag(TagKey tk, object value);
		bool HasTag(TagKey tk);
		void RemoveTag(TagKey tk);
		void ClearTags();
	}

	/*
		Class: TagHolder
		All Things (Items and Characters) and GameAccounts are TagHolders, as is Server.globals.
	*/
	[Summary("This is the base class for implementation of our \"lightweight polymorphism\". "
		+ "TagHolder class holds tags (values indexed by names) and Timers.")]
	public class TagHolder : IDeletable, ITagHolder {
		internal Hashtable tags; //in this Hashtable are stored Tags, Timers, and by PluginHolder class also Plugins and TGListNodes

		public TagHolder() {
		}

		#region DeepCopy implementation
		public TagHolder(TagHolder copyFrom) { //copying constuctor
			if (copyFrom.tags != null) {
				foreach (DictionaryEntry entry in copyFrom.tags) {
					TagKey tagK = entry.Key as TagKey;
					if (tagK != null) {
						DeepCopyFactory.GetCopyDelayed(entry.Value, DelayedGetCopy_Tag, tagK);
					} else {
						TimerKey timerK = entry.Key as TimerKey;
						if (timerK != null) {
							DeepCopyFactory.GetCopyDelayed(entry.Value, DelayedGetCopy_Timer);
						}
					}
				}
			}
		}

		private void DelayedGetCopy_Tag(object copy, object paramTagKey) {
			SetTag((TagKey) paramTagKey, copy);
		}

		private void DelayedGetCopy_Timer(object copy) {
			//it should enqueue itself
		}

		#endregion DeepCopy implementation

		public virtual string Name {
			get {
				return "<nameless TagHolder>";
			}
			set {

			}
		}

		#region Timers
		//called by Timer after load, do not use otherwise.
		public BoundTimer AddTimer(TimerKey key, BoundTimer timer) {
			if (tags != null) {
				TimerKey prevKey = tags[timer] as TimerKey;
				if (prevKey != null && prevKey != key) {
					throw new SEException("You can't assign one Timer to one TagHolder under 2 different TimerKeys");
				}

				BoundTimer prevTimer = tags[key] as BoundTimer;
				if (prevTimer != null && prevTimer != timer) {
					this.RemoveTimer(prevTimer);
				}
			} else {
				tags = new Hashtable();
			}
			tags[key] = timer;
			tags[timer] = key;
			timer.contRef.Target = this;
			return timer;
		}

		public BoundTimer RemoveTimer(TimerKey key) {
			if (tags != null) {
				BoundTimer timer = tags[key] as BoundTimer;
				if (timer != null) {
					tags.Remove(key);
					tags.Remove(timer);
					timer.contRef.Target = null;
					return timer;
				}
			}
			return null;
		}

		public void RemoveTimer(BoundTimer timer) {
			if (tags != null) {
				TimerKey key = tags[timer] as TimerKey;
				if (key != null) {
					tags.Remove(key);
					tags.Remove(timer);
					timer.contRef.Target = null;
				}
			}
		}

		public void DeleteTimer(TimerKey key) {
			BoundTimer timer = this.RemoveTimer(key);
			if (timer != null) {
				timer.Delete();
			}
		}

		public void DeleteTimers() {
			if (tags == null) {
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
				RemoveTimer(key).Delete();
			}
			ReleaseTagsTableIfEmpty();
		}

		public bool HasTimer(TimerKey key) {
			if (tags != null) {
				return (tags.ContainsKey(key));
			}
			return false;
		}

		public bool HasTimer(BoundTimer timer) {
			return timer.contRef.Target == this;
		}

		public BoundTimer GetTimer(TimerKey key) {
			if (tags != null) {
				return (BoundTimer) tags[key];
			}
			return null;
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures"), Summary("Return enumerable containing all timers")]
		public IEnumerable<KeyValuePair<TimerKey, BoundTimer>> GetAllTimers() {
			if (tags != null) {
				foreach (DictionaryEntry entry in tags) {
					TimerKey tk = entry.Key as TimerKey;
					if (tk != null) {
						yield return new KeyValuePair<TimerKey, BoundTimer>(tk, (BoundTimer) entry.Value);
					}
				}
			}
		}
		#endregion Timers

		#region Tags
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures"), Summary("Return enumerable containing all tags")]
		public IEnumerable<KeyValuePair<TagKey, Object>> GetAllTags() {
			if (tags != null) {
				foreach (DictionaryEntry entry in tags) {
					TagKey tk = entry.Key as TagKey;
					if (tk != null) {
						yield return new KeyValuePair<TagKey, Object>(tk, entry.Value);
					}
				}
			}
		}

		internal void EnsureTagsTable() {
			if (tags == null) {
				tags = new Hashtable();
			}
		}

		internal void ReleaseTagsTableIfEmpty() {
			if (tags != null) {
				if (tags.Count == 0) {
					tags = null;
				}
			}
		}

		public void SetTag(TagKey tk, object value) {
			EnsureTagsTable();
			//Console.WriteLine("TagKey["+tk+"]="+value);
			tags[tk] = value;
		}

		public object GetTag(TagKey tk) {
			if (tags == null) {
				return null;
			}
			return tags[tk];
		}

		public bool HasTag(TagKey tk) {
			if (tags == null) {
				return false;
			}
			return (tags.ContainsKey(tk));
		}

		public void RemoveTag(TagKey tk) {
			if (tags == null) return;
			tags.Remove(tk);
		}

		public void ClearTags() {
			if (tags == null) {
				return;
			}
			List<TagKey> toBeRemoved = new List<TagKey>();
			foreach (object keyObj in tags.Keys) {
				TagKey key = keyObj as TagKey;
				if (key != null) {
					toBeRemoved.Add(key);
				}
			}
			foreach (TagKey key in toBeRemoved) {
				RemoveTag(key);
			}
			ReleaseTagsTableIfEmpty();
		}

		public string ListTags() {
			int tagcount = 0;
			StringBuilder sb = null;
			if (tags != null) {
				sb = new StringBuilder("Tags of the object '").Append(this).Append("' :").Append(Environment.NewLine);
				foreach (DictionaryEntry entry in tags) {
					if (entry.Key is TagKey) {
						sb.Append(entry.Key).Append(" = ").Append(entry.Value).Append(Environment.NewLine);
						tagcount++;
					}
				}
				sb.Length -= Environment.NewLine.Length;
			}
			if (tagcount > 0) {
				return sb.ToString();
			} else {
				return "Object '" + this + "' has no tags";
			}
		}
		#endregion Tags

		#region HELP (todo? remove?)
		private static string ListProperties(Type type) {
			return ListProperties(type, null);
		}

		private static string ListProperties(Type type, string name) {
			PropertyInfo[] props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
			StringBuilder propNames = new StringBuilder("(");
			foreach (PropertyInfo propertyInfo in props) {
				if (name == null || StringComparer.OrdinalIgnoreCase.Equals(propertyInfo.Name, name)) {
					if (propNames.Length > 1) {
						propNames.Append(", ");
					}
					propNames.Append(propertyInfo.Name);
				}
			}
			if (propNames.Length == 1) {
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
			MethodInfo[] meths = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);
			StringBuilder methNames = new StringBuilder("(");
			foreach (MethodInfo methodInfo in meths) {
				if (name == null || StringComparer.OrdinalIgnoreCase.Equals(methodInfo.Name, name)) {
					if (methNames.Length > 1) {
						methNames.Append(", ");
					}
					methNames.Append(methodInfo.Name);
				}
			}
			if (methNames.Length == 1) {
				methNames.Append("[Sorry, no methods found])");
			} else {
				methNames.Append(")");
			}
			return methNames.ToString();
		}

		public void Help() {
			Globals.Src.WriteLine("Properties: " + ListProperties(GetType()));
			Globals.Src.WriteLine("Methods: " + ListMethods(GetType()));
		}
		#endregion HELP

		#region save/load


		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
		public virtual void Save(SaveStream output) {
			if (tags != null) {
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
						output.WriteValue("tag." + key, value);
					} else if (key is TimerKey) {
						output.WriteValue("%" + key.ToString(), value);
						//} else {
						//Logger.WriteError(string.Format("This should not happen. Unknown key-value pair: {0} - {1}", key, value));
					}
				}
				foreach (object key in forDeleting) {
					tags.Remove(key);
				}
			}
		}

		//regular expressions for textual loading
		//tag.name
		internal static Regex tagRE = new Regex(@"tag\.(?<name>\w+)\s*",
			RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

		internal static Regex timerKeyRE = new Regex(@"^\%(?<name>.+)\s*$", RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.IgnoreCase);

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1720:AvoidTypeNamesInParameters", MessageId = "3#")]
		public virtual void LoadLine(string filename, int line, string valueName, string valueString) {
			Match m = tagRE.Match(valueName);
			if (m.Success) {	//If the name begins with 'tag.'
				string tagName = m.Groups["name"].Value;
				TagKey tk = TagKey.Acquire(tagName);
				ObjectSaver.Load(valueString, DelayedLoad_Tag, filename, line, tk);
				return;
			}
			m = timerKeyRE.Match(valueName);
			if (m.Success) {	//If the name begins with '%'
				string timerName = m.Groups["name"].Value;
				TimerKey tk = TimerKey.Acquire(timerName);
				ObjectSaver.Load(valueString, DelayedLoad_Timer, filename, line, tk);
				return;
			}
			throw new ScriptException("Invalid data '" + LogStr.Ident(valueName) + "' = '" + LogStr.Number(valueString) + "'.");
		}

		//used by loaders (Thing, GameAccount...)
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		internal void LoadSectionLines(PropsSection ps) {
			foreach (PropsLine p in ps.PropsLines) {
				try {
					this.LoadLine(ps.Filename, p.Line, p.Name.ToLowerInvariant(), p.Value);
				} catch (FatalException) {
					throw;
				} catch (Exception ex) {
					Logger.WriteWarning(ps.Filename, p.Line, ex);
				}
			}
		}

		private void DelayedLoad_Tag(object resolvedObject, string filename, int line, object tagKey) {
			//throw new Exception("LoadTag_Delayed");
			SetTag((TagKey) tagKey, resolvedObject);
		}

		private void DelayedLoad_Timer(object resolvedObject, string filename, int line, object timerKey) {
			//throw new Exception("LoadTag_Delayed");
			AddTimer((TimerKey) timerKey, (BoundTimer) resolvedObject);
		}

		#endregion save/load

		#region IDeletable

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		public void ThrowIfDeleted() {
			if (this.IsDeleted) {
				throw new DeletedException("You can not manipulate a deleted object (" + this + ")");
			}
			if (this.IsLimbo && RunLevelManager.IsRunning) { //when loading, it's ok for stuff to be in limbo
				try {
					this.Delete();
				} catch { }
				throw new SEException("This object is in Limbo state (" + this + "). This should not happen.");
			}
		}

		internal virtual bool IsLimbo {
			get {
				return false;
			}
		}

		public virtual bool IsDeleted { get { return false; } }

		public virtual void Delete() {
			DeleteTimers();
		}
		#endregion IDeletable
	}
}