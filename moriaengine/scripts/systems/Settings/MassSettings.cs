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
using System.Reflection;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts.Dialogs {
	public interface IMassSettings {
		string Name { get; }
		int Count { get; }
		IDataFieldView GetFieldView(int index);
	}


	public abstract class MassSettings_ByClass<DefType> : SettingsMetaCategory, IMassSettings where DefType : AbstractDef {
		static protected List<DefType> defs;

		static MassSettings_ByClass() {
			if (defs == null) {
				defs = new List<DefType>();
				foreach (AbstractScript scp in AbstractScript.AllScripts) {
					DefType def = scp as DefType;
					if (def != null) {
						defs.Add(def);
					}
				}
				if (defs.Count == 0) {
					throw new SEException("ListMassSettingsByClass instantiated before scripts are loaded... or no " + typeof(DefType).Name + " in scripts?");
				}

				defs.Sort(delegate(DefType a, DefType b) {
					return string.CompareOrdinal(a.Defname, b.Defname);
				});
			}
		}

		public int Count {
			get {
				return defs.Count;
			}
		}

		public abstract string Name { get; }

		public abstract IDataFieldView GetFieldView(int index);
	}

	public abstract class MassSettings_ByClass_List<DefType> : MassSettings_ByClass<DefType> where DefType : AbstractDef {
		protected class FieldView_ByClass_List : ReadOnlyDataFieldView {
			protected int index;

			internal protected FieldView_ByClass_List(int index) {
				this.index = index;
			}

			public override string GetName(object target) {
				return defs[index].ToString();
			}

			public override Type FieldType {
				get {
					return typeof(DefType);
				}
			}

			public override object GetValue(object target) {
				return defs[this.index];
			}

			public override string GetStringValue(object target) {
				return this.GetName(target);
			}
		}

		public override IDataFieldView GetFieldView(int index) {
			return new FieldView_ByClass_List(index);
		}
	}

	public abstract class MassSettings_ByClass_SingleField<DefType, FieldType> : MassSettings_ByClass<DefType>, IMassSettings where DefType : AbstractDef {
		protected abstract class FieldView_ByClass_SingleField : ReadWriteDataFieldView {
			protected int index;

			protected FieldView_ByClass_SingleField(int index) {
				this.index = index;
			}

			public override string GetName(object target) {
				return defs[index].ToString();
			}

			public override Type FieldType {
				get {
					return typeof(FieldType);
				}
			}

			public override object GetValue(object target) {
				return this.GetValue(defs[index]);
			}

			public override void SetValue(object target, object value) {
				this.SetValue(defs[index], (FieldType) value);
			}

			public override string GetStringValue(object target) {
				return ObjectSaver.Save(this.GetValue(target));
			}

			public override void SetStringValue(object target, string value) {
				this.SetValue(target, ObjectSaver.Load(value));
			}

			internal abstract void SetValue(DefType def, FieldType value);

			internal abstract FieldType GetValue(DefType def);
		}
	}

	public abstract class MassSettings_ByClass_ThingDef<DefType, FieldType> : MassSettings_ByClass_SingleField<DefType, FieldType> where DefType : ThingDef {
		static MassSettings_ByClass_ThingDef() {
			defs.Sort(delegate(DefType a, DefType b) {
				return Comparer<int>.Default.Compare(a.Model, b.Model);
			});
		}

		protected abstract class FieldView_ByClass_ThingDef : FieldView_ByClass_SingleField {
			protected FieldView_ByClass_ThingDef(int index) : base(index) {
			}

			public override string GetName(object target) {
				return defs[index].Name;
			}
		}
	}

	public abstract class MassSettings_ByModel<DefType, FieldType> : SettingsMetaCategory, IMassSettings where DefType : ThingDef {
		static ushort[] models;
		List<DefType>[] defs;

		static MassSettings_ByModel() {
			if (models == null) {
				HashSet<int> modelsSet = new HashSet<int>();
				foreach (AbstractScript scp in AbstractScript.AllScripts) {
					DefType weap = scp as DefType;
					if (weap != null) {
						modelsSet.Add(weap.Model);
					}
				}
				if (modelsSet.Count == 0) {
					throw new SEException("WeaponMassSetting instantiated before scripts are loaded... or no " + typeof(DefType).Name + " in scripts?");
				}

				models = new ushort[modelsSet.Count];
				int i = 0;
				foreach (ushort model in modelsSet) {
					models[i] = model;
					i++;
				}
				Array.Sort(models);
			}
		}

		private void InitDefList() {
			if (defs == null) {
				int n = models.Length;
				defs = new List<DefType>[n];
				for (int i = 0; i < n; i++) {
					ushort model = models[i];
					List<DefType> list = new List<DefType>();
					defs[i] = list;
					foreach (AbstractScript scp in AbstractScript.AllScripts) {
						DefType def = scp as DefType;
						if ((def != null) &&
								(def.Model == model) &&
								CheckIfAppies(def)) {
							list.Add(def);
						}
					}
					if (list.Count == 0) {
						throw new SEException("Def for model " + model + " not found for mass setting");
					}
					foreach (DefType def in list) {
						if (def.Defname.StartsWith("i_0x") || def.Defname.StartsWith("c_0x")) {
							list.Remove(def);
							list.Insert(0, def);
							break;
						}
					}
				}
			}
		}

		public abstract string Name { get; }

		public int Count {
			get {
				InitDefList();
				return models.Length;
			}
		}

		internal virtual bool CheckIfAppies(DefType def) {
			return true;
		}

		protected abstract class FieldView_ByModel : ReadWriteDataFieldView {
			protected int index;

			protected FieldView_ByModel(int index) {
				this.index = index;

			}

			public override string GetName(object target) {
				DefType def = ((MassSettings_ByModel<DefType, FieldType>) target).defs[index][0];
				return def.Name;
			}

			public override Type FieldType {
				get {
					return typeof(FieldType);
				}
			}

			public override object GetValue(object target) {
				//set the fields on all defs to the same value.
				List<DefType> defs = ((MassSettings_ByModel<DefType, FieldType>) target).defs[index];
				FieldType value = this.GetValue(defs[0]);

				foreach (DefType def in defs) {
					this.SetValue(def, (FieldType) value);
				}

				return value;
			}

			public override void SetValue(object target, object value) {
				List<DefType> defs = ((MassSettings_ByModel<DefType, FieldType>) target).defs[index];

				foreach (DefType def in defs) {
					this.SetValue(def, (FieldType) value);
				}
			}

			public override string GetStringValue(object target) {
				return ObjectSaver.Save(this.GetValue(target));
			}

			public override void SetStringValue(object target, string value) {
				this.SetValue(target, ObjectSaver.Load(value));
			}

			internal abstract void SetValue(DefType def, FieldType value);

			internal abstract FieldType GetValue(DefType def);
		}

		public abstract IDataFieldView GetFieldView(int index);
	}

	public class MassSettingsView : IDataView {

		public Type HandledType {
			get {
				return typeof(IMassSettings);
			}
		}

		public bool HandleSubclasses {
			get {
				return true;
			}
		}

		public string GetName(object instance) {
			return ((IMassSettings) instance).Name;
		}

		public int GetActionButtonsCount(object instance) {
			return 0;
		}

		public int GetFieldsCount(object instance) {
			return ((IMassSettings) instance).Count;
		}

		public System.Collections.Generic.IEnumerable<IDataFieldView> GetDataFieldsPage(int firstLineIndex, object target) {
			IMassSettings holder = (IMassSettings) target;
			for (int i = firstLineIndex, n = holder.Count; i < n; i++) {
				yield return holder.GetFieldView(i);
			}
		}

		public System.Collections.Generic.IEnumerable<ButtonDataFieldView> GetActionButtonsPage(int firstLineIndex, object target) {
			yield break;
		}

	}


	public abstract class MassSettingByMaterial<DefType, FieldType> : MassSettings_ByModel<DefType, FieldType> where DefType : ThingDef, IObjectWithMaterial {
		public Material material;

		internal override bool CheckIfAppies(DefType def) {
			return ((IObjectWithMaterial) def).Material == this.material;
		}
	}

	public class MetaMassSetting<SettingType, DefType, FieldType> : IMassSettings
		where SettingType : MassSettingByMaterial<DefType, FieldType>, new()
		where DefType : ThingDef, IObjectWithMaterial {
		public SettingType[] settings = new SettingType[7];

		public MetaMassSetting() {
			for (int i = 0, n = settings.Length; i < n; i++) {
				settings[i] = new SettingType();
			}

			settings[0].material = Material.Copper;
			settings[1].material = Material.Iron;
			settings[2].material = Material.Verite;
			settings[3].material = Material.Valorite;
			settings[4].material = Material.Obsidian;
			settings[5].material = Material.Adamantinum;
			settings[6].material = Material.Mithril;
		}

		public string Name {
			get { return "Skupiny podle materi�lu"; }
		}

		public int Count {
			get { return 7; }
		}

		public IDataFieldView GetFieldView(int index) {
			return new FieldInfo(index);
		}

		class FieldInfo : ReadOnlyDataFieldView {
			int index;

			internal FieldInfo(int index) {
				this.index = index;
			}

			public override string GetName(object target) {
				return ((MetaMassSetting<SettingType, DefType, FieldType>) target).settings[index].material.ToString();
			}

			public override Type FieldType {
				get { return typeof(MetaMassSetting<,,>); }
			}

			public override object GetValue(object target) {
				return ((MetaMassSetting<SettingType, DefType, FieldType>) target).settings[index];
			}

			public override string GetStringValue(object target) {
				return ObjectSaver.Save(GetValue(target));
			}
		}
	}

	public abstract class MassSettings_ByWearableTypeAndMaterial<DefType, FieldType> : IMassSettings where DefType : WearableDef, IObjectWithMaterial {
		static List<DefType>[,] defSets;

		//Copper=1, Spruce=1,
		//Iron=2, Chestnut=2,
		//Silver=3,
		//Gold=4,
		//Verite=5, Oak=5,
		//Valorite=6, Teak=6,
		//Obsidian=7, Mahagon=7,
		//Adamantinum=8, Eben=8,
		//Mithril=9, Elven=9,

		//Studded = 2,
		//Bone = 3,
		//Chain = 4,
		//Ring = 5,
		//Plate = 6

		private static void InitLists() {
			if (defSets == null) {
				defSets = new List<DefType>[5, 9];
				for (int i = 0; i < 5; i++) {
					for (int j = 0; j < 9; j++) {
						defSets[i, j] = new List<DefType>();
					}
				}
				foreach (AbstractScript scp in AbstractScript.AllScripts) {
					DefType def = scp as DefType;
					if (def != null) {
						Material mat = def.Material;
						WearableType type = def.WearableType;
						if (((mat >= Material.Copper) && (mat <= Material.Mithril)) &&
							((type >= WearableType.Studded) && (type <= WearableType.Plate))) {

							defSets[(int) (type - 2), (int) (mat - 1)].Add(def);
						}
					}
				}
			}
		}

		public abstract string Name { get; }

		public int Count {
			get {
				InitLists();
				return defSets.Length;
			}
		}

		protected abstract class FieldView_ByWearableTypeAndMaterial : ReadWriteDataFieldView {
			protected int firstIndex;
			protected int secondIndex;
			protected Material mat;
			protected WearableType type;

			protected FieldView_ByWearableTypeAndMaterial(int index) {
				firstIndex = index / 9;
				secondIndex = index % 9;
				type = (WearableType) (firstIndex + 2);
				mat = (Material) (secondIndex + 1);
			}

			public override string GetName(object target) {
				return type + ", " + mat;
			}

			public override Type FieldType {
				get {
					return typeof(FieldType);
				}
			}

			public override object GetValue(object target) {
				//set the fields on all defs to the same value.
				List<DefType> defs = defSets[firstIndex, secondIndex];
				FieldType value = this.GetValue(defs[0]);

				foreach (DefType def in defs) {
					this.SetValue(def, (FieldType) value);
				}

				return value;
			}

			public override void SetValue(object target, object value) {
				List<DefType> defs = defSets[firstIndex, secondIndex];

				foreach (DefType def in defs) {
					this.SetValue(def, (FieldType) value);
				}
			}

			public override string GetStringValue(object target) {
				return ObjectSaver.Save(this.GetValue(target));
			}

			public override void SetStringValue(object target, string value) {
				this.SetValue(target, ObjectSaver.Load(value));
			}

			internal abstract void SetValue(DefType def, FieldType value);

			internal abstract FieldType GetValue(DefType def);
		}

		public abstract IDataFieldView GetFieldView(int index);
	}

}