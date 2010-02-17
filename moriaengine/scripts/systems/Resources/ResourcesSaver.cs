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
	[Summary("Class for saving resources (used mainly at info Dialogs)")]
	public sealed class ResourceListSaveImplementor : ISimpleSaveImplementor {
		//same regexp as for ResourcesParser but this time contains a prefix (used in Info dialogs for setting values) 
		public static readonly Regex re = new Regex(@"^\(RL\) (?<resource>\s* (?<number>(0x?[0-9a-f]+\s*)|(\d+(\.\d+)?\s*))? (?<value>[a-z_][a-z0-9_]*) ) (\s*,\s* (?<resource> (?<number>(0x?[0-9a-f]+\s*)|(\d+(\.\d+)?\s*))? (?<value>[a-z_][a-z0-9_]*) ) )* $",
				RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);

		#region ISimpleSaveImplementor Members
		public Type HandledType {
			get {
				return typeof(ResourcesList);
			}
		}

		public Regex LineRecognizer {
			get {
				return re;
			}
		}

		public string Save(object objToSave) {
			return Prefix + ((ResourcesList)objToSave).ToDefsString();
		}

		public object Load(Match match) {
			object retVal;
			if (processMatch(match, out retVal)) {
				return retVal;
			} else {
				throw new SEException("Unable to load: " + match.Value);
			}
		}

		public string Prefix {
			get {
				return "(RL)";
			}
		}
		#endregion

		//same method as in ResourcesParser...
		private bool processMatch(Match m, out object retVal) {
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
	}
}