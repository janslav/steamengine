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

	public class GenericListSaver : ISaveImplementor {
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
				Type elemType = ClassManager.GetType(pl.value);
				if (elemType == null) {
					elemType = Type.GetType(pl.value, false, true);
				}
				if (elemType == null) {
					throw new Exception("Generic List element type not recognised.");
				}

				Type typeOfList = typeof(List<>).MakeGenericType(elemType);
				IList list = (IList) Activator.CreateInstance(typeOfList);

				for (int i = 0; i<count; i++) {
					list.Add(null);
					PropsLine valueLine = input.PopPropsLine(i.ToString());
					currentLineNumber = valueLine.line;
					GenericListIndexPair alip = new GenericListIndexPair();
					alip.index = i;
					alip.list = list;
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
		
		public void DelayedLoad_Index(object loadedObj, string filename, int line, object param) {
			GenericListIndexPair alip = (GenericListIndexPair) param;
			alip.list[alip.index] = loadedObj;
		}
		
		private class GenericListIndexPair {
			internal IList list;
			internal int index;
		}
	}
}