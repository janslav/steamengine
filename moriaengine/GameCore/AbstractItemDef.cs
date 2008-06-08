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
		private FieldValue singularName;
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

			type = InitField_Typed("type", null, typeof(TriggerGroup));
			singularName = InitField_Typed("singularName", "", typeof(string));
			pluralName = InitField_Typed("pluralName", "", typeof(string));

			dupeItem = InitField_ThingDef("dupeItem", null, typeof(AbstractItemDef));
			//clilocName = InitField_Typed("clilocName", "0", typeof(uint));
			mountChar = InitField_ThingDef("mountChar", null, typeof(AbstractCharacterDef));
			flippable = InitField_Typed("flippable", false, typeof(bool));
			stackable = InitField_Typed("stackable", false, typeof(bool));

			dropSound = InitField_Typed("dropSound", 87, typeof(ushort));
		}

		public AbstractItemDef DupeItem {
			get {
				return (AbstractItemDef) dupeItem.CurrentValue;
			} 
			set {
				AbstractItemDef di = (AbstractItemDef) dupeItem.CurrentValue;
				if (di!=null) {
					di.RemoveFromDupeList(this);
				}
				dupeItem.CurrentValue=value;
				if (value!=null) {
					value.AddToDupeList(this);
				}
			}
		}
		
		public void AddToDupeList(AbstractItemDef idef) {
			if (dupeList==null) {
				dupeList = new List<AbstractItemDef>();
			}
			if(!dupeList.Contains(idef)) {
				dupeList.Add(idef);
			}
		}
		
		public void RemoveFromDupeList(AbstractItemDef idef) {
			Sanity.IfTrueThrow(dupeList==null,"RemoveFromDupeList called on an itemdef without a dupelist ("+this+").");
			Sanity.IfTrueThrow(!dupeList.Contains(idef),"In RemoveFromDupeList, Itemdef "+idef+" is not in "+this+"'s dupeList!");
			dupeList.Remove(idef);
			if (dupeList.Count==0) {
				dupeList=null;
			}
		}

		public List<AbstractItemDef> DupeList() {
			return dupeList;
		}
		
		public ushort GetNextFlipModel(ushort curModel) {
			if (curModel==Model) {
				if (dupeList!=null) {
					AbstractItemDef dup = dupeList[0];
					return dup.Model;
				}
			} else {
				if (dupeList!=null) {
					int cur=-1;
					for (int a=0; a<dupeList.Count; a++) {
						AbstractItemDef dup = dupeList[0];
						if (dup.Model==curModel) {
							cur=a;
							break;
						}
					}
					if (cur+1<dupeList.Count) {
						AbstractItemDef dup = dupeList[cur+1];
						return dup.Model;
					}
				}
			}
			return Model;
		}
		
		public AbstractCharacterDef MountChar {
			get {
				return (AbstractCharacterDef) mountChar.CurrentValue;
			} set {
				mountChar.CurrentValue=value;
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
				type.CurrentValue=value;
			}
		}
		
		public string SingularName {
			get {
				string retVal = (string) singularName.CurrentValue;
				if (string.IsNullOrEmpty(retVal)) {
					return Name;
				}
				return retVal;
			} set {
				singularName.CurrentValue=value;
			}
		}
		
		public string PluralName {
			get {
				string retVal = (string) pluralName.CurrentValue;
				if (string.IsNullOrEmpty(retVal)) {
					return Name;
				}
				return retVal;
			} set {
				pluralName.CurrentValue=value;
			}
		}
		
		//public uint ClilocName {
		//    get {
		//        return (uint) clilocName.CurrentValue;
		//    } set {
		//        clilocName.CurrentValue = value;
		//    }
		//}
			
		public bool IsStackable { 
			get {
				return (bool) stackable.CurrentValue;
			} set {
				stackable.CurrentValue = value;
			}
		}

		public bool IsFlippable { 
			get {
				return (bool) flippable.CurrentValue;
			} set {
				flippable.CurrentValue = value;
			}
		}

		public ushort DropSound {
			get {
				return (ushort) dropSound.CurrentValue;
			}
			set {
				dropSound.CurrentValue = value;
			}
		}

	
		private bool ParseName(string name, out string singular, out string plural) {
			int percentPos = name.IndexOf("%");
			if (percentPos==-1) {
				singular = name;
				plural = name;
			} else {
				string before = name.Substring(0, percentPos);
				string singadd = "";
				string pluradd = "";
				int percentPos2 = name.IndexOf("%", percentPos+1);
				int slashPos = name.IndexOf("/", percentPos+1);
				string after = "";
				if (percentPos2==-1) {	//This is sometimes the case in the tiledata info...
					pluradd=name.Substring(percentPos+1);
				} else if (slashPos==-1 || slashPos>percentPos2) {
					if (percentPos2==name.Length-1) {
						after = "";
					} else {
						after = name.Substring(percentPos2+1);
					}
					pluradd=name.Substring(percentPos+1, percentPos2-percentPos-1);
				} else { //This is: if (slashPos<percentPos2) {
					Sanity.IfTrueThrow(!(slashPos<percentPos2), "Expected that this else would mean slashPos<percentPos2, but it is not the case now. slashPos="+slashPos+" percentPos2="+percentPos2);
					if (slashPos==name.Length-1) {
						after = "";
					} else {
						after = name.Substring(slashPos+1);
					}
					pluradd=name.Substring(percentPos+1, slashPos-percentPos-1);
					singadd=name.Substring(slashPos+1, percentPos2-slashPos-1);
				}
				singular = before+singadd+after;
				plural = before+pluradd+after;
				return true;
			}
			return false;
		}
		
		public override string Name { 
			get { 
				string n = (string) base.Name;
				if ((n == null) || (n.Length == 0)) {
					ItemDispidInfo idi = this.DispidInfo;
					if (idi != null) {
						return idi.name;
					}
				}
				return n;
			}
			set {
				base.Name=value;
			 	string singular;
			 	string plural;
				ParseName(this.Name, out singular, out plural);
				SingularName = singular;
				PluralName = plural;
			}
		}
		
		public override sealed bool IsItemDef { get {
			return true;
		} }
		
		public override sealed bool IsCharDef { get {
			return false;
		} }

		public MultiData MultiData { get {
			return multiData;
		} }

		public ItemDispidInfo DispidInfo {
			get {
				if (dispidInfo == null) {
					dispidInfo = ItemDispidInfo.Get(this.Model);
				}
				return dispidInfo;
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

			switch(param) {
			 	case "dupelist":
			 		//Do nothing, for now.
			 		break;
			 	case "name":
					System.Text.RegularExpressions.Match m = TagMath.stringRE.Match(args);
					if (m.Success) {
						args = m.Groups["value"].Value;
					}

			 		string singular;
			 		string plural;
					if (ParseName(args, out singular, out plural)) {
						singularName.SetFromScripts(filename, line, "\""+singular+"\"");
						pluralName.SetFromScripts(filename, line, "\""+plural+"\"");
						base.LoadScriptLine(filename, line, param, "\""+singular+"\"");//will normally load name
					} else {
						base.LoadScriptLine(filename, line, param, "\""+args+"\"");//will normally load name
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
	}
}
