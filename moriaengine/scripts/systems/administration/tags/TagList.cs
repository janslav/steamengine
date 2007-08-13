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

	[Remark("Dialog listing all object(tagholder)'s tags")]
	public class D_TagList : CompiledGump {
		private static int width = 500;
		private static int innerWidth = width - 2 * ImprovedDialog.D_BORDER - 2 * ImprovedDialog.D_SPACE;
		
		[Remark("Instance of the D_TagList, for possible access from other dialogs etc.")]
        private static D_TagList instance;
		public static D_TagList Instance {
			get {
				return instance;
			}
		}
        [Remark("Set the static reference to the instance of this dialog")]
		public D_TagList() {
			instance = this;
		}

		[Remark("Seznam parametru: 0 - thing jehoz tagy zobrazujeme, "+
				"	1 - index ze seznamu tagu ktery bude na strance jako prvni"+
				"	2 - vyhledavaci kriterium pro jmena tagu"+
				"	3 - ulozeny taglist pro pripadnou navigaci v dialogu")]
		public override void Construct(Thing focus, AbstractCharacter src, object[] sa) {
			//vzit seznam tagu z tagholdera (char nebo item) prisleho v parametru dialogu
			TagHolder th = (TagHolder)sa[0];
			List<DictionaryEntry> tagList = null;
			if(sa[3].Equals("")) {
				//vzit seznam tagu dle vyhledavaciho kriteria
				//toto se provede jen pri prvnim zobrazeni nebo zmene kriteria!
				tagList = ListifyTags(th.AllTags, sa[2].ToString());
				tagList.Sort(TagsComparer.instance);
				sa[3] = tagList; //ulozime to do argumentu dialogu
			} else {
				//taglist si posilame v argumentu (napriklad pri pagingu)
				tagList = (List<DictionaryEntry>)sa[3];
			}
			//zjistit zda bude paging, najit maximalni index na strance
			int firstiVal = Convert.ToInt32(sa[1]);   //prvni index na strance
			int imax = Math.Min(firstiVal + ImprovedDialog.PAGE_ROWS, tagList.Count);
			
			ImprovedDialog dlg = new ImprovedDialog(this.GumpInstance);
			//pozadi    
			dlg.CreateBackground(width);
			dlg.SetLocation(50, 50);

			//nadpis
			dlg.Add(new GUTATable(1, innerWidth - 2 * ButtonFactory.D_BUTTON_WIDTH - ImprovedDialog.D_COL_SPACE, 0, ButtonFactory.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = TextFactory.CreateHeadline("Seznam všech tagù na "+th.ToString()+" (zobrazeno " + (firstiVal + 1) + "-" + imax + " z " + tagList.Count + ")");
			dlg.LastTable[0, 1] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonPaper, 2);//cudlik na info o hodnotach
			dlg.LastTable[0, 2] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonCross, 0);//cudlik na zavreni dialogu
			dlg.MakeTableTransparent();
			
			//cudlik a input field na zuzeni vyberu
			dlg.Add(new GUTATable(1,130,0,ButtonFactory.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = TextFactory.CreateLabel("Vyhledávací kriterium");
			dlg.LastTable[0, 1] = InputFactory.CreateInput(LeafComponentTypes.InputText,33);
			dlg.LastTable[0, 2] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonPaper,1);
			dlg.MakeTableTransparent();

			//cudlik na zalozeni noveho tagu
			dlg.Add(new GUTATable(1,130,ButtonFactory.D_BUTTON_WIDTH,0));
			dlg.LastTable[0, 0] = TextFactory.CreateLabel("Založit nový tag");
			dlg.LastTable[0, 1] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonOK,3);
			dlg.MakeTableTransparent();			

			//popis sloupcu
			dlg.Add(new GUTATable(1, 200, 0));
			dlg.LastTable[0, 0] = TextFactory.CreateLabel("Jméno tagu");
			dlg.LastTable[0, 1] = TextFactory.CreateLabel("Hodnota");
			dlg.MakeTableTransparent();

			//seznam tagu
			dlg.Add(new GUTATable(imax-firstiVal));
			dlg.CopyColsFromLastTable();

			//projet seznam v ramci daneho rozsahu indexu
			int rowCntr = 0;
			for(int i = firstiVal; i < imax; i++) {
				DictionaryEntry de = tagList[i];

				dlg.LastTable[rowCntr, 0] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonCross, 10 + i);
				dlg.LastTable[rowCntr, 0] = TextFactory.CreateText(ButtonFactory.D_BUTTON_WIDTH,0,((TagKey)de.Key).name);
				dlg.LastTable[rowCntr, 1] = TextFactory.CreateText(ObjectSaver.Save(de.Value)); //hodnota tagu, vcetne prefixu oznacujicim typ
				
				rowCntr++;			
			}
			dlg.MakeTableTransparent(); //zpruhledni zbytek dialogu

			//now handle the paging 
			dlg.CreatePaging(tagList.Count, firstiVal,1);

			//uložit info o právì vytvoøeném dialogu pro návrat
			DialogStackItem.EnstackDialog(src, focus, D_TagList.Instance,
					sa[0], //na kom se to spoustelo
					firstiVal, //cislo polozky kterou zacina stranka (pro paging)	
					sa[2], //informace pro vyber tagu dle jmena
					sa[3]); //seznam tagu odpovidajicich kriteriu

			dlg.WriteOut();
		}

		public override void OnResponse(GumpInstance gi, GumpResponse gr) {
			//vzit "tenhle" dialog ze stacku
			DialogStackItem dsi = DialogStackItem.PopStackedDialog(gi.Cont.Conn);			

			//seznam tagu bereme z parametru (mohl byt jiz trideny atd, nebudeme ho proto selectit znova)
			List<DictionaryEntry> tagList = (List<DictionaryEntry>)dsi.Args[3];
			int firstOnPage = (int)dsi.Args[1];
            if(gr.pressedButton < 10) { //ovladaci tlacitka (exit, new, vyhledej)				
                switch(gr.pressedButton) {
                    case 0: //exit
						DialogStackItem.ShowPreviousDialog(gi.Cont.Conn); //zobrazit pripadny predchozi dialog
						//aktualni dialog uz byl vynat ze stacku, takze se opravdu zobrazi ten minuly
                        break;
                    case 1: //vyhledat dle zadani
						string nameCriteria = gr.GetTextResponse(33);
						dsi.Args[1] = 0; //zrusit info o prvnich indexech - seznam se cely zmeni tim kriteriem						
						dsi.Args[2] = nameCriteria; //uloz info o vyhledavacim kriteriu
						dsi.Args[3] = ""; //vycistit soucasny odkaz na taglist aby se mohl prenacist
						dsi.Show();
                        break;
					case 2: //zobrazit info o vysvetlivkach
						DialogStackItem.EnstackDialog(gi.Cont, dsi); //vlozime napred dialog zpet do stacku
						gi.Cont.Dialog(D_Settings_Help.Instance);
						break;   						
                    case 3: //zalozit novy tag.
						DialogStackItem.EnstackDialog(gi.Cont, dsi); //vlozime napred dialog zpet do stacku
						gi.Cont.Dialog(D_NewTag.Instance,dsi.Args[0]); //posleme si parametr toho typka na nemz bude novy tag vytvoren
						break;
                }
			} else if(ImprovedDialog.PagingButtonsHandled(gi, gr, 1, tagList.Count, 1)) {//kliknuto na paging? (1 = index parametru nesoucim info o pagingu (zde dsi.Args[1] viz výše)
				//1 sloupecek
				return;
			} else {
				//buttony na smazani
				int tagIdx = (int)gr.pressedButton - 10;
				DictionaryEntry de = ((List<DictionaryEntry>)dsi.Args[3])[tagIdx];
				TagHolder tagOwner = (TagHolder)dsi.Args[0];
				tagOwner.RemoveTag((TagKey)de.Key);
				//na zaver smazat taglist (musi se reloadnout)
				dsi.Args[3] = "";
				dsi.Show();
			}
		}

		[Remark("Retreives the list of all tags the given TagHolder has")]
		private List<DictionaryEntry> ListifyTags(IEnumerable tags, string criteria) {
			List<DictionaryEntry> tagsList = new List<DictionaryEntry>();
			foreach(DictionaryEntry entry in tags) {
				//entry in this hashtable is TagKey and its object value
				if(criteria.Equals("")) {
					tagsList.Add(entry);//bereme vse
				} else if(((TagKey)entry.Key).name.ToUpper().Contains(criteria.ToUpper())) {
					tagsList.Add(entry);//jinak jen v pripade ze kriterium se vyskytuje v nazvu tagu
				}
			}
			return tagsList;
		}

		[Remark("Display an account list. Function accessible from the game."+
				"The function is designed to be triggered using .x TagsList(criteria)"+
			    "but it can be used also normally .TagList(criteria) to display runner's own tags")]
		[SteamFunction]
		public static void TagList(Thing self, ScriptArgs text) {
			//zavolat dialog, 
			//parametr self - thing jehoz tagy chceme zobrazit
			//0 - zacneme od prvniho tagu co ma
			//treti parametr vyhledavani dle parametru, if any...
			//ctvrty parametr = volny jeden prvek pole pro seznam tagu, pouzito az v dialogu
			if(text.Argv == null || text.Argv.Length == 0) {
				Globals.SrcCharacter.Dialog(D_TagList.Instance, self, 0, "", "");
			} else {
				Globals.SrcCharacter.Dialog(D_TagList.Instance, self, 0, text.Args, "");
			}
		}
	}

	[Remark("Comparer for sorting tag dictionary entries by tags name asc")]
	public class TagsComparer : IComparer<DictionaryEntry> {
		public readonly static TagsComparer instance = new TagsComparer();

		private TagsComparer() {
			//soukromy konstruktor, pristupovat budeme pres instanci
		}

		//we have to make sure that we are sorting a list of DictionaryEntries which are tags
		//otherwise this will crash on some ClassCastException -)
		public int Compare(DictionaryEntry x, DictionaryEntry y) {
			TagKey a = (TagKey)(x.Key);
			TagKey b = (TagKey)(y.Key);
			return string.Compare(a.name,b.name);
		}
	}
}