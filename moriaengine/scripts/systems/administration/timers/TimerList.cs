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
using SteamEngine.Timers;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts.Dialogs {

	[Remark("Dialog listing all object(tagholder)'s timers")]
	public class D_TimerList : CompiledGump {
		private static int width = 500;
		private static int innerWidth = width - 2 * ImprovedDialog.D_BORDER - 2 * ImprovedDialog.D_SPACE;
		
		[Remark("Seznam parametru: 0 - thing jehoz timery zobrazujeme, " +
				"	1 - index ze seznamu timeru ktery bude na strance jako prvni" +
				"	2 - vyhledavaci kriterium pro jmena timeru(timerkeye)" +
				"	3 - ulozeny timerlist pro pripadnou navigaci v dialogu")]
		public override void Construct(Thing focus, AbstractCharacter sendTo, object[] sa) {
			//vzit seznam timeru z tagholdera (char nebo item) prisleho v parametru dialogu
			TagHolder th = (TagHolder)sa[0];
			List<KeyValuePair<TimerKey, BoundTimer>> timerList = null;
			if(sa[3] == null) {
				//vzit seznam timeru dle vyhledavaciho kriteria
				//toto se provede jen pri prvnim zobrazeni nebo zmene kriteria!
				timerList = ListifyTimers(th.GetAllTimers(), sa[2].ToString());
				timerList.Sort(TimersComparer.instance);
				sa[3] = timerList; //ulozime to do argumentu dialogu
			} else {
				//timerList si posilame v argumentu (napriklad pri pagingu)
				timerList = (List<KeyValuePair<TimerKey, BoundTimer>>) sa[3];
			}
			//zjistit zda bude paging, najit maximalni index na strance
			int firstiVal = Convert.ToInt32(sa[1]);   //prvni index na strance
			int imax = Math.Min(firstiVal + ImprovedDialog.PAGE_ROWS, timerList.Count);

			ImprovedDialog dlg = new ImprovedDialog(this.GumpInstance);
			//pozadi    
			dlg.CreateBackground(width);
			dlg.SetLocation(50, 50);

			//nadpis
			dlg.AddTable(new GUTATable(1, innerWidth - 2 * ButtonFactory.D_BUTTON_WIDTH - ImprovedDialog.D_COL_SPACE, 0, ButtonFactory.D_BUTTON_WIDTH));
			//dlg.AddTable(new GUTATable(1, 0, ButtonFactory.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = TextFactory.CreateHeadline("Seznam všech timerù na " + th.ToString() + " (zobrazeno " + (firstiVal + 1) + "-" + imax + " z " + timerList.Count + ")");
			dlg.LastTable[0, 1] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonSend, 3);//cudlik na refresh dialogu
			dlg.LastTable[0, 2] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonCross, 0);//cudlik na zavreni dialogu
			dlg.MakeTableTransparent();

			//cudlik a input field na zuzeni vyberu
			dlg.AddTable(new GUTATable(1, 130, 0, ButtonFactory.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = TextFactory.CreateLabel("Vyhledávací kriterium");
			dlg.LastTable[0, 1] = InputFactory.CreateInput(LeafComponentTypes.InputText, 33);
			dlg.LastTable[0, 2] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonPaper, 1);
			dlg.MakeTableTransparent();

			//cudlik na zalozeni noveho timeru
			dlg.AddTable(new GUTATable(1, 130, ButtonFactory.D_BUTTON_WIDTH, 0));
			dlg.LastTable[0, 0] = TextFactory.CreateLabel("Založit nový timer");
			dlg.LastTable[0, 1] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonOK, 2);
			dlg.MakeTableTransparent();

			//popis sloupcu
			dlg.AddTable(new GUTATable(1, ButtonFactory.D_BUTTON_WIDTH, 200, 0, ButtonFactory.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = TextFactory.CreateLabel("Zruš");
			dlg.LastTable[0, 1] = TextFactory.CreateLabel("Jméno timeru");
			dlg.LastTable[0, 2] = TextFactory.CreateLabel("Zbývající èas");
			dlg.LastTable[0, 3] = TextFactory.CreateLabel("Uprav");
			dlg.MakeTableTransparent();

			//seznam timeru
			dlg.AddTable(new GUTATable(imax - firstiVal));
			dlg.CopyColsFromLastTable();

			//projet seznam v ramci daneho rozsahu indexu
			int rowCntr = 0;
			for(int i = firstiVal; i < imax; i++) {
				KeyValuePair<TimerKey, BoundTimer> de = timerList[i];
				BoundTimer tmr = de.Value;

				dlg.LastTable[rowCntr, 0] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonCross, (2*i)+10);
				dlg.LastTable[rowCntr, 1] = TextFactory.CreateText((de.Key).name);
				dlg.LastTable[rowCntr, 2] = TextFactory.CreateText(tmr.DueInSeconds.ToString()); //hodnota tagu, vcetne prefixu oznacujicim typ
				dlg.LastTable[rowCntr, 3] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonTick, (2*i)+11);

				rowCntr++;
			}
			dlg.MakeTableTransparent(); //zpruhledni zbytek dialogu

			//now handle the paging 
			dlg.CreatePaging(timerList.Count, firstiVal, 1);

			dlg.WriteOut();
		}

		public override void OnResponse(GumpInstance gi, GumpResponse gr, object[] args) {
			//seznam timeru bereme z parametru (mohl byt jiz trideny atd, nebudeme ho proto selectit znova)
			List<KeyValuePair<TimerKey, BoundTimer>> timerList = (List<KeyValuePair<TimerKey, BoundTimer>>) args[3];
			int firstOnPage = Convert.ToInt32(args[1]);
			if(gr.pressedButton < 10) { //ovladaci tlacitka (exit, new, vyhledej)				
				switch(gr.pressedButton) {
					case 0: //exit
						DialogStacking.ShowPreviousDialog(gi); //zobrazit pripadny predchozi dialog
						break;
					case 1: //vyhledat dle zadani
						string nameCriteria = gr.GetTextResponse(33);
						args[1] = 0; //zrusit info o prvnich indexech - seznam se cely zmeni tim kriteriem						
						args[2] = nameCriteria; //uloz info o vyhledavacim kriteriu
						args[3] = null; //vycistit soucasny odkaz na taglist aby se mohl prenacist
						DialogStacking.ResendAndRestackDialog(gi);
						break;					
					case 2: //zalozit novy timer - TOTO ZATIM DELAT NEBUDEME
						DialogStacking.ResendAndRestackDialog(gi);
						//DialogStackItem.EnstackDialog(gi.Cont, dsi); //vlozime napred dialog zpet do stacku
						//gi.Cont.Dialog(D_NewTimer.Instance, dsi.Args[0]); //posleme si parametr toho typka na nemz bude novy timer vytvoren
						break;
					case 3: //refresh
						DialogStacking.ResendAndRestackDialog(gi);
						break;
				}
			} else if(ImprovedDialog.PagingButtonsHandled(gi, gr, 1, timerList.Count, 1)) {//kliknuto na paging? (1 = index parametru nesoucim info o pagingu (zde dsi.Args[1] viz výše)
				//druhá 1 - dialog ma jen jeden sloupecek s hodnotama na okno (napriklad colors dialog jich ma daleko vic)
				return;
			} else {
				//zjistime kterej cudlik z radku byl zmacknut
				int row = (int)(gr.pressedButton - 10) / 2;
				int buttNum = (int)(gr.pressedButton - 10) % 2;
				KeyValuePair<TimerKey, BoundTimer> de = ((List<KeyValuePair<TimerKey, BoundTimer>>) args[3])[row];
				switch(buttNum) {
					case 0: //smazat timer
						TagHolder timerOwner = (TagHolder)args[0];
						timerOwner.RemoveTimer(de.Key);
						//na zaver smazat timerlist (musi se reloadnout)
						args[3] = null;
						DialogStacking.ResendAndRestackDialog(gi);
						break;
					case 1: //upravit timer
						GumpInstance newGi = gi.Cont.Dialog(SingletonScript<D_EditTimer>.Instance, args[0], de.Value); //posleme si parametr toho typka na nemz editujeme timer a taky timer sam
						//uložit info o dialogu pro návrat						
						DialogStacking.EnstackDialog(gi, newGi);
						break;
				}
			}
		}

		[Remark("Retreives the list of all timers the given TagHolder has")]
		private List<KeyValuePair<TimerKey, BoundTimer>> ListifyTimers(IEnumerable<KeyValuePair<TimerKey, BoundTimer>> tags, string criteria) {
			List<KeyValuePair<TimerKey, BoundTimer>> timersList = new List<KeyValuePair<TimerKey, BoundTimer>>();
			foreach (KeyValuePair<TimerKey, BoundTimer> entry in tags) {
				//entry in this hashtable is TimerKey and its Timer value
				if(criteria.Equals("")) {
					timersList.Add(entry);//bereme vse
				} else if (entry.Key.name.ToUpper().Contains(criteria.ToUpper())) {
					timersList.Add(entry);//jinak jen v pripade ze kriterium se vyskytuje v nazvu timeru
				}
			}
			return timersList;
		}

		[Remark("Display a timers ist. Function accessible from the game." +
				"The function is designed to be triggered using .x TimersList(criteria)" +
			    "but it can be used also normally .TimerList(criteria) to display runner's own timers")]
		[SteamFunction]
		public static void TimerList(TagHolder self, ScriptArgs text) {
			//zavolat dialog, 
			//parametr self - thing jehoz tagy chceme zobrazit
			//0 - zacneme od prvniho tagu co ma
			//treti parametr vyhledavani dle parametru, if any...
			//ctvrty parametr = volny jeden prvek pole pro seznam timeru, pouzito az v dialogu
			if(text == null || text.argv == null || text.argv.Length == 0) {
				Globals.SrcCharacter.Dialog(SingletonScript<D_TimerList>.Instance, self, 0, "", null);
			} else {
				Globals.SrcCharacter.Dialog(SingletonScript<D_TimerList>.Instance, self, 0, text.Args, null);
			}
		}
	}

	[Remark("Comparer for sorting timer dictionary entries by timers TimerKeys asc")]
	public class TimersComparer : IComparer<KeyValuePair<TimerKey, BoundTimer>> {
		public readonly static TimersComparer instance = new TimersComparer();

		private TimersComparer() {
			//soukromy konstruktor, pristupovat budeme pres instanci
		}

		//we have to make sure that we are sorting a list of DictionaryEntries which are tags
		//otherwise this will crash on some ClassCastException -)
		public int Compare(KeyValuePair<TimerKey, BoundTimer> x, KeyValuePair<TimerKey, BoundTimer> y) {
			TimerKey a = (x.Key);
			TimerKey b = (y.Key);
			return string.Compare(a.name, b.name);
		}
	}
}