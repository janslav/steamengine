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
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using SteamEngine.Packets;
using SteamEngine.Networking;
using SteamEngine.Communication;

namespace SteamEngine.CompiledScripts {
	[Dialogs.ViewableClass]
	public class Guild {

		public static bool AreInOneGuild(Character a, Character b) {
			return false;
		}

		public static Guild GetGuild(Character self) {
			return null;
		}

		public static bool AreAllied(Guild a, Guild b) {
			return false;
		}

		public static bool AreInWar(Guild a, Guild b) {
			return false;
		}
	}
}		
