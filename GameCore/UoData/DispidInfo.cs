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

using System.Collections.Generic;
using SteamEngine.Common;

namespace SteamEngine.UoData {
	public class ItemDispidInfo {
		private static readonly List<ItemDispidInfo> list = new List<ItemDispidInfo>();

		private readonly string singularName;
		private readonly string pluralName;

		public int Id { get; }

		public TileFlag Flags { get; }

		public byte Weight { get; }

		public byte Quality { get; }

		public int Unknown1 { get; }

		public byte MinItemsToDisplayThisArt { get; }

		public byte Quantity { get; }

		public int AnimId { get; }

		public byte Unknown2 { get; }

		public byte Hue { get; }

		public int Unknown3 { get; }

		public byte Height { get; }

		public byte CalcHeight { get; }

		public string SingularName => this.singularName;

		public string PluralName => this.pluralName;

		public bool IsEmpty { get; }

		internal ItemDispidInfo(TileFlag flags, byte weight, byte quality, int unknown, byte minItemsToDisplayThisArt, byte quantity, int animID, byte unknown2, byte hue, int unknown3, byte height, string name) {
			this.Flags = flags;
			this.Weight = weight;
			this.Quality = quality;
			this.Unknown1 = unknown;
			this.MinItemsToDisplayThisArt = minItemsToDisplayThisArt;
			this.Quantity = quantity;
			this.AnimId = animID;
			this.Unknown2 = unknown2;
			this.Hue = hue;
			this.Unknown3 = unknown3;
			this.Height = height;
			if ((flags & TileFlag.Bridge) == TileFlag.Bridge) {
				this.CalcHeight = (byte) (height / 2);
			} else {
				this.CalcHeight = height;
			}

			ParseName(name, out this.singularName, out this.pluralName);
			this.singularName = string.Intern(this.singularName);
			this.pluralName = string.Intern(this.pluralName);

			this.Id = list.Count;
			list.Add(this);
			this.IsEmpty = ((flags == 0 || flags == TileFlag.Unknown2) && (weight == 1 || weight == 0 || weight == 255) &&
						quality == 0 && unknown == 0 && minItemsToDisplayThisArt == 0 && quantity == 0 && animID == 0 &&
						unknown2 == 0 && hue == 0 && unknown3 == 0 && name.Length == 0);
			//height is sometimes not 0 for these.
		}

		public override bool Equals(object obj) {
			var idi = obj as ItemDispidInfo;
			if (idi != null) {
				return (this.Flags == idi.Flags && this.Weight == idi.Weight && this.Quality == idi.Quality && this.Unknown1 == idi.Unknown1 &&
						this.MinItemsToDisplayThisArt == idi.MinItemsToDisplayThisArt && this.Quantity == idi.Quantity &&
						this.AnimId == idi.AnimId && this.Unknown2 == idi.Unknown2 && this.Hue == idi.Hue && this.Unknown3 == idi.Unknown3 &&
						this.Height == idi.Height && this.singularName == idi.singularName);
			}
			return false;
		}

		//This method exists to suppress stupid warnings (We have Equals for dupeitem detection in TileData).
		public override int GetHashCode() {
			throw new SEException("ItemDispidInfo cannot return a hash code.");
		}

		public static int Count {
			get {
				return list.Count;
			}
		}

		public static ItemDispidInfo GetByModel(int num)
		{
			if (num >= 0 && num < list.Count) {
				return list[num];
			}
			return null;
		}

		internal static bool ParseName(string name, out string singular, out string plural) {
			var percentPos = name.IndexOf("%");
			if (percentPos == -1) {
				singular = name;
				plural = name;
			} else {
				var before = name.Substring(0, percentPos);
				var singadd = "";
				var pluradd = "";
				var percentPos2 = name.IndexOf("%", percentPos + 1);
				var slashPos = name.IndexOf("/", percentPos + 1);
				var after = "";
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