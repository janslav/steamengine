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
	

	public class ConvertedCharDef : ConvertedThingDef {
		public static Dictionary<string, ConvertedThingDef> charsByDefname = new Dictionary<string, ConvertedThingDef>(StringComparer.OrdinalIgnoreCase);
		public static Dictionary<int, ConvertedThingDef> charsByModel = new Dictionary<int, ConvertedThingDef>();
		//by model and by defnames
		
	
		private static LineImplTask[] firstStageImpl = new LineImplTask[] {
				new LineImplTask("anim", new LineImpl(MayBeHex_IgnorePoint)), 
//TODO:
				new LineImplTask("npc", new LineImpl(WriteAsComment)),
				new LineImplTask("brain", new LineImpl(WriteAsComment)),
				new LineImplTask("sound", new LineImpl(WriteAsComment)),


			};

		private static LineImplTask[] thirdStageImpl = new LineImplTask[] {
			new LineImplTask("mountid", new LineImpl(HandleMountId)),
			new LineImplTask("tag.mountid", new LineImpl(HandleMountId)),
																			  
		};

		public ConvertedCharDef(PropsSection input) : base(input) {
			this.byModel = charsByModel;
			this.byDefname = charsByDefname;

			this.firstStageImplementations.Add(firstStageImpl);
			this.thirdStageImplementations.Add(thirdStageImpl);
			headerType = "CharacterDef";
		}

		public override void ThirdStage() {
			base.ThirdStage();

			string defname = this.PrettyDefname;
			if ((string.Compare(defname, "c_man", true) == 0) ||
					(string.Compare(defname, "c_woman", true) == 0)) {

				headerType = "PlayerDef";
			}
		}

		private static string HandleMountId(ConvertedDef def, PropsLine line) {
			int num = -1; 
			if (!ConvertTools.TryParseInt32(line.value, out num)) {
				ConvertedThingDef i;
				if (ConvertedItemDef.itemsByDefname.TryGetValue(line.value, out i)) {
					num = i.Model;
				}
			}
			if (num != -1) {
				string retVal = "0x"+num.ToString("x");
				def.Set("MountItem", retVal, line.comment);
				return retVal;
			} else {
				def.Warning(line.line, "Unresolvable MountItem model");
			}
			return "";
		}
	}
}