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

namespace SteamEngine.CompiledScripts {
	public class AbilityDef : AbstractDef {
        public static readonly TriggerKey tkAssign = TriggerKey.Get("Assign");
        public static readonly TriggerKey tkUnAssign = TriggerKey.Get("UnAssign");

		private static Dictionary<string, AbilityDef> byName = new Dictionary<string, AbilityDef>(StringComparer.OrdinalIgnoreCase);
		
		private static Dictionary<string, ConstructorInfo> abilityDefCtorsByName = new Dictionary<string, ConstructorInfo>(StringComparer.OrdinalIgnoreCase);
		//string-ConstructorInfo pairs  ("AbilityDef" - AbilityDef.ctor)

		private TriggerGroup scriptedTriggers;

		[Summary("Overall method for running the abilites. Its basic implementation does not allow to run the "+
				"ability unless properly overriden in a child that is made to be run manually")]
		public virtual void Activate(Ability ab) {
			ab.Cont.RedMessage("Abilitu " + Name + " nelze spustit");
		}

        [Summary("This method implements the assigning of the first point to the Ability")]
        protected virtual void On_Assign(Character ch) {
        }

        internal void Trigger_Assign(Character chr, Ability ab) {
			if(chr != null) {
				ScriptArgs sa = new ScriptArgs(ab);
				//call the trigger @assign with argument "ability" (containing also info about its holder)
				TryTrigger(chr, AbilityDef.tkAssign, sa);
				chr.On_AbilityAssign(ab);
				On_Assign(chr);
			}
        }

        [Summary("This method implements the unassigning of the last point from the Ability")]
		protected void On_UnAssign(Character ch) {
        }

		internal void Trigger_UnAssign(Character chr, Ability ab) {
			if(chr != null) {
				ScriptArgs sa = new ScriptArgs(ab);
				//call the trigger @unassign with argument "ability" (containing also info about its holder)
				TryTrigger(chr, AbilityDef.tkUnAssign, sa);
				chr.On_AbilityUnAssign(ab);
				On_UnAssign(chr);
			}
        }		

		public static AbilityDef ByDefname(string defname) {
			AbstractScript script;
			byDefname.TryGetValue(defname, out script);
			return script as AbilityDef;
		}

		public static AbilityDef ByName(string key) {
			AbilityDef retVal;
			byName.TryGetValue(key, out retVal);
			return retVal;
		}

		public static int AbilitiesCount {
			get {
				return byName.Count;
			}
		}

		public static void RegisterAbilityDef(AbilityDef ad) {
			byDefname[ad.Defname] = ad;
			byName[ad.Name] = ad;
		}

		public static void UnRegisterAbilityDef(AbilityDef ad) {
			byDefname.Remove(ad.Defname);
			byName.Remove(ad.Name);
		}

		internal new static void UnloadScripts() {
			//byDefname.Clear();
			byName.Clear();
			abilityDefCtorsByName.Clear();
		}

		public static new void Bootstrap() {
			ClassManager.RegisterSupplySubclasses<AbilityDef>(RegisterAbilityDefType);
		}

		//for loading of abilitydefs from .scp scripts
		public static new bool ExistsDefType(string name) {
			return abilityDefCtorsByName.ContainsKey(name);
		}

		private static Type[] abilityDefConstructorParamTypes = new Type[] { typeof(string), typeof(string), typeof(int) };

		//called by ClassManager
		internal static bool RegisterAbilityDefType(Type abilityDefType) {
			ConstructorInfo ci;
			if(abilityDefCtorsByName.TryGetValue(abilityDefType.Name, out ci)) { //we have already a AbilityDef type named like that
				throw new OverrideNotAllowedException("Trying to overwrite class " + LogStr.Ident(ci.DeclaringType) + " in the register of AbilityDef classes.");
			}
			ci = abilityDefType.GetConstructor(abilityDefConstructorParamTypes);
			if(ci == null) {
				throw new Exception("Proper constructor not found.");
			}
			abilityDefCtorsByName[abilityDefType.Name] = MemberWrapper.GetWrapperFor(ci);

            ScriptLoader.RegisterScriptType(abilityDefType.Name, LoadFromScripts, false);

			return false;
		}


		internal static void StartingLoading() {

		}

		internal static AbilityDef LoadFromScripts(PropsSection input) {
			string typeName = input.headerType.ToLower();

			PropsLine prop = input.PopPropsLine("defname");
			if(prop == null) {
				throw new Exception("Missing the defname field for this AbilityDef.");
			}

			string defName;
			Match ma = TagMath.stringRE.Match(prop.value);
			if(ma.Success) {
				defName = String.Intern(ma.Groups["value"].Value);
			} else {
				defName = String.Intern(prop.value);
			}

			AbstractScript def;
			byDefname.TryGetValue(defName, out def);
			AbilityDef abilityDef = def as AbilityDef;

			ConstructorInfo constructor = abilityDefCtorsByName[typeName];

			if(abilityDef == null) {
				if(def != null) {//it isnt abilityDef
					throw new ScriptException("AbilityDef " + LogStr.Ident(defName) + " has the same name as " + LogStr.Ident(def));
				} else {
					object[] cargs = new object[] { defName, input.filename, input.headerLine };
					abilityDef = (AbilityDef)constructor.Invoke(cargs);
				}
            } else if (abilityDef.unloaded) {
                if (abilityDef.GetType() != constructor.DeclaringType) {
					throw new OverrideNotAllowedException("You can not change the class of a AbilityDef while resync. You have to recompile or restart to achieve that. Ignoring.");
				}
                abilityDef.unloaded = false;
				//we have to load the name first, so that it may be unloaded by it...

				PropsLine p = input.PopPropsLine("name");
				abilityDef.LoadScriptLine(input.filename, p.line, p.name.ToLower(), p.value);

                UnRegisterAbilityDef(abilityDef);//will be re-registered again
			} else {
				throw new OverrideNotAllowedException("AbilityDef " + LogStr.Ident(defName) + " defined multiple times.");
			}

            //if (!TagMath.TryParseUInt16(input.headerName, out abilityDef.id)) {
		    //    throw new ScriptException("Unrecognized format of the id number in the abilitydef script header.");
			//}

			//now do load the trigger code. 
			if(input.TriggerCount > 0) {
				//input.headerName = "t__" + input.headerName + "__";
				abilityDef.scriptedTriggers = ScriptedTriggerGroup.Load(input);
			} else {
                abilityDef.scriptedTriggers = null;
			}

            abilityDef.LoadScriptLines(input);

            RegisterAbilityDef(abilityDef);

            return abilityDef;
		}

		internal static void LoadingFinished() {

		}

		private FieldValue name;
		private FieldValue maxPoints;
		
		public AbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
            name = InitField_Typed("name", "", typeof(string));
			maxPoints = InitField_Typed("maxPoints", 0, typeof(ushort));			
		}

        public string Name {
			get {
				return (string)name.CurrentValue;
			}			
		}

		public ushort MaxPoints {
			get {
				return (ushort)maxPoints.CurrentValue;
			}			
		}	

		public bool TryCancellableTrigger(AbstractCharacter self, TriggerKey td, ScriptArgs sa) {
			//cancellable trigger just for the one triggergroup
			if(this.scriptedTriggers != null) {
				object retVal = this.scriptedTriggers.TryRun(self, td, sa);
				try {
					int retInt = Convert.ToInt32(retVal);
					if(retInt == 1) {
						return true;
					}
				} catch(Exception) {
				}
			}
			return false;
		}

		public void TryTrigger(AbstractCharacter self, TriggerKey td, ScriptArgs sa) {
		    if(this.scriptedTriggers != null) {
				this.scriptedTriggers.TryRun(self, td, sa);
			}
		}
		
		protected override void LoadScriptLine(string filename, int line, string param, string args) {
			base.LoadScriptLine(filename, line, param, args);
		}

		public override string ToString() {
			return GetType().Name + " " + Name;
		}
	}
}