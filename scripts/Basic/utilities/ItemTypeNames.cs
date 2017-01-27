/*
	This program is free software); you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation); either version 2 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY); without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program); if not, write to the Free Software
	Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
	Or visit http://www.gnu.org/copyleft/gpl.html
*/

using System;
using System.Collections.Generic;
using SteamEngine.Scripting.Objects;

namespace SteamEngine.CompiledScripts {

	public static class ItemTypeNames {
		private static readonly Dictionary<string, string> typeNames = CreateDict();

		static Dictionary<string, string> CreateDict() {
			var dict = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

			//fill the dictionary with names of various item types
			dict.Add("00", "Normal");
			dict.Add("t_normal", "Normal");
			dict.Add("t_container", "Kontejner");
			dict.Add("t_container_locked", "Zam�en� Kontejner");
			dict.Add("t_door", "Dve�e");
			dict.Add("t_door_locked", "Zam�en� dve�e");
			dict.Add("t_key", "Kl��");
			dict.Add("t_light_lit", "Sv�t�c� sv�tlo");
			dict.Add("t_light_out", "Zhasnut� sv�tlo");
			dict.Add("t_food", "J�dlo");
			dict.Add("t_food_raw", "Syrov� J�dlo");
			dict.Add("t_armor", "Brn�n�");
			dict.Add("t_weapon_mace_smith", "Tup� Zbra�(n�stroj?)");
			dict.Add("t_weapon_mace_sharp", "Tup� Zbra�");
			dict.Add("t_weapon_sword", "Ostra Zbra� (t�k�)");
			dict.Add("t_weapon_fence", "Ostr� Zbra� (lehk�)");
			dict.Add("t_weapon_bow", "Luk");
			dict.Add("t_wand", "H�lka");
			dict.Add("t_telepad", "Telepad");
			dict.Add("t_switch", "P�ep�na�");
			dict.Add("t_book", "Kniha");
			dict.Add("t_rune", "Runa");
			dict.Add("t_booze", "Alkohol");
			dict.Add("t_potion", "Lektvar");
			dict.Add("t_allpotions", "Lektvar");
			dict.Add("t_speedpotions", "Lektvar");
			dict.Add("t_potion_bomba", "Bomba");
			dict.Add("t_fire", "Ohe�");
			dict.Add("t_clock", "Hodiny");
			dict.Add("t_trap", "Past");
			dict.Add("t_trap_active", "Aktivn� past");
			dict.Add("t_musical", "Hudebn� n�stroj");
			dict.Add("t_spell", "Kouzlo");
			dict.Add("t_gem", "Drahokam");
			dict.Add("t_water", "Voda");
			dict.Add("t_clothing", "Oble�en�");
			dict.Add("t_scroll", "	Svitek kouzla");
			dict.Add("t_carpentry", "Truhl��sk� n�stroj");
			dict.Add("t_spawn_char", "Character spawner");
			dict.Add("t_game_piece", "Hrac� figurka");
			dict.Add("t_portculis", "Portculis?");
			dict.Add("t_figurine", "Figurina(shrink)");
			dict.Add("t_shrine", "Olt��(ress)");
			dict.Add("t_moongate", "Moongate");
			dict.Add("t_chair", "�idle");
			dict.Add("t_forge", "V�he�");
			dict.Add("t_ore", "Ruda");
			dict.Add("t_log", "Kmen");
			dict.Add("t_tree", "Strom");
			dict.Add("t_rock", "Sk�la");
			dict.Add("t_carpentry_chop", "Not Used?");
			dict.Add("t_multi", "Multiitem");
			dict.Add("t_reagent", "Reagent");
			dict.Add("t_ship", "Lo�");
			dict.Add("t_ship_plank", "Lodn� m�stek");
			dict.Add("t_ship_side", "Bok lodi");
			dict.Add("t_ship_side_locked", "Zam�en� bok lodi");
			dict.Add("t_ship_tiller", "Lodivod");
			dict.Add("t_eq_trade_window", "Secure trade item");
			dict.Add("t_fish", "Ryba");
			dict.Add("t_sign_gump", "Cedule");
			dict.Add("t_stone_guild", "Guild stone");
			dict.Add("t_anim_active", "Animace");
			dict.Add("t_advance_gate", "Advance Gate");
			dict.Add("t_cloth", "L�tka");
			dict.Add("t_hair", "Vlasy");
			dict.Add("t_beard", "Vousy");
			dict.Add("t_ingot", "Ingot");
			dict.Add("t_coin", "Mince");
			dict.Add("t_crops", "�roda");
			dict.Add("t_drink", "Pit�");
			dict.Add("t_anvil", "Kovadlina");
			dict.Add("t_port_locked", "Zam�en� port");
			dict.Add("t_spawn_item", "Item spawner");
			dict.Add("t_telescope", "Teleskop");
			dict.Add("t_bed", "Postel");
			dict.Add("t_gold", "Pen�ze");
			dict.Add("t_map", "Mapa");
			dict.Add("t_eq_memory_obj", "equippable memory object");
			dict.Add("t_weapon_mace_staff", "Tup� zbra� (hul)");
			dict.Add("t_eq_horse", "Mount item (equipped horse)");
			dict.Add("t_comm_crystal", "Komunka�n� krystal");
			dict.Add("t_game_board", "Hern� deska");
			dict.Add("t_trash_can", "Odpadkov� ko�");
			dict.Add("t_cannon_muzzle", "Kanon - prach?");
			dict.Add("t_cannon", "Kanon");
			dict.Add("t_cannon_ball", "D�lov� koule");
			dict.Add("t_armor_leather", "Ko�en� brn�n�");
			dict.Add("t_seed", "Semena");
			dict.Add("t_junk", "Odpad");
			dict.Add("t_crystal_ball", "K�횝�lov� koule");
			//t_old_cashiers_check	
			dict.Add("t_message", "Zpr�va");
			dict.Add("t_reagent_raw", "'Syrov�' reagent");
			dict.Add("t_eq_client_linger", "ud�l� z klienta NPC");
			dict.Add("t_dream_gate", "Br�na na jin� shard?");
			dict.Add("t_it_stone", "Supply stone");
			dict.Add("t_metronome", "Metronom");
			dict.Add("t_explosion", "Exploze");
			dict.Add("t_eq_npc_script", "EQuippable NPC script");
			dict.Add("t_web", "Pavou�� s�");
			dict.Add("t_grass", "Tr�va");
			dict.Add("t_arock", "K�men(h�zen obry)");
			dict.Add("t_tracker", "Tracking obj");
			dict.Add("t_sound", "Zvuk");
			dict.Add("t_stone_town", "Townstone");
			dict.Add("t_weapon_mace_crook", "Tup� zbra� (h�k)");
			dict.Add("t_weapon_bow_run", "Run st�eln� zbra� (/krump��)");
			dict.Add("t_leather", "K��e");
			dict.Add("t_ship_other", "��st lodi?");
			dict.Add("t_bboard", "��st tabule");
			dict.Add("t_spellbook", "Kouzeln� kniha");
			dict.Add("t_corpse", "Mrtvola");
			dict.Add("t_track_item", "Tracking item");
			dict.Add("t_track_char", "Tracking character");
			dict.Add("t_weapon_arrow", "��p");
			dict.Add("t_weapon_bolt", "�ipka");
			dict.Add("t_weapon_bolt_jagged", "Zubat� �ipka");
			dict.Add("t_eq_vendor_box", "Vendor box");
			dict.Add("t_eq_bank_box", "Bank box");
			dict.Add("t_deed", "Deed");
			dict.Add("t_loom", "Tkalcovsk� stav");
			dict.Add("t_bee_hive", "V�el� �l");
			dict.Add("t_archery_butte", "Ter�");
			dict.Add("t_eq_murder_count", "Killcount timer");
			dict.Add("t_eq_stuck", "para v pavou�� s�ti");
			dict.Add("t_trap_inactive", "Neaktivn� past");
			dict.Add("t_stone_room", "Region stone?");
			dict.Add("t_bandage", "Band�(healing)");
			dict.Add("t_campfire", "T�bor�k(camping)");
			dict.Add("t_map_blank", "Pr�zdn� mapa");
			dict.Add("t_spy_glass", "Dalekohled");
			dict.Add("t_sextant", "Sextant");
			dict.Add("t_scroll_blank", "Pr�zdn� svitek");
			dict.Add("t_fruit", "Ovoce");
			dict.Add("t_water_wash", "�ist� voda (bez ryb)");
			dict.Add("t_weapon_axe", "Ostr� zbra� (sekera?)");
			dict.Add("t_weapon_xbow", "Ku�e");
			dict.Add("t_spellicon", "Ikona kouzla");
			dict.Add("t_door_open", "Otev�en� dve�e");
			dict.Add("t_meat_raw", "Syrov� maso");
			dict.Add("t_garbage", "Odpad");
			dict.Add("t_keyring", "Krou�ek na kl��e");
			dict.Add("t_table", "St�l");
			dict.Add("t_floor", "Podlaha");
			dict.Add("t_roof", "St�echa");
			dict.Add("t_feather", "Pe��");
			dict.Add("t_wool", "Vlna");
			dict.Add("t_fur", "Srst");
			dict.Add("t_blood", "Krev");
			dict.Add("t_foliage", "j�dlo?");
			dict.Add("t_grain", "Zrn�");
			dict.Add("t_scissors", "N��ky");
			dict.Add("t_thread", "Nit�");
			dict.Add("t_yarn", "P��ze");
			dict.Add("t_spinwheel", "Kolovrat");
			dict.Add("t_bandage_blood", "Krvav� band�");
			dict.Add("t_fish_pole", "Ryb��sk� prut");
			dict.Add("t_shaft", "N�sada); D��k");
			dict.Add("t_lockpick", "�perh�k");
			dict.Add("t_kindling", "T��ska");
			dict.Add("t_train_dummy", "Pan�k k boji");
			dict.Add("t_train_pickpocket", "Pan�k k okr�d�ni");
			dict.Add("t_bedroll", "P�ikr�vka");
			dict.Add("t_bellows", "Mech");
			dict.Add("t_hide", "K��e");
			dict.Add("t_cloth_bolt", "Bal�k l�tky");
			dict.Add("t_board", "Tr�m/prkno");
			dict.Add("t_pitcher", "D�b�n");
			dict.Add("t_pitcher_empty", "Pr�zdn� d�b�n");
			dict.Add("t_dye_vat", "N�doba na barvu");
			dict.Add("t_dye", "Barva");
			dict.Add("t_potion_empty", "Pr�zdn� lektvar");
			dict.Add("t_mortar", "Hmo�d��");
			dict.Add("t_hair_dye", "Barva na vlasy");
			dict.Add("t_sewing_kit", "�ic� souprava");
			dict.Add("t_tinker_tools", "Nastroje(tinkering)");
			dict.Add("t_wall", "Ze�");
			dict.Add("t_window", "Okno");
			dict.Add("t_cotton", "Bavlna");
			dict.Add("t_bone", "Kost");
			dict.Add("t_eq_script", "Equippable script item");
			dict.Add("t_ship_hold", "Lodn� podpalub�");
			dict.Add("t_ship_hold_lock", "Zam�ene podpalub�");
			dict.Add("t_lava", "L�va");
			dict.Add("t_shield", "�t�t");
			dict.Add("t_jewelry", "�perk");
			dict.Add("t_dirt", "�p�na");
			dict.Add("t_script", "Skript item");
			dict.Add("t_eq_message", "Dialog item");
			dict.Add("t_shovel", "Lopata");
			dict.Add("t_book_shrink", "Recept shrink");
			dict.Add("t_bottle_form", "Forma L�hev");
			dict.Add("t_book_bottle", "Recept L�hev");
			dict.Add("t_src_changer", "m�nitko Src(script)");
			dict.Add("t_book_inscription", "Kniha inscripce");
			dict.Add("t_bottle_empty", "Pr�zdn� l�hev");

			return dict;
		}

		public static string GetPrettyName(TriggerGroup tg) {
			string name;
			var dn = tg.PrettyDefname;
			if (typeNames.TryGetValue(dn, out name)) {
				return name;
			}
			return dn;
		}

		public static string GetPrettyName(string itemTypeDefname) {
			string name;
			if (typeNames.TryGetValue(itemTypeDefname, out name)) {
				return name;
			}
			return itemTypeDefname;
		}
	}
}