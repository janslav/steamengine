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

	[Summary("Dialog listing all object(tagholder)'s tags")]
	public class D_TagList : CompiledGumpDef {
		internal static readonly TagKey holderTK = TagKey.Acquire("_tag_holder_");
		internal static readonly TagKey tagListTK = TagKey.Acquire("_tag_list_");
		internal static readonly TagKey tagCriteriumTK = TagKey.Acquire("_tag_criterium_");

		private static int width = 700;
		private static int innerWidth = width - 2 * ImprovedDialog.D_BORDER - 2 * ImprovedDialog.D_SPACE;

		[Summary("Seznam parametru: 0 - thing jehoz tagy zobrazujeme, " +
				"	1 - index ze seznamu tagu ktery bude na strance jako prvni" +
				"	2 - vyhledavaci kriterium pro jmena tagu" +
				"	3 - ulozeny taglist pro pripadnou navigaci v dialogu")]
		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			//vzit seznam tagu z tagholdera (char nebo item) prisleho v parametru dialogu
			TagHolder th = (TagHolder) args.GetTag(D_TagList.holderTK); //z koho budeme tagy brat?
			List<KeyValuePair<TagKey, Object>> tagList = args.GetTag(D_TagList.tagListTK) as List<KeyValuePair<TagKey, Object>>;
			if (tagList == null) {
				//vzit seznam tagu dle vyhledavaciho kriteria
				//toto se provede jen pri prvnim zobrazeni nebo zmene kriteria!
				tagList = ListifyTags(th.GetAllTags(), TagMath.SGetTag(args, D_TagList.tagCriteriumTK));
				tagList.Sort(TagsComparer.instance);
				args.SetTag(D_TagList.tagListTK, tagList); //ulozime to do argumentu dialogu				
			}
			int firstiVal = TagMath.IGetTag(args, ImprovedDialog.pagingIndexTK);//prvni index na strance
			int imax = Math.Min(firstiVal + ImprovedDialog.PAGE_ROWS, tagList.Count);

			ImprovedDialog dlg = new ImprovedDialog(this.GumpInstance);
			//pozadi    
			dlg.CreateBackground(width);
			dlg.SetLocation(50, 50);

			//nadpis
			dlg.AddTable(new GUTATable(1, innerWidth - 2 * ButtonMetrics.D_BUTTON_WIDTH - ImprovedDialog.D_COL_SPACE, 0, ButtonMetrics.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = GUTAText.Builder.TextHeadline("Seznam v�ech tag� na " + th.ToString() + " (zobrazeno " + (firstiVal + 1) + "-" + imax + " z " + tagList.Count + ")").Build();
			dlg.LastTable[0, 1] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonPaper).Id(2).Build();//cudlik na info o hodnotach
			dlg.LastTable[0, 2] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonCross).Id(0).Build();//cudlik na zavreni dialogu
			dlg.MakeLastTableTransparent();

			//cudlik a input field na zuzeni vyberu
			dlg.AddTable(new GUTATable(1, 130, 0, ButtonMetrics.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = GUTAText.Builder.TextLabel("Vyhled�vac� kriterium").Build();
			dlg.LastTable[0, 1] = GUTAInput.Builder.Id(33).Build();
			dlg.LastTable[0, 2] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonPaper).Id(1).Build();
			dlg.MakeLastTableTransparent();

			//cudlik na zalozeni noveho tagu
			dlg.AddTable(new GUTATable(1, 130, ButtonMetrics.D_BUTTON_WIDTH, 0));
			dlg.LastTable[0, 0] = GUTAText.Builder.TextLabel("Zalo�it nov� tag").Build();
			dlg.LastTable[0, 1] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonOK).Id(3).Build();
			dlg.MakeLastTableTransparent();

			//popis sloupcu
			dlg.AddTable(new GUTATable(1, ButtonMetrics.D_BUTTON_WIDTH, 200, ButtonMetrics.D_BUTTON_WIDTH, 0));
			dlg.LastTable[0, 0] = GUTAText.Builder.TextLabel("Sma�").Build();
			dlg.LastTable[0, 1] = GUTAText.Builder.TextLabel("Jm�no tagu").Build();
			dlg.LastTable[0, 2] = GUTAText.Builder.TextLabel("Info").Build();
			dlg.LastTable[0, 3] = GUTAText.Builder.TextLabel("Hodnota").Build();
			dlg.MakeLastTableTransparent();

			//seznam tagu
			dlg.AddTable(new GUTATable(imax - firstiVal));
			dlg.CopyColsFromLastTable();

			//projet seznam v ramci daneho rozsahu indexu
			int rowCntr = 0;
			for (int i = firstiVal; i < imax; i++) {
				KeyValuePair<TagKey, Object> de = tagList[i];

				dlg.LastTable[rowCntr, 0] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonCross).Id(10 + (3 * i)).Build();
				dlg.LastTable[rowCntr, 1] = GUTAText.Builder.Text(de.Key.Name).Build();
				//je li hodnota simple saveable nebo ma koordinator, muzeme ObjectSaver.Save
				if (ObjectSaver.IsSimpleSaveableOrCoordinated(de.Value.GetType())) {
					//hodnota tagu, vcetne prefixu oznacujicim typ
					dlg.LastTable[rowCntr, 3] = GUTAInput.Builder.Id(11 + (3 * i)).Text(ObjectSaver.Save(de.Value)).Build();
				} else {//jinak odkaz do infodialogu
					dlg.LastTable[rowCntr, 2] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonPaper).Id(12 + (3 * i)).Build();
					dlg.LastTable[rowCntr, 3] = GUTAText.Builder.Text(Tools.TypeToString(de.Value.GetType())).Build();
				}
				rowCntr++;
			}
			dlg.MakeLastTableTransparent(); //zpruhledni zbytek dialogu

			//ted paging
			dlg.CreatePaging(tagList.Count, firstiVal, 1);

			//Ok button
			dlg.AddTable(new GUTATable(1, ButtonMetrics.D_BUTTON_WIDTH, 0));
			dlg.LastTable[0, 0] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonOK).Id(4).Build();
			dlg.LastTable[0, 1] = GUTAText.Builder.Text("Ulo�it").Build();
			dlg.MakeLastTableTransparent();

			dlg.WriteOut();
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			//seznam tagu bereme z parametru (mohl byt jiz trideny atd, nebudeme ho proto selectit znova)
			List<KeyValuePair<TagKey, Object>> tagList = (List<KeyValuePair<TagKey, Object>>) args.GetTag(D_TagList.tagListTK);
			int firstOnPage = TagMath.IGetTag(args, ImprovedDialog.pagingIndexTK);
			int imax = Math.Min(firstOnPage + ImprovedDialog.PAGE_ROWS, tagList.Count);
			if (gr.PressedButton < 10) { //ovladaci tlacitka (exit, new, vyhledej)				
				switch (gr.PressedButton) {
					case 0: //exit
						DialogStacking.ShowPreviousDialog(gi); //zobrazit pripadny predchozi dialog
						break;
					case 1: //vyhledat dle zadani
						string nameCriteria = gr.GetTextResponse(33);
						args.RemoveTag(ImprovedDialog.pagingIndexTK);//zrusit info o prvnich indexech - seznam se cely zmeni tim kriteriem						
						args.SetTag(D_TagList.tagCriteriumTK, nameCriteria);
						args.RemoveTag(D_TagList.tagListTK);//vycistit soucasny odkaz na taglist aby se mohl prenacist
						DialogStacking.ResendAndRestackDialog(gi);
						break;
					case 2: //zobrazit info o vysvetlivkach
						Gump newGi = gi.Cont.Dialog(SingletonScript<D_Settings_Help>.Instance);
						DialogStacking.EnstackDialog(gi, newGi);
						break;
					case 3: //zalozit novy tag.
						DialogArgs newArgs = new DialogArgs();
						newArgs.SetTag(D_TagList.holderTK, (TagHolder) args.GetTag(D_TagList.holderTK));
						newGi = gi.Cont.Dialog(SingletonScript<D_NewTag>.Instance, newArgs); //posleme si parametr toho typka na nemz bude novy tag vytvoren
						DialogStacking.EnstackDialog(gi, newGi); //vlozime napred dialog do stacku
						break;
					case 4: //ulo�it pripadne zmeny
						//projdeme dostupny seznam tagu na strance a u tech editovatelnych zkoukneme zmeny
						for (int i = firstOnPage; i < imax; i++) {
							KeyValuePair<TagKey, Object> de = tagList[i];
							if (ObjectSaver.IsSimpleSaveableOrCoordinated(de.Value.GetType())) {
								//jen editovatelne primo (ostatni jsou editovatelne jen pres info dialog)
								string oldValue = ObjectSaver.Save(de.Value);
								string dialogValue = gr.GetTextResponse(11 + (3 * i));
								if (!string.Equals(oldValue, dialogValue)) {
									TagHolder tagOwner = (TagHolder) args.GetTag(D_TagList.holderTK);
									try {
										tagOwner.SetTag(de.Key, ObjectSaver.Load(dialogValue));
									} catch {
										//napsat chybovou hlasku, ale pojedeme dal
										((Character) gi.Cont).RedMessage("Failed to set the tag " + de.Key.Name + " to value " + dialogValue);
									}
								}
							}
						}
						//resendneme to
						DialogStacking.ResendAndRestackDialog(gi);
						break;
				}
			} else if (ImprovedDialog.PagingButtonsHandled(gi, gr, tagList.Count, 1)) {//kliknuto na paging?
				return;
			} else {
				//zjistime si radek
				int row = ((int) gr.PressedButton - 10) / 3;
				int buttNo = ((int) gr.PressedButton - 10) % 3;
				TagHolder tagOwner = (TagHolder) args.GetTag(D_TagList.holderTK); //z koho budeme tagy brat?
				KeyValuePair<TagKey, Object> de = tagList[row];
				switch (buttNo) {
					case 0: //smazat						
						tagOwner.RemoveTag(de.Key);
						args.RemoveTag(D_TagList.tagListTK);//na zaver smazat taglist (musi se reloadnout)
						DialogStacking.ResendAndRestackDialog(gi);
						break;
					case 2: //info
						Gump newGi = gi.Cont.Dialog(SingletonScript<D_Info>.Instance, new DialogArgs(tagOwner.GetTag(de.Key)));
						DialogStacking.EnstackDialog(gi, newGi);
						break;
				}
			}
		}

		[Summary("Retreives the list of all tags the given TagHolder has")]
		private List<KeyValuePair<TagKey, Object>> ListifyTags(IEnumerable<KeyValuePair<TagKey, Object>> tags, string criteria) {
			List<KeyValuePair<TagKey, Object>> tagsList = new List<KeyValuePair<TagKey, Object>>();
			foreach (KeyValuePair<TagKey, Object> entry in tags) {
				//entry in this hashtable is TagKey and its object value
				if (criteria == null || criteria.Equals("")) {
					tagsList.Add(entry);//bereme vse
				} else if (entry.Key.Name.ToUpper().Contains(criteria.ToUpper())) {
					tagsList.Add(entry);//jinak jen v pripade ze kriterium se vyskytuje v nazvu tagu
				}
			}
			return tagsList;
		}

		[Summary("Display a tag list. Function accessible from the game." +
				"The function is designed to be triggered using .x TagList(criteria)" +
				"but it can be used also normally .TagList(criteria) to display runner's own tags")]
		[SteamFunction]
		public static void TagList(TagHolder self, ScriptArgs text) {
			//zavolat dialog, 
			//parametr self - thing jehoz tagy chceme zobrazit
			//0 - zacneme od prvniho tagu
			DialogArgs newArgs = new DialogArgs();
			newArgs.SetTag(D_TagList.holderTK, self); //na sobe budeme zobrazovat tagy
			if (text == null || text.Argv == null || text.Argv.Length == 0) {
				Globals.SrcCharacter.Dialog(SingletonScript<D_TagList>.Instance, newArgs);
			} else {
				newArgs.SetTag(D_TagList.tagCriteriumTK, text.Args);//vyhl. kriterium
				Globals.SrcCharacter.Dialog(SingletonScript<D_TagList>.Instance, newArgs);
			}
		}
	}

	[Summary("Comparer for sorting tag dictionary entries by tags name asc")]
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
			return StringComparer.OrdinalIgnoreCase.Compare(a.Name, b.Name);
		}
	}
}