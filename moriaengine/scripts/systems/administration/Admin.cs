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
		static readonly TagKey players = TagKey.Get("plrList");
        
        [Remark("Instance of the D_Admin, for possible access from other dialogs etc.")]
        private static D_Admin instance;
		public static D_Admin Instance {
			get {
				return instance;
			}
		}
        [Remark("Set the static reference to the instance of this dialog")]
		public D_Admin() {
			instance = this;
		}

		public override void Construct(Thing focus, AbstractCharacter src, object[] sa) {
			//seznam lidi, ulozit na dialog
            ArrayList playersList = ScriptUtil.ArrayListFromEnumerable(Server.AllPlayers);            
            this.GumpInstance.SetTag(players, playersList);
			//zjistit zda bude paging, najit maximalni index na strance
			int firstiVal = Convert.ToInt32(sa[1]);   //prvni index na strance
			//maximalni index (20 radku mame) + hlidat konec seznamu...
			int imax = Math.Min(firstiVal + ImprovedDialog.PAGE_ROWS, playersList.Count);
			
			ImprovedDialog dialogHandler = new ImprovedDialog(this.GumpInstance);
			//pozadi    
			dialogHandler.CreateBackground(800);
			dialogHandler.SetLocation(50, 50);

			//nadpis
			dialogHandler.Add(new GUTATable(1,0,ButtonFactory.D_BUTTON_WIDTH));
			dialogHandler.LastTable[0,0] = TextFactory.CreateHeadline("Admin dialog - seznam pøipojených klientù ("+(firstiVal+1)+"-"+imax+" z "+playersList.Count+")");
			//cudlik na zavreni dialogu
			dialogHandler.LastTable[0,1] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonCross, 0);
			dialogHandler.MakeTableTransparent();

			//popis sloupecku
			dialogHandler.Add(new GUTATable(1, ButtonFactory.D_BUTTON_WIDTH, 180, 180, 180, 0)); 
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
			dialogHandler.MakeTableTransparent(); //zpruhledni nadpisovy radek

			//vlastni seznam lidi
            dialogHandler.Add(new GUTATable(imax-firstiVal));
			dialogHandler.CopyColsFromLastTable();

			switch ((SortingCriteria)sa[0]) {
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
				dialogHandler.LastTable[rowCntr,4] = TextFactory.CreateText(plrColor, plr.Action.ToString()); //region name
				
				rowCntr++;			
			}
			dialogHandler.MakeTableTransparent(); //zpruhledni zbytek dialogu

			//now handle the paging 
			dialogHandler.CreatePaging(playersList.Count, firstiVal,1);

			//uložit info o právì vytvoøeném dialogu pro návrat
			DialogStackItem.EnstackDialog(src, focus, D_Admin.Instance,
					(SortingCriteria)sa[0], //typ tøídìní
					firstiVal); //cislo zpravy kterou zacina stranka (pro paging)	

			dialogHandler.WriteOut();
		}

		public override void OnResponse(GumpInstance gi, GumpResponse gr) {
			//seznam hracu bereme z kontextu (mohl byt jiz trideny atd)
			ArrayList playersList = (ArrayList)gi.GetTag(players);
            if(gr.pressedButton < 10) { //ovladaci tlacitka (sorting, paging atd)
				DialogStackItem dsi = null;
                switch(gr.pressedButton) {
                    case 0: //exit
						DialogStackItem.PopStackedDialog(gi.Cont.Conn);	//odstranit ze stacku aktualni dialog (tj. tenhle)
						DialogStackItem.ShowPreviousDialog(gi.Cont.Conn); //zobrazit pripadny predchozi dialog						
                        break;
                    case 1: //acc tøídit asc
						dsi = DialogStackItem.PopStackedDialog(gi.Cont.Conn);//vem ulozene info o dialogu
						dsi.Args[0] = SortingCriteria.AccountAsc; //uprav info o sortovani
						dsi.Show();
                        break;
                    case 2: //hráèi tøídit asc
						dsi = DialogStackItem.PopStackedDialog(gi.Cont.Conn);//vem ulozene info o dialogu
						dsi.Args[0] = SortingCriteria.NameAsc; //uprav info o sortovani
						dsi.Show();
						break;
                    case 3: //lokace tøídit asc
						dsi = DialogStackItem.PopStackedDialog(gi.Cont.Conn);//vem ulozene info o dialogu
						dsi.Args[0] = SortingCriteria.LocationAsc; //uprav info o sortovani
						dsi.Show();
						break;                    
                    case 4: //acc tøídit desc
						dsi = DialogStackItem.PopStackedDialog(gi.Cont.Conn);//vem ulozene info o dialogu
						dsi.Args[0] = SortingCriteria.AccountDesc; //uprav info o sortovani
						dsi.Show();
						break;
                    case 5: //hráèi tøídit desc
						dsi = DialogStackItem.PopStackedDialog(gi.Cont.Conn);//vem ulozene info o dialogu
						dsi.Args[0] = SortingCriteria.NameDesc; //uprav info o sortovani
						dsi.Show();
						break;
                    case 6: //lokace tøídit desc
						dsi = DialogStackItem.PopStackedDialog(gi.Cont.Conn);//vem ulozene info o dialogu
						dsi.Args[0] = SortingCriteria.LocationDesc; //uprav info o sortovani
						dsi.Show();
						break;
                }
			} else if(ImprovedDialog.PagingButtonsHandled(gi, gr, 1, playersList.Count,1)) {
				//kliknuto na paging? (1 = index parametru nesoucim info o pagingu (zde dsi.Args[1] viz výše)				
				//posledni 1 - pocet sloupecku v dialogu
				return;
			} else { //skutecna adminovaci tlacitka z radku
                //zjistime kterej cudlik z radku byl zmacknut
                int row = (int)(gr.pressedButton - 10) / 4;
                int buttNum = (int)(gr.pressedButton - 10) % 4;
                Player plr = (Player)playersList[row];
                switch(buttNum) {
                    case 0: //player come
                        plr.Go(gi.Cont);
                        break;
                    case 1: //acc info
                        ///TODO
                        break;
                    case 2: //player info
                        ///TODO
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