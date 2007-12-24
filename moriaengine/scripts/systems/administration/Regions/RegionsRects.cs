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
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts.Dialogs {
	[Remark("Dialog for dispalying the regions rectangles")]
	public class D_Region_Rectangles : CompiledGump {
		private static int width = 450;

		[Remark("Seznam parametru: 0 - region" +
				"	1 - paging"+
				"	2 - rectangly v listu(je totiž možno pøidávat dynamicky)")]
		public override void Construct(Thing focus, AbstractCharacter sendTo, object[] args) {
			Region reg = (Region)args[0];
			List<MutableRectangle> rectList = null;
			if(args[2] != null) {
				rectList = (List<MutableRectangle>)args[2];				
			} else {
				//vezmeme je z regionu
				rectList = MutableRectangle.TakeRectsFromRegion(reg);
				args[2] = rectList; //ulozime to do argumentu dialogu
			}

			//zjistit zda bude paging, najit maximalni index na strance
			int firstiVal = Convert.ToInt32(args[1]);   //prvni index na strance
			int imax = Math.Min(firstiVal + ImprovedDialog.PAGE_ROWS, rectList.Count);

			ImprovedDialog dlg = new ImprovedDialog(this.GumpInstance);
			//pozadi    
			dlg.CreateBackground(width);
			dlg.SetLocation(50, 50);

			//nadpis
			dlg.Add(new GUTATable(1, 0, ButtonFactory.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = TextFactory.CreateHeadline("Pøehled rectanglù pro region " + reg.Name + "(" + reg.Defname + ") (zobrazeno " + (firstiVal + 1) + "-" + imax + " z " + rectList.Count + ")");
			dlg.LastTable[0, 1] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonCross, 0);//cudlik na zavreni dialogu
			dlg.MakeTableTransparent();

			//cudlik na zalozeni noveho rectanglu
			dlg.Add(new GUTATable(1, 130, ButtonFactory.D_BUTTON_WIDTH, 0));
			dlg.LastTable[0, 0] = TextFactory.CreateLabel("Pøidat rectangle");
			dlg.LastTable[0, 1] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonOK, 1);
			dlg.MakeTableTransparent();

			//popis sloupcu (Info, Height, Width, StartPoint, EndPoint, Tiles)
			dlg.Add(new GUTATable(1, ButtonFactory.D_BUTTON_WIDTH, ButtonFactory.D_BUTTON_WIDTH, 35, 35, 130, 0, 35));
			dlg.LastTable[0, 0] = TextFactory.CreateLabel("Edit");
			dlg.LastTable[0, 1] = TextFactory.CreateLabel("Smaž");			
			dlg.LastTable[0, 2] = TextFactory.CreateLabel("Šíøka");
			dlg.LastTable[0, 3] = TextFactory.CreateLabel("Výška");
			dlg.LastTable[0, 4] = TextFactory.CreateLabel("Poèáteèní bod");
			dlg.LastTable[0, 5] = TextFactory.CreateLabel("Koncový bod");
			dlg.LastTable[0, 6] = TextFactory.CreateLabel("Polí");
			dlg.MakeTableTransparent();

			//seznam rectnaglu
			dlg.Add(new GUTATable(imax - firstiVal));
			dlg.CopyColsFromLastTable();

			//projet seznam v ramci daneho rozsahu indexu
			int rowCntr = 0;
			for(int i = firstiVal; i < imax; i++) {
				MutableRectangle rect = rectList[i];

				dlg.LastTable[rowCntr, 0] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonPaper, 10 + (2 * i)); //editovat
				dlg.LastTable[rowCntr, 1] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonCross, 11 + (2 * i)); //smazat
				dlg.LastTable[rowCntr, 2] = TextFactory.CreateText(rect.Width.ToString());
				dlg.LastTable[rowCntr, 3] = TextFactory.CreateText(rect.Height.ToString());
				dlg.LastTable[rowCntr, 4] = TextFactory.CreateText("(" + rect.MinX + "," + rect.MinY + ")");
				dlg.LastTable[rowCntr, 5] = TextFactory.CreateText("(" + rect.MaxX + "," + rect.MaxY + ")");
				dlg.LastTable[rowCntr, 6] = TextFactory.CreateText(rect.TilesNumber.ToString());

				rowCntr++;
			}
			dlg.MakeTableTransparent(); //zpruhledni zbytek dialogu

			//send button
			dlg.Add(new GUTATable(1, ButtonFactory.D_BUTTON_WIDTH, 0));
			dlg.LastTable[0, 0] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonOK, 2);
			dlg.LastTable[0, 1] = TextFactory.CreateText("Uložit");
			dlg.MakeTableTransparent();

			dlg.CreatePaging(rectList.Count, firstiVal, 1);//ted paging		
			dlg.WriteOut();
		}

		public override void OnResponse(GumpInstance gi, GumpResponse gr, object[] args) {
			//seznam rectanglu bereme z parametru (mohl byt nejaky pridan/smazan)
			Region reg = (Region)args[0];
			List<MutableRectangle> rectsList = (List<MutableRectangle>)args[2];
			int firstOnPage = Convert.ToInt32(args[1]);
			if(gr.pressedButton < 10) { //ovladaci tlacitka (exit, new, tridit)				
				switch(gr.pressedButton) {
					case 0: //exit
						DialogStacking.ShowPreviousDialog(gi); //zobrazit pripadny predchozi dialog
						break;
					case 1: //zalozit novy rectangle
						//nemazat rectangly - budeme do nich ukladat novy						
						GumpInstance newGi = gi.Cont.Dialog(SingletonScript<D_New_Rectangle>.Instance, rectsList, null, null, null, null);
						DialogStacking.EnstackDialog(gi, newGi); //vlozime napred dialog do stacku
						break;
					case 2: //ulozit
						args[2] = null; //vycistit odkaz na seznam - prenacteme ho
						//DialogStacking.EnstackDialog(gi, newGi); //vlozime napred dialog do stacku
						break;
				}
			} else if(ImprovedDialog.PagingButtonsHandled(gi, gr, 1, rectsList.Count, 1)) {//kliknuto na paging? (1 = index parametru nesoucim info o pagingu (zde dsi.Args[2] viz výše)
				//1 sloupecek
				return;
			} else {
				//zjistime si radek a cudlik v nem
				int row = ((int)gr.pressedButton - 10) / 2;
				int buttNo = ((int)gr.pressedButton - 10) % 2;
				MutableRectangle rect = rectsList[row];
				GumpInstance newGi;
				switch(buttNo) {
					case 0: //region rectangle info
						newGi = gi.Cont.Dialog(SingletonScript<D_Info>.Instance, rect, 0, 0);
						DialogStacking.EnstackDialog(gi, newGi);
						break;
					case 1: //smazat rectangle
						rectsList.Remove(rect); //remove z listu, list nenulujeme zejo .)
						DialogStacking.ResendAndRestackDialog(gi);
						break;
				}
			}
		}		
	}
}