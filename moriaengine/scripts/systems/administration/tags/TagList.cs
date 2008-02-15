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
		private static int width = 700;
		private static int innerWidth = width - 2 * ImprovedDialog.D_BORDER - 2 * ImprovedDialog.D_SPACE;

		[Remark("Seznam parametru: 0 - thing jehoz tagy zobrazujeme, "+
				"	1 - index ze seznamu tagu ktery bude na strance jako prvni"+
				"	2 - vyhledavaci kriterium pro jmena tagu"+
				"	3 - ulozeny taglist pro pripadnou navigaci v dialogu")]
		public override void Construct(Thing focus, AbstractCharacter sendTo, object[] sa) {
			//vzit seznam tagu z tagholdera (char nebo item) prisleho v parametru dialogu
			TagHolder th = (TagHolder)sa[0];
			List<KeyValuePair<TagKey, Object>> tagList = null;
			if(sa[3] == null) {
				//vzit seznam tagu dle vyhledavaciho kriteria
				//toto se provede jen pri prvnim zobrazeni nebo zmene kriteria!
				tagList = ListifyTags(th.GetAllTags(), sa[2].ToString());
				tagList.Sort(TagsComparer.instance);
				sa[3] = tagList; //ulozime to do argumentu dialogu
			} else {
				//taglist si posilame v argumentu (napriklad pri pagingu)
				tagList = (List<KeyValuePair<TagKey, Object>>) sa[3];
			}
			//zjistit zda bude paging, najit maximalni index na strance
			int firstiVal = Convert.ToInt32(sa[1]);   //prvni index na strance
			int imax = Math.Min(firstiVal + ImprovedDialog.PAGE_ROWS, tagList.Count);
			
			ImprovedDialog dlg = new ImprovedDialog(this.GumpInstance);
			//pozadi    
			dlg.CreateBackground(width);
			dlg.SetLocation(50, 50);

			//nadpis
			dlg.AddTable(new GUTATable(1, innerWidth - 2 * ButtonFactory.D_BUTTON_WIDTH - ImprovedDialog.D_COL_SPACE, 0, ButtonFactory.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = TextFactory.CreateHeadline("Seznam v�ech tag� na "+th.ToString()+" (zobrazeno " + (firstiVal + 1) + "-" + imax + " z " + tagList.Count + ")");
			dlg.LastTable[0, 1] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonPaper, 2);//cudlik na info o hodnotach
			dlg.LastTable[0, 2] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonCross, 0);//cudlik na zavreni dialogu
			dlg.MakeTableTransparent();
			
			//cudlik a input field na zuzeni vyberu
			dlg.AddTable(new GUTATable(1,130,0,ButtonFactory.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = TextFactory.CreateLabel("Vyhled�vac� kriterium");
			dlg.LastTable[0, 1] = InputFactory.CreateInput(LeafComponentTypes.InputText,33);
			dlg.LastTable[0, 2] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonPaper,1);
			dlg.MakeTableTransparent();

			//cudlik na zalozeni noveho tagu
			dlg.AddTable(new GUTATable(1,130,ButtonFactory.D_BUTTON_WIDTH,0));
			dlg.LastTable[0, 0] = TextFactory.CreateLabel("Zalo�it nov� tag");
			dlg.LastTable[0, 1] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonOK,3);
			dlg.MakeTableTransparent();			

			//popis sloupcu
			dlg.AddTable(new GUTATable(1, ButtonFactory.D_BUTTON_WIDTH, 200, ButtonFactory.D_BUTTON_WIDTH, 0));
			dlg.LastTable[0, 0] = TextFactory.CreateLabel("Sma�");			
			dlg.LastTable[0, 1] = TextFactory.CreateLabel("Jm�no tagu");
			dlg.LastTable[0, 2] = TextFactory.CreateLabel("Info");			
			dlg.LastTable[0, 3] = TextFactory.CreateLabel("Hodnota");
			dlg.MakeTableTransparent();

			//seznam tagu
			dlg.AddTable(new GUTATable(imax-firstiVal));
			dlg.CopyColsFromLastTable();

			//projet seznam v ramci daneho rozsahu indexu
			int rowCntr = 0;
			for(int i = firstiVal; i < imax; i++) {
				KeyValuePair<TagKey, Object> de = tagList[i];

				dlg.LastTable[rowCntr, 0] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonCross, 10 + (3*i));
				dlg.LastTable[rowCntr, 1] = TextFactory.CreateText(de.Key.name);
				//je li hodnota simple saveable nebo ma koordinator, muzeme ObjectSaver.Save
				if(ObjectSaver.IsSimpleSaveableOrCoordinated(de.Value.GetType())) {
					//hodnota tagu, vcetne prefixu oznacujicim typ
					dlg.LastTable[rowCntr, 3] = InputFactory.CreateInput(LeafComponentTypes.InputText,11 + (3*i), ObjectSaver.Save(de.Value));
				} else {//jinak odkaz do infodialogu
					dlg.LastTable[rowCntr, 2] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonPaper, 12 + (3 * i));								
					dlg.LastTable[rowCntr, 3] = TextFactory.CreateText(de.Value.GetType().Name);
				}				
				rowCntr++;			
			}
			dlg.MakeTableTransparent(); //zpruhledni zbytek dialogu

			//ted paging
			dlg.CreatePaging(tagList.Count, firstiVal,1);

			//Ok button
			dlg.AddTable(new GUTATable(1, ButtonFactory.D_BUTTON_WIDTH, 0));
			dlg.LastTable[0, 0] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonOK, 4);
			dlg.LastTable[0, 1] = TextFactory.CreateText("Ulo�it");
			dlg.MakeTableTransparent();

			dlg.WriteOut();
		}

		public override void OnResponse(GumpInstance gi, GumpResponse gr, object[] args) {
			//seznam tagu bereme z parametru (mohl byt jiz trideny atd, nebudeme ho proto selectit znova)
			List<KeyValuePair<TagKey, Object>> tagList = (List<KeyValuePair<TagKey, Object>>) args[3];
			int firstOnPage = (int)args[1];
			int imax = Math.Min(firstOnPage + ImprovedDialog.PAGE_ROWS, tagList.Count);			
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
					case 2: //zobrazit info o vysvetlivkach
						GumpInstance newGi = gi.Cont.Dialog(SingletonScript<D_Settings_Help>.Instance);
						DialogStacking.EnstackDialog(gi, newGi);						
						break;   						
                    case 3: //zalozit novy tag.
						newGi = gi.Cont.Dialog(SingletonScript<D_NewTag>.Instance, args[0]); //posleme si parametr toho typka na nemz bude novy tag vytvoren
						DialogStacking.EnstackDialog(gi,newGi); //vlozime napred dialog do stacku
						break;
					case 4: //ulo�it pripadne zmeny
						//projdeme dostupny seznam tagu na strance a u tech editovatelnych zkoukneme zmeny
						for(int i = firstOnPage; i < imax; i++) {
							KeyValuePair<TagKey, Object> de = tagList[i];
							if(ObjectSaver.IsSimpleSaveableOrCoordinated(de.Value.GetType())) {
								//jen editovatelne primo (ostatni jsou editovatelne jen pres info dialog)
								string oldValue = ObjectSaver.Save(de.Value);
								string dialogValue = gr.GetTextResponse(11 + (3 * i));
								if(!string.Equals(oldValue, dialogValue)) {
									TagHolder tagOwner = (TagHolder)args[0];
									try {
										tagOwner.SetTag(de.Key, ObjectSaver.Load(dialogValue));
									} catch {
										//napsat chybovou hlasku, ale pojedeme dal
										((Character)gi.Cont).RedMessage("Failed to set the tag " + de.Key.name + " to value " + dialogValue);
									}
								}
							}											
						}
						//resendneme to
						DialogStacking.ResendAndRestackDialog(gi);
						break;
                }
			} else if(ImprovedDialog.PagingButtonsHandled(gi, gr, 1, tagList.Count, 1)) {//kliknuto na paging? (1 = index parametru nesoucim info o pagingu (zde dsi.Args[1] viz v��e)
				//1 sloupecek
				return;
			} else {
				//zjistime si radek
				int row = ((int)gr.pressedButton - 10) / 3;
				int buttNo = ((int)gr.pressedButton - 10) % 3;
				TagHolder tagOwner = (TagHolder)args[0];
				KeyValuePair<TagKey, Object> de = tagList[row];
				switch(buttNo) {
					case 0: //smazat						
						tagOwner.RemoveTag(de.Key);
						//na zaver smazat taglist (musi se reloadnout)
						args[3] = null;
						DialogStacking.ResendAndRestackDialog(gi);
						break;
					case 2: //info
						GumpInstance newGi = gi.Cont.Dialog(SingletonScript<D_Info>.Instance, tagOwner.GetTag(de.Key), 0, 0);
						DialogStacking.EnstackDialog(gi, newGi);
						break;
				}				
			}
		}

		[Remark("Retreives the list of all tags the given TagHolder has")]
		private List<KeyValuePair<TagKey, Object>> ListifyTags(IEnumerable<KeyValuePair<TagKey, Object>> tags, string criteria) {
			List<KeyValuePair<TagKey, Object>> tagsList = new List<KeyValuePair<TagKey, Object>>();
			foreach (KeyValuePair<TagKey, Object> entry in tags) {
				//entry in this hashtable is TagKey and its object value
				if(criteria.Equals("")) {
					tagsList.Add(entry);//bereme vse
				} else if(entry.Key.name.ToUpper().Contains(criteria.ToUpper())) {
					tagsList.Add(entry);//jinak jen v pripade ze kriterium se vyskytuje v nazvu tagu
				}
			}
			return tagsList;
		}

		[Remark("Display an account list. Function accessible from the game."+
				"The function is designed to be triggered using .x TagsList(criteria)"+
			    "but it can be used also normally .TagList(criteria) to display runner's own tags")]
		[SteamFunction]
		public static void TagList(TagHolder self, ScriptArgs text) {
			//zavolat dialog, 
			//parametr self - thing jehoz tagy chceme zobrazit
			//0 - zacneme od prvniho tagu co ma
			//treti parametr vyhledavani dle parametru, if any...
			//ctvrty parametr = volny jeden prvek pole pro seznam tagu, pouzito az v dialogu
			if(text == null || text.argv == null || text.argv.Length == 0) {
				Globals.SrcCharacter.Dialog(SingletonScript<D_TagList>.Instance, self, 0, "", null);
			} else {
				Globals.SrcCharacter.Dialog(SingletonScript<D_TagList>.Instance, self, 0, text.Args, null);
			}
		}
	}

	[Remark("Comparer for sorting tag dictionary entries by tags name asc")]
	public class TagsComparer : IComparer<KeyValuePair<TagKey, Object>> {
		public readonly static TagsComparer instance = new TagsComparer();

		private TagsComparer() {
			//soukromy konstruktor, pristupovat budeme pres instanci
		}

		//we have to make sure that we are sorting a list of DictionaryEntries which are tags
		//otherwise this will crash on some ClassCastException -)
		public int Compare(KeyValuePair<TagKey, Object> x, KeyValuePair<TagKey, Object> y) {
			TagKey a = x.Key;
			TagKey b = y.Key;
			return String.Compare(a.name,b.name,true);
		}
	}
}