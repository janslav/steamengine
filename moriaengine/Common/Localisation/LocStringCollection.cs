using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace SteamEngine.Common {

	public abstract class LocStringCollection : IEnumerable<KeyValuePair<string, string>> {
		const string servLocDir = "Languages";
		internal const string defaultText = "<LocalisationEntryNotAvailable>";

		//name=value //comment
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Member")]
		public static readonly Regex valueRE = new Regex(@"^\s*(?<name>.*?)((\s*=\s*)|(\s+))(?<value>.*?)\s*(//(?<comment>.*))?$",
			RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

		private Dictionary<string, string> entriesByName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		private Language language;

		protected LocStringCollection() {
		}

		public string GetEntry(string entryName) {
			string value;
			if (this.entriesByName.TryGetValue(entryName, out value)) {
				return value;
			}
			return defaultText;
		}

		internal virtual void InternalSetEntry(string entryName, string entry) {
			if (this.entriesByName.ContainsKey(entryName)) {
				this.entriesByName[entryName] = String.Intern(entry); ;
			}
		}

		public bool HasEntry(string entryName) {
			return this.entriesByName.ContainsKey(entryName);
		}

		public Language Language {
			get {
				return this.language;
			}
		}

		public abstract string AssemblyName {
			get;
		}

		public abstract string Defname {
			get;
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
		public IEnumerator<KeyValuePair<string, string>> GetEnumerator() {
			return this.entriesByName.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return this.entriesByName.GetEnumerator();
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "entriesByName"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "language"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
		internal protected virtual void Init(IEnumerable<KeyValuePair<string, string>> entriesByName, Language language) {			
			this.entriesByName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			Dictionary<string, string> helperList = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			foreach (KeyValuePair<string, string> pair in entriesByName) {
				this.entriesByName.Add(pair.Key, pair.Value);
				helperList.Add(pair.Key, pair.Value);
			}
			
			this.language = language;

			string path = Tools.CombineMultiplePaths(".", servLocDir,
				Enum.GetName(typeof(Language), language),
				this.AssemblyName,
				String.Concat(this.Defname, ".txt"));


			FileInfo file = new FileInfo(path);
			if (file.Exists) {
				using (StreamReader reader = file.OpenText()) {
					string line;
					int lineNum = 0;
					while ((line = reader.ReadLine()) != null) {
						lineNum++;
						line = line.Trim();
						if (String.IsNullOrEmpty(line) || line.StartsWith("//") || line.StartsWith("#")) {
							continue;
						}

						Match m = valueRE.Match(line);
						if (m.Success) {
							GroupCollection gc = m.Groups;
							string name = gc["name"].Value;
							if (this.HasEntry(name)) {
								if (helperList.ContainsKey(name)) {
									this.InternalSetEntry(name, gc["value"].Value);
									helperList.Remove(name);
								} else {
									Logger.WriteWarning(path, lineNum, "Duplicate value name '" + name + "'. Ignoring.");
								}
							} else {
								Logger.WriteWarning(path, lineNum, "Unknown value name '" + name + "'. Ignoring.");
							}
						} else {
							Logger.WriteWarning(path, lineNum, "Unknown format of line");
						}
					}
				}
			}


			//lines that are on the class but aren't in the text file, we append them to the file
			if ((helperList.Count > 0)) {
				Tools.EnsureDirectory(Path.GetDirectoryName(path));
				using (StreamWriter writer = new StreamWriter(file.Open(FileMode.Append, FileAccess.Write))) {
					foreach (KeyValuePair<string, string> pair in helperList) {
						writer.WriteLine(pair.Key.PadRight(32) + "= " + pair.Value);
					}
				}
			}

			LocManager.RegisterLoc(this);
		}

		public override string ToString() {
			return string.Concat(this.Defname,
				" (", Enum.GetName(typeof(Language), this.language), ")");
		}
	}
}
