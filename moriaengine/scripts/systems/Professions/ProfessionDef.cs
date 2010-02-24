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
	[Summary("Def listing fields necessary for all professions. Actual profession-elated active code is in ProfessionPlugin class")]
	public class ProfessionDef : AbstractIndexedDef<ProfessionDef, string> {
		#region Accessors
		public static new ProfessionDef GetByDefname(string defname) {
			return AbstractScript.GetByDefname(defname) as ProfessionDef;
		}

		public static ProfessionDef GetByName(string key) {
			return GetByDefIndex(key);
		}

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
		private FieldValue professionPluginDef;
		private FieldValue allowedSpells;
		private FieldValue allowedAbilities;

		private FieldValue ttb1; //ttb = TalentTreeBranch
		private FieldValue ttb2;
		private FieldValue ttb3;

		private HashSet<AbilityDef> cachedAbilities;
		private HashSet<SpellDef> cachedSpells;

		public string Name {
			get {
				return this.DefIndex;
			}
		}

		public PluginDef ProfessionPluginDef {
			get {
				return (PluginDef) this.professionPluginDef.CurrentValue;
			}
			set {
				this.professionPluginDef.CurrentValue = value;
			}
		}

		private HashSet<AbilityDef> GetCachedAbilities() {
			if (this.cachedAbilities == null) {
				HashSet<AbilityDef> hs = new HashSet<AbilityDef>();
				foreach (AbilityDef def in (AbilityDef[]) this.allowedAbilities.CurrentValue) {
					hs.Add(def);
				}
				this.cachedAbilities = hs;
			}
			return this.cachedAbilities;
		}

		public ICollection<AbilityDef> AllowedAbilities {
			get {
				return this.GetCachedAbilities();
			}
		}

		public bool CanUseAbility(AbilityDef ability) {
			return this.GetCachedAbilities().Contains(ability);
		}

		private HashSet<SpellDef> GetCachedSpells() {
			if (this.cachedSpells == null) {
				HashSet<SpellDef> hs = new HashSet<SpellDef>();
				foreach (SpellDef def in (SpellDef[]) this.allowedSpells.CurrentValue) {
					hs.Add(def);
				}
				this.cachedSpells = hs;
			}
			return this.cachedSpells;
		}

		public ICollection<SpellDef> AllowedSpells {
			get {
				return this.GetCachedSpells();
			}
		}

		public bool CanCastSpell(SpellDef spell) {
			return this.GetCachedSpells().Contains(spell);
		}

		public TalentTreeBranchDef TTB1 {
			get {
				return (TalentTreeBranchDef) this.ttb1.CurrentValue;
			}
			set {
				this.ttb1.CurrentValue = value;
			}
		}

		public TalentTreeBranchDef TTB2 {
			get {
				return (TalentTreeBranchDef) this.ttb2.CurrentValue;
			}
			set {
				this.ttb2.CurrentValue = value;
			}
		}

		public TalentTreeBranchDef TTB3 {
			get {
				return (TalentTreeBranchDef) this.ttb3.CurrentValue;
			}
			set {
				this.ttb3.CurrentValue = value;
			}
		}

		public override void Unload() {
			base.Unload();
			this.cachedSpells = null;
			this.cachedAbilities = null;
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
			return string.Concat("[", this.Name, " ", Tools.TypeToString(this.GetType()), "]");
		}
		#endregion Accessors

		#region Load from scripts
		public ProfessionDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {

			this.professionPluginDef = this.InitTypedField("professionPluginDef", null, typeof(PluginDef));
			this.allowedSpells = this.InitTypedField("allowedSpells", new SpellDef[0], typeof(SpellDef[]));
			this.allowedAbilities = this.InitTypedField("allowedAbilities", new AbilityDef[0], typeof(AbilityDef[]));

			this.ttb1 = this.InitTypedField("ttb1", null, typeof(TalentTreeBranchDef));
			this.ttb2 = this.InitTypedField("ttb2", null, typeof(TalentTreeBranchDef));
			this.ttb3 = this.InitTypedField("ttb3", null, typeof(TalentTreeBranchDef));

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
			basicAlchemy = InitTypedField("basicAlchemy", 0, typeof(int));
			basicAnatomy = InitTypedField("basicAnatomy", 0, typeof(int));
			basicAnimalLore = InitTypedField("basicAnimalLore", 0, typeof(int));
			basicItemID = InitTypedField("basicItemID", 0, typeof(int));
			basicArmsLore = InitTypedField("basicArmsLore", 0, typeof(int));
			basicParry = InitTypedField("basicParrying", 0, typeof(int));
			basicBegging = InitTypedField("basicBegging", 0, typeof(int));
			basicBlacksmith = InitTypedField("basicBlacksmithing", 0, typeof(int));
			basicFletching = InitTypedField("basicBowcraft", 0, typeof(int));
			basicPeacemaking = InitTypedField("basicPeacemaking", 0, typeof(int));
			basicCamping = InitTypedField("basicCamping", 0, typeof(int));
			basicCarpentry = InitTypedField("basicCarpentry", 0, typeof(int));
			basicCartography = InitTypedField("basicCartography", 0, typeof(int));
			basicCooking = InitTypedField("basicCooking", 0, typeof(int));
			basicDetectHidden = InitTypedField("basicDetectingHidden", 0, typeof(int));
			basicDiscordance = InitTypedField("basicDiscordance", 0, typeof(int));
			basicEvalInt = InitTypedField("basicEI", 0, typeof(int));
			basicHealing = InitTypedField("basicHealing", 0, typeof(int));
			basicFishing = InitTypedField("basicFishing", 0, typeof(int));
			basicForensics = InitTypedField("basicForensics", 0, typeof(int));
			basicHerding = InitTypedField("basicHerding", 0, typeof(int));
			basicHiding = InitTypedField("basicHiding", 0, typeof(int));
			basicProvocation = InitTypedField("basicProvocation", 0, typeof(int));
			basicInscribe = InitTypedField("basicInscription", 0, typeof(int));
			basicLockpicking = InitTypedField("basicLockpicking", 0, typeof(int));
			basicMagery = InitTypedField("basicMagery", 0, typeof(int));
			basicMagicResist = InitTypedField("basicResist", 0, typeof(int));
			basicTactics = InitTypedField("basicTactics", 0, typeof(int));
			basicSnooping = InitTypedField("basicSnooping", 0, typeof(int));
			basicMusicianship = InitTypedField("basicMusicianship", 0, typeof(int));
			basicPoisoning = InitTypedField("basicPoisoning", 0, typeof(int));
			basicArchery = InitTypedField("basicArchery", 0, typeof(int));
			basicSpiritSpeak = InitTypedField("basicSpiritSpeak", 0, typeof(int));
			basicStealing = InitTypedField("basicStealing", 0, typeof(int));
			basicTailoring = InitTypedField("basicTailoring", 0, typeof(int));
			basicAnimalTaming = InitTypedField("basicTaming", 0, typeof(int));
			basicTasteID = InitTypedField("basicTasteID", 0, typeof(int));
			basicTinkering = InitTypedField("basicTinkering", 0, typeof(int));
			basicTracking = InitTypedField("basicTracking", 0, typeof(int));
			basicVeterinary = InitTypedField("basicVeterinary", 0, typeof(int));
			basicSwords = InitTypedField("basicSwordsmanship", 0, typeof(int));
			basicMacing = InitTypedField("basicMacefighting", 0, typeof(int));
			basicFencing = InitTypedField("basicFencing", 0, typeof(int));
			basicWrestling = InitTypedField("basicWrestling", 0, typeof(int));
			basicLumberjacking = InitTypedField("basicLumberjacking", 0, typeof(int));
			basicMining = InitTypedField("basicMining", 0, typeof(int));
			basicMeditation = InitTypedField("basicMeditation", 0, typeof(int));
			basicStealth = InitTypedField("basicStealth", 0, typeof(int));
			basicRemoveTrap = InitTypedField("basicRemoveTrap", 0, typeof(int));
			basicNecromancy = InitTypedField("basicNecromancy", 0, typeof(int));
			basicMarksmanship = InitTypedField("basicMarksmanship", 0, typeof(int));
			basicChivalry = InitTypedField("basicChivalry", 0, typeof(int));
			basicBushido = InitTypedField("basicBushido", 0, typeof(int));
			basicNinjitsu = InitTypedField("basicNinjutsu", 0, typeof(int));

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
				Logger.WriteWarning("Triggers in a ProfessionDef aren't valid. Use the relevant PluginDef for that functionality");
			}
		}
		#endregion Load from scripts

		#region Static utility methods

		public static ProfessionDef GetProfessionOfChar(Player player) {
			ProfessionPlugin plugin = ProfessionPlugin.GetInstalledPlugin(player);
			if (plugin != null) {
				return plugin.ProfessionDef;
			}
			return null;
		}

		public static void SetProfessionOfChar(Player player, ProfessionDef value) {
			ProfessionPlugin.InstallProfessionPlugin(player, value);
		}
		#endregion Static utility methods

	}
}