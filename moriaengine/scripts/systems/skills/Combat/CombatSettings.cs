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
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {
	[SaveableClass]
	[HasSavedMembers]
	public class CombatSettings {

		[SavedMember]
		public static CombatSettings instance = new CombatSettings();

		[LoadingInitializer]
		public CombatSettings() {
		}

		[SaveableData]
		[Summary("How long should a character remember it's combat targets?")]
		public double secondsToRememberTargets = 300;

		[SaveableData]
		public double bareHandsAttack = 10;

		[SaveableData]
		public double bareHandsPiercing = 100;

		[SaveableData]
		public double bareHandsSpeed = 100;

		[SaveableData]
		public int bareHandsRange = 1;

		[SaveableData]
		public int bareHandsStrikeStartRange = 5;

		[SaveableData]
		public int bareHandsStrikeStopRange = 10;

		[SaveableData]
		public double weaponSpeedGlobal = 1.0;

		[SaveableData]
		public double weaponSpeedNPC = 0.15;

		[SaveableData]
		public double attackStrModifier = 317;
	}
}
