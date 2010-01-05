
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
using System.Reflection;
using System.Globalization;
using System.Text.RegularExpressions;
using SteamEngine.Common;
using SteamEngine.Persistence;

namespace SteamEngine {

	//adds a field the value of which is unique among all instances of TDef
	public abstract class AbstractIndexedDef<TDef, TIndex> : AbstractDef
		where TDef : AbstractIndexedDef<TDef, TIndex> {

		//for string indices, be case insensitive
		private static Dictionary<TIndex, TDef> byIndex = (typeof(TIndex) == typeof(string)) ?
			new Dictionary<TIndex, TDef>((IEqualityComparer<TIndex>) StringComparer.OrdinalIgnoreCase) :
			new Dictionary<TIndex, TDef>();

		private TIndex index;

		protected AbstractIndexedDef(string defname, string filename, int headerLine) : base(defname, filename, headerLine) {
		}

		public static TDef ByDefIndex(TIndex index) {
			TDef retVal;
			byIndex.TryGetValue(index, out retVal);
			return retVal;
		}

		public TIndex DefIndex {
			get {
				return this.index;
			}
			protected set {
				this.index = value;
			}
		}
		
		public static int IndexedCount {
			get {
				return byIndex.Count;
			}
		}

		public static ICollection<TDef> AllIndexedDefs {
			get {
				return byIndex.Values;
			}
		}

		protected internal override void Register() {
			try {
				TDef previous;
				if (byIndex.TryGetValue(this.index, out previous)) {
					Sanity.IfTrueThrow(previous != this, "previous != this when registering AbstractIndexedDef " + this.index);
				}
				byIndex[this.index] = (TDef) this;
			} finally {
				base.Register();
			}
		}

		protected override void Unregister() {
			try {
				TDef previous;
				if (byIndex.TryGetValue(this.index, out previous)) {
					Sanity.IfTrueThrow(previous != this, "previous != this when unregistering AbstractIndexedDef " + this.index);
				}
				byIndex.Remove(this.index);
			} finally {
				base.Unregister();
			}
		}
	}
}
