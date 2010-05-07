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
	//[SaveableClass]
	//[HasSavedMembers]
	[Dialogs.ViewableClass]
	public class PoisoningSettings : SettingsMetaCategory {

		//[SavedMember]
		public static PoisoningSettings instance = new PoisoningSettings();

		//[LoadingInitializer]
		public PoisoningSettings() {
		}

		public AllPoisonTypesMassSetting poisonTypes = new AllPoisonTypesMassSetting();

		public PoisonTypeTickIntervalMassSetting tickIntervalsPerType = new PoisonTypeTickIntervalMassSetting();
		public PoisonTypMaxTicksMassSetting maxTicksPerType = new PoisonTypMaxTicksMassSetting();
		public PoisonTypMaxPowerMassSetting maxPowerPerType = new PoisonTypMaxPowerMassSetting();


		public AllPoisonPotionsMassSetting poisonPotions = new AllPoisonPotionsMassSetting();

		public PoisonPotionPowerMassSetting powerPerPotion = new PoisonPotionPowerMassSetting();
		public PoisonPotionTickCountMassSetting ticksPerPotion = new PoisonPotionTickCountMassSetting();


		public ProjectilePoisoningDifficultyMassSetting difficultyPerProjectile = new ProjectilePoisoningDifficultyMassSetting();
		public ProjectilePoisoningEfficiencyMassSetting efficiencyPerProjectile = new ProjectilePoisoningEfficiencyMassSetting();

		public WeaponPoisoningDifficultyMassSetting difficultyPerWeaponModel = new WeaponPoisoningDifficultyMassSetting();
		public WeaponPoisoningEfficiencyMassSetting efficiencyPerWeaponModel = new WeaponPoisoningEfficiencyMassSetting();
		public WeaponPoisoningCapacityMassSetting capacityPerWeaponModel = new WeaponPoisoningCapacityMassSetting();

	}


	public class AllPoisonTypesMassSetting : MassSettings_ByClass_List<PoisonEffectPluginDef> {

		public override string Name {
			get { return "V�echny typy jed�"; }
		}
	}

	public class PoisonTypeTickIntervalMassSetting : 
		MassSettings_ByClass_SingleField<PoisonEffectPluginDef, double> {

		public override string Name { get { return "Tick intervaly podle typu jedu v sekund�ch"; } }

		protected class PoisonTypeTickIntervalFieldView : FieldView_ByClass_SingleField {
			internal PoisonTypeTickIntervalFieldView(int index)
				: base(index) {
			}

			internal override void SetValue(PoisonEffectPluginDef def, double value) {
				def.TickInterval = value;
			}

			internal override double GetValue(PoisonEffectPluginDef def) {
				return def.TickInterval;
			}
		}

		public override IDataFieldView GetFieldView(int index) {
			return new PoisonTypeTickIntervalFieldView(index);
		}
	}

	public class PoisonTypMaxTicksMassSetting :
		MassSettings_ByClass_SingleField<PoisonEffectPluginDef, int> {

		public override string Name { get { return "Max po�et tick� podle typu jedu, tj. s��tac� strop"; } }

		protected class MaxTicksFieldView : FieldView_ByClass_SingleField {
			internal MaxTicksFieldView(int index)
				: base(index) {
			}

			internal override void SetValue(PoisonEffectPluginDef def, int value) {
				def.MaxTicks = value;
			}

			internal override int GetValue(PoisonEffectPluginDef def) {
				return def.MaxTicks;
			}
		}

		public override IDataFieldView GetFieldView(int index) {
			return new MaxTicksFieldView(index);
		}
	}

	public class PoisonTypMaxPowerMassSetting :
		MassSettings_ByClass_SingleField<PoisonEffectPluginDef, int> {

		public override string Name { get { return "Max power podle typu jedu, tj. s��tac� strop. Pou�it p�i opakovan� aplikaci jedu na jednoho chud�ka."; } }

		protected class MaxPowerFieldView : FieldView_ByClass_SingleField {
			internal MaxPowerFieldView(int index)
				: base(index) {
			}

			internal override void SetValue(PoisonEffectPluginDef def, int value) {
				def.MaxPower = value;
			}

			internal override int GetValue(PoisonEffectPluginDef def) {
				return def.MaxPower;
			}
		}

		public override IDataFieldView GetFieldView(int index) {
			return new MaxPowerFieldView(index);
		}
	}

	public class AllPoisonPotionsMassSetting : MassSettings_ByClass_List<PoisonPotionDef> {

		public override string Name {
			get { return "V�echny poison potiony"; }
		}
	}

	public class PoisonPotionPowerMassSetting :
		MassSettings_ByClass_SingleField<PoisonPotionDef, int[]> {

		public override string Name { get { return "Power jedovat�ch potion� - dvoj��sl�, p�i pou�it� se bere n�hodn� hodnota v rozmez�, v�etn�."; } }

		protected class PoisonPotionPowerFieldView : FieldView_ByClass_SingleField {
			internal PoisonPotionPowerFieldView(int index)
				: base(index) {
			}

			internal override void SetValue(PoisonPotionDef def, int[] value) {
				def.PoisonPower = value;
			}

			internal override int[] GetValue(PoisonPotionDef def) {
				return def.PoisonPower;
			}
		}

		public override IDataFieldView GetFieldView(int index) {
			return new PoisonPotionPowerFieldView(index);
		}
	}

	public class PoisonPotionTickCountMassSetting :
		MassSettings_ByClass_SingleField<PoisonPotionDef, int> {

		public override string Name { get { return "Po�et tik� jedovat�ch potion�"; } }

		protected class PoisonPotionTickCountFieldView : FieldView_ByClass_SingleField {
			internal PoisonPotionTickCountFieldView(int index)
				: base(index) {
			}

			internal override void SetValue(PoisonPotionDef def, int value) {
				def.PoisonTickCount = value;
			}

			internal override int GetValue(PoisonPotionDef def) {
				return def.PoisonTickCount;
			}
		}

		public override IDataFieldView GetFieldView(int index) {
			return new PoisonPotionTickCountFieldView(index);
		}
	}

	public class ProjectilePoisoningEfficiencyMassSetting :
		MassSettings_ByClass_SingleField<ProjectileDef, double> {

		public override string Name { get { 
			return "Efektivita poisoningu pro jednotliv� druhy projektil�. 1.0 = 100% "; 
		} }

		protected class ProjectilePoisoningEfficiencyFieldView : FieldView_ByClass_SingleField {
			internal ProjectilePoisoningEfficiencyFieldView(int index)
				: base(index) {
			}

			internal override void SetValue(ProjectileDef def, double value) {
				def.PoisoningEfficiency = value;
			}

			internal override double GetValue(ProjectileDef def) {
				return def.PoisoningEfficiency;
			}
		}

		public override IDataFieldView GetFieldView(int index) {
			return new ProjectilePoisoningEfficiencyFieldView(index);
		}
	}

	public class ProjectilePoisoningDifficultyMassSetting :
		MassSettings_ByClass_SingleField<ProjectileDef, int> {

		public override string Name { get { 
			return "Obt�nost poisoningu pro jednotliv� druhy projektil�. 0 = nelze otr�vit, 1000 = 100% "; 
		} }

		protected class ProjectilePoisoningDifficultyFieldView : FieldView_ByClass_SingleField {
			internal ProjectilePoisoningDifficultyFieldView(int index)
				: base(index) {
			}

			internal override void SetValue(ProjectileDef def, int value) {
				def.PoisoningDifficulty = value;
			}

			internal override int GetValue(ProjectileDef def) {
				return def.PoisoningDifficulty;
			}
		}

		public override IDataFieldView GetFieldView(int index) {
			return new ProjectilePoisoningDifficultyFieldView(index);
		}
	}

	public class WeaponPoisoningDifficultyMassSetting : MassSettings_ByModel<WeaponDef, int> {
		public override string Name { get { 
			return "Obt�nost poisoningu pro zbran� dle modelu. 0 = nelze otr�vit, 1000 = 100% "; 
		} }

		protected class WeaponPoisoningDifficultyFieldView : FieldView_ByModel {
			internal WeaponPoisoningDifficultyFieldView(int index)
				: base(index) {
			}

			internal override void SetValue(WeaponDef def, int value) {
				def.PoisoningDifficulty = value;
			}

			internal override int GetValue(WeaponDef def) {
				return def.PoisoningDifficulty;
			}
		}

		public override IDataFieldView GetFieldView(int index) {
			return new WeaponPoisoningDifficultyFieldView(index);
		}
	}

	public class WeaponPoisoningEfficiencyMassSetting : MassSettings_ByModel<WeaponDef, double> {
		public override string Name { get {
			return "Efektivita poisoningu pro zbran� dle modelu. 1.0 = 100% "; 
		} }

		protected class WeaponPoisoningDifficultyFieldView : FieldView_ByModel {
			internal WeaponPoisoningDifficultyFieldView(int index)
				: base(index) {
			}

			internal override void SetValue(WeaponDef def, double value) {
				def.PoisoningEfficiency = value;
			}

			internal override double GetValue(WeaponDef def) {
				return def.PoisoningEfficiency;
			}
		}

		public override IDataFieldView GetFieldView(int index) {
			return new WeaponPoisoningDifficultyFieldView(index);
		}
	}

	public class WeaponPoisoningCapacityMassSetting : MassSettings_ByModel<WeaponDef, int> {
		public override string Name {get {
			return "Poisoning kapacita zbran� - Kolikr�t lze ude�it ne� se jed na zbrani vy�erp� ";
		} }

		protected class WeaponPoisoningDifficultyFieldView : FieldView_ByModel {
			internal WeaponPoisoningDifficultyFieldView(int index)
				: base(index) {
			}

			internal override void SetValue(WeaponDef def, int value) {
				def.PoisonCapacity = value;
			}

			internal override int GetValue(WeaponDef def) {
				return def.PoisonCapacity;
			}
		}

		public override IDataFieldView GetFieldView(int index) {
			return new WeaponPoisoningDifficultyFieldView(index);
		}
	}
}

