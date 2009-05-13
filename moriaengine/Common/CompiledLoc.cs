using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace SteamEngine.Common {

	public abstract class AbstractLoc : ServLoc {
		protected AbstractLoc() {
		}

		protected override void SetEntry(string entryName, string entry) {
			base.SetEntry(entryName, entry);
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

	//client has "cliloc", so we have servloc :)

	//T is supposed to be a simple class with lot of public string fields representing the localised messages
	public static class CompiledLoc<T> where T : AbstractLoc {
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

		//called by ClassManager (reflectively), does nothing but will cause the class to init, creating the txt files
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
