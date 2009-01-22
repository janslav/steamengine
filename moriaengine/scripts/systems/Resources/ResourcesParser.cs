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

		public bool TryParse(string input, out object retVal) {
			retVal = null;
			//we dont have it yet, perform parsing, we expect sth. like this:
			//3 i_apples, 1 i_spruce_log, t_light, 5 a_warcry, 35.6 hiding etc....
			Match m = re.Match(input);
			if (m.Success) {
				ResourcesList resList = new ResourcesList();
				int n = m.Groups["resource"].Captures.Count; //number of found resources
				CaptureCollection numbers = m.Groups["number"].Captures;
				CaptureCollection values = m.Groups["value"].Captures;
				for (int i = 0; i < n; i++) {
					string number = numbers[i].Value.Equals("") ? "1" : numbers[i].Value; //either number or "" if no number is specified (in this case take 1)
					string value = values[i].Value; //resource name (trimmed)
					double nmr = ConvertTools.ParseDouble(number);
					resList.Add(createResListItem(nmr, value));
				}
				retVal = resList;
				return true;
			} else {
				throw new SEException(LogStr.Error("Unexpected resources string: " + input));
			}
		}
		#endregion

		private IResourceListItem createResListItem(double number, string definition) {
			//we will try to find what does the 'definition' define
			//try ItemDef (i_apple)		
			AbstractScript resource = ThingDef.Get(definition);
			if (resource != null && ((ThingDef) resource).IsItemDef) {
				//this resource is item
				return new ItemResource((ItemDef) resource, number, definition);
			}
			//try TriggerGroup (t_light)
			resource = TriggerGroup.Get(definition);
			if (resource != null) {
				//we dont specify the number here, we know that for trigger groups their presence in the
				//resource list always means '1' (just to 'have it')
				return new TriggerGroupResource((TriggerGroup) resource, number, definition);
			}
			//try Skilldef
			resource = SkillDef.ByKey(definition); //"hiding", "anatomy" etc.
			if (resource == null) {
				resource = SkillDef.ByDefname(definition);//"skill_hiding, "skill_anatomy" etc.
			}
			if (resource != null) {
				return new SkillResource((SkillDef) resource, number, definition);
			}
			//try AbilityDef
			resource = AbilityDef.ByDefname(definition);
			if (resource != null) {
				return new AbilityResource((AbilityDef) resource, number, definition);
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
			throw new SEException(LogStr.Error("Unresolved resource: " + number + " " + definition));
		}
	}
}