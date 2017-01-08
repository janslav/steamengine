using System.Collections.Generic;
using SteamEngine.Common;

namespace SteamEngine.LScript {

	public class ScriptedLocStringCollection : LocStringCollection, IUnloadable {
		private string defname;
		private bool unloaded;

		private ScriptedLocStringCollection(string defname) {
			this.defname = defname;
		}

		public override string AssemblyName {
			get {
				return "LScript";
			}
		}

		public override string Defname {
			get {
				return this.defname;
			}
		}

		public void Unload() {
			LocManager.UnregisterLoc(this);
			this.unloaded = true;
		}

		public bool IsUnloaded {
			get {
				return this.unloaded;
			}
		}

		internal static IUnloadable Load(PropsSection section) {
			string defname = section.HeaderName;

			LocStringCollection oldLoc = LocManager.GetLoc(defname, Language.Default);
			if (oldLoc != null) {
				Logger.WriteError(section.Filename, section.HeaderLine, "ScriptedLoc " + LogStr.Ident(defname) + " defined multiple times. Ignoring");
				return null;
			}

			int n = Tools.GetEnumLength<Language>();
			IUnloadable[] langs = new IUnloadable[n];

			for (int i = 0; i < n; i++) {
				ScriptedLocStringCollection newLoc = new ScriptedLocStringCollection(defname);
				newLoc.Init(GetEntriesFromSection(section), (Language)i);
				langs[i] = newLoc;
			}

			if (section.TriggerCount > 1) {
				Logger.WriteWarning(section.Filename, section.HeaderLine, "Triggers in a ScriptLoc are nonsensual (and ignored).");
			}

			return new UnloadableGroup(langs);
		}

		private static IEnumerable<KeyValuePair<string, string>> GetEntriesFromSection(PropsSection section) {
			foreach (PropsLine line in section.PropsLines) {
				yield return new KeyValuePair<string, string>(line.Name, line.Value);
			}
		}
	}

	public class UnloadableGroup : IUnloadable {
		IUnloadable[] array;

		public UnloadableGroup(params IUnloadable[] array) {
			this.array = array;
		}

		public void Unload() {
			foreach (IUnloadable member in this.array) {
				if (member != null) {
					member.Unload();
				}
			}
		}

		public bool IsUnloaded {
			get {
				foreach (IUnloadable member in this.array) {
					if (member != null) {
						if (member.IsUnloaded) {
							return true;
						}
					}
				}
				return false;
			}
		}
	}
}
