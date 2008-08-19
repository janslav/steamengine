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

    [Summary("Dialog listing all character's roles")]
    public class D_CharsRolesList : CompiledGumpDef {
        internal static readonly TagKey listTK = TagKey.Get("_roles_set_");
        internal static readonly TagKey criteriumTK = TagKey.Get("_roles_criterium_");
        internal static readonly TagKey sortingTK = TagKey.Get("_roles_sorting_");

        private static int width = 600;

        public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
            //vzit seznam roli
            List<Role> rlList = args.GetTag(D_CharsRolesList.listTK) as List<Role>;

            if (rlList == null) {
                //vzit seznam roli z focusa dle vyhledavaciho kriteria
                Character whose = (Character)focus;
                HashSet<Role> rolesSet = RolesManagement.GetCharactersRoles(whose);
                //toto se provede jen pri prvnim zobrazeni nebo zmene kriteria!
                rlList = ListifyRoles(rolesSet, TagMath.SGetTag(args, D_CharsRolesList.criteriumTK));
				SortRoles(rlList, (SortingCriteria) args.GetTag(D_CharsRolesList.sortingTK));
                args.SetTag(D_CharsRolesList.listTK, rlList); //ulozime to do argumentu dialogu				
            }
            int firstiVal = TagMath.IGetTag(args, ImprovedDialog.pagingIndexTK);//prvni index na strance
            int imax = Math.Min(firstiVal + ImprovedDialog.PAGE_ROWS, rlList.Count);

            ImprovedDialog dlg = new ImprovedDialog(this.GumpInstance);
            //pozadi    
            dlg.CreateBackground(width);
            dlg.SetLocation(70, 70);

            //nadpis
            dlg.AddTable(new GUTATable(1, 0, ButtonFactory.D_BUTTON_WIDTH));
            dlg.LastTable[0, 0] = TextFactory.CreateHeadline("Seznam rol� co m� "+focus.Name+" (zobrazeno " + (firstiVal + 1) + "-" + imax + " z " + rlList.Count + ")");
            dlg.LastTable[0, 1] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonCross, 0);//cudlik na zavreni dialogu
            dlg.MakeLastTableTransparent();

            //cudlik a input field na zuzeni vyberu
            dlg.AddTable(new GUTATable(1, 130, 0, ButtonFactory.D_BUTTON_WIDTH));
            dlg.LastTable[0, 0] = TextFactory.CreateLabel("Vyhled�vac� kriterium");
            dlg.LastTable[0, 1] = InputFactory.CreateInput(LeafComponentTypes.InputText, 33);
            dlg.LastTable[0, 2] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonPaper, 1);
            dlg.MakeLastTableTransparent();

            //popis sloupcu
            dlg.AddTable(new GUTATable(1, ButtonFactory.D_BUTTON_WIDTH, 300, 0, ButtonFactory.D_BUTTON_WIDTH));
            dlg.LastTable[0, 0] = TextFactory.CreateLabel("Info");
            dlg.LastTable[0, 1] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonSortUp, 2); //tridit podle name asc
            dlg.LastTable[0, 1] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonSortDown, 0, ButtonFactory.D_SORTBUTTON_LINE_OFFSET, 3); //tridit podle name desc				
            dlg.LastTable[0, 1] = TextFactory.CreateLabel(ButtonFactory.D_SORTBUTTON_COL_OFFSET, 0, "N�zev");
            dlg.LastTable[0, 2] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonSortUp, 4); //tridit podle roledefname asc
            dlg.LastTable[0, 2] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonSortDown, 0, ButtonFactory.D_SORTBUTTON_LINE_OFFSET, 5); //tridit podle roledefname desc				
            dlg.LastTable[0, 2] = TextFactory.CreateLabel(ButtonFactory.D_SORTBUTTON_COL_OFFSET, 0, "Roledef");            
            dlg.LastTable[0, 3] = TextFactory.CreateLabel("Roledef info");
            dlg.MakeLastTableTransparent();

            //seznam roli
            dlg.AddTable(new GUTATable(imax - firstiVal));
            dlg.CopyColsFromLastTable();

            //projet seznam v ramci daneho rozsahu indexu
            int rowCntr = 0;
            for (int i = firstiVal; i < imax; i++) {
                Role rl = rlList[i];
                //infodialog
                dlg.LastTable[rowCntr, 0] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonPaper, 10 + 2*i);
                dlg.LastTable[rowCntr, 1] = TextFactory.CreateText(rl.Name);
                dlg.LastTable[rowCntr, 2] = TextFactory.CreateText(rl.RoleDef.Defname);
                //roledef info dialog
                dlg.LastTable[rowCntr, 3] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonPaper, 11 + 2*i);
                rowCntr++;
            }
            dlg.MakeLastTableTransparent(); //zpruhledni zbytek dialogu

            //ted paging
            dlg.CreatePaging(rlList.Count, firstiVal, 1);

            dlg.WriteOut();
        }

        public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
            //seznam tagu bereme z parametru (mohl byt jiz trideny atd, nebudeme ho proto selectit znova)
            List<Role> rlList = (List<Role>)args.GetTag(D_CharsRolesList.listTK);
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
                        args.SetTag(D_CharsRolesList.criteriumTK, nameCriteria);
                        args.RemoveTag(D_CharsRolesList.listTK);//vycistit soucasny odkaz na taglist aby se mohl prenacist
                        DialogStacking.ResendAndRestackDialog(gi);
                        break;
                    case 2: //name asc
						args.SetTag(D_CharsRolesList.sortingTK, SortingCriteria.NameAsc);
                        args.RemoveTag(D_CharsRolesList.listTK);//vycistit soucasny odkaz na taglist aby se mohl prenacist
                        DialogStacking.ResendAndRestackDialog(gi);
                        break;
                    case 3: //name desc
						args.SetTag(D_CharsRolesList.sortingTK, SortingCriteria.NameDesc);
                        args.RemoveTag(D_CharsRolesList.listTK);//vycistit soucasny odkaz na taglist aby se mohl prenacist
                        DialogStacking.ResendAndRestackDialog(gi);
                        break;
                    case 4: //roledefname asc
						args.SetTag(D_CharsRolesList.sortingTK, SortingCriteria.DefnameAsc);
                        args.RemoveTag(D_CharsRolesList.listTK);//vycistit soucasny odkaz na taglist aby se mohl prenacist
                        DialogStacking.ResendAndRestackDialog(gi);
                        break;
                    case 5: //roledefname desc
						args.SetTag(D_CharsRolesList.sortingTK, SortingCriteria.DefnameDesc);
                        args.RemoveTag(D_CharsRolesList.listTK);//vycistit soucasny odkaz na taglist aby se mohl prenacist
                        DialogStacking.ResendAndRestackDialog(gi);
                        break;
                }
            } else if (ImprovedDialog.PagingButtonsHandled(gi, gr, rlList.Count, 1)) {//kliknuto na paging?
                return;
            } else {
                //zjistime kterej cudlik z radku byl zmacknut
                int row = (int)(gr.pressedButton - 10) / 2;
                int buttNum = (int)(gr.pressedButton - 10) % 2;
                Role rl = rlList[row];
                Gump newGi;
                switch (buttNum) {
                    case 0: //role info
                        newGi = gi.Cont.Dialog(SingletonScript<D_Info>.Instance, new DialogArgs(rl));
                        DialogStacking.EnstackDialog(gi, newGi);
                        break;
                    case 1: //role def info
                        newGi = gi.Cont.Dialog(SingletonScript<D_Info>.Instance, new DialogArgs(rl.RoleDef));
                        DialogStacking.EnstackDialog(gi, newGi);
                        break;                    
                }
            }
        }

        [Summary("Retreives the list of all chars roles")]
        private List<Role> ListifyRoles(IEnumerable<Role> roles, string criteria) {
            List<Role> rlsList = new List<Role>();
            foreach (Role entry in roles) {
                if (criteria == null || criteria.Equals("")) {
                    rlsList.Add(entry);//bereme vse
                } else if (entry.Name.ToUpper().Contains(criteria.ToUpper())) {
                    rlsList.Add(entry);//jinak jen v pripade ze kriterium se vyskytuje v nazvu ability
                }
            }
            return rlsList;
        }

        [Summary("Sorting of the roles list")]
		private void SortRoles(List<Role> list, SortingCriteria criteria) {
            switch (criteria) {
				case SortingCriteria.NameAsc:
                    list.Sort(RolesNameComparer.instance);
                    break;
				case SortingCriteria.NameDesc:
                    list.Sort(RolesNameComparer.instance);
                    list.Reverse();
                    break;
				case SortingCriteria.DefnameAsc:
                    list.Sort(RolesNameComparer.instance);
                    break;
				case SortingCriteria.DefnameDesc:
                    list.Sort(RolesNameComparer.instance);
                    list.Reverse();
                    break;
            }
        }

        [Summary("Display a list of roles on a given character. Function accessible from the game." +
                "The function is designed to be triggered using .x RolesList(criteria)")]
        [SteamFunction]
        public static void RolesList(Character self, ScriptArgs text) {
            DialogArgs newArgs = new DialogArgs();
			newArgs.SetTag(D_CharsRolesList.sortingTK, SortingCriteria.NameAsc);//trideni
            if (text == null || text.argv == null || text.argv.Length == 0) {
				self.Dialog(SingletonScript<D_CharsRolesList>.Instance, newArgs);
            } else {                
                newArgs.SetTag(D_CharsRolesList.criteriumTK, text.Args);//vyhl. kriterium
                self.Dialog(SingletonScript<D_CharsRolesList>.Instance, newArgs);
            }
        }
    }

    [Summary("Comparer for sorting roles by name asc")]
    public class RolesNameComparer : IComparer<Role> {
        public readonly static RolesNameComparer instance = new RolesNameComparer();

        private RolesNameComparer() {
            //soukromy konstruktor, pristupovat budeme pres instanci
        }

        public int Compare(Role x, Role y) {
            return String.Compare(x.Name, y.Name, true);
        }
    }

    [Summary("Comparer for sorting roles by their roledefs defname asc")]
    public class RolesRoleDefsDefNameComparer : IComparer<Role> {
        public readonly static RolesRoleDefsDefNameComparer instance = new RolesRoleDefsDefNameComparer();

        private RolesRoleDefsDefNameComparer() {
            //soukromy konstruktor, pristupovat budeme pres instanci
        }

        public int Compare(Role x, Role y) {
            return String.Compare(x.RoleDef.Defname, y.RoleDef.Defname, true);
        }
    }
}