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
using Shielded;
using SteamEngine.Common;
using SteamEngine.Parsing;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {
	public sealed class ArraySaver : ISaveImplementor, IDeepCopyImplementor {
		public string HeaderName {
			get {
				return "Array";
			}
		}

		public Type HandledType {
			get {
				return typeof(Array);
			}
		}

		public void Save(object objToSave, SaveStream writer) {
			var arr = (Array) objToSave;
			var arrType = arr.GetType();
			if (arrType.GetArrayRank() > 1) {
				throw new SEException("Multi-dimensional array saving not implemented.");
			}
			var elemType = arrType.GetElementType();
			writer.WriteLine("type=" + GenericListSaver.GetTypeName(elemType));

			var length = arr.Length;
			writer.WriteValue("length", length);
			for (var i = 0; i < length; i++) {
				writer.WriteValue(i.ToString(), arr.GetValue(i));
			}
		}

		public object LoadSection(PropsSection input) {
			var currentLineNumber = input.HeaderLine;
			try {
				var pl = input.PopPropsLine("type");
				currentLineNumber = pl.Line;
				var elemType = GenericListSaver.ParseType(pl);

				var lengthLine = input.PopPropsLine("length");
				currentLineNumber = lengthLine.Line;
				var length = ConvertTools.ParseInt32(lengthLine.Value);

				var arr = Array.CreateInstance(elemType, length);

				for (var i = 0; i < length; i++) {
					var valueLine = input.PopPropsLine(i.ToString());
					currentLineNumber = valueLine.Line;
					var alip = new ArrayLoadHelper(arr, i, elemType);
					ObjectSaver.Load(valueLine.Value, this.DelayedLoad_Index, input.Filename, valueLine.Line, alip);
				}
				return arr;
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

		public void DelayedLoad_Index(object loadedObj, string filename, int line, object param) {
			var alip = (ArrayLoadHelper) param;
			alip.arr.SetValue(ConvertTools.ConvertTo(alip.elemType, loadedObj), alip.index);
		}

		public void DelayedCopy_Index(object loadedObj, object param) {
			var alip = (ArrayLoadHelper) param;
			alip.arr.SetValue(ConvertTools.ConvertTo(alip.elemType, loadedObj), alip.index);
		}

		private class ArrayLoadHelper {
			internal Array arr;
			internal int index;
			internal Type elemType;

			internal ArrayLoadHelper(Array arr, int index, Type elemType) {
				this.index = index;
				this.arr = arr;
				this.elemType = elemType;
			}
		}

		public object DeepCopy(object copyFrom) {
			var copyFromArray = (Array) copyFrom;
			var n = copyFromArray.Length;
			var elemType = copyFromArray.GetType().GetElementType();
			var newArray = Array.CreateInstance(elemType, n);
			for (var i = 0; i < n; i++) {
				var aip = new ArrayLoadHelper(newArray, i, elemType);
				DeepCopyFactory.GetCopyDelayed(copyFromArray.GetValue(i), this.DelayedCopy_Index, aip);
			}
			return newArray;
		}
	}
}