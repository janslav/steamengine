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
using SteamEngine.LScript;
using SteamEngine.CompiledScripts;

namespace SteamEngine.CompiledScripts.Dialogs {
	[Summary("Dialog that will display the list of clients delayed messages")]
	public class D_DelayedMessages : CompiledGumpDef {
		internal static readonly TagKey msgsSortingTK = TagKey.Get("_messages_sorting_");
		internal static readonly TagKey msgsListTK = TagKey.Get("_messages_list_");

		[Summary("Display the list of the messages")]
		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			List<DelayedMsg> messagesList = (List<DelayedMsg>)args.GetTag(D_DelayedMessages.msgsListTK); //seznam msgi si posilame v argumentu (napriklad pri pagingu)
			if (messagesList == null) {
				//vzit seznam a pripadne ho setridit...
				//toto se provede jen pri prvnim zobrazeni nebo zmene kriteria!
				messagesList = MsgsBoard.GetClientsMessages((Character) sendTo);
				//setrid zpravy (neni li specifikaovano trideni, pouzije se prirozene trideni dle casu)
				messagesList = MsgsBoard.GetSortedBy(messagesList, (SortingCriteria) TagMath.IGetTag(args, D_DelayedMessages.msgsSortingTK));
				args.SetTag(D_DelayedMessages.msgsListTK, messagesList); //ulozime mezi parametry dialogu
			}

			int unreadCnt = MsgsBoard.CountUnread((Character) sendTo);
			int firstiVal = TagMath.IGetTag(args, ImprovedDialog.pagingIndexTK);   //prvni index na strance
			//maximalni index (20 radku mame) + hlidat konec seznamu...
			int imax = Math.Min(firstiVal + ImprovedDialog.PAGE_ROWS, messagesList.Count);

			ImprovedDialog dialogHandler = new ImprovedDialog(this.GumpInstance);
			//pozadi    
			dialogHandler.CreateBackground(800);
			dialogHandler.SetLocation(40, 30);

			//nadpis
			dialogHandler.AddTable(new GUTATable(1, 300, 0, ButtonFactory.D_BUTTON_WIDTH));
			dialogHandler.LastTable[0, 0] = TextFactory.CreateHeadline("Seznam zpráv (" + (firstiVal + 1) + "-" + imax + " z " + messagesList.Count + ")");

			//cudliky na trideni dle neprectenych (i s popiskem)
			dialogHandler.LastTable[0, 1] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonSortUp, 5);//tridit dle neprectenych (neprectene nahoru)
			dialogHandler.LastTable[0, 1] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonSortDown, 0, ButtonFactory.D_SORTBUTTON_LINE_OFFSET, 6);//tridit dle neprectenych (neprectene dolu)
			dialogHandler.LastTable[0, 1] = TextFactory.CreateLabel(ButtonFactory.D_SORTBUTTON_COL_OFFSET, 0, "Tøídit dle nepøeètených (nepøeètených " + unreadCnt + ")");
			//cudlik na zavreni dialogu			
			dialogHandler.LastTable[0, 2] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonCross, 0);
			dialogHandler.MakeLastTableTransparent();

			//popis sloupecku
			dialogHandler.AddTable(new GUTATable(1, 180, 160, ButtonFactory.D_BUTTON_WIDTH, ButtonFactory.D_BUTTON_WIDTH, 0)); //radek na nadpisy            
			//cas
			dialogHandler.LastTable[0, 0] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonSortUp, 1); //tridit podle casu asc
			dialogHandler.LastTable[0, 0] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonSortDown, 0, ButtonFactory.D_SORTBUTTON_LINE_OFFSET, 2); //tridit podle casu desc            
			dialogHandler.LastTable[0, 0] = TextFactory.CreateLabel(ButtonFactory.D_SORTBUTTON_COL_OFFSET, 0, "Èas odeslání");
			//cudlik s odesilatelem
			dialogHandler.LastTable[0, 1] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonSortUp, 3); //tridit dle jmena sendera asc
			dialogHandler.LastTable[0, 1] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonSortDown, 0, ButtonFactory.D_SORTBUTTON_LINE_OFFSET, 4); //tridit dle jmena sendera desc
			dialogHandler.LastTable[0, 1] = TextFactory.CreateLabel(ButtonFactory.D_SORTBUTTON_COL_OFFSET, 0, "Odesilatel");
			//cudlik pro cteni
			dialogHandler.LastTable[0, 2] = TextFactory.CreateLabel("Èíst");
			//cudlik pro mazani   
			dialogHandler.LastTable[0, 3] = TextFactory.CreateLabel("Del");
			//text
			dialogHandler.LastTable[0, 4] = TextFactory.CreateLabel("Text zprávy");

			dialogHandler.MakeLastTableTransparent(); //zpruhledni nadpisovy radek

			//vlastni seznam zprav
			dialogHandler.AddTable(new GUTATable(imax - firstiVal));
			dialogHandler.CopyColsFromLastTable();

			//projet seznam v ramci daneho rozsahu indexu
			int rowCntr = 0;
			for (int i = firstiVal; i < imax; i++) {
				DelayedMsg msg = (DelayedMsg) messagesList[i];
				Hues msgColor = msg.color;
				dialogHandler.LastTable[rowCntr, 0] = TextFactory.CreateText(msgColor, msg.time.ToString());//cas odeslani
				dialogHandler.LastTable[rowCntr, 1] = TextFactory.CreateText(msgColor, (msg.sender == null ? MsgsBoard.NO_SENDER : msg.sender.Name)); //odesilatel
				dialogHandler.LastTable[rowCntr, 2] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonPaper, (2 * i) + 10); //èíst
				dialogHandler.LastTable[rowCntr, 3] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonCross, (2 * i) + 11); //smazat
				dialogHandler.LastTable[rowCntr, 4] = TextFactory.CreateText(msgColor, msg.text); //text zpravy				

				rowCntr++;
			}
			dialogHandler.MakeLastTableTransparent(); //zpruhledni zbytek dialogu

			//now handle the paging 
			dialogHandler.CreatePaging(messagesList.Count, firstiVal, 1);

			dialogHandler.WriteOut();
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			//seznam zprav z kontextu (mohl jiz byt trideny apd.)
			ArrayList messagesList = (ArrayList) args.GetTag(D_DelayedMessages.msgsListTK);
			if (gr.pressedButton < 10) { //ovladaci tlacitka (sorting, paging atd)
				switch (gr.pressedButton) {
					case 0: //exit
						DialogStacking.ShowPreviousDialog(gi); //zobrazit pripadny predchozi dialog
						break;
					case 1: //tridit dle casu asc						
						args.SetTag(D_DelayedMessages.msgsSortingTK, SortingCriteria.TimeAsc);
						args.RemoveTag(D_DelayedMessages.msgsListTK);//odstranit seznm, bude prenacten a jinak setriden
						DialogStacking.ResendAndRestackDialog(gi);
						break;
					case 2: //tridit dle casu desc
						args.SetTag(D_DelayedMessages.msgsSortingTK, SortingCriteria.TimeDesc);
						args.RemoveTag(D_DelayedMessages.msgsListTK);
						DialogStacking.ResendAndRestackDialog(gi);
						break;
					case 3: //tridit dle sendera asc
						args.SetTag(D_DelayedMessages.msgsSortingTK, SortingCriteria.NameAsc);
						args.RemoveTag(D_DelayedMessages.msgsListTK);
						DialogStacking.ResendAndRestackDialog(gi);
						break;
					case 4: //tridit dle sendera desc
						args.SetTag(D_DelayedMessages.msgsSortingTK, SortingCriteria.NameDesc);
						args.RemoveTag(D_DelayedMessages.msgsListTK);
						DialogStacking.ResendAndRestackDialog(gi);
						break;
					case 5: //tridit dle neprectenych asc
						args.SetTag(D_DelayedMessages.msgsSortingTK, SortingCriteria.UnreadAsc);
						args.RemoveTag(D_DelayedMessages.msgsListTK);
						DialogStacking.ResendAndRestackDialog(gi);
						break;
					case 6: //tridit dle neprectenych desc
						args.SetTag(D_DelayedMessages.msgsSortingTK, SortingCriteria.UnreadDesc);
						args.RemoveTag(D_DelayedMessages.msgsListTK);
						DialogStacking.ResendAndRestackDialog(gi);
						break;
				}
			} else if (ImprovedDialog.PagingButtonsHandled(gi, gr, messagesList.Count, 1)) {//kliknuto na paging?
				return;
			} else { //skutecna tlacitka z radku
				//zjistime kterej cudlik z radku byl zmacknut
				int row = (int) (gr.pressedButton - 10) / 2;
				int buttNum = (int) (gr.pressedButton - 10) % 2;
				DelayedMsg msg = (DelayedMsg) messagesList[row];
				switch (buttNum) {
					case 0: //èíst
						if (!msg.read) {
							//store the info about the message read status and change its color a bit
							msg.read = true;
							msg.color = msg.color + 3;//trosku ztmavit barvu
						}
						//zobrazit tex zprávy (první parametr je nadpis, druhý je zobrazný text)
						Gump newGi = gi.Cont.Dialog(SingletonScript<D_Display_Text>.Instance, new DialogArgs("Text zprávy", msg.text));
						//stacknout messageslist pro navrat
						DialogStacking.EnstackDialog(gi, newGi);
						break;
					case 1: //smazat
						MsgsBoard.DeleteMessage((Character) gi.Cont, msg);
						//znovuzavolat dialog
						DialogStacking.ResendAndRestackDialog(gi);
						break;
				}
			}
		}

		[SteamFunction]
		public static void Messages(AbstractCharacter sender, ScriptArgs args) {
			//ten poslendi null - misto pro seznam messagi
			DialogArgs newArgs = new DialogArgs();
			if (args.Args.Length == 0) {
				newArgs.SetTag(D_DelayedMessages.msgsSortingTK, SortingCriteria.TimeAsc);
				sender.Dialog(SingletonScript<D_DelayedMessages>.Instance, newArgs);//default sorting, beginning from the first message
			} else {
				newArgs.SetTag(D_DelayedMessages.msgsSortingTK, (SortingCriteria) args.Args[0]);
				sender.Dialog(SingletonScript<D_DelayedMessages>.Instance, newArgs); //we expect a sorting criterion !, listing from the first message
			}
		}
	}
}