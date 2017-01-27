using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Shielded;

namespace SteamEngine.Common {

	public enum Language {
		Default, English = Default,
		Czech
	}

	public static class LocManager {
		private static readonly ShieldedDictNc<string, LocStringCollection>[] loadedLanguages = InitDictArrays();

		private static ShieldedDictNc<string, LocStringCollection>[] InitDictArrays() {
			var n = Tools.GetEnumLength<Language>();
			var langs = new ShieldedDictNc<string, LocStringCollection>[n];
			for (var i = 0; i < n; i++) {
				langs[i] = new ShieldedDictNc<string, LocStringCollection>(comparer: StringComparer.InvariantCultureIgnoreCase);
			}

			return langs;
		}

		[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
		public static void RegisterLoc(LocStringCollection newLoc) {
			Shield.AssertInTransaction();

			var dict = loadedLanguages[(int) newLoc.Language];
			LocStringCollection previous;
			if (dict.TryGetValue(newLoc.Defname, out previous)) {
				if (previous != newLoc) {
					throw new SEException("Loc instance '" + newLoc.Defname + "' already exists");
				}
			} else {
				dict.Add(newLoc.Defname, newLoc);
			}
		}

		[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
		public static void UnregisterLoc(LocStringCollection newLoc) {
			Shield.AssertInTransaction();

			var dict = loadedLanguages[(int) newLoc.Language];
			LocStringCollection previous;
			if (dict.TryGetValue(newLoc.Defname, out previous)) {
				if (previous != newLoc) {
					throw new SEException("Loc instance '" + newLoc.Defname + "' was not registered.");
				}
			} else {
				dict.Remove(newLoc.Defname);
			}
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
			Shield.AssertInTransaction();
			//we copy the list first, because we're going to remove some entries in a foreach loop
			var allLocCollections = loadedLanguages[(int) Language.Default].Values.ToList();

			foreach (var locCollection in allLocCollections) {
				if (locCollection.GetType().Assembly == assemblyBeingUnloaded) {
					foreach (var langDict in loadedLanguages) {
						langDict.Remove(locCollection.Defname);
					}
				}
			}
		}

		public static void ForgetInstancesOfType(Type type) {
			Shield.AssertInTransaction();
			//we copy the list first, because we're going to remove some entries in a foreach loop
			var allLocCollections = loadedLanguages[(int) Language.Default].Values.ToList();

			foreach (var locCollection in allLocCollections) {
				if (locCollection.GetType() == type) {
					foreach (var langDict in loadedLanguages) {
						langDict.Remove(locCollection.Defname);
					}
				}
			}
		}
	}
}
