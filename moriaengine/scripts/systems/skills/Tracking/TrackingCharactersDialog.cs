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
using SteamEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.LScript;

namespace SteamEngine.CompiledScripts.Dialogs {

	[Summary("Dialog that will display all trackable characters nearby the tracker with the possibility to track them...")]
	public class D_Tracking_Characters : CompiledGumpDef {
		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			SkillSequenceArgs ssa = (SkillSequenceArgs)args.ArgsArray[0];
			CharacterTypes charType = (CharacterTypes)ssa.Param1;
			List<Character> charsAround = (List<Character>) args.ArgsArray[1];//trackable characters around
			charsAround.Sort(CharComparerByName.instance); //sort by name (why not? :-) )

			int firstiVal = TagMath.IGetTag(args, ImprovedDialog.pagingIndexTK);//prvni index na strance
			int imax = Math.Min(firstiVal + ImprovedDialog.PAGE_ROWS, charsAround.Count);
			
			ImprovedDialog dlg = new ImprovedDialog(this.GumpInstance);
			//pozadi    
			dlg.CreateBackground(180);
			dlg.SetLocation(80, 50);

			//nadpis
			dlg.AddTable(new GUTATable(1, 0, ButtonFactory.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = TextFactory.CreateHeadline("Co chceš stopovat?");
			//cudlik na zavreni dialogu
			dlg.LastTable[0, 1] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonCross, 0);
			dlg.MakeLastTableTransparent();

			//popis sloupcu
			dlg.AddTable(new GUTATable(1, 43, 0, ButtonFactory.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = TextFactory.CreateLabel(""); //nic, to bude obrazek
			dlg.LastTable[0, 1] = TextFactory.CreateLabel("Jméno",DialogAlignment.Align_Center,DialogAlignment.Valign_Top);
			dlg.LastTable[0, 2] = TextFactory.CreateLabel(""); //cudlik na stopovani, to je hotovka
			dlg.MakeLastTableTransparent();

			//seznam charu ke stopovani
			GUTATable charsTable = new GUTATable(imax - firstiVal);
			charsTable.RowHeight = 50; //dobra vejska pro obrazky
			charsTable.InnerRowsDelimited = true;
			dlg.AddTable(charsTable);
			dlg.CopyColsFromLastTable();
			
			//projet seznam v ramci daneho rozsahu indexu
			int rowCntr = 0;
			GumpIDs displayGump = GumpIDs.Figurine_Man; //default
			switch (charType) {
				case CharacterTypes.Animals: 
					displayGump = GumpIDs.Figurine_Llama;
					break;
				case CharacterTypes.Monsters:
					displayGump = GumpIDs.Figurine_Ogre;
					break;
				case CharacterTypes.NPCs:
					displayGump = GumpIDs.Figurine_NPC;
					break;
				default:
					break; //for All or Players we need to determine either the whole Icon or at lease the Player's gender
			}
			for (int i = firstiVal; i < imax; i++) {
				Character chr = charsAround[i];
				if (charType == CharacterTypes.Players) {
					displayGump = chr.IsMale ? GumpIDs.Figurine_Man : GumpIDs.Figurine_Woman;
				} else if (charType == CharacterTypes.All) {//for "all" determine the icon char by char
					displayGump = GetGumpIconForChar(chr);
				}

				dlg.LastTable[rowCntr, 0] = ImageFactory.CreateImage(displayGump);
				dlg.LastTable[rowCntr, 1] = TextFactory.CreateText(chr.Name, DialogAlignment.Align_Center, DialogAlignment.Valign_Center);
				dlg.LastTable[rowCntr, 2] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonTick, 10 + rowCntr, DialogAlignment.Valign_Center);
				
				rowCntr++;
			}
			dlg.MakeLastTableTransparent(); //zpruhledni zbytek dialogu

			dlg.WriteOut();
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			if (gr.pressedButton < 10) {
				switch (gr.pressedButton) {
					case 0: //exit - finish tracking without selecting anything
						((SkillSequenceArgs) args.ArgsArray[0]).PhaseAbort();
						break;
				}
			} else {
				//check which character from the list is to be tracked
				SkillSequenceArgs ssa = (SkillSequenceArgs) args.ArgsArray[0];
				List<Character> charsAround = (List<Character>) args.ArgsArray[1];
				Character charToTrack = charsAround[(int)gr.pressedButton - 10];

				ssa.Param1 = charToTrack;
				ssa.Param2 = TrackingEnums.Phase_Character_Track; //track the particular character
				ssa.PhaseStart();//start again (but with another parameters)
			}
		}

		//simple method for finding the character's icon to be displayed 
		//(when the "icon" property starts to work, replace this method!)
		private GumpIDs GetGumpIconForChar(Character chr) {
			if (chr.IsAnimal) {//animal
				return GumpIDs.Figurine_Llama;
			}
			if (chr.IsMonster) {//monster
				return GumpIDs.Figurine_Orc;
			}
			if (chr is Player) {//player
				if (chr.IsMale) {
					return GumpIDs.Figurine_Man;
				}
				return GumpIDs.Figurine_Woman;
			}
			if (chr.IsHuman) {//NPC
				return GumpIDs.Figurine_NPC;
			}
			return GumpIDs.Figurine_Man; //default
		}
	}
}