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
using SteamEngine;
using SteamEngine.Common;
using SteamEngine.LScript;
using SteamEngine.CompiledScripts;

namespace SteamEngine.CompiledScripts.Dialogs {

	[Remark("Dialog that will display the list of clients delayed messages")]
	public class D_DelayedMessages : CompiledGump {
		[Remark("Instance of the D_DelayedMessages, for possible access from other dialogs, buttons etc.")]
		private static D_DelayedMessages instance;
		public static D_DelayedMessages Instance {
			get {
				return instance;
			}
		}
		[Remark("Set the static reference to the instance of this dialog")]
		public D_DelayedMessages() {
			instance = this;
		}

		[Remark("Display the list of the messages")]
		public override void Construct(Thing focus, AbstractCharacter src, object[] sa) {
			ArrayList messagesList = MsgsBoard.GetClientsMessages((Character)src);
			//setrid zpravy (neni li specifikaovano trideni, pouzije se prirozene trideni dle casu)
			messagesList = MsgsBoard.GetSortedBy(messagesList, (SortingCriteria)sa[0]);
			sa[2] = messagesList; //ulozime mezi parametry dialogu

			int unreadCnt = MsgsBoard.CountUnread((Character)src);
			int firstiVal = Convert.ToInt32(sa[1]);   //prvni index na strance
			//maximalni index (20 radku mame) + hlidat konec seznamu...
			int imax = Math.Min(firstiVal + ImprovedDialog.PAGE_ROWS, messagesList.Count);

			ImprovedDialog dialogHandler = new ImprovedDialog(this.GumpInstance);
			//pozadi    
			dialogHandler.CreateBackground(800);
			dialogHandler.SetLocation(40, 30);

			//nadpis
			dialogHandler.Add(new GUTATable(1, 300, 0, ButtonFactory.D_BUTTON_WIDTH));			
			dialogHandler.LastTable[0,0] = TextFactory.CreateHeadline("Seznam zpráv (" + (firstiVal+1) + "-" + imax +" z " + messagesList.Count+ ")");

			//cudliky na trideni dle neprectenych (i s popiskem)
			dialogHandler.LastTable[0,1] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonSortUp, 5);//tridit dle neprectenych (neprectene nahoru)
			dialogHandler.LastTable[0,1] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonSortDown, 0, ButtonFactory.D_SORTBUTTON_LINE_OFFSET, 6);//tridit dle neprectenych (neprectene dolu)
			dialogHandler.LastTable[0, 1] = TextFactory.CreateLabel(ButtonFactory.D_SORTBUTTON_COL_OFFSET, 0, "Tøídit dle nepøeètených (nepøeètených " + unreadCnt + ")");			
			//cudlik na zavreni dialogu			
			dialogHandler.LastTable[0,2] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonCross, 0);
			dialogHandler.MakeTableTransparent();

			//popis sloupecku
			dialogHandler.Add(new GUTATable(1, 180, 160, ButtonFactory.D_BUTTON_WIDTH, ButtonFactory.D_BUTTON_WIDTH, 0)); //radek na nadpisy            
				//cas
			dialogHandler.LastTable[0,0] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonSortUp, 1); //tridit podle casu asc
			dialogHandler.LastTable[0,0] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonSortDown, 0, ButtonFactory.D_SORTBUTTON_LINE_OFFSET, 2); //tridit podle casu desc            
			dialogHandler.LastTable[0, 0] = TextFactory.CreateLabel(ButtonFactory.D_SORTBUTTON_COL_OFFSET, 0, "Èas odeslání");
				//cudlik s odesilatelem
			dialogHandler.LastTable[0,1] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonSortUp, 3); //tridit dle jmena sendera asc
			dialogHandler.LastTable[0,1] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonSortDown, 0, ButtonFactory.D_SORTBUTTON_LINE_OFFSET, 4); //tridit dle jmena sendera desc
			dialogHandler.LastTable[0, 1] = TextFactory.CreateLabel(ButtonFactory.D_SORTBUTTON_COL_OFFSET, 0, "Odesilatel");
				//cudlik pro cteni
			dialogHandler.LastTable[0, 2] = TextFactory.CreateLabel("Èíst");
				//cudlik pro mazani   
			dialogHandler.LastTable[0, 3] = TextFactory.CreateLabel("Del");
				//text
			dialogHandler.LastTable[0, 4] = TextFactory.CreateLabel("Text zprávy");

			dialogHandler.MakeTableTransparent(); //zpruhledni nadpisovy radek

			//vlastni seznam zprav
			dialogHandler.Add(new GUTATable(imax-firstiVal));
			dialogHandler.CopyColsFromLastTable();

			//projet seznam v ramci daneho rozsahu indexu
			int rowCntr = 0;
			for(int i = firstiVal; i < imax; i++) {
				DelayedMsg msg = (DelayedMsg)messagesList[i];
				Hues msgColor = msg.color;
				dialogHandler.LastTable[rowCntr,0] = TextFactory.CreateText(msgColor, msg.time.ToString());//cas odeslani
				dialogHandler.LastTable[rowCntr,1] = TextFactory.CreateText(msgColor, (msg.sender == null ? MsgsBoard.NO_SENDER : msg.sender.Name)); //odesilatel
				dialogHandler.LastTable[rowCntr,2] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonPaper, (2 * i) + 10); //èíst
				dialogHandler.LastTable[rowCntr,3] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonCross, (2 * i) + 11); //smazat
				dialogHandler.LastTable[rowCntr,4] = TextFactory.CreateText(msgColor, msg.text); //text zpravy				

				rowCntr++;
			}
			dialogHandler.MakeTableTransparent(); //zpruhledni zbytek dialogu

			//now handle the paging 
			dialogHandler.CreatePaging(messagesList.Count, firstiVal,1);

			dialogHandler.WriteOut();
		}

		public override void OnResponse(GumpInstance gi, GumpResponse gr, object[] args) {
			//seznam zprav z kontextu (mohl jiz byt trideny apd.)
			ArrayList messagesList = (ArrayList)args[2];
            if(gr.pressedButton < 10) { //ovladaci tlacitka (sorting, paging atd)
				switch(gr.pressedButton) {
					case 0: //exit
						DialogStackItem.ShowPreviousDialog(gi.Cont.Conn); //zobrazit pripadny predchozi dialog
						break;
					case 1: //tridit dle casu asc						
						args[0] = SortingCriteria.TimeAsc; //uprav info o sortovani
						gi.Cont.SendGump(gi.Focus, D_DelayedMessages.instance, args);
						break;
					case 2: //tridit dle casu desc
						args[0] = SortingCriteria.TimeDesc; //uprav info o sortovani
						gi.Cont.SendGump(gi.Focus, D_DelayedMessages.instance, args);
						break;
					case 3: //tridit dle sendera asc
						args[0] = SortingCriteria.NameAsc; //uprav info o sortovani
						gi.Cont.SendGump(gi.Focus, D_DelayedMessages.instance, args);
						break;
					case 4: //tridit dle sendera desc
						args[0] = SortingCriteria.NameDesc; //uprav info o sortovani
						gi.Cont.SendGump(gi.Focus, D_DelayedMessages.instance, args);
						break;
					case 5: //tridit dle neprectenych asc
						args[0] = SortingCriteria.UnreadAsc; //uprav info o sortovani
						gi.Cont.SendGump(gi.Focus, D_DelayedMessages.instance, args);
						break;
					case 6: //tridit dle neprectenych desc
						args[0] = SortingCriteria.UnreadDesc; //uprav info o sortovani
						gi.Cont.SendGump(gi.Focus, D_DelayedMessages.instance, args);
						break;					
				}
			} else if(ImprovedDialog.PagingButtonsHandled(gi, gr, D_DelayedMessages.instance, args, 1, messagesList.Count, 1)) {//kliknuto na paging? (1 = index parametru nesoucim info o pagingu (zde dsi.Args[1] viz výše)
				//1 sloupecek
				return;
            } else { //skutecna tlacitka z radku
                //zjistime kterej cudlik z radku byl zmacknut
                int row = (int)(gr.pressedButton - 10) / 2;
                int buttNum = (int)(gr.pressedButton - 10) % 2;
                DelayedMsg msg = (DelayedMsg)messagesList[row];
                switch(buttNum) {
                    case 0: //èíst
						if(!msg.read) { 
							//store the info about the message read status and change its color a bit
							msg.read = true;
							msg.color = msg.color + 3;//trosku ztmavit barvu
						}
						//stacknout messageslist pro navrat
						DialogStackItem.EnstackDialog(gi, D_DelayedMessages.instance, args);

						//zobrazit tex zprávy (první parametr je nadpis, druhý je zobrazný text)
						gi.Cont.Dialog(D_Display_Text.Instance, "Text zprávy", msg.text);
                        break;
                    case 1: //smazat
						MsgsBoard.DeleteMessage((Character)gi.Cont, msg);
						//znovuzavolat dialog
						gi.Cont.SendGump(gi.Focus, D_DelayedMessages.instance, args);
                        break;                    
                }
			}
		}

		[SteamFunction]
		public static void Messages(AbstractCharacter sender, ScriptArgs args) {
			//ten poslendi null - misto pro seznam messagi
			if(args.Args.Length == 0) {
				sender.Dialog(D_DelayedMessages.Instance, SortingCriteria.TimeAsc, 0, null);//default sorting, beginning from the first message
			} else {
				sender.Dialog(D_DelayedMessages.Instance, (SortingCriteria)args.Args[0], 0, null); //we expect a sorting criterion !, listing from the first message
			}
		}	
	}
}