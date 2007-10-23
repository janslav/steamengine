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
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts.Dialogs {

	[Remark("Dialog listing all accounts notes")]
	public class D_AccountNotes : CompiledGump {
		private static int width = 700;
		
		[Remark("Seznam parametru: 0 - account jehoz noty zobrazujeme, " +
				"	1 - tridici kriterium" +
				"	2 - index ze seznamu notu ktery bude na strance jako prvni" +				
				"	3 - ulozeny noteslist pro pripadnou navigaci v dialogu")]
		public override void Construct(Thing focus, AbstractCharacter sendTo, object[] args) {
			//vzit seznam notu z accountu prisleho v parametru dialogu
			ScriptedAccount acc = (ScriptedAccount)args[0];
			List<AccountNote> notesList = null;
			if(args[3] == null) {
				//vzit seznam notu a pripadne ho setridit...
				//toto se provede jen pri prvnim zobrazeni nebo zmene kriteria!
				notesList = AccountRegister.GetNotes(acc, (AccountNotesSorting)args[1]);
				args[3] = notesList; //ulozime to do argumentu dialogu
			} else {
				//taglist si posilame v argumentu (napriklad pri pagingu)
				notesList = (List<AccountNote>)args[3];
			}
			//zjistit zda bude paging, najit maximalni index na strance
			int firstiVal = Convert.ToInt32(args[2]);   //prvni index na strance
			int imax = Math.Min(firstiVal + ImprovedDialog.PAGE_ROWS, notesList.Count);

			ImprovedDialog dlg = new ImprovedDialog(this.GumpInstance);
			//pozadi    
			dlg.CreateBackground(width);
			dlg.SetLocation(50, 50);

			//nadpis
			dlg.Add(new GUTATable(1, 0, ButtonFactory.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = TextFactory.CreateHeadline("Poznámky k accountu " + acc.Name + " (zobrazeno " + (firstiVal + 1) + "-" + imax + " z " + notesList.Count + ")");
			dlg.LastTable[0, 1] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonCross, 0);//cudlik na zavreni dialogu
			dlg.MakeTableTransparent();

			//cudlik na zalozeni nove pozmamky
			dlg.Add(new GUTATable(1, 130, ButtonFactory.D_BUTTON_WIDTH, 0));
			dlg.LastTable[0, 0] = TextFactory.CreateLabel("Nová poznámka");
			dlg.LastTable[0, 1] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonOK, 1);
			dlg.MakeTableTransparent();

			//popis sloupcu
			dlg.Add(new GUTATable(1, 180, 160, 250, 160, ButtonFactory.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonSortUp, 2); //tridit podle casu asc
			dlg.LastTable[0, 0] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonSortDown, 0, ButtonFactory.D_SORTBUTTON_LINE_OFFSET, 3); //tridit podle casu desc            
			dlg.LastTable[0, 0] = TextFactory.CreateLabel(ButtonFactory.D_SORTBUTTON_COL_OFFSET, 0, "Èas");
			dlg.LastTable[0, 1] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonSortUp, 4); //tridit dle refcharu asc
			dlg.LastTable[0, 1] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonSortDown, 0, ButtonFactory.D_SORTBUTTON_LINE_OFFSET, 5); //tridit dle refcharu desc
			dlg.LastTable[0, 1] = TextFactory.CreateLabel(ButtonFactory.D_SORTBUTTON_COL_OFFSET, 0, "Postava");
			dlg.LastTable[0, 2] = TextFactory.CreateLabel("Text poznámky");
			dlg.LastTable[0, 3] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonSortUp, 6); //tridit dle issuera asc
			dlg.LastTable[0, 3] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonSortDown, 0, ButtonFactory.D_SORTBUTTON_LINE_OFFSET, 7); //tridit dle issuera desc
			dlg.LastTable[0, 3] = TextFactory.CreateLabel(ButtonFactory.D_SORTBUTTON_COL_OFFSET, 0, "Autor");			
			dlg.LastTable[0, 4] = TextFactory.CreateLabel("Smaž");
			dlg.MakeTableTransparent();

			//seznam poznamek
			dlg.Add(new GUTATable(imax - firstiVal));
			dlg.CopyColsFromLastTable();

			//projet seznam v ramci daneho rozsahu indexu
			int rowCntr = 0;
			for(int i = firstiVal; i < imax; i++) {
				AccountNote note = notesList[i];

				dlg.LastTable[rowCntr, 0] = TextFactory.CreateText(note.time.ToString());
				if(note.referredChar != null) {
					dlg.LastTable[rowCntr, 1] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonPaper, 10 + (4 * i)); //info o postave
					dlg.LastTable[rowCntr, 1] = TextFactory.CreateText(ButtonFactory.D_BUTTON_WIDTH, 0, note.referredChar.Name); //postava
				} else {
					dlg.LastTable[rowCntr, 1] = TextFactory.CreateText(AccountRegister.ALL_CHARS); //tyka se vsech postav na acc
				}
				dlg.LastTable[rowCntr, 2] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonPaper,11 + (4 * i)); //zobrazit text zpravy
				dlg.LastTable[rowCntr, 2] = TextFactory.CreateText(ButtonFactory.D_BUTTON_WIDTH, 0, note.text); //text zpravy
				dlg.LastTable[rowCntr, 3] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonPaper, 12 + (4 * i)); //info o issuerovi
				dlg.LastTable[rowCntr, 3] = TextFactory.CreateText(ButtonFactory.D_BUTTON_WIDTH, 0, note.issuer.Name); //issuer

				if(sendTo == note.issuer || sendTo.Plevel > note.issuer.Plevel) {
					dlg.LastTable[rowCntr, 4] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonCross, 13 + (4 * i)); //smaz
				} else {
					//pokud ten co se diva neni ten kdo zpravu postnul a ani nema vyssi plevel, pak nesmi poznamku smazat!
					dlg.LastTable[rowCntr, 4] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonNoOperation, false, 9999);
				}
				rowCntr++;
			}
			dlg.MakeTableTransparent(); //zpruhledni zbytek dialogu
			dlg.CreatePaging(notesList.Count, firstiVal, 1);//ted paging			
			dlg.WriteOut();
		}

		public override void OnResponse(GumpInstance gi, GumpResponse gr, object[] args) {
			//seznam tagu bereme z parametru (mohl byt jiz trideny atd, nebudeme ho proto selectit znova)
			List<AccountNote> notesList = (List<AccountNote>)args[3];
			int firstOnPage = Convert.ToInt32(args[2]);
			if(gr.pressedButton < 10) { //ovladaci tlacitka (exit, new, tridit)				
				switch(gr.pressedButton) {
					case 0: //exit
						DialogStacking.ShowPreviousDialog(gi); //zobrazit pripadny predchozi dialog
						break;
					case 1: //zalozit novou poznamku
						//newGi = gi.Cont.Dialog(SingletonScript<D_NewAccountNote>.Instance, args[0]); //posleme si parametr toho accountu kde chceme novou poznamku
						//DialogStacking.EnstackDialog(gi, newGi); //vlozime napred dialog do stacku
						break;						
					case 2: //time asc
						args[1] = AccountNotesSorting.TimeAsc;
						args[3] = null;
						DialogStacking.ResendAndRestackDialog(gi);
						break;
					case 3: //time desc
						args[1] = AccountNotesSorting.TimeDesc;
						args[3] = null;
						DialogStacking.ResendAndRestackDialog(gi);
						break;
					case 4: //refcar asc
						args[1] = AccountNotesSorting.RefCharAsc;
						args[3] = null;
						DialogStacking.ResendAndRestackDialog(gi);
						break;
					case 5: //refchar desc
						args[1] = AccountNotesSorting.RefCharDesc;
						args[3] = null;
						DialogStacking.ResendAndRestackDialog(gi);
						break;
					case 6: //issuer asc
						args[1] = AccountNotesSorting.IssuerAsc;
						args[3] = null;
						DialogStacking.ResendAndRestackDialog(gi);
						break;
					case 7: //issuer desc
						args[1] = AccountNotesSorting.IssuerDesc;
						args[3] = null;
						DialogStacking.ResendAndRestackDialog(gi);
						break;
				}
			} else if (ImprovedDialog.PagingButtonsHandled(gi, gr, 2, notesList.Count, 1)) {//kliknuto na paging? (1 = index parametru nesoucim info o pagingu (zde dsi.Args[2] viz výše)
				//1 sloupecek
				return;
			} else {
				//zjistime si radek
				int row = ((int)gr.pressedButton - 10) / 4;
				int buttNo = ((int)gr.pressedButton - 10) % 4;
				AccountNote note = notesList[row];
				GumpInstance newGi;
				switch(buttNo) {
					case 0: //char info
						newGi = gi.Cont.Dialog(SingletonScript<D_Info>.Instance, note.referredChar, 0, 0);
						DialogStacking.EnstackDialog(gi, newGi);
						break;
					case 1: //text zpravy
						//zobrazit tex zprávy (první parametr je nadpis, druhý je zobrazný text)
						newGi = gi.Cont.Dialog(D_Display_Text.Instance, "Text poznámky", note.text);
						DialogStacking.EnstackDialog(gi, newGi);
						break;
					case 2: //issuer info
						newGi = gi.Cont.Dialog(SingletonScript<D_Info>.Instance, note.issuer, 0, 0);
						DialogStacking.EnstackDialog(gi, newGi);
						break;
					case 3: //smazat poznamku (to muze jen jeji autor nebo clovek s vyssim plevelem)
						ScriptedAccount acc = (ScriptedAccount)args[0];
						acc.RemoveNote(note);
						DialogStacking.ResendAndRestackDialog(gi);
						break;
				}
			}
		}		

		[Remark("Display an account notes. Function accessible from the game." +
				"The function is designed to be triggered using .x AccNotes" +
				"but it can be also called from other dialogs - such as info..."+
				"Default sorting is by Time, desc."+
				"Last usage us .AccNotes('acc_name'[,'sorting'])")]
		[SteamFunction]
		public static void AccNotes(AbstractCharacter self, ScriptArgs text) {
			//zavolat dialog, 
			//0. parametr - account
			//1. param - trideni dle...
			//2. od kolikate poznamky zaciname (0), 3. prostor pro potreby dialogu
			if(text == null || text.argv == null || text.argv.Length == 0) {
				Globals.SrcCharacter.Dialog(SingletonScript<D_AccountNotes>.Instance, self.Account, AccountNotesSorting.TimeDesc, 0, null);
			} else {
				ScriptedAccount acc = (ScriptedAccount)AbstractAccount.Get(text.Argv[0].ToString());
				if(acc == null) {
					Globals.SrcCharacter.SysMessage("Account se jménem "+text.Argv[0].ToString()+" neexistuje.", (int)Hues.Red);
					return;
				}
				if(text.argv.Length == 1) { //mame jen nazev accountu
					Globals.SrcCharacter.Dialog(SingletonScript<D_AccountNotes>.Instance, acc, null, 0, null);
				} else { //mame i trideni
					Globals.SrcCharacter.Dialog(SingletonScript<D_AccountNotes>.Instance, acc, (AccountNotesSorting)text.argv[1], 0, null);
				}
			}
		}
	}	
}
