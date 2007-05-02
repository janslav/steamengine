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

	public static class DamageImpl {

		public static double GetResistModifier(Character resistingChar, DamageType damageType) {
			int intDamageType = (int) damageType;

			//phase 1
			double hasResistMagic =		((intDamageType & 0x0001) == 0)? 0.0 : 0.001;
			double hasResistPhysical =	((intDamageType & 0x0002) == 0)? 0.0 : 0.001;

			double modifier = 1000;
			double resistCount = hasResistMagic + hasResistPhysical;
			modifier = modifier * resistCount;

			modifier -= hasResistMagic * resistingChar.ResistMagic;
			modifier -= hasResistPhysical * resistingChar.ResistPhysical;

			//phase 2
			double hasResistFire =		((intDamageType & 0x0004) == 0)? 0.0 : 0.001;
			double hasResistElectric =	((intDamageType & 0x0008) == 0)? 0.0 : 0.001;
			double hasResistAcid =		((intDamageType & 0x0010) == 0)? 0.0 : 0.001;
			double hasResistCold =		((intDamageType & 0x0020) == 0)? 0.0 : 0.001;
			double hasResistPoison =	((intDamageType & 0x0040) == 0)? 0.0 : 0.001;
			double hasResistMystical =	((intDamageType & 0x0080) == 0)? 0.0 : 0.001;
			double hasResistSlashing =	((intDamageType & 0x0100) == 0)? 0.0 : 0.001;
			double hasResistStabbing =	((intDamageType & 0x0200) == 0)? 0.0 : 0.001;
			double hasResistBlunt =		((intDamageType & 0x0400) == 0)? 0.0 : 0.001;
			double hasResistArchery =	((intDamageType & 0x0800) == 0)? 0.0 : 0.001;
			double hasResistBleed =		((intDamageType & 0x1000) == 0)? 0.0 : 0.001;
			double hasResistSummon =	((intDamageType & 0x2000) == 0)? 0.0 : 0.001;
			double hasResistDragon =	((intDamageType & 0x4000) == 0)? 0.0 : 0.001;

			resistCount = hasResistFire + hasResistElectric + hasResistAcid + hasResistCold +
				hasResistPoison + hasResistMystical + hasResistSlashing + hasResistStabbing +
				hasResistBlunt + hasResistArchery + hasResistBleed + hasResistSummon + hasResistDragon;
			modifier = modifier * resistCount;

			modifier -= hasResistFire * resistingChar.ResistFire;
			modifier -= hasResistElectric * resistingChar.ResistElectric;
			modifier -= hasResistAcid * resistingChar.ResistAcid;
			modifier -= hasResistCold * resistingChar.ResistCold;
			modifier -= hasResistPoison * resistingChar.ResistPoison;
			modifier -= hasResistMystical * resistingChar.ResistMystical;
			modifier -= hasResistSlashing * resistingChar.ResistSlashing;
			modifier -= hasResistStabbing * resistingChar.ResistStabbing;
			modifier -= hasResistBlunt * resistingChar.ResistBlunt;
			modifier -= hasResistArchery * resistingChar.ResistArchery;
			modifier -= hasResistBleed * resistingChar.ResistBleed;
			modifier -= hasResistSummon * resistingChar.ResistSummon;
			modifier -= hasResistDragon * resistingChar.ResistDragon;

			return modifier;
		}



	}
}
