using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace SteamEngine.Common {

	public enum Language {
		Default, English = Default,
		Czech
	}

	public abstract class ServLoc : IEnumerable<KeyValuePair<string, string>> {
		const string servLocDir = "Languages";
		internal const string defaultText = "<LocalisationEntryNotAvailable>";

		//name=value //comment
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Member")]
		public static readonly Regex valueRE = new Regex(@"^\s*(?<name>.*?)((\s*=\s*)|(\s+))(?<value>.*?)\s*(//(?<comment>.*))?$",
			RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

		private Dictionary<string, string> entriesByName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		private Language language;

		protected ServLoc() {
		}

		public virtual string GetEntry(string entryName) {
			string value;
			if (this.entriesByName.TryGetValue(entryName, out value)) {
				return value;
			}
			return defaultText;
		}

		protected virtual void SetEntry(string entryName, string entry) {
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

		public IEnumerator<KeyValuePair<string, string>> GetEnumerator() {
			return this.entriesByName.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return this.entriesByName.GetEnumerator();
		}

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
									this.SetEntry(name, gc["value"].Value);
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

	public static class LocManager {
		private static Dictionary<string, ServLoc>[] loadedLanguages = InitDictArrays();

		private static Dictionary<string, ServLoc>[] InitDictArrays() {
			int n = Tools.GetEnumLength<Language>();
			Dictionary<string, ServLoc>[] langs = new Dictionary<string, ServLoc>[n];

			for (int i = 0; i < n; i++) {
				langs[i] = new Dictionary<string, ServLoc>(StringComparer.InvariantCultureIgnoreCase);
			}

			return langs;
		}

		public static void RegisterLoc(ServLoc newLoc) {
			string className = newLoc.Defname;
			Language lan = newLoc.Language;
			if (loadedLanguages[(int) lan].ContainsKey(className)) {
				throw new SEException("Loc instance '" + className + "' already exists");
			}
			loadedLanguages[(int) lan].Add(className, newLoc);
		}

		public static void UnregisterLoc(ServLoc newLoc) {
			string className = newLoc.Defname;
			Language lan = newLoc.Language;
			loadedLanguages[(int) lan].Remove(className);
		}

		public static ServLoc GetLoc(string defname, Language language) {
			ServLoc retVal;
			loadedLanguages[(int) language].TryGetValue(defname, out retVal);
			return retVal;
		}

		public static string GetEntry(string defname, string entryName) {
			return GetEntry(defname, entryName, Language.Default);
		}

		public static string GetEntry(string defname, string entryName, Language language) {
			ServLoc retVal;
			if (loadedLanguages[(int) language].TryGetValue(defname, out retVal)) {
				return retVal.GetEntry(entryName);
			}
			return ServLoc.defaultText;
		}

		public static Language TranslateLanguageCode(string languageCode) {
			languageCode = languageCode.ToLower(System.Globalization.CultureInfo.InvariantCulture);
			switch (languageCode) {
				case "cz":
				case "cze":
				case "czech":
				case "cs":
				case "cesky":
				case "èesky":
				case "èeština":
				case "cs-cz":
					return Language.Czech;
			}
			return Language.English;
		}
	}
}
