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

	[Remark("Dialog listing all players timers")]
	public class D_TimerList : CompiledGump {
		[Remark("Instance of the D_TimerList, for possible access from other dialogs etc.")]
		private static D_TimerList instance;
		public static D_TimerList Instance {
			get {
				return instance;
			}
		}
        [Remark("Set the static reference to the instance of this dialog")]
		public D_TimerList() {
			instance = this;
		}

		public override void Construct(Thing focus, AbstractCharacter src, object[] sa) {
			//seznam accountu vyhovujici zadanemu parametru, ulozit na dialog
			List<GameAccount> accList = GameAccount.RetreiveByStr(sa[1].ToString());
			accList.Sort(AccountComparer.instance);
			sa[2] = accList; //ulozime to do argumentu dialogu
			//zjistit zda bude paging, najit maximalni index na strance
			int firstiVal = Convert.ToInt32(sa[0]);   //prvni index na strance
			//maximalni index (20 radku mame) + hlidat konec seznamu...			
			int imax = Math.Min(firstiVal + ImprovedDialog.PAGE_ROWS, accList.Count);
			
			ImprovedDialog dlg = new ImprovedDialog(this.GumpInstance);
			//pozadi    
			dlg.CreateBackground(400);
			dlg.SetLocation(50, 50);

			//nadpis
			dlg.Add(new GUTATable(1, 0, ButtonFactory.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = TextFactory.CreateText("Seznam hr��sk�ch ��t� (" + (firstiVal + 1) + "-" + imax + " z " + accList.Count + ")");
			//cudlik na zavreni dialogu
			dlg.LastTable[0, 1] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonCross, 0);
			dlg.MakeTableTransparent();
			
			//cudlik a input field na zuzeni vyberu
			dlg.Add(new GUTATable(1,130,0,ButtonFactory.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = TextFactory.CreateText("Vyhled�vac� kriterium");
			dlg.LastTable[0, 1] = InputFactory.CreateInput(LeafComponentTypes.InputText,33);
			dlg.LastTable[0, 2] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonPaper,1);
			dlg.MakeTableTransparent();

			//cudlik na zalozeni noveho uctu
			dlg.Add(new GUTATable(1,130,ButtonFactory.D_BUTTON_WIDTH,0));
			dlg.LastTable[0, 0] = TextFactory.CreateText("Zalo�it nov� account");
			dlg.LastTable[0, 1] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonOK,2);
			dlg.MakeTableTransparent();

			//seznam uctu s tlacitkem pro detail (tolik radku, kolik se bude zobrazovat uctu)
			dlg.Add(new GUTATable(imax-firstiVal, ButtonFactory.D_BUTTON_WIDTH, 0)); 
			//cudlik pro info
			dlg.LastTable[0, 0] = TextFactory.CreateText("Come");
			//projet seznam v ramci daneho rozsahu indexu
			int rowCntr = 0;
			for(int i = firstiVal; i < imax; i++) {
				GameAccount ga = accList[i];
				Hues nameColor = ga.Online ? Hues.OnlineColor : Hues.OfflineColor;

				dlg.LastTable[rowCntr, 0] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonTick, i + 10); //account info
				dlg.LastTable[rowCntr, 1] = TextFactory.CreateText(nameColor, ga.Name); //acc name
				
				rowCntr++;			
			}
			dlg.MakeTableTransparent(); //zpruhledni zbytek dialogu

			//now handle the paging 
			dlg.CreatePaging(accList.Count, firstiVal,1);

			//ulo�it info o pr�v� vytvo�en�m dialogu pro n�vrat
			DialogStackItem.EnstackDialog(src, focus, D_TimerList.Instance,
					firstiVal, //cislo polozky kterou zacina stranka (pro paging)	
					sa[1], //zde je ulozena informace pro vyber uctu dle jmena
					sa[2]); //toto je seznam accountu

			dlg.WriteOut();
		}

		public override void OnResponse(GumpInstance gi, GumpResponse gr) {
			//vzit "tenhle" dialog ze stacku
			DialogStackItem dsi = DialogStackItem.PopStackedDialog(gi.Cont.Conn);			

			//seznam hracu bereme z parametru (mohl byt jiz trideny atd, nebudeme ho proto selectit znova)
			List<GameAccount> accList = (List<GameAccount>)dsi.Args[2];
			int firstOnPage = (int)dsi.Args[0];
            if(gr.pressedButton < 10) { //ovladaci tlacitka (exit, new, vyhledej)				
                switch(gr.pressedButton) {
                    case 0: //exit
						DialogStackItem.ShowPreviousDialog(gi.Cont.Conn); //zobrazit pripadny predchozi dialog
						//aktualni dialog uz byl vynat ze stacku, takze se opravdu zobrazi ten minuly
                        break;
                    case 1: //vyhledat dle zadani
						string nameCriteria = gr.GetTextResponse(33);
						dsi.Args[0] = 0; //zrusit info o prvnich indexech - seznam se cely zmeni tim kriteriem
						dsi.Args[1] = nameCriteria; //uloz info o vyhledavacim kriteriu
						dsi.Args[2] = null; //vycistit soucasny odkaz
						dsi.Show();
                        break;
                    case 2: //zalozit novy acc.
						DialogStackItem.EnstackDialog(gi.Cont, dsi); //vlozime napred dialog zpet do stacku
						gi.Cont.Dialog(D_NewAccount.Instance);
						break;                    
                }
			} else if(ImprovedDialog.PagingButtonsHandled(gi, gr, 0, accList.Count,1)) {//kliknuto na paging? (0 = index parametru nesoucim info o pagingu (zde dsi.Args[0] viz v��e)
				//1 sloupecek
				return;
			} else { //skutecna talcitka z radku
                //zjistime kterej cudlik z kteryho radku byl zmacknut
                int row = (int)(gr.pressedButton - 10);
				int listIndex = firstOnPage + row;
				GameAccount ga = accList[row];
				DialogStackItem.EnstackDialog(gi.Cont, dsi); //vlozime napred dialog zpet do stacku
				gi.Cont.Dialog(D_AccInfo.Instance, ga);               
                
            }
		}		
	}

	[Remark("Comparer for sorting accounts by account name asc")]
	public class TimersComparer : IComparer<GameAccount> {
		public readonly static TimersComparer instance = new TimersComparer();

		private TimersComparer() {
			//soukromy konstruktor, pristupovat budeme pres instanci
		}

		public int Compare(GameAccount x, GameAccount y) {
			return string.Compare(x.Name, y.Name);
		}
	}
}