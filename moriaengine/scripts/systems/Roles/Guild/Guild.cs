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
	[Dialogs.ViewableClass]
	[SaveableClass]
	public class Guild : RankSystem {

		public static readonly RoleKey guildRK = RoleKey.Get("_guild_");

		private static readonly List<Guild> allGuilds = new List<Guild>();

		public Guild()
			: base() {

			allGuilds.Add(this);
		}

		public Guild(string name)
			: base(name) {

			allGuilds.Add(this);
		}

		public static bool AreInOneGuild(Character a, Character b) {
			return false;
		}

		public static Guild GetGuild(Character self) {
			return null;
		}

		//TODO?
		public static bool AreAllied(Guild a, Guild b) {
			return false;
		}

		//TODO?
		public static bool AreInWar(Guild a, Guild b) {
			return false;
		}




		public override void LoadLine(string filename, int line, string valueName, string valueString) {
			base.LoadLine(filename, line, valueName, valueString);
		}

		public override void Save(SteamEngine.Persistence.SaveStream output) {
			base.Save(output);
		}

		protected override void On_DisposeManagedResources() {
			allGuilds.Remove(this);

			base.On_DisposeManagedResources();
		}
	}
}		
