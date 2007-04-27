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
using SteamEngine.Common;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {
	public class ArraySaver : ISaveImplementor {
		public string HeaderName { get {
			return "Array";
		} }

		public Type HandledType { get {
			return typeof(Array);
		} }
		
		public void Save(object objToSave, SaveStream writer) {
			Array arr = (Array) objToSave;
			Type arrType = arr.GetType();
			if (arrType.GetArrayRank() > 1) {
				throw new NotImplementedException("Multi-dimensional array saving not implemented.");
			}
			Type elemType = arrType.GetElementType();
			if (ClassManager.GetType(elemType.Name) == elemType) {//steamengine class
				writer.WriteLine("type="+ elemType.Name);
			} else {
				writer.WriteLine("type="+ elemType.FullName);
			}
			
			int length = arr.Length;
			writer.WriteValue("length", length);
			for (int i = 0; i<length; i++) {
				writer.WriteValue(i.ToString(), arr.GetValue(i));
			}
		}

		public object LoadSection(PropsSection input) {
			int currentLineNumber = input.headerLine;
			try {
				PropsLine pl = input.PopPropsLine("type");
				currentLineNumber = pl.line;
				if (pl == null) {
					throw new Exception("Array save section missing the Type line.");
				}

				Type elemType = ClassManager.GetType(pl.value);
				if (elemType == null) {
					elemType = Type.GetType(pl.value, false, true);
				}
				if (elemType == null) {
					throw new Exception("Array element type not recognised.");
				}

				PropsLine lengthLine = input.PopPropsLine("length");
				currentLineNumber = lengthLine.line;
				int length = int.Parse(lengthLine.value);

				Array arr = Array.CreateInstance(elemType, length);

				for (int i = 0; i<length; i++) {
					PropsLine valueLine = input.PopPropsLine(i.ToString());
					currentLineNumber = valueLine.line;
					ArrayIndexPair alip = new ArrayIndexPair();
					alip.index = i;
					alip.arr = arr;
					ObjectSaver.Load(valueLine.value, new LoadObjectParam(LoadIndex_Delayed), input.filename, valueLine.line, alip);
				}
				return arr;
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
			ArrayIndexPair alip = (ArrayIndexPair) param;
			alip.arr.SetValue(loadedObj, alip.index);
		}
		
		private class ArrayIndexPair {
			internal Array arr;
			internal int index;
		}
	}
}