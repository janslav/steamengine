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
		public double weaponSpeedGlobal = 1.000;

		[SaveableData]
		public double weaponSpeedNPC = 0.150;

		[SaveableData]
		public double attackStrModifier = 3.170;

		[SaveableData]
		public double weapAttack = 1.100; //nastaveni_global_UC

		[SaveableData]
		public double weapAttackM = 1.000; //nastaveni_global_MUC

		[SaveableData]
		public double weapAttackVsM = 1.600; //nastaveni_global_vMweapattack

		[SaveableData]
		public double weapAttackPvP = 0.900; //nastaveni_global_PvPweapattack

		[SaveableData]
		public double armorClassP = 0.700; //nastaveni_global_Parmor

		[SaveableData]
		public double armorClassM = 1.000; //nastaveni_global_Marmor

		[SaveableData]
		public double armorClassMvP = 5.000; //1000/nastaveni_global_MvP_armorfactor

		[SaveableData]
		[Remark("Every case other than (M is attacker, P is defender)")]
		public double armorRandEffectMin = 0.945; //skill_parrying.effect

		[SaveableData]
		[Remark("Every case other than (M is attacker, P is defender)")]
		public double armorRandEffectMax = 1.050; //skill_parrying.effect

		[SaveableData]
		public double swingDamagePvP = 0.900; //nastaveni_global_PvPweapdam

		[SaveableData]
		public double swingDamageVsM = 1.350; //nastaveni_global_vMweapdam

		[SaveableData]
		public double swingDamageM = 1.0; //nastaveni_global_Mvweapdam

		[SaveableData]
		public double swingDamageRandMvPMin = 0.850; //nastaveni_global_MvP_randomfactor

		[SaveableData]
		public double swingDamageRandMvPMax = 1.000; //nastaveni_global_MvP_randomfactor		

		public WeaponTypeMassSetting weaponTypes = new WeaponTypeMassSetting();

		public WeaponSpeedMassSetting weaponSpeeds = new WeaponSpeedMassSetting();

		public WeaponAnimTypeSetting weaponAnims = new WeaponAnimTypeSetting();

		public WeaponMaterialTypeMassSetting weaponMaterialTypes = new WeaponMaterialTypeMassSetting();
	}

	public abstract class WeaponMassSettingByModel<DefType, FieldType> : MassSettingsByModel<DefType, FieldType> where DefType : WeaponDef {
		private static ushort[] weaponModels;

		private static ushort[] WeaponModels { 
			get {
				if (weaponModels == null) {
					HashSet<ushort> models = new HashSet<ushort>();
					foreach (AbstractScript scp in AbstractScript.AllScrips) {
						DefType weap = scp as DefType;
						if (weap != null) {
							models.Add(weap.Model);
						}
					}
					if (models.Count == 0) {
						throw new Exception("WeaponMassSetting instantiated before scripts are loaded... or no weapons in scripts?");
					}

					weaponModels = new ushort[models.Count];
					int i = 0;
					foreach (ushort model in models) {
						weaponModels[i] = model;
						i++;
					}
					Array.Sort(weaponModels);
				}
				return weaponModels;
			}
		}

		public WeaponMassSettingByModel()
			: base(WeaponModels) {
		}
	}

	public class WeaponTypeMassSetting : WeaponMassSettingByModel<WeaponDef,WeaponType> {
		public override string Name {
			get { 
				return "Typy zbran�";
			}
		}

		protected class WeaponTypeFieldView : FieldView {
			internal WeaponTypeFieldView(int index)
				: base(index) {
			}

			internal override void SetValue(WeaponDef def, WeaponType value) {
				def.WeaponType = value;
				switch (value) {
					case WeaponType.TwoHandBlunt:
					case WeaponType.TwoHandSpike:
					case WeaponType.TwoHandSword:
					case WeaponType.TwoHandAxe:
					case WeaponType.XBowRunning:
					case WeaponType.XBowStand:
					case WeaponType.BowRunning:
					case WeaponType.BowStand:
						def.Layer = 2;
						def.TwoHanded = true;
						break;
					case WeaponType.OneHandBlunt:
					case WeaponType.OneHandSpike:
					case WeaponType.OneHandSword:
					case WeaponType.OneHandAxe:
						def.Layer = 1;
						def.TwoHanded = false;
						break;
				}
			}

			internal override WeaponType GetValue(WeaponDef def) {
				return def.WeaponType;
			}
		}

		public override ReadWriteDataFieldView GetFieldView(int index) {
			return new WeaponTypeFieldView(index);
		}
	}

	public class WeaponSpeedMassSetting : WeaponMassSettingByModel<WeaponDef, double> {
		public override string Name {
			get {
				return "Rychlosti zbran�";
			}
		}

		protected class WeaponSpeedFieldView : FieldView {
			internal WeaponSpeedFieldView(int index)
				: base(index) {
			}

			internal override void SetValue(WeaponDef def, double value) {
				def.Speed = value;
			}

			internal override double GetValue(WeaponDef def) {
				return def.Speed;
			}
		}

		public override ReadWriteDataFieldView GetFieldView(int index) {
			return new WeaponSpeedFieldView(index);
		}
	}

	public class WeaponAnimTypeSetting : WeaponMassSettingByModel<WeaponDef, WeaponAnimType> {
		public override string Name {
			get {
				return "Typy animace zbran�";
			}
		}

		protected class WeaponAnimTypeFieldView : FieldView {
			internal WeaponAnimTypeFieldView(int index)
				: base(index) {
			}

			internal override void SetValue(WeaponDef def, WeaponAnimType value) {
				def.WeaponAnimType = value;
			}

			internal override WeaponAnimType GetValue(WeaponDef def) {
				WeaponAnimType anim = def.WeaponAnimType;
				if (anim == WeaponAnimType.Undefined) {
					anim = TranslateAnimType(def.WeaponType);
				}
				return anim;
			}
		}

		public static WeaponAnimType TranslateAnimType(WeaponType type) {
			WeaponAnimType anim = WeaponAnimType.Undefined;
			switch (type) {
				case WeaponType.BareHands:
					anim = WeaponAnimType.BareHands;
					break;
				case WeaponType.XBowRunning:
				case WeaponType.XBowStand:
					anim = WeaponAnimType.XBow;
					break;
				case WeaponType.BowRunning:
				case WeaponType.BowStand:
					anim = WeaponAnimType.Bow;
					break;
				case WeaponType.OneHandAxe:
				case WeaponType.OneHandSpike:
				case WeaponType.OneHandSword:
				case WeaponType.OneHandBlunt:
					anim = WeaponAnimType.HeldInRightHand;
					break;
				case WeaponType.TwoHandSword:
				case WeaponType.TwoHandBlunt:
				case WeaponType.TwoHandSpike:
				case WeaponType.TwoHandAxe:
					anim = WeaponAnimType.HeldInLeftHand;
					break;
			}
			return anim;
		}

		public override ReadWriteDataFieldView GetFieldView(int index) {
			return new WeaponAnimTypeFieldView(index);
		}
	}

	public class WeaponMaterialTypeMassSetting : WeaponMassSettingByModel<ColoredWeaponDef, MaterialType> {
		public override string Name {
			get {
				return "Typ materi�lu zbran�";
			}
		}

		protected class WeaponMaterialTypeFieldView : FieldView {
			internal WeaponMaterialTypeFieldView(int index)
				: base(index) {
			}

			internal override void SetValue(ColoredWeaponDef def, MaterialType value) {
				def.MaterialType = value;
			}

			internal override MaterialType GetValue(ColoredWeaponDef def) {
				return def.MaterialType;
			}
		}

		public override ReadWriteDataFieldView GetFieldView(int index) {
			return new WeaponMaterialTypeFieldView(index);
		}
	}
}

