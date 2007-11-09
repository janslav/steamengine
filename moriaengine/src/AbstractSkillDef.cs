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
	public abstract class AbstractSkillDef : AbstractDefTriggerGroupHolder {

		//string(defname)-Skilldef pairs
		private static Dictionary<string, AbstractSkillDef> byKey = new Dictionary<string, AbstractSkillDef>(StringComparer.OrdinalIgnoreCase);
		//string(key)-Skilldef pairs
		private static List<AbstractSkillDef> byId = new List<AbstractSkillDef>();
		//Skilldef instances by their ID

		private static Dictionary<string, ConstructorInfo> skillDefCtorsByName = new Dictionary<string, ConstructorInfo>(StringComparer.OrdinalIgnoreCase);
		//string-ConstructorInfo pairs  ("CombatSkillDef" - CombatSkillDef.ctor)
		
		public static AbstractSkillDef ByDefname(string defname) {
			AbstractScript script;
			byDefname.TryGetValue(defname, out script);
			return script as AbstractSkillDef;
		}
		
		public static AbstractSkillDef ByKey(string key) {
			AbstractSkillDef retVal;
			byKey.TryGetValue(key, out retVal);
			return retVal;
		}
		
		public static AbstractSkillDef ById(int id) {
			if ((id >= 0) || (id < byId.Count)) {
				return byId[id];
			}
			return null;
		}

		public static int SkillsCount { get {
			return byId.Count;
		} }
		
		public static void RegisterSkillDef(AbstractSkillDef sd) {
			ushort id = sd.Id;
			while (byId.Count <= id) {
				byId.Add(null);
			}
			AbstractSkillDef oldSd = byId[id];
			if (oldSd != null) { //or should we throw exception or what...?
				UnRegisterSkillDef(oldSd);
			}
			byId[id] = sd;
			byDefname[sd.Defname] = sd;
			byKey[sd.Key] = sd;
		}
		
		public static void UnRegisterSkillDef(AbstractSkillDef sd) {
			byId[sd.Id] = null;
			byDefname.Remove(sd.Defname);
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
		
		private static Type[] skillDefConstructorParamTypes = new Type[] {typeof(string), typeof(string), typeof(int)};
		
		//this should be typically called by the Bootstrap methods of scripted SkillDefs
		internal static bool RegisterSkillDefType(Type skillDefType) {
			ConstructorInfo ci;
			if (skillDefCtorsByName.TryGetValue(skillDefType.Name, out ci)) { //we have already a ThingDef type named like that
				throw new OverrideNotAllowedException("Trying to overwrite class "+LogStr.Ident(ci.DeclaringType)+" in the register of SkillDef classes.");
			}
			ci = skillDefType.GetConstructor(skillDefConstructorParamTypes);
			if (ci == null) {
				throw new Exception("Proper constructor not found.");
			}
			skillDefCtorsByName[skillDefType.Name] = MemberWrapper.GetWrapperFor(ci);
			return false;
		}

		
		internal static void StartingLoading() {
			
		}
		
		internal static AbstractSkillDef LoadFromScripts(PropsSection input) {
			string typeName = input.headerType.ToLower();
			
			PropsLine prop = input.PopPropsLine("defname");
			if (prop==null) {
				throw new Exception("Missing the defname field for this SkillDef.");
			}
			
			string defName;
			Match ma = TagMath.stringRE.Match(prop.value);
			if (ma.Success) {
				defName = String.Intern(ma.Groups["value"].Value);
			} else {
				defName = String.Intern(prop.value);
			}
			
			AbstractScript def;
			byDefname.TryGetValue(defName, out def);
			AbstractSkillDef skillDef = def as AbstractSkillDef;

			ConstructorInfo constructor = skillDefCtorsByName[typeName];
			
			if (skillDef == null) {
				if (def != null) {//it isnt skilldef
					throw new ScriptException("SkillDef "+LogStr.Ident(defName)+" has the same name as "+LogStr.Ident(def));	
				} else {
					object[] cargs = new object[] {defName, input.filename, input.headerLine};
					skillDef = (AbstractSkillDef) constructor.Invoke(cargs);
				}
			} else if (skillDef.unloaded) {
				if (skillDef.GetType() != constructor.DeclaringType) {
					throw new OverrideNotAllowedException("You can not change the class of a Skilldef while resync. You have to recompile or restart to achieve that. Ignoring.");
				}
				skillDef.unloaded = false;
				//we have to load the key first, so that it may be unloaded by it...

				PropsLine p = input.PopPropsLine("key");
				skillDef.LoadScriptLine(input.filename, p.line, p.name.ToLower(), p.value);

				UnRegisterSkillDef(skillDef);//will be re-registered again
			} else {
				throw new OverrideNotAllowedException("SkillDef "+LogStr.Ident(defName)+" defined multiple times.");
			}
			
			if (!TagMath.TryParseUInt16(input.headerName, out skillDef.id)) {
				throw new ScriptException("Unrecognized format of the id number in the skilldef script header.");
			}
		
			//now do load the trigger code. 
			if (input.TriggerCount>0) {
				input.headerName = "t__"+input.headerName+"__";
				TriggerGroup tg = ScriptedTriggerGroup.Load(input);
				skillDef.AddTriggerGroup(tg);
			}
			
			skillDef.LoadScriptLines(input);
			
			RegisterSkillDef(skillDef);
			
			return skillDef;
		}
		
		internal static void LoadingFinished() {
			
		}
		
		private FieldValue key;
		private ushort id;
		
		public AbstractSkillDef(string defname, string filename, int headerLine) : base(defname, filename, headerLine) {
			key = InitField_Typed("key", "", typeof(string));
		}
		
		public string Key  {
			get {
				return (string) key.CurrentValue;
			} 
			set {
				string prev = Key;
				key.CurrentValue = value;
				string after = Key;
				if (string.Compare(prev, after, true)!=0) {
					byKey.Remove(prev);
					byKey[after] = this;
				}
			}
		}
		
		public ushort Id  {
			get {
				return id;
			}
			set {
				id = value;
			}
		}
		
		public TriggerGroup TG  {
			get {
				if (firstTGListNode != null) {
					return firstTGListNode.storedTG;
				}
				return null;
			} 
		}

		public bool TryCancellableSkillTrigger(AbstractCharacter self, TriggerKey td, ScriptArgs sa) {
			//cancellable trigger just for the one triggergroup
			if (firstTGListNode != null) {
				object retVal = firstTGListNode.storedTG.TryRun(self, td, sa);
				try {
					int retInt = Convert.ToInt32(retVal);
					if (retInt == 1) {
						return true;
					}
				} catch (Exception) {
				}
			}
			return false;
		}

		public void TrySkillTrigger(AbstractCharacter self, TriggerKey td, ScriptArgs sa) {
			//cancellable trigger just for the one triggergroup
			if (firstTGListNode != null) {
				firstTGListNode.storedTG.TryRun(self, td, sa);
			}
		}

		public override bool TryCancellableTrigger(TriggerKey td, ScriptArgs sa) {
			throw new NotImplementedException();
		}

		public override bool CancellableTrigger(TriggerKey td, ScriptArgs sa) {
			throw new NotImplementedException();
		}

		public override void Trigger(TriggerKey td, ScriptArgs sa) {
			throw new NotImplementedException();
		}

		public override void TryTrigger(TriggerKey td, ScriptArgs sa) {
			throw new NotImplementedException();
		}

		protected override void LoadScriptLine(string filename, int line, string param, string args) {
			base.LoadScriptLine(filename, line, param, args);
		}
		
		public abstract void Select(AbstractCharacter ch);
		
		public override string ToString() {
			return GetType().Name+" "+Key;
		}
	}
	
	[Summary("Instances of this class store the skill values of each character")]
	public interface ISkill {
		ushort RealValue {get; set;}
		ushort Cap {get; set;}
		SkillLockType Lock {get; set;}
		ushort Id {get;}
	}
}
