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
		private static int width = 800;
		
		[Remark("Seznam parametru: 0 - account jehoz noty zobrazujeme, " +
				"	1 - tridici kriterium" +
				"	2 - index ze seznamu notu ktery bude na strance jako prvni" +				
				"	3 - ulozeny noteslist pro pripadnou navigaci v dialogu")]
		public override void Construct(Thing focus, AbstractCharacter sendTo, object[] args) {
			ScriptedAccount acc = (ScriptedAccount)args[0]; //vzit seznam notu z accountu prisleho v parametru dialogu
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
			dlg.Add(new GUTATable(1, 145, 120, 350, 0, ButtonFactory.D_BUTTON_WIDTH));
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
			//seznam poznamek bereme z parametru (mohl byt jiz trideny atd, nebudeme ho proto selectit znova)
			List<AccountNote> notesList = (List<AccountNote>)args[3];
			int firstOnPage = Convert.ToInt32(args[2]);
			if(gr.pressedButton < 10) { //ovladaci tlacitka (exit, new, tridit)				
				switch(gr.pressedButton) {
					case 0: //exit
						DialogStacking.ShowPreviousDialog(gi); //zobrazit pripadny predchozi dialog
						break;
					case 1: //zalozit novou poznamku													poznamka, char, account
						GumpInstance newGi = gi.Cont.Dialog(SingletonScript<D_New_AccountNote>.Instance, false, null, args[0]);
						DialogStacking.EnstackDialog(gi, newGi); //vlozime napred dialog do stacku
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
						args[3] = null;
						DialogStacking.ResendAndRestackDialog(gi);
						break;
				}
			}
		}		

		[Remark("Display an account notes. Function accessible from the game." +
				"The function is designed to be triggered using .x AccNotes or .AccNotes" +
				"but it can be also called from other dialogs - such as info..."+
				"Default sorting is by Time, desc."+
				"Another way to use is: .AccNotes('acc_name'[,'sorting']).")]
		[SteamFunction]
		public static void AccNotes(Thing self, ScriptArgs text) {
			//Parametry dialogu:
			//1. parametr - account
			//2. param - trideni dle...
			//3. od kolikate poznamky zaciname (0), 3. prostor pro potreby dialogu
			if(text == null || text.argv == null || text.argv.Length == 0) {
				AbstractCharacter refChar = self as AbstractCharacter;
				if(refChar == null) {
					Globals.SrcCharacter.SysMessage("Chybne zamereni, zamer hrace", (int)Hues.Red);
					return;
				}
				if(refChar.IsPlayer) {
					Globals.SrcCharacter.Dialog(SingletonScript<D_AccountNotes>.Instance, refChar.Account, AccountNotesSorting.TimeDesc, 0, null);
				} else {
					Globals.SrcCharacter.SysMessage("Zameruj hrace!");
				}				
			} else {
				ScriptedAccount acc = (ScriptedAccount)AbstractAccount.Get(text.argv[0].ToString());
				if(acc == null) {
					Globals.SrcCharacter.SysMessage("Account se jménem "+text.argv[0].ToString()+" neexistuje.", (int)Hues.Red);
					return;
				}
				if(text.argv.Length == 1) { //mame jen nazev accountu
					Globals.SrcCharacter.Dialog(SingletonScript<D_AccountNotes>.Instance, acc, AccountNotesSorting.TimeDesc, 0, null);
				} else { //mame i trideni
					Globals.SrcCharacter.Dialog(SingletonScript<D_AccountNotes>.Instance, acc, (AccountNotesSorting)text.argv[1], 0, null);
				}
			}
		}		
	}

	[Remark("Dialog listing all account's crimes")]
	public class D_AccountCrimes : CompiledGump {
		private static int width = 900;

		[Remark("Seznam parametru: 0 - account jehoz crimy zobrazujeme, " +
				"	1 - tridici kriterium" +
				"	2 - index ze seznamu notu ktery bude na strance jako prvni" +
				"	3 - ulozeny noteslist pro pripadnou navigaci v dialogu")]
		public override void Construct(Thing focus, AbstractCharacter sendTo, object[] args) {
			ScriptedAccount acc = (ScriptedAccount)args[0]; //vzit seznam crimu z accountu prisleho v parametru dialogu
			List<AccountCrime> crimesList = null;
			if(args[3] == null) {
				//vzit seznam crimu a pripadne ho setridit...
				//toto se provede jen pri prvnim zobrazeni nebo zmene kriteria!
				crimesList = AccountRegister.GetCrimes(acc, (AccountNotesSorting)args[1]);
				args[3] = crimesList; //ulozime to do argumentu dialogu
			} else {
				//taglist si posilame v argumentu (napriklad pri pagingu)
				crimesList = (List<AccountCrime>)args[3];
			}
			//zjistit zda bude paging, najit maximalni index na strance
			int firstiVal = Convert.ToInt32(args[2]);   //prvni index na strance
			int imax = Math.Min(firstiVal + ImprovedDialog.PAGE_ROWS, crimesList.Count);

			ImprovedDialog dlg = new ImprovedDialog(this.GumpInstance);
			//pozadi    
			dlg.CreateBackground(width);
			dlg.SetLocation(50, 50);

			//nadpis
			dlg.Add(new GUTATable(1, 0, ButtonFactory.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = TextFactory.CreateHeadline("Prohøešky accountu " + acc.Name + " (zobrazeno " + (firstiVal + 1) + "-" + imax + " z " + crimesList.Count + ")");
			dlg.LastTable[0, 1] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonCross, 0);//cudlik na zavreni dialogu
			dlg.MakeTableTransparent();

			//cudlik na zalozeni noveho trestu
			dlg.Add(new GUTATable(1, 130, ButtonFactory.D_BUTTON_WIDTH, 0));
			dlg.LastTable[0, 0] = TextFactory.CreateLabel("Nový prohøešek");
			dlg.LastTable[0, 1] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonOK, 1);
			dlg.MakeTableTransparent();

			//popis sloupcu
			dlg.Add(new GUTATable(1, 145, 120, 225, 225, 0, ButtonFactory.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonSortUp, 2); //tridit podle casu asc
			dlg.LastTable[0, 0] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonSortDown, 0, ButtonFactory.D_SORTBUTTON_LINE_OFFSET, 3); //tridit podle casu desc            
			dlg.LastTable[0, 0] = TextFactory.CreateLabel(ButtonFactory.D_SORTBUTTON_COL_OFFSET, 0, "Èas");
			dlg.LastTable[0, 1] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonSortUp, 4); //tridit dle refcharu asc
			dlg.LastTable[0, 1] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonSortDown, 0, ButtonFactory.D_SORTBUTTON_LINE_OFFSET, 5); //tridit dle refcharu desc
			dlg.LastTable[0, 1] = TextFactory.CreateLabel(ButtonFactory.D_SORTBUTTON_COL_OFFSET, 0, "Postava");
			dlg.LastTable[0, 2] = TextFactory.CreateLabel("Popis prohøešku");
			dlg.LastTable[0, 3] = TextFactory.CreateLabel("Popis trestu");
			dlg.LastTable[0, 4] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonSortUp, 6); //tridit dle issuera asc
			dlg.LastTable[0, 4] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonSortDown, 0, ButtonFactory.D_SORTBUTTON_LINE_OFFSET, 7); //tridit dle issuera desc
			dlg.LastTable[0, 4] = TextFactory.CreateLabel(ButtonFactory.D_SORTBUTTON_COL_OFFSET, 0, "Autor");
			dlg.LastTable[0, 5] = TextFactory.CreateLabel("Smaž");
			dlg.MakeTableTransparent();

			//seznam trestu
			dlg.Add(new GUTATable(imax - firstiVal));
			dlg.CopyColsFromLastTable();

			//projet seznam v ramci daneho rozsahu indexu
			int rowCntr = 0;
			for(int i = firstiVal; i < imax; i++) {
				AccountCrime crime = crimesList[i];

				dlg.LastTable[rowCntr, 0] = TextFactory.CreateText(crime.time.ToString());
				if(crime.referredChar != null) {
					dlg.LastTable[rowCntr, 1] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonPaper, 10 + (5 * i)); //info o postave
					dlg.LastTable[rowCntr, 1] = TextFactory.CreateText(ButtonFactory.D_BUTTON_WIDTH, 0, crime.referredChar.Name); //postava
				} else {
					dlg.LastTable[rowCntr, 1] = TextFactory.CreateText(AccountRegister.ALL_CHARS); //tyka se vsech postav na acc
				}
				dlg.LastTable[rowCntr, 2] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonPaper, 11 + (5 * i)); //zobrazit text zpravy
				dlg.LastTable[rowCntr, 2] = TextFactory.CreateText(ButtonFactory.D_BUTTON_WIDTH, 0, crime.text); //text prohresku
				dlg.LastTable[rowCntr, 3] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonPaper, 12 + (5 * i)); //zobrazit text zpravy
				dlg.LastTable[rowCntr, 3] = TextFactory.CreateText(ButtonFactory.D_BUTTON_WIDTH, 0, crime.punishment); //text trestu

				dlg.LastTable[rowCntr, 4] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonPaper, 13 + (5 * i)); //info o issuerovi
				dlg.LastTable[rowCntr, 4] = TextFactory.CreateText(ButtonFactory.D_BUTTON_WIDTH, 0, crime.issuer.Name); //issuer

				if(sendTo == crime.issuer || sendTo.Plevel > crime.issuer.Plevel) {
					dlg.LastTable[rowCntr, 5] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonCross, 14 + (5 * i)); //smaz
				} else {
					//pokud ten co se diva neni ten kdo zpravu postnul a ani nema vyssi plevel, pak nesmi trest smazat!
					dlg.LastTable[rowCntr, 5] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonNoOperation, false, 9999);
				}
				rowCntr++;
			}
			dlg.MakeTableTransparent(); //zpruhledni zbytek dialogu
			dlg.CreatePaging(crimesList.Count, firstiVal, 1);//ted paging			
			dlg.WriteOut();
		}

		public override void OnResponse(GumpInstance gi, GumpResponse gr, object[] args) {
			//seznam crimu bereme z parametru (mohl byt jiz trideny atd, nebudeme ho proto selectit znova)
			List<AccountCrime> crimesList = (List<AccountCrime>)args[3];
			int firstOnPage = Convert.ToInt32(args[2]);
			if(gr.pressedButton < 10) { //ovladaci tlacitka (exit, new, tridit)				
				switch(gr.pressedButton) {
					case 0: //exit
						DialogStacking.ShowPreviousDialog(gi); //zobrazit pripadny predchozi dialog
						break;
					case 1: //zalozit novy trest														trest, char, account
						GumpInstance newGi = gi.Cont.Dialog(SingletonScript<D_New_AccountNote>.Instance, true, null, args[0]);
						DialogStacking.EnstackDialog(gi, newGi); //vlozime napred dialog do stacku
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
			} else if(ImprovedDialog.PagingButtonsHandled(gi, gr, 2, crimesList.Count, 1)) {//kliknuto na paging? (1 = index parametru nesoucim info o pagingu (zde dsi.Args[2] viz výše)
				//1 sloupecek
				return;
			} else {
				//zjistime si radek
				int row = ((int)gr.pressedButton - 10) / 5;
				int buttNo = ((int)gr.pressedButton - 10) % 5;
				AccountCrime crime = crimesList[row];
				GumpInstance newGi;
				switch(buttNo) {
					case 0: //char info
						newGi = gi.Cont.Dialog(SingletonScript<D_Info>.Instance, crime.referredChar, 0, 0);
						DialogStacking.EnstackDialog(gi, newGi);
						break;
					case 1: //text prohresku
						//první parametr je nadpis, druhy je zobrazeny text)
						newGi = gi.Cont.Dialog(D_Display_Text.Instance, "Popis prohøešku", crime.text);
						DialogStacking.EnstackDialog(gi, newGi);
						break;
					case 2: //text trestu
						//první parametr je nadpis, druhy je zobrazeny text)
						newGi = gi.Cont.Dialog(D_Display_Text.Instance, "Popis trestu", crime.punishment);
						DialogStacking.EnstackDialog(gi, newGi);
						break;
					case 3: //issuer info
						newGi = gi.Cont.Dialog(SingletonScript<D_Info>.Instance, crime.issuer, 0, 0);
						DialogStacking.EnstackDialog(gi, newGi);
						break;
					case 4: //smazat trest (to muze jen jeji autor nebo clovek s vyssim plevelem)
						ScriptedAccount acc = (ScriptedAccount)args[0];
						acc.RemoveCrime(crime);
						args[3] = null;
						DialogStacking.ResendAndRestackDialog(gi);
						break;
				}
			}
		}

		[Remark("Display an account crimes. Function accessible from the game." +
				"The function is designed to be triggered using .x AccCrimes or .AccCrimes" +
				"but it can be also called from other dialogs - such as info..." +
				"Default sorting is by Time, desc." +
				"Another way to use is: .AccCrimes('acc_name'[,'sorting']).")]
		[SteamFunction]
		public static void AccCrimes(Thing self, ScriptArgs text) {
			//Parametry dialogu:
			//1. parametr - account
			//2. param - trideni dle...
			//3. od kolikate poznamky zaciname (0), 3. prostor pro potreby dialogu
			if(text == null || text.argv == null || text.argv.Length == 0) {
				AbstractCharacter refChar = self as AbstractCharacter;
				if(refChar == null) {
					Globals.SrcCharacter.SysMessage("Chybne zamereni, zamer hrace", (int)Hues.Red);
					return;
				}
				if(refChar.IsPlayer) {
					Globals.SrcCharacter.Dialog(SingletonScript<D_AccountCrimes>.Instance, refChar.Account, AccountNotesSorting.TimeDesc, 0, null);
				} else {
					Globals.SrcCharacter.SysMessage("Zameruj hrace!");
				}
			} else {
				ScriptedAccount acc = (ScriptedAccount)AbstractAccount.Get(text.argv[0].ToString());
				if(acc == null) {
					Globals.SrcCharacter.SysMessage("Account se jménem " + text.argv[0].ToString() + " neexistuje.", (int)Hues.Red);
					return;
				}
				if(text.argv.Length == 1) { //mame jen nazev accountu
					Globals.SrcCharacter.Dialog(SingletonScript<D_AccountCrimes>.Instance, acc, AccountNotesSorting.TimeDesc, 0, null);
				} else { //mame i trideni
					Globals.SrcCharacter.Dialog(SingletonScript<D_AccountCrimes>.Instance, acc, (AccountNotesSorting)text.argv[1], 0, null);
				}
			}
		}
	}

	public class D_New_AccountNote : CompiledGump {
		public override void Construct(Thing focus, AbstractCharacter sendTo, object[] args) {
			bool isCrime = (bool)args[0]; //true - crime note, false - normal note
			AbstractCharacter refChar = args[1] as AbstractCharacter; //if not present the note is for the whole account
			ScriptedAccount acc = args[2] as ScriptedAccount; //the account could or might not have arrived... :]

			string dlgHeadline; //crime / note
			string textFldLabel; //crime desc / record
			string target; //who is this note for?
			if(refChar != null) { //we have player, we dont care for the account any more
				target = "hráèe " + refChar.Name;
			} else {
				if(acc != null) {
					target = "account " + acc.Name;
				} else {
					target = "account";
				}
			}
			if(isCrime) {
				dlgHeadline = "Nový záznam trestu pro " + target;
				textFldLabel = "Prohøešek";
			} else {
				dlgHeadline = "Nová poznámka pro " + target;
				textFldLabel = "Poznámka";
			}

			ImprovedDialog dlg = new ImprovedDialog(this.GumpInstance);
			dlg.CreateBackground(400);
			dlg.SetLocation(50, 50);

			dlg.Add(new GUTATable(1, 0, ButtonFactory.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = TextFactory.CreateHeadline(dlgHeadline);
			dlg.LastTable[0, 1] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonCross, 0);
			dlg.MakeTableTransparent();

			dlg.Add(new GUTATable((isCrime ? 3 : 2), 0, 275)); //1.sl - edit nazev, 2.sl - edit hodnota
			dlg.LastTable[1, 0] = TextFactory.CreateLabel(textFldLabel);
			if(refChar == null) {
				dlg.LastTable[0, 0] = TextFactory.CreateLabel("Account");
				if(acc == null) { //account didnt come					
					dlg.LastTable[0, 1] = InputFactory.CreateInput(LeafComponentTypes.InputText, 10);
				} else {
					dlg.LastTable[0, 1] = TextFactory.CreateText(acc.Name);
				}
			} else {//we have player
				dlg.LastTable[0, 0] = TextFactory.CreateLabel("Postava");
				dlg.LastTable[0, 1] = TextFactory.CreateText(refChar.Name);				
			}
			dlg.LastTable[1, 1] = InputFactory.CreateInput(LeafComponentTypes.InputText, 11);
			if(isCrime) { //crimes have more fields...
				dlg.LastTable[2, 0] = TextFactory.CreateLabel("Trest");
				dlg.LastTable[2, 1] = InputFactory.CreateInput(LeafComponentTypes.InputText, 12);
			}
			dlg.MakeTableTransparent();

			dlg.Add(new GUTATable(1, ButtonFactory.D_BUTTON_WIDTH, 0));
			dlg.LastTable[0, 0] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonTick, 1);
			dlg.LastTable[0, 1] = TextFactory.CreateLabel("Uložit");
			dlg.MakeTableTransparent(); 

			dlg.WriteOut();
		}

		public override void OnResponse(GumpInstance gi, GumpResponse gr, object[] args) {
			bool isCrime = (bool)args[0];
			AbstractCharacter refChar = args[1] as AbstractCharacter;
			ScriptedAccount acc = args[2] as ScriptedAccount;

			if(gr.pressedButton == 0) {
				DialogStacking.ShowPreviousDialog(gi);			
			} else if(gr.pressedButton == 1) {
				if(refChar != null) {//creating note for the player
					acc = (ScriptedAccount)refChar.Account; //get the account
				} else {
					if(acc == null) {//try to load the account
						string accName = gr.GetTextResponse(10);
						acc = (ScriptedAccount)AbstractAccount.Get(accName);
						if(acc == null) {//failed to find the acc
							gi.Cont.SysMessage("Account se jménem "+accName+" neexistuje!",(int)Hues.Red);
							DialogStacking.ResendAndRestackDialog(gi);
						}
					}
				}
				//and now the note itself
				string noteDesc = gr.GetTextResponse(11);					
				if(isCrime) {
					string punishmentDesc = gr.GetTextResponse(12);
					AccountCrime newCrime = new AccountCrime(gi.Cont, refChar, punishmentDesc, noteDesc);
					acc.AddCrime(newCrime);
				} else {
					AccountNote newNote = new AccountNote(gi.Cont, refChar, noteDesc);
					acc.AddNote(newNote);
				}
				//if the previous dialog is the accnotes or acccrimes list, we have to clear the list in the stacked instance
				//so it can be reread again with the newly created note
				GumpInstance prevStacked = DialogStacking.PopStackedDialog(gi);
				if(prevStacked != null) {
					if(prevStacked.def.GetType().IsAssignableFrom(typeof(D_AccountNotes)) ||
					   prevStacked.def.GetType().IsAssignableFrom(typeof(D_AccountCrimes))) {
						prevStacked.InputParams[3] = null;
					}
					DialogStacking.ResendAndRestackDialog(prevStacked);
				}				
			} 
		}

		[Remark("Display the dialog for creating a new AccountNote."+
				"Is called directly by .x NewAccNote on the target player")]
		[SteamFunction]
		public static void NewAccNote(Thing self, ScriptArgs text) {
			//dialog parameters: 
			//1 - true (isCrime) / false (isNote)
			//2 - referred character
			//3 - referred account
			if(text == null || text.argv == null || text.argv.Length == 0) {
				//no player, no account specified - we will use the "self" as the target player
				AbstractCharacter refChar = self as AbstractCharacter;
				if(refChar == null) {
					Globals.SrcCharacter.SysMessage("Chybne zamereni, zamer playera", (int)Hues.Red);
				}
				if(refChar.IsPlayer) {
					Globals.SrcCharacter.Dialog(SingletonScript<D_New_AccountNote>.Instance, false, self, null);
				} else {
					Globals.SrcCharacter.SysMessage("Zameruj hrace!");
				}
			} else {
				//we dont support parameters here!
				Globals.SrcCharacter.SysMessage("Pouziti: .NewAccNote - nova poznamka na sebe; .x NewAccNote - nova poznamka na zamereneho hrace");
			}
		}

		[Remark("Display the dialog for creating a new AccountCrime." +
				"Is called directly by .x NewAccCrime on the target player")]
		[SteamFunction]
		public static void NewAccCrime(Thing self, ScriptArgs text) {
			//dialog parameters: 
			//1 - true (isCrime) / false (isNote)
			//2 - referred character
			//3 - referred account
			if(text == null || text.argv == null || text.argv.Length == 0) {
				//no player, no account specified - we will use the "self" as the target player
				AbstractCharacter refChar = self as AbstractCharacter;
				if(refChar == null) {
					Globals.SrcCharacter.SysMessage("Chybne zamereni, zamer playera", (int)Hues.Red);
				}
				if(refChar.IsPlayer) {
					Globals.SrcCharacter.Dialog(SingletonScript<D_New_AccountNote>.Instance, true, self, null);
				} else {
					Globals.SrcCharacter.SysMessage("Zameruj hrace!");
				}
			} else {
				//we dont support parameters here!
				Globals.SrcCharacter.SysMessage("Pouziti: .NewAccCrime - novy trest pro sebe :); .x NewAccCrime - novy trest na zamereneho hrace");
			}
		}
	}
}
