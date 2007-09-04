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
using System.Globalization;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {

	public class LinkedListSerializer : ISaveImplementor {
		public string HeaderName { get {
			return "LinkedList";
		} }

		public Type HandledType { get {
			return typeof(LinkedList<>);
		} }
		
		public void Save(object objToSave, SaveStream writer) {
			ICollection linkedList = (ICollection) objToSave;
			Type listType = linkedList.GetType();
			Type memberType = listType.GetGenericArguments()[0];
			int count = linkedList.Count;
			writer.WriteValue("count", count);
			writer.WriteLine("type="+memberType.Name);
			int i = 0;
			foreach (object o in linkedList) {
				writer.WriteValue(i.ToString(), o);
				i++;
			}
		}

		public object LoadSection(PropsSection input) {
			int currentLineNumber = input.headerLine;
			try {
				PropsLine countLine = input.PopPropsLine("count");
				currentLineNumber = countLine.line;
				int count = int.Parse(countLine.value);

				PropsLine pl = input.PopPropsLine("type");
				currentLineNumber = pl.line;
				Type elemType = ClassManager.GetType(pl.value);
				if (elemType == null) {
					elemType = Type.GetType(pl.value, false, true);
				}
				if (elemType == null) {
					throw new Exception("Generic LinkedList element type not recognised.");
				}

				Type typeOfList = typeof(LinkedList<>).MakeGenericType(elemType);
				object linkedList = Activator.CreateInstance(typeOfList);
				Type typeOfWrapper = typeof(LinkedListWrapper<>).MakeGenericType(elemType);
				IHelper linkedListWrapper = (IHelper) Activator.CreateInstance(typeOfWrapper, new object[] { linkedList });

				for (int i = 0; i<count; i++) {
					PropsLine valueLine = input.PopPropsLine(i.ToString());
					currentLineNumber = valueLine.line;
					ObjectSaver.Load(valueLine.value, linkedListWrapper.DelayedLoad_Value, input.filename, valueLine.line, i);
				}
				return linkedList;
			} catch (FatalException) {
				throw;
			} catch (SEException sex) {
				sex.TryAddFileLineInfo(input.filename, currentLineNumber);
				throw;
			} catch (Exception e) {
				throw new SEException(input.filename, currentLineNumber, e);
			}
		}

		private interface IHelper {
			void DelayedLoad_Value(object o, string filename, int line, object index);
			void DelayedCopy_Value(object o, object index);
		}

		public class LinkedListWrapper<T> : IHelper {
			LinkedList<T> linkedList;
			T[] loadedValues;
			bool[] loaded;
			int loadedCount;
			int countToLoad;

			public LinkedListWrapper(LinkedList<T> linkedList, int count) {
				this.linkedList = linkedList;
				this.loadedValues = new T[count];
				this.loaded = new bool[count];
				this.countToLoad = count;
			}

			public void DelayedLoad_Value(object o, string filename, int line, object index) {
				DelayedCopy_Value(o, index);
			}

			public void DelayedCopy_Value(object o, object index) {
				int i = (int)index;
				if (!loaded[i]) {
					loadedCount++;
					loaded[i] = true;
				}
				loadedValues[i] = (T) ConvertTools.ConvertTo(typeof(T), o);
				if (loadedCount == countToLoad) {
					foreach (T value in loadedValues) {
						linkedList.AddLast(value);
					}
				}
			}
		}

		public object DeepCopy(object copyFrom) {
			ICollection copyFromList = (ICollection) copyFrom;
			int n = copyFromList.Count;

			Type elemType = copyFrom.GetType().GetGenericArguments()[0];
			Type typeOfList = typeof(LinkedList<>).MakeGenericType(elemType);
			ICollection newList = (ICollection) Activator.CreateInstance(typeOfList, new object[] { n });

			Type typeOfWrapper = typeof(LinkedListWrapper<>).MakeGenericType(elemType);
			IHelper linkedListWrapper = (IHelper) Activator.CreateInstance(typeOfWrapper, new object[] { newList });

			int i = 0;
			foreach (object o in copyFromList) {
				DeepCopyFactory.GetCopyDelayed(o, linkedListWrapper.DelayedCopy_Value, i);
				i++;
			}
			return newList;
		}
	}
}