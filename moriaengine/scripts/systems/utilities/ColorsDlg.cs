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
		
		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			//zjistit zda bude paging, najit maximalni index na strance
			int startingColor = Convert.ToInt32(args.ArgsArray[0]); //cislo barvy od ktere (pocinaje) se zobrazi vsechny ostatni 
			int firstiVal = Convert.ToInt32(args.ArgsArray[1]);   //prvni barva na strance - pro paging
			
			//maximalni index (20 radku mame) + hlidat konec seznamu...
			int imax = Math.Min(firstiVal + (ImprovedDialog.PAGE_ROWS*columnsCnt), lastColor);
			
			ImprovedDialog dlg = new ImprovedDialog(this.GumpInstance);
			//pozadi    
			dlg.CreateBackground(dlgWidth);
			dlg.SetLocation(50, 50);

			//nadpis
			dlg.AddTable(new GUTATable(1, 0, ButtonFactory.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = TextFactory.CreateHeadline("Colors dialog - uk�zky barev v textu (po��naje " + startingColor + ")");
			//cudlik na zavreni dialogu
			dlg.LastTable[0, 1] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonCross, 0);
			dlg.MakeLastTableTransparent();

			//input field pro vyber barvy
			dlg.AddTable(new GUTATable(1, 100, 40, 0));
			dlg.LastTable[0, 0] = TextFactory.CreateLabel("Zadej po��te�n� barvu: ");
			dlg.LastTable[0, 1] = InputFactory.CreateInput(LeafComponentTypes.InputNumber, 10, startingColor.ToString());
			dlg.LastTable[0, 2] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonPaper, 1);
			dlg.MakeLastTableTransparent(); //zpruhledni zbytek dialogu

			//sloupecky - napred pripravime sirky
			int[] columns = new int[columnsCnt];
			for(int i = 0; i < columns.Length; i++) {
				//columns[i] = dlgWidth / columnsCnt;
				columns[i] = 80;
			}
			dlg.AddTable(new GUTATable(ImprovedDialog.PAGE_ROWS, columns));
			int colorCntr = firstiVal; //zacneme od te, ktera ma byt na strance prvni
			for(int i = 0; i < columnsCnt; i++) {//pro kazdy sloupecek
				for(int j = 0; j < ImprovedDialog.PAGE_ROWS && colorCntr <= lastColor ; j++, colorCntr++) { //a v nem kazdy radek
					//vlozit priklad jedne pouzite barvy (dokud nedojdou barvy)
					dlg.LastTable[j, i] = TextFactory.CreateText(colorCntr, "Color(" + colorCntr + ")");
				}
			}
			dlg.MakeLastTableTransparent(); //zpruhledni zbytek dialogu

			//now handle the paging 
			dlg.CreatePaging(lastColor, firstiVal, columnsCnt);

			dlg.WriteOut();
		}

		public override void OnResponse(GumpInstance gi, GumpResponse gr, DialogArgs args) {
			if(gr.pressedButton < 10) { //ovladaci tlacitka (sorting, paging atd)
				switch(gr.pressedButton) {
                    case 0: //exit
						DialogStacking.ShowPreviousDialog(gi); //zobrazit pripadny predchozi dialog						
                        break;
                    case 1: //vybrat prvni barvu
						args.ArgsArray[0] = (int)gr.GetNumberResponse(10); //vezmi zvolenou prvni barvu
						args.ArgsArray[1] = (int)gr.GetNumberResponse(10); //ta bude zaroven prvni na strance
						DialogStacking.ResendAndRestackDialog(gi);
                        break;
                }
			} else if(ImprovedDialog.PagingButtonsHandled(gi, gr, lastColor, columnsCnt)) {//kliknuto na paging? (1 = index parametru nesoucim info o pagingu (zde dsi.Args[1] viz v��e)
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

		[Remark("Display a Colors dialog. Can be called without parameters (then the first displayed color will be the 0th)"+
				"or with one parameter (number of color which will be taken as the first in the dialog)")]
		[SteamFunction]
		public static void ColorsDialog(AbstractCharacter sender, ScriptArgs text) {
			if(text == null || text.Args.Length == 0) {
				//zaciname od nulte barvy
				sender.Dialog(SingletonScript<D_Colors>.Instance, new DialogArgs(0,0)); //zaciname od 0. barvy, a 0. barva bude prvni na strance
			} else {
				//zacneme od zvolene barvy (argv0 bude prvni na strance i se od ni bude zacinat)
				sender.Dialog(SingletonScript<D_Colors>.Instance, new DialogArgs(Convert.ToInt32(text.argv[0]), Convert.ToInt32(text.argv[0])));
			}
		}	
	}
}