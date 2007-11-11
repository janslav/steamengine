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
using System.IO;
using SteamEngine.Common;

namespace SteamEngine {
	public class SEIniHandler : IniHandler {
		public static readonly string defaultIniFileName="steamengine.ini";

		public string IniPath {
			get { return iniPath; }
		}

		public SEIniHandler() {
			Comment("This is a comment. Comments can come either on their own lines or at the end of other lines.");
			if (this.GetType()==typeof(SEIniHandler)) {
				iniPath=defaultIniFileName;
				return;
			}
			string name=this.GetType().Name+".ini";

			iniPath = Path.Combine(Globals.scriptsPath, "config");
			Tools.EnsureDirectory(iniPath, true);
			
			iniPath = Path.Combine(iniPath, name);
		}

		public SEIniHandler(string filename) : base(filename){
		}

		public override object ConvertTo(Type type, object obj) {
			return TagMath.ConvertTo(type,obj);
		}
	}
}