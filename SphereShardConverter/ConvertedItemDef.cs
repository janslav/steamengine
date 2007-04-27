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
using System.Reflection;
using System.IO;
using System.Globalization;
using SteamEngine.Common;
using System.Configuration;
using SteamEngine;

namespace SteamEngine.Converter {
	public class ConvertedItemDef : ConvertedThingDef {
		public static Hashtable itemdefs = new Hashtable(StringComparer.OrdinalIgnoreCase);
		//by model and by defnames
		
		bool layerSet = false;
		bool isEquippable = false;
	
		private static LineImplTask[] firstStageImpl = new LineImplTask[] {
				new LineImplTask("type", new LineImpl(HandleType)), 
				new LineImplTask("dupelist", new LineImpl(WriteAsComment)), 
				new LineImplTask("weight", new LineImpl(MayBeInt_IgnorePoint)), 
//TODO
				new LineImplTask("twohanded", new LineImpl(WriteAsComment)),
				new LineImplTask("twohands", new LineImpl(WriteAsComment)),
				new LineImplTask("resources", new LineImpl(WriteAsComment)),
				new LineImplTask("resources2", new LineImpl(WriteAsComment)),
				new LineImplTask("skillmake", new LineImpl(WriteAsComment)),
				new LineImplTask("skillmake2", new LineImpl(WriteAsComment)),
				new LineImplTask("armor", new LineImpl(WriteAsComment)),
				new LineImplTask("flip", new LineImpl(WriteAsComment)),
				new LineImplTask("reqstr", new LineImpl(WriteAsComment)),
				new LineImplTask("dye", new LineImpl(WriteAsComment)),
				new LineImplTask("tdata1", new LineImpl(WriteAsComment)),
				new LineImplTask("tdata2", new LineImpl(WriteAsComment)),
				new LineImplTask("tdata3", new LineImpl(WriteAsComment)),
				new LineImplTask("skill", new LineImpl(WriteAsComment)),
				new LineImplTask("dam", new LineImpl(WriteAsComment)),
				new LineImplTask("speed", new LineImpl(WriteAsComment))
			};
			
			private static LineImplTask[] secondStageImpl = new LineImplTask[] {
				new LineImplTask("layer", new LineImpl(HandleLayer)), 
			};
//					} case "tdata1": {
//						if (Get("typeclass")=="Musical") {
//							Set("successSound", SphereNumberCheck(args, false));
//						}
//						break;
//					} case "tdata2": {
//						if (Get("typeclass")!=null && Get("typeclass")=="Container") {
//							Set("gump",SphereNumberCheck(args, false));
//						} else {
//							if (Get("typeclass")=="Musical") {
//								Set("failureSound", SphereNumberCheck(args, false));
//							}
//						}
//						break;
//					} case "tdata3": {
//						if (isHorseMountItem) {
//							Set("mountchar", args);
//						}
//						break;


		public ConvertedItemDef(PropsSection input) : base(input) {
			this.myTypeList = itemdefs;

			this.firstStageImplementations.Add(firstStageImpl);
			this.secondStageImplementations.Add(secondStageImpl);
			
			headerType="ItemDef";
		}

//		private static void HandleTwohanded(convertedDef def, PropsLine line) {
//						string largs = args.ToLower();
//						if (largs=="y") {
//							Set("twohanded","true");
//						} else {
//							if (largs!="n") {
//								Warning("Twohands was expected to be either 'Y' or 'N' (or 'y' or 'n'...), but it was '"+args+"'!");
//							}
//							Set("twohanded","false");
//						}
//						break;
//		}

		public static void SecondStageFinished() {
			Globals.useTileData = true;
			TileData.Init();
		}

		private void MakeEquippable() {
			if (headerType.Equals("ItemDef")) {
				headerType = "EquippableDef";
			}
			isEquippable = true;
		}

		public override void ThirdStage() {
			if (!isEquippable) {
				ConvertedItemDef m = (ConvertedItemDef) itemdefs[Model];
				if ((m != null) && m.isEquippable) {
					MakeEquippable();
				}
			}

			if (isEquippable && !layerSet) {
				int model = this.Model;
				ItemDispidInfo info = ItemDispidInfo.Get(model);
				if (info != null) {
					Set("layer", info.quality.ToString(), "Set by Converter");
					layerSet = true;
				} else {
					Info(origData.headerLine, "Unknown layer for ItemDef "+headerName);
				}
			}
			base.ThirdStage();
		}


		private static void HandleType(ConvertedDef def, PropsLine line) {
			def.Set(line);

			string args=line.value.ToLower();
			if (args=="t_container" || args=="t_container_locked" || args=="t_eq_vendor_box" ||
					args=="t_eq_bank_box" || args=="t_corpse") {
				def.headerType="ContainerDef";
				((ConvertedItemDef) def).isEquippable = true;
			} else if (args=="t_light_lit" || args=="t_light_out" || args=="t_armor" ||
					args=="t_wand" || args=="t_clothing" || args=="t_carpentry_chop" || args=="t_hair" ||
					args=="t_beard" || args=="t_armor_leather" || args=="t_spellbook" ||
					args=="t_shield" || args=="t_jewelry" ||
					args.IndexOf("t_weapon")==0) {
				def.headerType="EquippableDef";
				((ConvertedItemDef) def).isEquippable = true;
			} else if (args=="t_multi") {
				def.headerType="MultiItemDef";
				def.DontDump();	//for now
			} else if (args=="t_eq_horse") {
				//isHorseMountItem=true;
				Logger.WriteInfo(ConverterMain.AdditionalConverterMessages, "Ignoring mountitem def "+LogStr.Ident(def.headerName)+" (steamengine does not need those defined).");
				def.DontDump();	//TODO: make just some constant out of it?
			} else if (args=="t_musical") {
				def.headerType="MusicalDef";
				//Remove("type");
			}
			//TODO: Implement args=="t_sign_gump" || "t_bboard", which have gumps too in sphere scripts,
			//but aren't containers.
			//TODO: Detect if item with 'resmake' isn't craftable.
		}
		
		private static void HandleLayer(ConvertedDef def, PropsLine line) {
			((ConvertedItemDef) def).MakeEquippable();
			((ConvertedItemDef) def).layerSet = true;
			MayBeInt_IgnorePoint(def, line);
		}
	}
}