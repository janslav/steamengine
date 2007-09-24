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
		private readonly TagKey pageInfoMap = TagKey.Get("_pageInfoMap_");
		private readonly TagKey lastKeyTag = TagKey.Get("_lastKeyTag_");
		private readonly TagKey lastIndexTag = TagKey.Get("_lastIndexTag_");
		private readonly TagKey categoriesListTag = TagKey.Get("_categoriesListTag_");

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
		public Hashtable valuesToSet;

		public override void Construct(Thing focus, AbstractCharacter sendTo, object[] args) {
			valuesToSet = new Hashtable(); //vycistime tabulku inputfieldu ted
			
			//pole obsahujici vsechny ketegorie pro zobrazeni
			SettingsCategory[] categories = StaticMemberSaver.GetMembersForSetting();

			ImprovedDialog dlg = new ImprovedDialog(GumpInstance);
			dlg.CreateBackground(width);
			dlg.SetLocation(0, 30);

			dlg.Add(new GUTATable(1, innerWidth - 2 * ButtonFactory.D_BUTTON_WIDTH - ImprovedDialog.D_COL_SPACE, 0, ButtonFactory.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = TextFactory.CreateHeadline("Nastavení globálních promìnných. Pro informace zmáèkni tlaèítko s papírem vpravo v rohu.");			
			dlg.LastTable[0, 1] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonPaper, 2);
			dlg.LastTable[0, 2] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonCross, 0);
			dlg.MakeTableTransparent();

			dlg.Add(new GUTATable(ImprovedDialog.PAGE_ROWS, innerWidth / 4, innerWidth / 4, 0, innerWidth / 4));//bude to ve 4 sloupcích
			dlg.MakeTableTransparent();
			rowCounter = 0;
			dlgIndex = 0; //indexování inputfieldù v dialogu
			string catKey = (string)args[0]; //klic vybrane kategorie (na tretim argumentu pak zavisi zda zobrazi jen tuto kategorii nebo vsechny abecedne od ni(vcetne) dal)
			int listIndex = (int)args[1]; //indexování poøadí SavedMemberù v prvním procházeném listu (to bude ten kde jsme skonèili na predchozi strance)
			int actualPage = (int)args[2]; //ktera stranka se nam zobrazuje?
			SettingsDisplay dspl = (SettingsDisplay)args[3]; //All nebo Single - zobrazene kategorie

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
			if(args[4] != null) {					
				//na pate pozici v argumentech je ukladan ten seznam
				valuesToSet = (Hashtable)args[4];
			}
			
			//par indikatoru pro layout - pocitadlo sloupecku a informace o tom kam se ma vypisovat
			filledColumn = 0;
			bool switchToNextColumn = false, switchToNextPage = false;
			bool useListIndex = true;
			String actualKey = "";
			int idx = 0; //index pro listovani ve vnitrnich seznamech kazde kategorie

			List<SettingsCategory> selectedCats = GetRelevantCategories(categories, catKey, dspl);

			this.GumpInstance.SetTag(categoriesListTag, selectedCats);//ulozime seznam kategorii pro potreby vycisteni

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
			dlg.Add(new GUTATable(1, ButtonFactory.D_BUTTON_WIDTH, 0));
			dlg.LastTable[0, 0] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonOK, 1);
			dlg.LastTable[0, 1] = TextFactory.CreateText("Nastavit");
			dlg.MakeTableTransparent();
			dlg.WriteOut();

			//ulozit informaci o tom ktery klic a index seznamu v kategorii byl na teto strance prvni
			StoreKeyIndexInfo(catKey, listIndex, actualPage, ref args);
			//ulozit informaci o posledni kategorii a poslednim indexu v ni
			this.GumpInstance.SetTag(lastKeyTag, actualKey);//tato kategorie bude prvni na dalsi strance
			this.GumpInstance.SetTag(lastIndexTag, idx + 1);//toto bude index prvniho membera z vyse uvedene kategorie ktery se zobrazi
		}

		//args:
		//0 - prvni klic skupiny promennych co bude na strance
		//1 - kolikaty saved member ze skupiny bude zobrazen jako prvni
		//2 - cislo stranky
		//3	- haash tabulka se všemi hodnotami a indexy v dialogu 
		public override void OnResponse(GumpInstance gi, GumpResponse gr, object[] args) {
			if(gr.pressedButton == 0) { //exit button
				//vezmem seznam zobrazenych kategorii, projdeme ho a vsechny membery vycistime
				List<SettingsCategory> catlist = (List<SettingsCategory>)this.GumpInstance.GetTag(categoriesListTag);
				foreach(SettingsCategory sCat in catlist) {
					sCat.ClearSettingValues();					
				}
				DialogStackItem.ShowPreviousDialog(gi.Cont.Conn); //zobrazit pripadny predchozi dialog
			}
			//navigacni buttony (paging)
			if(gr.pressedButton == ImprovedDialog.ID_PREV_BUTTON) {
				args[2] = Convert.ToInt32(args[2]) - 1;//strankovani o krok zpet
				KeyIndexPair kip = loadKeyIndexInfo(Convert.ToInt32(args[2]),args);
									//(KeyIndexPair)keytable[Convert.ToInt32(dsi.Args[2])];
				args[0] = kip.Key; //updatnout info o prvni kategorii na predchozi strance				
				args[1] = kip.Index; //ve vybrane prvni kategorii zacneme zobrazovat od tohoto clena
				gi.Cont.SendGump(gi);
			} else if(gr.pressedButton == ImprovedDialog.ID_NEXT_BUTTON) {
				string firstCat = (string)this.GumpInstance.GetTag(lastKeyTag);
				int firstIdx = Convert.ToInt32(this.GumpInstance.GetTag(lastIndexTag));

				args[2] = Convert.ToInt32(args[2]) + 1;
				args[0] = firstCat; //updatnout info o prvni kategorii				
				args[1] = firstIdx; //v prvni kategorii zacneme zobrazovat od tohoto clena
				gi.Cont.SendGump(gi);
			} else if(gr.pressedButton == 1) { //nastaveni
				//napred vycistime vsechny mozne predchozi neuspechy v nastaveni - nyni totiz jedem znova
				List<SettingsCategory> catlist = (List<SettingsCategory>)this.GumpInstance.GetTag(categoriesListTag);
				foreach(SettingsCategory sCat in catlist) {
					sCat.ClearSettingValues();
				}

				TryMakeSetting(valuesToSet,gr);
				args[4] = valuesToSet; //predame si seznam hodnot v dialogu pro pozdejsi pripadny navrat
				gi.Cont.SendGump(gi);//zobrazime znovu tentyz dialog
				//a zobrazime take dialog s vysledky (null = volne misto pro seznamy resultu v nasledujicim dialogu)
				gi.Cont.Dialog(SingletonScript<D_Settings_Result>.Instance, 0, valuesToSet, null);
			} else if(gr.pressedButton == 2) { //info
				//stackneme se pro navrat
				DialogStackItem.EnstackDialog(gi);
				gi.Cont.Dialog(SingletonScript<D_Settings_Help>.Instance);				
			}
		}

		[Remark("Specificky zpusob vytvareni pagingu pro nastavovaci dialogy - nebudeme pouzivat ten "+ 
				"klasickej zpusob protoze neni jendoduchyy zjistit kolik polozek je vlastne na strance, "+ 
				"stranka nemusi mit nutne konstantni pocet radku. =>vynechame ciselnou navigaci "+
				"Predavame si jako parametr posledni klic z naseho sortedslovniku se SavedMemberama a "+
				"posledne pouzity index do listu MemberInfo trid pro dany klic. Take posilame cislo stranky "+
				"(to jen abychom vedeli jak udelat navigacni sloupecek)")]
		private void CreateSettingsPaging(ImprovedDialog dlg, int actualPage, bool onlyBack) {
			bool prevNextColumnAdded = false; //indikator navigacniho sloupecku
			if(actualPage > 0) {
				prevNextColumnAdded = true; //navigacni sloupecek uz bude existovat
				dlg.AddLast(new GUTAColumn(ButtonFactory.D_BUTTON_PREVNEXT_WIDTH));
				//button pro predchozi stranku
				dlg.LastColumn.AddComponent(ButtonFactory.CreateButton(LeafComponentTypes.ButtonPrev, ImprovedDialog.ID_PREV_BUTTON)); //prev
			}
			if(!onlyBack) {
				//ne chceme jen tlacitko zpet (to se stane napr kdyz na strance bude jen 
				//nekolik itemu takze uz nebude zadna dalsi strana ale potrebujeme se vracet)
				if(!prevNextColumnAdded) { //navigacni sloupecek jeste nebyl zalozen - jsme na prvni strance teprve.
					dlg.AddLast(new GUTAColumn(ButtonFactory.D_BUTTON_PREVNEXT_WIDTH));
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
		private void StoreKeyIndexInfo(string firstKey, int firstIndex, int actualPage, ref object[] args) {
			Hashtable keytable = null;
			if(args[5] == null) { //jeste ji tam nemame
				keytable = new Hashtable();
				args[5] = keytable;
			} else {
				keytable = (Hashtable)args[args.Length - 1];
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