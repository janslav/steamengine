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
	public class D_Regions_Rectangles : CompiledGump {
		private static int width = 600;

		[Remark("Seznam parametru: 0 - region" +
				"	1 - paging"+
				"	2 - rectangly v listu(je totiž možno pøidávat dynamicky)")]
		public override void Construct(Thing focus, AbstractCharacter sendTo, object[] args) {
			Region reg = (Region)args[0];
			List<RegionRectangle> rectList = null;
			if(args[2] != null) {
				rectList = (List<RegionRectangle>)args[2];				
			} else {
				//vezmeme je z regionu
				rectList = new List<RegionRectangle>(reg.Rectangles);
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
			dlg.Add(new GUTATable(1, ButtonFactory.D_BUTTON_WIDTH,30,30,230,0,40));
			dlg.LastTable[0, 0] = TextFactory.CreateLabel("Edit");
			dlg.LastTable[0, 1] = TextFactory.CreateLabel("Šíøka");
			dlg.LastTable[0, 2] = TextFactory.CreateLabel("Výška");
			dlg.LastTable[0, 3] = TextFactory.CreateLabel("Poèáteèní bod");
			dlg.LastTable[0, 4] = TextFactory.CreateLabel("Koncový bod");
			dlg.LastTable[0, 5] = TextFactory.CreateLabel("Polí");
			dlg.MakeTableTransparent();

			//seznam rectnaglu
			dlg.Add(new GUTATable(imax - firstiVal));
			dlg.CopyColsFromLastTable();

			//projet seznam v ramci daneho rozsahu indexu
			int rowCntr = 0;
			for(int i = firstiVal; i < imax; i++) {
				RegionRectangle rect = rectList[i];

				dlg.LastTable[rowCntr, 0] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonPaper, 10 + i); //editovat
				dlg.LastTable[rowCntr, 1] = TextFactory.CreateText(rect.Width.ToString());
				dlg.LastTable[rowCntr, 2] = TextFactory.CreateText(rect.Height.ToString());
				dlg.LastTable[rowCntr, 3] = TextFactory.CreateText(rect.StartPoint.ToString());
				dlg.LastTable[rowCntr, 4] = TextFactory.CreateText(rect.EndPoint.ToString());
				dlg.LastTable[rowCntr, 5] = TextFactory.CreateText(rect.TilesNumber.ToString());

				rowCntr++;
			}
			dlg.MakeTableTransparent(); //zpruhledni zbytek dialogu
			dlg.CreatePaging(rectList.Count, firstiVal, 1);//ted paging			
			dlg.WriteOut();
		}

		public override void OnResponse(GumpInstance gi, GumpResponse gr, object[] args) {
			//seznam rectanglu bereme z parametru (mohl byt nejaky pridan/smazan)
			Region reg = (Region)args[0];
			List<RegionRectangle> rectsList = (List<RegionRectangle>)args[2];
			int firstOnPage = Convert.ToInt32(args[1]);
			//if(gr.pressedButton < 10) { //ovladaci tlacitka (exit, new, tridit)				
			//    switch(gr.pressedButton) {
			//        case 0: //exit
			//            DialogStacking.ShowPreviousDialog(gi); //zobrazit pripadny predchozi dialog
			//            break;
			//        case 1: //zalozit novy rectangle
			//            //dummy
			//            //newRect.region = reg;
			//            args[2] = null; //vycistit odkaz na seznam - prenacteme ho
			//            //GumpInstance newGi = gi.Cont.Dialog(SingletonScript<D_New_Region>.Instance);
			//            //DialogStacking.EnstackDialog(gi, newGi); //vlozime napred dialog do stacku
			//            break;
			//        case 2: //vyhledavat / zuzit vyber
			//            string nameCriteria = gr.GetTextResponse(33);
			//            args[0] = nameCriteria; //uloz info o vyhledavacim kriteriu
			//            args[1] = 0; //zrusit info o prvnich indexech - seznam se cely zmeni tim kriteriem												
			//            args[2] = null; //vycistit soucasny odkaz na regionlist aby se mohl prenacist
			//            DialogStacking.ResendAndRestackDialog(gi);
			//            break;
			//        case 3: //name asc						
			//            args[2] = null;
			//            args[3] = RegionsSorting.NameAsc;
			//            DialogStacking.ResendAndRestackDialog(gi);
			//            break;
			//        case 4: //name desc
			//            args[2] = null;
			//            args[3] = RegionsSorting.NameDesc;
			//            DialogStacking.ResendAndRestackDialog(gi);
			//            break;
			//        case 5: //defname asc
			//            args[2] = null;
			//            args[3] = RegionsSorting.DefnameAsc;
			//            DialogStacking.ResendAndRestackDialog(gi);
			//            break;
			//        case 6: //defname desc
			//            args[2] = null;
			//            args[3] = RegionsSorting.DefnameDesc;
			//            DialogStacking.ResendAndRestackDialog(gi);
			//            break;
			//    }
			//} else if(ImprovedDialog.PagingButtonsHandled(gi, gr, 1, rectsList.Count, 1)) {//kliknuto na paging? (1 = index parametru nesoucim info o pagingu (zde dsi.Args[2] viz výše)
			//    //1 sloupecek
			//    return;
			//} else {
			//    //zjistime si radek
			//    int row = ((int)gr.pressedButton - 10) / 2;
			//    int buttNo = ((int)gr.pressedButton - 10) % 2;
			//    RegionRectangle region = rectsList[row];
			//    GumpInstance newGi;
			//    switch(buttNo) {
			//        case 0: //region info
			//            newGi = gi.Cont.Dialog(SingletonScript<D_Info>.Instance, region, 0, 0);
			//            DialogStacking.EnstackDialog(gi, newGi);
			//            break;
			//        case 1: //smazat region
			//            //region.Delete(); - TODO (remove z dictu, rectangly atd.)
			//            args[3] = null;
			//            DialogStacking.ResendAndRestackDialog(gi);
			//            break;
			//    }
			//}
		}		

		//[Remark("Display region's rectangles. Function accessible from the game." +
		//        "The function is designed to be triggered using .RegionsRectangles to display the "+
		//        "rectangles of the callers actual region" +
		//        "but it can be also called from other dialogs - such as region's info...")]				
		//[SteamFunction]
		//public static void RegionsRectangles(Thing self, ScriptArgs text) {
		//    //Parametry dialogu:
		//    //0 - vyhledavaci kriterium
		//    //1 - kolikaty bude prvni
		//    //2 - seznam
		//    //3 - trideni
		//    Globals.SrcCharacter.Dialog(SingletonScript<D_Regions_Rectangles>.Instance, "", 0, null, RegionsSorting.NameAsc);
		//}
	}
}