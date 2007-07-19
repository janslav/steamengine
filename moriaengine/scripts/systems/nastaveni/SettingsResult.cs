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
	[Remark("Dialog zobrazící výsledek po uplatnìní nastavení - vypíše seznam zmìnìných hodnot doplnìný"+
			"o pøípadné hodnoty které se zmìnit nepodaøilo")]
	public class D_Settings_Result : CompiledGump {
		static readonly TagKey setResultsTag = TagKey.Get("_setResultsTag_");


		private static D_Settings_Result instance;
		public static D_Settings_Result Instance {
			get {
				return instance;
			}
		}

		public D_Settings_Result() {
			instance = this;
		}

		public override void Construct(Thing focus, AbstractCharacter src, object[] args) {
			//pole obsahujici vysledky k zobrazeni
			Hashtable setResults = (Hashtable)args[1];
			List<SettingsValue> settingValues = GetDisplayedSettingValues(setResults); //pro iterování do výpisu dialogu
			//setridit dle nazvu zobrazovaneho itemu, jinak by nebylo zaruceno spolehlive strankovani
			settingValues.Sort(SettingsValuesComparer.Instance);

			this.GumpInstance.SetTag(setResultsTag, setResults);
			int firstiVal = Convert.ToInt32(args[0]);   //prvni index na strance
			//maximalni index (20 radku mame) + hlidat konec seznamu...
			int imax = Math.Min(firstiVal + ImprovedDialog.PAGE_ROWS, settingValues.Count);

			int resultsOK = CountSuccessfulSettings(setResults);
			int resultsNOK = CountUnSuccessfulSettings(setResults);
			int allFields = setResults.Count;

			ImprovedDialog dlg = new ImprovedDialog(GumpInstance);
			dlg.CreateBackground(700);
			dlg.SetLocation(100, 100);

			dlg.Add(new GUTATable(1, 0, ButtonFactory.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = TextFactory.CreateText("Výsledky nastavení (celkem:" + allFields + ", pøenastaveno: " + resultsOK + ", chybnì zadáno: " + resultsNOK + ")");
			dlg.LastTable[0, 1] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonCross, 0);
			dlg.MakeTableTransparent();

			dlg.Add(new GUTATable(1, 175, 175, 175, 0));			
			dlg.LastTable[0, 0] = TextFactory.CreateText("Název");//jméno položky nastavení
			dlg.LastTable[0, 1] = TextFactory.CreateText("Souèasná hodnota");//po nastavení
			dlg.LastTable[0, 2] = TextFactory.CreateText("Pùvodní hodnota");//vyplnìno pøi úspìšném nastavení
			dlg.LastTable[0, 3] = TextFactory.CreateText("Chybná hodnota");//vyplnìno pøi neúspìšném nastavení
			dlg.MakeTableTransparent();

			dlg.Add(new GUTATable(imax - firstiVal)); //jen tolik radku kolik kategorii je na strance (tj bud PAGE_ROWS anebo mene)
			dlg.CopyColsFromLastTable();

			//projet seznam v ramci daneho rozsahu indexu
			int rowCntr = 0;
			for(int i = firstiVal; i < imax; i++) {
				SettingsValue sval = settingValues[i];
				dlg.LastTable[rowCntr, 0] = TextFactory.CreateText(sval.Color, sval.FullPath()); //název položky nastavení
				dlg.LastTable[rowCntr, 1] = TextFactory.CreateText(sval.Color, ObjectSaver.Save(sval.Value)); //aktuální hodnota (buï je to ta pùvodní, nebo je to ta zmìnìná)
				dlg.LastTable[rowCntr, 2] = TextFactory.CreateText(sval.Color, sval.OldValue); //pùvodní hodnota (vyplnìno jen pokud tato položka byla zmìnìna)
														//pvodni hotnotu nezjistujeme tim "save" neb mohla byt prave spatna (tj by to opet spadlo)!
				dlg.LastTable[rowCntr, 3] = TextFactory.CreateText(sval.Color, sval.NewValue); //zamýšlená hodnota (vyplnìno pøi selhání - napø nekompatibilní datový typ atd.)
				rowCntr++;
			}
			dlg.MakeTableTransparent();

			//ted paging, klasika
			dlg.CreatePaging(settingValues.Count, firstiVal);

			dlg.WriteOut();

			//uložit info o právì vytvoøeném dialogu pro návrat
			DialogStackItem.EnstackDialog(src, focus, D_Settings_Result.Instance,
					firstiVal, setResults);	//prvni index na strance; vysledna sada
		}

		public override void OnResponse(GumpInstance gi, GumpResponse gr) {
			//seznam nastavenych nebo zkousenych polozek
			Hashtable setVals = (Hashtable)gi.GetTag(setResultsTag);
			if(gr.pressedButton == 0) { //end				
				DialogStackItem.PopStackedDialog(gi.Cont.Conn);	//odstranit ze stacku aktualni dialog

				//vycistime seznam od new a old valui - aby se polozky nezobrazovaly i pri pristim nastaveni
				foreach(SettingsValue sval in setVals.Values) {
					sval.OldValue = "";
					sval.NewValue = "";
				}
				//neobrazovat predchozi dialog, puvodni dialog nastaveni jiz nam sviti vespod
				//DialogStackItem.ShowPreviousDialog(gi.Cont.Conn); //zobrazit pripadny predchozi dialog						
			} else if(ImprovedDialog.PagingButtonsHandled(gi, gr, 0, setVals.Count)) {//kliknuto na paging? (0 = index parametru nesoucim info o pagingu (zde dsi.Args[0] viz výše)
				return;
			} 
		}

		[Remark("Spocita v seznamu nastavovanych input fieldu vsechny ktere byly uspesne"+
				"prenastaveny")]
		private int CountSuccessfulSettings(Hashtable results) {
			int resCntr = 0;
			foreach(SettingsValue sval in results.Values) {
				//zjisti, zda je ulozena puvodni hodnota (to se stane pri uspesnem prenastaveni)
				if(sval.OldValue != null && !sval.OldValue.Equals("")) {
					resCntr++;
				}
			}
			return resCntr;
		}

		[Remark("Spocita v seznamu nastavovanych input fieldu vsechny ktere byly neuspesne" +
				"prenastaveny")]
		private int CountUnSuccessfulSettings(Hashtable results) {
			int resCntr = 0;
			foreach(SettingsValue sval in results.Values) {
				//zjisti, zda je ulozena hodnota co jsme zkouseli nastavit (to se stane pri neuspesnem)
				if(sval.NewValue != null && !sval.NewValue.Equals("")) {
					resCntr++;
				}
			}
			return resCntr;
		}

		[Remark("Vybere ze seznamui vsech fieldu jen ty, ktere bud byly prenastaveny anebo ktere"+
				"byly nastavovany neuspesne")]
		private List<SettingsValue> GetDisplayedSettingValues(Hashtable results) {
			List<SettingsValue> retList = new List<SettingsValue>();
			foreach(SettingsValue sval in results.Values) {
				//zjisti, zda je ulozena hodnota co jsme zkouseli nastavit (to se stane pri neuspesnem)
				if((sval.NewValue != null && !sval.NewValue.Equals("")) || //bud byl neuspesne nastavovan
				   (sval.OldValue != null && !sval.OldValue.Equals(""))) { //nebo naopak uspesne prenastaven
					retList.Add(sval);					
				}
			}
			return retList;	
		}
	}

	[Remark("Komparator pro setrideni vysledku nastaveni podle abecedy. Singleton")]
	class SettingsValuesComparer : IComparer<SettingsValue> {
		internal static SettingsValuesComparer instance;

		public static SettingsValuesComparer Instance {
			get {
				if(instance == null) {
					instance = new SettingsValuesComparer();
				}
				return instance;				
			}
		}

		private SettingsValuesComparer() {
		}

		public int Compare(SettingsValue a, SettingsValue b) {
			return a.FullPath().CompareTo(b.FullPath());
		}
	}
}