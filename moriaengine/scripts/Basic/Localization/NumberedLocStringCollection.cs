using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace SteamEngine.Common {

	//children are supposed to be simple classes with some string fields (public or internal, doesn't really matter) representing the localised messages.
	//Then it should be used with the Loc<> class

	//the system will then make sure it's available in all languages and corresponding txt files are created and maintained in the \Language\ subdir
	public class NumberedLocStringCollection : LocStringCollection {
		string defname;
		string assemblyName;
		List<string> strings = new List<string>();
		ReadOnlyCollection<string> wrapper;

		public NumberedLocStringCollection(string defname, string assemblyName,
			IList<string> strings, Language language) {

			this.defname = defname;
			this.assemblyName = assemblyName;

			this.wrapper = new ReadOnlyCollection<string>(this.strings);

			base.Init(GetEntriesFromList(strings), language);
		}
		
		private static IEnumerable<KeyValuePair<string, string>> GetEntriesFromList(IList<string> strings) {
			for (int i = 0, n = strings.Count; i < n; i++) {
				yield return new KeyValuePair<string, string>(
					i.ToString(System.Globalization.CultureInfo.InvariantCulture),
					strings[i]);
			}
		}

		protected override void ProtectedSetEntry(string entryName, string entry) {
			base.ProtectedSetEntry(entryName, entry);
			int index = ConvertTools.ParseInt32(entryName);
			
			while (this.strings.Count <= index) {
				this.strings.Add(null);
			}
			this.strings[index] = entry;
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
				return this.wrapper;
			}
		}
	}
}
