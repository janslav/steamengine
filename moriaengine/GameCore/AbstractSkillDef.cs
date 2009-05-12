/*
	This program is free software; you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation; either version 2 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program; if not, write to the Free Software
	Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
	Or visit http://www.gnu.org/copyleft/gpl.html
*/

using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using SteamEngine.Common;
using SteamEngine.CompiledScripts;

namespace SteamEngine {
	public abstract class AbstractSkillDef : AbstractDef/*TriggerGroupHolder*/ {

		//string(defname)-Skilldef pairs
		private static Dictionary<string, AbstractSkillDef> byKey = new Dictionary<string, AbstractSkillDef>(StringComparer.OrdinalIgnoreCase);
		//string(key)-Skilldef pairs
		private static List<AbstractSkillDef> byId = new List<AbstractSkillDef>();
		//Skilldef instances by their ID

		private static Dictionary<string, ConstructorInfo> skillDefCtorsByName = new Dictionary<string, ConstructorInfo>(StringComparer.OrdinalIgnoreCase);
		//string-ConstructorInfo pairs  ("CombatSkillDef" - CombatSkillDef.ctor)


		private FieldValue key;
		private int id;
		private FieldValue startByMacroEnabled;

		private TriggerGroup scriptedTriggers;


		public static AbstractSkillDef GetByDefname(string defname) {
			AbstractScript script;
			AllScriptsByDefname.TryGetValue(defname, out script);
			return script as AbstractSkillDef;
		}

		public static AbstractSkillDef GetByKey(string key) {
			AbstractSkillDef retVal;
			byKey.TryGetValue(key, out retVal);
			return retVal;
		}

		public static AbstractSkillDef GetById(int id) {
			if ((id >= 0) || (id < byId.Count)) {
				return byId[id];
			}
			return null;
		}

		public static int SkillsCount {
			get {
				return byId.Count;
			}
		}

		public static void RegisterSkillDef(AbstractSkillDef sd) {
			int id = sd.Id;
			while (byId.Count <= id) {
				byId.Add(null);
			}
			AbstractSkillDef oldSd = byId[id];
			if (oldSd != null) { //or should we throw exception or what...?
				UnregisterSkillDef(oldSd);
			}
			byId[id] = sd;
			AllScriptsByDefname[sd.Defname] = sd;
			byKey[sd.Key] = sd;
		}

		public static void UnregisterSkillDef(AbstractSkillDef sd) {
			byId[sd.Id] = null;
			AllScriptsByDefname.Remove(sd.Defname);
			byKey.Remove(sd.Key);
		}

		internal new static void UnloadScripts() {
			//byDefname.Clear();
			byKey.Clear();
			byId.Clear();
			skillDefCtorsByName.Clear();
		}

		public static new void Bootstrap() {
			ClassManager.RegisterSupplySubclasses<AbstractSkillDef>(RegisterSkillDefType);
		}

		//for loading of skilldefs from .scp/.def scripts
		public static new bool ExistsDefType(string name) {
			return skillDefCtorsByName.ContainsKey(name);
		}

		private static Type[] skillDefConstructorParamTypes = new Type[] { typeof(string), typeof(string), typeof(int) };

		//called by ClassManager
		internal static bool RegisterSkillDefType(Type skillDefType) {
			if (!skillDefType.IsAbstract) {
				ConstructorInfo ci;
				if (skillDefCtorsByName.TryGetValue(skillDefType.Name, out ci)) { //we have already a ThingDef type named like that
					throw new OverrideNotAllowedException("Trying to overwrite class " + LogStr.Ident(ci.DeclaringType) + " in the register of SkillDef classes.");
				}
				ci = skillDefType.GetConstructor(skillDefConstructorParamTypes);
				if (ci == null) {
					throw new SEException("Proper constructor not found.");
				}
				skillDefCtorsByName[skillDefType.Name] = MemberWrapper.GetWrapperFor(ci);
			}
			return false;
		}


		internal static void StartingLoading() {

		}

		internal static IUnloadable LoadFromScripts(PropsSection input) {
			string typeName = input.HeaderType.ToLower(System.Globalization.CultureInfo.InvariantCulture);

			PropsLine prop = input.PopPropsLine("defname");
			if (prop == null) {
				throw new SEException("Missing the defname field for this SkillDef.");
			}

			string defName;
			Match ma = TagMath.stringRE.Match(prop.Value);
			if (ma.Success) {
				defName = String.Intern(ma.Groups["value"].Value);
			} else {
				defName = String.Intern(prop.Value);
			}

			AbstractScript def;
			AllScriptsByDefname.TryGetValue(defName, out def);
			AbstractSkillDef skillDef = def as AbstractSkillDef;

			ConstructorInfo constructor = skillDefCtorsByName[typeName];

			if (skillDef == null) {
				if (def != null) {//it isnt skilldef
					throw new ScriptException("SkillDef " + LogStr.Ident(defName) + " has the same name as " + LogStr.Ident(def));
				} else {
					object[] cargs = new object[] { defName, input.Filename, input.HeaderLine };
					skillDef = (AbstractSkillDef) constructor.Invoke(cargs);
				}
			} else if (skillDef.IsUnloaded) {
				if (skillDef.GetType() != constructor.DeclaringType) {
					throw new OverrideNotAllowedException("You can not change the class of a Skilldef while resync. You have to recompile or restart to achieve that. Ignoring.");
				}
				skillDef.IsUnloaded = false;
				//we have to load the key first, so that it may be unloaded by it...

				PropsLine p = input.PopPropsLine("key");
				skillDef.LoadScriptLine(input.Filename, p.Line, p.Name.ToLower(System.Globalization.CultureInfo.InvariantCulture), p.Value);

				UnregisterSkillDef(skillDef);//will be re-registered again
			} else {
				throw new OverrideNotAllowedException("SkillDef " + LogStr.Ident(defName) + " defined multiple times.");
			}

			ushort skillId;
			if (!TagMath.TryParseUInt16(input.HeaderName, out skillId)) {
				throw new ScriptException("Unrecognized format of the id number in the skilldef script header.");
			}

			skillDef.id = skillId;

			//now do load the trigger code. 
			if (input.TriggerCount > 0) {
				input.HeaderName = "t__" + input.HeaderName + "__";
				skillDef.scriptedTriggers = ScriptedTriggerGroup.Load(input);
			} else {
				skillDef.scriptedTriggers = null;
			}

			skillDef.LoadScriptLines(input);

			RegisterSkillDef(skillDef);

			if (skillDef.scriptedTriggers == null) {
				return skillDef;
			} else {
				return new UnloadableGroup(skillDef, skillDef.scriptedTriggers);
			}
		}

		internal static void LoadingFinished() {

		}

		protected AbstractSkillDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.key = this.InitTypedField("key", "", typeof(string));
			this.startByMacroEnabled = this.InitTypedField("startByMacroEnabled", false, typeof(bool));
		}

		public int Id {
			get {
				return id;
			}
		}

		public string Key {
			get {
				return (string) this.key.CurrentValue;
			}
			set {
				string prev = this.Key;
				key.CurrentValue = value;
				string after = this.Key;
				if (!StringComparer.OrdinalIgnoreCase.Equals(prev, after)) {
					byKey.Remove(prev);
					byKey[after] = this;
				}
			}
		}

		public bool StartByMacroEnabled {
			get {
				return (bool) this.startByMacroEnabled.CurrentValue;
			}
			set {
				this.startByMacroEnabled.CurrentValue = value;
			}
		}

		public TriggerGroup TG {
			get {
				return this.scriptedTriggers;
			}
		}

		public bool TryCancellableTrigger(AbstractCharacter self, TriggerKey td, ScriptArgs sa) {
			//cancellable trigger just for the one triggergroup
			if (this.scriptedTriggers != null) {
				if (TagMath.Is1(this.scriptedTriggers.TryRun(self, td, sa))) {
					return true;
				}
			}
			return false;
		}

		public void TryTrigger(AbstractCharacter self, TriggerKey td, ScriptArgs sa) {
			//cancellable trigger just for the one triggergroup
			if (this.scriptedTriggers != null) {
				this.scriptedTriggers.TryRun(self, td, sa);
			}
		}

		public override string ToString() {
			return Tools.TypeToString(this.GetType()) + " " + Key;
		}
	}

	[Summary("Instances of this class store the skill values of each character")]
	public interface ISkill {
		int RealValue { get; set;}
		int ModifiedValue { get; }
		int Cap { get; set;}
		SkillLockType Lock { get; set;}
		int Id { get;}
	}
}
