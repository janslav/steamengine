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
using System.Diagnostics;

namespace SteamEngine.CompiledScripts.Dialogs {

	[Summary("Class that will display the info dialog")]
	public class D_Info : CompiledGumpDef {
		internal static TagKey infoizedTargT = TagKey.Get("_info_target_");
		internal static TagKey pagingButtonsTK = TagKey.Get("_paging_buttons_");
		internal static TagKey pagingFieldsTK = TagKey.Get("_paging_fields_");
		internal static TagKey btnsIndexPairingTK = TagKey.Get("_button_index_pairing_");
		internal static TagKey editFieldsIndexPairingTK = TagKey.Get("_edit_fields_index_pairing_");

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			object target = args.ArgsArray[0];//target of info dialog

			//first argument is the object being infoized - we will get its DataView first
			IDataView viewCls = DataViewProvider.FindDataViewByType(target.GetType());
			int firstItemButt = TagMath.IGetTag(args, D_Info.pagingButtonsTK);//buttons paging 1st item index
			int firstItemFld = TagMath.IGetTag(args, D_Info.pagingFieldsTK);//fields paging 1st item index

			InfoDialogHandler dlg = new InfoDialogHandler(this.GumpInstance);
			dlg.CreateBackground(InfoDialogHandler.INFO_WIDTH);
			dlg.SetLocation(50, 50);
			int innerWidth = InfoDialogHandler.INFO_WIDTH - 2 * ImprovedDialog.D_BORDER - 2 * ImprovedDialog.D_SPACE;

			string headline;
			//decide the headline according to the dialog type (info/setting)
			if (target is SettingsMetaCategory) {
				headline = "Settings dialog " + (viewCls == null ? "" : " - " + viewCls.GetName(target));
			} else {
				headline = "Info dialog" + (viewCls == null ? "" : " - " + viewCls.GetName(target));
			}

			dlg.AddTable(new GUTATable(1, innerWidth - 2 * ButtonFactory.D_BUTTON_WIDTH - ImprovedDialog.D_COL_SPACE, 0, ButtonFactory.D_BUTTON_WIDTH));
			//the viewCls could be null ! - e.g. DataView does not exist
			dlg.LastTable[0, 0] = TextFactory.CreateHeadline(headline);
			dlg.LastTable[0, 1] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonPaper, 2);
			dlg.LastTable[0, 2] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonCross, 0);
			dlg.MakeLastTableTransparent();

			//no data - no dialog necessary
			if (viewCls == null) {
				dlg.AddTable(new GUTATable(1, 0));
				dlg.LastTable[0, 0] = TextFactory.CreateLabel("No DataView found for the given type " + target.GetType());
				dlg.MakeLastTableTransparent();
				dlg.WriteOut();
				return;
			}

			dlg.CreateDataFieldsSpace(viewCls, target);

			int buttonsIndex = 10; //start counting buttons from 10
			int editsIndex = 10; //start counting editable input fields also from 10

			int finishIndex = firstItemFld + dlg.REAL_COLUMNS_COUNT * ImprovedDialog.PAGE_ROWS;
			int counter = firstItemFld;
			foreach (IDataFieldView field in viewCls.GetDataFieldsPage(firstItemFld, target)) {
				//add both indexing params - the buttons index will be used (and raised) when the field is Button or 
				//ReadWrite or ReadOnly field with type that itself has the DataView implemented (and can be infoized)
				// - the edits index will be used for input fields in ReadWrite field case
				dlg.WriteDataField(field, target, ref buttonsIndex, ref editsIndex);
				//check if we should continue
				counter++;
				if (counter == finishIndex)
					break;
			}

			//now write the single page of action buttons (one column - normal rowcount)
			finishIndex = firstItemButt + ImprovedDialog.PAGE_ROWS;
			counter = firstItemButt;
			foreach (ButtonDataFieldView button in viewCls.GetActionButtonsPage(firstItemButt, target)) {
				dlg.WriteDataField(button, target, ref buttonsIndex, ref editsIndex);
				//check if we should continue
				counter++;
				if (counter == finishIndex)
					break;
			}

			//now handle the paging 
			dlg.CreatePaging(viewCls, target, firstItemButt, firstItemFld);

			//send button
			dlg.AddTable(new GUTATable(1, ButtonFactory.D_BUTTON_WIDTH, 0));
			dlg.LastTable[0, 0] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonOK, 1);
			dlg.LastTable[0, 1] = TextFactory.CreateText("Uložit");
			dlg.MakeLastTableTransparent();

			dlg.WriteOut();
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			object target = args.ArgsArray[0];//target of info dialog

			if (gr.pressedButton < 10) { //basic dialog buttons (close, info, store)
				switch (gr.pressedButton) {
					case 0: //exit
						DialogStacking.ShowPreviousDialog(gi); //zobrazit pripadny predchozi dialog
						break;
					case 1: //store
						Dictionary<int, IDataFieldView> editFieldsPairing = (Dictionary<int, IDataFieldView>) args.GetTag(D_Info.editFieldsIndexPairingTK);

						List<SettingResult> reslist = SettingsProvider.AssertSettings(editFieldsPairing, gr, target);
						DialogStacking.ResendAndRestackDialog(gi);
						if (reslist.Count > 0) {
							//show the results dialog (if there is any change)
							DialogArgs newArgs = new DialogArgs();
							newArgs.SetTag(D_Settings_Result.resultsListTK, reslist); //list of settings resluts
							gi.Cont.Dialog(SingletonScript<D_Settings_Result>.Instance, newArgs);
						}
						break;
					case 2: //info
						Gump newGi = gi.Cont.Dialog(SingletonScript<D_Settings_Help>.Instance);
						DialogStacking.EnstackDialog(gi, newGi); //stack self for return						
						break;
				}
			} else if (InfoDialogHandler.PagingHandled(gi, gr)) {
				//kliknuto na paging? 
				return;
			} else { //info dialog buttons
				Dictionary<int, IDataFieldView> btnsPairing = (Dictionary<int, IDataFieldView>) args.GetTag(D_Info.btnsIndexPairingTK);

				//get the IDataFieldView and do something
				IDataFieldView idfv = (IDataFieldView) btnsPairing[(int) gr.pressedButton];

				if (idfv.IsButtonEnabled) {
					DialogStacking.ResendAndRestackDialog(gi);
					//action button field - call the method
					((ButtonDataFieldView) idfv).OnButton(target);
				} else {
					object fieldValue = idfv.GetValue(target);
					Type fieldValueType = null;
					if (fieldValue != null) {
						fieldValueType = fieldValue.GetType();
					}
					if (fieldValueType != null) {
						Gump newGi = gi.Cont.Dialog(SingletonScript<D_Info>.Instance, new DialogArgs(idfv.GetValue(target)));
						DialogStacking.EnstackDialog(gi, newGi); //store						
						//display info dialog on this datafield
					} else {
						throw new SEException("Null value can't be viewed");
					}
				}
			}
		}

		[Summary("Display an info dialog. Function accessible from the game." +
				"The function is designed to be triggered using .x info" +
				"it can be used also normally .info to display runner's own info dialog" +
				"finally - we can use it also like .info(obj) to display the info about obj")]
		[SteamFunction]
		public static void Info(object self, ScriptArgs args) {
			if (args.argv == null || args.argv.Length == 0) {
				//display it normally (targetted or for self)
				Globals.SrcCharacter.Dialog(SingletonScript<D_Info>.Instance, new DialogArgs(self));
			} else {
				//get the arguments to be sent to the dialog (especialy the first one which is the 
				//desired object for infoizing)
				Globals.SrcCharacter.Dialog(SingletonScript<D_Info>.Instance, new DialogArgs(args.argv[0]));
			}
		}

		[Summary("Display a settings dialog. Function accessible from the game." +
				"The function is designed to be triggered using .x settings, but it will be" +
				"mainly used from the SettingsCategories dialog on a various buttons")]
		[SteamFunction]
		public static void Settings(object self, ScriptArgs args) {
			if (args.argv == null || args.argv.Length == 0) {
				//call the default settings dialog
				Globals.SrcCharacter.Dialog(SingletonScript<D_Info>.Instance, new DialogArgs(SettingsCategories.instance));
			} else {
				//get the arguments to be sent to the dialog (especialy the first one which is the 
				//desired object for infoizing)
				Globals.SrcCharacter.Dialog(SingletonScript<D_Info>.Instance, new DialogArgs(args.argv[0]));
			}
		}

		[SteamFunction]
		public static void Inf(object self, ScriptArgs args) {
			Globals.SrcCharacter.Dialog(SingletonScript<D_Info>.Instance, new DialogArgs(new SimpleClass()));
		}
	}
}
