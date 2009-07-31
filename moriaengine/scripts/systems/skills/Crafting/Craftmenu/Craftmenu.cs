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
			typeNames["t_container_locked"] = "Zamèenı Kontejner";
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
			typeNames["t_weapon_sword"] = "Ostra Zbraò (tìká)";
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
			typeNames["t_carpentry"] = "Truhláøskı nástroj";
			typeNames["t_spawn_char"] = "Character spawner";
			typeNames["t_game_piece"] = "Hrací figurka";
			typeNames["t_portculis"] = "Portculis?";
			typeNames["t_figurine"] = "Figurina(shrink)";
			typeNames["t_shrine"] = "Oltáø(ress)";
			typeNames["t_moongate"] = "Moongate";
			typeNames["t_chair"] = "idle";
			typeNames["t_forge"] = "Vıheò";
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
			typeNames["t_ship_side_locked"] = "Zamèenı bok lodi";
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
			typeNames["t_port_locked"] = "Zamèenı port";
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
			typeNames["t_trash_can"] = "Odpadkovı koš";
			typeNames["t_cannon_muzzle"] = "Kanon - prach?";
			typeNames["t_cannon"] = "Kanon";
			typeNames["t_cannon_ball"] = "Dìlová koule";
			typeNames["t_armor_leather"] = "Koené brnìní";
			typeNames["t_seed"] = "Semena";
			typeNames["t_junk"] = "Odpad";
			typeNames["t_crystal_ball"] = "Køíšálová koule";
			//Typename_t_old_cashiers_check	
			typeNames["t_message"] = "Zpráva";
			typeNames["t_reagent_raw"] = "'Syrovı' reagent";
			typeNames["t_eq_client_linger"] = "udìlá z klienta NPC";
			typeNames["t_dream_gate"] = "Brána na jinı shard?";
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
			typeNames["t_leather"] = "Kùe";
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
			typeNames["t_loom"] = "Tkalcovskı stav";
			typeNames["t_bee_hive"] = "Vèelí úl";
			typeNames["t_archery_butte"] = "Terè";
			typeNames["t_eq_murder_count"] = "Killcount timer";
			typeNames["t_eq_stuck"] = "para v pavouèí síti";
			typeNames["t_trap_inactive"] = "Neaktivní past";
			typeNames["t_stone_room"] = "Region stone?";
			typeNames["t_bandage"] = "Bandá(healing)";
			typeNames["t_campfire"] = "Táborák(camping)";
			typeNames["t_map_blank"] = "Prázdná mapa";
			typeNames["t_spy_glass"] = "Dalekohled";
			typeNames["t_sextant"] = "Sextant";
			typeNames["t_scroll_blank"] = "Prázdnı svitek";
			typeNames["t_fruit"] = "Ovoce";
			typeNames["t_water_wash"] = "Èistá voda (bez ryb)";
			typeNames["t_weapon_axe"] = "Ostrá zbraò (sekera?)";
			typeNames["t_weapon_xbow"] = "Kuèe";
			typeNames["t_spellicon"] = "Ikona kouzla";
			typeNames["t_door_open"] = "Otevøené dveøe";
			typeNames["t_meat_raw"] = "Syrové maso";
			typeNames["t_garbage"] = "Odpad";
			typeNames["t_keyring"] = "Krouek na klíèe";
			typeNames["t_table"] = "Stùl";
			typeNames["t_floor"] = "Podlaha";
			typeNames["t_roof"] = "Støecha";
			typeNames["t_feather"] = "Peøí";
			typeNames["t_wool"] = "Vlna";
			typeNames["t_fur"] = "Srst";
			typeNames["t_blood"] = "Krev";
			typeNames["t_foliage"] = "jídlo?";
			typeNames["t_grain"] = "Zrní";
			typeNames["t_scissors"] = "Nùky";
			typeNames["t_thread"] = "Nitì";
			typeNames["t_yarn"] = "Pøíze";
			typeNames["t_spinwheel"] = "Kolovrat";
			typeNames["t_bandage_blood"] = "Krvavá bandá";
			typeNames["t_fish_pole"] = "Rybáøskı prut";
			typeNames["t_shaft"] = "Násada; Døík";
			typeNames["t_lockpick"] = "Šperhák";
			typeNames["t_kindling"] = "Tøíska";
			typeNames["t_train_dummy"] = "Panák k boji";
			typeNames["t_train_pickpocket"] = "Panák k okrádáni";
			typeNames["t_bedroll"] = "Pøikrıvka";
			typeNames["t_bellows"] = "Mech";
			typeNames["t_hide"] = "Kùe";
			typeNames["t_cloth_bolt"] = "Balík látky";
			typeNames["t_board"] = "Trám/prkno";
			typeNames["t_pitcher"] = "Dbán";
			typeNames["t_pitcher_empty"] = "Prázdnı dbán";
			typeNames["t_dye_vat"] = "Nádoba na barvu";
			typeNames["t_dye"] = "Barva";
			typeNames["t_potion_empty"] = "Prázdnı lektvar";
			typeNames["t_mortar"] = "Hmodíø";
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
		private static readonly Dictionary<SkillName, CraftmenuCategory> mainCategories = new Dictionary<SkillName, CraftmenuCategory>();
		
		public static Dictionary<SkillName, CraftmenuCategory> MainCategories {
			get {
				if (mainCategories.Count == 0) {//yet empty - first access, initialize it
					for (int i = 0, n = AbstractSkillDef.SkillsCount; i < n; i++) {
						CraftingSkillDef csk = AbstractSkillDef.GetById(i) as CraftingSkillDef;
						if (csk != null) {
							//add only Crafting skills...
							mainCategories[(SkillName)i] = new CraftmenuCategory(csk);
						}
					}
				}
				return mainCategories;
			}
		}		

		//method for lazy loading the ICraftmenuElement parental info, used only when accessing some element's parent which is yet null
		internal static void TryLoadParents() {
			foreach (CraftmenuCategory mainCat in mainCategories.Values) { //these dont have Parents...
				ResolveChildParent(mainCat);
				mainCat.Parent = mainCat; //main categories will have themselves as parents...
				mainCat.categorySkill = (CraftingSkillDef)AbstractSkillDef.GetByKey(mainCat.Name); //main categories have the skill Key as their name
				mainCat.isLoaded = true; //loaded, now if the parental reference is null then the category should be taken as deleted
			}
		}

		//for the given category iterate through all of its children and set itself as parent to them
		//continue recursively into subcategories
		private static void ResolveChildParent(CraftmenuCategory ofWhat) {
			foreach (ICraftmenuElement elem in ofWhat.Contents) {
				CraftmenuCategory elemCat = elem as CraftmenuCategory;
				if (elemCat != null) {//we have the subcategory - resolve parents inside
					elemCat.Parent = ofWhat; //set the subcategory's parent
					elemCat.isLoaded = true; //loaded, now if the parental reference is null then the category should be taken as deleted
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
	public class CraftmenuCategory : ICraftmenuElement, IDeletable {
		internal bool isLoaded = false;

		//lazily loaded reference to the crafting skill connected to this category (main categories only)
		internal CraftingSkillDef categorySkill;
		
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

		//constructor used for main categories only
		internal CraftmenuCategory(CraftingSkillDef csd) : this(csd.Key) {
			this.categorySkill = csd;
		}

		[Summary("Get name compound also from the possible parent's name (if any)")]
		public string FullName {
			get {
				ThrowIfDeleted();
				if (this.Parent == this) {//main Categories have Parental reference on themselves
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

		public bool IsLoaded {
			get {
				return isLoaded;
			}
		}

		[Summary("Return the (grand)parent from this category that lies on the first hierarchy level")]
		public CraftmenuCategory MainParent {
			get {
				if (this.Parent == this) {
					return this;
				} else {
					return this.Parent.MainParent;
				}
			}
		}

		[Summary("Return the category's skill which is used for crafting items from it")]
		public CraftingSkillDef CategorySkill {
			get {
				return this.MainParent.categorySkill;
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
				if (!isLoaded) {
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
				this.Parent.Contents.Remove(this); //remove from the parent's hierarchy list
			}
			foreach(ICraftmenuElement subElem in contents) {
				subElem.Remove();//remove every element in the removed category (incl. subcategories)
			}
			Delete(); //will clear the reference to the parent and disable the overall usage as favourite category etc.
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
	
		#region IDeletable Members

		public bool IsDeleted {
			get { 
				return (isLoaded == true && parent == null);//has been loaded but the parent is null? (this can happen only if the category was deleted)
			}
		}

		public void Delete() {
			//method is called when the category is removed from the categories hierarchy
			//but there can exist references on it (favourite categroy etc...) so we need to mark it somehow
			//in order to disable its usage anymore
			isLoaded = true; //consider it as loaded (but in time of deleting it should be loaded anyways)
 			parent = null;
		}
		#endregion

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		public void ThrowIfDeleted() {
			if (this.IsDeleted) {
				throw new DeletedException("Invalid usage of deleted Craftmenu Category (" + this + ")");
			}
		} 
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
			self.SysMessage("Zamìø pøedmìt pøípadnì konejner s pøedmìty pro pøidání do craftmenu.");
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
			CraftmenuCategory prevCat = (CraftmenuCategory) self.GetTag(D_Craftmenu.tkCraftmenuLastpos);
			if (prevCat != null) {
				//not null means that the categroy was not deleted and can be accessed again
				self.Dialog(SingletonScript<D_Craftmenu>.Instance, new DialogArgs(prevCat));
			} else {
				//null means that it either not existed (the tag) or the categroy was deleted from the menu
				self.Dialog(SingletonScript<D_CraftmenuCategories>.Instance);
			}
			return false;
		}
	}

	[Summary("Class for describing one of the selected items and its count to be crafted")]
	public class CraftingSelection {
		private readonly ItemDef itemDef;
		private int count;

		public CraftingSelection(ItemDef itemDef, int count) {
			this.itemDef = itemDef;
			this.count = count;
		}

		public ItemDef ItemDef {
			get {
				return this.itemDef;
			}
		}

		public int Count {
			get {
				return this.count;
			}
			set {
				this.count = value;
			}
		}
	}
}