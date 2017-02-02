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
using Shielded;
using SteamEngine.Common;
using SteamEngine.Parsing;
using SteamEngine.Persistence;
using SteamEngine.Scripting.Compilation;

namespace SteamEngine.CompiledScripts {

	public sealed class LinkedListSerializer : ISaveImplementor, IDeepCopyImplementor {
		public string HeaderName {
			get {
				return "LinkedList";
			}
		}

		public Type HandledType {
			get {
				return typeof(LinkedList<>);
			}
		}

		public void Save(object objToSave, SaveStream writer) {
			var linkedList = (ICollection) objToSave;
			var listType = linkedList.GetType();
			var memberType = listType.GetGenericArguments()[0];
			var count = linkedList.Count;
			writer.WriteValue("count", count);
			writer.WriteLine("type=" + memberType.Name);
			var i = 0;
			foreach (var o in linkedList) {
				writer.WriteValue(i.ToString(), o);
				i++;
			}
		}

		public object LoadSection(PropsSection input) {
			var currentLineNumber = input.HeaderLine;
			try {
				var countLine = input.PopPropsLine("count");
				currentLineNumber = countLine.Line;
				var count = int.Parse(countLine.Value);

				var pl = input.PopPropsLine("type");
				currentLineNumber = pl.Line;
				var elemType = ClassManager.GetType(pl.Value);
				if (elemType == null) {
					elemType = Type.GetType(pl.Value, false, true);
				}
				if (elemType == null) {
					throw new SEException("Generic LinkedList element type not recognised.");
				}

				var typeOfList = typeof(LinkedList<>).MakeGenericType(elemType);
				var linkedList = Activator.CreateInstance(typeOfList);
				var typeOfWrapper = typeof(LinkedListWrapper<>).MakeGenericType(elemType);
				var linkedListWrapper = (IHelper) Activator.CreateInstance(typeOfWrapper, linkedList, count);

				for (var i = 0; i < count; i++) {
					var valueLine = input.PopPropsLine(i.ToString());
					currentLineNumber = valueLine.Line;
					ObjectSaver.Load(valueLine.Value, linkedListWrapper.DelayedLoad_Value, input.Filename, valueLine.Line, i);
				}
				return linkedList;
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
				this.DelayedCopy_Value(o, index);
			}

			public void DelayedCopy_Value(object o, object index) {
				var i = (int) index;
				if (!this.loaded[i]) {
					this.loadedCount++;
					this.loaded[i] = true;
				}
				this.loadedValues[i] = ConvertTools.ConvertTo<T>(o);
				if (this.loadedCount == this.countToLoad) {
					foreach (var value in this.loadedValues) {
						this.linkedList.AddLast(value);
					}
				}
			}
		}

		public object DeepCopy(object copyFrom) {
			var copyFromList = (ICollection) copyFrom;
			var n = copyFromList.Count;

			var elemType = copyFrom.GetType().GetGenericArguments()[0];
			var typeOfList = typeof(LinkedList<>).MakeGenericType(elemType);
			var newList = (ICollection) Activator.CreateInstance(typeOfList, n);

			var typeOfWrapper = typeof(LinkedListWrapper<>).MakeGenericType(elemType);
			var linkedListWrapper = (IHelper) Activator.CreateInstance(typeOfWrapper, newList);

			var i = 0;
			foreach (var o in copyFromList) {
				DeepCopyFactory.GetCopyDelayed(o, linkedListWrapper.DelayedCopy_Value, i);
				i++;
			}
			return newList;
		}
	}
}