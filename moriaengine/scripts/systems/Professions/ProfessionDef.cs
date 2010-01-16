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
	public class ProfessionDef : AbstractIndexedDef<ProfessionDef, string> {
		internal static readonly TriggerKey tkAssign = TriggerKey.Acquire("Assign");
		internal static readonly TriggerKey tkUnAssign = TriggerKey.Acquire("UnAssign");

		//private static Dictionary<string, ProfessionDef> byName = new Dictionary<string, ProfessionDef>(StringComparer.OrdinalIgnoreCase);

		//private static Dictionary<string, ConstructorInfo> profDefCtorsByName = new Dictionary<string, ConstructorInfo>(StringComparer.OrdinalIgnoreCase);

		//triggery class-specific
		private TriggerGroup scriptedTriggers; //specified in the LScript code of the profession

		[Summary("Method for assigning the selected profession to specified player")]
		public void AssignTo(Player plr) {
			ProfessionPlugin pplInst = (ProfessionPlugin) plr.GetPlugin(ProfessionPlugin.professionKey);
			if (pplInst != null) {//we already have some profession...
				//first remove the old profession (including proper unassignment of all TGs etc.)
				Trigger_UnAssign(pplInst, plr);
			}
			//add the new profession (update the local variable reference to ProfessionPlugin
			pplInst = (ProfessionPlugin) plr.AddNewPlugin(ProfessionPlugin.professionKey, SingletonScript<ProfessionPluginDef>.Instance);
			pplInst.ProfessionDef = this;
			Trigger_Assign(pplInst, plr);
		}

		#region triggerMethods
		protected virtual void On_Assign(Player plr) {
			plr.AddTriggerGroup(scriptedTriggers); //LScript trigs. (if any)
			plr.AddTriggerGroup(CompiledTriggers); //compiled trigs. (if any)
		}

		protected void Trigger_Assign(ProfessionPlugin prof, Player plr) {
			TryTrigger(plr, ProfessionDef.tkAssign, new ScriptArgs(prof));
			plr.On_ProfessionAssign(this);
			On_Assign(plr);
		}

		protected virtual void On_UnAssign(Player plr) {
			plr.RemoveTriggerGroup(scriptedTriggers); //remove both TGs (if any)
			plr.RemoveTriggerGroup(CompiledTriggers);
			plr.RemovePlugin(ProfessionPlugin.professionKey);//and also the plugin...
		}

		[Remark("This trigger method should be called only when assigning another profession over one old " +
				"so the player never stays without the profession as a result")]
		protected void Trigger_UnAssign(ProfessionPlugin prof, Player plr) {
			TryTrigger(plr, ProfessionDef.tkUnAssign, new ScriptArgs(prof));
			plr.On_ProfessionUnAssign(this);
			On_UnAssign(plr);
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
			if (this.scriptedTriggers != null) {
				this.scriptedTriggers.TryRun(self, td, sa);
			}
		}

		#endregion triggerMethods
		
		#region Accessors
		public static new ProfessionDef GetByDefname(string defname) {
			return AbstractScript.GetByDefname(defname) as ProfessionDef;
		}

		public static ProfessionDef GetByName(string key) {
			return GetByDefIndex(key);
		}


		//this comes from sphere tables - defined and used here as well
		//CPROFESSIONPROP(Name,		CSCRIPTPROP_ARG1S, "Profession Name")
		//CPROFESSIONPROP(SkillSum,	0, "Max Sum of skills allowed")
		//CPROFESSIONPROP(StatSum,	0, "Max Sum of stats allowed")		
		//private FieldValue name; //logical name of the profession (such as "Necro")
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

		public string Name {
			get {
				return this.DefIndex;
			}
		}

		[Summary("Return the maximal value of the given skill (by name) for this profession")]
		public int MaxSkill(SkillName skillName) {
			return this.MaxSkill((int) skillName);
		}
		[Summary("Return the maximal value of the given skill (by id) for this profession")]
		public int MaxSkill(int skillId) {
			return (int) maxSkills[skillId].CurrentValue;
		}

		[Summary("Return the basic value of the given skill (by name) for this profession")]
		public int BasicSkill(SkillName skillName) {
			return this.BasicSkill((int) skillName);
		}
		[Summary("Return the basic value of the given skill (by id) for this profession")]
		public int BasicSkill(int skillId) {
			return (int) basicSkills[skillId].CurrentValue;
		}

		public override string ToString() {
			return Tools.TypeToString(this.GetType()) + " " + Name;
		}

		[Summary("Compiled trigger group specific for the given ProfessionDef")]
		public virtual E_Profession CompiledTriggers {
			get {
				//return the specific instance of triggergroup (can differ for various professions)
				return SingletonScript<E_Profession>.Instance;
			}
		}
		#endregion Accessors

		#region Load from scripts
		public ProfessionDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			//name = InitTypedField("name", "", typeof(string));
			skillSum = InitTypedField("skillSum", 0, typeof(int));
			statSum = InitTypedField("statSum", 0, typeof(int));
			//max skills
			maxAlchemy = InitTypedField("maxAlchemy", 1000, typeof(int));
			maxAnatomy = InitTypedField("maxAnatomy", 1000, typeof(int));
			maxAnimalLore = InitTypedField("maxAnimalLore", 1000, typeof(int));
			maxItemID = InitTypedField("maxItemID", 1000, typeof(int));
			maxArmsLore = InitTypedField("maxArmsLore", 1000, typeof(int));
			maxParry = InitTypedField("maxParrying", 1000, typeof(int));
			maxBegging = InitTypedField("maxBegging", 1000, typeof(int));
			maxBlacksmith = InitTypedField("maxBlacksmithing", 1000, typeof(int));
			maxFletching = InitTypedField("maxBowcraft", 1000, typeof(int));
			maxPeacemaking = InitTypedField("maxPeacemaking", 1000, typeof(int));
			maxCamping = InitTypedField("maxCamping", 1000, typeof(int));
			maxCarpentry = InitTypedField("maxCarpentry", 1000, typeof(int));
			maxCartography = InitTypedField("maxCartography", 1000, typeof(int));
			maxCooking = InitTypedField("maxCooking", 1000, typeof(int));
			maxDetectHidden = InitTypedField("maxDetectingHidden", 1000, typeof(int));
			maxDiscordance = InitTypedField("maxDiscordance", 1000, typeof(int));
			maxEvalInt = InitTypedField("maxEI", 1000, typeof(int));
			maxHealing = InitTypedField("maxHealing", 1000, typeof(int));
			maxFishing = InitTypedField("maxFishing", 1000, typeof(int));
			maxForensics = InitTypedField("maxForensics", 1000, typeof(int));
			maxHerding = InitTypedField("maxHerding", 1000, typeof(int));
			maxHiding = InitTypedField("maxHiding", 1000, typeof(int));
			maxProvocation = InitTypedField("maxProvocation", 1000, typeof(int));
			maxInscribe = InitTypedField("maxInscription", 1000, typeof(int));
			maxLockpicking = InitTypedField("maxLockpicking", 1000, typeof(int));
			maxMagery = InitTypedField("maxMagery", 1000, typeof(int));
			maxMagicResist = InitTypedField("maxResist", 1000, typeof(int));
			maxTactics = InitTypedField("maxTactics", 1000, typeof(int));
			maxSnooping = InitTypedField("maxSnooping", 1000, typeof(int));
			maxMusicianship = InitTypedField("maxMusicianship", 1000, typeof(int));
			maxPoisoning = InitTypedField("maxPoisoning", 1000, typeof(int));
			maxArchery = InitTypedField("maxArchery", 1000, typeof(int));
			maxSpiritSpeak = InitTypedField("maxSpiritSpeak", 1000, typeof(int));
			maxStealing = InitTypedField("maxStealing", 1000, typeof(int));
			maxTailoring = InitTypedField("maxTailoring", 1000, typeof(int));
			maxAnimalTaming = InitTypedField("maxTaming", 1000, typeof(int));
			maxTasteID = InitTypedField("maxTasteID", 1000, typeof(int));
			maxTinkering = InitTypedField("maxTinkering", 1000, typeof(int));
			maxTracking = InitTypedField("maxTracking", 1000, typeof(int));
			maxVeterinary = InitTypedField("maxVeterinary", 1000, typeof(int));
			maxSwords = InitTypedField("maxSwordsmanship", 1000, typeof(int));
			maxMacing = InitTypedField("maxMacefighting", 1000, typeof(int));
			maxFencing = InitTypedField("maxFencing", 1000, typeof(int));
			maxWrestling = InitTypedField("maxWrestling", 1000, typeof(int));
			maxLumberjacking = InitTypedField("maxLumberjacking", 1000, typeof(int));
			maxMining = InitTypedField("maxMining", 1000, typeof(int));
			maxMeditation = InitTypedField("maxMeditation", 1000, typeof(int));
			maxStealth = InitTypedField("maxStealth", 1000, typeof(int));
			maxRemoveTrap = InitTypedField("maxRemoveTrap", 1000, typeof(int));
			maxNecromancy = InitTypedField("maxNecromancy", 1000, typeof(int));
			maxMarksmanship = InitTypedField("maxMarksmanship", 1000, typeof(int));
			maxChivalry = InitTypedField("maxChivalry", 1000, typeof(int));
			maxBushido = InitTypedField("maxBushido", 1000, typeof(int));
			maxNinjitsu = InitTypedField("maxNinjutsu", 1000, typeof(int));
			//basic skills
			basicAlchemy = InitTypedField("basicAlchemy", 1000, typeof(int));
			basicAnatomy = InitTypedField("basicAnatomy", 1000, typeof(int));
			basicAnimalLore = InitTypedField("basicAnimalLore", 1000, typeof(int));
			basicItemID = InitTypedField("basicItemID", 1000, typeof(int));
			basicArmsLore = InitTypedField("basicArmsLore", 1000, typeof(int));
			basicParry = InitTypedField("basicParrying", 1000, typeof(int));
			basicBegging = InitTypedField("basicBegging", 1000, typeof(int));
			basicBlacksmith = InitTypedField("basicBlacksmithing", 1000, typeof(int));
			basicFletching = InitTypedField("basicBowcraft", 1000, typeof(int));
			basicPeacemaking = InitTypedField("basicPeacemaking", 1000, typeof(int));
			basicCamping = InitTypedField("basicCamping", 1000, typeof(int));
			basicCarpentry = InitTypedField("basicCarpentry", 1000, typeof(int));
			basicCartography = InitTypedField("basicCartography", 1000, typeof(int));
			basicCooking = InitTypedField("basicCooking", 1000, typeof(int));
			basicDetectHidden = InitTypedField("basicDetectingHidden", 1000, typeof(int));
			basicDiscordance = InitTypedField("basicDiscordance", 1000, typeof(int));
			basicEvalInt = InitTypedField("basicEI", 1000, typeof(int));
			basicHealing = InitTypedField("basicHealing", 1000, typeof(int));
			basicFishing = InitTypedField("basicFishing", 1000, typeof(int));
			basicForensics = InitTypedField("basicForensics", 1000, typeof(int));
			basicHerding = InitTypedField("basicHerding", 1000, typeof(int));
			basicHiding = InitTypedField("basicHiding", 1000, typeof(int));
			basicProvocation = InitTypedField("basicProvocation", 1000, typeof(int));
			basicInscribe = InitTypedField("basicInscription", 1000, typeof(int));
			basicLockpicking = InitTypedField("basicLockpicking", 1000, typeof(int));
			basicMagery = InitTypedField("basicMagery", 1000, typeof(int));
			basicMagicResist = InitTypedField("basicResist", 1000, typeof(int));
			basicTactics = InitTypedField("basicTactics", 1000, typeof(int));
			basicSnooping = InitTypedField("basicSnooping", 1000, typeof(int));
			basicMusicianship = InitTypedField("basicMusicianship", 1000, typeof(int));
			basicPoisoning = InitTypedField("basicPoisoning", 1000, typeof(int));
			basicArchery = InitTypedField("basicArchery", 1000, typeof(int));
			basicSpiritSpeak = InitTypedField("basicSpiritSpeak", 1000, typeof(int));
			basicStealing = InitTypedField("basicStealing", 1000, typeof(int));
			basicTailoring = InitTypedField("basicTailoring", 1000, typeof(int));
			basicAnimalTaming = InitTypedField("basicTaming", 1000, typeof(int));
			basicTasteID = InitTypedField("basicTasteID", 1000, typeof(int));
			basicTinkering = InitTypedField("basicTinkering", 1000, typeof(int));
			basicTracking = InitTypedField("basicTracking", 1000, typeof(int));
			basicVeterinary = InitTypedField("basicVeterinary", 1000, typeof(int));
			basicSwords = InitTypedField("basicSwordsmanship", 1000, typeof(int));
			basicMacing = InitTypedField("basicMacefighting", 1000, typeof(int));
			basicFencing = InitTypedField("basicFencing", 1000, typeof(int));
			basicWrestling = InitTypedField("basicWrestling", 1000, typeof(int));
			basicLumberjacking = InitTypedField("basicLumberjacking", 1000, typeof(int));
			basicMining = InitTypedField("basicMining", 1000, typeof(int));
			basicMeditation = InitTypedField("basicMeditation", 1000, typeof(int));
			basicStealth = InitTypedField("basicStealth", 1000, typeof(int));
			basicRemoveTrap = InitTypedField("basicRemoveTrap", 1000, typeof(int));
			basicNecromancy = InitTypedField("basicNecromancy", 1000, typeof(int));
			basicMarksmanship = InitTypedField("basicMarksmanship", 1000, typeof(int));
			basicChivalry = InitTypedField("basicChivalry", 1000, typeof(int));
			basicBushido = InitTypedField("basicBushido", 1000, typeof(int));
			basicNinjitsu = InitTypedField("basicNinjutsu", 1000, typeof(int));

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

		public override void LoadScriptLines(PropsSection ps) {
			PropsLine p = ps.PopPropsLine("name");
			this.DefIndex = ConvertTools.LoadSimpleQuotedString(p.Value);

			base.LoadScriptLines(ps);

			if (ps.TriggerCount > 0) {
				ps.HeaderName = "t__" + this.Defname + "__";
				this.scriptedTriggers = ScriptedTriggerGroup.Load(ps);
			}
		}

		public override void Unload() {
			if (this.scriptedTriggers != null) {
				this.scriptedTriggers.Unload();
			}
			base.Unload();
		}

		protected override void LoadScriptLine(string filename, int line, string param, string args) {
			base.LoadScriptLine(filename, line, param, args);
		}
		#endregion Load from scripts

		#region utilities
		[SteamFunction]
		[Summary("Assign the selected profession to the given char. Expecting existing professions defname as an argument")]
		public static void Profession(Character chr, ScriptArgs args) {
			Player plr = chr as Player;
			if (plr == null) {
				Globals.SrcCharacter.Message("Povolání mùže být pøiøazeno pouze hráèi", (int) Hues.Red);
				return;
			}
			if ((args == null) || (args.Args == null) || args.Argv.Length == 0) {
				Globals.SrcCharacter.Message("Nebylo zvoleno povolání pro pøiøazení", (int) Hues.Red);
				return;
			}
			ProfessionDef profDef = ProfessionDef.GetByDefname(args.Args);
			if (profDef == null) {
				Globals.SrcCharacter.Message("Povolání " + args.Args + " neexistuje!", (int) Hues.Red);
			} else {
				profDef.AssignTo(plr);
			}
		}
		#endregion utilities
	}

	[Summary("Triggergroup holding all possible triggers for profession that can have some influence on players actions " +
			" - cancellable triggers that can cancel the performed action if the profession doesn't allow it")]
	public class E_Profession : CompiledTriggerGroup {

		public virtual bool On_SkillSelect(Character self, ScriptArgs sa) {
			//sa contains "self" and "skill ID"
			if (self.IsGM) {//GM always allowed, stop checking
				return false;
			}
			return false;
		}

		public virtual bool On_SkillStart(Character self, ScriptArgs sa) {
			//sa contains "self" and "skill ID"
			if (self.IsGM) {//GM always allowed, stop checking
				return false;
			}
			return false;
		}

		public virtual bool On_AbilityDenyAssign(DenyAbilityArgs args) {
			if (args.abiliter.IsGM) {//GM always allowed, stop checking
				return false;
			}
			return false;
		}

		public virtual bool On_AbilityDenyUse(DenyAbilityArgs args) {
			if (args.abiliter.IsGM) {//GM always allowed, stop checking
				return false;
			}
			return false;
		}
	}
}
