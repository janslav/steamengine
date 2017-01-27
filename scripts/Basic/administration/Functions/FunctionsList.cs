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

using System;
using System.Collections.Generic;
using SteamEngine.Scripting;
using SteamEngine.Scripting.Compilation;
using SteamEngine.Scripting.Objects;

namespace SteamEngine.CompiledScripts.Dialogs {

	/// <summary>Dialog that will display all steam functions available</summary>
	public class D_Functions : CompiledGumpDef {
		internal static readonly TagKey listTK = TagKey.Acquire("_functions_list_");
		internal static readonly TagKey criteriaTK = TagKey.Acquire("_functions_search_criteria_");
		internal static readonly TagKey sortTK = TagKey.Acquire("_functions_list_sorting_");

		private const int width = 300;

		public override void Construct(CompiledGump gi, Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			//vzit seznam funkci
			var fList = args.GetTag(listTK) as List<ScriptHolder>;

			if (fList == null) {
				//vzit seznam fci dle vyhledavaciho kriteria
				//toto se provede jen pri prvnim zobrazeni nebo zmene kriteria!
				fList = this.ListifyFunctions(ScriptHolder.AllFunctions, TagMath.SGetTag(args, criteriaTK));
				this.SortFunctions(fList, (SortingCriteria) TagMath.IGetTag(args, sortTK));
				args.SetTag(listTK, fList); //ulozime to do argumentu dialogu				
			}
			var firstiVal = TagMath.IGetTag(args, ImprovedDialog.pagingIndexTK);//prvni index na strance
			var imax = Math.Min(firstiVal + ImprovedDialog.PAGE_ROWS, fList.Count);

			var dlg = new ImprovedDialog(gi);
			//pozadi    
			dlg.CreateBackground(width);
			dlg.SetLocation(70, 70);

			//nadpis
			dlg.AddTable(new GUTATable(1, 0, ButtonMetrics.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = GUTAText.Builder.TextHeadline("Seznam v�ech funkc� (zobrazeno " + (firstiVal + 1) + "-" + imax + " z " + fList.Count + ")").Build();
			dlg.LastTable[0, 1] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonCross).Id(0).Build();//cudlik na zavreni dialogu
			dlg.MakeLastTableTransparent();

			//cudlik a input field na zuzeni vyberu
			dlg.AddTable(new GUTATable(1, 130, 0, ButtonMetrics.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = GUTAText.Builder.TextLabel("Vyhled�vac� kriterium").Build();
			dlg.LastTable[0, 1] = GUTAInput.Builder.Id(33).Build();
			dlg.LastTable[0, 2] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonPaper).Id(1).Build();
			dlg.MakeLastTableTransparent();

			//popis sloupcu
			dlg.AddTable(new GUTATable(1, 160, ButtonMetrics.D_BUTTON_WIDTH, 0));
			dlg.LastTable[0, 0] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonSortUp).Id(2).Build(); //tridit podle name asc
			dlg.LastTable[0, 0] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonSortDown).YPos(ButtonMetrics.D_SORTBUTTON_LINE_OFFSET).Id(3).Build(); //tridit podle name desc				
			dlg.LastTable[0, 0] = GUTAText.Builder.TextLabel("N�zev").XPos(ButtonMetrics.D_SORTBUTTON_COL_OFFSET).Build();
			dlg.LastTable[0, 1] = GUTAText.Builder.TextLabel("Desc").Build();
			dlg.LastTable[0, 2] = GUTAText.Builder.TextLabel("Type").Build();//scripted/compiled
			dlg.MakeLastTableTransparent();

			//seznam funkci
			dlg.AddTable(new GUTATable(imax - firstiVal));
			dlg.CopyColsFromLastTable();

			//projet seznam v ramci daneho rozsahu indexu
			var rowCntr = 0;
			for (var i = firstiVal; i < imax; i++) {
				var sh = fList[i];

				dlg.LastTable[rowCntr, 0] = GUTAText.Builder.Text(sh.Name).Build();
				//cudl na zobrazeni popisu }aktivni pouze je-li popis
				dlg.LastTable[rowCntr, 1] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonPaper).Active(sh.Description != null).Id(10 + i).Build();
				dlg.LastTable[rowCntr, 2] = GUTAText.Builder.Text((sh is CompiledScriptHolder) ? "compiled" : "scripted").Build();
				rowCntr++;
			}
			dlg.MakeLastTableTransparent(); //zpruhledni zbytek dialogu

			//ted paging
			dlg.CreatePaging(fList.Count, firstiVal, 1);

			dlg.WriteOut();
		}

		public override void OnResponse(CompiledGump gi, Thing focus, GumpResponse gr, DialogArgs args) {
			//seznam fci bereme z parametru (mohl byt jiz trideny atd, nebudeme ho proto selectit znova)
			var fList = (List<ScriptHolder>) args.GetTag(listTK);
			var firstOnPage = TagMath.IGetTag(args, ImprovedDialog.pagingIndexTK);
			var imax = Math.Min(firstOnPage + ImprovedDialog.PAGE_ROWS, fList.Count);
			if (gr.PressedButton < 10) { //ovladaci tlacitka (sorting, paging atd)
				switch (gr.PressedButton) {
					case 0: //exit
						DialogStacking.ShowPreviousDialog(gi); //zobrazit pripadny predchozi dialog						
						break;
					case 1: //vyhledat dle zadani
						var nameCriteria = gr.GetTextResponse(33);
						args.RemoveTag(ImprovedDialog.pagingIndexTK);//zrusit info o prvnich indexech - seznam se cely zmeni tim kriteriem						
						args.SetTag(criteriaTK, nameCriteria);
						args.RemoveTag(listTK);//vycistit soucasny odkaz na list aby se mohl prenacist
						DialogStacking.ResendAndRestackDialog(gi);
						break;
					case 2: //name asc
						args.SetTag(sortTK, SortingCriteria.NameAsc);
						args.RemoveTag(listTK);//vycistit soucasny odkaz na list aby se mohl prenacist
						DialogStacking.ResendAndRestackDialog(gi);
						break;
					case 3: //name desc
						args.SetTag(sortTK, SortingCriteria.NameDesc);
						args.RemoveTag(listTK);//vycistit soucasny odkaz na list aby se mohl prenacist
						DialogStacking.ResendAndRestackDialog(gi);
						break;
				}
			} else if (ImprovedDialog.PagingButtonsHandled(gi, gr, fList.Count, 1)) {//posledni 1 - pocet sloupecku v dialogu				
			} else {
				var row = (gr.PressedButton - 10);//zjistime si radek
				var sh = fList[row];
				//a zobrazime info dialog
				var newGi = D_Display_Text.ShowInfo(sh.Description + ""); //nezobrazovat "null", jen prazdno evt...
				DialogStacking.EnstackDialog(gi, newGi);
			}
		}

		/// <summary>Retreives the list of all existing functions</summary>
		private List<ScriptHolder> ListifyFunctions(IEnumerable<ScriptHolder> fctions, string criteria) {
			var fList = new List<ScriptHolder>();
			foreach (var entry in fctions) {
				if (criteria == null || criteria.Equals("")) {
					fList.Add(entry);//bereme vse
				} else if (entry.Name.ToUpper().Contains(criteria.ToUpper())) {
					fList.Add(entry);//jinak jen v pripade ze kriterium se vyskytuje v nazvu ability
				}
			}
			return fList;
		}

		/// <summary>Sorting of the functions list</summary>
		private void SortFunctions(List<ScriptHolder> list, SortingCriteria criteria) {
			switch (criteria) {
				case SortingCriteria.NameAsc:
					list.Sort(FunctionsNameComparer.instance);
					break;
				case SortingCriteria.NameDesc:
					list.Sort(FunctionsNameComparer.instance);
					list.Reverse();
					break;
			}
		}

		/// <summary>
		/// Display a list of all functions (ScriptHolders). Function accessible from the game.
		/// The function is designed to be triggered using .AllFunctions(criteria)
		/// </summary>
		[SteamFunction]
		public static void AllFunctions(Character self, ScriptArgs text) {
			var newArgs = new DialogArgs();
			newArgs.SetTag(sortTK, SortingCriteria.NameAsc);//default sorting
			if (text == null || text.Argv == null || text.Argv.Length == 0) {
				self.Dialog(SingletonScript<D_Functions>.Instance, newArgs);
			} else {
				newArgs.SetTag(criteriaTK, text.Args);//vyhl. kriterium
				self.Dialog(SingletonScript<D_Functions>.Instance, newArgs);
			}
		}
	}

	/// <summary>Comparer for sorting functions by name asc</summary>
	public class FunctionsNameComparer : IComparer<ScriptHolder> {
		public static readonly FunctionsNameComparer instance = new FunctionsNameComparer();

		private FunctionsNameComparer() {
			//soukromy konstruktor, pristupovat budeme pres instanci
		}

		public int Compare(ScriptHolder x, ScriptHolder y) {
			return StringComparer.OrdinalIgnoreCase.Compare(x.Name, y.Name);
		}
	}
}