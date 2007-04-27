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

namespace SteamEngine {
	enum layers : int {
		layer_hand1=1,			//one-handed weapons or tools
		layer_hand2=2,			//two-handed weapons or tools, or shields or one-handed accessory items (like torches)
		layer_shoes=3,	
		layer_pants=4,			//Also some kinds of armor
		layer_shirt=5,
		layer_helm=6,			//or hats, etc
		layer_gloves=7,
		layer_ring=8,			//only one, oddly
		layer_light=9,  		//Apparently this is where you put i_light_source or i_dark_source. Does i_dark_source work?
		layer_collar=10,		//Necklace, Gorget, Mempo, etc.
			layer_gorget=10,
		layer_hair=11,
		layer_half_apron=12,
		layer_chest=13,			//Primarily for armor
		layer_wrist=14,			//Bracelets.
		layer_hidden=15,		//Apparently you can equip animation items (i_fx_*) here - that would probably give you an endlessly repeating animation.
		layer_beard=16,
		layer_tunic=17,
		layer_ears=18,			//Earrings
		layer_arms=19,			//For various armor's arms
		layer_cape=20,			//Cape/Cloak/Etc
		layer_pack=21,			//For the backpack.
		layer_robe=22,			//Robe, death shroud, hooded robe, etc
		layer_skirt=23,
		layer_legs=24,			//Platemail in particular
		layer_horse=25,			//Or any mount
			layer_mount=25,
		layer_vendor_stock=26,	//Items we restock automatically, and sell. (Price, amount, amount when fully stocked)
		layer_vendor_extra=27,	//Items given or sold to us by players, which we will re-sell (Price, amount)
		layer_vendor_buys=28,	//Examples of items that we will buy. (Price, max amount we want to have?)
		layer_bankbox=29,
		layer_special=30,		//Used in sphere for memory items, timer items, etc.
		layer_dragging=31,		//Used when dragging stuff around.
	}
}