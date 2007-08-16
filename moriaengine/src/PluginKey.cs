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
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;
using System.Collections;
using System.Reflection;
using System.Globalization;
using SteamEngine.Packets;
using System.Text.RegularExpressions;
	
namespace SteamEngine {
	public class PluginKey : AbstractKey {
		private static Hashtable byName = new Hashtable(StringComparer.OrdinalIgnoreCase);

		private PluginKey(string name, int uid)
			: base(name, uid) {
		}

		public static PluginKey Get(string name) {
			PluginKey tk = byName[name] as PluginKey;
			if (tk!=null) {
				return tk;
			}
			int uid=uids++;
			tk = new PluginKey(name, uid);
			byName[name]=tk;
			return tk;
		}
	}


	public class PluginKeySaveImplementor : SteamEngine.Persistence.ISimpleSaveImplementor {
		public static Regex re = new Regex(@"^\@\@(?<value>.+)\s*$",                     
			RegexOptions.IgnoreCase|RegexOptions.CultureInvariant|RegexOptions.Compiled);
	
		public Type HandledType { get {
				return typeof(PluginKey);
		} }
		
		public Regex LineRecognizer { get {
			return re;
		} }
		
		public string Save(object objToSave) {
			return "@@"+((PluginKey) objToSave).name;
		}
		
		public object Load(Match match) {
			return PluginKey.Get(match.Groups["value"].Value);
		}
		
		public string Prefix {
			get {
				return "@@";
			}
		}
	}
}		