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
				if (modelNum == -1) {
					if (modelDef != null) {
						return modelDef.Model;
					}
				} else {
					return modelNum;
				}
				Error(origData.headerLine, "ThingDef " + headerName + " has no model set...?");
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
			def.Set("triggerGroup", line.value, line.comment);
		}



		public override void FirstStage() {
			base.FirstStage();

			dupeItemLine = origData.TryPopPropsLine("dupeitem");
			if (dupeItemLine != null) {
				int headerNum;
				if (ConvertTools.TryParseInt32(headerName, out headerNum)) {
					headerName = "0x" + headerNum.ToString("x");
					modelNum = headerNum;
				}
			} else {
				bool needsHeader = false;
				idLine = origData.TryPopPropsLine("id");

				int headerNum;
				if (ConvertTools.TryParseInt32(headerName, out headerNum)) {
					if (idLine != null) {//it does not mean model...
						needsHeader = true;
					} else {
						hasNumericalHeader = true;
						//ownModel = true;
						modelNum = headerNum;
						byModel[headerNum] = this;
						headerName = "0x" + headerNum.ToString("x");
					}
				} else {
					byDefname[headerName] = this;
				}

				defname1 = origData.TryPopPropsLine("defname");
				if (defname1 != null) {
					if (needsHeader) {
						headerName = defname1.value;
						needsHeader = false;
					}
					byDefname[defname1.value] = this;
				}

				defname2 = origData.TryPopPropsLine("defname2");
				if (defname2 != null) {
					if (needsHeader) {
						headerName = defname2.value;
						needsHeader = false;
					}
					byDefname[defname2.value] = this;
				}

				if (needsHeader) {
					//what now? :)
					headerName = "i_hadnodefname_0x" + headerNum.ToString("x");
					Info(origData.headerLine, "Has no defname except a number, and model defined elsewhere...");
				}
			}
		}

		public override void SecondStage() {
			base.SecondStage();
			if (idLine != null) {
				int idNum;
				if (ConvertTools.TryParseInt32(idLine.value, out idNum)) {
					if (byModel.TryGetValue(idNum, out modelDef)) {
						Set("model", modelDef.PrettyDefname, idLine.comment);
						//Info(idLine.line, "ID Written as "+modelDef.PrettyDefname);
					} else {
						Set("model", "0x" + idNum.ToString("x"), idLine.comment);
						modelNum = idNum;
					}
				} else {
					Set("model", idLine.value, idLine.comment);
					byDefname.TryGetValue(idLine.value, out modelDef);
				}
			}

			if (dupeItemLine != null) {
				int dupeItemNum;
				bool dupeItemSet = false;
				if (ConvertTools.TryParseInt32(dupeItemLine.value, out dupeItemNum)) {
					ConvertedThingDef dupeItemDef;
					if (byModel.TryGetValue(dupeItemNum, out dupeItemDef)) {
						Set(dupeItemLine.name, dupeItemDef.PrettyDefname, dupeItemLine.comment);
						dupeItemSet = true;
						//Info(dupeItemLine.line, "DupeItem Written as "+dupeItemDef.PrettyDefname);
					}
				}
				if (!dupeItemSet) {
					MayBeHex_IgnorePoint(this, dupeItemLine);
				}
			}
		}

		public override void ThirdStage() {
			bool defnameWritten = false;
			if (defname1 != null) {
				if (string.Compare(headerName, defname1.value, true) != 0) {
					defnameWritten = true;
					Set(defname1);
				}
			}
			if (defname2 != null) {
				if (defnameWritten) {
					Warning(defname2.line, "Defname2 ignored. In steamengine, defs can have mostly 1 alternative defname.");
				} else if (string.Compare(headerType, defname2.value, true) != 0) {
					Set("defname", defname2.value, defname2.comment);
				}
			}

			base.ThirdStage();
		}

		public string PrettyDefname {
			get {
				if (!hasNumericalHeader) {
					return headerName;
				}
				if (defname1 != null) {
					return defname1.value;
				}
				if (defname2 != null) {
					return defname2.value;
				}

				//it's numeric, so...
				return "0x" + ConvertTools.ParseInt64(headerName).ToString("x");
			}
		}
	}
}