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
	public static class Notoriety {
		public static HighlightColor GetHighlightColor(Character viewer, Character self) {
			if (Party.AreInOneParty(viewer, self)) {
				return HighlightColor.Yellow;
			}
			Guild viewerGuild = Guild.GetGuild(viewer);
			if (viewerGuild != null) {
				Guild myGuild = Guild.GetGuild(self);
				if (myGuild == viewerGuild) {
					return HighlightColor.Allied;
				} else if (myGuild != null) {
					if (Guild.AreAllied(myGuild, viewerGuild)) {
						return HighlightColor.Allied;
					} else if (Guild.AreInWar(myGuild, viewerGuild)) {
						return HighlightColor.Attackable;
					}
				}
			}
			return HighlightColor.NoColor;
		}

		public static CharRelation GetCharRelation(Character a, Character b) {
			return CharRelation.Unknown;
		}
	}
}		