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
using System.Globalization;
using SteamEngine.Common;

namespace SteamEngine {
	public abstract class AbstractItemDef : ThingDef {
		private FieldValue type;
		private FieldValue pluralName;

		private FieldValue dupeItem;
		//private FieldValue clilocName;
		private FieldValue mountChar;
		private FieldValue flippable;
		private FieldValue stackable;

		private FieldValue dropSound;

		private List<AbstractItemDef> dupeList = null;

		private ItemDispidInfo dispidInfo;

		public AbstractItemDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {

			this.type = InitTypedField("type", null, typeof(TriggerGroup));
			this.pluralName = InitTypedField("pluralName", "", typeof(string));

			this.dupeItem = InitThingDefField("dupeItem", null, typeof(AbstractItemDef));
			//clilocName = InitField_Typed("clilocName", "0", typeof(uint));
			this.mountChar = InitThingDefField("mountChar", null, typeof(AbstractCharacterDef));
			this.flippable = InitTypedField("flippable", false, typeof(bool));
			this.stackable = InitTypedField("stackable", false, typeof(bool));

			this.dropSound = InitTypedField("dropSound", 87, typeof(int));
		}

		public AbstractItemDef DupeItem {
			get {
				return (AbstractItemDef) dupeItem.CurrentValue;
			}
			set {
				AbstractItemDef di = (AbstractItemDef) dupeItem.CurrentValue;
				if (di != null) {
					di.RemoveFromDupeList(this);
				}
				dupeItem.CurrentValue = value;
				if (value != null) {
					value.AddToDupeList(this);
				}
			}
		}

		public void AddToDupeList(AbstractItemDef idef) {
			if (dupeList == null) {
				dupeList = new List<AbstractItemDef>();
			}
			if (!dupeList.Contains(idef)) {
				dupeList.Add(idef);
			}
		}

		public void RemoveFromDupeList(AbstractItemDef idef) {
			Sanity.IfTrueThrow(dupeList == null, "RemoveFromDupeList called on an itemdef without a dupelist (" + this + ").");
			Sanity.IfTrueThrow(!dupeList.Contains(idef), "In RemoveFromDupeList, Itemdef " + idef + " is not in " + this + "'s dupeList!");
			dupeList.Remove(idef);
			if (dupeList.Count == 0) {
				dupeList = null;
			}
		}

		public List<AbstractItemDef> DupeList() {
			return dupeList;
		}

		public int GetNextFlipModel(int curModel) {
			if (curModel == Model) {
				if (dupeList != null) {
					AbstractItemDef dup = dupeList[0];
					return dup.Model;
				}
			} else {
				if (dupeList != null) {
					int cur = -1;
					for (int a = 0; a < dupeList.Count; a++) {
						AbstractItemDef dup = dupeList[0];
						if (dup.Model == curModel) {
							cur = a;
							break;
						}
					}
					if (cur + 1 < dupeList.Count) {
						AbstractItemDef dup = dupeList[cur + 1];
						return dup.Model;
					}
				}
			}
			return Model;
		}

		public AbstractCharacterDef MountChar {
			get {
				return (AbstractCharacterDef) mountChar.CurrentValue;
			}
			set {
				mountChar.CurrentValue = value;
			}
		}

		private static TriggerGroup t_normal;
		private static TriggerGroup T_Normal {
			get {
				if (t_normal == null) {
					t_normal = TriggerGroup.Get("t_normal");
				}
				return t_normal;
			}
		}


		public TriggerGroup Type {
			get {
				TriggerGroup tg = (TriggerGroup) type.CurrentValue;
				if (tg == null) {
					return T_Normal;
				}
				return (TriggerGroup) type.CurrentValue;
			}
			set {
				type.CurrentValue = value;
			}
		}

		public bool IsStackable {
			get {
				return (bool) stackable.CurrentValue;
			}
			set {
				stackable.CurrentValue = value;
			}
		}

		public bool IsFlippable {
			get {
				return (bool) flippable.CurrentValue;
			}
			set {
				flippable.CurrentValue = value;
			}
		}

		public int DropSound {
			get {
				return (int) dropSound.CurrentValue;
			}
			set {
				dropSound.CurrentValue = value;
			}
		}

		public override string Name {
			get {
				if (this.name.IsDefaultCodedValue) {
					ItemDispidInfo idi = this.DispidInfo;
					if (idi != null) {
						return idi.SingularName;
					}
				}

				return base.Name;
			}
			set {
				string singular, plural;
				if (ItemDispidInfo.ParseName(value, out singular, out plural)) {
					base.Name = singular;
					this.PluralName = plural;
				} else {
					base.Name = value;
				}
			}
		}

		public string PluralName {
			get {
				if (!this.pluralName.IsDefaultCodedValue) {
					return (string) pluralName.CurrentValue;
				}
				if (this.name.IsDefaultCodedValue) {
					ItemDispidInfo idi = this.DispidInfo;
					if (idi != null) {
						return idi.PluralName;
					}
				}

				return this.Name;
			}
			set {
				pluralName.CurrentValue = value;
			}
		}

		public override sealed bool IsItemDef {
			get {
				return true;
			}
		}

		public override sealed bool IsCharDef {
			get {
				return false;
			}
		}

		public MultiData MultiData {
			get {
				return multiData;
			}
		}

		public ItemDispidInfo DispidInfo {
			get {
				int model = this.Model;
				if ((this.dispidInfo == null) || (this.dispidInfo.Id != model)) {
					this.dispidInfo = ItemDispidInfo.Get(model);
				}
				return this.dispidInfo;
			}
		}

		protected override void LoadScriptLine(string filename, int line, string param, string args) {
			if ("stack".Equals(param)) {
				param = "stackable";
			}
			if ("isstackable".Equals(param)) {
				param = "stackable";
			}
			if ("flip".Equals(param)) {
				param = "flippable";
			}
			if ("isflippable".Equals(param)) {
				param = "flippable";
			}
			//if ("cliloc".Equals(param)) {
			//    param = "clilocname";
			//}

			switch (param) {
				case "dupelist": //Do nothing (for now?)
					break;
				case "name":
					System.Text.RegularExpressions.Match m = TagMath.stringRE.Match(args);
					if (m.Success) {
						args = m.Groups["value"].Value;
					}

					string singular, plural;
					if (ItemDispidInfo.ParseName(args, out singular, out plural)) {
						pluralName.SetFromScripts(filename, line, "\"" + plural + "\"");
						base.LoadScriptLine(filename, line, param, "\"" + singular + "\"");//will normally load name
					} else {
						base.LoadScriptLine(filename, line, param, "\"" + args + "\"");//will normally load name
					}
					break;
				default:
					base.LoadScriptLine(filename, line, param, args);//the AbstractThingDef Loadline
					break;
			}
		}

		public override void Unload() {
			if (dupeList != null) {
				dupeList.Clear();
			}
			base.Unload();
			//other various properties...
			//todo: not clear those tags/tgs/timers/whatever that were set dynamically (ie not in scripted defs)
		}

		public override int Height {
			get {
				if (this.height.IsDefaultCodedValue) {
					//if (this.IsContainer) {
					//    return 4;
					//}
					ItemDispidInfo idi = this.DispidInfo;
					if (idi == null) {
						return 1;
					}
					return idi.CalcHeight;
				} else {
					return (int) this.height.CurrentValue;
				}
			}
		}
	}
}
