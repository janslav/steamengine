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
using System.Collections.Generic;
using SteamEngine.Common;

namespace SteamEngine {
	public class ItemDispidInfo {
		private static List<ItemDispidInfo> array = new List<ItemDispidInfo>();

		public readonly ushort id;
		public readonly uint flags;
		public readonly byte weight;
		public readonly byte quality;
		public readonly ushort unknown;
		public readonly byte minItemsToDisplayThisArt;
		public readonly byte quantity;
		public readonly ushort animID;
		public readonly byte unknown2;
		public readonly byte hue;
		public readonly ushort unknown3;
		public readonly byte height;
		public readonly byte calcHeight; //half for bridges
		public readonly string singularName;
		public readonly string pluralName;
		public readonly bool isEmpty;

		public ItemDispidInfo(uint flags, byte weight, byte quality, ushort unknown, byte minItemsToDisplayThisArt, byte quantity, ushort animID, byte unknown2, byte hue, ushort unknown3, byte height, string name) {
			this.flags = flags;
			this.weight = weight;
			this.quality = quality;
			this.unknown = unknown;
			this.minItemsToDisplayThisArt = minItemsToDisplayThisArt;
			this.quantity = quantity;
			this.animID = animID;
			this.unknown2 = unknown2;
			this.hue = hue;
			this.unknown3 = unknown3;
			this.height = height;
			if ((flags & TileData.flag_bridge) == TileData.flag_bridge) {
				this.calcHeight = (byte) (height / 2);
			} else {
				this.calcHeight = height;
			}

			ParseName(name, out this.singularName, out this.pluralName);
			this.singularName = String.Intern(this.singularName);
			this.pluralName = String.Intern(this.pluralName);

			this.id = (ushort) array.Count;
			array.Add(this);
			this.isEmpty = ((flags == 0 || flags == TileData.flag_unknown_2) && (weight == 1 || weight == 0 || weight == 255) &&
						quality == 0 && unknown == 0 && minItemsToDisplayThisArt == 0 && quantity == 0 && animID == 0 &&
						unknown2 == 0 && hue == 0 && unknown3 == 0 && name.Length == 0);
			//height is sometimes not 0 for these.
		}

		public override bool Equals(object obj) {
			if (obj is ItemDispidInfo) {
				ItemDispidInfo idi = (ItemDispidInfo) obj;
				return (flags == idi.flags && weight == idi.weight && quality == idi.quality && unknown == idi.unknown &&
						minItemsToDisplayThisArt == idi.minItemsToDisplayThisArt && quantity == idi.quantity &&
						animID == idi.animID && unknown2 == idi.unknown2 && hue == idi.hue && unknown3 == idi.unknown3 &&
						height == idi.height && singularName == idi.singularName);
			}
			return false;
		}

		//This method exists to suppress stupid warnings (We have Equals for dupeitem detection in TileData).
		public override int GetHashCode() {
			throw new SEException("ItemDispidInfo cannot return a hash code.");
		}

		public static int Num() {
			return array.Count;
		}

		public static ItemDispidInfo Get(int num) {
			if (num >= 0 && num < array.Count) {
				return array[num];
			} else {
				return null;
			}
		}

		internal static bool ParseName(string name, out string singular, out string plural) {
			int percentPos = name.IndexOf("%");
			if (percentPos == -1) {
				singular = name;
				plural = name;
			} else {
				string before = name.Substring(0, percentPos);
				string singadd = "";
				string pluradd = "";
				int percentPos2 = name.IndexOf("%", percentPos + 1);
				int slashPos = name.IndexOf("/", percentPos + 1);
				string after = "";
				if (percentPos2 == -1) {	//This is sometimes the case in the tiledata info...
					pluradd = name.Substring(percentPos + 1);
				} else if (slashPos == -1 || slashPos > percentPos2) {
					if (percentPos2 == name.Length - 1) {
						after = "";
					} else {
						after = name.Substring(percentPos2 + 1);
					}
					pluradd = name.Substring(percentPos + 1, percentPos2 - percentPos - 1);
				} else { //This is: if (slashPos<percentPos2) {
					Sanity.IfTrueThrow(!(slashPos < percentPos2), "Expected that this else would mean slashPos<percentPos2, but it is not the case now. slashPos=" + slashPos + " percentPos2=" + percentPos2);
					if (slashPos == name.Length - 1) {
						after = "";
					} else {
						after = name.Substring(slashPos + 1);
					}
					pluradd = name.Substring(percentPos + 1, slashPos - percentPos - 1);
					singadd = name.Substring(slashPos + 1, percentPos2 - slashPos - 1);
				}
				singular = before + singadd + after;
				plural = before + pluradd + after;
				return true;
			}
			return false;
		}
	}

}