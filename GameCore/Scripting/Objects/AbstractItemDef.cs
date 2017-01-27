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
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Shielded;
using SteamEngine.Common;
using SteamEngine.UoData;

namespace SteamEngine.Scripting.Objects {
	public abstract class AbstractItemDef : ThingDef {
		private readonly FieldValue type;
		private readonly FieldValue pluralName;

		private readonly FieldValue dupeItem;
		//private readonly FieldValue clilocName;
		private readonly FieldValue mountChar;
		private readonly FieldValue flippable;
		private readonly FieldValue stackable;

		private readonly FieldValue dropSound;

		private readonly ShieldedSeq<AbstractItemDef> dupeList = new ShieldedSeq<AbstractItemDef>();

		protected AbstractItemDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {

			this.type = this.InitTypedField("type", null, typeof(TriggerGroup));
			this.pluralName = this.InitTypedField("pluralName", "", typeof(string));

			this.dupeItem = this.InitThingDefField("dupeItem", null, typeof(AbstractItemDef));
			//clilocName = InitField_Typed("clilocName", "0", typeof(uint));
			this.mountChar = this.InitThingDefField("mountChar", null, typeof(AbstractCharacterDef));
			this.flippable = this.InitTypedField("flippable", false, typeof(bool));
			this.stackable = this.InitTypedField("stackable", false, typeof(bool));

			this.dropSound = this.InitTypedField("dropSound", 87, typeof(int));
		}

		public AbstractItemDef DupeItem {
			get {
				return (AbstractItemDef) this.dupeItem.CurrentValue;
			}
			set {
				SeShield.AssertInTransaction();

				var di = (AbstractItemDef) this.dupeItem.CurrentValue;
				di?.RemoveFromDupeList(this);
				this.dupeItem.CurrentValue = value;
				value?.AddToDupeList(this);
			}
		}

		private void AddToDupeList(AbstractItemDef idef) {
			if (!this.dupeList.Contains(idef)) {
				this.dupeList.Add(idef);
			}
		}

		private void RemoveFromDupeList(AbstractItemDef idef) {
			Sanity.IfTrueThrow(!this.dupeList.Contains(idef), "In RemoveFromDupeList, Itemdef " + idef + " is not in " + this + "'s dupeList!");
			this.dupeList.Remove(idef);
		}

		public IReadOnlyCollection<AbstractItemDef> DupeList {
			get {
				SeShield.AssertInTransaction();
				return this.dupeList.ToList();
			}
		}

		public int GetNextFlipModel(int curModel) {
			SeShield.AssertInTransaction();

			if (curModel == this.Model) {
				if (this.dupeList.Any()) {
					return this.dupeList.First().Model;
				}
			} else {
				var returnNext = false;
				foreach (var dup in this.dupeList) {
					if (returnNext) {
						return dup.Model;
					}

					if (dup.Model == curModel) {
						returnNext = true;
					}
				}
			}
			return this.Model;
		}

		public AbstractCharacterDef MountChar {
			get {
				return (AbstractCharacterDef) this.mountChar.CurrentValue;
			}
			set {
				this.mountChar.CurrentValue = value;
			}
		}

		private static TriggerGroup T_Normal => TriggerGroup.GetByDefname("t_normal");

		public TriggerGroup Type {
			get {
				var tg = (TriggerGroup) this.type.CurrentValue;
				if (tg == null) {
					return T_Normal;
				}
				return (TriggerGroup) this.type.CurrentValue;
			}
			set {
				this.type.CurrentValue = value;
			}
		}

		public bool IsStackable {
			get {
				return (bool) this.stackable.CurrentValue;
			}
			set {
				this.stackable.CurrentValue = value;
			}
		}

		public bool IsFlippable {
			get {
				return (bool) this.flippable.CurrentValue;
			}
			set {
				this.flippable.CurrentValue = value;
			}
		}

		public int DropSound {
			get {
				return (int) this.dropSound.CurrentValue;
			}
			set {
				this.dropSound.CurrentValue = value;
			}
		}

		public override string Name {
			get {
				if (this.name.IsEmptyAndUnchanged) {
					var idi = this.DispidInfo;
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
				if (!this.pluralName.IsEmptyAndUnchanged) {
					return (string) this.pluralName.CurrentValue;
				}
				if (this.name.IsEmptyAndUnchanged) {
					var idi = this.DispidInfo;
					if (idi != null) {
						return idi.PluralName;
					}
				}

				return this.Name;
			}
			set {
				this.pluralName.CurrentValue = value;
			}
		}

		public sealed override bool IsItemDef => true;

		public sealed override bool IsCharDef => false;

		public MultiData MultiData => MultiData.GetByModel(this.Model);

		public ItemDispidInfo DispidInfo => ItemDispidInfo.GetByModel(this.Model);

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
					args = ConvertTools.LoadSimpleQuotedString(args);

					string singular, plural;
					if (ItemDispidInfo.ParseName(args, out singular, out plural)) {
						this.pluralName.SetFromScripts(filename, line, "\"" + plural + "\"");
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
			SeShield.AssertInTransaction();

			this.dupeList.Clear();
			base.Unload();
			//other various properties...
			//todo: not clear those tags/tgs/timers/whatever that were set dynamically (ie not in scripted defs)
		}

		public override int Height {
			get {
				if (this.height.IsEmptyAndUnchanged) {
					//if (this.IsContainer) {
					//    return 4;
					//}
					var idi = this.DispidInfo;
					if (idi == null) {
						return 1;
					}
					return idi.CalcHeight;
				}
				return (int) this.height.CurrentValue;
			}
		}

		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		internal new static void LoadingFinished() {
			//dump number of loaded instances?
			Logger.WriteDebug($"Highest itemdef model #: {HighestItemModel} (0x{HighestItemModel.ToString("x", CultureInfo.InvariantCulture)})");
			Logger.WriteDebug($"Highest chardef model #: {HighestCharModel} (0x{HighestCharModel.ToString("x", CultureInfo.InvariantCulture)})");

			var allScripts = AllScripts;
			var count = allScripts.Count;

			using (StopWatch.StartAndDisplay("Resolving dupelists and multidata...")) {
				var a = 0;
				var countPerCent = count / 200;
				foreach (var td in allScripts) {
					if ((a % countPerCent) == 0) {
						Logger.SetTitle("Resolving dupelists and multidata: " + ((a * 100) / count) + " %");
					}
					var idef = td as AbstractItemDef;
					if (idef != null) {
						try {
							SeShield.InTransaction(() => {
								idef.DupeItem?.AddToDupeList(idef);
							});
						} catch (FatalException) {
							throw;
						} catch (TransException) {
							throw;
						} catch (Exception e) {
							Logger.WriteWarning(e);
						}
					}
					a++;
				}
			}
			Logger.SetTitle("");
		}
	}
}
