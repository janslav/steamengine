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
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {
	[ViewableClass]
	public class ProfessionDef : AbstractDef {
		internal static readonly TriggerKey tkAssign = TriggerKey.Get("Assign");        

		private static Dictionary<string, ProfessionDef> byName = new Dictionary<string, ProfessionDef>(StringComparer.OrdinalIgnoreCase);

		private static Dictionary<string, ConstructorInfo> profDefCtorsByName = new Dictionary<string, ConstructorInfo>(StringComparer.OrdinalIgnoreCase);

		//triggery class-specific
        private TriggerGroup scriptedTriggers; //specified in the LScript code of the profession
		
		[Summary("Method for assigning the selected profession to specified player")]
		public void AssignTo(Player plr) {
			Profession prof = new Profession(this, plr);
			plr.Profession = prof;
			plr.AddTriggerGroup(scriptedTriggers); //LScript trigs. (if any)
			plr.AddTriggerGroup(CompiledTriggers); //compiled trigs. (if any)
			Trigger_Assign(prof, plr);
		}

		#region triggerMethods
		protected virtual void On_Assign(Player plr) {
			//implement if needed...
		}

		protected void Trigger_Assign(Profession prof, Player plr) {
			TryTrigger(plr, ProfessionDef.tkAssign, new ScriptArgs(prof));
			plr.On_ProfessionAssign(this);
			On_Assign(plr);
		}
		#endregion triggerMethods

		public static ProfessionDef ByDefname(string defname) {
			AbstractScript script;
			byDefname.TryGetValue(defname, out script);
			return script as ProfessionDef;
		}

		public static ProfessionDef ByName(string key) {
			ProfessionDef retVal;
			byName.TryGetValue(key, out retVal);
			return retVal;
		}

		public static void RegisterProfessionDef(ProfessionDef pd) {
			byDefname[pd.Defname] = pd;
			byName[pd.Name] = pd;
		}

		public static void UnRegisterProfessionDef(ProfessionDef pd) {
			byDefname.Remove(pd.Defname);
			byName.Remove(pd.Name);
		}

		internal static void UnloadScripts() {
			byName.Clear();
			profDefCtorsByName.Clear();
		}

		public static new void Bootstrap() {
			ClassManager.RegisterSupplySubclasses<ProfessionDef>(RegisterProfessionDefType);
		}

		//for loading of ProfessionDefs from .scp scripts
		public static new bool ExistsDefType(string name) {
			return profDefCtorsByName.ContainsKey(name);
		}

		private static Type[] profDefConstructorParamTypes = new Type[] { typeof(string), typeof(string), typeof(int) };

		//called by ClassManager
		internal static bool RegisterProfessionDefType(Type profDefType) {
			ConstructorInfo ci;
			if (profDefCtorsByName.TryGetValue(profDefType.Name, out ci)) { //we have already a ProfessionDef type named like that
				throw new OverrideNotAllowedException("Trying to overwrite class " + LogStr.Ident(ci.DeclaringType) + " in the register of ProfessionDef classes.");
			}
			ci = profDefType.GetConstructor(profDefConstructorParamTypes);
			if (ci == null) {
				throw new Exception("Proper constructor not found.");
			}
			profDefCtorsByName[profDefType.Name] = MemberWrapper.GetWrapperFor(ci);

			ScriptLoader.RegisterScriptType(profDefType.Name, LoadFromScripts, false);

			return false;
		}


		internal static void StartingLoading() {
		}

		internal static ProfessionDef LoadFromScripts(PropsSection input) {
			//it is something like this in the .scp file: [headerType headerName] = [ProfessionDef class_necro] etc.
			string typeName = input.headerType.ToLower();
			string profDefName = input.headerName.ToLower();

			AbstractScript def;
			byDefname.TryGetValue(profDefName, out def);
			ProfessionDef profDef = def as ProfessionDef;

			ConstructorInfo constructor = profDefCtorsByName[typeName];

			if (profDef == null) {
				if (def != null) {//it isnt profDef
					throw new ScriptException("ProfessionDef " + LogStr.Ident(profDefName) + " has the same name as " + LogStr.Ident(def));
				} else {
					object[] cargs = new object[] { profDefName, input.filename, input.headerLine };
					profDef = (ProfessionDef) constructor.Invoke(cargs);
				}
			} else if (profDef.unloaded) {
				if (profDef.GetType() != constructor.DeclaringType) {
					throw new OverrideNotAllowedException("You can not change the class of a ProfessionDef while resync. You have to recompile or restart to achieve that. Ignoring.");
				}
				profDef.unloaded = false;
				//we have to load the name first, so that it may be unloaded by it...

				PropsLine p = input.PopPropsLine("name");
				profDef.LoadScriptLine(input.filename, p.line, p.name.ToLower(), p.value);

				UnRegisterProfessionDef(profDef);//will be re-registered again
			} else {
				throw new OverrideNotAllowedException("ProfessionDef " + LogStr.Ident(profDefName) + " defined multiple times.");
			}

			//now do load the trigger code.
			if (input.TriggerCount > 0) {
				input.headerName = "t__" + input.headerName + "__"; //naming of the trigger group for @login, logout etc. triggers
				profDef.scriptedTriggers = ScriptedTriggerGroup.Load(input);
			} else {
				profDef.scriptedTriggers = null;
			}

			profDef.LoadScriptLines(input);

			RegisterProfessionDef(profDef);

			return profDef;
		}

		internal static void LoadingFinished() {
		}

		//this comes from sphere tables - defined and used here as well
		//CPROFESSIONPROP(Name,		CSCRIPTPROP_ARG1S, "Profession Name")
		//CPROFESSIONPROP(SkillSum,	0, "Max Sum of skills allowed")
		//CPROFESSIONPROP(StatSum,	0, "Max Sum of stats allowed")		
        private FieldValue name; //logical name of the profession (such as "Necro")
        private FieldValue skillSum; //Max Sum of skills allowed
        private FieldValue statSum; //Max Sum of stats allowed

        #region Maximum skill values
        private FieldValue maxAlchemy;
		private FieldValue maxAnatomy;
		private FieldValue maxAnimalLore;
		private FieldValue maxItemID;
		private FieldValue maxArmsLore;
		private FieldValue maxParry;
		private FieldValue maxBegging;
		private FieldValue maxBlacksmith;
		private FieldValue maxFletching;
		private FieldValue maxPeacemaking;
		private FieldValue maxCamping;
		private FieldValue maxCarpentry;
		private FieldValue maxCartography;
		private FieldValue maxCooking;
		private FieldValue maxDetectHidden;
		private FieldValue maxDiscordance;
		private FieldValue maxEvalInt;
		private FieldValue maxHealing;
		private FieldValue maxFishing;
		private FieldValue maxForensics;
		private FieldValue maxHerding;
		private FieldValue maxHiding;
		private FieldValue maxProvocation;
		private FieldValue maxInscribe;
		private FieldValue maxLockpicking;
		private FieldValue maxMagery;
		private FieldValue maxMagicResist;
		private FieldValue maxTactics;
		private FieldValue maxSnooping;
		private FieldValue maxMusicianship;
		private FieldValue maxPoisoning;
		private FieldValue maxArchery;
		private FieldValue maxSpiritSpeak;
		private FieldValue maxStealing;
		private FieldValue maxTailoring;
		private FieldValue maxAnimalTaming;
		private FieldValue maxTasteID;
		private FieldValue maxTinkering;
		private FieldValue maxTracking;
		private FieldValue maxVeterinary;
		private FieldValue maxSwords;
		private FieldValue maxMacing;
		private FieldValue maxFencing;
		private FieldValue maxWrestling;
		private FieldValue maxLumberjacking;
		private FieldValue maxMining;
		private FieldValue maxMeditation;
		private FieldValue maxStealth;
		private FieldValue maxRemoveTrap;
		private FieldValue maxNecromancy;
		private FieldValue maxMarksmanship;
		private FieldValue maxChivalry;
		private FieldValue maxBushido;
        private FieldValue maxNinjitsu;
        #endregion

		#region Basic skill values
		private FieldValue basicAlchemy;
		private FieldValue basicAnatomy;
		private FieldValue basicAnimalLore;
		private FieldValue basicItemID;
		private FieldValue basicArmsLore;
		private FieldValue basicParry;
		private FieldValue basicBegging;
		private FieldValue basicBlacksmith;
		private FieldValue basicFletching;
		private FieldValue basicPeacemaking;
		private FieldValue basicCamping;
		private FieldValue basicCarpentry;
		private FieldValue basicCartography;
		private FieldValue basicCooking;
		private FieldValue basicDetectHidden;
		private FieldValue basicDiscordance;
		private FieldValue basicEvalInt;
		private FieldValue basicHealing;
		private FieldValue basicFishing;
		private FieldValue basicForensics;
		private FieldValue basicHerding;
		private FieldValue basicHiding;
		private FieldValue basicProvocation;
		private FieldValue basicInscribe;
		private FieldValue basicLockpicking;
		private FieldValue basicMagery;
		private FieldValue basicMagicResist;
		private FieldValue basicTactics;
		private FieldValue basicSnooping;
		private FieldValue basicMusicianship;
		private FieldValue basicPoisoning;
		private FieldValue basicArchery;
		private FieldValue basicSpiritSpeak;
		private FieldValue basicStealing;
		private FieldValue basicTailoring;
		private FieldValue basicAnimalTaming;
		private FieldValue basicTasteID;
		private FieldValue basicTinkering;
		private FieldValue basicTracking;
		private FieldValue basicVeterinary;
		private FieldValue basicSwords;
		private FieldValue basicMacing;
		private FieldValue basicFencing;
		private FieldValue basicWrestling;
		private FieldValue basicLumberjacking;
		private FieldValue basicMining;
		private FieldValue basicMeditation;
		private FieldValue basicStealth;
		private FieldValue basicRemoveTrap;
		private FieldValue basicNecromancy;
		private FieldValue basicMarksmanship;
		private FieldValue basicChivalry;
		private FieldValue basicBushido;
		private FieldValue basicNinjitsu;
		#endregion
		private FieldValue[] maxSkills;
		private FieldValue[] basicSkills;
        public ProfessionDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			name = InitField_Typed("name", "", typeof(string));
            skillSum = InitField_Typed("skillSum", 0, typeof(int));
            statSum = InitField_Typed("statSum", 0, typeof(int));
			//max skills
            maxAlchemy = InitField_Typed("maxAlchemy", 1000, typeof(ushort));
			maxAnatomy = InitField_Typed("maxAnatomy", 1000, typeof(ushort));
			maxAnimalLore = InitField_Typed("maxAnimalLore", 1000, typeof(ushort));
			maxItemID = InitField_Typed("maxItemID", 1000, typeof(ushort));
			maxArmsLore = InitField_Typed("maxArmsLore", 1000, typeof(ushort));
			maxParry = InitField_Typed("maxParrying", 1000, typeof(ushort));
			maxBegging = InitField_Typed("maxBegging", 1000, typeof(ushort));
			maxBlacksmith = InitField_Typed("maxBlacksmithing", 1000, typeof(ushort));
			maxFletching = InitField_Typed("maxBowcraft", 1000, typeof(ushort));
			maxPeacemaking = InitField_Typed("maxPeacemaking", 1000, typeof(ushort));
			maxCamping = InitField_Typed("maxCamping", 1000, typeof(ushort));
			maxCarpentry = InitField_Typed("maxCarpentry", 1000, typeof(ushort));
			maxCartography = InitField_Typed("maxCartography", 1000, typeof(ushort));
			maxCooking = InitField_Typed("maxCooking", 1000, typeof(ushort));
			maxDetectHidden = InitField_Typed("maxDetectingHidden", 1000, typeof(ushort));
			maxDiscordance = InitField_Typed("maxDiscordance", 1000, typeof(ushort));
			maxEvalInt = InitField_Typed("maxEI", 1000, typeof(ushort));
			maxHealing = InitField_Typed("maxHealing", 1000, typeof(ushort));
			maxFishing = InitField_Typed("maxFishing", 1000, typeof(ushort));
			maxForensics = InitField_Typed("maxForensics", 1000, typeof(ushort));
			maxHerding = InitField_Typed("maxHerding", 1000, typeof(ushort));
			maxHiding = InitField_Typed("maxHiding", 1000, typeof(ushort));
			maxProvocation = InitField_Typed("maxProvocation", 1000, typeof(ushort));
			maxInscribe = InitField_Typed("maxInscription", 1000, typeof(ushort));
			maxLockpicking = InitField_Typed("maxLockpicking", 1000, typeof(ushort));
			maxMagery = InitField_Typed("maxMagery", 1000, typeof(ushort));
			maxMagicResist = InitField_Typed("maxResist", 1000, typeof(ushort));
			maxTactics = InitField_Typed("maxTactics", 1000, typeof(ushort));
			maxSnooping = InitField_Typed("maxSnooping", 1000, typeof(ushort));
			maxMusicianship = InitField_Typed("maxMusicianship", 1000, typeof(ushort));
			maxPoisoning = InitField_Typed("maxPoisoning", 1000, typeof(ushort));
			maxArchery = InitField_Typed("maxArchery", 1000, typeof(ushort));
			maxSpiritSpeak = InitField_Typed("maxSpiritSpeak", 1000, typeof(ushort));
			maxStealing = InitField_Typed("maxStealing", 1000, typeof(ushort));
			maxTailoring = InitField_Typed("maxTailoring", 1000, typeof(ushort));
			maxAnimalTaming = InitField_Typed("maxTaming", 1000, typeof(ushort));
			maxTasteID = InitField_Typed("maxTasteID", 1000, typeof(ushort));
			maxTinkering = InitField_Typed("maxTinkering", 1000, typeof(ushort));
			maxTracking = InitField_Typed("maxTracking", 1000, typeof(ushort));
			maxVeterinary = InitField_Typed("maxVeterinary", 1000, typeof(ushort));
			maxSwords = InitField_Typed("maxSwordsmanship", 1000, typeof(ushort));
			maxMacing = InitField_Typed("maxMacefighting", 1000, typeof(ushort));
			maxFencing = InitField_Typed("maxFencing", 1000, typeof(ushort));
			maxWrestling = InitField_Typed("maxWrestling", 1000, typeof(ushort));
			maxLumberjacking = InitField_Typed("maxLumberjacking", 1000, typeof(ushort));
			maxMining = InitField_Typed("maxMining", 1000, typeof(ushort));
			maxMeditation = InitField_Typed("maxMeditation", 1000, typeof(ushort));
			maxStealth = InitField_Typed("maxStealth", 1000, typeof(ushort));
			maxRemoveTrap = InitField_Typed("maxRemoveTrap", 1000, typeof(ushort));
			maxNecromancy = InitField_Typed("maxNecromancy", 1000, typeof(ushort));
			maxMarksmanship = InitField_Typed("maxMarksmanship", 1000, typeof(ushort));
			maxChivalry = InitField_Typed("maxChivalry", 1000, typeof(ushort));
			maxBushido = InitField_Typed("maxBushido", 1000, typeof(ushort));
			maxNinjitsu = InitField_Typed("maxNinjutsu", 1000, typeof(ushort));
			//basic skills
			basicAlchemy = InitField_Typed("basicAlchemy", 1000, typeof(ushort));
			basicAnatomy = InitField_Typed("basicAnatomy", 1000, typeof(ushort));
			basicAnimalLore = InitField_Typed("basicAnimalLore", 1000, typeof(ushort));
			basicItemID = InitField_Typed("basicItemID", 1000, typeof(ushort));
			basicArmsLore = InitField_Typed("basicArmsLore", 1000, typeof(ushort));
			basicParry = InitField_Typed("basicParrying", 1000, typeof(ushort));
			basicBegging = InitField_Typed("basicBegging", 1000, typeof(ushort));
			basicBlacksmith = InitField_Typed("basicBlacksmithing", 1000, typeof(ushort));
			basicFletching = InitField_Typed("basicBowcraft", 1000, typeof(ushort));
			basicPeacemaking = InitField_Typed("basicPeacemaking", 1000, typeof(ushort));
			basicCamping = InitField_Typed("basicCamping", 1000, typeof(ushort));
			basicCarpentry = InitField_Typed("basicCarpentry", 1000, typeof(ushort));
			basicCartography = InitField_Typed("basicCartography", 1000, typeof(ushort));
			basicCooking = InitField_Typed("basicCooking", 1000, typeof(ushort));
			basicDetectHidden = InitField_Typed("basicDetectingHidden", 1000, typeof(ushort));
			basicDiscordance = InitField_Typed("basicDiscordance", 1000, typeof(ushort));
			basicEvalInt = InitField_Typed("basicEI", 1000, typeof(ushort));
			basicHealing = InitField_Typed("basicHealing", 1000, typeof(ushort));
			basicFishing = InitField_Typed("basicFishing", 1000, typeof(ushort));
			basicForensics = InitField_Typed("basicForensics", 1000, typeof(ushort));
			basicHerding = InitField_Typed("basicHerding", 1000, typeof(ushort));
			basicHiding = InitField_Typed("basicHiding", 1000, typeof(ushort));
			basicProvocation = InitField_Typed("basicProvocation", 1000, typeof(ushort));
			basicInscribe = InitField_Typed("basicInscription", 1000, typeof(ushort));
			basicLockpicking = InitField_Typed("basicLockpicking", 1000, typeof(ushort));
			basicMagery = InitField_Typed("basicMagery", 1000, typeof(ushort));
			basicMagicResist = InitField_Typed("basicResist", 1000, typeof(ushort));
			basicTactics = InitField_Typed("basicTactics", 1000, typeof(ushort));
			basicSnooping = InitField_Typed("basicSnooping", 1000, typeof(ushort));
			basicMusicianship = InitField_Typed("basicMusicianship", 1000, typeof(ushort));
			basicPoisoning = InitField_Typed("basicPoisoning", 1000, typeof(ushort));
			basicArchery = InitField_Typed("basicArchery", 1000, typeof(ushort));
			basicSpiritSpeak = InitField_Typed("basicSpiritSpeak", 1000, typeof(ushort));
			basicStealing = InitField_Typed("basicStealing", 1000, typeof(ushort));
			basicTailoring = InitField_Typed("basicTailoring", 1000, typeof(ushort));
			basicAnimalTaming = InitField_Typed("basicTaming", 1000, typeof(ushort));
			basicTasteID = InitField_Typed("basicTasteID", 1000, typeof(ushort));
			basicTinkering = InitField_Typed("basicTinkering", 1000, typeof(ushort));
			basicTracking = InitField_Typed("basicTracking", 1000, typeof(ushort));
			basicVeterinary = InitField_Typed("basicVeterinary", 1000, typeof(ushort));
			basicSwords = InitField_Typed("basicSwordsmanship", 1000, typeof(ushort));
			basicMacing = InitField_Typed("basicMacefighting", 1000, typeof(ushort));
			basicFencing = InitField_Typed("basicFencing", 1000, typeof(ushort));
			basicWrestling = InitField_Typed("basicWrestling", 1000, typeof(ushort));
			basicLumberjacking = InitField_Typed("basicLumberjacking", 1000, typeof(ushort));
			basicMining = InitField_Typed("basicMining", 1000, typeof(ushort));
			basicMeditation = InitField_Typed("basicMeditation", 1000, typeof(ushort));
			basicStealth = InitField_Typed("basicStealth", 1000, typeof(ushort));
			basicRemoveTrap = InitField_Typed("basicRemoveTrap", 1000, typeof(ushort));
			basicNecromancy = InitField_Typed("basicNecromancy", 1000, typeof(ushort));
			basicMarksmanship = InitField_Typed("basicMarksmanship", 1000, typeof(ushort));
			basicChivalry = InitField_Typed("basicChivalry", 1000, typeof(ushort));
			basicBushido = InitField_Typed("basicBushido", 1000, typeof(ushort));
			basicNinjitsu = InitField_Typed("basicNinjutsu", 1000, typeof(ushort));

            //now prepare the array with skills indexed by numbers in SkillName enumeration
            maxSkills = new FieldValue[] {maxAlchemy,maxAnatomy,maxAnimalLore,maxItemID,maxArmsLore,
                            maxParry,maxBegging,maxBlacksmith,maxFletching,maxPeacemaking,maxCamping,
                            maxCarpentry,maxCartography,maxCooking,maxDetectHidden,maxDiscordance,
                            maxEvalInt,maxHealing,maxFishing,maxForensics,maxHerding,maxHiding,maxProvocation,
                            maxInscribe,maxLockpicking,maxMagery,maxMagicResist,maxTactics,maxSnooping,
                            maxMusicianship,maxPoisoning,maxArchery,maxSpiritSpeak,maxStealing,
                            maxTailoring,maxAnimalTaming,maxTasteID,maxTinkering,maxTracking,
                            maxVeterinary,maxSwords,maxMacing,maxFencing,maxWrestling,maxLumberjacking,
                            maxMining,maxMeditation,maxStealth,maxRemoveTrap,maxNecromancy,
                            maxMarksmanship,maxChivalry,maxBushido,maxNinjitsu};

			//now prepare the array with basic skills indexed by numbers in SkillName enumeration
			basicSkills = new FieldValue[] {basicAlchemy,basicAnatomy,basicAnimalLore,basicItemID,basicArmsLore,
                            basicParry,basicBegging,basicBlacksmith,basicFletching,basicPeacemaking,basicCamping,
                            basicCarpentry,basicCartography,basicCooking,basicDetectHidden,basicDiscordance,
                            basicEvalInt,basicHealing,basicFishing,basicForensics,basicHerding,basicHiding,basicProvocation,
                            basicInscribe,basicLockpicking,basicMagery,basicMagicResist,basicTactics,basicSnooping,
                            basicMusicianship,basicPoisoning,basicArchery,basicSpiritSpeak,basicStealing,
                            basicTailoring,basicAnimalTaming,basicTasteID,basicTinkering,basicTracking,
                            basicVeterinary,basicSwords,basicMacing,basicFencing,basicWrestling,basicLumberjacking,
                            basicMining,basicMeditation,basicStealth,basicRemoveTrap,basicNecromancy,
                            basicMarksmanship,basicChivalry,basicBushido,basicNinjitsu};
		}

		public string Name {
			get {
				return (string) name.CurrentValue;
			}
		}

		public bool TryCancellableTrigger(AbstractCharacter self, TriggerKey td, ScriptArgs sa) {
			//cancellable trigger just for the one triggergroup
			if (this.scriptedTriggers != null) {
				object retVal = this.scriptedTriggers.TryRun(self, td, sa);
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

		public void TryTrigger(AbstractCharacter self, TriggerKey td, ScriptArgs sa) {
			if (this.scriptedTriggers != null) {
				this.scriptedTriggers.TryRun(self, td, sa);
			}
		}

		protected override void LoadScriptLine(string filename, int line, string param, string args) {
			base.LoadScriptLine(filename, line, param, args);
		}

		public override string ToString() {
			return GetType().Name + " " + Name;
		}

		[Summary("Compiled trigger group specific for the given ProfessionDef")]
		public virtual E_Profession CompiledTriggers {
			get {
				//return the specific instance of triggergroup (can differ for various professions)
				return SingletonScript<E_Profession>.Instance;
			}
		}

		#region utilities
		[Summary("Return the maximal value of the given skill (by name) for this profession")]
		public ushort MaxSkill(SkillName skillName) {
			return MaxSkill((ushort) skillName);
		}
		[Summary("Return the maximal value of the given skill (by id) for this profession")]
		public ushort MaxSkill(ushort skillId) {
			return (ushort) maxSkills[skillId].CurrentValue;
		}

		[Summary("Return the basic value of the given skill (by name) for this profession")]
		public ushort BasicSkill(SkillName skillName) {
			return BasicSkill((int) skillName);
		}
		[Summary("Return the basic value of the given skill (by id) for this profession")]
		public ushort BasicSkill(int skillId) {
			return (ushort) basicSkills[skillId].CurrentValue;
		}

		[SteamFunction]
		[Summary("Assign the selected profession to the given char. Expecting existing professions defname as an argument")]
		public static void Profession(Character chr, ScriptArgs args) {
			Player plr = chr as Player;
			if (plr == null) {
				Globals.SrcCharacter.Message("Povolání mùže být pøiøazeno pouze hráèi", (int) Hues.Red);
				return;
			}
			if ((args == null) || (args.Args == null) || args.argv.Length == 0) {
				Globals.SrcCharacter.Message("Nebylo zvoleno povolání pro pøiøazení", (int)Hues.Red);
				return;
			}
			ProfessionDef profDef = ProfessionDef.ByDefname(args.Args);
			if (profDef == null) {
				Globals.SrcCharacter.Message("Povolání " + args.Args + " neexistuje!", (int) Hues.Red);
			} else {
				profDef.AssignTo(plr);
			}
		}
		#endregion utilities
	}

	[Summary("Triggergroup holding all possible triggers for profession that can have some influence on players actions "+
			" - cancellable triggers that can cancel the performed action if the profession doesn't allow it")]
	public class E_Profession : CompiledTriggerGroup {
		
		public virtual bool On_SkillSelect(Character self, ScriptArgs sa) {
			//sa contains "self" and "skill ID"
			if (self.IsGM()) {//GM always allowed, stop checking
				return false;
			}
			return false;
		}

		public virtual bool On_SkillStart(Character self, ScriptArgs sa) {
			//sa contains "self" and "skill ID"
			if (self.IsGM()) {//GM always allowed, stop checking
				return false;
			}
			return false;
		}

		public virtual bool On_AbilityDenyAssign(DenyAbilityArgs args) {
			if (args.abiliter.IsGM()) {//GM always allowed, stop checking
				return false;
			}
			return false;
		}

		public virtual bool On_AbilityDenyUse(DenyAbilityArgs args) {
			if (args.abiliter.IsGM()) {//GM always allowed, stop checking
				return false;
			}
			return false;
		}
	}
}
