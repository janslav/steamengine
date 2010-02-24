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

	public static class LocManager {
		private static Dictionary<string, LocStringCollection>[] loadedLanguages = InitDictArrays();

		private static Dictionary<string, LocStringCollection>[] InitDictArrays() {
			int n = Tools.GetEnumLength<Language>();
			Dictionary<string, LocStringCollection>[] langs = new Dictionary<string, LocStringCollection>[n];

			for (int i = 0; i < n; i++) {
				langs[i] = new Dictionary<string, LocStringCollection>(StringComparer.InvariantCultureIgnoreCase);
			}

			return langs;
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
		public static void RegisterLoc(LocStringCollection newLoc) {
			string className = newLoc.Defname;
			Language lan = newLoc.Language;
			if (loadedLanguages[(int) lan].ContainsKey(className)) {
				throw new SEException("Loc instance '" + className + "' already exists");
			}
			loadedLanguages[(int) lan].Add(className, newLoc);
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
		public static void UnregisterLoc(LocStringCollection newLoc) {
			string className = newLoc.Defname;
			Language lan = newLoc.Language;
			loadedLanguages[(int) lan].Remove(className);
		}

		public static LocStringCollection GetLoc(string defname, Language language) {
			LocStringCollection retVal;
			loadedLanguages[(int) language].TryGetValue(defname, out retVal);
			return retVal;
		}

		public static string GetEntry(string defname, string entryName) {
			return GetEntry(defname, entryName, Language.Default);
		}

		public static string GetEntry(string defname, string entryName, Language language) {
			LocStringCollection retVal;
			if (loadedLanguages[(int) language].TryGetValue(defname, out retVal)) {
				return retVal.GetEntry(entryName);
			}
			return LocStringCollection.defaultText;
		}

		public static Language TranslateLanguageCode(string languageCode) {
			languageCode = languageCode.ToLowerInvariant();
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

		public static void ForgetInstancesFromAssembly(Assembly assemblyBeingUnloaded) {
			Dictionary<string, LocStringCollection> defaults = loadedLanguages[(int)Language.Default];
			LocStringCollection[] allLocs = new LocStringCollection[defaults.Count];
			defaults.Values.CopyTo(allLocs, 0); //we copy the list first, because we're going to remove some entries in a foreach loop

			foreach (LocStringCollection loc in allLocs) {
				if (loc.GetType().Assembly == assemblyBeingUnloaded) {
					foreach (Dictionary<string, LocStringCollection> langDict in loadedLanguages) {
						langDict.Remove(loc.Defname);
					}
				}
			}
		}

		public static void ForgetInstancesOfType(Type type) {
			Dictionary<string, LocStringCollection> defaults = loadedLanguages[(int) Language.Default];
			LocStringCollection[] allLocs = new LocStringCollection[defaults.Count];
			defaults.Values.CopyTo(allLocs, 0); //we copy the list first, because we're going to remove some entries in a foreach loop

			foreach (LocStringCollection loc in allLocs) {
				if (loc.GetType() == type) {
					foreach (Dictionary<string, LocStringCollection> langDict in loadedLanguages) {
						langDict.Remove(loc.Defname);
					}
				}
			}
		}
	}
}
