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
	[Summary("Dialog listing all regions and enabling us to edit them")]
	public class D_Regions : CompiledGumpDef {
		public static readonly TagKey regsListTK = TagKey.Get("_regions_list_");//bude vyuzit jeste jinde, proto public a static
		public static readonly TagKey regsSearchTK = TagKey.Get("_regions_list_search_crit_");
		public static readonly TagKey regsSortingTK = TagKey.Get("_regions_list_sorting_");
		private static int width = 600;

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			List<StaticRegion> regionsList = (List<StaticRegion>) args.GetTag(D_Regions.regsListTK);//regionlist si posilame v argumentu (napriklad pri pagingu)
			if (regionsList == null) {
				//vzit seznam a pripadne ho setridit...
				//toto se provede jen pri prvnim zobrazeni nebo zmene kriteria!
				regionsList = StaticRegion.FindByString(TagMath.SGetTag(args, D_Regions.regsSearchTK));
				args.SetTag(D_Regions.regsListTK, regionsList); //ulozime to do argumentu dialogu
			}

			object sorting = args.GetTag(D_Regions.regsSortingTK);
			if (sorting != null) {//mame cim tridit?
				SortBy(regionsList, (SortingCriteria) Convert.ToInt32(sorting));
			}

			//zjistit zda bude paging, najit maximalni index na strance
			int firstiVal = TagMath.IGetTag(args, ImprovedDialog.pagingIndexTK);   //prvni index na strance
			int imax = Math.Min(firstiVal + ImprovedDialog.PAGE_ROWS, regionsList.Count);

			ImprovedDialog dlg = new ImprovedDialog(this.GumpInstance);
			//pozadi    
			dlg.CreateBackground(width);
			dlg.SetLocation(50, 50);

			//nadpis
			dlg.AddTable(new GUTATable(1, 0, ButtonMetrics.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = GUTAText.Builder.TextHeadline("Seznam statických regionù (zobrazeno " + (firstiVal + 1) + "-" + imax + " z " + regionsList.Count + ")").Build();
			dlg.LastTable[0, 1] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonCross).Id(0).Build();//cudlik na zavreni dialogu
			dlg.MakeLastTableTransparent();

			//cudlik na zalozeni noveho regionu
			dlg.AddTable(new GUTATable(1, 130, ButtonMetrics.D_BUTTON_WIDTH, 0));
			dlg.LastTable[0, 0] = GUTAText.Builder.TextLabel("Vytvoøit region").Build();
			dlg.LastTable[0, 1] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonOK).Id(1).Build();
			dlg.MakeLastTableTransparent();

			//cudlik a input field na zuzeni vyberu
			dlg.AddTable(new GUTATable(1, 130, 0, ButtonMetrics.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = GUTAText.Builder.TextLabel("Vyhledávací kriterium").Build();
			dlg.LastTable[0, 1] = GUTAInput.Builder.Id(33).Build();
			dlg.LastTable[0, 2] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonPaper).Id(2).Build();
			dlg.MakeLastTableTransparent();

			//popis sloupcu (Detail, Delete, Name, Defname, P)
			dlg.AddTable(new GUTATable(1, ButtonMetrics.D_BUTTON_WIDTH, ButtonMetrics.D_BUTTON_WIDTH, 150, 150, 0));
			dlg.LastTable[0, 0] = GUTAText.Builder.TextLabel("Detail").Build();
			dlg.LastTable[0, 1] = GUTAText.Builder.TextLabel("Smaž").Build();
			dlg.LastTable[0, 2] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonSortUp).Id(3).Build(); //tridit podle jmena asc
			dlg.LastTable[0, 2] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonSortDown).YPos(ButtonMetrics.D_SORTBUTTON_LINE_OFFSET).Id(4).Build(); //tridit podle jmena desc            
			dlg.LastTable[0, 2] = GUTAText.Builder.TextLabel("Jméno").XPos(ButtonMetrics.D_SORTBUTTON_COL_OFFSET).Build();
			dlg.LastTable[0, 3] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonSortUp).Id(5).Build(); //tridit dle defnamu asc
			dlg.LastTable[0, 3] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonSortDown).YPos(ButtonMetrics.D_SORTBUTTON_LINE_OFFSET).Id(6).Build(); //tridit dle defnamu desc
			dlg.LastTable[0, 3] = GUTAText.Builder.TextLabel("Defname").XPos(ButtonMetrics.D_SORTBUTTON_COL_OFFSET).Build();
			dlg.LastTable[0, 4] = GUTAText.Builder.TextLabel("Pozice").Build();
			dlg.MakeLastTableTransparent();

			//seznam regionu
			dlg.AddTable(new GUTATable(imax - firstiVal));
			dlg.CopyColsFromLastTable();

			//projet seznam v ramci daneho rozsahu indexu
			int rowCntr = 0;
			for (int i = firstiVal; i < imax; i++) {
				StaticRegion reg = regionsList[i];

				dlg.LastTable[rowCntr, 0] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonPaper).Id(10 + (2 * i)).Build(); //info o regionu
				dlg.LastTable[rowCntr, 1] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonCross).Id(11 + (2 * i)).Build(); //smazat region (huh!)
				dlg.LastTable[rowCntr, 2] = GUTAText.Builder.Text(reg.Name).Build(); //nazev
				dlg.LastTable[rowCntr, 3] = GUTAText.Builder.Text(reg.Defname).Build(); //defname
				dlg.LastTable[rowCntr, 4] = GUTAText.Builder.Text(reg.P.ToNormalString()).Build(); //home pozice				

				rowCntr++;
			}
			dlg.MakeLastTableTransparent(); //zpruhledni zbytek dialogu
			dlg.CreatePaging(regionsList.Count, firstiVal, 1);//ted paging			
			dlg.WriteOut();
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			//seznam regionu bereme z parametru (mohl byt jiz trideny atd, nebudeme ho proto selectit znova)
			List<StaticRegion> regionsList = (List<StaticRegion>) args.GetTag(D_Regions.regsListTK);
			int firstOnPage = TagMath.IGetTag(args, ImprovedDialog.pagingIndexTK);
			if (gr.pressedButton < 10) { //ovladaci tlacitka (exit, new, tridit)				
				switch (gr.pressedButton) {
					case 0: //exit
						DialogStacking.ShowPreviousDialog(gi); //zobrazit pripadny predchozi dialog
						break;
					case 1: //zalozit novy region
						//jako parametry vzit jednoduse 4 zakladni souradnice rectanglu
						Gump newGi = gi.Cont.Dialog(SingletonScript<D_New_Region>.Instance, new DialogArgs(0, 0, 0, 0));
						DialogStacking.EnstackDialog(gi, newGi); //vlozime napred dialog do stacku
						break;
					case 2: //vyhledavat / zuzit vyber
						string nameCriteria = gr.GetTextResponse(33);
						args.SetTag(D_Regions.regsSearchTK, nameCriteria);//uloz info o vyhledavacim kriteriu
						args.RemoveTag(ImprovedDialog.pagingIndexTK);//zrusit info o prvnich indexech - seznam se cely zmeni tim kriteriem												
						args.RemoveTag(D_Regions.regsListTK);//vycistit soucasny odkaz na regionlist aby se mohl prenacist
						DialogStacking.ResendAndRestackDialog(gi);
						break;
					case 3: //name asc						
						args.RemoveTag(D_Regions.regsListTK);//vycistit soucasny odkaz na regionlist aby se mohl prenacist a pretridit
						args.SetTag(D_Regions.regsSortingTK, SortingCriteria.NameAsc);
						DialogStacking.ResendAndRestackDialog(gi);
						break;
					case 4: //name desc
						args.RemoveTag(D_Regions.regsListTK);//vycistit soucasny odkaz na regionlist aby se mohl prenacist a pretridit
						args.SetTag(D_Regions.regsSortingTK, SortingCriteria.NameDesc);
						DialogStacking.ResendAndRestackDialog(gi);
						break;
					case 5: //defname asc
						args.RemoveTag(D_Regions.regsListTK);//vycistit soucasny odkaz na regionlist aby se mohl prenacist a pretridit
						args.SetTag(D_Regions.regsSortingTK, SortingCriteria.DefnameAsc);
						DialogStacking.ResendAndRestackDialog(gi);
						break;
					case 6: //defname desc
						args.RemoveTag(D_Regions.regsListTK);//vycistit soucasny odkaz na regionlist aby se mohl prenacist a pretridit
						args.SetTag(D_Regions.regsSortingTK, SortingCriteria.DefnameDesc);
						DialogStacking.ResendAndRestackDialog(gi);
						break;
				}
			} else if (ImprovedDialog.PagingButtonsHandled(gi, gr, regionsList.Count, 1)) {//kliknuto na paging? (1 = index parametru nesoucim info o pagingu (zde dsi.Args[2] viz výše)
				//1 sloupecek
				return;
			} else {
				//zjistime si radek
				int row = ((int) gr.pressedButton - 10) / 2;
				int buttNo = ((int) gr.pressedButton - 10) % 2;
				StaticRegion region = regionsList[row];
				Gump newGi;
				switch (buttNo) {
					case 0: //region info
						newGi = gi.Cont.Dialog(SingletonScript<D_Info>.Instance, new DialogArgs(region));
						DialogStacking.EnstackDialog(gi, newGi);
						break;
					case 1: //smazat region
						region.Delete(); //(remove z dictu, rectangly atd.)
						D_Display_Text.ShowInfo("Region byl smazán úspìšnì");
						//DialogStacking.ShowPreviousDialog(gi); //zobrazit pripadny predchozi dialog
						break;
				}
			}
		}

		private void SortBy(List<StaticRegion> regList, SortingCriteria criterium) {
			switch (criterium) {
				case SortingCriteria.NameAsc:
					regList.Sort(RegionComparerByName.instance);
					break;
				case SortingCriteria.NameDesc:
					regList.Sort(RegionComparerByName.instance);
					regList.Reverse();
					break;
				case SortingCriteria.DefnameAsc:
					regList.Sort(RegionComparerByDefname.instance);
					break;
				case SortingCriteria.DefnameDesc:
					regList.Sort(RegionComparerByDefname.instance);
					regList.Reverse();
					break;
				default:
					break; //netridit
			}
		}

		[Summary("Display regions. Function accessible from the game." +
				"The function is designed to be triggered using .RegionsList" +
				"but it can be also called from other dialogs - such as info..." +
				"Default sorting is by Name, asc.")]
		[SteamFunction]
		public static void RegionsList(Thing self, ScriptArgs text) {
			//Parametry dialogu:
			DialogArgs newArgs = new DialogArgs();
			newArgs.SetTag(D_Regions.regsSortingTK, SortingCriteria.NameAsc);//zakladni trideni
			self.Dialog(SingletonScript<D_Regions>.Instance, newArgs);
		}
	}

	[Summary("Comparer for sorting regions by name asc")]
	public class RegionComparerByName : IComparer<StaticRegion> {
		public readonly static RegionComparerByName instance = new RegionComparerByName();

		public int Compare(StaticRegion x, StaticRegion y) {
			return string.Compare(x.Name, y.Name);
		}
	}

	[Summary("Comparer for sorting regions by defname name asc")]
	public class RegionComparerByDefname : IComparer<StaticRegion> {
		public readonly static RegionComparerByDefname instance = new RegionComparerByDefname();

		public int Compare(StaticRegion x, StaticRegion y) {
			return string.Compare(x.Defname, y.Defname);
		}
	}
}