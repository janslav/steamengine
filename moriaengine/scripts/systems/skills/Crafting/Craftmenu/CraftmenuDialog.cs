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

	[Summary("Craftmenu for the specified crafting skill")]
	public class D_Craftmenu : CompiledGumpDef {
		public static readonly TagKey tkCraftmenuLastpos = TagKey.Get("_cm_lastPosition_");
		private static int width = 600;

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			ImprovedDialog dlg = new ImprovedDialog(this.GumpInstance);
			CraftmenuCategory cat = (CraftmenuCategory) args[0];

			int firstiVal = TagMath.IGetTag(args, ImprovedDialog.pagingIndexTK); //prvni index na strance
			int imax = Math.Min(firstiVal + ImprovedDialog.PAGE_ROWS, cat.contents.Count); //nejvyssi index na strance

			int innerWidth = (width - 2 * ImprovedDialog.D_BORDER - 2 * ImprovedDialog.D_SPACE);

			dlg.CreateBackground(width);
			dlg.SetLocation(80, 50);

			//nadpis dialogu, cudliky na back a zruseni
			dlg.AddTable(new GUTATable(1, innerWidth - 2 * ButtonFactory.D_BUTTON_WIDTH - ImprovedDialog.D_COL_SPACE, 0, ButtonFactory.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = TextFactory.CreateHeadline("Kategorie výroby: " + cat.FullName);
			dlg.LastTable[0, 1] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonBack, 1); //one category back button
			dlg.LastTable[0, 2] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonCross, 0); //exit button
			dlg.MakeLastTableTransparent();

			//"new subcategory" and "add items" button	
			if (((Player) sendTo).IsGM) {//only for GMs
				dlg.AddTable(new GUTATable(1, ButtonFactory.D_BUTTON_WIDTH, 203, ButtonFactory.D_BUTTON_WIDTH, 0));
				dlg.LastTable[0, 0] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonPaper, 3); //new item(s) btn
				dlg.LastTable[0, 1] = TextFactory.CreateLabel("Pøidat pøedmìt(y)");
				dlg.LastTable[0, 2] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonPaper, 2); //new subcategory btn
				dlg.LastTable[0, 3] = TextFactory.CreateLabel("Pøidat podkategorii");
				dlg.MakeLastTableTransparent();
			}

			
			//headlines row
			//make | icon | name | move up/down | info | resources
			dlg.AddTable(new GUTATable(1, ButtonFactory.D_BUTTON_WIDTH, ImprovedDialog.ICON_WIDTH, 150, 10, ButtonFactory.D_BUTTON_WIDTH, 0));
			dlg.LastTable[0, 2] = TextFactory.CreateLabel("Jméno", DialogAlignment.Align_Center, DialogAlignment.Valign_Top);
			dlg.LastTable[0, 4] = TextFactory.CreateLabel("Info");
			dlg.LastTable[0, 5] = TextFactory.CreateLabel("Suroviny", DialogAlignment.Align_Center, DialogAlignment.Valign_Top);
			dlg.MakeLastTableTransparent();

			//konecne seznam polozek
			dlg.AddTable(new GUTATable(imax - firstiVal));
			dlg.LastTable.RowHeight = ImprovedDialog.ICON_HEIGHT; //it'll contain pictures...
			dlg.CopyColsFromLastTable();
			dlg.LastTable.InnerRowsDelimited = true;
			
			//projet seznam v ramci daneho rozsahu indexu
			int rowCntr = 0;
			int maxIndex = cat.contents.Count - 1; //maximal index to access (in case of omitting...)
			CraftmenuItem oneItm = null;
			CraftmenuCategory oneCat = null;
			for (int i = firstiVal; i < imax; i++) {
				ICraftmenuElement elem = cat.contents[i];
				if (elem.IsCategory) {
					oneCat = (CraftmenuCategory) elem;
					dlg.LastTable[rowCntr, 0] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonTick, 4 * i + 10, DialogAlignment.Valign_Center);
					dlg.LastTable[rowCntr, 1] = ImageFactory.CreateNamedImage(GumpIDs.Pouch);
				} else {
					oneItm = (CraftmenuItem) elem;
					if (!oneItm.itemDef.CanBeMade((Character)focus)) {
						//cannot be made, do not display, step over and continue
						if (imax < maxIndex) {
							imax++; //we can iterate additional one step 
						}
						continue;
					}
					dlg.LastTable[rowCntr, 0] = ButtonFactory.CreateCheckbox(false, 1 * i + 10, DialogAlignment.Align_Center, DialogAlignment.Valign_Center);
					dlg.LastTable[rowCntr, 1] = ImageFactory.CreateImage((int)oneItm.itemDef.Model);
					dlg.LastTable[rowCntr, 4] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonPaper, 4 * i + 13, DialogAlignment.Valign_Center);//display info
					//now the list of resources
					ResourcesList reses = oneItm.itemDef.Resources;
					if (reses != null) {//only if we have any resources...
						int spaceLength = ImprovedDialog.TextLength(" ");
						int lastColPos = spaceLength; //relative position in the resources column (beginning one space from the border)
						foreach (IResourceListItemMultiplicable rlItm in reses.MultiplicablesSublist) {
							ItemResource itmRes = rlItm as ItemResource;
							if (itmRes != null) {//add count + item picture
								string textToShow = (lastColPos > spaceLength) ? "  " : ""; //second and more items will be separated by 2spaces
								textToShow += itmRes.DesiredCount.ToString() + " ";
								dlg.LastTable[rowCntr, 5] = TextFactory.CreateText(lastColPos, 0, textToShow, DialogAlignment.Align_Left, DialogAlignment.Valign_Center);
								int countLength = ImprovedDialog.TextLength(textToShow); //length of the text with number (count of items needed) plus the seaprating space
								dlg.LastTable[rowCntr, 5] = ImageFactory.CreateImage(lastColPos + countLength, 0, itmRes.ItemDef.Model);
								//prepare next offset:
								GumpArtDimension gad = GumpDimensions.Table[itmRes.ItemDef.Model];
								lastColPos += spaceLength + countLength + gad.Width; //first offset, counted length of the number text including separating space, width of the icon, space after the icon
							} else {//not an item => can be a typedef... (t_fruit etc.)
								TriggerGroupResource tgrRes = rlItm as TriggerGroupResource;
								if (tgrRes != null) {
									string textToShow = (lastColPos > spaceLength) ? "  " : ""; //second and more items will be separated by 2 spaces
									textToShow += tgrRes.DesiredCount.ToString() + " " + tgrRes.Name;
									dlg.LastTable[rowCntr, 5] = TextFactory.CreateText(lastColPos, 0, textToShow, DialogAlignment.Align_Left, DialogAlignment.Valign_Center);
									//prepare next offset:
									lastColPos += spaceLength + ImprovedDialog.TextLength(textToShow);
								}
							}
						}
					}
				}
				dlg.LastTable[rowCntr, 2] = TextFactory.CreateText(elem.Name, DialogAlignment.Align_Center, DialogAlignment.Valign_Center);

				if (i > 0) {//item can be moved up only if it is not the first one
					dlg.LastTable[rowCntr, 3] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonSortUp, 4 * i + 11, DialogAlignment.Valign_Center); //v seznamu posunout nahoru
				}
				if (i < maxIndex) {//similarly with the last one
					dlg.LastTable[rowCntr, 3] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonSortDown, 0, ButtonFactory.D_SORTBUTTON_LINE_OFFSET, 4 * i + 12, DialogAlignment.Valign_Center); //v seznamu posunout dolu
				}
				rowCntr++;
			}
			dlg.MakeLastTableTransparent(); //zpruhledni zbytek dialogu

			if (dlg.LastTable.RowCount > rowCntr) {//there were less rows displayed than was prepared to display
				dlg.LastTable.RowCount = rowCntr; //(this can occur when displaying the last page of the dialog and not all items are to be shown)
			}

			dlg.MakeLastTableTransparent(); //zpruhledni zbytek dialogu

			//ted paging
			dlg.CreatePaging(cat.contents.Count, firstiVal, 1);

			dlg.AddTable(new GUTATable(1, ButtonFactory.D_BUTTON_WIDTH, ImprovedDialog.ICON_WIDTH, 160, ButtonFactory.D_BUTTON_WIDTH, 150, 0, ButtonFactory.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonOK, 4); //start making
			dlg.LastTable[0, 1] = InputFactory.CreateInput(LeafComponentTypes.InputNumber, 5); //how many to make of the selected items
			dlg.LastTable[0, 2] = TextFactory.CreateLabel("Množství k vyrobení");
			dlg.LastTable[0, 3] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonBack, 6); //next time open here
			dlg.LastTable[0, 4] = TextFactory.CreateLabel("Pøíštì otevøít zde");
			dlg.LastTable[0, 5] = TextFactory.CreateLabel("Zrušit otevírání");
			dlg.LastTable[0, 6] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonCross, 7); //discard saved position
			dlg.MakeLastTableTransparent();

			dlg.WriteOut();
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			CraftmenuCategory cat = (CraftmenuCategory)args[0];
			int btnNo = (int)gr.pressedButton;
			if (btnNo < 10) {//basic buttons
				Gump newGi;
				switch (btnNo) {
					case 0: //exit
						//DialogStacking.ShowPreviousDialog(gi); //zobrazit pripadny predchozi dialog
						break;
					case 1: //one category back (if any)
						if (cat.Parent != null) {
							newGi = gi.Cont.Dialog(SingletonScript<D_Craftmenu>.Instance, new DialogArgs(cat.Parent));
							//DialogStacking.EnstackDialog(gi, newGi);
						} else {
							gi.Cont.SysMessage("Nelze se vrátit na pøedchozí kategorii, souèasná je v hierarchii nejvýše!");
							DialogStacking.ResendAndRestackDialog(gi);
						}
						break;
					case 2: //new subcategory
						newGi = gi.Cont.Dialog(SingletonScript<D_Input_CraftmenuNewSubcat>.Instance, new DialogArgs(cat));
						DialogStacking.EnstackDialog(gi, newGi);
						break;
					case 3: //new items to add (target)
						gi.Cont.SetTag(tkCraftmenuLastpos, cat.FullName);//remember the category
						((Player)gi.Cont).Target(SingletonScript<Targ_Craftmenu>.Instance, cat);
						DialogStacking.ClearDialogStack(gi.Cont); //dont show any dialogs now
						break;
					case 4: //start making
						break;
					case 6: //openhere
						gi.Cont.SetTag(tkCraftmenuLastpos, cat.FullName);
						gi.Cont.SysMessage("Pozice výrobního menu nastavena na kategorii " + cat.FullName);
						DialogStacking.ResendAndRestackDialog(gi);
						break;
					case 7: //stop opening here
						gi.Cont.RemoveTag(tkCraftmenuLastpos);
						gi.Cont.SysMessage("Nastavení poslední pozice výrobního menu zrušeno");
						DialogStacking.ResendAndRestackDialog(gi);
						break;
				}
			} else {
				int btnNumber = (btnNo - 10) % 4; //on one line we have numbers 10,11,12,13 next line is 14,15,16,17 etc.
				int line = (int)((btnNo - (10+btnNumber)) / 4); //e.g. 12 - (10+2) / 4 = 0; 21 - (10+3) / 4 = 8/4 = 2; 15 - (10 + 1) / 4 = 1 etc...
				ICraftmenuElement elem = cat.contents[line];
				Gump newGi = null;
				switch (btnNumber) {
					case 0://show the subcategory contents
						newGi = gi.Cont.Dialog(SingletonScript<D_Craftmenu>.Instance, new DialogArgs((CraftmenuCategory) elem));
						//DialogStacking.EnstackDialog(gi, newGi);
						break;
					case 1://move element one position up in the list
						ICraftmenuElement prevSibling = cat.contents[line - 1];
						cat.contents[line - 1] = elem;
						cat.contents[line] = prevSibling;
						DialogStacking.ResendAndRestackDialog(gi);
						break;
					case 2://move element one position down
						ICraftmenuElement nextSibling = cat.contents[line + 1];
						cat.contents[line + 1] = elem;
						cat.contents[line] = nextSibling;
						DialogStacking.ResendAndRestackDialog(gi);
						break;
					case 3://show the Item Info
						newGi = gi.Cont.Dialog(SingletonScript<D_Craftmenu_ItemInfo>.Instance, new DialogArgs(((CraftmenuItem)elem).itemDef));
						DialogStacking.EnstackDialog(gi, newGi);
						break;
				}
			}
		}

		[Summary("Display the craftmenu categories dialog. You can use some of the crafting skills SkillName as " +
				"a parameter to display directly the particular skill Craftmenu")]
		[SteamFunction]
		public static void Craftmenu(Character self, ScriptArgs args) {
			if (args == null || args.argv == null || args.argv.Length == 0) {
				//check the possibly stored last displayed category
				string prevCat = TagMath.SGetTag(self, D_Craftmenu.tkCraftmenuLastpos);
				if (prevCat != null) {
					CraftmenuCategory oldCat = CraftmenuContents.GetCategoryByPath(prevCat);
					if (oldCat != null) {
						self.Dialog(SingletonScript<D_Craftmenu>.Instance, new DialogArgs(oldCat));
					} else {
						self.Dialog(SingletonScript<D_CraftmenuCategories>.Instance);
					}
				} else {
					self.Dialog(SingletonScript<D_CraftmenuCategories>.Instance);
				}
			} else {
				DialogArgs newArgs = new DialogArgs((CraftmenuCategory) args.argv[0]);
				self.Dialog(SingletonScript<D_Craftmenu>.Instance, newArgs);
			}
		}
	}

	[Summary("Dialog listing all available craftmenu categories (one for every crafting skill)")]
	public class D_CraftmenuCategories : CompiledGumpDef {
		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			ImprovedDialog dlg = new ImprovedDialog(this.GumpInstance);
			//pozadi    
			dlg.CreateBackground(240);
			dlg.SetLocation(70, 70);

			//nadpis tabulky
			dlg.AddTable(new GUTATable(1,0,ButtonFactory.D_BUTTON_WIDTH));
			dlg.LastTable.AddToCell(0,0,TextFactory.CreateHeadline("Kategorie výroby"));
			dlg.LastTable.AddToCell(0,1,ButtonFactory.CreateButton(LeafComponentTypes.ButtonCross,0)); //exit button
			dlg.MakeLastTableTransparent();

			GUTATable picTable = new GUTATable(8, ImprovedDialog.ICON_WIDTH, 0, ButtonFactory.D_BUTTON_WIDTH);
			picTable.RowHeight = 40;
			picTable.InnerRowsDelimited = true;
			dlg.AddTable(picTable);
			dlg.LastTable[0,0] = ImageFactory.CreateNamedImage(GumpIDs.Mortar);
			dlg.LastTable[0,1] = TextFactory.CreateLabel("Alchemy", DialogAlignment.Align_Center, DialogAlignment.Valign_Center);
			dlg.LastTable[0,2] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonTick,1, DialogAlignment.Valign_Center);

			dlg.LastTable[1, 0] = ImageFactory.CreateNamedImage(GumpIDs.Anvil);
			dlg.LastTable[1,1] = TextFactory.CreateLabel("Blacksmithing", DialogAlignment.Align_Center, DialogAlignment.Valign_Center);
			dlg.LastTable[1,2] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonTick,2, DialogAlignment.Valign_Center);

			dlg.LastTable[2, 0] = ImageFactory.CreateNamedImage(GumpIDs.Bow);
			dlg.LastTable[2,1] = TextFactory.CreateLabel("Bowcraft", DialogAlignment.Align_Center, DialogAlignment.Valign_Center);
			dlg.LastTable[2,2] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonTick,3, DialogAlignment.Valign_Center);

			dlg.LastTable[3, 0] = ImageFactory.CreateNamedImage(GumpIDs.Saw);
			dlg.LastTable[3,1] = TextFactory.CreateLabel("Carpentry", DialogAlignment.Align_Center, DialogAlignment.Valign_Center);
			dlg.LastTable[3,2] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonTick,4, DialogAlignment.Valign_Center);

			dlg.LastTable[4, 0] = ImageFactory.CreateNamedImage(GumpIDs.Cake);
			dlg.LastTable[4,1] = TextFactory.CreateLabel("Cooking", DialogAlignment.Align_Center, DialogAlignment.Valign_Center);
			dlg.LastTable[4,2] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonTick,5, DialogAlignment.Valign_Center);

			dlg.LastTable[5, 0] = ImageFactory.CreateNamedImage(GumpIDs.Scroll);
			dlg.LastTable[5,1] = TextFactory.CreateLabel("Inscription", DialogAlignment.Align_Center, DialogAlignment.Valign_Center);
			dlg.LastTable[5,2] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonTick,6, DialogAlignment.Valign_Center);

			dlg.LastTable[6, 0] = ImageFactory.CreateNamedImage(GumpIDs.SewingKit);
			dlg.LastTable[6,1] = TextFactory.CreateLabel("Tailoring", DialogAlignment.Align_Center, DialogAlignment.Valign_Center);
			dlg.LastTable[6,2] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonTick,7, DialogAlignment.Valign_Center);

			dlg.LastTable[7, 0] = ImageFactory.CreateNamedImage(GumpIDs.Tools);
			dlg.LastTable[7,1] = TextFactory.CreateLabel("Tinkering", DialogAlignment.Align_Center, DialogAlignment.Valign_Center);
			dlg.LastTable[7,2] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonTick,8, DialogAlignment.Valign_Center);

			dlg.MakeLastTableTransparent();

			dlg.WriteOut();
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			Gump newGi = null;
			switch (gr.pressedButton) {
				case 0://exit
					DialogStacking.ShowPreviousDialog(gi); //zobrazit pripadny predchozi dialog
					break;
				case 1://alchemy
					newGi = gi.Cont.Dialog(SingletonScript<D_Craftmenu>.Instance, new DialogArgs(CraftmenuContents.categoryAlchemy));
					DialogStacking.EnstackDialog(gi, newGi);
					break;
				case 2://blacksmithy
					newGi = gi.Cont.Dialog(SingletonScript<D_Craftmenu>.Instance, new DialogArgs(CraftmenuContents.categoryBlacksmithing));
					DialogStacking.EnstackDialog(gi, newGi);
					break;
				case 3://bowcraft
					newGi = gi.Cont.Dialog(SingletonScript<D_Craftmenu>.Instance, new DialogArgs(CraftmenuContents.categoryBowcraft));
					DialogStacking.EnstackDialog(gi, newGi);
					break;
				case 4://carpentry
					newGi = gi.Cont.Dialog(SingletonScript<D_Craftmenu>.Instance, new DialogArgs(CraftmenuContents.categoryCarpentry));
					DialogStacking.EnstackDialog(gi, newGi);
					break;
				case 5://cooking
					newGi = gi.Cont.Dialog(SingletonScript<D_Craftmenu>.Instance, new DialogArgs(CraftmenuContents.categoryCooking));
					DialogStacking.EnstackDialog(gi, newGi);
					break;
				case 6://inscription
					newGi = gi.Cont.Dialog(SingletonScript<D_Craftmenu>.Instance, new DialogArgs(CraftmenuContents.categoryInscription));
					DialogStacking.EnstackDialog(gi, newGi);
					break;
				case 7://tailoring
					newGi = gi.Cont.Dialog(SingletonScript<D_Craftmenu>.Instance, new DialogArgs(CraftmenuContents.categoryTailoring));
					DialogStacking.EnstackDialog(gi, newGi);
					break;
				case 8://tinkering
					newGi = gi.Cont.Dialog(SingletonScript<D_Craftmenu>.Instance, new DialogArgs(CraftmenuContents.categoryTinkering));
					DialogStacking.EnstackDialog(gi, newGi);
					break;
			}
		}
	}

	[Summary("Založení nové subkategorie v craftmenu")]
	public class D_Input_CraftmenuNewSubcat : CompiledInputDef {
		public override string Label {
			get {
				return "Nová subkategtorie";
			}
		}

		public override string DefaultInput {
			get {
				return "Jméno";
			}
		}

		public override void Response(Character sentTo, TagHolder focus, string filledText) {
			CraftmenuCategory cat = (CraftmenuCategory)GumpInstance.InputArgs[0];
			cat.contents.Add(new CraftmenuCategory(filledText, cat));
		}
	}
}