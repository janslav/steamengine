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
				IAdder linkedListWrapper = (IAdder) Activator.CreateInstance(typeOfWrapper, new object[] { linkedList });

				for (int i = 0; i<count; i++) {
					PropsLine valueLine = input.PopPropsLine(i.ToString());
					currentLineNumber = valueLine.line;
					ObjectSaver.Load(valueLine.value, LoadNode_Delayed, input.filename, valueLine.line, linkedListWrapper);
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

		private interface IAdder {
			void Add(object o);
		}

		public class LinkedListWrapper<T> : IAdder {
			LinkedList<T> linkedList;
			bool typeIsConvertible = false;

			public LinkedListWrapper(LinkedList<T> linkedList) {
				this.linkedList = linkedList;
				typeIsConvertible = typeof(IConvertible).IsAssignableFrom(typeof(T));
			}

			public void Add(object o) {
				if (typeIsConvertible) {
					o = Convert.ChangeType(o, typeof(T));
				}
				linkedList.AddLast((T) o);
			}
		}
		
		public void LoadNode_Delayed(object loadedObj, string filename, int line, object param) {
			IAdder linkedListWrapper = (IAdder) param;
			linkedListWrapper.Add(loadedObj);
		}
	}
}