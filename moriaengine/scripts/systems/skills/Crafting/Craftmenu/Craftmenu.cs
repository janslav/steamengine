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
			typeNames["t_container_locked"] = "Zam�en� Kontejner";
			typeNames["t_door"] = "Dve�e";
			typeNames["t_door_locked"] = "Zam�en� dve�e";
			typeNames["t_key"] = "Kl��";
			typeNames["t_light_lit"] = "Sv�t�c� sv�tlo";
			typeNames["t_light_out"] = "Zhasnut� sv�tlo";
			typeNames["t_food"] = "J�dlo";
			typeNames["t_food_raw"] = "Syrov� J�dlo";
			typeNames["t_armor"] = "Brn�n�";
			typeNames["t_weapon_mace_smith"] = "Tup� Zbra�(n�stroj?)";
			typeNames["t_weapon_mace_sharp"] = "Tup� Zbra�";
			typeNames["t_weapon_sword"] = "Ostra Zbra� (t�k�)";
			typeNames["t_weapon_fence"] = "Ostr� Zbra� (lehk�)";
			typeNames["t_weapon_bow"] = "Luk";
			typeNames["t_wand"] = "H�lka";
			typeNames["t_telepad"] = "Telepad";
			typeNames["t_switch"] = "P�ep�na�";
			typeNames["t_book"] = "Kniha";
			typeNames["t_rune"] = "Runa";
			typeNames["t_booze"] = "Alkohol";
			typeNames["t_potion"] = "Lektvar";
			typeNames["t_allpotions"] = "Lektvar";
			typeNames["t_speedpotions"] = "Lektvar";
			typeNames["t_potion_bomba"] = "Bomba";
			typeNames["t_fire"] = "Ohe�";
			typeNames["t_clock"] = "Hodiny";
			typeNames["t_trap"] = "Past";
			typeNames["t_trap_active"] = "Aktivn� past";
			typeNames["t_musical"] = "Hudebn� n�stroj";
			typeNames["t_spell"] = "Kouzlo";
			typeNames["t_gem"] = "Drahokam";
			typeNames["t_water"] = "Voda";
			typeNames["t_clothing"] = "Oble�en�";
			typeNames["t_scroll"] = "	Svitek kouzla";
			typeNames["t_carpentry"] = "Truhl��sk� n�stroj";
			typeNames["t_spawn_char"] = "Character spawner";
			typeNames["t_game_piece"] = "Hrac� figurka";
			typeNames["t_portculis"] = "Portculis?";
			typeNames["t_figurine"] = "Figurina(shrink)";
			typeNames["t_shrine"] = "Olt��(ress)";
			typeNames["t_moongate"] = "Moongate";
			typeNames["t_chair"] = "�idle";
			typeNames["t_forge"] = "V�he�";
			typeNames["t_ore"] = "Ruda";
			typeNames["t_log"] = "Kmen";
			typeNames["t_tree"] = "Strom";
			typeNames["t_rock"] = "Sk�la";
			typeNames["t_carpentry_chop"] = "Not Used?";
			typeNames["t_multi"] = "Multiitem";
			typeNames["t_reagent"] = "Reagent";
			typeNames["t_ship"] = "Lo�";
			typeNames["t_ship_plank"] = "Lodn� m�stek";
			typeNames["t_ship_side"] = "Bok lodi";
			typeNames["t_ship_side_locked"] = "Zam�en� bok lodi";
			typeNames["t_ship_tiller"] = "Lodivod";
			typeNames["t_eq_trade_window"] = "Secure trade item";
			typeNames["t_fish"] = "Ryba";
			typeNames["t_sign_gump"] = "Cedule";
			typeNames["t_stone_guild"] = "Guild stone";
			typeNames["t_anim_active"] = "Animace";
			typeNames["t_advance_gate"] = "Advance Gate";
			typeNames["t_cloth"] = "L�tka";
			typeNames["t_hair"] = "Vlasy";
			typeNames["t_beard"] = "Vousy";
			typeNames["t_ingot"] = "Ingot";
			typeNames["t_coin"] = "Mince";
			typeNames["t_crops"] = "�roda";
			typeNames["t_drink"] = "Pit�";
			typeNames["t_anvil"] = "Kovadlina";
			typeNames["t_port_locked"] = "Zam�en� port";
			typeNames["t_spawn_item"] = "Item spawner";
			typeNames["t_telescope"] = "Teleskop";
			typeNames["t_bed"] = "Postel";
			typeNames["t_gold"] = "Pen�ze";
			typeNames["t_map"] = "Mapa";
			typeNames["t_eq_memory_obj"] = "equippable memory object";
			typeNames["t_weapon_mace_staff"] = "Tup� zbra� (hul)";
			typeNames["t_eq_horse"] = "Mount item (equipped horse)";
			typeNames["t_comm_crystal"] = "Komunka�n� krystal";
			typeNames["t_game_board"] = "Hern� deska";
			typeNames["t_trash_can"] = "Odpadkov� ko�";
			typeNames["t_cannon_muzzle"] = "Kanon - prach?";
			typeNames["t_cannon"] = "Kanon";
			typeNames["t_cannon_ball"] = "D�lov� koule";
			typeNames["t_armor_leather"] = "Ko�en� brn�n�";
			typeNames["t_seed"] = "Semena";
			typeNames["t_junk"] = "Odpad";
			typeNames["t_crystal_ball"] = "K�횝�lov� koule";
			//Typename_t_old_cashiers_check	
			typeNames["t_message"] = "Zpr�va";
			typeNames["t_reagent_raw"] = "'Syrov�' reagent";
			typeNames["t_eq_client_linger"] = "ud�l� z klienta NPC";
			typeNames["t_dream_gate"] = "Br�na na jin� shard?";
			typeNames["t_it_stone"] = "Supply stone";
			typeNames["t_metronome"] = "Metronom";
			typeNames["t_explosion"] = "Exploze";
			typeNames["t_eq_npc_script"] = "EQuippable NPC script";
			typeNames["t_web"] = "Pavou�� s�";
			typeNames["t_grass"] = "Tr�va";
			typeNames["t_arock"] = "K�men(h�zen obry)";
			typeNames["t_tracker"] = "Tracking obj";
			typeNames["t_sound"] = "Zvuk";
			typeNames["t_stone_town"] = "Townstone";
			typeNames["t_weapon_mace_crook"] = "Tup� zbra� (h�k)";
			typeNames["t_weapon_bow_run"] = "Run st�eln� zbra� (/krump��)";
			typeNames["t_leather"] = "K��e";
			typeNames["t_ship_other"] = "��st lodi?";
			typeNames["t_bboard"] = "��st tabule";
			typeNames["t_spellbook"] = "Kouzeln� kniha";
			typeNames["t_corpse"] = "Mrtvola";
			typeNames["t_track_item"] = "Tracking item";
			typeNames["t_track_char"] = "Tracking character";
			typeNames["t_weapon_arrow"] = "��p";
			typeNames["t_weapon_bolt"] = "�ipka";
			typeNames["t_weapon_bolt_jagged"] = "Zubat� �ipka";
			typeNames["t_eq_vendor_box"] = "Vendor box";
			typeNames["t_eq_bank_box"] = "Bank box";
			typeNames["t_deed"] = "Deed";
			typeNames["t_loom"] = "Tkalcovsk� stav";
			typeNames["t_bee_hive"] = "V�el� �l";
			typeNames["t_archery_butte"] = "Ter�";
			typeNames["t_eq_murder_count"] = "Killcount timer";
			typeNames["t_eq_stuck"] = "para v pavou�� s�ti";
			typeNames["t_trap_inactive"] = "Neaktivn� past";
			typeNames["t_stone_room"] = "Region stone?";
			typeNames["t_bandage"] = "Band�(healing)";
			typeNames["t_campfire"] = "T�bor�k(camping)";
			typeNames["t_map_blank"] = "Pr�zdn� mapa";
			typeNames["t_spy_glass"] = "Dalekohled";
			typeNames["t_sextant"] = "Sextant";
			typeNames["t_scroll_blank"] = "Pr�zdn� svitek";
			typeNames["t_fruit"] = "Ovoce";
			typeNames["t_water_wash"] = "�ist� voda (bez ryb)";
			typeNames["t_weapon_axe"] = "Ostr� zbra� (sekera?)";
			typeNames["t_weapon_xbow"] = "Ku�e";
			typeNames["t_spellicon"] = "Ikona kouzla";
			typeNames["t_door_open"] = "Otev�en� dve�e";
			typeNames["t_meat_raw"] = "Syrov� maso";
			typeNames["t_garbage"] = "Odpad";
			typeNames["t_keyring"] = "Krou�ek na kl��e";
			typeNames["t_table"] = "St�l";
			typeNames["t_floor"] = "Podlaha";
			typeNames["t_roof"] = "St�echa";
			typeNames["t_feather"] = "Pe��";
			typeNames["t_wool"] = "Vlna";
			typeNames["t_fur"] = "Srst";
			typeNames["t_blood"] = "Krev";
			typeNames["t_foliage"] = "j�dlo?";
			typeNames["t_grain"] = "Zrn�";
			typeNames["t_scissors"] = "N��ky";
			typeNames["t_thread"] = "Nit�";
			typeNames["t_yarn"] = "P��ze";
			typeNames["t_spinwheel"] = "Kolovrat";
			typeNames["t_bandage_blood"] = "Krvav� band�";
			typeNames["t_fish_pole"] = "Ryb��sk� prut";
			typeNames["t_shaft"] = "N�sada; D��k";
			typeNames["t_lockpick"] = "�perh�k";
			typeNames["t_kindling"] = "T��ska";
			typeNames["t_train_dummy"] = "Pan�k k boji";
			typeNames["t_train_pickpocket"] = "Pan�k k okr�d�ni";
			typeNames["t_bedroll"] = "P�ikr�vka";
			typeNames["t_bellows"] = "Mech";
			typeNames["t_hide"] = "K��e";
			typeNames["t_cloth_bolt"] = "Bal�k l�tky";
			typeNames["t_board"] = "Tr�m/prkno";
			typeNames["t_pitcher"] = "D�b�n";
			typeNames["t_pitcher_empty"] = "Pr�zdn� d�b�n";
			typeNames["t_dye_vat"] = "N�doba na barvu";
			typeNames["t_dye"] = "Barva";
			typeNames["t_potion_empty"] = "Pr�zdn� lektvar";
			typeNames["t_mortar"] = "Hmo�d��";
			typeNames["t_hair_dye"] = "Barva na vlasy";
			typeNames["t_sewing_kit"] = "�ic� souprava";
			typeNames["t_tinker_tools"] = "Nastroje(tinkering)";
			typeNames["t_wall"] = "Ze�";
			typeNames["t_window"] = "Okno";
			typeNames["t_cotton"] = "Bavlna";
			typeNames["t_bone"] = "Kost";
			typeNames["t_eq_script"] = "Equippable script item";
			typeNames["t_ship_hold"] = "Lodn� podpalub�";
			typeNames["t_ship_hold_lock"] = "Zam�ene podpalub�";
			typeNames["t_lava"] = "L�va";
			typeNames["t_shield"] = "�t�t";
			typeNames["t_jewelry"] = "�perk";
			typeNames["t_dirt"] = "�p�na";
			typeNames["t_script"] = "Skript item";
			typeNames["t_eq_message"] = "Dialog item";
			typeNames["t_shovel"] = "Lopata";
			typeNames["t_book_shrink"] = "Recept shrink";
			typeNames["t_bottle_form"] = "Forma L�hev";
			typeNames["t_book_bottle"] = "Recept L�hev";
			typeNames["t_src_changer"] = "m�nitko Src(script)";
			typeNames["t_book_inscription"] = "Kniha inscripce";
			typeNames["t_bottle_empty"] = "Pr�zdn� l�hev";
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
			self.SysMessage("Zam�� p�edm�t p��padn� konejner s p�edm�ty pro p�id�n� do craftmenu.");
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