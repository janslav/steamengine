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

	[Summary("Dialog listing all object(pluginholder)'s trigger groups")]
	public class D_TriggerGroupsList : CompiledGumpDef {
		internal static readonly TagKey tgListTK = TagKey.Get("_tg_list_");
		internal static readonly TagKey tgCriteriumTK = TagKey.Get("_tg_criterium_");

		private static int width = 500;
		private static int innerWidth = width - 2 * ImprovedDialog.D_BORDER - 2 * ImprovedDialog.D_SPACE;

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			//vzit seznam tagu z tagholdera (char nebo item) prisleho v parametru dialogu
			PluginHolder ph = (PluginHolder)args.GetTag(D_PluginList.holderTK); //z koho budeme triggergroupy brat?
			List<TriggerGroup> tgList = args.GetTag(D_TriggerGroupsList.tgListTK) as List<TriggerGroup>;
			if(tgList == null) {
				//vzit seznam triggergroup dle vyhledavaciho kriteria
				//toto se provede jen pri prvnim zobrazeni nebo zmene kriteria!
				tgList = ListifyTriggerGroups(ph.GetAllTriggerGroups(), TagMath.SGetTag(args, D_TriggerGroupsList.tgCriteriumTK));
				tgList.Sort(TriggerGroupsComparer.instance); //setridit podle abecedy
				args.SetTag(D_TriggerGroupsList.tgListTK, tgList); //ulozime to do argumentu dialogu				
			} 
			int firstiVal = TagMath.IGetTag(args, ImprovedDialog.pagingIndexTK);//prvni index na strance
			int imax = Math.Min(firstiVal + ImprovedDialog.PAGE_ROWS, tgList.Count);
			
			ImprovedDialog dlg = new ImprovedDialog(this.GumpInstance);
			//pozadi    
			dlg.CreateBackground(width);
			dlg.SetLocation(50, 50);

			//nadpis
			dlg.AddTable(new GUTATable(1, 0, ButtonFactory.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = TextFactory.CreateHeadline("Seznam v�ech trigger group na " + ph.ToString() + " (zobrazeno " + (firstiVal + 1) + "-" + imax + " z " + tgList.Count + ")");
			dlg.LastTable[0, 1] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonCross, 0);//cudlik na zavreni dialogu
			dlg.MakeLastTableTransparent();
			
			//cudlik a input field na zuzeni vyberu
			dlg.AddTable(new GUTATable(1,130,0,ButtonFactory.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = TextFactory.CreateLabel("Vyhled�vac� kriterium");
			dlg.LastTable[0, 1] = InputFactory.CreateInput(LeafComponentTypes.InputText,33);
			dlg.LastTable[0, 2] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonPaper,1);
			dlg.MakeLastTableTransparent();

			//cudlik na pridani nove triggergroupy
			dlg.AddTable(new GUTATable(1, 130, ButtonFactory.D_BUTTON_WIDTH, 0));
			dlg.LastTable[0, 0] = TextFactory.CreateLabel("P�idat trigger group");
			dlg.LastTable[0, 1] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonOK, 2);
			dlg.MakeLastTableTransparent();

			//popis sloupcu
			dlg.AddTable(new GUTATable(1, ButtonFactory.D_BUTTON_WIDTH, 200, 0));
			dlg.LastTable[0, 0] = TextFactory.CreateLabel("Sma�");			
			dlg.LastTable[0, 1] = TextFactory.CreateLabel("Jm�no Trigger groupy");
			dlg.LastTable[0, 2] = TextFactory.CreateLabel("Defname");
			dlg.MakeLastTableTransparent();

			//seznam tgcek
			dlg.AddTable(new GUTATable(imax-firstiVal));
			dlg.CopyColsFromLastTable();

			//projet seznam v ramci daneho rozsahu indexu
			int rowCntr = 0;
			for(int i = firstiVal; i < imax; i++) {
				TriggerGroup tg = tgList[i];

				dlg.LastTable[rowCntr, 0] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonCross, 10 + (i));
				dlg.LastTable[rowCntr, 1] = TextFactory.CreateText(tg.PrettyDefname);
				dlg.LastTable[rowCntr, 2] = TextFactory.CreateText(tg.Defname);	
				rowCntr++;			
			}
			dlg.MakeLastTableTransparent(); //zpruhledni zbytek dialogu

			//ted paging
			dlg.CreatePaging(tgList.Count, firstiVal, 1);			

			dlg.WriteOut();
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			PluginHolder tgOwner = (PluginHolder)args.GetTag(D_PluginList.holderTK); //z koho budeme tg brat?				
			//seznam plugin bereme z parametru (mohl byt jiz trideny atd, nebudeme ho proto selectit znova)
			List<TriggerGroup> tgList = (List<TriggerGroup>)args.GetTag(D_TriggerGroupsList.tgListTK);
			int firstOnPage = TagMath.IGetTag(args, ImprovedDialog.pagingIndexTK);
			int imax = Math.Min(firstOnPage + ImprovedDialog.PAGE_ROWS, tgList.Count);			
            if(gr.pressedButton < 10) { //ovladaci tlacitka (exit, new, vyhledej)				
                switch(gr.pressedButton) {
                    case 0: //exit
						DialogStacking.ShowPreviousDialog(gi); //zobrazit pripadny predchozi dialog
						break;
                    case 1: //vyhledat dle zadani
						string nameCriteria = gr.GetTextResponse(33);
						args.RemoveTag(ImprovedDialog.pagingIndexTK);//zrusit info o prvnich indexech - seznam se cely zmeni tim kriteriem						
						args.SetTag(D_TriggerGroupsList.tgCriteriumTK, nameCriteria);
						args.RemoveTag(D_TriggerGroupsList.tgListTK);//vycistit soucasny odkaz na tglist aby se mohl prenacist
						DialogStacking.ResendAndRestackDialog(gi);
						break;
					case 2: //pridat novou trigger groupu
						DialogArgs newArgs = new DialogArgs();
						newArgs.SetTag(D_PluginList.holderTK, tgOwner);
						Gump newGi = gi.Cont.Dialog(SingletonScript<D_NewTriggerGroup>.Instance, newArgs);
						DialogStacking.EnstackDialog(gi, newGi);						
						break;
                }
			} else if(ImprovedDialog.PagingButtonsHandled(gi, gr, tgList.Count, 1)) {//kliknuto na paging?
				return;
			} else {
				//zjistime si radek
				int row = (int)gr.pressedButton - 10;
				int buttNo = ((int)gr.pressedButton - 10) % 1; //vzdy nula, ale pokud budem chtit pridat cudlik do radku, tak to jen zmenim na 2 :)
				TriggerGroup tg = tgList[row];
				switch(buttNo) {
					case 0: //smazat						
						tgOwner.RemoveTriggerGroup(tg);
						args.RemoveTag(D_TriggerGroupsList.tgListTK);//na zaver smazat tglist (musi se reloadnout)
						DialogStacking.ResendAndRestackDialog(gi);
						break;
				}				
			}
		}

		[Summary("Retreives the list of all trigger groups the given PluginHolder has")]
		private List<TriggerGroup> ListifyTriggerGroups(IEnumerable<TriggerGroup> tgs, string criteria) {
			List<TriggerGroup> tgList = new List<TriggerGroup>();
			foreach(TriggerGroup entry in tgs) {
				if(criteria == null || criteria.Equals("")) {
					tgList.Add(entry);//bereme vse
				} else if(entry.Defname.ToUpper().Contains(criteria.ToUpper())) {
					tgList.Add(entry);//jinak jen v pripade ze kriterium se vyskytuje v nazvu tagu
				}
			}
			return tgList;
		}

		[Summary("Display a trigger group list. Function accessible from the game."+
				"The function is designed to be triggered using .x TriggerGroupList(criteria)"+
			   "but it can be used also normally .TriggerGroupList(criteria) to display runner's own plugins")]
		[SteamFunction]
		public static void TriggerGroupList(PluginHolder self, ScriptArgs text) {
			//zavolat dialog, 
			//parametr self - thing jehoz tagy chceme zobrazit
			//0 - zacneme od prvni pluginy co ma
			DialogArgs newArgs = new DialogArgs();
			newArgs.SetTag(D_PluginList.holderTK, self); //na kom budeme zobrazovat tagy
			if(text == null || text.argv == null || text.argv.Length == 0) {
				Globals.SrcCharacter.Dialog(SingletonScript<D_TriggerGroupsList>.Instance, newArgs);
			} else {
				newArgs.SetTag(D_TriggerGroupsList.tgCriteriumTK, text.Args);//vyhl. kriterium
				Globals.SrcCharacter.Dialog(SingletonScript<D_TriggerGroupsList>.Instance, newArgs);
			}
		}
	}

	[Summary("Comparer for sorting triggergroups by name")]
	public class TriggerGroupsComparer : IComparer<TriggerGroup> {
		public readonly static TriggerGroupsComparer instance = new TriggerGroupsComparer();

		private TriggerGroupsComparer() {
			//soukromy konstruktor, pristupovat budeme pres instanci
		}

		//we have to make sure that we are sorting a list of DictionaryEntries which are tags
		//otherwise this will crash on some ClassCastException -)
		public int Compare(TriggerGroup x, TriggerGroup y) {
			string nameA = x.Defname;
			string nameB = y.Defname;
			return String.Compare(nameA,nameB,true);
		}
	}
}