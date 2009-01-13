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
using SteamEngine.Persistence;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {
	[SaveableClass]
	public class GuildMembership : Rank {
		public bool canShowAbbrev = false;

		internal GuildMembership(RankDef def, RoleKey key)
			: base(def, key) {

		}

		[Save]
		public override void Save(SaveStream output) {
			if (canShowAbbrev) {
				output.WriteValue("canShowAbbrev", canShowAbbrev);
			}
			base.Save(output);
		}

		public override void LoadLine(string filename, int line, string valueName, string valueString) {
			switch (valueName) {
				case "canShowAbbrev":
					this.canShowAbbrev = ConvertTools.ParseBoolean(valueString);
					return;
			}
			base.LoadLine(filename, line, valueName, valueString);
		}
	}

	public class GuildMembershipDef : RankDef {
		public static readonly GuildMembershipDef instance = new GuildMembershipDef("r_guild", "C# scripts", -1);

		public GuildMembershipDef(string defname, string filename, int headerline)
			: base(defname, filename, headerline) {

		}

		protected override Role CreateImpl(RoleKey key) {
			return new GuildMembership(this, key);
		}
	}
}		
