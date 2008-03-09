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
	[Remark("Dialog pro nastavení všech promìnných majících atribut SavedMember a jsou zároveò urèeny"+
			"pro dynamické nastavování.")]
	public class D_Static_Settings : CompiledGump {
		private static int width = 900;
		public static int innerWidth = width - 2 * ImprovedDialog.D_BORDER - 2 * ImprovedDialog.D_SPACE;
		public static int ITEM_INDENT = 20; //pocet pixelu o ktere budou popisy s inputfieldem odsazeny od kraje
		public static int INPUT_INDENT = 75; //pocet pixelu o ktery bude odsazen inputfield od zacatku textoveho popisku (tj zbyde i misto na vlastni text)

		//tohle bude tag urcujici hashtabulku s informaci o klicich kategorii 
		//pro tu kterou stranku - tj ktery klic bude na ktere strance prvni
		//je to nahrazka za klasicky radkovy paging protoze tady pocty radku nesledujeme
		private static readonly TagKey lastKeyTK = TagKey.Get("__last_key_");
		private static readonly TagKey lastIndexTK = TagKey.Get("__last_index_");
		private static readonly TagKey categoriesListTK = TagKey.Get("__categories_list_");
        private static readonly TagKey actualPageTK = TagKey.Get("__actual_page_");
        internal static readonly TagKey settingsDisplayTypeTK = TagKey.Get("__settings_type_"); //all nebo jeno categories...
        internal static readonly TagKey settingsFirstCategoryTK = TagKey.Get("__displayed_category_");//jak bude prvni zobrazena kategorie na strance
        internal static readonly TagKey categorysFirstIndexTK = TagKey.Get("__categorys_first_index_");//index itemu z kategorie kterej bude zobrazen jako prvni (v kategorii nemusime nutne zacinat od 0)
        private static readonly TagKey keytableTK = TagKey.Get("__key_table_");
       
		private int rowCounter; //pocitadlo radku pro konstrukci dialogu
		private int dlgIndex; //indexování inputfieldù v dialogu
		private int filledColumn; //pocitadlo sloupecku v dialogu, indikuje kam se ma davat SettingsValues a ketegorie

		public int RowCounter {
			get {
				return rowCounter;
			}
			set {
				rowCounter = value;
			}
		}
		public int DlgIndex {
			get {
				return dlgIndex;
			}
			set {
				dlgIndex = value;
			}
		}
		public int FilledColumn {
			get {
				return filledColumn;
			}
			set {
				filledColumn = value;
			}
		}

		[Remark("Hashtabulka pro uchovávání nastavovaných memberù v návaznosti na poøadí jejich "+
				"inputu v dialogu.")]
		internal Hashtable valuesToSet;

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			//pole obsahujici vsechny ketegorie pro zobrazeni
			SettingsCategory[] categories = StaticMemberSaver.GetMembersForSetting();

			ImprovedDialog dlg = new ImprovedDialog(GumpInstance);
			dlg.CreateBackground(width);
			dlg.SetLocation(0, 30);

			dlg.AddTable(new GUTATable(1, innerWidth - 2 * ButtonFactory.D_BUTTON_WIDTH - ImprovedDialog.D_COL_SPACE, 0, ButtonFactory.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = TextFactory.CreateHeadline("Nastavení globálních promìnných. Pro informace zmáèkni tlaèítko s papírem vpravo v rohu.");			
			dlg.LastTable[0, 1] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonPaper, 2);
			dlg.LastTable[0, 2] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonCross, 0);
			dlg.MakeLastTableTransparent();

			dlg.AddTable(new GUTATable(ImprovedDialog.PAGE_ROWS, innerWidth / 4, innerWidth / 4, 0, innerWidth / 4));//bude to ve 4 sloupcích
			dlg.MakeLastTableTransparent();
			rowCounter = 0;
			dlgIndex = 0; //indexování inputfieldù v dialogu
            string catKey = ""; //defaultne nespscifikovana kategorie
            if (args.HasTag(D_Static_Settings.settingsFirstCategoryTK)) {
                //mame-li prvni kategorii udanou, vezmeme ji
                catKey = (string)args.GetTag(D_Static_Settings.settingsFirstCategoryTK); //klic vybrane kategorie (na tretim argumentu pak zavisi zda zobrazi jen tuto kategorii nebo vsechny abecedne od ni(vcetne) dal)
            }
            int listIndex = 0;
            if (args.HasTag(D_Static_Settings.categorysFirstIndexTK)) {
                listIndex = Convert.ToInt32(args.GetTag(D_Static_Settings.categorysFirstIndexTK));//index itemu kategorie ktery bude prvni
            }
			int actualPage = 0;
            if (args.HasTag(D_Static_Settings.actualPageTK)) {
                //mame-li, vezmeme cislo stranky z tagu
                actualPage = (int)args.GetTag(D_Static_Settings.actualPageTK);
            } 
			SettingsDisplay dspl = (SettingsDisplay)args.GetTag(D_Static_Settings.settingsDisplayTypeTK); //All nebo Single - zobrazene kategorie

			//pozice parametru:
			//	0. nazev kategorie ktera ma byt zobrazena ("" - od zacatku)
			//	1. kolikaty member z teto kategorie bude prvni na strance - 0 pro prvni
			//	2. (interni)cislo stranky odkud se zacina (pouze pro interni ucely, volat vzdy s nulou!)
			//	3. zobrazovat vsechny dalsi kategorie pocinaje abecedne zvolenou v parametru 0. nebo zobrazovat jen tuto jednu?
			//	4. (interni)hashtabulka se SettingsValue tridami (input fieldy), klicem jsou indexy techto fieldu v dialogu
			//	5. (interni)info o pagingu a navratovych hodnotach
			//pri prnvim zobrazeni dialogu mame jen prvni ctyri parametry
			//paty parametr se nastavuje pouze v pripade ze dialog zobrazujeme pote co doslo k "nastaveni" (umozni nam to 
			//	sledovat nastavovane hodnoty a zjistit uspech ci neuspech pri nastaveni
			//sesty parametr se nastavuje v pripade ze je potreba uplatnit paging
            if (args.HasTag(D_Settings_Result.resultsListTK)) {
                //mame-li ho, vezmeme ho	
                valuesToSet = (Hashtable)args.GetTag(D_Settings_Result.resultsListTK);
            } else {
                valuesToSet = new Hashtable(); //vycistime tabulku inputfieldu ted						
            }
			
			//par indikatoru pro layout - pocitadlo sloupecku a informace o tom kam se ma vypisovat
			filledColumn = 0;
			bool switchToNextColumn = false, switchToNextPage = false;
			bool useListIndex = true;
			String actualKey = "";
			int idx = 0; //index pro listovani ve vnitrnich seznamech kazde kategorie

			List<SettingsCategory> selectedCats = GetRelevantCategories(categories, catKey, dspl);

			args.SetTag(D_Static_Settings.categoriesListTK, selectedCats);//ulozime seznam kategorii pro potreby vycisteni

			//ted je potreba projit pole kategorii a udelat dialog z hodnot v nem ulozenejch
			//bereme klice od posledne pouziteho dal (vcetne, protoze mame take specifikovany index 
			//do pole SettingsValues teto kategorie, takze budeme pokracovat v tom poli)
			foreach(SettingsCategory cat in selectedCats) {
				actualKey = cat.Name; //pamatuj naposled zobrazovanou kategorii
				idx = 0; //vynulujeme pro kazdy list
				if(useListIndex) {//pouzijeme promennou 'listIndex' ktera prisla v parametrech?
					useListIndex = false;
					idx = listIndex;
					//pokud ne, tak jsme jiz pouzili listIndex pro prvni pristupovany list 
					//(ten ve kterem jako prvnim pokracujeme na nove strance,
					//kazdy dalsi kategorijni list uz musime prochazet od zacatku !)
					if(idx == 0) {
						//zaciname od zacatku seznamu memeberu kategorie - musime vypsat jeji nazev
						//toto se stane napriklad pri prvnim zobrazeni stranky
						//pokud bychom nahodou prechazeli na jinou stranku a stejne zde byla 0 tak tim lip,
						//aspon ta kategorie bude znovu nadepsana !
						cat.WriteLabel(dlg);
					}
				} else {
					//nezaciname "zprostredka" kategorie => musime napred zobrazit jeji nazev
					cat.WriteLabel(dlg);
				}
				//a nyni membery
				for(; idx < cat.Members.Length; idx++) {
					AbstractSetting absSet = cat.Members[idx];
					//napred zkontrolujeme, zda jsme jiz neprekrocili aktualni pocet radku
					//(to se mohlo stat napr pokud predchozi sloupec byl nucene prodlouzen kvuli 
					//komplexnimu datovemu typu)						
					if(rowCounter >= ImprovedDialog.PAGE_ROWS) {
						switchToNextColumn = true;//novy sloupecek
						switchToNextPage = (filledColumn == 3);//nova stranka
						idx--;//tohoto membera si dame az v dalsim sloupecku/strance							
					} else {
						//vypiseme (to se stara jak o jednoduche SettingsValues tak o SettingsCategories) 
						//tj pripadne subkategorie
						absSet.WriteSetting(dlg);
						//zkontrolujeme nyni stav radku
						if(rowCounter >= dlg.LastTable.RowCount) {
							//sloupecek je uz plnej, zacni vypisovat seznam do dalsiho
							switchToNextColumn = true;//novy sloupecek
							switchToNextPage = (filledColumn == 3);//nova stranka

							//dorovnat rozdil v poctu radek
							//to se muze stat u komplexnich typu, kde je nechceme trhat na vice sloupcu nebo stran
							dlg.LastTable.RowCount += (rowCounter - dlg.LastTable.RowCount);
						}
					}
					//nyni zkontrolujeme zda prechazime na novou stránku...
					if(switchToNextPage) {
						CreateSettingsPaging(dlg, actualPage, false);
						switchToNextPage = false;
						rowCounter = 0;
						goto WriteOut; //rovnou vypiseme tuto stranku
					} else if(switchToNextColumn) {//...nebo do dalsiho sloupeèku
						switchToNextColumn = false;
						filledColumn++;
						rowCounter = 0;
					}
				}
			}
			if(actualPage > 0) {
				//pridame navigaci o stranu zpet
				CreateSettingsPaging(dlg, actualPage, true);
			}

			WriteOut:
			//dalsi oddeleny radek s cudlikem k odeslani
			dlg.AddTable(new GUTATable(1, ButtonFactory.D_BUTTON_WIDTH, 0));
			dlg.LastTable[0, 0] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonOK, 1);
			dlg.LastTable[0, 1] = TextFactory.CreateText("Nastavit");
			dlg.MakeLastTableTransparent();
			dlg.WriteOut();

			//ulozit informaci o tom ktery klic a index seznamu v kategorii byl na teto strance prvni
			StoreKeyIndexInfo(catKey, listIndex, actualPage, args);
			//ulozit informaci o posledni kategorii a poslednim indexu v ni
			args.SetTag(D_Static_Settings.lastKeyTK, actualKey);//tato kategorie bude prvni na dalsi strance
			args.SetTag(D_Static_Settings.lastIndexTK, idx + 1);//toto bude index prvniho membera z vyse uvedene kategorie ktery se zobrazi
		}

		//args:
		//0 - prvni klic skupiny promennych co bude na strance
		//1 - kolikaty saved member ze skupiny bude zobrazen jako prvni
		//2 - cislo stranky
		//3	- haash tabulka se všemi hodnotami a indexy v dialogu 
		public override void OnResponse(GumpInstance gi, GumpResponse gr, DialogArgs args) {
			if(gr.pressedButton == 0) { //exit button
				//vezmem seznam zobrazenych kategorii, projdeme ho a vsechny membery vycistime
				List<SettingsCategory> catlist = (List<SettingsCategory>)args.GetTag(D_Static_Settings.categoriesListTK);
				foreach(SettingsCategory sCat in catlist) {
					sCat.ClearSettingValues();					
				}
				DialogStacking.ShowPreviousDialog(gi); //zobrazit pripadny predchozi dialog
			}
			//navigacni buttony (paging)
			if(gr.pressedButton == ImprovedDialog.ID_PREV_BUTTON) {
                args.SetTag(D_Static_Settings.actualPageTK, Convert.ToInt32(args.GetTag(D_Static_Settings.actualPageTK)) - 1);//strankovani o krok zpet
                KeyIndexPair kip = loadKeyIndexInfo(Convert.ToInt32(args.GetTag(D_Static_Settings.actualPageTK)), args);
									//(KeyIndexPair)keytable[Convert.ToInt32(dsi.Args[2])];
                args.SetTag(D_Static_Settings.settingsFirstCategoryTK, kip.Key); //updatnout info o prvni kategorii na predchozi strance				
                args.SetTag(D_Static_Settings.categorysFirstIndexTK, kip.Index);//ve vybrane prvni kategorii zacneme zobrazovat od tohoto clena
				DialogStacking.ResendAndRestackDialog(gi);
			} else if(gr.pressedButton == ImprovedDialog.ID_NEXT_BUTTON) {
                args.SetTag(D_Static_Settings.actualPageTK, Convert.ToInt32(args.GetTag(D_Static_Settings.actualPageTK)) + 1);
                args.SetTag(D_Static_Settings.settingsFirstCategoryTK, (string)args.GetTag(D_Static_Settings.lastKeyTK));//updatnout info o prvni kategorii				
                args.SetTag(D_Static_Settings.categorysFirstIndexTK, Convert.ToInt32(args.GetTag(D_Static_Settings.lastIndexTK)));//v prvni kategorii zacneme zobrazovat od tohoto clena
				DialogStacking.ResendAndRestackDialog(gi);
			} else if(gr.pressedButton == 1) { //nastaveni
				//napred vycistime vsechny mozne predchozi neuspechy v nastaveni - nyni totiz jedem znova
				List<SettingsCategory> catlist = (List<SettingsCategory>)args.GetTag(D_Static_Settings.categoriesListTK);
				foreach(SettingsCategory sCat in catlist) {
					sCat.ClearSettingValues();
				}

				TryMakeSetting(valuesToSet,gr);
				args.SetTag(D_Settings_Result.resultsListTK, valuesToSet); //predame si kolekci hodnot v dialogu pro pozdejsi pripadny navrat
				DialogStacking.ResendAndRestackDialog(gi);
				//a zobrazime take dialog s vysledky (null = volne misto pro seznamy resultu v nasledujicim dialogu)
                DialogArgs newArgs = new DialogArgs();
                newArgs.SetTag(D_Settings_Result.resultsListTK, valuesToSet);
                newArgs.SetTag(ImprovedDialog.pagingIndexTK, 0);
				gi.Cont.Dialog(SingletonScript<D_Settings_Result>.Instance, newArgs);
			} else if(gr.pressedButton == 2) { //info
				//stackneme se pro navrat
				GumpInstance newGi = gi.Cont.Dialog(SingletonScript<D_Settings_Help>.Instance);
				DialogStacking.EnstackDialog(gi, newGi);				
			}
		}

		[Remark("Specificky zpusob vytvareni pagingu pro nastavovaci dialogy - nebudeme pouzivat ten "+ 
				"klasickej zpusob protoze neni jendoduchy zjistit kolik polozek je vlastne na strance, "+ 
				"stranka nemusi mit nutne konstantni pocet radku. => vynechame ciselnou navigaci "+
				"Predavame si jako parametr posledni klic z naseho sortedslovniku se SavedMemberama a "+
				"posledne pouzity index do listu MemberInfo trid pro dany klic. Take posilame cislo stranky "+
				"(to jen abychom vedeli jak udelat navigacni sloupecek)")]
		private void CreateSettingsPaging(ImprovedDialog dlg, int actualPage, bool onlyBack) {
			bool prevNextColumnAdded = false; //indikator navigacniho sloupecku
			if(actualPage > 0) {
				prevNextColumnAdded = true; //navigacni sloupecek uz bude existovat
				dlg.AddLastColumn(new GUTAColumn(ButtonFactory.D_BUTTON_PREVNEXT_WIDTH));
				//button pro predchozi stranku
				dlg.LastColumn.AddComponent(ButtonFactory.CreateButton(LeafComponentTypes.ButtonPrev, ImprovedDialog.ID_PREV_BUTTON)); //prev
			}
			if(!onlyBack) {
				//ne chceme jen tlacitko zpet (to se stane napr kdyz na strance bude jen 
				//nekolik itemu takze uz nebude zadna dalsi strana ale potrebujeme se vracet)
				if(!prevNextColumnAdded) { //navigacni sloupecek jeste nebyl zalozen - jsme na prvni strance teprve.
					dlg.AddLastColumn(new GUTAColumn(ButtonFactory.D_BUTTON_PREVNEXT_WIDTH));
				}
				//a button pro dalsi stranku
				dlg.LastColumn.AddComponent(ButtonFactory.CreateButton(LeafComponentTypes.ButtonNext, 0, dlg.LastColumn.Height - 21, ImprovedDialog.ID_NEXT_BUTTON)); //next
			}
		}

		[Remark("Vybere z pole kategorii jen ty, ktere zacina danym klicem a dale (jsou tridene) - tj podmnozinu"+
				"anebo jen tu jednu jedinou, danou klicem - zalezi na parametru dispType")]
		private List<SettingsCategory> GetRelevantCategories(SettingsCategory[] catarray, string keyFrom, SettingsDisplay dispType) {
			List<SettingsCategory> retList = new List<SettingsCategory>();
			//napred najdeme cislo indexu kde se nachazi nas keyFrom
			int strIdx = 0;
			if(!keyFrom.Equals("")) {//je-li keyFrom prazdny, pak zaciname od zacatku normalka
				for(int i = 0; i < catarray.Length; i++) {
					string key = catarray[i].Name;
					if(key.ToUpper().Equals(keyFrom.ToUpper())) {//velka/mala pismena nas nezajimaji
						break;
					}
					strIdx++;
				}
			}
			if(strIdx >= catarray.Length) { //presahli jsme pocet kategorii (=hledanou jsme nenasli)
				strIdx = 0; //zacneme od pocatecni !
			}
			if(dispType == SettingsDisplay.All) {
				//od daneho indexu bereme vsechno
				for(int i = strIdx; i < catarray.Length; i++) {
					retList.Add(catarray[i]);
				}
			} else if(dispType == SettingsDisplay.Single) {
				retList.Add(catarray[strIdx]); //pridame jen tu jednu
			}	
			return retList;
		}

		[Remark("Metoda pro ukladani informace pro paging. Do hashtabulky ulozime cislo stranky"+
				"a k nemu klic urcujici kategorii nastaveni, ktera bude na dane strance zobrazena"+ 
				"jako prvni. Nahrazujeme tak klasicky radkovy paging kde se posouvame po strankach"+
				"o urcity pocet radku v seznamu zobrazovanych hodnot (to zde nelze).")]
		private void StoreKeyIndexInfo(string firstKey, int firstIndex, int actualPage, DialogArgs args) {
			Hashtable keytable = null;
			if(!args.HasTag(D_Static_Settings.keytableTK)) { //jeste ji tam nemame
				keytable = new Hashtable();
				args.SetTag(D_Static_Settings.keytableTK,keytable);
			} else {
				keytable = (Hashtable)args.GetTag(D_Static_Settings.keytableTK);
			}			
			keytable[actualPage] = new KeyIndexPair(firstKey, firstIndex); //na teto strance bude prvni tento klic
		}

		[Remark("Metoda pro ziskani klice kategorie ktera ma byt na dane strance zobrazena jako prvni "+
				"(vezme hodnotu ulozenou v tabulce nebo prazdny retezec pokud jde o prvni stranku)")]
		private KeyIndexPair loadKeyIndexInfo(int actualPage, object[] args) {
			Hashtable keytable = (Hashtable)args[args.Length - 1];
			return (keytable[actualPage] == null ? KeyIndexPair.deflt : (KeyIndexPair)keytable[actualPage]);
		}

		[Remark("Projdeme seznam vsech input fieldu v dialogu, precteme jejich hodnoty a "+
				"porovname s hodnotami v ulozenem seznamu SettingsValues instanci."+
				"Nove hodnoty rovnou nastavime, pokud to pujde. Pokud to nepujde tak se pripravi"+
				"error flag pro zvyrazneni chybnych hodnot v znovuzobrazenem dialogu.")]
		private void TryMakeSetting(Hashtable values, GumpResponse gr) {
			SettingsValue sval = null;
			string response = "";
			foreach(int index in values.Keys) {				
				sval = (SettingsValue)values[index];
				response = gr.GetTextResponse(index);
				//zkusime nastavit - logika nastaveni je v tride SettingsValue
				sval.TrySet(response);
			}

		}
	}

	[Remark("Trida pro uchovani informace pro paging - klic prvni zobrazovane kategorie "+
			"a index prvniho membera z teto kategorie ktery se na strance objevi")]
	class KeyIndexPair {
		string key;
		int index;

		internal static KeyIndexPair deflt = new KeyIndexPair("", 0);

		internal KeyIndexPair(string key, int index) {
			this.key = key;
			this.index = index;
		}

		internal string Key {
			get {
				return key;
			}
		}

		internal int Index {
			get {
				return index;
			}
		}
	}
}