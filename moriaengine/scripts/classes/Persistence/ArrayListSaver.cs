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
	public class ArrayListSaver : ISaveImplementor {
		public string HeaderName { get {
			return "ArrayList";
		} }

		public Type HandledType { get {
			return typeof(ArrayList);
		} }
		
		public void Save(object objToSave, SaveStream writer) {
			ArrayList list = (ArrayList) objToSave;
			int count = list.Count;
			writer.WriteValue("count", count);
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
				ArrayList list = new ArrayList(count);
				for (int i = 0; i<count; i++) {
					list.Add(null);
					PropsLine valueLine = input.PopPropsLine(i.ToString());
					currentLineNumber = valueLine.line;
					ArrayListIndexPair alip = new ArrayListIndexPair();
					alip.index = i;
					alip.list = list;
					ObjectSaver.Load(valueLine.value, new LoadObjectParam(LoadIndex_Delayed), input.filename, valueLine.line, alip);
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
		
		public void LoadIndex_Delayed(object loadedObj, string filename, int line, object param) {
			ArrayListIndexPair alip = (ArrayListIndexPair) param;
			alip.list[alip.index] = loadedObj;
		}
		
		private class ArrayListIndexPair {
			internal ArrayList list;
			internal int index;
		}
	}
}