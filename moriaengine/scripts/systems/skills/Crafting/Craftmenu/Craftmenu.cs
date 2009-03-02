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
using SteamEngine;
using SteamEngine.Common;
using SteamEngine.Persistence;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {
	[HasSavedMembers]
	[Summary("Class containing all main crafting categories (according to the crafting skills)." +
			"Each skill has one main category for its items and possible subcategories")]
	public static class CraftmenuContents {
		public static Dictionary<string, string> typeNames = new Dictionary<string, string>();

		static CraftmenuContents() {
			//fill the dictionary with names of various item types
			typeNames["00"] = "Normal";
			typeNames["t_normal"] = "Normal";
			typeNames["t_container"] = "Kontejner";
			typeNames["t_container_locked"] = "Zamèený Kontejner";
			typeNames["t_door"] = "Dveøe";
			typeNames["t_door_locked"] = "Zamèené dveøe";
			typeNames["t_key"] = "Klíè";
			typeNames["t_light_lit"] = "Svítící svìtlo";
			typeNames["t_light_out"] = "Zhasnuté svìtlo";
			typeNames["t_food"] = "Jídlo";
			typeNames["t_food_raw"] = "Syrové Jídlo";
			typeNames["t_armor"] = "Brnìní";
			typeNames["t_weapon_mace_smith"] = "Tupá Zbraò(nástroj?)";
			typeNames["t_weapon_mace_sharp"] = "Tupá Zbraò";
			typeNames["t_weapon_sword"] = "Ostra Zbraò (tìžká)";
			typeNames["t_weapon_fence"] = "Ostrá Zbraò (lehká)";
			typeNames["t_weapon_bow"] = "Luk";
			typeNames["t_wand"] = "Hùlka";
			typeNames["t_telepad"] = "Telepad";
			typeNames["t_switch"] = "Pøepínaè";
			typeNames["t_book"] = "Kniha";
			typeNames["t_rune"] = "Runa";
			typeNames["t_booze"] = "Alkohol";
			typeNames["t_potion"] = "Lektvar";
			typeNames["t_allpotions"] = "Lektvar";
			typeNames["t_speedpotions"] = "Lektvar";
			typeNames["t_potion_bomba"] = "Bomba";
			typeNames["t_fire"] = "Oheò";
			typeNames["t_clock"] = "Hodiny";
			typeNames["t_trap"] = "Past";
			typeNames["t_trap_active"] = "Aktivní past";
			typeNames["t_musical"] = "Hudební nástroj";
			typeNames["t_spell"] = "Kouzlo";
			typeNames["t_gem"] = "Drahokam";
			typeNames["t_water"] = "Voda";
			typeNames["t_clothing"] = "Obleèení";
			typeNames["t_scroll"] = "	Svitek kouzla";
			typeNames["t_carpentry"] = "Truhláøský nástroj";
			typeNames["t_spawn_char"] = "Character spawner";
			typeNames["t_game_piece"] = "Hrací figurka";
			typeNames["t_portculis"] = "Portculis?";
			typeNames["t_figurine"] = "Figurina(shrink)";
			typeNames["t_shrine"] = "Oltáø(ress)";
			typeNames["t_moongate"] = "Moongate";
			typeNames["t_chair"] = "Židle";
			typeNames["t_forge"] = "Výheò";
			typeNames["t_ore"] = "Ruda";
			typeNames["t_log"] = "Kmen";
			typeNames["t_tree"] = "Strom";
			typeNames["t_rock"] = "Skála";
			typeNames["t_carpentry_chop"] = "Not Used?";
			typeNames["t_multi"] = "Multiitem";
			typeNames["t_reagent"] = "Reagent";
			typeNames["t_ship"] = "Loï";
			typeNames["t_ship_plank"] = "Lodní mùstek";
			typeNames["t_ship_side"] = "Bok lodi";
			typeNames["t_ship_side_locked"] = "Zamèený bok lodi";
			typeNames["t_ship_tiller"] = "Lodivod";
			typeNames["t_eq_trade_window"] = "Secure trade item";
			typeNames["t_fish"] = "Ryba";
			typeNames["t_sign_gump"] = "Cedule";
			typeNames["t_stone_guild"] = "Guild stone";
			typeNames["t_anim_active"] = "Animace";
			typeNames["t_advance_gate"] = "Advance Gate";
			typeNames["t_cloth"] = "Látka";
			typeNames["t_hair"] = "Vlasy";
			typeNames["t_beard"] = "Vousy";
			typeNames["t_ingot"] = "Ingot";
			typeNames["t_coin"] = "Mince";
			typeNames["t_crops"] = "Ùroda";
			typeNames["t_drink"] = "Pití";
			typeNames["t_anvil"] = "Kovadlina";
			typeNames["t_port_locked"] = "Zamèený port";
			typeNames["t_spawn_item"] = "Item spawner";
			typeNames["t_telescope"] = "Teleskop";
			typeNames["t_bed"] = "Postel";
			typeNames["t_gold"] = "Peníze";
			typeNames["t_map"] = "Mapa";
			typeNames["t_eq_memory_obj"] = "equippable memory object";
			typeNames["t_weapon_mace_staff"] = "Tupá zbraò (hul)";
			typeNames["t_eq_horse"] = "Mount item (equipped horse)";
			typeNames["t_comm_crystal"] = "Komunkaèní krystal";
			typeNames["t_game_board"] = "Herní deska";
			typeNames["t_trash_can"] = "Odpadkový koš";
			typeNames["t_cannon_muzzle"] = "Kanon - prach?";
			typeNames["t_cannon"] = "Kanon";
			typeNames["t_cannon_ball"] = "Dìlová koule";
			typeNames["t_armor_leather"] = "Kožené brnìní";
			typeNames["t_seed"] = "Semena";
			typeNames["t_junk"] = "Odpad";
			typeNames["t_crystal_ball"] = "Køíšálová koule";
			//Typename_t_old_cashiers_check	
			typeNames["t_message"] = "Zpráva";
			typeNames["t_reagent_raw"] = "'Syrový' reagent";
			typeNames["t_eq_client_linger"] = "udìlá z klienta NPC";
			typeNames["t_dream_gate"] = "Brána na jiný shard?";
			typeNames["t_it_stone"] = "Supply stone";
			typeNames["t_metronome"] = "Metronom";
			typeNames["t_explosion"] = "Exploze";
			typeNames["t_eq_npc_script"] = "EQuippable NPC script";
			typeNames["t_web"] = "Pavouèí sí";
			typeNames["t_grass"] = "Tráva";
			typeNames["t_arock"] = "Kámen(házen obry)";
			typeNames["t_tracker"] = "Tracking obj";
			typeNames["t_sound"] = "Zvuk";
			typeNames["t_stone_town"] = "Townstone";
			typeNames["t_weapon_mace_crook"] = "Tupá zbraò (hák)";
			typeNames["t_weapon_bow_run"] = "Run støelná zbraò (/krumpáè)";
			typeNames["t_leather"] = "Kùže";
			typeNames["t_ship_other"] = "Èást lodi?";
			typeNames["t_bboard"] = "Èást tabule";
			typeNames["t_spellbook"] = "Kouzelná kniha";
			typeNames["t_corpse"] = "Mrtvola";
			typeNames["t_track_item"] = "Tracking item";
			typeNames["t_track_char"] = "Tracking character";
			typeNames["t_weapon_arrow"] = "Šíp";
			typeNames["t_weapon_bolt"] = "Šipka";
			typeNames["t_weapon_bolt_jagged"] = "Zubatá šipka";
			typeNames["t_eq_vendor_box"] = "Vendor box";
			typeNames["t_eq_bank_box"] = "Bank box";
			typeNames["t_deed"] = "Deed";
			typeNames["t_loom"] = "Tkalcovský stav";
			typeNames["t_bee_hive"] = "Vèelí úl";
			typeNames["t_archery_butte"] = "Terè";
			typeNames["t_eq_murder_count"] = "Killcount timer";
			typeNames["t_eq_stuck"] = "para v pavouèí síti";
			typeNames["t_trap_inactive"] = "Neaktivní past";
			typeNames["t_stone_room"] = "Region stone?";
			typeNames["t_bandage"] = "Bandáž(healing)";
			typeNames["t_campfire"] = "Táborák(camping)";
			typeNames["t_map_blank"] = "Prázdná mapa";
			typeNames["t_spy_glass"] = "Dalekohled";
			typeNames["t_sextant"] = "Sextant";
			typeNames["t_scroll_blank"] = "Prázdný svitek";
			typeNames["t_fruit"] = "Ovoce";
			typeNames["t_water_wash"] = "Èistá voda (bez ryb)";
			typeNames["t_weapon_axe"] = "Ostrá zbraò (sekera?)";
			typeNames["t_weapon_xbow"] = "Kuèe";
			typeNames["t_spellicon"] = "Ikona kouzla";
			typeNames["t_door_open"] = "Otevøené dveøe";
			typeNames["t_meat_raw"] = "Syrové maso";
			typeNames["t_garbage"] = "Odpad";
			typeNames["t_keyring"] = "Kroužek na klíèe";
			typeNames["t_table"] = "Stùl";
			typeNames["t_floor"] = "Podlaha";
			typeNames["t_roof"] = "Støecha";
			typeNames["t_feather"] = "Peøí";
			typeNames["t_wool"] = "Vlna";
			typeNames["t_fur"] = "Srst";
			typeNames["t_blood"] = "Krev";
			typeNames["t_foliage"] = "jídlo?";
			typeNames["t_grain"] = "Zrní";
			typeNames["t_scissors"] = "Nùžky";
			typeNames["t_thread"] = "Nitì";
			typeNames["t_yarn"] = "Pøíze";
			typeNames["t_spinwheel"] = "Kolovrat";
			typeNames["t_bandage_blood"] = "Krvavá bandáž";
			typeNames["t_fish_pole"] = "Rybáøský prut";
			typeNames["t_shaft"] = "Násada; Døík";
			typeNames["t_lockpick"] = "Šperhák";
			typeNames["t_kindling"] = "Tøíska";
			typeNames["t_train_dummy"] = "Panák k boji";
			typeNames["t_train_pickpocket"] = "Panák k okrádáni";
			typeNames["t_bedroll"] = "Pøikrývka";
			typeNames["t_bellows"] = "Mech";
			typeNames["t_hide"] = "Kùže";
			typeNames["t_cloth_bolt"] = "Balík látky";
			typeNames["t_board"] = "Trám/prkno";
			typeNames["t_pitcher"] = "Džbán";
			typeNames["t_pitcher_empty"] = "Prázdný džbán";
			typeNames["t_dye_vat"] = "Nádoba na barvu";
			typeNames["t_dye"] = "Barva";
			typeNames["t_potion_empty"] = "Prázdný lektvar";
			typeNames["t_mortar"] = "Hmoždíø";
			typeNames["t_hair_dye"] = "Barva na vlasy";
			typeNames["t_sewing_kit"] = "Šicí souprava";
			typeNames["t_tinker_tools"] = "Nastroje(tinkering)";
			typeNames["t_wall"] = "Zeï";
			typeNames["t_window"] = "Okno";
			typeNames["t_cotton"] = "Bavlna";
			typeNames["t_bone"] = "Kost";
			typeNames["t_eq_script"] = "Equippable script item";
			typeNames["t_ship_hold"] = "Lodní podpalubí";
			typeNames["t_ship_hold_lock"] = "Zamèene podpalubí";
			typeNames["t_lava"] = "Láva";
			typeNames["t_shield"] = "Štít";
			typeNames["t_jewelry"] = "Šperk";
			typeNames["t_dirt"] = "Špína";
			typeNames["t_script"] = "Skript item";
			typeNames["t_eq_message"] = "Dialog item";
			typeNames["t_shovel"] = "Lopata";
			typeNames["t_book_shrink"] = "Recept shrink";
			typeNames["t_bottle_form"] = "Forma Láhev";
			typeNames["t_book_bottle"] = "Recept Láhev";
			typeNames["t_src_changer"] = "mìnitko Src(script)";
			typeNames["t_book_inscription"] = "Kniha inscripce";
			typeNames["t_bottle_empty"] = "Prázdná láhev";
		}

		[SavedMember]
		public static CraftmenuCategory categoryAlchemy = new CraftmenuCategory("Alchemy", null);
		[SavedMember]
		public static CraftmenuCategory categoryBlacksmithing = new CraftmenuCategory("Blacksmithing", null);
		[SavedMember]
		public static CraftmenuCategory categoryBowcraft = new CraftmenuCategory("Bowcraft", null);
		[SavedMember]
		public static CraftmenuCategory categoryCarpentry = new CraftmenuCategory("Carpentry", null);
		[SavedMember]
		public static CraftmenuCategory categoryCooking = new CraftmenuCategory("Cooking", null);
		[SavedMember]
		public static CraftmenuCategory categoryInscription = new CraftmenuCategory("Inscription", null);
		[SavedMember]
		public static CraftmenuCategory categoryTailoring = new CraftmenuCategory("Tailoring", null);
		[SavedMember]
		public static CraftmenuCategory categoryTinkering = new CraftmenuCategory("Tinkering", null);

		[Summary("Get the category path ('cat1->cat2->cat3') and try to find and return the category specified")]
		public static CraftmenuCategory GetCategoryByPath(string path) {
			string[] catChain = path.Split(new string[] { "->" }, StringSplitOptions.None);
			CraftmenuCategory candidateCat = null;
			switch (catChain[0]) {
				case "Alchemy":
					candidateCat = categoryAlchemy;
					break;
				case "Blacksmithing":
					candidateCat = categoryBlacksmithing;
					break;
				case "Bowcraft":
					candidateCat = categoryBowcraft;
					break;
				case "Carpentry":
					candidateCat = categoryCarpentry;
					break;
				case "Cooking":
					candidateCat = categoryCooking;
					break;
				case "Inscription":
					candidateCat = categoryInscription;
					break;
				case "Tailoring":
					candidateCat = categoryTailoring;
					break;
				case "Tinkering":
					candidateCat = categoryTinkering;
					break;
			}
			if (candidateCat != null) {
				if (catChain.Length == 1) {
					return candidateCat;
				}
				for (int i = 1; i < catChain.Length; i++) {
					foreach (ICraftmenuElement elem in candidateCat.contents) {
						if (elem.IsCategory && elem.Name.Equals(catChain[i], StringComparison.InvariantCultureIgnoreCase)) {
							candidateCat = (CraftmenuCategory) elem; //found the correct category in the chain
							break; //continue with another identifier string
						}
					}
				}
				return candidateCat; //last Category found
			}
			return null;//nothing was found which will not occur
		}
	}

	[Summary("One line in the craftmenu - can be either the category of items or the particular item(def) itself")]
	public interface ICraftmenuElement {
		string Name {
			get;
		}

		bool IsCategory {
			get;
		}

		CraftmenuCategory Parent {
			get;
		}

		void Remove();
	}

	[SaveableClass]
	[Summary("Craftmenu category class. This is the entity where all Items from the menu are stored as well " +
			"as it is a container for another subcategories")]
	public class CraftmenuCategory : ICraftmenuElement {
		[SaveableData]
		public string name; //name of the category
		[SaveableData]
		public List<ICraftmenuElement> contents = new List<ICraftmenuElement>(); //itemdefs and subcategorties contained in the category
		[SaveableData]
		public CraftmenuCategory parent = null;

		[LoadingInitializer]
		public CraftmenuCategory() {
		}

		public CraftmenuCategory(string name, CraftmenuCategory parent) {
			this.name = name;
			this.parent = parent;
		}

		[Summary("Get name compound also from the possible parent's name (if any)")]
		public string FullName {
			get {
				if (parent == null) {
					return name;
				} else {
					return parent.FullName + "->" + name;
				}
			}
		}

		#region ICraftmenuElement Members
		public string Name {
			get {
				return name;
			}
		}

		public bool IsCategory {
			get {
				return true;
			}
		}

		public CraftmenuCategory Parent {
			get {
				return parent;
			}
		}

		[Summary("Category cleaning method - clear the contents and remove from the parent")]
		public void Remove() {
			if (parent != null) {
				parent.contents.Remove(this);
			}
			contents.Clear();
		}
		#endregion
	}

	[SaveableClass]
	[Summary("Craftmenu item class. This is the entity representing a single Item in the craftmenu")]
	public class CraftmenuItem : ICraftmenuElement {
		[SaveableData]
		public ItemDef itemDef;
		[SaveableData]
		public CraftmenuCategory parent;

		[LoadingInitializer]
		public CraftmenuItem() {
		}

		public CraftmenuItem(ItemDef itemDef, CraftmenuCategory parent) {
			this.itemDef = itemDef;
			this.parent = parent;
		}

		#region ICraftmenuElement Members
		public string Name {
			get {
				return itemDef.Name;
			}
		}

		public bool IsCategory {
			get {
				return false;
			}
		}

		[Summary("CraftmenuItem must always be in some CraftmenuCategory!")]
		public CraftmenuCategory Parent {
			get {
				return parent;
			}
		}

		[Summary("Remove the item from the parent's list")]
		public void Remove() {
			if (parent != null) {
				parent.contents.Remove(this);
			}
		}
		#endregion
	}

	[Summary("Target for targetting single items or containers containing items to be added to the craftmenu (to the selected category)")]
	public class Targ_Craftmenu : CompiledTargetDef {
		protected override void On_Start(Player self, object parameter) {
			self.SysMessage("Zamìø pøedmìt pøípadnì konejner s pøedmìty pro pøidání do craftmenu.");
			base.On_Start(self, parameter);
		}

		protected override bool On_TargonItem(Player self, Item targetted, object parameter) {
			CraftmenuCategory catToPut = (CraftmenuCategory) parameter;

			CraftmenuItem newItem = null;
			if (targetted.IsContainer) {
				int containedCount = 0;
				foreach (Item inner in targetted.EnumShallow()) {
					newItem = new CraftmenuItem((ItemDef) inner.Def, catToPut);//add the contained items
					catToPut.contents.Add(newItem);
					containedCount++;
				}
				if (containedCount == 0) {//nothing was inside, we attempted to add the empty container
					newItem = new CraftmenuItem((ItemDef) targetted.Def, catToPut);//add the container itself
					catToPut.contents.Add(newItem);
				}
			} else {//single item - add it now
				newItem = new CraftmenuItem((ItemDef) targetted.Def, catToPut);
				catToPut.contents.Add(newItem);
			}

			//reopen the dialog on the stored position
			//check the stored last displayed category
			string prevCat = TagMath.SGetTag(self, D_Craftmenu.tkCraftmenuLastpos);
			if (prevCat != null) {
				CraftmenuCategory oldCat = CraftmenuContents.GetCategoryByPath(prevCat);
				if (oldCat != null) {
					self.Dialog(SingletonScript<D_Craftmenu>.Instance, new DialogArgs(oldCat));
				} else {
					self.Dialog(SingletonScript<D_CraftmenuCategories>.Instance);
				}
			} else {
				self.Dialog(SingletonScript<D_CraftmenuCategories>.Instance);
			}
			return false;
		}
	}
}