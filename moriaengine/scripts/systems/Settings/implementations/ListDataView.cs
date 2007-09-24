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
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts.Dialogs {
	public class ListDataView : ButtonDataFieldView, IDataView {
		public Type HandledType {
			get {
				return typeof(ArrayList);
			}
		}

		public string GetName(object instance) {
			return instance.GetType().Name;
		}

		public int GetActionButtonsCount(object instance) {
			return 1;
		}

		public int GetFieldsCount(object instance) {
			return ((IList) target).Count+1;
		}

		public IEnumerable<IDataFieldView> GetDataFieldsPage(int firstLineIndex, int maxLinesOnPage) {
			throw new Exception("The method or operation is not implemented.");
		}

		public IEnumerable<ButtonDataFieldView> GetActionButtonsPage(int firstLineIndex, int maxLinesOnPage) {
			yield return this;
		}

		private class CountField : ReadOnlyDataFieldView {

			public override string Name {
				get { "Count"; }
			}

			public override object GetValue(object target) {
				return ((IList) target).Count;
			}

			public override string GetStringValue(object target) {
				return ((IList) target).Count.ToString();
			}
		}

		private class IndexField : ReadOnlyDataFieldView {

			public override string Name {
				get { "Count"; }
			}

			public override object GetValue(object target) {
				return ((IList) target).Count;
			}

			public override string GetStringValue(object target) {
				return ((IList) target).Count.ToString();
			}
		}

		#region ButtonDataFieldView (Clear)
		public override string Name {
			get { 
				return "Clear"; 
			}
		}

		public override void OnButton(object target) {
			((IList) target).Clear();
		}
		#endregion ButtonDataFieldView (Clear)
	}
}