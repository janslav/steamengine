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
			int firstiVal = (sa[1] == null ? 0 : (int)sa[1]);   //prvni index na strance (neprisel v argumentu?)
			int imax = firstiVal + ImprovedDialog.PAGE_ROWS; //maximalni index (20 radku mame)
			if (imax >= playersList.Count) { //neni uz konec seznamu?
 				imax = playersList.Count;
			}

			ImprovedDialog dialogHandler = new ImprovedDialog(this.GumpInstance);
			//pozadi    
			dialogHandler.CreateBackground(800);
			dialogHandler.SetLocation(50, 50);

			//nadpis
			dialogHandler.Add(new GumpTable(1, ButtonFactory.D_BUTTON_HEIGHT));
			dialogHandler.Add(new GumpColumn());
			dialogHandler.Add(TextFactory.CreateText("Admin dialog - seznam pøipojených klientù ("+(firstiVal+1)+"-"+imax+" z "+playersList.Count+")"));
			//cudlik na zavreni dialogu
			dialogHandler.AddLast(new GumpColumn(ButtonFactory.D_BUTTON_WIDTH));
			dialogHandler.Add(ButtonFactory.CreateButton(LeafComponentTypes.ButtonCross, 0));
			dialogHandler.MakeTableTransparent();

			//popis sloupecku
			dialogHandler.Add(new GumpTable(1)); //radek na nadpisy

            dialogHandler.Add(new GumpColumn(ButtonFactory.D_BUTTON_WIDTH)); //cudlik pro privolani hrace
			dialogHandler.Add(TextFactory.CreateText("Come"));

			dialogHandler.Add(new GumpColumn(180)); //Accounts
            dialogHandler.Add(ButtonFactory.CreateButton(LeafComponentTypes.ButtonSortUp, 1)); //tridit podle accountu asc
            dialogHandler.Add(ButtonFactory.CreateButton(LeafComponentTypes.ButtonSortDown, 0, ButtonFactory.D_SORTBUTTON_LINE_OFFSET, 6)); //tridit podle accountu desc			
            dialogHandler.Add(TextFactory.CreateText(ButtonFactory.D_SORTBUTTON_COL_OFFSET, 0, "Account"));

			dialogHandler.Add(new GumpColumn(180)); //Jméno
            dialogHandler.Add(ButtonFactory.CreateButton(LeafComponentTypes.ButtonSortUp, 2)); //tridit podle hráèù asc
            dialogHandler.Add(ButtonFactory.CreateButton(LeafComponentTypes.ButtonSortDown, 0, ButtonFactory.D_SORTBUTTON_LINE_OFFSET, 7)); //tridit podle hráèù desc			
            dialogHandler.Add(TextFactory.CreateText(ButtonFactory.D_SORTBUTTON_COL_OFFSET, 0, "Jméno"));

			dialogHandler.Add(new GumpColumn(180));//Lokace
            dialogHandler.Add(ButtonFactory.CreateButton(LeafComponentTypes.ButtonSortUp, 3)); //tridit dle lokaci asc
            dialogHandler.Add(ButtonFactory.CreateButton(LeafComponentTypes.ButtonSortDown, 0, ButtonFactory.D_SORTBUTTON_LINE_OFFSET, 8)); //tridit podle lokaci desc			
            dialogHandler.Add(TextFactory.CreateText(ButtonFactory.D_SORTBUTTON_COL_OFFSET, 0, "Lokace"));

			dialogHandler.Add(new GumpColumn()); //Akce
			dialogHandler.Add(TextFactory.CreateText("Action"));

			dialogHandler.MakeTableTransparent(); //zpruhledni nadpisovy radek

			//vlastni seznam lidi
            dialogHandler.Add(new GumpTable(ImprovedDialog.PAGE_ROWS, ButtonFactory.D_BUTTON_HEIGHT));
			dialogHandler.CopyColsFromLastTable();

			bool prevNextColumnAdded = false; //pridat/nepridat sloupecek pro navigacni sipky?
			if (firstiVal > 0) { //nejdeme od nulteho playera - jsme uz na dalsich strankach
				dialogHandler.AddLast(new GumpColumn(ButtonFactory.D_BUTTON_PREVNEXT_WIDTH));	
				prevNextColumnAdded = true; //"next" button uz nemusi vytvorit sloupecek 
                dialogHandler.Add(ButtonFactory.CreateButton(LeafComponentTypes.ButtonPrev, 4)); //prev
			}
			if(imax < playersList.Count) {//jeste bude dalsi stranka
				if(!prevNextColumnAdded) { //jeste nemame sloupecek na prevnext buttony, pridat ted
					dialogHandler.AddLast(new GumpColumn(ButtonFactory.D_BUTTON_PREVNEXT_WIDTH));
				}
                dialogHandler.Add(ButtonFactory.CreateButton(LeafComponentTypes.ButtonNext, 0, dialogHandler.LastColumn.Height - 21, 5)); //next
			}

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
				dialogHandler.AddToColumn(0, rowCntr, ButtonFactory.CreateButton(LeafComponentTypes.ButtonTick, (4 * i) + 10)); //player come
                dialogHandler.AddToColumn(1, rowCntr, ButtonFactory.CreateButton(LeafComponentTypes.ButtonPaper, (4 * i) + 11)); //account detail
				dialogHandler.AddToColumn(1, rowCntr, TextFactory.CreateText(ButtonFactory.D_BUTTON_WIDTH, 0, plrColor, plr.Account.Name)); //acc name
                dialogHandler.AddToColumn(2, rowCntr, ButtonFactory.CreateButton(LeafComponentTypes.ButtonPaper, (4 * i) + 12)); //player info
				dialogHandler.AddToColumn(2, rowCntr, TextFactory.CreateText(ButtonFactory.D_BUTTON_WIDTH, 0, plrColor, plr.Name)); //plr name
                dialogHandler.AddToColumn(3, rowCntr, ButtonFactory.CreateButton(LeafComponentTypes.ButtonTick, (4 * i) + 13)); //goto location
				dialogHandler.AddToColumn(3, rowCntr, TextFactory.CreateText(ButtonFactory.D_BUTTON_WIDTH, 0, plrColor, plr.Region.Name)); //region name
				dialogHandler.AddToColumn(4, rowCntr, TextFactory.CreateText(plrColor, plr.Action.ToString())); //region name
				
				rowCntr++;			
			}
			dialogHandler.MakeTableTransparent(); //zpruhledni zbytek dialogu

			//uložit info o právì vytvoøeném dialogu pro návrat
			DialogStackItem.EnstackDialog(src, focus, D_Admin.Instance,
					(SortingCriteria)sa[0], //typ tøídìní
					firstiVal); //cislo zpravy kterou zacina stranka (pro paging)	

			dialogHandler.WriteOut();
		}

		public override void OnResponse(GumpInstance gi, GumpResponse gr) {
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
                    case 4: //previous page
						dsi = DialogStackItem.PopStackedDialog(gi.Cont.Conn);//vem ulozene info o dialogu
						dsi.Args[1] = Convert.ToInt32(dsi.Args[1]) - ImprovedDialog.PAGE_ROWS; //uprav info o stránkování 						
						dsi.Show();
						break;
                    case 5: //next page
						dsi = DialogStackItem.PopStackedDialog(gi.Cont.Conn);//vem ulozene info o dialogu
						dsi.Args[1] = Convert.ToInt32(dsi.Args[1]) + ImprovedDialog.PAGE_ROWS; //uprav info o stránkování 						
						dsi.Show();
						break;
                    case 6: //acc tøídit desc
						dsi = DialogStackItem.PopStackedDialog(gi.Cont.Conn);//vem ulozene info o dialogu
						dsi.Args[0] = SortingCriteria.AccountDesc; //uprav info o sortovani
						dsi.Show();
						break;
                    case 7: //hráèi tøídit desc
						dsi = DialogStackItem.PopStackedDialog(gi.Cont.Conn);//vem ulozene info o dialogu
						dsi.Args[0] = SortingCriteria.NameDesc; //uprav info o sortovani
						dsi.Show();
						break;
                    case 8: //lokace tøídit desc
						dsi = DialogStackItem.PopStackedDialog(gi.Cont.Conn);//vem ulozene info o dialogu
						dsi.Args[0] = SortingCriteria.LocationDesc; //uprav info o sortovani
						dsi.Show();
						break;
                }
            } else { //skutecna adminovaci tlacitka z radku
                //seznam hracu bereme z kontextu (mohl byt jiz trideny atd)
                ArrayList playersList = (ArrayList)gi.GetTag(players);

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