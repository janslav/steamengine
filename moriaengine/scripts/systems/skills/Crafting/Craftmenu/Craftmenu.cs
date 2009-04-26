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
			typeNames["t_container_locked"] = "ZamËen˝ Kontejner";
			typeNames["t_door"] = "Dve¯e";
			typeNames["t_door_locked"] = "ZamËenÈ dve¯e";
			typeNames["t_key"] = "KlÌË";
			typeNames["t_light_lit"] = "SvÌtÌcÌ svÏtlo";
			typeNames["t_light_out"] = "ZhasnutÈ svÏtlo";
			typeNames["t_food"] = "JÌdlo";
			typeNames["t_food_raw"] = "SyrovÈ JÌdlo";
			typeNames["t_armor"] = "BrnÏnÌ";
			typeNames["t_weapon_mace_smith"] = "Tup· ZbraÚ(n·stroj?)";
			typeNames["t_weapon_mace_sharp"] = "Tup· ZbraÚ";
			typeNames["t_weapon_sword"] = "Ostra ZbraÚ (tÏûk·)";
			typeNames["t_weapon_fence"] = "Ostr· ZbraÚ (lehk·)";
			typeNames["t_weapon_bow"] = "Luk";
			typeNames["t_wand"] = "H˘lka";
			typeNames["t_telepad"] = "Telepad";
			typeNames["t_switch"] = "P¯epÌnaË";
			typeNames["t_book"] = "Kniha";
			typeNames["t_rune"] = "Runa";
			typeNames["t_booze"] = "Alkohol";
			typeNames["t_potion"] = "Lektvar";
			typeNames["t_allpotions"] = "Lektvar";
			typeNames["t_speedpotions"] = "Lektvar";
			typeNames["t_potion_bomba"] = "Bomba";
			typeNames["t_fire"] = "OheÚ";
			typeNames["t_clock"] = "Hodiny";
			typeNames["t_trap"] = "Past";
			typeNames["t_trap_active"] = "AktivnÌ past";
			typeNames["t_musical"] = "HudebnÌ n·stroj";
			typeNames["t_spell"] = "Kouzlo";
			typeNames["t_gem"] = "Drahokam";
			typeNames["t_water"] = "Voda";
			typeNames["t_clothing"] = "ObleËenÌ";
			typeNames["t_scroll"] = "	Svitek kouzla";
			typeNames["t_carpentry"] = "Truhl·¯sk˝ n·stroj";
			typeNames["t_spawn_char"] = "Character spawner";
			typeNames["t_game_piece"] = "HracÌ figurka";
			typeNames["t_portculis"] = "Portculis?";
			typeNames["t_figurine"] = "Figurina(shrink)";
			typeNames["t_shrine"] = "Olt·¯(ress)";
			typeNames["t_moongate"] = "Moongate";
			typeNames["t_chair"] = "éidle";
			typeNames["t_forge"] = "V˝heÚ";
			typeNames["t_ore"] = "Ruda";
			typeNames["t_log"] = "Kmen";
			typeNames["t_tree"] = "Strom";
			typeNames["t_rock"] = "Sk·la";
			typeNames["t_carpentry_chop"] = "Not Used?";
			typeNames["t_multi"] = "Multiitem";
			typeNames["t_reagent"] = "Reagent";
			typeNames["t_ship"] = "LoÔ";
			typeNames["t_ship_plank"] = "LodnÌ m˘stek";
			typeNames["t_ship_side"] = "Bok lodi";
			typeNames["t_ship_side_locked"] = "ZamËen˝ bok lodi";
			typeNames["t_ship_tiller"] = "Lodivod";
			typeNames["t_eq_trade_window"] = "Secure trade item";
			typeNames["t_fish"] = "Ryba";
			typeNames["t_sign_gump"] = "Cedule";
			typeNames["t_stone_guild"] = "Guild stone";
			typeNames["t_anim_active"] = "Animace";
			typeNames["t_advance_gate"] = "Advance Gate";
			typeNames["t_cloth"] = "L·tka";
			typeNames["t_hair"] = "Vlasy";
			typeNames["t_beard"] = "Vousy";
			typeNames["t_ingot"] = "Ingot";
			typeNames["t_coin"] = "Mince";
			typeNames["t_crops"] = "Ÿroda";
			typeNames["t_drink"] = "PitÌ";
			typeNames["t_anvil"] = "Kovadlina";
			typeNames["t_port_locked"] = "ZamËen˝ port";
			typeNames["t_spawn_item"] = "Item spawner";
			typeNames["t_telescope"] = "Teleskop";
			typeNames["t_bed"] = "Postel";
			typeNames["t_gold"] = "PenÌze";
			typeNames["t_map"] = "Mapa";
			typeNames["t_eq_memory_obj"] = "equippable memory object";
			typeNames["t_weapon_mace_staff"] = "Tup· zbraÚ (hul)";
			typeNames["t_eq_horse"] = "Mount item (equipped horse)";
			typeNames["t_comm_crystal"] = "KomunkaËnÌ krystal";
			typeNames["t_game_board"] = "HernÌ deska";
			typeNames["t_trash_can"] = "Odpadkov˝ koö";
			typeNames["t_cannon_muzzle"] = "Kanon - prach?";
			typeNames["t_cannon"] = "Kanon";
			typeNames["t_cannon_ball"] = "DÏlov· koule";
			typeNames["t_armor_leather"] = "KoûenÈ brnÏnÌ";
			typeNames["t_seed"] = "Semena";
			typeNames["t_junk"] = "Odpad";
			typeNames["t_crystal_ball"] = "K¯Ìöù·lov· koule";
			//Typename_t_old_cashiers_check	
			typeNames["t_message"] = "Zpr·va";
			typeNames["t_reagent_raw"] = "'Syrov˝' reagent";
			typeNames["t_eq_client_linger"] = "udÏl· z klienta NPC";
			typeNames["t_dream_gate"] = "Br·na na jin˝ shard?";
			typeNames["t_it_stone"] = "Supply stone";
			typeNames["t_metronome"] = "Metronom";
			typeNames["t_explosion"] = "Exploze";
			typeNames["t_eq_npc_script"] = "EQuippable NPC script";
			typeNames["t_web"] = "PavouËÌ sÌù";
			typeNames["t_grass"] = "Tr·va";
			typeNames["t_arock"] = "K·men(h·zen obry)";
			typeNames["t_tracker"] = "Tracking obj";
			typeNames["t_sound"] = "Zvuk";
			typeNames["t_stone_town"] = "Townstone";
			typeNames["t_weapon_mace_crook"] = "Tup· zbraÚ (h·k)";
			typeNames["t_weapon_bow_run"] = "Run st¯eln· zbraÚ (/krump·Ë)";
			typeNames["t_leather"] = "K˘ûe";
			typeNames["t_ship_other"] = "»·st lodi?";
			typeNames["t_bboard"] = "»·st tabule";
			typeNames["t_spellbook"] = "Kouzeln· kniha";
			typeNames["t_corpse"] = "Mrtvola";
			typeNames["t_track_item"] = "Tracking item";
			typeNames["t_track_char"] = "Tracking character";
			typeNames["t_weapon_arrow"] = "äÌp";
			typeNames["t_weapon_bolt"] = "äipka";
			typeNames["t_weapon_bolt_jagged"] = "Zubat· öipka";
			typeNames["t_eq_vendor_box"] = "Vendor box";
			typeNames["t_eq_bank_box"] = "Bank box";
			typeNames["t_deed"] = "Deed";
			typeNames["t_loom"] = "Tkalcovsk˝ stav";
			typeNames["t_bee_hive"] = "VËelÌ ˙l";
			typeNames["t_archery_butte"] = "TerË";
			typeNames["t_eq_murder_count"] = "Killcount timer";
			typeNames["t_eq_stuck"] = "para v pavouËÌ sÌti";
			typeNames["t_trap_inactive"] = "NeaktivnÌ past";
			typeNames["t_stone_room"] = "Region stone?";
			typeNames["t_bandage"] = "Band·û(healing)";
			typeNames["t_campfire"] = "T·bor·k(camping)";
			typeNames["t_map_blank"] = "Pr·zdn· mapa";
			typeNames["t_spy_glass"] = "Dalekohled";
			typeNames["t_sextant"] = "Sextant";
			typeNames["t_scroll_blank"] = "Pr·zdn˝ svitek";
			typeNames["t_fruit"] = "Ovoce";
			typeNames["t_water_wash"] = "»ist· voda (bez ryb)";
			typeNames["t_weapon_axe"] = "Ostr· zbraÚ (sekera?)";
			typeNames["t_weapon_xbow"] = "KuËe";
			typeNames["t_spellicon"] = "Ikona kouzla";
			typeNames["t_door_open"] = "Otev¯enÈ dve¯e";
			typeNames["t_meat_raw"] = "SyrovÈ maso";
			typeNames["t_garbage"] = "Odpad";
			typeNames["t_keyring"] = "Krouûek na klÌËe";
			typeNames["t_table"] = "St˘l";
			typeNames["t_floor"] = "Podlaha";
			typeNames["t_roof"] = "St¯echa";
			typeNames["t_feather"] = "Pe¯Ì";
			typeNames["t_wool"] = "Vlna";
			typeNames["t_fur"] = "Srst";
			typeNames["t_blood"] = "Krev";
			typeNames["t_foliage"] = "jÌdlo?";
			typeNames["t_grain"] = "ZrnÌ";
			typeNames["t_scissors"] = "N˘ûky";
			typeNames["t_thread"] = "NitÏ";
			typeNames["t_yarn"] = "P¯Ìze";
			typeNames["t_spinwheel"] = "Kolovrat";
			typeNames["t_bandage_blood"] = "Krvav· band·û";
			typeNames["t_fish_pole"] = "Ryb·¯sk˝ prut";
			typeNames["t_shaft"] = "N·sada; D¯Ìk";
			typeNames["t_lockpick"] = "äperh·k";
			typeNames["t_kindling"] = "T¯Ìska";
			typeNames["t_train_dummy"] = "Pan·k k boji";
			typeNames["t_train_pickpocket"] = "Pan·k k okr·d·ni";
			typeNames["t_bedroll"] = "P¯ikr˝vka";
			typeNames["t_bellows"] = "Mech";
			typeNames["t_hide"] = "K˘ûe";
			typeNames["t_cloth_bolt"] = "BalÌk l·tky";
			typeNames["t_board"] = "Tr·m/prkno";
			typeNames["t_pitcher"] = "Dûb·n";
			typeNames["t_pitcher_empty"] = "Pr·zdn˝ dûb·n";
			typeNames["t_dye_vat"] = "N·doba na barvu";
			typeNames["t_dye"] = "Barva";
			typeNames["t_potion_empty"] = "Pr·zdn˝ lektvar";
			typeNames["t_mortar"] = "HmoûdÌ¯";
			typeNames["t_hair_dye"] = "Barva na vlasy";
			typeNames["t_sewing_kit"] = "äicÌ souprava";
			typeNames["t_tinker_tools"] = "Nastroje(tinkering)";
			typeNames["t_wall"] = "ZeÔ";
			typeNames["t_window"] = "Okno";
			typeNames["t_cotton"] = "Bavlna";
			typeNames["t_bone"] = "Kost";
			typeNames["t_eq_script"] = "Equippable script item";
			typeNames["t_ship_hold"] = "LodnÌ podpalubÌ";
			typeNames["t_ship_hold_lock"] = "ZamËene podpalubÌ";
			typeNames["t_lava"] = "L·va";
			typeNames["t_shield"] = "ätÌt";
			typeNames["t_jewelry"] = "äperk";
			typeNames["t_dirt"] = "äpÌna";
			typeNames["t_script"] = "Skript item";
			typeNames["t_eq_message"] = "Dialog item";
			typeNames["t_shovel"] = "Lopata";
			typeNames["t_book_shrink"] = "Recept shrink";
			typeNames["t_bottle_form"] = "Forma L·hev";
			typeNames["t_book_bottle"] = "Recept L·hev";
			typeNames["t_src_changer"] = "mÏnitko Src(script)";
			typeNames["t_book_inscription"] = "Kniha inscripce";
			typeNames["t_bottle_empty"] = "Pr·zdn· l·hev";			
		}

		[SavedMember]
		private static readonly Dictionary<SkillName, CraftmenuCategory> mainCategories = new Dictionary<SkillName, CraftmenuCategory>();
		
		//[SavedMember]
		//public static CraftmenuCategory categoryAlchemy = new CraftmenuCategory("Alchemy", null);
		//[SavedMember]
		//public static CraftmenuCategory categoryBlacksmithing = new CraftmenuCategory("Blacksmithing", null);
		//[SavedMember]
		//public static CraftmenuCategory categoryBowcraft = new CraftmenuCategory("Bowcraft", null);
		//[SavedMember]
		//public static CraftmenuCategory categoryCarpentry = new CraftmenuCategory("Carpentry", null);
		//[SavedMember]
		//public static CraftmenuCategory categoryCooking = new CraftmenuCategory("Cooking", null);
		//[SavedMember]
		//public static CraftmenuCategory categoryInscription = new CraftmenuCategory("Inscription", null);
		//[SavedMember]
		//public static CraftmenuCategory categoryTailoring = new CraftmenuCategory("Tailoring", null);
		//[SavedMember]
		//public static CraftmenuCategory categoryTinkering = new CraftmenuCategory("Tinkering", null);

		public static Dictionary<SkillName, CraftmenuCategory> MainCategories {
			get {
				if (mainCategories.Count == 0) {//yet empty - first access, initialize it
					//every other access will return an unempty dictionary as it will be initialized at least with te main categories
					//initialize the dictionary with categories
					mainCategories[SkillName.Alchemy] = new CraftmenuCategory("Alchemy");
					mainCategories[SkillName.Blacksmith] = new CraftmenuCategory("Blacksmithing");
					mainCategories[SkillName.Fletching] = new CraftmenuCategory("Bowcraft");
					mainCategories[SkillName.Carpentry] = new CraftmenuCategory("Carpentry");
					mainCategories[SkillName.Cooking] = new CraftmenuCategory("Cooking");
					mainCategories[SkillName.Inscribe] = new CraftmenuCategory("Inscription");
					mainCategories[SkillName.Tailoring] = new CraftmenuCategory("Tailoring");
					mainCategories[SkillName.Tinkering] = new CraftmenuCategory("Tinkering");
				}
				return mainCategories;
			}
		}

		[Summary("Get the category path ('cat1->cat2->cat3') and try to find and return the category specified")]
		public static CraftmenuCategory GetCategoryByPath(string path) {
			string[] catChain = path.Split(new string[] { "->" }, StringSplitOptions.None);
			CraftmenuCategory candidateCat = null;
			switch (catChain[0]) {
				case "Alchemy":
					candidateCat = MainCategories[SkillName.Alchemy];
					break;
				case "Blacksmithing":
					candidateCat = MainCategories[SkillName.Blacksmith];
					break;
				case "Bowcraft":
					candidateCat = MainCategories[SkillName.Fletching];
					break;
				case "Carpentry":
					candidateCat = MainCategories[SkillName.Carpentry];
					break;
				case "Cooking":
					candidateCat = MainCategories[SkillName.Cooking];
					break;
				case "Inscription":
					candidateCat = MainCategories[SkillName.Inscribe];
					break;
				case "Tailoring":
					candidateCat = MainCategories[SkillName.Tailoring];
					break;
				case "Tinkering":
					candidateCat = MainCategories[SkillName.Tinkering];
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

		//method for lazy loading the ICraftmenuElement parental info, used only when accessing some element's parent which is yet null
		internal static void TryLoadParents() {
			foreach (CraftmenuCategory mainCat in mainCategories.Values) { //these dont have Parents...
				ResolveChildParent(mainCat);
			}
		}

		//for the given category iterate through all of its children and set itself as parent to them
		//continue recursively into subcategories
		private static void ResolveChildParent(CraftmenuCategory ofWhat) {
			foreach (ICraftmenuElement elem in ofWhat.Contents) {
				CraftmenuCategory elemCat = elem as CraftmenuCategory;
				if (elemCat != null) {//we have the subcategory - resolve parents inside
					elemCat.Parent = ofWhat; //set the subcategory's parent
					ResolveChildParent(elemCat); //and recurse into children
				} else {//we have the item
					((CraftmenuItem) elem).Parent = ofWhat;
				}
			}
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

		void Bounce(AbstractItem whereto);
	}

	[SaveableClass]
	[Summary("Craftmenu category class. This is the entity where all Items from the menu are stored as well " +
			"as it is a container for another subcategories")]
	[ViewableClass]
	public class CraftmenuCategory : ICraftmenuElement {
		[SaveableData]
		public string name; //name of the category
		[SaveableData]
		public List<ICraftmenuElement> contents = new List<ICraftmenuElement>(); //itemdefs and subcategorties contained in the category
		private CraftmenuCategory parent = null;

		[LoadingInitializer]
		public CraftmenuCategory() {
		}

		public CraftmenuCategory(string name) {
			this.name = name;
		}

		[Summary("Get name compound also from the possible parent's name (if any)")]
		public string FullName {
			get {
				if (this.Parent == null) {
					return name;
				} else {
					return this.Parent.FullName + "->" + name;
				}
			}
		}

		public List<ICraftmenuElement> Contents {
			get {
				return contents;
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
				if (parent == null && !CraftmenuContents.MainCategories.ContainsValue(this)) {
					//if the parent is null and we aren't working with one of the main categories, try load all craftmenu elements' parental hierarchy
					CraftmenuContents.TryLoadParents();
				}
				return parent;
			}
			internal set {
				this.parent = value;
			}
		}

		[Summary("Category cleaning method - clear the contents and remove from the parent")]
		public void Remove() {
			if (this.Parent != null) {
				this.Parent.Contents.Remove(this);
				this.parent = null;
			}
			Contents.Clear();
		}

		[Summary("After removing the category from the craftmenu, create a pouch for it, put it into the specified location and bounce all inside items into it")]
		public void Bounce(AbstractItem whereto) {
			Item newPouch = (Item)ItemDef.Get("i_pouch").Create(whereto);
			newPouch.Name = this.Name;
			foreach (ICraftmenuElement innerElem in this.Contents) {
				innerElem.Bounce(newPouch);
			}
		}
		#endregion
	}

	[SaveableClass]
	[ViewableClass]
	[Summary("Craftmenu item class. This is the entity representing a single Item in the craftmenu")]
	public class CraftmenuItem : ICraftmenuElement {
		[SaveableData]
		public ItemDef itemDef;
		private CraftmenuCategory parent = null;

		[LoadingInitializer]
		public CraftmenuItem() {
		}

		public CraftmenuItem(ItemDef itemDef) {
			this.itemDef = itemDef;
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
				if (parent == null) {
					//if the parent is null try to load the parental hierarchy
					CraftmenuContents.TryLoadParents();
				}
				return parent;
			}
			internal set {
				this.parent = value;
			}
		}

		[Summary("Remove the item from the parent's list")]
		public void Remove() {
			if (this.Parent != null) {
				this.Parent.Contents.Remove(this);
				this.parent = null;
			}
		}

		[Summary("Bouncing of the item means creating an instance and putting to the specified location")]
		public void Bounce(AbstractItem whereto) {
			Item newItm = (Item)itemDef.Create(whereto);
		}
		#endregion
	}

	[Summary("Target for targetting single items or containers containing items to be added to the craftmenu (to the selected category)")]
	public class Targ_Craftmenu : CompiledTargetDef {
		protected override void On_Start(Player self, object parameter) {
			self.SysMessage("ZamÏ¯ p¯edmÏt p¯ÌpadnÏ konejner s p¯edmÏty pro p¯id·nÌ do craftmenu.");
			base.On_Start(self, parameter);
		}

		//put the given item into the category. if the item is a non-empty container, make it a subcategory
		//and add its contents recursively
		private void Encategorize(Item oneItm, CraftmenuCategory whereto) {
			if (oneItm.IsContainer) {
				if (oneItm.Count > 0) {//make it a subcategory
						//use the container's name for the category name - it is up to user to name all containers properly..
					CraftmenuCategory newSubcat = new CraftmenuCategory(oneItm.Name);
					newSubcat.Parent = whereto;
					whereto.Contents.Add(newSubcat);
					foreach (Item inner in oneItm) {
						Encategorize(inner, newSubcat);//proceed with every found item
					}
				} else {//empty container - it will be a single item...
					CraftmenuItem newItem = new CraftmenuItem((ItemDef) oneItm.Def);//add the contained items
					newItem.Parent = whereto;
					whereto.Contents.Add(newItem);
				}
			} else {//normal item
				CraftmenuItem newItem = new CraftmenuItem((ItemDef) oneItm.Def);//add the contained items
				newItem.Parent = whereto;
				whereto.Contents.Add(newItem);
			}
		}

		protected override bool On_TargonItem(Player self, Item targetted, object parameter) {
			CraftmenuCategory catToPut = (CraftmenuCategory) parameter;

			Encategorize(targetted, catToPut);

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