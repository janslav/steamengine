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
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using SteamEngine.Packets;
using SteamEngine.Common;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {
	public class RoleKey : AbstractKey {
		private static Dictionary<string, RoleKey> byName = new Dictionary<string, RoleKey>(StringComparer.OrdinalIgnoreCase);

		private RoleKey(string name, int uid)
			: base(name, uid) {
		}

		public static RoleKey Get(string name) {
			RoleKey key;
			if (byName.TryGetValue(name, out key)) {
				return key;
			}
			key = new RoleKey(name, uids++);
			byName[name] = key;
			return key;
		}
	}


	public sealed class RoleKeySaveImplementor : ISimpleSaveImplementor {
		public static Regex re = new Regex(@"^\°(?<value>.+)\s*$",
			RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

		public Type HandledType {
			get {
				return typeof(RoleKey);
			}
		}

		public Regex LineRecognizer {
			get {
				return re;
			}
		}

		public string Save(object objToSave) {
			return "°" + ((RoleKey) objToSave).name;
		}

		public object Load(Match match) {
			return RoleKey.Get(match.Groups["value"].Value);
		}

		public string Prefix {
			get {
				return "°";
			}
		}
	}
}
