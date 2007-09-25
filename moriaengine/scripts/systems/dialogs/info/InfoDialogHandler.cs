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
using System.Text;
using System.Collections;
using System.Collections.Generic;
using SteamEngine;
using SteamEngine.Common;
using SteamEngine.CompiledScripts;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts.Dialogs {
	[Remark("Class used to manage and create all necessarities for various Info dialogs.")]
	public class InfoDialogHandler : ImprovedDialog {
		private GUTATable actionTable;
		private GUTATable actualFieldTable;

		private int actualActionRow;
		private int actualFieldRow;
		private int actualFieldColumn;

		//hashtables for button/editfield index relationing with IDataFieldViews...
		private Hashtable buttons;
		private Hashtable editFlds;

		public GUTATable ActionTable {
			get {
				return actionTable;
			}
		}

		public GUTATable ActualFieldTable {
			get {
				return actualFieldTable;
			}
		}

		[Remark("Create the wrapper instance and prepare set the instance variable for receiving dialog method calls")]
		public InfoDialogHandler(GumpInstance dialogInstance, Hashtable buttons, Hashtable editFlds) : base(dialogInstance) {
			this.buttons = buttons;
			this.editFlds = editFlds;
		}

		[Remark("Table for all of the IDataFieldViews")]
		public void CreateDataFieldsSpace() {
			int[] columns = new int[1 + COLS_COUNT];
			columns[0] = ACTION_COLUMN; //action buttons column
			for(int i = 1; i <= COLS_COUNT; i++) {
				columns[i] = FIELD_COLUMN; //same width for every other datafield column
			}
			Add(new GUTATable(PAGE_ROWS, columns));
			MakeTableTransparent();

			//now add subtables to every defined column... first- action table (two subcolumns - button and his label)
			LastTable.Components[0].AddComponent(new GUTATable(PAGE_ROWS, ButtonFactory.D_BUTTON_WIDTH, 0));
			actionTable = (GUTATable)LastTable.Components[0].Components[0]; //(dialogtable - first column - his just added inner table)
			actionTable.NoWrite = true; //only its columns will be seen, the table itself is just virtual, for operating
			actualActionRow = 0;
			actionTable.Transparent = true;
			//then other tables (3 columns - name(, button), value)
			for(int i = 1; i <= COLS_COUNT; i++) {
				LastTable.Components[i].AddComponent(new GUTATable(PAGE_ROWS, 110, ButtonFactory.D_BUTTON_WIDTH, 0));
				((GUTATable)LastTable.Components[i].Components[0]).Transparent = true;
				((GUTATable)LastTable.Components[i].Components[0]).NoWrite = true;//also not for writing out!
			}
			actualFieldTable = (GUTATable)LastTable.Components[1].Components[0];//(dialogtable - second column - his inner table)
			actualFieldRow = 0; //row counter
			actualFieldColumn = 1; //column counter (we have COLS_COUNT -1 columns to write to)
		}		

		[Remark("Write a single DataField to the dialog. Target is the infoized object - we will use it to get the proper values of displayed fields")]
		public void WriteDataField(IDataFieldView field, object target, ref int buttonsIndex, ref int editsIndex) {
			if(field.IsButtonEnabled) { //buttonized field - we need the button index
				actionTable[actualActionRow, 0] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonTick, buttonsIndex);
				actionTable[actualActionRow, 1] = TextFactory.CreateLabel(field.Name);				
				actualActionRow++;
				//store the field under the buttons index
				buttons.Add(buttonsIndex, field);
				buttonsIndex++; //raise the button index - and prepare it for other buttons
				return;
			} else if(!field.ReadOnly) { //editable label-value field - we need the edit index and probably the button index!
					//first column holds the type information in brackets() and the name of the field
				actualFieldTable[actualFieldRow, 0] = TextFactory.CreateLabel(SettingsProvider.GetTypePrefix(field)+field.Name);
				//if necessary, insert the button
				bool willHaveButton = false;
				if(field.GetValue(target) != null) {
					if(!ObjectSaver.IsSimpleSaveableType(field.GetValue(target).GetType())) {
						actualFieldTable[actualFieldRow, 1] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonTick, buttonsIndex);
						//store the field under the buttons index
						buttons.Add(buttonsIndex, field);				
						buttonsIndex++;
						willHaveButton = true;
					}
				}
				//insert the input field - specify the x and y position, let the engine to compute the width and height of the component
				if(willHaveButton) {
					//non simple field (buttonized - the redirect to another info dialog is present)
					//the field will not be editable, we will therefore display only non editable label
					actualFieldTable[actualFieldRow, 2] = TextFactory.CreateText(field.Name);
				} else {
					//no buttonized edit field - it is some simple type and can be edited
					actualFieldTable[actualFieldRow, 2] = InputFactory.CreateInput(LeafComponentTypes.InputText, editsIndex, field.GetStringValue(target));
					//store the field under the edits index
					editFlds.Add(editsIndex, field);
					editsIndex++;
				}
				actualFieldRow++;				
			} else { //non-editable label-value field
				actualFieldTable[actualFieldRow, 0] = TextFactory.CreateLabel(SettingsProvider.GetTypePrefix(field) + field.Name);
				actualFieldTable[actualFieldRow, 2] = TextFactory.CreateText(field.GetStringValue(target));
				actualFieldRow++;
			}

			//after adding the data field, check whether we haven't reached the last line in the column
			//if so, check also if there are more columns to write to and prepare another one for writing
			if(actualFieldRow == ImprovedDialog.PAGE_ROWS && actualFieldColumn < COLS_COUNT) {
				actualFieldRow = 0;
				actualFieldColumn++;
				//next column to write to (indexing is from 0 and actualFieldColumn starts at 1 so this is correct)
				//Lasttable - the main table containing action buttons columns and COLS_COUNT odf columns for datafields
				//every column for datfields contain the GUTATable for writing fields to -
														//column						table inside
				actualFieldTable = (GUTATable)LastTable.Components[actualFieldColumn].Components[0];
			}
		}

		[Remark("Create paging for the Info dialog - it looks a little bit different than normal paging")]
		public void HandlePaging(IDataView viewCls, object target, int firstItemButt, int firstItemFld) {
			int buttonLines = viewCls.GetActionButtonsCount(target); //number of action buttons
			int fieldLines = viewCls.GetFieldsCount(target) / InfoDialogHandler.COLS_COUNT; //number of fields didvided by number of columns per page
			int maxLines = Math.Max(buttonLines, fieldLines);
				//more buttons than fields in all columns? - the number of buttons will be the director of paging (or it will be the opposite way)
			int columnsForPagingCreate = (buttonLines > fieldLines) ? 1 : COLS_COUNT;
			//do we need the paging at all?
			if(maxLines <= ImprovedDialog.PAGE_ROWS * columnsForPagingCreate) {
				//no...
				return;
			}

			int pagesCount = (int)Math.Ceiling((double)maxLines / (ImprovedDialog.PAGE_ROWS * columnsForPagingCreate));
			int actualPage = (firstItemButt / ImprovedDialog.PAGE_ROWS) + 1; //we can count it from the buttons column -its OK

			bool prevNextColumnAdded = false; //indicator of navigating column
											   //last column          //the inner table
			GUTATable pagingTable = (GUTATable)LastTable.Components[COLS_COUNT].Components[0];
			GUTAColumn pagingCol = new GUTAColumn(ButtonFactory.D_BUTTON_PREVNEXT_WIDTH);
			pagingCol.IsLast = true;					
			if(actualPage > 1) {
				pagingTable.AddComponent(pagingCol);

				pagingCol.AddComponent(ButtonFactory.CreateButton(LeafComponentTypes.ButtonPrev, ID_PREV_BUTTON)); //prev
				prevNextColumnAdded = true; //the column has been created				
			}
			if(actualPage < pagesCount) { //there will be a next page
				if(!prevNextColumnAdded) { //the navigating column does not exist (e.g. we are on the 1st page)
					pagingTable.AddComponent(pagingCol);					
				}
				pagingCol.AddComponent(ButtonFactory.CreateButton(LeafComponentTypes.ButtonNext, 0, pagingCol.Height - 21, ID_NEXT_BUTTON)); //next
			}
			//MakeTableTransparent(); //the row where we added the navigating column
			//add a navigating bar to the bottom (editable field for jumping to the selected page)
			//it looks like this: "Str�nka |__| / 23. <GOPAGE>  where |__| is editable field
			//and <GOPAGE> is confirming button that jumps to the written page.
			GUTATable storedLastTable = LastTable; //store this one :)
			
			Add(new GUTATable(1, 0));
			LastTable[0, 0] = TextFactory.CreateLabel("Str�nka");
			//type if input,x,y,ID, width, height, prescribed text
			LastTable[0, 0] = InputFactory.CreateInput(LeafComponentTypes.InputNumber, 65, 0, ID_PAGE_NO_INPUT, 30, D_ROW_HEIGHT, actualPage.ToString());
			LastTable[0, 0] = TextFactory.CreateLabel(95, 0, "/" + pagesCount.ToString());
			LastTable[0, 0] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonOK, 135, 0, ID_JUMP_PAGE_BUTTON);
			MakeTableTransparent(); //newly created row
			//restore the last components
			lastTable = storedLastTable;			
		}

		[Remark("Check the gump response for the pressed button number and if it is one of the paging buttons, do something")]
		public static bool PagingHandled(GumpInstance gi, GumpResponse gr) {
			return false;
		}

		public const int INFO_WIDTH = 800; //width of the info dialog
		public const int COLS_COUNT = 2; //one column is always for action buttons, two columns for other fields
		private const int ACTION_COLUMN = 180; //action column width
										 
		private const int FIELD_COLUMN = (INFO_WIDTH - //complete dialog width
										 ACTION_COLUMN - //first column
										 2 * (ImprovedDialog.D_BORDER + ImprovedDialog.D_SPACE + ImprovedDialog.D_ROW_SPACE) - //left and right tables delimits
										 (COLS_COUNT-1) * ImprovedDialog.D_COL_SPACE) //one less columns delimit than there are columns
										 / COLS_COUNT;//divide by number of desired data columns									 
	}
}