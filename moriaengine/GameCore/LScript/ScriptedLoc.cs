using System.Collections.Generic;
using System.Linq;
using SteamEngine.Common;

namespace SteamEngine.LScript {

	public class ScriptedLocStringCollection : LocStringCollection, IUnloadable {
		public ScriptedLocStringCollection(Language language, string assemblyName, string defname, IEnumerable<KeyValuePair<string, string>> entriesByName)
			: base(language, assemblyName, defname, entriesByName) {
		}

		public void Unload() {
			LocManager.UnregisterLoc(this);
			this.IsUnloaded = true;
		}

		public bool IsUnloaded { get; private set; }

		internal static IUnloadable Load(PropsSection section) {
			string defname = section.HeaderName;

			var oldLoc = LocManager.GetLoc(defname, Language.Default);
			if (oldLoc != null) {
				Logger.WriteError(section.Filename, section.HeaderLine, "ScriptedLoc " + LogStr.Ident(defname) + " defined multiple times. Ignoring");
				return null;
			}

			int n = Tools.GetEnumLength<Language>();
			IUnloadable[] langs = new IUnloadable[n];

			for (int i = 0; i < n; i++) {
				ScriptedLocStringCollection newLoc = new ScriptedLocStringCollection((Language) i, assemblyName: "LScript", defname: defname,
					entriesByName: section.PropsLines.Select(line => new KeyValuePair<string, string>(line.Name, line.Value)));
				langs[i] = newLoc;
			}

			if (section.TriggerCount > 1) {
				Logger.WriteWarning(section.Filename, section.HeaderLine, "Triggers in a ScriptLoc are nonsensual (and ignored).");
			}

			return new UnloadableGroup(langs);
		}
	}

	public class UnloadableGroup : IUnloadable {
		readonly IUnloadable[] array;

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
