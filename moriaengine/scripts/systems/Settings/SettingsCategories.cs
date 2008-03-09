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

namespace SteamEngine.CompiledScripts.Dialogs {
	[Remark("Dialog zobrazící seznam všech kategorií které v nastavení máme, umožní rozkliknout"+
			"a nastavit požadovanou kategorii (nebo taky všechny).")]
	public class D_Settings_Categories : CompiledGump {
        internal static readonly TagKey settingsCatTK = TagKey.Get("__settings_categories_");

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			//pole obsahujici vsechny ketegorie pro zobrazeni
			SettingsCategory[] categories = StaticMemberSaver.GetMembersForSetting();

            args.SetTag(D_Settings_Categories.settingsCatTK, categories);
			int firstiVal = Convert.ToInt32(args.GetTag(ImprovedDialog.pagingIndexTK));   //prvni index na strance
			//maximalni index (20 radku mame) + hlidat konec seznamu...
			int imax = Math.Min(firstiVal + ImprovedDialog.PAGE_ROWS, categories.Length);

			ImprovedDialog dlg = new ImprovedDialog(GumpInstance);
			dlg.CreateBackground(300);
			dlg.SetLocation(100, 100);

			dlg.AddTable(new GUTATable(1, 0, ButtonFactory.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = TextFactory.CreateHeadline("Kategorie pro nastavení (" + (firstiVal + 1) + "-" + imax + " z " + categories.Length + ")");
			dlg.LastTable[0, 1] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonCross, 0);
			dlg.MakeLastTableTransparent();

			dlg.AddTable(new GUTATable(1, ButtonFactory.D_BUTTON_WIDTH, 0));
							//èudlik pro zobrazeni
			dlg.LastTable[0, 0] = TextFactory.CreateLabel("Zobraz");
			dlg.LastTable[0, 1] = TextFactory.CreateLabel("Název kategorie");					
			dlg.MakeLastTableTransparent();

			//odkaz na "ALL"
			dlg.AddTable(new GUTATable(1));
			dlg.CopyColsFromLastTable();
			dlg.LastTable[0, 0] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonPaper, 1); //zobrazit
			dlg.LastTable[0, 1] = TextFactory.CreateLabel("Všechny");
			dlg.MakeLastTableTransparent();

			dlg.AddTable(new GUTATable(imax-firstiVal)); //jen tolik radku kolik kategorii je na strance (tj bud PAGE_ROWS anebo mene)
			dlg.CopyColsFromLastTable();

			//projet seznam v ramci daneho rozsahu indexu
			int rowCntr = 0;
			for(int i = firstiVal; i < imax; i++) {
				SettingsCategory cat = categories[i];
				dlg.LastTable[rowCntr, 0] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonPaper, i + 10); //zobrazit
				dlg.LastTable[rowCntr, 1] = TextFactory.CreateText(Hues.SettingsTitleColor, cat.Name); //název kategorie		
				rowCntr++;
			}
			dlg.MakeLastTableTransparent();

			//ted paging, klasika
			dlg.CreatePaging(categories.Length, firstiVal,1);

			dlg.WriteOut();
		}

		public override void OnResponse(GumpInstance gi, GumpResponse gr, DialogArgs args) {
			//seznam kategorii kontextu
			SettingsCategory[] categories = (SettingsCategory[])args.GetTag(D_Settings_Categories.settingsCatTK);
			if(gr.pressedButton < 10) { //zakladni tlacitka - end, zobraz vse 
				switch(gr.pressedButton) {
					case 0: //exit
						DialogStacking.ShowPreviousDialog(gi); //zobrazit pripadny predchozi dialog
						break;
					case 1: //zobraz vsechny kategorie	
						//uložit info o dialogu pro návrat
							//params:	1 - prazdny, zacne od prvni kategorie
							//			2 - 0, zacne od prvniho membera dane kategorie
							//			3 - 0, zacne na nulte strance (jinak to ani nejde)
							//			4 - info o tom ze ma zobrazit vsechny kategorie pocinaje specifikovanou (zde tou prvni)
							//			5,6 - vnitrodialogove potreby
                        DialogArgs newArgs = new DialogArgs();
                        newArgs.SetTag(D_Static_Settings.settingsDisplayTypeTK, SettingsDisplay.All);
						GumpInstance newGi = gi.Cont.Dialog(SingletonScript<D_Static_Settings>.Instance, "", 0, 0, SettingsDisplay.All, null, null);
						DialogStacking.EnstackDialog(gi, newGi);						
						break;					
				}
			} else if(ImprovedDialog.PagingButtonsHandled(gi, gr, categories.Length,1)) {//kliknuto na paging?
				//1 sloupecek
				return;
			} else { //skutecna tlacitka z radku
				//zjistime kterej cudlik z radku byl zmacknut
				int row = (int)(gr.pressedButton - 10);//- cislo kategorie v jejich setridenem seznamu
				SettingsCategory cat = categories[row];
								//parametry stejny vyznam, zde zobrazime jen tu jednu kliknutou kategorii
                DialogArgs newArgs = new DialogArgs();
                newArgs.SetTag(D_Static_Settings.settingsDisplayTypeTK, SettingsDisplay.Single); //budem zobrazovat jen jednu specifikovanou kategorii
                newArgs.SetTag(D_Static_Settings.settingsFirstCategoryTKs, cat.Name); //mame konkretni kategorii k zobrazeni jako prvni	(to bude ta jedina)
				GumpInstance newGi = gi.Cont.Dialog(SingletonScript<D_Static_Settings>.Instance, cat.Name, 0, 0, SettingsDisplay.Single, null, null);
				DialogStacking.EnstackDialog(gi, newGi);
			}
		}		
	}	
}