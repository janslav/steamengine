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
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	public enum WallDirection {
		None = 0,
		WestEast = 1, EastWest = WestEast,
		NorthSouth = 2, SouthNorth = NorthSouth
	}

	[ViewableClass]
	public class WallSpellDef : DurableSpellDef {

		public static PassiveAbilityDef FieldDurationBonusDef => (PassiveAbilityDef) AbilityDef.GetByDefname("a_field_duration_bonus");

		private readonly FieldValue itemDefWestEast;
		private readonly FieldValue itemDefNorthSouth;

		public WallSpellDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {

			this.itemDefWestEast = this.InitTypedField("itemDefWestEast", null, typeof(ItemDef));
			this.itemDefNorthSouth = this.InitTypedField("itemDefNorthSouth", null, typeof(ItemDef));
		}

		public ItemDef ItemDefWestEast {
			get {
				return (ItemDef) this.itemDefWestEast.CurrentValue;
			}
			set {
				this.itemDefWestEast.CurrentValue = value;
			}
		}

		public ItemDef ItemDefNorthSouth {
			get {
				return (ItemDef) this.itemDefNorthSouth.CurrentValue;
			}
			set {
				this.itemDefNorthSouth.CurrentValue = value;
			}
		}

		protected override void On_EffectGround(IPoint3D target, SpellEffectArgs spellEffectArgs) {
			base.On_EffectGround(target, spellEffectArgs);

			var caster = spellEffectArgs.Caster;
			var targetX = target.X;
			var targetY = target.Y;
			var targetZ = target.Z;
			var map = caster.GetMap();

			var dx = (caster.X - targetX);
			var dy = (caster.Y - targetY);

			var ax = Math.Abs(dx);
			var ay = Math.Abs(dy);

			var spellPower = spellEffectArgs.SpellPower;
			var durationSecs = this.GetDurationForValue(caster.GetSkill(SkillName.Magery));
			//Magery used instead of spellpower, because the power is designed for use for the field effect, not for it's duration
			durationSecs += durationSecs * caster.GetAbility(FieldDurationBonusDef) * FieldDurationBonusDef.EffectPower;
			//applied ability bonus
			var durationSpan = TimeSpan.FromSeconds(durationSecs);

			if (ay > ax) {
				var wallDef = this.ItemDefWestEast;
				InitWallItem(spellEffectArgs, wallDef, spellPower, durationSpan, targetX - 2, targetY, targetZ, map);
				InitWallItem(spellEffectArgs, wallDef, spellPower, durationSpan, targetX - 1, targetY, targetZ, map);
				InitWallItem(spellEffectArgs, wallDef, spellPower, durationSpan, targetX, targetY, targetZ, map);
				InitWallItem(spellEffectArgs, wallDef, spellPower, durationSpan, targetX + 1, targetY, targetZ, map);
				InitWallItem(spellEffectArgs, wallDef, spellPower, durationSpan, targetX + 2, targetY, targetZ, map);
			} else {
				var wallDef = this.ItemDefNorthSouth;
				InitWallItem(spellEffectArgs, wallDef, spellPower, durationSpan, targetX, targetY - 2, targetZ, map);
				InitWallItem(spellEffectArgs, wallDef, spellPower, durationSpan, targetX, targetY - 1, targetZ, map);
				InitWallItem(spellEffectArgs, wallDef, spellPower, durationSpan, targetX, targetY, targetZ, map);
				InitWallItem(spellEffectArgs, wallDef, spellPower, durationSpan, targetX, targetY + 1, targetZ, map);
				InitWallItem(spellEffectArgs, wallDef, spellPower, durationSpan, targetX, targetY + 2, targetZ, map);
			}
		}

		private static void InitWallItem(SpellEffectArgs spellEffectArgs, ItemDef wallDef, int spellPower, TimeSpan duration, int x, int y, int z, Map map) {
			if (SpellEffectItem.CheckPositionForItem(x, y, ref z, map, wallDef.Height, true)) {
				var t = wallDef.Create((ushort) x, (ushort) y, (sbyte) z, map.M);
				var asWallitem = t as SpellEffectItem;
				if (asWallitem != null) {
					asWallitem.Init(spellPower, duration, true);
				}
			}
		}

		//load "itemdef" as both n-s and w-e variants
		protected override void LoadScriptLine(string filename, int line, string param, string args) {
			if (param.Equals("itemDef", StringComparison.OrdinalIgnoreCase)) {
				base.LoadScriptLine(filename, line, "itemdefnorthsouth", args);
				base.LoadScriptLine(filename, line, "itemdefwesteast", args);
			} else {
				base.LoadScriptLine(filename, line, param, args);
			}
		}
	}
}