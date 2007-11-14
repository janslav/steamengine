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
using SteamEngine;
using SteamEngine.Regions;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts.Dialogs {
	[ViewDescriptor(typeof(Region), "Region",
	 new string[] { "Parent", "Rectangles", "WorldRegion", "IsWorldRegion", "P"}
		)]
	public static class RegionDescriptor {
		//automaticky se zobrazi defname, createdAt, hierarchy index, mapplane
		[Button("Parent")]
		public static void Parent(object target) {
			Globals.SrcCharacter.Dialog(SingletonScript<D_Info>, target, 0, 0);
		}

		[Button("Rectangles")]
		public static void Rectangles(object target) {
			Globals.SrcCharacter.Dialog(SingletonScript<D_Region_Rectangles>, target);
		}

		[GetMethod("Position", typeof(string))]
		public static object GetPosition(object target) {
			return ((Region)target).P.ToNormalString();
		}

		[SetMethod("Position", typeof(Point4D))]
		public static object SetPosition(object target) {
			return ((Region)target).P.ToNormalString();
		}
	}

	[ViewDescriptor(typeof(StaticRegion), "Region")]
	public static class StaticRegionDescriptor {
		
	}
}