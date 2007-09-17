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
			///TODO - just for debugging
			target = new SimpleClass();

			//first argument is the object being infoized - we will get its DataView first
			AbstractDataView viewCls = GetAbstractDataView(target);
			int firstItem = Convert.ToInt32(args[1]);
			
			InfoDialogHandler dlg = new InfoDialogHandler(this.GumpInstance, buttons, editFlds);
			dlg.CreateBackground(InfoDialogHandler.INFO_WIDTH);
			dlg.SetLocation(50, 50);
			int innerWidth = InfoDialogHandler.INFO_WIDTH - 2 * ImprovedDialog.D_BORDER - 2 * ImprovedDialog.D_SPACE;

			dlg.Add(new GUTATable(1, innerWidth - 2 * ButtonFactory.D_BUTTON_WIDTH - ImprovedDialog.D_COL_SPACE, 0, ButtonFactory.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = TextFactory.CreateHeadline("Info dialog - " + viewCls.Name);
			dlg.LastTable[0, 1] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonPaper, 2);
			dlg.LastTable[0, 2] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonCross, 0);
			dlg.MakeTableTransparent();

			dlg.CreateDataFieldsSpace();

			int buttonsIndex = 10; //start counting buttons from 10
			int editsIndex = 10; //start counting editable input fields also from 10

			//first get the single page of data fields (we use COLS_COUNT columns for them)
			
			foreach(IDataFieldView field in ((IPageableCollection<IDataFieldView>)viewCls).GetPage(firstItem, InfoDialogHandler.COLS_COUNT * ImprovedDialog.PAGE_ROWS)) {
				//add both indexing params - the buttons index will be used (and raised) when the field is Button or 
				//ReadWrite or ReadOnly field with type that itself has the DataView implemented (and can be infoized)
				// - the edits index will be used for input fields in ReadWrite field case
				dlg.WriteDataField(field, target, buttonsIndex, editsIndex);
			}

			//now write the single page of action buttons (one column - normal rowcount)
			foreach(ButtonDataFieldView button in ((IPageableCollection<ButtonDataFieldView>)viewCls).GetPage(firstItem, ImprovedDialog.PAGE_ROWS)) {
				dlg.WriteDataField(button, target, buttonsIndex, editsIndex);
			}

			//now handle the paging 
			//dlg.CreatePaging(playersList.Count, firstiVal, 1);

			//send button
			dlg.Add(new GUTATable(1, ButtonFactory.D_BUTTON_WIDTH, 0));
			dlg.LastTable[0, 0] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonOK, 1);
			dlg.LastTable[0, 1] = TextFactory.CreateText("Uložit");
			dlg.MakeTableTransparent();

			dlg.WriteOut();
		}

		public override void OnResponse(GumpInstance gi, GumpResponse gr, object[] args) {
			object target = args[0];
			///TODO - just for debugging
			target = new SimpleClass();

			if(gr.pressedButton < 10) { //basic dialog buttons (close, info, store)
				switch(gr.pressedButton) {
					case 0: //exit
						DialogStackItem.ShowPreviousDialog(gi.Cont.Conn); //zobrazit pripadny predchozi dialog
						break;
					case 1: //store
						/*
						//TryMakeSetting(valuesToSet, gr);
						//args[4] = valuesToSet; //predame si seznam hodnot v dialogu pro pozdejsi pripadny navrat
						*/
						///TODO - udelat nejake to nastavenicko...; bude hotovo az udelam novej dialog k nastaveni (bude se to delat stejne)
						///a bude to prizpusobeny tomu dialog nastaveni, tj proto pockam
						gi.Cont.SendGump(gi);//resend the dialog
						/*
						//a zobrazime take dialog s vysledky (null = volne misto pro seznamy resultu v nasledujicim dialogu)
						gi.Cont.Dialog(D_Settings_Result.Instance, 0, valuesToSet, null);
						*/
						break;
					case 2: //info
						DialogStackItem.EnstackDialog(gi); //stack self for return
						gi.Cont.Dialog(SingletonScript<D_Settings_Help>.Instance);
						break;
				}
				///TODO - pagingove cudlicky
			/*} else if(ImprovedDialog.PagingButtonsHandled(gi, gr, 1, playersList.Count, 1)) {
				//kliknuto na paging? (1 = index parametru nesoucim info o pagingu (zde dsi.Args[1] viz výše)				
				//posledni 1 - pocet sloupecku v dialogu
				return;
			 */
			} else { //info dialog buttons
				//get the IDataFieldView and do something
				IDataFieldView idfv = (IDataFieldView)buttons[Convert.ToInt32(gr.pressedButton)];
				if(idfv.IsButtonEnabled) { 
					//action button field - call the method
					((ButtonDataFieldView)idfv).OnButton(target);
				} else if(!idfv.ReadOnly) {
					//normal editable field but with button - it will redirect to another info dialog...
					DialogStackItem.EnstackDialog(gi); //store
					gi.Cont.Dialog(SingletonScript<D_Info>.Instance, idfv.GetValue(target)); //display info dialog on this datafield
				}
			}
		}

		[Remark("Method for finding the AbstractDataView for given infoized object")]
		private AbstractDataView GetAbstractDataView(object target) {
			///TODO - prozatim takto, pozdeji vzit dle typu targetu z globalni hashtable
			return new GeneratedDataView_SimpleClass();
		}

		[Remark("Display an info dialog. Function accessible from the game." +
				"The function is designed to be triggered using .x info" +
				"it can be used also normally .info to display runner's own info dialog"+
				"finally - we can use it also like .info(obj) to display the info about obj")]
		[SteamFunction]
		public static void Info(object self, ScriptArgs args) {
			if(args.Argv == null || args.Argv.Length == 0) {
				//display it normally (targetted or for self)
				Globals.SrcCharacter.Dialog(SingletonScript<D_Info>.Instance, self, 0);
			} else {
				//get the arguments to be sent to the dialog (especialy the first one which is the 
				//desired object for infoizing)
				Globals.SrcCharacter.Dialog(SingletonScript<D_Info>.Instance, args.Argv[0], 0);
			}
		}
	}	
}