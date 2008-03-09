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
		internal static readonly TagKey regionTK = TagKey.Get("__region_with_rects_");
		internal static readonly TagKey rectsListTK = TagKey.Get("__rects_list_");


		private static int width = 450;

		[Remark("Seznam parametru: 0 - region" +
				"	1 - paging"+
				"	2 - rectangly v listu(je totiž možno pøidávat dynamicky)")]
		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			Region reg = (Region)args.GetTag(D_Region_Rectangles.regionTK);
			List<MutableRectangle> rectList = null;
			if(args.HasTag(D_Region_Rectangles.rectsListTK)) {
				rectList = (List<MutableRectangle>)args.GetTag(D_Region_Rectangles.rectsListTK);
			} else {
				//vezmeme je z regionu
				rectList = MutableRectangle.TakeRectsFromRegion(reg);
				args.SetTag(D_Region_Rectangles.rectsListTK,rectList); //ulozime to do argumentu dialogu
			}

			//zjistit zda bude paging, najit maximalni index na strance
			int firstiVal = TagMath.IGetTag(args, ImprovedDialog.pagingIndexTK);   //prvni index na strance
			int imax = Math.Min(firstiVal + ImprovedDialog.PAGE_ROWS, rectList.Count);

			ImprovedDialog dlg = new ImprovedDialog(this.GumpInstance);
			//pozadi    
			dlg.CreateBackground(width);
			dlg.SetLocation(25, 25);

			//nadpis
			dlg.AddTable(new GUTATable(1, 0, ButtonFactory.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = TextFactory.CreateHeadline("Pøehled rectanglù pro region " + reg.Name + "(" + reg.Defname + ") (zobrazeno " + (firstiVal + 1) + "-" + imax + " z " + rectList.Count + ")");
			dlg.LastTable[0, 1] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonCross, 0);//cudlik na zavreni dialogu
			dlg.MakeLastTableTransparent();

			//cudlik na zalozeni noveho rectanglu
			dlg.AddTable(new GUTATable(1, 130, ButtonFactory.D_BUTTON_WIDTH, 0));
			dlg.LastTable[0, 0] = TextFactory.CreateLabel("Pøidat rectangle");
			dlg.LastTable[0, 1] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonOK, 1);
			dlg.MakeLastTableTransparent();

			//popis sloupcu (Info, Height, Width, StartPoint, EndPoint, Tiles)
			dlg.AddTable(new GUTATable(1, ButtonFactory.D_BUTTON_WIDTH, ButtonFactory.D_BUTTON_WIDTH, 35, 35, 130, 0, 35));
			dlg.LastTable[0, 0] = TextFactory.CreateLabel("Edit");
			dlg.LastTable[0, 1] = TextFactory.CreateLabel("Smaž");			
			dlg.LastTable[0, 2] = TextFactory.CreateLabel("Šíøka");
			dlg.LastTable[0, 3] = TextFactory.CreateLabel("Výška");
			dlg.LastTable[0, 4] = TextFactory.CreateLabel("Poèáteèní bod");
			dlg.LastTable[0, 5] = TextFactory.CreateLabel("Koncový bod");
			dlg.LastTable[0, 6] = TextFactory.CreateLabel("Polí");
			dlg.MakeLastTableTransparent();

			//seznam rectnaglu
			dlg.AddTable(new GUTATable(imax - firstiVal));
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
			dlg.MakeLastTableTransparent(); //zpruhledni zbytek dialogu

			//send button
			dlg.AddTable(new GUTATable(1, ButtonFactory.D_BUTTON_WIDTH, 0));
			dlg.LastTable[0, 0] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonOK, 2);
			dlg.LastTable[0, 1] = TextFactory.CreateText("Uložit");
			dlg.MakeLastTableTransparent();

			dlg.CreatePaging(rectList.Count, firstiVal, 1);//ted paging		
			dlg.WriteOut();
		}

		public override void OnResponse(GumpInstance gi, GumpResponse gr, DialogArgs args) {
			//seznam rectanglu bereme z parametru (mohl byt nejaky pridan/smazan)
			StaticRegion reg = (StaticRegion)args.GetTag(D_Region_Rectangles.regionTK);
			List<MutableRectangle> rectsList = (List<MutableRectangle>)args.GetTag(D_Region_Rectangles.rectsListTK);
			int firstOnPage = TagMath.IGetTag(args, ImprovedDialog.pagingIndexTK);
			if(gr.pressedButton < 10) { //ovladaci tlacitka (exit, new, tridit)				
				switch(gr.pressedButton) {
					case 0: //exit
						DialogStacking.ShowPreviousDialog(gi); //zobrazit pripadny predchozi dialog
						break;
					case 1: //zalozit novy rectangle
						//nemazat rectangly - budeme do nich ukladat novy						
						DialogArgs newArgs = new DialogArgs(0, 0, 0, 0);//zakladni souradnice rectanglu
						newArgs.SetTag(D_Region_Rectangles.rectsListTK, rectsList); //seznam budeme potrebovat
						GumpInstance newGi = gi.Cont.Dialog(SingletonScript<D_New_Rectangle>.Instance, newArgs);
						DialogStacking.EnstackDialog(gi, newGi); //vlozime napred dialog do stacku
						break;
					case 2: //ulozit
						//nastavenim rectanglu dojde k reinicializaci vsech regionu
						if(reg.SetRectangles(rectsList)) {
							if(!reg.ContainsPoint(reg.P)) {//jeste zkoukneme pozici - mohla zmizet smazáním/resizem rectanglu
								D_Display_Text.ShowError("Home pozice je mimo region - je potøeba ji pøenastavit");
							} else {
								D_Display_Text.ShowInfo("Ukládání rectanglù bylo úspìšné");
							}
							//zobrazime info a zmizime (z infa bude navrat k predchozimu dlg neb tento nestackneme)
							break;
						} else { //nekde to neproslo
							GumpInstance infoGi = D_Display_Text.ShowError("Ukládání rectanglù skonèilo s chybami - viz konzole!");
							DialogStacking.EnstackDialog(gi, infoGi); //vlozime dialog do stacku pro navrat						
							break;
						}
						//pokud to nekde spadne, tak to uvidime v konzoli - to uz je zavazny problem a nesmi to jen tak projit! (=nechytam vyjimku)
						//DialogStacking.ShowPreviousDialog(gi); //zobrazit pripadny predchozi dialog
						//break;
				}
			} else if(ImprovedDialog.PagingButtonsHandled(gi, gr, rectsList.Count, 1)) {//kliknuto na paging?
				return;
			} else {
				//zjistime si radek a cudlik v nem
				int row = ((int)gr.pressedButton - 10) / 2;
				int buttNo = ((int)gr.pressedButton - 10) % 2;
				MutableRectangle rect = rectsList[row];
				GumpInstance newGi;
				switch(buttNo) {
					case 0: //region rectangle info
						DialogArgs newArgs = new DialogArgs(0, 0); //button, items paging
						newArgs.SetTag(D_Info.infoizedTargTK, rect);
						newGi = gi.Cont.Dialog(SingletonScript<D_Info>.Instance, newArgs);
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