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
using System.Text.RegularExpressions;
using SteamEngine.Packets;
using SteamEngine.Common;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {
	[SaveableClass]
	public class RankSystem : Disposable,  IList<Rank> {
		private List<Rank> list = new List<Rank>();

		private string name;

		[LoadingInitializer]
		public RankSystem() {
		}

		public RankSystem(string name) {
			this.name = name;
		}

		public int IndexOf(Rank item) {
			return this.list.IndexOf(item);
		}

		public void Insert(int index, Rank item) {
			this.ClearAndSetRS(item);
			this.list.Insert(index, item);
		}

		public void RemoveAt(int index) {
			this.ClearRSAtIndex(index);
			this.list.RemoveAt(index);
		}

		public Rank this[int index] {
			get {
				return this.list[index];
			}
			set {
				this.ClearRSAtIndex(index);
				this.ClearAndSetRS(value);
				this.list[index] = value;
			}
		}

		public void Add(Rank item) {
			this.ClearAndSetRS(item);
			this.list.Add(item);
		}

		public void Clear() {
			for (int i = 0, n = this.list.Count; i < n; i++) {
				this.ClearRSAtIndex(i);
			}
			this.list.Clear();
		}

		public bool Contains(Rank item) {
			return this.list.Contains(item);
		}

		public int Count {
			get { return this.list.Count; ; }
		}

		public bool Remove(Rank item) {
			int index = this.list.IndexOf(item);
			if (index >= 0) {
				this.list.RemoveAt(index);
				item.InternalSetRankSystem(null);
				return true;
			}
			return false;
		}

		public IEnumerator<Rank> GetEnumerator() {
			return this.list.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return this.list.GetEnumerator();
		}

		void ICollection<Rank>.CopyTo(Rank[] array, int arrayIndex) {
			this.list.CopyTo(array, arrayIndex);
		}

		bool ICollection<Rank>.IsReadOnly {
			get { return false; }
		}


		private void ClearRSAtIndex(int index) {
			Rank previous = this.list[index];
			if (previous != null) {
				previous.InternalSetRankSystem(null);
			}
		}

		private void ClearAndSetRS(Rank item) {
			RankSystem rs = item.RankSystem;
			if (rs != null) {
				rs.Remove(item);
			}
			item.InternalSetRankSystem(this);
		}

		[Save]
		public virtual void Save(SaveStream output) {
			if (this.name != null) {
				output.WriteValue("name", this.name);
			}
			for (int i = 0, n = this.list.Count; i < n; i++) {
				output.WriteValue(i.ToString(), this.list[i]);
			}
		}

		[LoadLine]
		public virtual void LoadLine(string filename, int line, string valueName, string valueString) {
			switch (valueName) {
				case "name":
					this.name = (string) ObjectSaver.OptimizedLoad_String(valueString);
					return;
				default:
					int i;
					if (ConvertTools.TryParseInt32(valueName, out i)) {
						ObjectSaver.Load(valueString, this.Load_Member, filename, line, i);
						return;
					}
					break;
			}
			throw new ScriptException("Invalid data '" + LogStr.Ident(valueName) + "' = '" + LogStr.Number(valueString) + "'.");
		}

		private void Load_Member(object resolvedObject, string filename, int line, object parameter) {
			Rank loaded = (Rank) resolvedObject;
			int index = (int) parameter;
			while (this.list.Count < index) {
				this.list.Add(null);
			}
			this[index] = loaded;
		}

		protected override void On_DisposeManagedResources() {
			this.Clear();
			base.On_DisposeManagedResources();
		}

	}

	[Dialogs.ViewableClass]
	public class Rank : Role {
		private RankSystem rankSystem;

		internal Rank(RankDef def, RoleKey key)
			: base(def, key) {
		}

		public RankSystem RankSystem {
			get {
				return this.rankSystem;
			}
		}

		internal void InternalSetRankSystem(RankSystem rankSystem) {
			this.rankSystem = rankSystem;
		}

	}

	public class RankDef : RoleDef {
		public RankDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		protected override Role CreateImpl(RoleKey key) {
			return new Rank(this, key);
		}
	}
}		
