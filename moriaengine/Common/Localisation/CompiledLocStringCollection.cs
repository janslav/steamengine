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
	public abstract class CompiledLocStringCollection : LocStringCollection {
		protected CompiledLocStringCollection() {
		}

		protected override void ProtectedSetEntry(string entryName, string entry) {
			base.ProtectedSetEntry(entryName, entry);
			FieldInfo field = this.GetType().GetField(entryName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase);
			if ((field != null) || (field.DeclaringType.IsAbstract)) {
				field.SetValue(this, String.Intern(entry));
			}
		}

		public override string Defname {
			get {
				return this.GetType().Name;
			}
		}

		public override string AssemblyName {
			get {
				return GetAssemblyTitle(this.GetType().Assembly);
			}
		}

		internal void Init(Language lan) {
			this.Init(this.GetEntriesFromFields(), lan);
		}

		//protected internal override void Init(IEnumerable<KeyValuePair<string, string>> entriesByName, Language lan) {
		//    base.Init(entriesByName, lan);
		//}

		private IEnumerable<KeyValuePair<string, string>> GetEntriesFromFields() {
			foreach (FieldInfo fi in this.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)) {
				if (fi.FieldType == typeof(string) && !fi.DeclaringType.IsAbstract) {
					yield return new KeyValuePair<string, string>(fi.Name, 
						(string) fi.GetValue(this));
				}
			}
		}

		private static string GetAssemblyTitle(Assembly assembly) {
			AssemblyTitleAttribute titleAttr = (AssemblyTitleAttribute) Attribute.GetCustomAttribute(assembly, typeof(AssemblyTitleAttribute));
			if (titleAttr != null) {
				return titleAttr.Title;
			}
			return assembly.GetName().Name;
		}
	}

	public static class Loc<T> where T : CompiledLocStringCollection {
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

		//called by ClassManager (reflectively), does nothing but will cause the class to init, creating/reading the txt files
		public static void Init() {
		}

		public static T Get(string languageCode) {
			Language lan = LocManager.TranslateLanguageCode(languageCode);

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
