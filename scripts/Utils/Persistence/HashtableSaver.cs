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
using System.Collections;
using Shielded;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {
	public sealed class HashtableSaver : ISaveImplementor, IDeepCopyImplementor {
		public string HeaderName { get {
			return "Hashtable";
		} }

		public Type HandledType { get {
			return typeof(Hashtable);
		} }
		
		public void Save(object objToSave, SaveStream writer) {
			var table = (Hashtable) objToSave;
			var count = table.Count;
			writer.WriteValue("count", count);
			var i = 0;
			foreach (DictionaryEntry entry in table) {
				writer.WriteValue(i+".K", entry.Key);
				writer.WriteValue(i+".V", entry.Value);
				i++;
			}
		}
		
		public object LoadSection(PropsSection input) {
			var currentLineNumber = input.HeaderLine;
			try {
				var countLine = input.PopPropsLine("count");
				currentLineNumber = countLine.Line;
				var count = int.Parse(countLine.Value);
				var table = new Hashtable(count);
				for (var i = 0; i<count; i++) {
					var keyLine = input.PopPropsLine(i+".K");
					var valueLine = input.PopPropsLine(i+".V");
					var helper = new HashtableLoadHelper(table);
					currentLineNumber = keyLine.Line;
					ObjectSaver.Load(keyLine.Value, helper.DelayedLoad_Key, input.Filename, keyLine.Line);
					currentLineNumber = valueLine.Line;
					ObjectSaver.Load(valueLine.Value, helper.DelayedLoad_Value, input.Filename, valueLine.Line);
				}
				return table;
			} catch (FatalException) {
				throw;
			} catch (TransException) {
				throw;
			} catch (SEException sex) {
				sex.TryAddFileLineInfo(input.Filename, currentLineNumber);
				throw;
			} catch (Exception e) {
				throw new SEException(input.Filename, currentLineNumber, e);
			}
		}
		
		private class HashtableLoadHelper {
			internal Hashtable table;
			private object key;
			private object value;
			private bool valueSet;
			private bool keySet;


			internal HashtableLoadHelper(Hashtable table) {
				this.table = table;
			}

			private void TryInsertIntoTable() {
				if (this.valueSet && this.keySet) {
					this.table[this.key] = this.value;
				}
			}

			internal void DelayedLoad_Value(object loadedObj, string filename, int line) {
				this.value = loadedObj;
				this.valueSet = true;
				this.TryInsertIntoTable();
			}

			internal void DelayedLoad_Key(object loadedObj, string filename, int line) {
				this.key = loadedObj;
				this.keySet = true;
				this.TryInsertIntoTable();
			}

			internal void DelayedCopy_Value(object loadedObj) {
				this.value = loadedObj;
				this.valueSet = true;
				this.TryInsertIntoTable();
			}

			internal void DelayedCopy_Key(object loadedObj) {
				this.key = loadedObj;
				this.keySet = true;
				this.TryInsertIntoTable();
			}
		}

		public object DeepCopy(object copyFrom) {
			var copyFromTable = (Hashtable) copyFrom;
			var newTable = new Hashtable();

			foreach (DictionaryEntry entry in copyFromTable) {
				var helper = new HashtableLoadHelper(newTable);
				DeepCopyFactory.GetCopyDelayed(entry.Key, helper.DelayedCopy_Key);
				DeepCopyFactory.GetCopyDelayed(entry.Value, helper.DelayedCopy_Value);
			}
			return newTable;
		}
	}
}