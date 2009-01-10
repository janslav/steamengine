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
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {

	[Summary("IDs of important and used Gumps")]
	public enum GumpIDs : int {
		Figurine_Orc = 0x20e0,
		Figurine_Ogre = 0x20df,
		Figurine_Llama = 0x20f6,
		Figurine_Man = 0x20cd,
		Figurine_Woman = 0x20ce,
		Figurine_NPC = 0x2106,

		//models for footprints (unfortunatelly we have only 4 directions although we need 8 :-/)
		Footprint_West = 0x1e03,
		Footprint_North = 0x1e04,
		Footprint_East = 0x1e05,
		Footprint_South = 0x1e06
	}	
}