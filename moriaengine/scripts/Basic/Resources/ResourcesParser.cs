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
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Globalization;
using SteamEngine.Common;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {
	[Summary("Class for parsing resources strings from LScript")]
	public class ResourcesParser : IFieldValueParser {
		//(^(\s*0x?[0-9a-f]+\s*)$)|(^(\s*\d+(\.\d+)?)$) = pattern for recognizing hex, float and decimal numbers together
		//regular expression for recogninzing the whole reslist (first resource is obligatory, others are voluntary (separated by commas), all resources contain from a number-value pair where number can be hex, float and decimal
		public static readonly Regex re = new Regex(@"^ (?<resource>\s* (?<number>(0x?[0-9a-f]+\s*)|(\d+(\.\d+)?\s*))? (?<value>[a-z_][a-z0-9_]*) ) (\s*,\s* (?<resource> (?<number>(0x?[0-9a-f]+\s*)|(\d+(\.\d+)?\s*))? (?<value>[a-z_][a-z0-9_]*) ) )* $",
				RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
		
		#region IFieldValueParser Members

		public Type HandledType {
			get {
				return typeof(ResourcesList);
			}
		}

		public static ResourcesList Parse(string input) {
			object retVal;
			Match m = re.Match(input);
			if (processMatch(m, out retVal)) {
				return (ResourcesList)retVal;
			}
			throw new SEException("Invalid resources string: " + input);
		}

		public bool TryParse(string input, out object retVal) {
			return InternalTryParse(input, out retVal);
		}

		private static bool InternalTryParse(string input, out object retVal) {
			//we dont have it yet, perform parsing, we expect sth. like this:
			//3 i_apples, 1 i_spruce_log, t_light, 5 a_warcry, 35.6 hiding etc....
			Match m = re.Match(input);
			if (processMatch(m, out retVal)) {
				return true;
			}
			return false;
		}

		private static bool processMatch(Match m, out object retVal) {
			if (m.Success) {
				List<IResourceListItem> resources = new List<IResourceListItem>();
				int n = m.Groups["resource"].Captures.Count; //number of found resources
				CaptureCollection numbers = m.Groups["number"].Captures;
				CaptureCollection values = m.Groups["value"].Captures;
				for (int i = 0; i < n; i++) {
					string number = "";
					try {
						number = numbers[i].Value;
						if (number.Equals(""))
							number = "1";
					} catch {
						//maybe we have something like this: "2 i_something, i_something_else" which we want to be interpreted as "2 ..., 1 ..."
						number = "1";
					}
					string value = values[i].Value; //resource name (trimmed)
					double nmr = ConvertTools.ParseDouble(number);
					resources.Add(ResourcesParser.createResListItem(nmr, value));
				}
				retVal = new ResourcesList(resources);
				return true;
			}
			retVal = null;
			return false;
		}
		#endregion

		[Summary("Check if the given string defines some Item. If so, return the def")]
		internal static bool IsItemResource(string definition, out ItemDef idef) {
			if ((idef = ItemDef.GetByDefname(definition) as ItemDef) != null) {
				//this resource is an item
				return true;
			}
			return false;
		}

		[Summary("Check if the given string defines some TriggerGroup. If so, return it")]
		internal static bool IsTriggerGroupResource(string definition, out TriggerGroup tgr) {
			if ((tgr = TriggerGroup.GetByDefname(definition)) != null) {
				//this resource is a trigger group
				return true;
			}
			return false;
		}

		[Summary("Check if the given string defines some Skill. If so, return the def")]
		internal static bool IsSkillResource(string definition, out SkillDef skl) {
			if (((skl = SkillDef.GetByKey(definition) as SkillDef) != null) ||  //"hiding", "anatomy" etc.
				 ((skl = SkillDef.GetByDefname(definition) as SkillDef) != null)) {//"skill_hiding, "skill_anatomy" etc.
				//this resource is a skill
				return true;
			}
			return false;
		}

		[Summary("Check if the given string defines some Ability. If so, return the def")]
		internal static bool IsAbilityResource(string definition, out AbilityDef abl) {
			if ((abl = AbilityDef.GetByDefname(definition)) != null) {
				//this resource is an ability
				return true;
			}
			return false;
		}

		internal static IResourceListItem createResListItem(double number, string definition) {
			//we will try to find what does the 'definition' define
			//try ItemDef (i_apple)
			ItemDef idef;
			if(IsItemResource(definition, out idef)) {
				//this resource is item
				return new ItemResource(idef, number, definition);
			}
			//try TriggerGroup (t_light)
			TriggerGroup tgr;
			if (IsTriggerGroupResource(definition, out tgr)) {
				//we dont specify the number here, we know that for trigger groups their presence in the
				//resource list always means '1' (just to 'have it')
				return new TriggerGroupResource(tgr, number, definition);
			}
			//try Skilldef
			SkillDef skl;
			if (IsSkillResource(definition, out skl)) {
				return new SkillResource(skl, number, definition);
			}
			//try AbilityDef
			AbilityDef abl;
			if (IsAbilityResource(definition, out abl)) {
				return new AbilityResource(abl, number, definition);
			}
			
			//try stats
			if (definition.Equals("str", StringComparison.InvariantCultureIgnoreCase)) {
				return new StatStrResource(number);
			}
			if (definition.Equals("dex", StringComparison.InvariantCultureIgnoreCase)) {
				return new StatDexResource(number);
			}
			if (definition.Equals("int", StringComparison.InvariantCultureIgnoreCase)) {
				return new StatIntResource(number);
			}
			if (definition.Equals("vit", StringComparison.InvariantCultureIgnoreCase)) {
				return new StatVitResource(number);
			}

			//try the level resource
			if (definition.Equals("level", StringComparison.InvariantCultureIgnoreCase)) {
				return new LevelResource((int)number);
			}
			throw new SEException(LogStr.Error("Unresolved resource: " + number + " " + definition));
		}
	}
}