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

    [Summary("Dialog listing all available roles (role defs)")]
    public class D_RolesList : CompiledGumpDef {
        internal static readonly TagKey listTK = TagKey.Get("_roles_list_");
        internal static readonly TagKey criteriumTK = TagKey.Get("_roles_criterium_");
        internal static readonly TagKey sortingTK = TagKey.Get("_roles_sorting_");

        private static int width = 600;

        public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
            //vzit seznam roli
            List<RoleDef> rlList = args.GetTag(D_RolesList.listTK) as List<RoleDef>;

            if (rlList == null) {
                //vzit seznam roli dle vyhledavaciho kriteria
                //toto se provede jen pri prvnim zobrazeni nebo zmene kriteria!
                rlList = ListifyRoles(RoleDef.AllRoles, TagMath.SGetTag(args, D_RolesList.criteriumTK));
				SortRoleDefs(rlList, (SortingCriteria) args.GetTag(D_RolesList.sortingTK));
                args.SetTag(D_RolesList.listTK, rlList); //ulozime to do argumentu dialogu				
            }
            int firstiVal = TagMath.IGetTag(args, ImprovedDialog.pagingIndexTK);//prvni index na strance
            int imax = Math.Min(firstiVal + ImprovedDialog.PAGE_ROWS, rlList.Count);

            ImprovedDialog dlg = new ImprovedDialog(this.GumpInstance);
            //pozadi    
            dlg.CreateBackground(width);
            dlg.SetLocation(70, 70);

            //nadpis
            dlg.AddTable(new GUTATable(1, 0, ButtonFactory.D_BUTTON_WIDTH));
            dlg.LastTable[0, 0] = TextFactory.CreateHeadline("Seznam všech rolí (zobrazeno " + (firstiVal + 1) + "-" + imax + " z " + rlList.Count + ")");
            dlg.LastTable[0, 1] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonCross, 0);//cudlik na zavreni dialogu
            dlg.MakeLastTableTransparent();

            //cudlik a input field na zuzeni vyberu
            dlg.AddTable(new GUTATable(1, 130, 0, ButtonFactory.D_BUTTON_WIDTH));
            dlg.LastTable[0, 0] = TextFactory.CreateLabel("Vyhledávací kriterium");
            dlg.LastTable[0, 1] = InputFactory.CreateInput(LeafComponentTypes.InputText, 33);
            dlg.LastTable[0, 2] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonPaper, 1);
            dlg.MakeLastTableTransparent();

            //popis sloupcu
            dlg.AddTable(new GUTATable(1, 300, 0, ButtonFactory.D_BUTTON_WIDTH));
            dlg.LastTable[0, 0] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonSortUp, 2); //tridit podle name asc
            dlg.LastTable[0, 0] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonSortDown, 0, ButtonFactory.D_SORTBUTTON_LINE_OFFSET, 3); //tridit podle name desc				
            dlg.LastTable[0, 0] = TextFactory.CreateLabel(ButtonFactory.D_SORTBUTTON_COL_OFFSET, 0, "Název");
            dlg.LastTable[0, 1] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonSortUp, 4); //tridit dle defname asc
            dlg.LastTable[0, 1] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonSortDown, 0, ButtonFactory.D_SORTBUTTON_LINE_OFFSET, 5); //tridit dle defname desc
            dlg.LastTable[0, 1] = TextFactory.CreateLabel(ButtonFactory.D_SORTBUTTON_COL_OFFSET, 0, "Defname");
            dlg.LastTable[0, 2] = TextFactory.CreateLabel("Info");
            dlg.MakeLastTableTransparent();

            //seznam roli
            dlg.AddTable(new GUTATable(imax - firstiVal));
            dlg.CopyColsFromLastTable();

            //projet seznam v ramci daneho rozsahu indexu
            int rowCntr = 0;
            for (int i = firstiVal; i < imax; i++) {
                RoleDef rd = rlList[i];

                dlg.LastTable[rowCntr, 0] = TextFactory.CreateText(rd.Name);
                dlg.LastTable[rowCntr, 1] = TextFactory.CreateText(rd.Defname);
                //a na zaver odkaz do infodialogu
                dlg.LastTable[rowCntr, 2] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonPaper, 10 + i);
                rowCntr++;
            }
            dlg.MakeLastTableTransparent(); //zpruhledni zbytek dialogu

            //ted paging
            dlg.CreatePaging(rlList.Count, firstiVal, 1);

            dlg.WriteOut();
        }

        public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
            //seznam roledefu bereme z parametru (mohl byt jiz trideny atd, nebudeme ho proto selectit znova)
            List<RoleDef> rlList = (List<RoleDef>)args.GetTag(D_RolesList.listTK);
            int firstOnPage = TagMath.IGetTag(args, ImprovedDialog.pagingIndexTK);
            int imax = Math.Min(firstOnPage + ImprovedDialog.PAGE_ROWS, rlList.Count);
            if (gr.pressedButton < 10) { //ovladaci tlacitka (exit, new, vyhledej)				
                switch (gr.pressedButton) {
                    case 0: //exit
                        DialogStacking.ShowPreviousDialog(gi); //zobrazit pripadny predchozi dialog
                        break;
                    case 1: //vyhledat dle zadani
                        string nameCriteria = gr.GetTextResponse(33);
                        args.RemoveTag(ImprovedDialog.pagingIndexTK);//zrusit info o prvnich indexech - seznam se cely zmeni tim kriteriem						
                        args.SetTag(D_RolesList.criteriumTK, nameCriteria);
                        args.RemoveTag(D_RolesList.listTK);//vycistit soucasny odkaz na list aby se mohl prenacist
                        DialogStacking.ResendAndRestackDialog(gi);
                        break;
                    case 2: //name asc
						args.SetTag(D_RolesList.sortingTK, SortingCriteria.NameAsc);
						args.RemoveTag(D_RolesList.listTK);//vycistit soucasny odkaz na list aby se mohl prenacist
                        DialogStacking.ResendAndRestackDialog(gi);
                        break;
                    case 3: //name desc
						args.SetTag(D_RolesList.sortingTK, SortingCriteria.NameDesc);
						args.RemoveTag(D_RolesList.listTK);//vycistit soucasny odkaz na list aby se mohl prenacist
                        DialogStacking.ResendAndRestackDialog(gi);
                        break;
                    case 4: //defname asc
						args.SetTag(D_RolesList.sortingTK, SortingCriteria.DefnameAsc);
						args.RemoveTag(D_RolesList.listTK);//vycistit soucasny odkaz na list aby se mohl prenacist
                        DialogStacking.ResendAndRestackDialog(gi);
                        break;
                    case 5: //defname desc
						args.SetTag(D_RolesList.sortingTK, SortingCriteria.DefnameDesc);
						args.RemoveTag(D_RolesList.listTK);//vycistit soucasny odkaz na list aby se mohl prenacist
                        DialogStacking.ResendAndRestackDialog(gi);
                        break;
                }
            } else if (ImprovedDialog.PagingButtonsHandled(gi, gr, rlList.Count, 1)) {//kliknuto na paging?
                return;
            } else {
                //zjistime si radek
                int row = ((int)gr.pressedButton - 10);
                RoleDef ad = rlList[row];
                //a zobrazime info dialog
                Gump newGi = gi.Cont.Dialog(SingletonScript<D_Info>.Instance, new DialogArgs(ad));
                DialogStacking.EnstackDialog(gi, newGi);
            }
        }

        [Summary("Retreives the list of all existing roledefs")]
        private List<RoleDef> ListifyRoles(IEnumerable<RoleDef> roles, string criteria) {
            List<RoleDef> rlsList = new List<RoleDef>();
            foreach (RoleDef entry in roles) {
                if (criteria == null || criteria.Equals("")) {
                    rlsList.Add(entry);//bereme vse
                } else if (entry.Name.ToUpper().Contains(criteria.ToUpper())) {
                    rlsList.Add(entry);//jinak jen v pripade ze kriterium se vyskytuje v nazvu ability
                }
            }
            return rlsList;
        }

        [Summary("Sorting of the roledefs list")]
		private void SortRoleDefs(List<RoleDef> list, SortingCriteria criteria) {
            switch (criteria) {
				case SortingCriteria.NameAsc:
                    list.Sort(RoleDefsNameComparer.instance);
                    break;
				case SortingCriteria.NameDesc:
                    list.Sort(RoleDefsNameComparer.instance);
                    list.Reverse();
                    break;
				case SortingCriteria.DefnameAsc:
                    list.Sort(RoleDefsDefNameComparer.instance);
                    break;
				case SortingCriteria.DefnameDesc:
                    list.Sort(RoleDefsDefNameComparer.instance);
                    list.Reverse();
                    break;
            }
        }

        [Summary("Display a list of all roles. Function accessible from the game." +
               "The function is designed to be triggered using .AllRoles(criteria)")]
        [SteamFunction]
        public static void AllRoles(Character self, ScriptArgs text) {
            DialogArgs newArgs = new DialogArgs();
			newArgs.SetTag(D_RolesList.sortingTK, SortingCriteria.NameAsc);//default sorting
            if (text == null || text.argv == null || text.argv.Length == 0) {
                self.Dialog(SingletonScript<D_RolesList>.Instance, newArgs);
            } else {
                newArgs.SetTag(D_RolesList.criteriumTK, text.Args);//vyhl. kriterium
                self.Dialog(SingletonScript<D_RolesList>.Instance, newArgs);
            }
        }
    }

    [Summary("Comparer for sorting roledefs by name asc")]
    public class RoleDefsNameComparer : IComparer<RoleDef> {
        public readonly static RoleDefsNameComparer instance = new RoleDefsNameComparer();

        private RoleDefsNameComparer() {
            //soukromy konstruktor, pristupovat budeme pres instanci
        }

        public int Compare(RoleDef x, RoleDef y) {
            return String.Compare(x.Name, y.Name, true);
        }
    }

    [Summary("Comparer for sorting roledefs by defnames asc")]
    public class RoleDefsDefNameComparer : IComparer<RoleDef> {
        public readonly static RoleDefsDefNameComparer instance = new RoleDefsDefNameComparer();

        private RoleDefsDefNameComparer() {
            //soukromy konstruktor, pristupovat budeme pres instanci
        }

        public int Compare(RoleDef x, RoleDef y) {
            return String.Compare(x.Defname, y.Defname, true);
        }
    }
}