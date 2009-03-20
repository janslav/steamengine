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

	public class Loc {
		const string servLocDir = "Languages";
		const string defaultText = "<TextStringNotAvailable>";

		//name=value //comment
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Member")]
		public static readonly Regex valueRE = new Regex(@"^\s*(?<name>.*?)((\s*=\s*)|(\s+))(?<value>.*?)\s*(//(?<comment>.*))?$",
			RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

		private readonly Dictionary<string, string> valuesByName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		private Language language;


		public Dictionary<string, string> ValuesByName {
			get { return valuesByName; }
		}

		public Language Language {
			get { return this.language; }
		}

		internal void Init(Language lan) {
			this.language = lan;

			Type languageClass = this.GetType();

			Dictionary<string, FieldInfo> fieldsDict = this.GetFieldInfoDict(languageClass);


			string path = Tools.CombineMultiplePaths(".", servLocDir,
				Enum.GetName(typeof(Language), lan),
				GetAssemblyTitle(languageClass.Assembly),
				String.Concat(languageClass.Name, ".txt"));


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
							FieldInfo field;
							if (fieldsDict.TryGetValue(name, out field)) {
								string value = String.Intern(gc["value"].Value);
								field.SetValue(this, value);
								this.valuesByName[name] = value;
								fieldsDict.Remove(name);
							} else {
								Logger.WriteWarning(path, lineNum, "Unknown or duplicite value name '" + name + "'. Ignoring.");
							}
						} else {
							Logger.WriteWarning(path, lineNum, "Unknown format of line");
						}
					}
				}
			}


			//lines that are on the class but aren't in the text file
			if ((fieldsDict.Count > 0)) {
				Tools.EnsureDirectory(Path.GetDirectoryName(path));
				using (StreamWriter writer = new StreamWriter(file.Open(FileMode.Append, FileAccess.Write))) {

					foreach (KeyValuePair<string, FieldInfo> pair in fieldsDict) {
						string name = pair.Key;
						string value = (string) pair.Value.GetValue(this);
						writer.WriteLine(name.PadRight(20) + "= " + value);
						this.valuesByName[name] = value;
					}
				}
			}
		}

		private Dictionary<string, FieldInfo> GetFieldInfoDict(Type languageClass) {
			Dictionary<string, FieldInfo> fieldsDict = new Dictionary<string, FieldInfo>(StringComparer.InvariantCultureIgnoreCase);

			foreach (FieldInfo fi in languageClass.GetFields(BindingFlags.Public | BindingFlags.Instance)) {
				if (fi.FieldType == typeof(string)) {
					fieldsDict[fi.Name] = fi;
					string value = (string) fi.GetValue(this);
					if (value == null) {
						value = defaultText;
					} else {
						value = string.Intern(value);
					}
					fi.SetValue(this, value);
				}
			}

			return fieldsDict;
		}

		private static string GetAssemblyTitle(Assembly assembly) {
			AssemblyTitleAttribute titleAttr = (AssemblyTitleAttribute) Attribute.GetCustomAttribute(assembly, typeof(AssemblyTitleAttribute));
			if (titleAttr != null) {
				return titleAttr.Title;
			}
			return assembly.GetName().Name;
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

	//client has "cliloc", so we have servloc :)

	//T is supposed to be a simple class with lot of public string fields representing the localised messages
	public static class ServLoc<T> where T : Loc {
		private static T[] loadedLanguages = LoadLanuages();

		private static T[] LoadLanuages() {
			int n = Tools.GetEnumLength<Language>();
			T[] langs = new T[n];

			for (int i = 0; i < n; i++) {
				T l = Activator.CreateInstance<T>();
				l.Init((Language) i);
				langs[i] = l;
			}

			return langs;
		}

		//run by ClassManager, does nothing but will cause the class to init, creating the txt files
		public static void Init() {
		}

		public static T Get(string languageCode) {
			Language lan = Loc.TranslateLanguageCode(languageCode);

			return loadedLanguages[(int) lan];
		}

		public static T Get(Language language) {
			return loadedLanguages[(int) language];
		}

		public static T Default {
			get {
				return loadedLanguages[(int) Language.Default];
			}
		}
	}
}
