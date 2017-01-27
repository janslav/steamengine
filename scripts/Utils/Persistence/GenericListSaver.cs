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
using System.Linq;
using Shielded;
using SteamEngine.Common;
using SteamEngine.Persistence;
using SteamEngine.Scripting.Compilation;

namespace SteamEngine.CompiledScripts {

	public sealed class GenericListSaver : ISaveImplementor, IDeepCopyImplementor {
		public string HeaderName {
			get {
				return "GenericList";
			}
		}

		public Type HandledType {
			get {
				return typeof(List<>);
			}
		}

		public void Save(object objToSave, SaveStream writer) {
			var list = (IList) objToSave;
			var listType = list.GetType();
			var memberType = listType.GetGenericArguments()[0];
			var count = list.Count;
			writer.WriteValue("count", count);
			writer.WriteLine("type=" + GetTypeName(memberType));
			for (var i = 0; i < count; i++) {
				writer.WriteValue(i.ToString(), list[i]);
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
				var elemType = ParseType(pl);

				var typeOfList = typeof(List<>).MakeGenericType(elemType);
				var list = (IList) Activator.CreateInstance(typeOfList, count);

				for (var i = 0; i < count; i++) {
					list.Add(null);
					var valueLine = input.PopPropsLine(i.ToString());
					currentLineNumber = valueLine.Line;
					var alip = new GenericListLoadHelper(list, i, elemType);
					ObjectSaver.Load(valueLine.Value, this.DelayedLoad_Index, input.Filename, valueLine.Line, alip);
				}
				return list;
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

		public static Type ParseType(PropsLine pl) {
			var elemType = (ClassManager.GetType(pl.Value)
				?? Type.GetType(pl.Value, throwOnError: false, ignoreCase: true))
				?? AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(pl.Value, throwOnError: false, ignoreCase: true)).First(t => t != null);

			if (elemType == null) {
				throw new SEException("Element type not recognised.");
			}
			return elemType;
		}

		public static string GetTypeName(Type type) {
			if (ClassManager.GetType(type.Name) == type) {//steamengine class
				return type.Name;
			}
			return type.FullName;
		}

		public void DelayedLoad_Index(object loadedObj, string filename, int line, object param) {
			var alip = (GenericListLoadHelper) param;
			alip.list[alip.index] = ConvertTools.ConvertTo(alip.elemType, loadedObj);
		}

		public void DelayedCopy_Index(object loadedObj, object param) {
			var alip = (GenericListLoadHelper) param;
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
			var copyFromList = (IList) copyFrom;
			var n = copyFromList.Count;

			var elemType = copyFrom.GetType().GetGenericArguments()[0];
			var typeOfList = typeof(List<>).MakeGenericType(elemType);
			var newList = (IList) Activator.CreateInstance(typeOfList, n);

			for (var i = 0; i < n; i++) {
				newList.Add(null);
				var alip = new GenericListLoadHelper(newList, i, elemType);
				DeepCopyFactory.GetCopyDelayed(copyFromList[i], this.DelayedCopy_Index, alip);
			}
			return newList;
		}
	}
}