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

using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {

	[Remark("Numbers of important hues")]
	public enum Hues : int {
		Red=0x21,
		Blue=0x63,
		Green=0x44,
		Info=0x282, //shit color :) (dark yellow-green-brown undefinable) :-/ its hard to choose
        //text colors
        WriteColor=2300,
        PlayerColor=2301,//color for players name in Admin dialog (until the coloring players is solved)
		WriteColor2=0481,
		ReadColor=2303,
		NAColor=2305
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
}
