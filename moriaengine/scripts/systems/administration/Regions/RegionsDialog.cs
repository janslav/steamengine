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
	[Remark("Dialog listing all regions and enabling us to edit them")]
	public class D_Regions : CompiledGump {
		private static int width = 600;

		[Remark("Seznam parametru: 0 - vyhledavaci retezec"+
				"	1 - paging"+
				"	2 - seznam regionu"+
				"	3 - trideni")]
		public override void Construct(Thing focus, AbstractCharacter sendTo, object[] args) {
			List<StaticRegion> regionsList = null;
			if(args[2] == null) {
				//vzit seznam notu a pripadne ho setridit...
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
			dlg.SetLocation(50, 50);

			//nadpis
			dlg.Add(new GUTATable(1, 0, ButtonFactory.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = TextFactory.CreateHeadline("Seznam statických regionù (zobrazeno " + (firstiVal + 1) + "-" + imax + " z " + regionsList.Count + ")");
			dlg.LastTable[0, 1] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonCross, 0);//cudlik na zavreni dialogu
			dlg.MakeTableTransparent();

			//cudlik na zalozeni noveho regionu
			dlg.Add(new GUTATable(1, 130, ButtonFactory.D_BUTTON_WIDTH, 0));
			dlg.LastTable[0, 0] = TextFactory.CreateLabel("Vytvoøit region");
			dlg.LastTable[0, 1] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonOK, 1);
			dlg.MakeTableTransparent();

			//cudlik a input field na zuzeni vyberu
			dlg.Add(new GUTATable(1, 130, 0, ButtonFactory.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = TextFactory.CreateLabel("Vyhledávací kriterium");
			dlg.LastTable[0, 1] = InputFactory.CreateInput(LeafComponentTypes.InputText, 33);
			dlg.LastTable[0, 2] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonPaper, 2);
			dlg.MakeTableTransparent();

			//popis sloupcu (Detail, Name, Defname, P, Delete)
			dlg.Add(new GUTATable(1, ButtonFactory.D_BUTTON_WIDTH, ButtonFactory.D_BUTTON_WIDTH, 150, 150, 0));
			dlg.LastTable[0, 0] = TextFactory.CreateLabel("Detail");
			dlg.LastTable[0, 1] = TextFactory.CreateLabel("Smaž");
			dlg.LastTable[0, 2] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonSortUp, 3); //tridit podle jmena asc
			dlg.LastTable[0, 2] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonSortDown, 0, ButtonFactory.D_SORTBUTTON_LINE_OFFSET, 4); //tridit podle jmena desc            
			dlg.LastTable[0, 2] = TextFactory.CreateLabel(ButtonFactory.D_SORTBUTTON_COL_OFFSET, 0, "Jméno");
			dlg.LastTable[0, 3] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonSortUp, 5); //tridit dle defnamu asc
			dlg.LastTable[0, 3] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonSortDown, 0, ButtonFactory.D_SORTBUTTON_LINE_OFFSET, 6); //tridit dle defnamu desc
			dlg.LastTable[0, 3] = TextFactory.CreateLabel(ButtonFactory.D_SORTBUTTON_COL_OFFSET, 0, "Defname");
			dlg.LastTable[0, 4] = TextFactory.CreateLabel("Pozice");			
			dlg.MakeTableTransparent();

			//seznam regionu
			dlg.Add(new GUTATable(imax - firstiVal));
			dlg.CopyColsFromLastTable();

			//projet seznam v ramci daneho rozsahu indexu
			int rowCntr = 0;
			for(int i = firstiVal; i < imax; i++) {
				StaticRegion reg = regionsList[i];

				dlg.LastTable[rowCntr, 0] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonPaper, 10 + (2 * i)); //info o regionu
				dlg.LastTable[rowCntr, 1] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonCross, 11 + (2 * i)); //smazat region (huh!)
				dlg.LastTable[rowCntr, 2] = TextFactory.CreateText(reg.Name); //nazev
				dlg.LastTable[rowCntr, 3] = TextFactory.CreateText(reg.Defname); //defname
				dlg.LastTable[rowCntr, 4] = TextFactory.CreateText(reg.P.ToNormalString()); //home pozice				
				
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
					case 1: //zalozit novy region
						//GumpInstance newGi = gi.Cont.Dialog(SingletonScript<D_New_Region>.Instance);
						//DialogStacking.EnstackDialog(gi, newGi); //vlozime napred dialog do stacku
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
				int row = ((int)gr.pressedButton - 10) / 2;
				int buttNo = ((int)gr.pressedButton - 10) % 2;
				StaticRegion region = regionsList[row];
				GumpInstance newGi;
				switch(buttNo) {
					case 0: //region info
						//newGi = gi.Cont.Dialog(SingletonScript<D_Region_Info>.Instance, region);
						//DialogStacking.EnstackDialog(gi, newGi);
						break;
					case 1: //smazat region
						//region.Delete(); - TODO (remove z dictu, rectangly atd.)
						args[3] = null;
						DialogStacking.ResendAndRestackDialog(gi);
						break; 
				}
			}
		}

		private static void SortBy(List<StaticRegion> regList, RegionsSorting criterium) {
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

		[Remark("Comparer for sorting regions by name asc")]
		public class RegionComparerByName : IComparer<StaticRegion> {
			public readonly static RegionComparerByName instance = new RegionComparerByName();

			public int Compare(StaticRegion x, StaticRegion y) {
				return string.Compare(x.Name, y.Name);
			}
		}

		[Remark("Comparer for sorting regions by defname name asc")]
		public class RegionComparerByDefname : IComparer<StaticRegion> {
			public readonly static RegionComparerByDefname instance = new RegionComparerByDefname();

			public int Compare(StaticRegion x, StaticRegion y) {
				return string.Compare(x.Defname, y.Defname);
			}
		}

		[Remark("Display an account notes. Function accessible from the game." +
				"The function is designed to be triggered using .RegionsList" +
				"but it can be also called from other dialogs - such as info..." +
				"Default sorting is by Name, asc.")]
		[SteamFunction]
		public static void RegionsList(Thing self, ScriptArgs text) {
			//Parametry dialogu:
			//0 - vyhledavaci kriterium
			//1 - kolikaty bude prvni
			//2 - seznam
			//3 - trideni
			Globals.SrcCharacter.Dialog(SingletonScript<D_Regions>.Instance, "", 0, null, RegionsSorting.NameAsc);
		}
	}
}