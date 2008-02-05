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
	[Remark("Dialog listing all regions for selecting single one for parent dialog")]
	public class D_SelectParent : CompiledGump {
		private static int width = 450;

		[Remark("Seznam parametru: 0 - vyhledavaci retezec" +
				"	1 - paging" +
				"	2 - seznam regionu" +
				"	3 - trideni")]
		public override void Construct(Thing focus, AbstractCharacter sendTo, object[] args) {
			List<StaticRegion> regionsList = null;
			if(args[2] == null) {
				//vzit seznam a pripadne ho setridit...
				//toto se provede jen pri prvnim zobrazeni nebo zmene kriteria!
				regionsList = StaticRegion.FindByString((string)args[0]);
				args[2] = regionsList; //ulozime to do argumentu dialogu
			} else {
				//regionlist si posilame v argumentu (napriklad pri pagingu)
				regionsList = (List<StaticRegion>)args[2];
			}
			if(args[3] != null) {
				SortBy(regionsList, (RegionsSorting)args[3]);
			}

			//zjistit zda bude paging, najit maximalni index na strance
			int firstiVal = Convert.ToInt32(args[1]);   //prvni index na strance
			int imax = Math.Min(firstiVal + ImprovedDialog.PAGE_ROWS, regionsList.Count);

			ImprovedDialog dlg = new ImprovedDialog(this.GumpInstance);
			//pozadi    
			dlg.CreateBackground(width);
			dlg.SetLocation(200, 100);

			//nadpis
			dlg.Add(new GUTATable(1, 0, ButtonFactory.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = TextFactory.CreateHeadline("Výbìr regionu (zobrazeno " + (firstiVal + 1) + "-" + imax + " z " + regionsList.Count + ")");
			dlg.LastTable[0, 1] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonCross, 0);//cudlik na zavreni dialogu
			dlg.MakeTableTransparent();
						
			//cudlik a input field na zuzeni vyberu
			dlg.Add(new GUTATable(1, 130, 0, ButtonFactory.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = TextFactory.CreateLabel("Vyhledávací kriterium");
			dlg.LastTable[0, 1] = InputFactory.CreateInput(LeafComponentTypes.InputText, 33);
			dlg.LastTable[0, 2] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonPaper, 2);
			dlg.MakeTableTransparent();

			//popis sloupcu (Detail, Delete, Name, Defname, P)
			dlg.Add(new GUTATable(1, ButtonFactory.D_BUTTON_WIDTH, 150, 0));
			dlg.LastTable[0, 0] = TextFactory.CreateLabel("Vyber");
			dlg.LastTable[0, 1] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonSortUp, 3); //tridit podle jmena asc
			dlg.LastTable[0, 1] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonSortDown, 0, ButtonFactory.D_SORTBUTTON_LINE_OFFSET, 4); //tridit podle jmena desc            
			dlg.LastTable[0, 1] = TextFactory.CreateLabel(ButtonFactory.D_SORTBUTTON_COL_OFFSET, 0, "Jméno");
			dlg.LastTable[0, 2] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonSortUp, 5); //tridit dle defnamu asc
			dlg.LastTable[0, 2] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonSortDown, 0, ButtonFactory.D_SORTBUTTON_LINE_OFFSET, 6); //tridit dle defnamu desc
			dlg.LastTable[0, 2] = TextFactory.CreateLabel(ButtonFactory.D_SORTBUTTON_COL_OFFSET, 0, "Defname");
			dlg.MakeTableTransparent();

			//seznam regionu
			dlg.Add(new GUTATable(imax - firstiVal));
			dlg.CopyColsFromLastTable();

			//projet seznam v ramci daneho rozsahu indexu
			int rowCntr = 0;
			for(int i = firstiVal; i < imax; i++) {
				StaticRegion reg = regionsList[i];

				dlg.LastTable[rowCntr, 0] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonTick, 10 + i); //vyber tento
				dlg.LastTable[rowCntr, 1] = TextFactory.CreateText(reg.Name); //nazev
				dlg.LastTable[rowCntr, 2] = TextFactory.CreateText(reg.Defname); //defname

				rowCntr++;
			}
			dlg.MakeTableTransparent(); //zpruhledni zbytek dialogu
			dlg.CreatePaging(regionsList.Count, firstiVal, 1);//ted paging			
			dlg.WriteOut();
		}

		public override void OnResponse(GumpInstance gi, GumpResponse gr, object[] args) {
			//seznam regionu bereme z parametru (mohl byt jiz trideny atd, nebudeme ho proto selectit znova)
			List<StaticRegion> regionsList = (List<StaticRegion>)args[2];
			int firstOnPage = Convert.ToInt32(args[1]);
			if(gr.pressedButton < 10) { //ovladaci tlacitka (exit, new, tridit)				
				switch(gr.pressedButton) {
					case 0: //exit
						DialogStacking.ShowPreviousDialog(gi); //zobrazit pripadny predchozi dialog
						break;
					case 2: //vyhledavat / zuzit vyber
						string nameCriteria = gr.GetTextResponse(33);
						args[0] = nameCriteria; //uloz info o vyhledavacim kriteriu
						args[1] = 0; //zrusit info o prvnich indexech - seznam se cely zmeni tim kriteriem												
						args[2] = null; //vycistit soucasny odkaz na regionlist aby se mohl prenacist
						DialogStacking.ResendAndRestackDialog(gi);
						break;
					case 3: //name asc						
						args[2] = null;
						args[3] = RegionsSorting.NameAsc;
						DialogStacking.ResendAndRestackDialog(gi);
						break;
					case 4: //name desc
						args[2] = null;
						args[3] = RegionsSorting.NameDesc;
						DialogStacking.ResendAndRestackDialog(gi);
						break;
					case 5: //defname asc
						args[2] = null;
						args[3] = RegionsSorting.DefnameAsc;
						DialogStacking.ResendAndRestackDialog(gi);
						break;
					case 6: //defname desc
						args[2] = null;
						args[3] = RegionsSorting.DefnameDesc;
						DialogStacking.ResendAndRestackDialog(gi);
						break;
				}
			} else if(ImprovedDialog.PagingButtonsHandled(gi, gr, 1, regionsList.Count, 1)) {//kliknuto na paging? (1 = index parametru nesoucim info o pagingu (zde dsi.Args[2] viz výše)
				//1 sloupecek
				return;
			} else {
				//zjistime si radek
				int row = (int)gr.pressedButton - 10;
				StaticRegion region = regionsList[row];
				//vezmem z vybraneho regionu jeho defname a predame do predchoziho dialogu
				GumpInstance previousGi = DialogStacking.PopStackedDialog(gi);
				previousGi.InputParams[7] = region.Defname; //to je zalozeni noveho regionu (7 argument - defname parenta)
				DialogStacking.ResendAndRestackDialog(previousGi);				
			}
		}

		private void SortBy(List<StaticRegion> regList, RegionsSorting criterium) {
			switch(criterium) {
				case RegionsSorting.NameAsc:
					regList.Sort(RegionComparerByName.instance);
					break;
				case RegionsSorting.NameDesc:
					regList.Sort(RegionComparerByName.instance);
					regList.Reverse();
					break;
				case RegionsSorting.DefnameAsc:
					regList.Sort(RegionComparerByDefname.instance);
					break;
				case RegionsSorting.DefnameDesc:
					regList.Sort(RegionComparerByDefname.instance);
					regList.Reverse();
					break;
				default:
					break; //netridit
			}
		}
	}
}