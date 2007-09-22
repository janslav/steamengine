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
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Globalization;
using SteamEngine.Common;
using System.Configuration;
using SteamEngine;
using SteamEngine.CompiledScripts;

namespace SteamEngine.Converter {
	public class ConvertedItemDef : ConvertedThingDef {
		public static Dictionary<string, ConvertedThingDef> itemsByDefname = new Dictionary<string, ConvertedThingDef>(StringComparer.OrdinalIgnoreCase);
		public static Dictionary<int, ConvertedThingDef> itemsByModel = new Dictionary<int, ConvertedThingDef>();

		string type;

		string layer = "-1";
		bool layerSet = false;
		bool isEquippable = false;

		bool isWeapon = false;
		bool isWearable = false;
		bool armorOrDamHandled = false;
		bool isTwoHanded = false;
		bool twoHandedSet = false;
	
		private static LineImplTask[] firstStageImpl = new LineImplTask[] {
				new LineImplTask("type", new LineImpl(HandleType)), 
				new LineImplTask("dupelist", new LineImpl(WriteAsComment)), 
				new LineImplTask("weight", new LineImpl(MayBeInt_IgnorePoint)), 
//TODO
				new LineImplTask("resources", new LineImpl(WriteAsComment)),
				new LineImplTask("resources2", new LineImpl(WriteAsComment)),
				new LineImplTask("skillmake", new LineImpl(WriteAsComment)),
				new LineImplTask("skillmake2", new LineImpl(WriteAsComment)),
				new LineImplTask("flip", new LineImpl(WriteAsComment)),
				new LineImplTask("reqstr", new LineImpl(WriteAsComment)),
				new LineImplTask("dye", new LineImpl(WriteAsComment)),
				new LineImplTask("tdata1", new LineImpl(WriteAsComment)),
				new LineImplTask("tdata2", new LineImpl(WriteAsComment)),
				new LineImplTask("tdata3", new LineImpl(WriteAsComment)),
				new LineImplTask("skill", new LineImpl(WriteAsComment)),
				new LineImplTask("speed", new LineImpl(WriteAsComment))
			};
			
		private static LineImplTask[] secondStageImpl = new LineImplTask[] {
				new LineImplTask("layer", new LineImpl(HandleLayer)), 
				new LineImplTask("twohanded", new LineImpl(HandleTwohanded)),
				new LineImplTask("twohands", new LineImpl(HandleTwohanded)),
			};

		private static LineImplTask[] thirdStageImpl = new LineImplTask[] {
				new LineImplTask("armor", new LineImpl(HandleArmorOrDam)),
				new LineImplTask("dam", new LineImpl(HandleArmorOrDam)),
			};


		public ConvertedItemDef(PropsSection input) : base(input) {
			this.byModel = itemsByModel;
			this.byDefname = itemsByDefname;

			this.firstStageImplementations.Add(firstStageImpl);
			this.secondStageImplementations.Add(secondStageImpl);
			this.thirdStageImplementations.Add(thirdStageImpl);
			
			headerType="ItemDef";
		}

		private static string HandleType(ConvertedDef d, PropsLine line) {
			ConvertedItemDef def = (ConvertedItemDef) d;
			def.Set(line);

			string args = line.value.ToLower();
			def.type = args;

			switch (args) {
				case "t_container":
				case "t_container_locked":
				case "t_eq_vendor_box":
				case "t_eq_bank_box":
					def.headerType="ContainerDef";
					def.isEquippable = true;
					break;
				case "t_corpse":
					def.headerType="CorpseDef";
					def.isEquippable = true;
					break;
				case "t_light_lit":
				case "t_light_out":
				case "t_wand":
				case "t_clothing":
				case "t_carpentry_chop":
				case "t_hair":
				case "t_beard":
				case "t_spellbook":
					def.headerType="EquippableDef";
					def.isEquippable = true;
					break;
				case "t_armor_leather":
				case "t_armor":
				case "t_shield":
					def.isWearable = true;
					def.isEquippable = true;
					break;
				case "t_multi":
					def.headerType="MultiItemDef";
					def.DontDump();	//for now
					break;
				case "t_musical":
					def.headerType="MusicalDef";
					break;
				case "t_eq_horse":
					//isHorseMountItem=true;
					Logger.WriteInfo(ConverterMain.AdditionalConverterMessages, "Ignoring mountitem def "+LogStr.Ident(def.headerName)+" (steamengine does not need those defined).");
					def.DontDump();	//TODO: make just some constant out of it?
					break;

				default:
					if (args.StartsWith("t_weapon")) {
						def.isWeapon = true;
						def.isEquippable = true;
					}
					break;
			}
			//TODO: Implement args=="t_sign_gump" || "t_board", which have gumps too in sphere scripts,
			//but aren't containers.
			//TODO: Detect if item with 'resmake' isn't craftable.
			return line.value;
		}

		private static string HandleTwohanded(ConvertedDef d, PropsLine line) {
			ConvertedItemDef def = (ConvertedItemDef) d;
			def.twoHandedSet = true;
			string largs = line.value.ToLower();
			switch (largs) {
				case "0":
				case "n":
				case "false":
					def.Set("TwoHanded", "false", line.comment);
					return "false";
				default:
					def.Set("TwoHanded", "true", line.comment);
					def.isTwoHanded = true;
					return "true";
			}

		}

		private static string HandleArmorOrDam(ConvertedDef d, PropsLine line) {
			ConvertedItemDef def = (ConvertedItemDef) d;
			if (!def.armorOrDamHandled) {
				def.armorOrDamHandled = true;
				string value = null;
				string[] strings = Utility.SplitSphereString(line.value);
				int n = strings.Length;
				double sum = 0;
				for (int i = 0; i<n; i++) {
					string str = strings[i];
					int number;
					if (ConvertTools.TryParseInt32(str, out number)) {
						sum += number;
					} else {
						sum = int.MinValue;
						break;
					}
				}
				if (sum != 0) {
					sum = sum / n;
					if (def.isWearable) {
						sum = Math.Round(sum);
					}
					value = sum.ToString();
				} else {
					value = line.value;
				}

				if (def.isWeapon) {
					def.Set("attack", value, line.comment);
					def.Set("piercing", "100", "");
					def.Set("speed", "100", "");
					def.Set("range", "1", "");
					def.Set("strikeStartRange", "5", "");
					def.Set("strikeStopRange", "10", "");
				} else if (def.isWearable) {
					def.Set("armorVsP", value, line.comment);
					def.Set("mindDefenseVsP", value, "");
					def.Set("armorVsM", value, "");
					def.Set("mindDefenseVsM", value, "");
				}
				return value;
			}
			return "";
		}

		public static void SecondStageFinished() {
			HashSet<ConvertedItemDef> itemDefSet = new HashSet<ConvertedItemDef>();
			foreach (ConvertedItemDef def in itemsByDefname.Values) {
				itemDefSet.Add(def);
			}

			foreach (ConvertedItemDef def in itemDefSet) {
				ConvertedItemDef baseDef = def.modelDef as ConvertedItemDef;
				if (baseDef != null) {
					if (baseDef.isEquippable) {
						def.MakeEquippable();

						if ((baseDef.isTwoHanded) && (!def.twoHandedSet)) {
							def.Set("TwoHanded", "true", "guessed from base def by Converter");
						}
						if ((baseDef.layerSet) && (!def.layerSet)) {
							def.Set("Layer", baseDef.layer, "guessed from base def by Converter");
							def.layerSet = true;
							def.layer = baseDef.layer;
						}
					}

					if ((baseDef.type != null) && (def.type == null)) {
						HandleType(def, new PropsLine("type", baseDef.type, -1, "guessed from base def by Converter"));
					}
				}
			}
		}

		private void MakeEquippable() {
			if (headerType.Equals("ItemDef")) {
				headerType = "EquippableDef";
			}
			isEquippable = true;
		}

		public override void ThirdStage() {
			if (isEquippable && !layerSet) {
				int model = this.Model;
				ItemDispidInfo info = ItemDispidInfo.Get(model);
				if (info != null) {
					this.layer = info.quality.ToString();
					Set("layer", this.layer, "Set by Converter");
					layerSet = true;
				} else {
					Set("//layer", "unknown", "");
					Info(origData.headerLine, "Unknown layer for ItemDef "+headerName);
				}
			}

			if (isWeapon) {
				bool isColored = IsColoredMetal();
				if (isColored) {
					this.headerType = "ColoredWeaponDef";
				} else {
					this.headerType = "WeaponDef";
				}

				MaterialType materialType = MaterialType.Metal;
				WeaponType weaponType = WeaponType.BareHands;

				switch (this.type.ToLower()) {
					case "t_weapon_sword":
						string prettyDefName = this.PrettyDefname.ToLower();
						if (prettyDefName.Contains("_axe_") || prettyDefName.EndsWith("_axe")) {
							if (isTwoHanded) {
								weaponType = WeaponType.TwoHandAxe;
							} else {
								weaponType = WeaponType.OneHandAxe;
							}
						} else {
							if (isTwoHanded) {
								weaponType = WeaponType.TwoHandSword;
							} else {
								weaponType = WeaponType.OneHandSword;
							}
						}
						break;
					case "t_weapon_fence":
						if (isTwoHanded) {
							weaponType = WeaponType.TwoHandSpike;
						} else {
							weaponType = WeaponType.OneHandSpike;
						}
						break;
					case "t_weapon_mace_crook":
					case "t_weapon_mace_staff":
						materialType = MaterialType.Wood;
						goto case "t_weapon_mace_smith";
					case "t_weapon_mace_smith":
						if (isTwoHanded) {
							weaponType = WeaponType.TwoHandSpike;
						} else {
							weaponType = WeaponType.OneHandSpike;
						}
						break;
					case "t_weapon_bow":
					case "t_weapon_xbow":
						weaponType = WeaponType.ArcheryStand;
						materialType = MaterialType.Wood;
						break;
					case "t_weapon_bow_run":
						weaponType = WeaponType.ArcheryRunning;
						materialType = MaterialType.Wood;
						//TODO - preferred ammo...?
						break;
					case "t_weapon_bolt":
					case "t_weapon_bolt_jagged":
					case "t_weapon_arrow":
						materialType = MaterialType.Wood;
						//TODO - different class altogether?
						break;
				}
				Set("WeaponType", "WeaponType."+weaponType, "guessed by Converter");

				if (isColored) {
					Set("MaterialType", "MaterialType."+materialType, "guessed by Converter");
				}
			} else if (isWearable) {
				bool isColored = IsColoredMetal();
				if (isColored) {
					this.headerType = "ColoredArmorDef";
					Set("MaterialType", "MaterialType.Metal", "guessed by Converter");
				} else {
					this.headerType = "WearableDef";
				}
			}

			base.ThirdStage();
		}

		private bool IsColoredMetal() {
			string material = GetMaterialFromDefname(this.PrettyDefname);
			if (material != null) {
				Set("Material", "Material."+Utility.Capitalize(material), "guessed by Converter");
				return true;
			}
			return false;
		}

		private static Regex materialRE = new Regex(
			@".*(?<material>((Copper)|(Iron)|(Silver)|(Gold)|(Verite)|(Valorite)|(Obsidian)|(Adamantinum)|(Mithril))).*", 
			RegexOptions.Compiled|RegexOptions.CultureInvariant|RegexOptions.ExplicitCapture|RegexOptions.IgnoreCase);

		private static string GetMaterialFromDefname(string defname) {
			Match m = materialRE.Match(defname);
			if (m.Success) {
				return m.Groups["material"].Value;
			}
			return null;
		}

		
		private static string HandleLayer(ConvertedDef d, PropsLine line) {
			ConvertedItemDef def = (ConvertedItemDef) d;
			def.MakeEquippable();
			def.layerSet = true;
			def.layer = MayBeInt_IgnorePoint(def, line);
			return def.layer;
		}
	}
}