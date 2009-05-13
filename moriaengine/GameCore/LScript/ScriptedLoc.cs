using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace SteamEngine.Common {


	public class ScriptedLoc : ServLoc, IUnloadable {
		private string defname;
		private bool unloaded;

		private ScriptedLoc(string defname) {
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

			ServLoc oldLoc = LocManager.GetLoc(defname, Language.Default);
			if (oldLoc != null) {
				Logger.WriteError(section.Filename, section.HeaderLine, "ScriptedLoc " + LogStr.Ident(defname) + " defined multiple times. Ignoring");
				return null;
			}

			int n = Tools.GetEnumLength<Language>();
			IUnloadable[] langs = new IUnloadable[n];

			for (int i = 0; i < n; i++) {
				ScriptedLoc newLoc = new ScriptedLoc(defname);
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

}
