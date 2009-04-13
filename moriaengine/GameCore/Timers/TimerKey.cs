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

namespace SteamEngine.Timers {
	public class TimerKey : AbstractKey {
		private static Dictionary<string, TimerKey> byName = new Dictionary<string, TimerKey>(StringComparer.OrdinalIgnoreCase);

		private TimerKey(string name, int uid)
			: base(name, uid) {
		}

		public static TimerKey Get(string name) {
			TimerKey key;
			if (byName.TryGetValue(name, out key)) {
				return key;
			}
			key = new TimerKey(name, AbstractKey.GetNewUid());
			byName[name] = key;
			return key;
		}
	}

	public sealed class TimerKeySaveImplementor : SteamEngine.Persistence.ISimpleSaveImplementor {
		public Type HandledType {
			get {
				return typeof(TimerKey);
			}
		}


		public Regex LineRecognizer {
			get {
				return TagHolder.timerKeyRE;
			}
		}

		public string Save(object objToSave) {
			return "%" + ((TimerKey) objToSave).Name;
		}

		public object Load(Match match) {
			return TimerKey.Get(match.Groups["value"].Value);
		}

		public string Prefix {
			get {
				return "%";
			}
		}
	}
}