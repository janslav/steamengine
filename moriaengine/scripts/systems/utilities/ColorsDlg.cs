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

	[Remark("Dialog that will display all colors examples - useful for determining which color we need.")]
	public class D_Colors : CompiledGump {
		static readonly int lastColor = 2999;

		static readonly int columnsCnt = 10; //kolik sloupecku bude mit dialog?
		static readonly int dlgWidth = 850; //sirka dialogu
		
		//seznam zpracovavanych barev pro tuto verzi dialogu
		static readonly TagKey _colorsLst_ = TagKey.Get("_colorsLst_");
		//prvni barva od ktere se pojede 
		static readonly TagKey _startingColor_ = TagKey.Get("_startingColor_");

        [Remark("Instance of the D_Colors, for possible access from other dialogs etc.")]
        private static D_Colors instance;
		public static D_Colors Instance {
			get {
				return instance;
			}
		}
        [Remark("Set the static reference to the instance of this dialog")]
		public D_Colors() {
			instance = this;
		}

		public override void Construct(Thing focus, AbstractCharacter sendTo, object[] sa) {
			//zjistit zda bude paging, najit maximalni index na strance
			int startingColor = Convert.ToInt32(sa[0]); //cislol barvy od ktere (pocinaje) se zobrazi vsechny ostatni 
			int firstiVal = Convert.ToInt32(sa[1]);   //prvni barva na strance - pro paging

			//int[] colorsList = prepareColorList(startingColor);
			//ulozit tento seznam do tagu
			//this.GumpInstance.SetTag(_colorsLst_, colorsList);

			//maximalni index (20 radku mame) + hlidat konec seznamu...
			int imax = Math.Min(firstiVal + (ImprovedDialog.PAGE_ROWS*columnsCnt), lastColor);
			
			ImprovedDialog dlg = new ImprovedDialog(this.GumpInstance);
			//pozadi    
			dlg.CreateBackground(dlgWidth);
			dlg.SetLocation(50, 50);

			//nadpis
			dlg.Add(new GUTATable(1, 0, ButtonFactory.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = TextFactory.CreateHeadline("Colors dialog - ukázky barev v textu (poèínaje " + startingColor + ")");
			//cudlik na zavreni dialogu
			dlg.LastTable[0, 1] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonCross, 0);
			dlg.MakeTableTransparent();

			//input field pro vyber barvy
			dlg.Add(new GUTATable(1, 100, 40, 0));
			dlg.LastTable[0, 0] = TextFactory.CreateLabel("Zadej poèáteèní barvu: ");
			dlg.LastTable[0, 1] = InputFactory.CreateInput(LeafComponentTypes.InputNumber, 10, startingColor.ToString());
			dlg.LastTable[0, 2] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonPaper, 1);
			dlg.MakeTableTransparent(); //zpruhledni zbytek dialogu

			//sloupecky - napred pripravime sirky
			int[] columns = new int[columnsCnt];
			for(int i = 0; i < columns.Length; i++) {
				//columns[i] = dlgWidth / columnsCnt;
				columns[i] = 80;
			}
			dlg.Add(new GUTATable(ImprovedDialog.PAGE_ROWS, columns));
			int colorCntr = firstiVal; //zacneme od te, ktera ma byt na strance prvni
			for(int i = 0; i < columnsCnt; i++) {//pro kazdy sloupecek
				for(int j = 0; j < ImprovedDialog.PAGE_ROWS && colorCntr <= lastColor ; j++, colorCntr++) { //a v nem kazdy radek
					//vlozit priklad jedne pouzite barvy (dokud nedojdou barvy)
					dlg.LastTable[j, i] = TextFactory.CreateText(colorCntr, "Color(" + colorCntr + ")");
				}
			}
			dlg.MakeTableTransparent(); //zpruhledni zbytek dialogu

			//now handle the paging 
			dlg.CreatePaging(lastColor, firstiVal, columnsCnt);

			dlg.WriteOut();
		}

		public override void OnResponse(GumpInstance gi, GumpResponse gr, object[] args) {
			//seznam barev 
			//int[] colorsList = (int[])gi.GetTag(_colorsLst_);
            if(gr.pressedButton < 10) { //ovladaci tlacitka (sorting, paging atd)
				switch(gr.pressedButton) {
                    case 0: //exit
						DialogStackItem.ShowPreviousDialog(gi.Cont.Conn); //zobrazit pripadny predchozi dialog						
                        break;
                    case 1: //vybrat prvni barvu
						args[0] = (int)gr.GetNumberResponse(10); //vezmi zvolenou prvni barvu
						args[1] = (int)gr.GetNumberResponse(10); //ta bude zaroven prvni na strance
						gi.Cont.SendGump(gi);
                        break;
                }
			} else if(ImprovedDialog.PagingButtonsHandled(gi, gr, 1, lastColor, columnsCnt)) {//kliknuto na paging? (1 = index parametru nesoucim info o pagingu (zde dsi.Args[1] viz výše)
				//zde je sloupecku vice (columnsCnt, viz nahore)
				return;
			} 
		}

		[Remark("Prepare an array of colors to be displayed")]
		private int[] prepareColorList(int startingColor) {			
			int[] retArr = new int[lastColor - startingColor + 1];
			for(int i = startingColor, j=0; i <= lastColor; i++,j++) {
				retArr[j] = i;
			}
			return retArr;
		}
	}

	public static class UtilityFunctions {
		[Remark("Display a Colors dialog")]
		[SteamFunction] 
		public static void ColorsDialog(AbstractCharacter sender, ScriptArgs text) {			
			if(text == null || text.Args.Length == 0) {
				//zaciname od nulte barvy
				sender.Dialog(D_Colors.Instance, 0, 0);
			} else {
				//zacneme od zvolene barvy
				sender.Dialog(D_Colors.Instance, Convert.ToInt32(text.Argv[0]), Convert.ToInt32(text.Argv[0]));
			}
		}		
	}
}