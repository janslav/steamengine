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
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {

	[Remark("Numbers of important hues")]
	public enum Hues : int {
		Red=33,
		Blue=99,
		Green=68,
		Info=642, //shit color :) (dark yellow-green-brown undefinable) :-/ its hard to choose
		//text colors
		PageRepliedColor=1740, //some other orange shit (used for labeling replied pages)
		WriteColor=2300,
		PlayerColor=2301,//color for players name in Admin dialog (until the coloring players is solved)
		WriteColor2=0481,//tmave tyrkysova
		ReadColor=2303,
		NAColor=2305,

		OnlineColor=2301,//color for highlighting online accounts/players
		OfflineColor = 0481,//color for highlighting offline accounts/players

		LabelColor = 1675, //nadpisy (sloupecku atd) - svetlounce bezova
		HeadlineColor = 1152, //titulek (obvykle celeho dialogu atd)

		SettingsTitleColor=2413, //the settings category color
		SettingsNormalColor=2300, //normal settings items
		SettingsFailedColor=0x25 //setting items that weren't possible to be set
	}

	[Remark("Various sorting criteria used in different dialogs")]
	public enum SortingCriteria : int {
		NameAsc,
		NameDesc,
		AccountAsc,
		AccountDesc,
		LocationAsc,
		LocationDesc,
		TimeAsc,
		TimeDesc,
		IPAsc,
		IPDesc,
		UnreadAsc,
		UnreadDesc
	}

	[Remark("Various types of GUTA Leaf Components")]
	public enum LeafComponentTypes : int {
		//Buttons
		[Remark("Button with the big X inside")]
		ButtonCross,
		[Remark("Button with the OK inside")]
		ButtonOK,
		[Remark("Button with the tick inside")]
		ButtonTick,
		[Remark("Button with the sheet of paper inside")]
		ButtonPaper,
		[Remark("Button with flying paper")]
		ButtonSend,
		[Remark("Button for sorting (small up arrow)")]
		ButtonSortUp,
		[Remark("Button for sorting (small down arrow)")]
		ButtonSortDown,
		[Remark("Medium UP arrow")]
		ButtonPrev,
		[Remark("Medium DOWN arrow")]
		ButtonNext,
		[Remark("Button with people")]
		ButtonPeople,
		CheckBox,
		RadioButton,
		//Inputs
		InputText,
		InputNumber
	}

	public enum SettingsDisplay {
		[Remark("Zobrazit vsechny kategorie v jednom dialogu")]
		All,
		[Remark("Zobrazit jen jednu vybranou kategorii")]
		Single
	}

	[Flags]
	[Remark("Urcuje, jakej rezist se ma aplikovat na dotycny damage.")]
	public enum DamageType : int {
		[Remark("Damage neredukovano")]
		Irresistable=0x000000,
		[Remark("Damage redukovano magickym rezistem (neplest s obranou mysli), i kdyz mozna nic takovyho neexistuje ;)")]
		Magic=0x000001,
		MagicFire=Magic|0x000004,
		Electric=Magic|0x000008,
		Acid=Magic|0x000010,
		Cold=Magic|0x000020,
		MagicPoison=Magic|0x000040,
		[Remark("Mystikuv utok")]
		Mystical=Magic|0x000080,
		[Remark("Damage redukovano fyzickym rezistem (neplest s armorem)")]
		Physical=0x000002,
		[Remark("Secne zbrane (mece, sekery) ")]
		Slashing=Physical|0x000100,
		[Remark("Bodne zbrane (mece, dyky, vidle) (drive piercing, prejmenovano aby se to nepletlo s prubojnosti)")]
		Stabbing=Physical|0x000200,
		[Remark("Secne bodne zbrane (mece)")]
		Sharp=Physical|Slashing|Stabbing,
		[Remark("Tupe zbrane (hole, palcaty)")]
		Blunt=Physical|0x000400,
		[Remark("Palne zbrane (luky, kuse)")]
		Archery=Physical|0x000800,
		Bleed=Physical|0x001000,

		Summon=0x002000,
		Dragon=0x004000,
		NonMagicFire=0x000004,
		NonMagicPoison=0x000040
	}

	public enum WeaponType : int {
		Blunt,//also bare hands
		Stabbing,
		Sword,
		Axe,
		Archery
	}
}