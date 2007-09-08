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
using System.Collections.Generic;
using SteamEngine;
using SteamEngine.Common;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {
	public class DictionarySaver : ISaveImplementor, IDeepCopyImplementor {
		public string HeaderName {
			get {
				return "Dictionary";
			}
		}

		public Type HandledType {
			get {
				return typeof(Dictionary<,>);
			}
		}

		public void Save(object objToSave, SaveStream writer) {
			IDictionary dict = (IDictionary) objToSave;
			int count = dict.Count;
			writer.WriteValue("count", count);
			Type[] genericArguments = objToSave.GetType().GetGenericArguments();
			writer.WriteLine("TKey="+genericArguments[0].Name);
			writer.WriteLine("TValue="+genericArguments[1].Name);
			int i = 0;
			foreach (DictionaryEntry entry in dict) {
				writer.WriteValue(i+".K", entry.Key);
				writer.WriteValue(i+".V", entry.Value);
				i++;
			}
		}

		public object LoadSection(PropsSection input) {
			int currentLineNumber = input.headerLine;
			try {
				PropsLine pl = input.PopPropsLine("TKey");
				currentLineNumber = pl.line;
				Type[] genericTypes = new Type[2];
				genericTypes[0] = GenericListSaver.ParseType(pl);
				pl = input.PopPropsLine("TValue");
				currentLineNumber = pl.line;
				genericTypes[1] = GenericListSaver.ParseType(pl);
				Type dictType = typeof(Dictionary<,>);
				dictType.MakeGenericType(genericTypes);
				IDictionary dict = (IDictionary) Activator.CreateInstance(dictType);

				PropsLine countLine = input.PopPropsLine("count");
				currentLineNumber = countLine.line;
				int count = int.Parse(countLine.value);
				for (int i = 0; i<count; i++) {
					PropsLine keyLine = input.PopPropsLine(i+".K");
					PropsLine valueLine = input.PopPropsLine(i+".V");
					DictionaryLoadHelper helper = new DictionaryLoadHelper(dict, genericTypes[0], genericTypes[1]);
					currentLineNumber = keyLine.line;
					ObjectSaver.Load(keyLine.value, new LoadObject(helper.DelayedLoad_Key), input.filename, keyLine.line);
					currentLineNumber = valueLine.line;
					ObjectSaver.Load(valueLine.value, new LoadObject(helper.DelayedLoad_Value), input.filename, valueLine.line);
				}
				return dict;
			} catch (FatalException) {
				throw;
			} catch (SEException sex) {
				sex.TryAddFileLineInfo(input.filename, currentLineNumber);
				throw;
			} catch (Exception e) {
				throw new SEException(input.filename, currentLineNumber, e);
			}
		}

		private class DictionaryLoadHelper {
			internal IDictionary dict;
			private Type keyType;
			private object key;
			private Type valueType;
			private object value;
			private bool valueSet = false;
			private bool keySet = false;


			internal DictionaryLoadHelper(IDictionary dict, Type keyType, Type valueType) {
				this.dict = dict;
				this.keyType = keyType;
				this.valueType = valueType;
			}

			private void TryInsertIntoDict() {
				if (valueSet && keySet) {
					dict[key] = value;
				}
			}

			internal void DelayedLoad_Value(object loadedObj, string filename, int line) {
				this.value = ConvertTools.ConvertTo(valueType, loadedObj);
				valueSet = true;
				TryInsertIntoDict();
			}

			internal void DelayedLoad_Key(object loadedObj, string filename, int line) {
				this.key = ConvertTools.ConvertTo(keyType, loadedObj);
				keySet = true;
				TryInsertIntoDict();
			}

			internal void DelayedCopy_Value(object loadedObj) {
				this.value = ConvertTools.ConvertTo(valueType, loadedObj);
				valueSet = true;
				TryInsertIntoDict();
			}

			internal void DelayedCopy_Key(object loadedObj) {
				this.key = ConvertTools.ConvertTo(keyType, loadedObj);
				keySet = true;
				TryInsertIntoDict();
			}
		}

		public object DeepCopy(object copyFrom) {
			IDictionary copyFromDict = (IDictionary) copyFrom;

			Type[] genericArguments = copyFrom.GetType().GetGenericArguments();
			Type dictType = typeof(Dictionary<,>);
			dictType.MakeGenericType(genericArguments);
			IDictionary newDict = (IDictionary) Activator.CreateInstance(dictType);

			foreach (DictionaryEntry entry in copyFromDict) {
				DictionaryLoadHelper helper = new DictionaryLoadHelper(newDict, genericArguments[0], genericArguments[1]);
				DeepCopyFactory.GetCopyDelayed(entry.Key, helper.DelayedCopy_Key);
				DeepCopyFactory.GetCopyDelayed(entry.Value, helper.DelayedCopy_Value);
			}
			return newDict;
		}
	}
}