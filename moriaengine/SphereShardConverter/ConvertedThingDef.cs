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
using System.Reflection;
using System.IO;
using System.Globalization;
using SteamEngine.Common;
using System.Configuration;
//
namespace SteamEngine.Converter {
	//	/**
	//	Hardcoded changes to converted scripts:
	//		1) The fishing pole is forced to be twohanded.
	//			(It's detected by checking if an item's layer is 2 and model is 0xdbf. 0xdc0 is a dupeitem of it and so will inherit its twohandedness.)
	//	
	//	*/
	//	
	public class ConvertedThingDef : ConvertedDef {
		PropsLine idLine, defname1, defname2, dupeItemLine;
		//bool ownModel = false;
		bool hasNumericalHeader = false;
		int modelNum = -1;
		protected ConvertedThingDef modelDef;
		//bool isDupeItem = false;

		public Dictionary<string, ConvertedThingDef> byDefname = new Dictionary<string, ConvertedThingDef>(StringComparer.OrdinalIgnoreCase);
		public Dictionary<int, ConvertedThingDef> byModel = new Dictionary<int, ConvertedThingDef>();

		public int Model {
			get {
				if (this.modelNum == -1) {
					if (this.modelDef != null) {
						return this.modelDef.Model;
					}
				} else {
					return this.modelNum;
				}
				Error(this.origData.HeaderLine, "ThingDef " + headerName + " has no model set...?");
				return -1;
			}
		}

		private static LineImplTask[] firstStageImpl = new LineImplTask[] {
//				new LineImplTask("event", new LineImpl(WriteAsTG)), 
//				new LineImplTask("events", new LineImpl(WriteAsTG)), 
//				new LineImplTask("tevent", new LineImpl(WriteAsTG)), 
//				new LineImplTask("tevents", new LineImpl(WriteAsTG)), 
				new LineImplTask("name", new LineImpl(WriteInQuotes))
			};

		public ConvertedThingDef(PropsSection input)
			: base(input) {
			this.firstStageImplementations.Add(firstStageImpl);
		}

		private static void WriteAsTG(ConvertedDef def, PropsLine line) {
			def.Set("triggerGroup", line.Value, line.Comment);
		}



		public override void FirstStage() {
			base.FirstStage();

			this.dupeItemLine = this.origData.TryPopPropsLine("dupeitem");
			if (this.dupeItemLine != null) {
				int headerNum;
				if (ConvertTools.TryParseInt32(headerName, out headerNum)) {
					headerName = "0x" + headerNum.ToString("x");
					this.modelNum = headerNum;
				}
			} else {
				bool needsHeader = false;
				this.idLine = this.origData.TryPopPropsLine("id");

				int headerNum;
				if (ConvertTools.TryParseInt32(headerName, out headerNum)) {
					if (this.idLine != null) {//it does not mean model...
						needsHeader = true;
					} else {
						this.hasNumericalHeader = true;
						//ownModel = true;
						this.modelNum = headerNum;
						this.byModel[headerNum] = this;
						headerName = "0x" + headerNum.ToString("x");
					}
				} else {
					this.byDefname[headerName] = this;
				}

				this.defname1 = this.origData.TryPopPropsLine("defname");
				if (this.defname1 != null) {
					if (needsHeader) {
						headerName = this.defname1.Value;
						needsHeader = false;
					}
					this.byDefname[this.defname1.Value] = this;
				}

				this.defname2 = this.origData.TryPopPropsLine("defname2");
				if (this.defname2 != null) {
					if (needsHeader) {
						headerName = this.defname2.Value;
						needsHeader = false;
					}
					this.byDefname[this.defname2.Value] = this;
				}

				if (needsHeader) {
					//what now? :)
					headerName = "i_hadnodefname_0x" + headerNum.ToString("x");
					Info(this.origData.HeaderLine, "Has no defname except a number, and model defined elsewhere...");
				}
			}
		}

		public override void SecondStage() {
			base.SecondStage();
			if (this.idLine != null) {
				int idNum;
				if (ConvertTools.TryParseInt32(this.idLine.Value, out idNum)) {
					if (this.byModel.TryGetValue(idNum, out this.modelDef)) {
						Set("model", this.modelDef.PrettyDefname, this.idLine.Comment);
						//Info(idLine.line, "ID Written as "+modelDef.PrettyDefname);
					} else {
						Set("model", "0x" + idNum.ToString("x"), this.idLine.Comment);
						this.modelNum = idNum;
					}
				} else {
					Set("model", this.idLine.Value, this.idLine.Comment);
					this.byDefname.TryGetValue(this.idLine.Value, out this.modelDef);
				}
			}

			if (this.dupeItemLine != null) {
				int dupeItemNum;
				bool dupeItemSet = false;
				if (ConvertTools.TryParseInt32(this.dupeItemLine.Value, out dupeItemNum)) {
					ConvertedThingDef dupeItemDef;
					if (this.byModel.TryGetValue(dupeItemNum, out dupeItemDef)) {
						Set(this.dupeItemLine.Name, dupeItemDef.PrettyDefname, this.dupeItemLine.Comment);
						dupeItemSet = true;
						//Info(dupeItemLine.line, "DupeItem Written as "+dupeItemDef.PrettyDefname);
					}
				}
				if (!dupeItemSet) {
					MayBeHex_IgnorePoint(this, this.dupeItemLine);
				}
			}
		}

		public override void ThirdStage() {
			bool defnameWritten = false;
			if (this.defname1 != null) {
				if (!StringComparer.OrdinalIgnoreCase.Equals(headerName, this.defname1.Value)) {
					defnameWritten = true;
					Set(this.defname1);
				}
			}
			if (this.defname2 != null) {
				if (defnameWritten) {
					Warning(this.defname2.Line, "Defname2 ignored. In steamengine, defs can have mostly 1 alternative defname.");
				} else if (!StringComparer.OrdinalIgnoreCase.Equals(this.headerType, this.defname2.Value)) {
					Set("defname", this.defname2.Value, this.defname2.Comment);
				}
			}

			base.ThirdStage();
		}

		public string PrettyDefname {
			get {
				if (!this.hasNumericalHeader) {
					return headerName;
				}
				if (this.defname1 != null) {
					return this.defname1.Value;
				}
				if (this.defname2 != null) {
					return defname2.Value;
				}

				//it's numeric, so...
				return "0x" + ConvertTools.ParseInt64(headerName).ToString("x");
			}
		}
	}
}