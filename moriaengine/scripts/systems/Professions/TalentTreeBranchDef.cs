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
	public class TalentTreeBranchDef : AbstractIndexedDef<TalentTreeBranchDef, string> {
		#region Accessors
		public static new TalentTreeBranchDef GetByDefname(string defname) {
			return AbstractScript.GetByDefname(defname) as TalentTreeBranchDef;
		}

		public static TalentTreeBranchDef GetByName(string key) {
			return GetByDefIndex(key);
		}

		private Dictionary<AbilityDef, TalentTreeLeaf> cachedLeafs = new Dictionary<AbilityDef, TalentTreeLeaf>();

		public string Name {
			get {
				return this.DefIndex;
			}
		}

		public int GetTalentMaxPoints(AbilityDef def) {
			TalentTreeLeaf leaf = this.GetLeaf(def);
			if (leaf != null) {
				return leaf.MaxPoints;
			}
			return 0;
		}

		public void SetTalentMaxPoints(AbilityDef def, int points) {
			string fvName = "TalentMaxPoints." + def.PrettyDefname;
			FieldValue fv = this.GetFieldValue(fvName);
			if (fv == null) {
				fv = this.InitTypedField(fvName, 0, typeof(int));
			}
			fv.CurrentValue = points;
			this.cachedLeafs.Remove(def); //remove from cache
		}

		public ResourcesList GetTalentDependency(AbilityDef def) {
			TalentTreeLeaf leaf = this.GetLeaf(def);
			if (leaf != null) {
				return leaf.Dependencies;
			}
			return null;
		}

		public void SetTalentDependency(AbilityDef def, ResourcesList dependency) {
			string fvName = "TalentDependency." + def.PrettyDefname;
			FieldValue fv = this.GetFieldValue(fvName);
			if (fv == null) {
				fv = this.InitTypedField(fvName, 0, typeof(int));
			}
			fv.CurrentValue = dependency;
			this.cachedLeafs.Remove(def); //remove from cache
		}

		public TalentTreeLeaf GetLeaf(AbilityDef def) {
			TalentTreeLeaf leaf;
			if (!cachedLeafs.TryGetValue(def, out leaf)) {
				string defname = def.PrettyDefname;
				object tierObj = this.GetCurrentFieldValue("TalentTier." + defname);
				if (tierObj != null) {
					leaf = new TalentTreeLeaf(def,
						Convert.ToInt32(tierObj),
						Convert.ToInt32(this.GetCurrentFieldValue("TalentTierPosition." + defname)),
						Convert.ToInt32(this.GetCurrentFieldValue("TalentMaxPoints." + defname)),
						(ResourcesList) this.GetCurrentFieldValue("TalentDependency." + defname));
					cachedLeafs[def] = leaf;
				}
			}
			return leaf;
		}

		public override string ToString() {
			return string.Concat("[", this.Name, ": ", Tools.TypeToString(this.GetType()), "]");
		}
		#endregion Accessors

		#region Load from scripts
		public TalentTreeBranchDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		public override void LoadScriptLines(PropsSection ps) {
			PropsLine p = ps.PopPropsLine("name");
			this.DefIndex = ConvertTools.LoadSimpleQuotedString(p.Value);

			base.LoadScriptLines(ps);

			if (ps.TriggerCount > 0) {
				Logger.WriteWarning("Triggers in a TalentTreeBranchDef are ignored.");
			}
		}

		protected override void LoadScriptLine(string filename, int line, string param, string args) {
			//try recognizing an ability name or defname
			AbilityDef ability = AbilityDef.GetByDefname(param);
			if (ability == null) {
				ability = AbilityDef.GetByName(param);
			}

			//now load the parameters. it goes Tier, TierPosition, MaxPoints, resourcelist
			if (ability != null) {
				string[] preparsed = Utility.SplitSphereString(args);
				int len = preparsed.Length;
				if (len < 3) {
					throw new SEException("TalentTree entries need at least 3 numbers - Tier, TierPosition, MaxPoints");
				}

				string abilityName = ability.PrettyDefname;
				this.InitTypedField("TalentTier." + abilityName, 0, typeof(int))
					.SetFromScripts(filename, line, preparsed[0]);
				this.InitTypedField("TalentTierPosition." + abilityName, 0, typeof(int))
					.SetFromScripts(filename, line, preparsed[1]);
				this.InitTypedField("TalentMaxPoints." + abilityName, 0, typeof(int))
					.SetFromScripts(filename, line, preparsed[2]);

				string reconstructedResList = "";
				if (len > 3) {
					reconstructedResList = String.Join(", ", preparsed, 3, len - 3);
				}
				this.InitTypedField("TalentDependency." + abilityName, null, typeof(ResourcesList))
					.SetFromScripts(filename, line, reconstructedResList);

			} else {
				base.LoadScriptLine(filename, line, param, args);
			}
		}

		public override void Unload() {
			this.cachedLeafs.Clear();
			base.Unload();
		}

		public override void UnUnload() {
			this.cachedLeafs.Clear();
			base.UnUnload();
		}

		protected override void Unregister() {
			this.cachedLeafs.Clear();
			base.Unregister();
		}

		public override AbstractScript Register() {
			this.cachedLeafs.Clear();
			return base.Register();
		}
		#endregion Load from scripts

		#region Load from saves
		public override void LoadFromSaves(PropsSection input) {
			foreach (PropsLine line in input.PropsLines) {
				string name = line.Name;
				if (this.GetFieldValue(name) == null) {
					if (name.StartsWith("TalentTier.") ||
							name.StartsWith("TalentTierPosition.") ||
							name.StartsWith("TalentMaxPoints.")) {
						this.InitTypedField(name, 0, typeof(int));
					} else if (name.StartsWith("TalentDependency.")) {
						this.InitTypedField(name, "", typeof(ResourcesList));
					}
				}
			}

			base.LoadFromSaves(input);
		}
		#endregion Load from saves
	}

	//represents the settings of 1 talent in 1 talenttree branch
	public class TalentTreeLeaf {
		private AbilityDef talent;
		private int tier;
		private int tierPosition;
		private int maxPoints;
		private ResourcesList dependencies;

		public TalentTreeLeaf(AbilityDef talent, int tier, int tierPosition, int maxPoints, ResourcesList dependencies) {
			this.talent = talent;
			this.tier = tier;
			this.tierPosition = tierPosition;
			this.maxPoints = maxPoints;
			this.dependencies = dependencies;
		}

		public AbilityDef Talent {
			get {
				return this.talent;
			}
		}

		public int Tier {
			get {
				return this.tier;
			}
		}

		public int TierPosition {
			get {
				return this.tierPosition;
			}
		}

		public int MaxPoints {
			get {
				return this.maxPoints;
			}
		}

		public ResourcesList Dependencies {
			get {
				return this.dependencies;
			}
		}
	}
}