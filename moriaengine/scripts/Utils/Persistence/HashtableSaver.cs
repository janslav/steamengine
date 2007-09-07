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
using SteamEngine;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {
	public class HashtableSaver : ISaveImplementor, IDeepCopyImplementor {
		public string HeaderName { get {
			return "Hashtable";
		} }

		public Type HandledType { get {
			return typeof(Hashtable);
		} }
		
		public void Save(object objToSave, SaveStream writer) {
			Hashtable table = (Hashtable) objToSave;
			int count = table.Count;
			writer.WriteValue("count", count);
			int i = 0;
			foreach (DictionaryEntry entry in table) {
				writer.WriteValue(i+".K", entry.Key);
				writer.WriteValue(i+".V", entry.Value);
				i++;
			}
		}
		
		public object LoadSection(PropsSection input) {
			int currentLineNumber = input.headerLine;
			try {
				PropsLine countLine = input.PopPropsLine("count");
				currentLineNumber = countLine.line;
				int count = int.Parse(countLine.value);
				Hashtable table = new Hashtable(count);
				for (int i = 0; i<count; i++) {
					PropsLine keyLine = input.PopPropsLine(i+".K");
					PropsLine valueLine = input.PopPropsLine(i+".V");
					HashtableLoadHelper helper = new HashtableLoadHelper(table);
					currentLineNumber = keyLine.line;
					ObjectSaver.Load(keyLine.value, new LoadObject(helper.DelayedLoad_Key), input.filename, keyLine.line);
					currentLineNumber = valueLine.line;
					ObjectSaver.Load(valueLine.value, new LoadObject(helper.DelayedLoad_Value), input.filename, valueLine.line);
				}
				return table;
			} catch (FatalException) {
				throw;
			} catch (SEException sex) {
				sex.TryAddFileLineInfo(input.filename, currentLineNumber);
				throw;
			} catch (Exception e) {
				throw new SEException(input.filename, currentLineNumber, e);
			}
		}
		
		private class HashtableLoadHelper {
			internal Hashtable table;
			private object key = null;
			private object value = null;
			private bool valueSet = false;
			private bool keySet = false;


			internal HashtableLoadHelper(Hashtable table) {
				this.table = table;
			}

			private void TryInsertIntoTable() {
				if (valueSet && keySet) {
					table[key] = value;
				}
			}

			internal void DelayedLoad_Value(object loadedObj, string filename, int line) {
				this.value = loadedObj;
				valueSet = true;
				TryInsertIntoTable();
			}

			internal void DelayedLoad_Key(object loadedObj, string filename, int line) {
				this.key = loadedObj;
				keySet = true;
				TryInsertIntoTable();
			}

			internal void DelayedCopy_Value(object loadedObj) {
				this.value = loadedObj;
				valueSet = true;
				TryInsertIntoTable();
			}

			internal void DelayedCopy_Key(object loadedObj) {
				this.key = loadedObj;
				keySet = true;
				TryInsertIntoTable();
			}
		}

		public object DeepCopy(object copyFrom) {
			Hashtable copyFromTable = (Hashtable) copyFrom;
			Hashtable newTable = new Hashtable();

			foreach (DictionaryEntry entry in copyFromTable) {
				HashtableLoadHelper helper = new HashtableLoadHelper(newTable);
				DeepCopyFactory.GetCopyDelayed(entry.Key, helper.DelayedCopy_Key);
				DeepCopyFactory.GetCopyDelayed(entry.Value, helper.DelayedCopy_Value);
			}
			return newTable;
		}
	}
}