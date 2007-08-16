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
	/*
		Class: TagKey
		Used as an ID for tags
	*/
	public class TagKey : AbstractKey {
		private static Hashtable byName = new Hashtable(StringComparer.OrdinalIgnoreCase);
				
		private TagKey(string name, int uid) : base(name, uid) {
		}
		
		public static TagKey Get(string name) {
			TagKey tk = byName[name] as TagKey;
			if (tk!=null) {
				return tk;
			}
			int uid=uids++;
			tk = new TagKey(name,uid);
			byName[name]=tk;
			return tk;
		}
	}
}