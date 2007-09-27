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
using SteamEngine.LScript;

namespace SteamEngine.CompiledScripts.Dialogs {

	[Remark("Class that will display the info dialog")]
	public class D_Info : CompiledGump {
		//keys - button or edit field index; value - related IDataFieldView for performing some action
		private Hashtable buttons;
		private Hashtable editFlds;

		public override void Construct(Thing focus, AbstractCharacter sendTo, object[] args) {
			buttons = new Hashtable();
			editFlds = new Hashtable();

			object target = args[0];
			
			//first argument is the object being infoized - we will get its DataView first
			IDataView viewCls = DataViewProvider.FindDataViewByType(target.GetType());
			int firstItemButt = Convert.ToInt32(args[1]);
			int firstItemFld = Convert.ToInt32(args[2]);
			
			InfoDialogHandler dlg = new InfoDialogHandler(this.GumpInstance, buttons, editFlds);
			dlg.CreateBackground(InfoDialogHandler.INFO_WIDTH);
			dlg.SetLocation(50, 50);
			int innerWidth = InfoDialogHandler.INFO_WIDTH - 2 * ImprovedDialog.D_BORDER - 2 * ImprovedDialog.D_SPACE;

			dlg.Add(new GUTATable(1, innerWidth - 2 * ButtonFactory.D_BUTTON_WIDTH - ImprovedDialog.D_COL_SPACE, 0, ButtonFactory.D_BUTTON_WIDTH));
				//the viewCls could be null ! - e.g. DataView does not exist
			dlg.LastTable[0, 0] = TextFactory.CreateHeadline("Info dialog" + (viewCls == null ? "" : " - "+viewCls.GetName(target)));
			dlg.LastTable[0, 1] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonPaper, 2);
			dlg.LastTable[0, 2] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonCross, 0);
			dlg.MakeTableTransparent();

			//no data - no dialog necessary
			if (viewCls == null) {
				dlg.Add(new GUTATable(1,0));
				dlg.LastTable[0, 0] = TextFactory.CreateLabel("No DataView found for the given type " + target.GetType());
				dlg.MakeTableTransparent();
				dlg.WriteOut();
				return;
			}

			dlg.CreateDataFieldsSpace(viewCls, target);

			int buttonsIndex = 10; //start counting buttons from 10
			int editsIndex = 10; //start counting editable input fields also from 10

			int finishIndex = firstItemFld + dlg.REAL_COLUMNS_COUNT * ImprovedDialog.PAGE_ROWS;
			int counter = firstItemFld;
			foreach(IDataFieldView field in viewCls.GetDataFieldsPage(firstItemFld, target)) {
				//add both indexing params - the buttons index will be used (and raised) when the field is Button or 
				//ReadWrite or ReadOnly field with type that itself has the DataView implemented (and can be infoized)
				// - the edits index will be used for input fields in ReadWrite field case
				dlg.WriteDataField(field, target, ref buttonsIndex, ref editsIndex);
				//check if we should continue
				counter++;
				if(counter==finishIndex)
					break;
			}

			//now write the single page of action buttons (one column - normal rowcount)
			finishIndex = firstItemButt + ImprovedDialog.PAGE_ROWS;
			counter = firstItemButt;
			foreach(ButtonDataFieldView button in viewCls.GetActionButtonsPage(firstItemButt, target)) {
				dlg.WriteDataField(button, target, ref buttonsIndex, ref editsIndex);
				//check if we should continue
				counter++;
				if(counter == finishIndex)
					break;
			}

			//now handle the paging 
			dlg.HandlePaging(viewCls, target, firstItemButt, firstItemFld);
				
			//send button
			dlg.Add(new GUTATable(1, ButtonFactory.D_BUTTON_WIDTH, 0));
			dlg.LastTable[0, 0] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonOK, 1);
			dlg.LastTable[0, 1] = TextFactory.CreateText("Uložit");
			dlg.MakeTableTransparent();

			dlg.WriteOut();
		}

		public override void OnResponse(GumpInstance gi, GumpResponse gr, object[] args) {
			object target = args[0];			

			if(gr.pressedButton < 10) { //basic dialog buttons (close, info, store)
				switch(gr.pressedButton) {
					case 0: //exit
						DialogStackItem.ShowPreviousDialog(gi.Cont.Conn); //zobrazit pripadny predchozi dialog
						break;
					case 1: //store
						List<SettingResult> reslist = SettingsProvider.AssertSettings(editFlds, gr, target);
						gi.Cont.SendGump(gi);//resend the dialog
						if(reslist.Count > 0) {
							//show the results dialog (if there is any change)
							gi.Cont.Dialog(SingletonScript<D_Settings_Result>.Instance, 0, reslist, null);
						}
						break;
					case 2: //info
						DialogStackItem.EnstackDialog(gi); //stack self for return
						gi.Cont.Dialog(SingletonScript<D_Settings_Help>.Instance);
						break;
				}			
			} else if(InfoDialogHandler.PagingHandled(gi, gr)) {
				//kliknuto na paging? 
				return;			
			} else { //info dialog buttons
				//get the IDataFieldView and do something
				IDataFieldView idfv = (IDataFieldView)buttons[Convert.ToInt32(gr.pressedButton)];
				if(idfv.IsButtonEnabled) { 
					//action button field - call the method
					((ButtonDataFieldView)idfv).OnButton(target);
					gi.Cont.SendGump(gi);//resend the dialog
				} else if(!idfv.ReadOnly) {
					//normal editable field but with button - it will redirect to another info dialog...
					DialogStackItem.EnstackDialog(gi); //store
					gi.Cont.Dialog(SingletonScript<D_Info>.Instance, idfv.GetValue(target),0,0); //display info dialog on this datafield
				}
			}
		}

		[Remark("Display an info dialog. Function accessible from the game." +
				"The function is designed to be triggered using .x info" +
				"it can be used also normally .info to display runner's own info dialog"+
				"finally - we can use it also like .info(obj) to display the info about obj")]
		[SteamFunction]
		public static void Info(object self, ScriptArgs args) {
			if(args.Argv == null || args.Argv.Length == 0) {
				//display it normally (targetted or for self)
				Globals.SrcCharacter.Dialog(SingletonScript<D_Info>.Instance, self, 0, 0);
			} else {
				//get the arguments to be sent to the dialog (especialy the first one which is the 
				//desired object for infoizing)
				Globals.SrcCharacter.Dialog(SingletonScript<D_Info>.Instance, args.Argv[0], 0, 0);
			}
		}

		[SteamFunction]
		public static void Inf(object self, ScriptArgs args) {
			Globals.SrcCharacter.Dialog(SingletonScript<D_Info>.Instance, new SimpleClass(), 0, 0);			
		}
	}	
}
