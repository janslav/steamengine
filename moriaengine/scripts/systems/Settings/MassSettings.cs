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
		ReadWriteDataFieldView GetFieldView(int index);
	}

	public abstract class MassSettingsByModel<DefType, FieldType> : IMassSettings where DefType : ThingDef {
		ushort[] models;
		DefType[] defs;

		internal MassSettingsByModel(ushort[] models) {
			this.models = models;
		}

		private void InitDefList() {
			if (defs == null) {
				int n = models.Length;
				defs = new DefType[n];
				for (int i = 0; i<n; i++) {
					ushort model = models[i];
					foreach (AbstractScript scp in AbstractScript.AllScrips) {
						DefType def = scp as DefType;
						if ((def != null) && 
								(def.Model == model) &&
								CheckIfAppies(def)) {
							defs[i] = def;
							break;
						}
					}
					if (defs[i] == null) {
						throw new Exception("Def for model "+model+" not found for mass setting");
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

		protected abstract class FieldView : ReadWriteDataFieldView {
			protected int index;

			protected FieldView(int index) {
				this.index = index;
			}

			public override Type FieldType {
				get {
					return typeof(FieldType);
				}
			}

			public override object GetValue(object target) {
				DefType def = ((MassSettingsByModel<DefType, FieldType>) target).defs[index];

				return this.GetValue(def);
			}

			public override void SetValue(object target, object value) {
				DefType def = ((MassSettingsByModel<DefType, FieldType>) target).defs[index];

				this.SetValue(def, (FieldType) ConvertTools.ConvertTo(typeof(FieldType), value));
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

		public abstract ReadWriteDataFieldView GetFieldView(int index);
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
			throw new Exception("The method or operation is not implemented.");
		}

		public System.Collections.Generic.IEnumerable<IDataFieldView> GetDataFieldsPage(int firstLineIndex, object target) {
			IMassSettings holder = (IMassSettings) target;
			for (int i = firstLineIndex, n = holder.Count; i<n; i++) {
				yield return holder.GetFieldView(i);
			}
		}

		public System.Collections.Generic.IEnumerable<ButtonDataFieldView> GetActionButtonsPage(int firstLineIndex, object target) {
			yield break;
		}

	}
}