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

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;

namespace SteamEngine.Common {

	public class NumberedLocStringCollection : LocStringCollection {
		string defname;
		string assemblyName;
		List<string> entriesByNumber = new List<string>();
		ReadOnlyCollection<string> entriesReadonly;

		public NumberedLocStringCollection(string defname, string assemblyName,
			IList<string> strings, Language language) {

			this.defname = defname;
			this.assemblyName = assemblyName;

			this.entriesReadonly = new ReadOnlyCollection<string>(this.entriesByNumber);

			this.Init(GetEntriesFromList(strings), language);
		}
		
		private static IEnumerable<KeyValuePair<string, string>> GetEntriesFromList(IList<string> strings) {
			for (int i = 0, n = strings.Count; i < n; i++) {
				yield return new KeyValuePair<string, string>(
					i.ToString(CultureInfo.InvariantCulture),
					strings[i]);
			}
		}

		protected override void ProtectedSetEntry(string entryName, string entry) {
			base.ProtectedSetEntry(entryName, entry);
			int index = ConvertTools.ParseInt32(entryName);
			
			while (this.entriesByNumber.Count <= index) {
				this.entriesByNumber.Add(null);
			}
			this.entriesByNumber[index] = entry;
		}

		public override string Defname {
			get {
				return this.defname;
			}
		}

		public override string AssemblyName {
			get {
				return this.assemblyName;
			}
		}

		public ReadOnlyCollection<string> Entries {
			get {
				return this.entriesReadonly;
			}
		}
	}
}
