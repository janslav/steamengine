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
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {
	[Remark("Dialog zobrazící seznam všech kategorií které v nastavení máme, umožní rozkliknout"+
			"a nastavit požadovanou kategorii (nebo taky všechny).")]
	public class D_Settings_Categories : CompiledGump {
		static readonly TagKey categoriesTag = TagKey.Get("categoriesTag");


		private static D_Settings_Categories instance;
		public static D_Settings_Categories Instance {
			get {
				return instance;
			}
		}

		public D_Settings_Categories() {
			instance = this;
		}

		public override void Construct(Thing focus, AbstractCharacter src, object[] args) {
			//pole obsahujici vsechny ketegorie pro zobrazeni
			SettingsCategory[] categories = StaticMemberSaver.GetMembersForSetting();

			this.GumpInstance.SetTag(categoriesTag, categories);
			int firstiVal = Convert.ToInt32(args[0]);   //prvni index na strance
			//maximalni index (20 radku mame) + hlidat konec seznamu...
			int imax = Math.Min(firstiVal + ImprovedDialog.PAGE_ROWS, categories.Length);

			ImprovedDialog dlg = new ImprovedDialog(GumpInstance);
			dlg.CreateBackground(300);
			dlg.SetLocation(100, 100);

			dlg.Add(new GUTATable(1, 0, ButtonFactory.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = TextFactory.CreateText("Kategorie pro nastavení (" + (firstiVal + 1) + "-" + imax + " z " + categories.Length + ")");
			dlg.LastTable[0, 1] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonCross, 0);
			dlg.MakeTableTransparent();

			dlg.Add(new GUTATable(1, ButtonFactory.D_BUTTON_WIDTH, 0));
							//èudlik pro zobrazeni
			dlg.LastTable[0, 0] = TextFactory.CreateText("Zobraz");
			dlg.LastTable[0, 1] = TextFactory.CreateText("Název kategorie");					
			dlg.MakeTableTransparent();

			//odkaz na "ALL"
			dlg.Add(new GUTATable(1));
			dlg.CopyColsFromLastTable();
			dlg.LastTable[0, 0] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonPaper, 1); //zobrazit
			dlg.LastTable[0, 1] = TextFactory.CreateText("Všechny");
			dlg.MakeTableTransparent();

			dlg.Add(new GUTATable(imax-firstiVal)); //jen tolik radku kolik kategorii je na strance (tj bud PAGE_ROWS anebo mene)
			dlg.CopyColsFromLastTable();

			//projet seznam v ramci daneho rozsahu indexu
			int rowCntr = 0;
			for(int i = firstiVal; i < imax; i++) {
				SettingsCategory cat = categories[i];
				dlg.LastTable[rowCntr, 0] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonPaper, i + 10); //zobrazit
				dlg.LastTable[rowCntr, 1] = TextFactory.CreateText(Hues.SettingsTitleColor, cat.Name); //název kategorie		
				rowCntr++;
			}
			dlg.MakeTableTransparent();

			//ted paging, klasika
			dlg.CreatePaging(categories.Length, firstiVal);

			dlg.WriteOut();
			
			//uložit info o právì vytvoøeném dialogu pro návrat
			DialogStackItem.EnstackDialog(src, focus, D_Settings_Categories.Instance,
					firstiVal);	//prvni index na strance
		}

		public override void OnResponse(GumpInstance gi, GumpResponse gr) {
			//seznam kategorii kontextu
			SettingsCategory[] categories = (SettingsCategory[])gi.GetTag(categoriesTag);
			if(gr.pressedButton < 10) { //zakladni tlacitka - end, zobraz vse 
				DialogStackItem dsi = null;
				switch(gr.pressedButton) {
					case 0: //exit
						DialogStackItem.PopStackedDialog(gi.Cont.Conn);	//odstranit ze stacku aktualni dialog
						DialogStackItem.ShowPreviousDialog(gi.Cont.Conn); //zobrazit pripadny predchozi dialog
						break;
					case 1: //zobraz vsechny kategorie	
							//params:	1 - prazdny, zacne od prvni kategorie
							//			2 - 0, zacne od prvniho membera dane kategorie
							//			3 - 0, zacne na nulte strance (jinak to ani nejde)
							//			4 - info o tom ze ma zobrazit vsechny kategorie pocinaje specifikovanou (zde tou prvni)
						gi.Cont.Dialog(D_Static_Settings.Instance, "", 0, 0, SettingsDisplay.All);
						break;					
				}
			} else if(ImprovedDialog.PagingButtonsHandled(gi, gr, 0, categories.Length)) {//kliknuto na paging? (0 = index parametru nesoucim info o pagingu (zde dsi.Args[0] viz výše)
				return;
			} else { //skutecna tlacitka z radku
				//zjistime kterej cudlik z radku byl zmacknut
				int row = (int)(gr.pressedButton - 10);//- cislo kategorie v jejich setridenem seznamu
				SettingsCategory cat = categories[row];
								//parametry stejny vyznam, zde zobrazime jen tu jednu kliknutou kategorii
				gi.Cont.Dialog(D_Static_Settings.Instance, cat.Name, 0, 0, SettingsDisplay.Single);				
			}
		}		
	}	
}