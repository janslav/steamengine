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

	public sealed class GenericListSaver : ISaveImplementor {
		public string HeaderName { get {
			return "GenericList";
		} }

		public Type HandledType { get {
			return typeof(List<>);
		} }
		
		public void Save(object objToSave, SaveStream writer) {
			IList list = (IList) objToSave;
			Type listType = list.GetType();
			Type memberType = listType.GetGenericArguments()[0];
			int count = list.Count;
			writer.WriteValue("count", count);
			writer.WriteLine("type="+memberType.Name);
			for (int i = 0; i<count; i++) {
				writer.WriteValue(i.ToString(), list[i]);
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
				Type elemType = ParseType(pl);

				Type typeOfList = typeof(List<>).MakeGenericType(elemType);
				IList list = (IList) Activator.CreateInstance(typeOfList, new object[] { count });

				for (int i = 0; i<count; i++) {
					list.Add(null);
					PropsLine valueLine = input.PopPropsLine(i.ToString());
					currentLineNumber = valueLine.line;
					GenericListLoadHelper alip = new GenericListLoadHelper(list, i, elemType);
					ObjectSaver.Load(valueLine.value, new LoadObjectParam(DelayedLoad_Index), input.filename, valueLine.line, alip);
				}
				return list;
			} catch (FatalException) {
				throw;
			} catch (SEException sex) {
				sex.TryAddFileLineInfo(input.filename, currentLineNumber);
				throw;
			} catch (Exception e) {
				throw new SEException(input.filename, currentLineNumber, e);
			}
		}

		public static Type ParseType(PropsLine pl) {
			Type elemType = ClassManager.GetType(pl.value);
			if (elemType == null) {
				elemType = Type.GetType(pl.value, false, true);
			}
			if (elemType == null) {
				throw new Exception("Element type not recognised.");
			}
			return elemType;
		}
		
		public void DelayedLoad_Index(object loadedObj, string filename, int line, object param) {
			GenericListLoadHelper alip = (GenericListLoadHelper) param;
			alip.list[alip.index] = ConvertTools.ConvertTo(alip.elemType, loadedObj);
		}

		public void DelayedCopy_Index(object loadedObj, object param) {
			GenericListLoadHelper alip = (GenericListLoadHelper) param;
			alip.list[alip.index] = ConvertTools.ConvertTo(alip.elemType, loadedObj);
		}
		
		private class GenericListLoadHelper {
			internal IList list;
			internal int index;
			internal Type elemType;

			internal GenericListLoadHelper(IList list, int index, Type elemType) {
				this.index = index;
				this.list = list;
				this.elemType = elemType;
			}
		}

		public object DeepCopy(object copyFrom) {
			IList copyFromList = (IList) copyFrom;
			int n = copyFromList.Count;

			Type elemType = copyFrom.GetType().GetGenericArguments()[0];
			Type typeOfList = typeof(List<>).MakeGenericType(elemType);
			IList newList = (IList) Activator.CreateInstance(typeOfList, new object[] { n });

			for (int i = 0; i<n; i++) {
				newList.Add(null);
				GenericListLoadHelper alip = new GenericListLoadHelper(newList, i, elemType);
				DeepCopyFactory.GetCopyDelayed(copyFromList[i], DelayedCopy_Index, alip);
			}
			return newList;
		}
	}
}