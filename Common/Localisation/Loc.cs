using System;

namespace SteamEngine.Common {
	public static class Loc<T> where T : CompiledLocStringCollection<T> {
		private static T[] loadedLanguages = LoadLanuages();

		private static T[] LoadLanuages() {
			var n = Tools.GetEnumLength<Language>();
			var langs = new T[n];

			for (var i = 0; i < n; i++)
			{
				CompiledLocStringCollection<T>.languageBeingActivated = (Language) i;
				var l = Activator.CreateInstance<T>();
				langs[i] = l;
			}

			return langs;
		}

		//called by ClassManager (reflectively), does nothing but will cause the class to init, creating/reading the txt files
		public static void Init() {
		}

		public static T Get(string languageCode) {
			var lan = LocManager.TranslateLanguageCode(languageCode);

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