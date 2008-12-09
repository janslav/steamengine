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
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts.Dialogs {

	[Summary("Dialog listing all character's abilities")]
	public class D_CharsAbilitiesList : CompiledGumpDef {
		internal static readonly TagKey listTK = TagKey.Get("_abilities_set_");
		internal static readonly TagKey criteriumTK = TagKey.Get("_abilities_criterium_");
		internal static readonly TagKey sortingTK = TagKey.Get("_abilities_sorting_");

		private static int width = 800;

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			//vzit seznam roli
			List<Ability> abList = args.GetTag(D_CharsAbilitiesList.listTK) as List<Ability>;

			if (abList == null) {
				//vzit seznam abilit z focusa dle vyhledavaciho kriteria
				Character whose = (Character) focus;
				//toto se provede jen pri prvnim zobrazeni nebo zmene kriteria!
				abList = ListifyAbilities(whose.Abilities, TagMath.SGetTag(args, D_CharsAbilitiesList.criteriumTK));
				SortAbilities(abList, (SortingCriteria) TagMath.IGetTag(args, D_CharsAbilitiesList.sortingTK));
				args.SetTag(D_CharsAbilitiesList.listTK, abList); //ulozime to do argumentu dialogu				
			}
			int firstiVal = TagMath.IGetTag(args, ImprovedDialog.pagingIndexTK);//prvni index na strance
			int imax = Math.Min(firstiVal + ImprovedDialog.PAGE_ROWS, abList.Count);

			ImprovedDialog dlg = new ImprovedDialog(this.GumpInstance);
			//pozadi    
			dlg.CreateBackground(width);
			dlg.SetLocation(70, 70);

			//nadpis
			dlg.AddTable(new GUTATable(1, 0, ButtonFactory.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = TextFactory.CreateHeadline("Seznam abilit co má " + focus.Name + " (zobrazeno " + (firstiVal + 1) + "-" + imax + " z " + abList.Count + ")");
			dlg.LastTable[0, 1] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonCross, 0);//cudlik na zavreni dialogu
			dlg.MakeLastTableTransparent();

			//cudlik a input field na zuzeni vyberu
			dlg.AddTable(new GUTATable(1, 130, 0, ButtonFactory.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = TextFactory.CreateLabel("Vyhledávací kriterium");
			dlg.LastTable[0, 1] = InputFactory.CreateInput(LeafComponentTypes.InputText, 33);
			dlg.LastTable[0, 2] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonPaper, 1);
			dlg.MakeLastTableTransparent();

			//popis sloupcu
			dlg.AddTable(new GUTATable(1, ButtonFactory.D_BUTTON_WIDTH, 250, 50, 150, 40, 0, ButtonFactory.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = TextFactory.CreateLabel("Info");
			dlg.LastTable[0, 1] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonSortUp, 2); //tridit podle name asc
			dlg.LastTable[0, 1] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonSortDown, 0, ButtonFactory.D_SORTBUTTON_LINE_OFFSET, 3); //tridit podle name desc				
			dlg.LastTable[0, 1] = TextFactory.CreateLabel(ButtonFactory.D_SORTBUTTON_COL_OFFSET, 0, "Název");
			dlg.LastTable[0, 2] = TextFactory.CreateLabel("Pts/Max pts");
			dlg.LastTable[0, 3] = TextFactory.CreateLabel("Last usage");
			dlg.LastTable[0, 4] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonSortUp, 6); //tridit podle running asc
			dlg.LastTable[0, 4] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonSortDown, 0, ButtonFactory.D_SORTBUTTON_LINE_OFFSET, 7); //tridit podle running desc							
			dlg.LastTable[0, 4] = TextFactory.CreateLabel(ButtonFactory.D_SORTBUTTON_COL_OFFSET, 0, "Run");
			dlg.LastTable[0, 5] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonSortUp, 4); //tridit podle roledefname asc
			dlg.LastTable[0, 5] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonSortDown, 0, ButtonFactory.D_SORTBUTTON_LINE_OFFSET, 5); //tridit podle abilitydefname desc				
			dlg.LastTable[0, 5] = TextFactory.CreateLabel(ButtonFactory.D_SORTBUTTON_COL_OFFSET, 0, "Abilitydef");
			dlg.LastTable[0, 6] = TextFactory.CreateLabel("Abilitydef info");
			dlg.MakeLastTableTransparent();

			//seznam roli
			dlg.AddTable(new GUTATable(imax - firstiVal));
			dlg.CopyColsFromLastTable();

			//projet seznam v ramci daneho rozsahu indexu
			int rowCntr = 0;
			for (int i = firstiVal; i < imax; i++) {
				Ability ab = abList[i];
				Hues hue = ab.Running ? Hues.WriteColor2 : Hues.WriteColor;

				//infodialog
				dlg.LastTable[rowCntr, 0] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonPaper, 10 + 2 * i);
				dlg.LastTable[rowCntr, 1] = TextFactory.CreateText(hue, ab.Name);
				dlg.LastTable[rowCntr, 2] = TextFactory.CreateText(hue, ab.Points + "/" + ab.MaxPoints);
				dlg.LastTable[rowCntr, 3] = TextFactory.CreateText(hue, Globals.TimeInSeconds - ab.LastUsage + "secs ago");
				dlg.LastTable[rowCntr, 4] = TextFactory.CreateText(hue, ab.Running ? "Y" : "N");
				dlg.LastTable[rowCntr, 5] = TextFactory.CreateText(hue, ab.AbilityDef.Defname);
				//abilitydef info dialog
				dlg.LastTable[rowCntr, 6] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonPaper, 11 + 2 * i);
				rowCntr++;
			}
			dlg.MakeLastTableTransparent(); //zpruhledni zbytek dialogu

			//ted paging
			dlg.CreatePaging(abList.Count, firstiVal, 1);

			dlg.WriteOut();
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			//seznam abilit bereme z parametru (mohl byt jiz trideny atd, nebudeme ho proto selectit znova)
			List<Ability> abList = (List<Ability>) args.GetTag(D_CharsAbilitiesList.listTK);
			int firstOnPage = TagMath.IGetTag(args, ImprovedDialog.pagingIndexTK);
			int imax = Math.Min(firstOnPage + ImprovedDialog.PAGE_ROWS, abList.Count);
			if (gr.pressedButton < 10) { //ovladaci tlacitka (exit, new, vyhledej)				
				switch (gr.pressedButton) {
					case 0: //exit
						DialogStacking.ShowPreviousDialog(gi); //zobrazit pripadny predchozi dialog
						break;
					case 1: //vyhledat dle zadani
						string nameCriteria = gr.GetTextResponse(33);
						args.RemoveTag(ImprovedDialog.pagingIndexTK);//zrusit info o prvnich indexech - seznam se cely zmeni tim kriteriem						
						args.SetTag(D_CharsAbilitiesList.criteriumTK, nameCriteria);
						args.RemoveTag(D_CharsAbilitiesList.listTK);//vycistit soucasny odkaz na taglist aby se mohl prenacist
						DialogStacking.ResendAndRestackDialog(gi);
						break;
					case 2: //name asc
						args.SetTag(D_CharsAbilitiesList.sortingTK, SortingCriteria.NameAsc);
						args.RemoveTag(D_CharsAbilitiesList.listTK);//vycistit soucasny odkaz na list aby se mohl prenacist
						DialogStacking.ResendAndRestackDialog(gi);
						break;
					case 3: //name desc
						args.SetTag(D_CharsAbilitiesList.sortingTK, SortingCriteria.NameDesc);
						args.RemoveTag(D_CharsAbilitiesList.listTK);//vycistit soucasny odkaz na list aby se mohl prenacist
						DialogStacking.ResendAndRestackDialog(gi);
						break;
					case 4: //roledefname asc
						args.SetTag(D_CharsAbilitiesList.sortingTK, SortingCriteria.DefnameAsc);
						args.RemoveTag(D_CharsAbilitiesList.listTK);//vycistit soucasny odkaz na list aby se mohl prenacist
						DialogStacking.ResendAndRestackDialog(gi);
						break;
					case 5: //roledefname desc
						args.SetTag(D_CharsAbilitiesList.sortingTK, SortingCriteria.DefnameDesc);
						args.RemoveTag(D_CharsAbilitiesList.listTK);//vycistit soucasny odkaz na list aby se mohl prenacist
						DialogStacking.ResendAndRestackDialog(gi);
						break;
					case 6: //running asc
						args.SetTag(D_CharsAbilitiesList.sortingTK, SortingCriteria.RunningAsc);
						args.RemoveTag(D_CharsAbilitiesList.listTK);//vycistit soucasny odkaz na list aby se mohl prenacist
						DialogStacking.ResendAndRestackDialog(gi);
						break;
					case 7: //running desc
						args.SetTag(D_CharsAbilitiesList.sortingTK, SortingCriteria.RunningDesc);
						args.RemoveTag(D_CharsAbilitiesList.listTK);//vycistit soucasny odkaz na list aby se mohl prenacist
						DialogStacking.ResendAndRestackDialog(gi);
						break;
				}
			} else if (ImprovedDialog.PagingButtonsHandled(gi, gr, abList.Count, 1)) {//kliknuto na paging?
				return;
			} else {
				//zjistime kterej cudlik z radku byl zmacknut
				int row = (int) (gr.pressedButton - 10) / 2;
				int buttNum = (int) (gr.pressedButton - 10) % 2;
				Ability ab = abList[row];
				Gump newGi;
				switch (buttNum) {
					case 0: //ability info
						newGi = gi.Cont.Dialog(SingletonScript<D_Info>.Instance, new DialogArgs(ab));
						DialogStacking.EnstackDialog(gi, newGi);
						break;
					case 1: //abilitydef info
						newGi = gi.Cont.Dialog(SingletonScript<D_Info>.Instance, new DialogArgs(ab.AbilityDef));
						DialogStacking.EnstackDialog(gi, newGi);
						break;
				}
			}
		}

		[Summary("Retreives the list of all chars abilities")]
		private List<Ability> ListifyAbilities(IEnumerable<Ability> abilities, string criteria) {
			List<Ability> absList = new List<Ability>();
			foreach (Ability entry in abilities) {
				if (criteria == null || criteria.Equals("")) {
					absList.Add(entry);//bereme vse
				} else if (entry.Name.ToUpper().Contains(criteria.ToUpper())) {
					absList.Add(entry);//jinak jen v pripade ze kriterium se vyskytuje v nazvu ability
				}
			}
			return absList;
		}

		[Summary("Sorting of the abilities list")]
		private void SortAbilities(List<Ability> list, SortingCriteria criteria) {
			switch (criteria) {
				case SortingCriteria.NameAsc:
					list.Sort(AbilitiesNameComparer.instance);
					break;
				case SortingCriteria.NameDesc:
					list.Sort(AbilitiesNameComparer.instance);
					list.Reverse();
					break;
				case SortingCriteria.DefnameAsc:
					list.Sort(AbilitiesAbilityDefsDefNameComparer.instance);
					break;
				case SortingCriteria.DefnameDesc:
					list.Sort(AbilitiesAbilityDefsDefNameComparer.instance);
					list.Reverse();
					break;
				case SortingCriteria.RunningAsc:
					list.Sort(AbilitiesRunningStateComparer.instance);
					break;
				case SortingCriteria.RunningDesc:
					list.Sort(AbilitiesRunningStateComparer.instance);
					list.Reverse();
					break;
			}
		}

		[Summary("Display a list of roles on a given character. Function accessible from the game." +
			   "The function is designed to be triggered using .x AbilitiessList(criteria)")]
		[SteamFunction]
		public static void AbilitiesList(Character self, ScriptArgs text) {
			DialogArgs newArgs = new DialogArgs();
			newArgs.SetTag(D_CharsAbilitiesList.sortingTK, SortingCriteria.NameAsc);//trideni
			if (text == null || text.argv == null || text.argv.Length == 0) {
				self.Dialog(SingletonScript<D_CharsAbilitiesList>.Instance, newArgs);
			} else {
				newArgs.SetTag(D_CharsAbilitiesList.criteriumTK, text.Args);//vyhl. kriterium
				self.Dialog(SingletonScript<D_CharsAbilitiesList>.Instance, newArgs);
			}
		}
	}

	[Summary("Comparer for sorting abilities by name asc")]
	public class AbilitiesNameComparer : IComparer<Ability> {
		public readonly static AbilitiesNameComparer instance = new AbilitiesNameComparer();

		private AbilitiesNameComparer() {
			//soukromy konstruktor, pristupovat budeme pres instanci
		}

		public int Compare(Ability x, Ability y) {
			return String.Compare(x.Name, y.Name, true);
		}
	}

	[Summary("Comparer for sorting abilities by their abilitydefs defname asc")]
	public class AbilitiesAbilityDefsDefNameComparer : IComparer<Ability> {
		public readonly static AbilitiesAbilityDefsDefNameComparer instance = new AbilitiesAbilityDefsDefNameComparer();

		private AbilitiesAbilityDefsDefNameComparer() {
			//soukromy konstruktor, pristupovat budeme pres instanci
		}

		public int Compare(Ability x, Ability y) {
			return String.Compare(x.AbilityDef.Defname, y.AbilityDef.Defname, true);
		}
	}

	[Summary("Comparer for sorting abilitiys by running status")]
	public class AbilitiesRunningStateComparer : IComparer<Ability> {
		public readonly static AbilitiesRunningStateComparer instance = new AbilitiesRunningStateComparer();

		private AbilitiesRunningStateComparer() {
			//soukromy konstruktor, pristupovat budeme pres instanci
		}

		public int Compare(Ability x, Ability y) {
			if (x.Running)
				return 1; //x running => always larger or equals to y
			return (y.Running ? -1 : 1); //x not running => if y is running x is less. otherwise is equal
		}
	}
}