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
using SteamEngine.LScript;

namespace SteamEngine.CompiledScripts.Dialogs {

	[Remark("Dialog that will display the admin dialog")]
	public class D_Admin : CompiledGump {
		internal static readonly TagKey playersListTK = TagKey.Get("__players_list_");
		internal static readonly TagKey plrListSortTK = TagKey.Get("__players_list_sorting_");
		
		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			//seznam lidi z parametru (if any)
			ArrayList playersList = null;
			if(!args.HasTag(D_Admin.playersListTK)) {
				playersList = ScriptUtil.ArrayListFromEnumerable(Server.AllPlayers);
				args.SetTag(D_Admin.playersListTK, playersList);//ulozime do parametru dialogu
			} else {
				playersList = (ArrayList)args.GetTag(D_Admin.playersListTK);
			}
            //zjistit zda bude paging, najit maximalni index na strance
			int firstiVal = TagMath.IGetTag(args,ImprovedDialog.pagingIndexTK);//prvni index na strance
			//maximalni index (20 radku mame) + hlidat konec seznamu...
			int imax = Math.Min(firstiVal + ImprovedDialog.PAGE_ROWS, playersList.Count);
			
			ImprovedDialog dialogHandler = new ImprovedDialog(this.GumpInstance);
			//pozadi    
			dialogHandler.CreateBackground(800);
			dialogHandler.SetLocation(50, 50);

			//nadpis
			dialogHandler.AddTable(new GUTATable(1,0,ButtonFactory.D_BUTTON_WIDTH));
			dialogHandler.LastTable[0,0] = TextFactory.CreateHeadline("Admin dialog - seznam pøipojených klientù ("+(firstiVal+1)+"-"+imax+" z "+playersList.Count+")");
			//cudlik na zavreni dialogu
			dialogHandler.LastTable[0,1] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonCross, 0);
			dialogHandler.MakeLastTableTransparent();

			//popis sloupecku
			dialogHandler.AddTable(new GUTATable(1, ButtonFactory.D_BUTTON_WIDTH, 180, 180, 180, 0)); 
			//cudlik pro privolani hrace
			dialogHandler.LastTable[0,0] = TextFactory.CreateLabel("Come");

			//Accounts
			dialogHandler.LastTable[0,1] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonSortUp, 1); //tridit podle accountu asc
            dialogHandler.LastTable[0,1] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonSortDown, 0, ButtonFactory.D_SORTBUTTON_LINE_OFFSET, 4); //tridit podle accountu desc			
            dialogHandler.LastTable[0,1] = TextFactory.CreateLabel(ButtonFactory.D_SORTBUTTON_COL_OFFSET, 0, "Account");

			//Jméno
            dialogHandler.LastTable[0,2] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonSortUp, 2); //tridit podle hráèù asc
            dialogHandler.LastTable[0,2] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonSortDown, 0, ButtonFactory.D_SORTBUTTON_LINE_OFFSET, 5); //tridit podle hráèù desc			
			dialogHandler.LastTable[0, 2] = TextFactory.CreateLabel(ButtonFactory.D_SORTBUTTON_COL_OFFSET, 0, "Jméno");

			//Lokace
            dialogHandler.LastTable[0,3] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonSortUp, 3); //tridit dle lokaci asc
            dialogHandler.LastTable[0,3] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonSortDown, 0, ButtonFactory.D_SORTBUTTON_LINE_OFFSET, 6); //tridit podle lokaci desc			
			dialogHandler.LastTable[0, 3] = TextFactory.CreateLabel(ButtonFactory.D_SORTBUTTON_COL_OFFSET, 0, "Lokace");

			//Akce
			dialogHandler.LastTable[0, 4] = TextFactory.CreateLabel("Action");
			dialogHandler.MakeLastTableTransparent(); //zpruhledni nadpisovy radek

			//vlastni seznam lidi
            dialogHandler.AddTable(new GUTATable(imax-firstiVal));
			dialogHandler.CopyColsFromLastTable();

			switch ((SortingCriteria)args.GetTag(D_Admin.plrListSortTK)) {
				case SortingCriteria.NameAsc:
					playersList.Sort(CharComparerByName.instance);                   
					break;
                case SortingCriteria.NameDesc:
                    playersList.Sort(CharComparerByName.instance);
                    playersList.Reverse();
                    break;
				case SortingCriteria.LocationAsc:
					playersList.Sort(CharComparerByLocation.instance);
					break;
                case SortingCriteria.LocationDesc:
                    playersList.Sort(CharComparerByLocation.instance);
                    playersList.Reverse();
                    break;
				case SortingCriteria.AccountAsc:
					playersList.Sort(CharComparerByAccount.instance);
					break;
                case SortingCriteria.AccountDesc:
                    playersList.Sort(CharComparerByAccount.instance);
                    playersList.Reverse();
                    break;
                default:
                    break; //netridit
			}
            
			//projet seznam v ramci daneho rozsahu indexu
			int rowCntr = 0;
			for(int i = firstiVal; i < imax; i++) {
				Player plr = (Player)playersList[i];
				Hues plrColor = Hues.PlayerColor;
				//TODO - barveni dle prislusnosti
				dialogHandler.LastTable[rowCntr,0] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonTick, (4 * i) + 10); //player come
                dialogHandler.LastTable[rowCntr,1] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonPaper, (4 * i) + 11); //account detail
				dialogHandler.LastTable[rowCntr,1] = TextFactory.CreateText(ButtonFactory.D_BUTTON_WIDTH, 0, plrColor, plr.Account.Name); //acc name
                dialogHandler.LastTable[rowCntr,2] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonPaper, (4 * i) + 12); //player info
				dialogHandler.LastTable[rowCntr,2] = TextFactory.CreateText(ButtonFactory.D_BUTTON_WIDTH, 0, plrColor, plr.Name); //plr name
                dialogHandler.LastTable[rowCntr,3] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonTick, (4 * i) + 13); //goto location
				dialogHandler.LastTable[rowCntr,3] = TextFactory.CreateText(ButtonFactory.D_BUTTON_WIDTH, 0, plrColor, plr.Region.Name); //region name
				dialogHandler.LastTable[rowCntr,4] = TextFactory.CreateText(plrColor, plr.CurrentSkillName.ToString()); //action name
				
				rowCntr++;			
			}
			dialogHandler.MakeLastTableTransparent(); //zpruhledni zbytek dialogu

			//now handle the paging 
			dialogHandler.CreatePaging(playersList.Count, firstiVal,1);

			dialogHandler.WriteOut();
		}

		public override void OnResponse(GumpInstance gi, GumpResponse gr, DialogArgs args) {
			//seznam hracu bereme z kontextu (mohl byt jiz trideny atd)
			ArrayList playersList = (ArrayList)args.GetTag(D_Admin.playersListTK);
            if(gr.pressedButton < 10) { //ovladaci tlacitka (sorting, paging atd)
				switch(gr.pressedButton) {
                    case 0: //exit
						DialogStacking.ShowPreviousDialog(gi); //zobrazit pripadny predchozi dialog						
                        break;
                    case 1: //acc tøídit asc
						args.SetTag(D_Admin.plrListSortTK, SortingCriteria.AccountAsc);//uprav info o sortovani
						DialogStacking.ResendAndRestackDialog(gi);
						break;
                    case 2: //hráèi tøídit asc
						args.SetTag(D_Admin.plrListSortTK, SortingCriteria.NameAsc);//uprav info o sortovani
						DialogStacking.ResendAndRestackDialog(gi);
						break;
                    case 3: //lokace tøídit asc
						args.SetTag(D_Admin.plrListSortTK, SortingCriteria.LocationAsc);//uprav info o sortovani
						DialogStacking.ResendAndRestackDialog(gi);
						break;                    
                    case 4: //acc tøídit desc
						args.SetTag(D_Admin.plrListSortTK, SortingCriteria.AccountDesc);//uprav info o sortovani
						DialogStacking.ResendAndRestackDialog(gi);
						break;
                    case 5: //hráèi tøídit desc
						args.SetTag(D_Admin.plrListSortTK, SortingCriteria.NameDesc);//uprav info o sortovani
						DialogStacking.ResendAndRestackDialog(gi);
						break;
                    case 6: //lokace tøídit desc
						args.SetTag(D_Admin.plrListSortTK, SortingCriteria.LocationDesc);//uprav info o sortovani
						DialogStacking.ResendAndRestackDialog(gi);
						break;
                }
			} else if(ImprovedDialog.PagingButtonsHandled(gi, gr, playersList.Count, 1)) {//posledni 1 - pocet sloupecku v dialogu				
				return;
			} else { //skutecna adminovaci tlacitka z radku
                //zjistime kterej cudlik z radku byl zmacknut
                int row = (int)(gr.pressedButton - 10) / 4;
                int buttNum = (int)(gr.pressedButton - 10) % 4;
                Player plr = (Player)playersList[row];
				GumpInstance newGi;
				DialogArgs newArgs;
                switch(buttNum) {
                    case 0: //player come
                        plr.Go(gi.Cont);
                        break;
                    case 1: //acc info
						newArgs = new DialogArgs(0,0); //buttons, fields paging
						newArgs.SetTag(D_Info.infoizedTargTK, plr.Account);
						newGi = gi.Cont.Dialog(SingletonScript<D_Info>.Instance, newArgs);
						DialogStacking.EnstackDialog(gi, newGi);
                        break;
                    case 2: //player info
						newArgs = new DialogArgs(0, 0); //buttons, fields paging
						newArgs.SetTag(D_Info.infoizedTargTK, plr);						
						newGi = gi.Cont.Dialog(SingletonScript<D_Info>.Instance, newArgs);
						DialogStacking.EnstackDialog(gi, newGi);
                        break;
                    case 3: //goto location
                        ((Character)gi.Cont).Go(plr);
                        break;
                }
            }
		}
	}

    [Remark("Comparer for sorting players (chars) by name asc")]
    public class CharComparerByName : IComparer<Character>, IComparer {
        public readonly static CharComparerByName instance = new CharComparerByName();

        public int Compare(object a, object b) {
            return Compare((Character)a, (Character)b);
        }

        public int Compare(Character x, Character y) {
            return string.Compare(x.Name,y.Name);
        }
    }

    [Remark("Comparer for sorting players (chars) by location name asc")]
    public class CharComparerByLocation : IComparer<Character>, IComparer {
        public readonly static CharComparerByLocation instance = new CharComparerByLocation();

        public int Compare(object a, object b) {
            return Compare((Character)a, (Character)b);
        }

        public int Compare(Character x, Character y) {
            return string.Compare(x.Region == null ? "" : x.Region.Name,
                y.Region == null ? "" : y.Region.Name);
        }
    }

    [Remark("Comparer for sorting players (chars) by account name asc")]
    public class CharComparerByAccount : IComparer<Character>, IComparer {
        public readonly static CharComparerByAccount instance = new CharComparerByAccount();
        
        public int Compare(object a, object b) {
            return Compare((Character)a, (Character)b);
        }

        public int Compare(Character x, Character y) {
            return string.Compare(x.Account == null ? "" : x.Account.Name,
                y.Account == null ? "" : y.Account.Name);
        }
    }
}