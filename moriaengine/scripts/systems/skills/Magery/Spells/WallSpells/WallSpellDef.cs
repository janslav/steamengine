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
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using SteamEngine.Common;
using SteamEngine.Regions;
using SteamEngine.CompiledScripts;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	public enum WallDirection {
		None = 0,
		WestEast = 1, EastWest = WestEast,
		NorthSouth = 2, SouthNorth = NorthSouth
	}

	public interface IWallItem {
		void Init(SpellEffectArgs spellEffectArgs, WallDirection wallDir);
	}

	[ViewableClass]
	public class WallSpellDef : DurableSpellDef {

		private FieldValue wallItemDef;

		public WallSpellDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {

			this.wallItemDef = this.InitField_Typed("wallItemDef", null, typeof(ItemDef));
		}

		public ItemDef WallItemDef {
			get {
				return (ItemDef) this.wallItemDef.CurrentValue;
			}
			set {
				this.wallItemDef.CurrentValue = value;
			}
		}

		protected override void On_EffectGround(IPoint4D target, SpellEffectArgs spellEffectArgs) {
			base.On_EffectGround(target, spellEffectArgs);

			int targetX = target.X;
			int targetY = target.Y;
			int targetZ = target.Z;
			Map map = spellEffectArgs.Caster.GetMap();

			int dx = (spellEffectArgs.Caster.X - targetX);
			int dy = (spellEffectArgs.Caster.Y - targetY);

			int ax = Math.Abs(dx);
			int ay = Math.Abs(dy);

			WallDirection dir = WallDirection.NorthSouth;
			ItemDef wallDef = this.WallItemDef;
			if (ay > ax) {
				InitWallItem(spellEffectArgs, wallDef, targetX - 2, targetY, targetZ, map, dir);
				InitWallItem(spellEffectArgs, wallDef, targetX - 1, targetY, targetZ, map, dir);
				InitWallItem(spellEffectArgs, wallDef, targetX + 1, targetY, targetZ, map, dir);
				InitWallItem(spellEffectArgs, wallDef, targetX + 2, targetY, targetZ, map, dir);
			} else {
				dir = WallDirection.WestEast;
				InitWallItem(spellEffectArgs, wallDef, targetX, targetY - 2, targetZ, map, dir);
				InitWallItem(spellEffectArgs, wallDef, targetX, targetY - 1, targetZ, map, dir);
				InitWallItem(spellEffectArgs, wallDef, targetX, targetY + 1, targetZ, map, dir);
				InitWallItem(spellEffectArgs, wallDef, targetX, targetY + 2, targetZ, map, dir);
			}
			InitWallItem(spellEffectArgs, wallDef, targetX, targetY, targetZ, map, dir);
		}

		private static void InitWallItem(SpellEffectArgs spellEffectArgs, ItemDef wallDef, int x, int y, int z, Map map, WallDirection wallDir) {
			if (AdjustField(x, y, ref z, map, wallDef.Height, true)) {
				Thing t = wallDef.Create((ushort) x, (ushort) y, (sbyte) z, map.m);
				IWallItem asWallitem = t as IWallItem;
				if (asWallitem != null) {
					asWallitem.Init(spellEffectArgs, wallDir);
				}
			}
		}

		public static bool AdjustField(int x, int y, ref int z, Map map, int height, bool checkCharacters) {
			if (map == null)
				return false;

			for (int offset = 0; offset < 10; offset++) {
				int offsetZ = z - offset;
				if (map.CanFit(x, y, offsetZ, height, true, checkCharacters)) {
					z = offsetZ;
					return true;
				}
			}

			return false;
		}
	}
}