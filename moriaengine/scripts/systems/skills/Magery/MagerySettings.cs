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
using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.Persistence;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {
	[SaveableClass]
	[HasSavedMembers]
	[Dialogs.ViewableClass]
	public class MagerySettings {

		[SavedMember]
		public static MagerySettings instance = new MagerySettings();

		[LoadingInitializer]
		public MagerySettings() {
		}

		[SaveableData]
		public double bareHandsMindPowerVsP = 700;

		[SaveableData]
		public double bareHandsMindPowerVsM = 700;

		public MetaMassSetting<WeaponMindPowerVsMMassSetting, ColoredWeaponDef, double> mindPowerVsM = new MetaMassSetting<WeaponMindPowerVsMMassSetting, ColoredWeaponDef, double>();

		public MetaMassSetting<WeaponMindPowerVsPMassSetting, ColoredWeaponDef, double> mindPowerVsP = new MetaMassSetting<WeaponMindPowerVsPMassSetting, ColoredWeaponDef, double>();
	}

	public class WeaponMindPowerVsPMassSetting : MassSettingByMaterial<ColoredWeaponDef,double> {

		public override string Name {
			get { return "Síla mysli proti hráèùm"; }
		}

		protected class WeaponvVsPFieldView : FieldView {
			internal WeaponvVsPFieldView(int index)
				: base(index) {
			}

			internal override void SetValue(ColoredWeaponDef def, double value) {
				def.MindPowerVsP = value;
			}

			internal override double GetValue(ColoredWeaponDef def) {
				return def.MindPowerVsP;
			}
		}

		public override IDataFieldView GetFieldView(int index) {
			return new WeaponvVsPFieldView(index);
		}
	}

	public class WeaponMindPowerVsMMassSetting : MassSettingByMaterial<ColoredWeaponDef, double> {

		public override string Name {
			get { return "Síla mysli proti monstrùm"; }
		}

		protected class WeaponMindPowerVsMFieldView : FieldView {
			internal WeaponMindPowerVsMFieldView(int index)
				: base(index) {
			}

			internal override void SetValue(ColoredWeaponDef def, double value) {
				def.MindPowerVsM = value;
			}

			internal override double GetValue(ColoredWeaponDef def) {
				return def.MindPowerVsM;
			}
		}

		public override IDataFieldView GetFieldView(int index) {
			return new WeaponMindPowerVsMFieldView(index);
		}
	}
}

