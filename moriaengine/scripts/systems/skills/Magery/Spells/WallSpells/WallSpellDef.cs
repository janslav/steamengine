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

	[ViewableClass]
	public class WallSpellDef : DurableSpellDef {

		private FieldValue wallItemDef;

		public WallSpellDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {

			this.wallItemDef = this.InitField_Typed("wallItemDef", null, typeof(ItemDef));
		}

		protected override void On_EffectGround(IPoint4D target, SpellEffectArgs spellEffectArgs) {
			base.On_EffectGround(target, spellEffectArgs);

			ushort targetX = target.X;
			ushort targetY = target.Y;
			sbyte targetZ = target.Z;
			byte targetM = target.M;

			int dx = (spellEffectArgs.Caster.X - targetX);
			int dy = (spellEffectArgs.Caster.Y - targetY);

			int ax = Math.Abs(dx);
			int ay = Math.Abs(dy);

			if (ay > ax) {
				this.WallItemDef.Create((ushort) (targetX - 2), targetY, targetZ, targetM);
				this.WallItemDef.Create((ushort) (targetX - 1), targetY, targetZ, targetM);
				this.WallItemDef.Create((ushort) (targetX + 1), targetY, targetZ, targetM);
				this.WallItemDef.Create((ushort) (targetX + 2), targetY, targetZ, targetM);
			} else {
				this.WallItemDef.Create(targetX, (ushort) (targetY - 2), targetZ, targetM);
				this.WallItemDef.Create(targetX, (ushort) (targetY - 1), targetZ, targetM);
				this.WallItemDef.Create(targetX, (ushort) (targetY + 1), targetZ, targetM);
				this.WallItemDef.Create(targetX, (ushort) (targetY + 2), targetZ, targetM);
			}
			this.WallItemDef.Create(target);
		}

		public ItemDef WallItemDef {
			get {
				return (ItemDef) this.wallItemDef.CurrentValue;
			}
			set {
				this.wallItemDef.CurrentValue = value;
			}
		}
	}
}