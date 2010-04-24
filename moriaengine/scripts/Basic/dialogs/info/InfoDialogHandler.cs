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
	[Summary("Class used to manage and create all necessarities for various Info dialogs.")]
	public class InfoDialogHandler : ImprovedDialog {
		private GUTATable actionTable;
		private GUTATable actualFieldTable;

		private int actualActionRow;
		private int actualFieldRow;
		private int actualFieldColumn;

		private IDataView viewCls;
		private object target;

		private bool isSettings; //settings dialog (true) / info dialog (false)

		private int fieldColumnWidth;

		//how many data fields there will be? (normally 2 but if we dont have any action buttons
		//then there will be 3
		public int REAL_COLUMNS_COUNT;

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

		[Summary("Create the wrapper instance and prepare set the instance variable for receiving dialog method calls")]
		public InfoDialogHandler(Gump dialogInstance)
			: base(dialogInstance) {
			//pairing collections (pairs IDAtaFieldViews and indexes of edit fields or buttons
			//keys - button edit field or details button index; value - related IDataFieldView for performing some action
			//these collections will be renewed on every InfoDialog instantiation (even during the paging!)
			dialogInstance.InputArgs.SetTag(D_Info.btnsIndexPairingTK, new Dictionary<int, IDataFieldView>());
			dialogInstance.InputArgs.SetTag(D_Info.editFieldsIndexPairingTK, new Dictionary<int, IDataFieldView>());
			dialogInstance.InputArgs.SetTag(D_Info.detailIndexPairingTK, new Dictionary<int, IDataFieldView>());
		}

		[Summary("Table for all of the IDataFieldViews")]
		public void CreateDataFieldsSpace(IDataView viewCls, object target) {
			this.target = target;
			this.viewCls = viewCls;

			//what type of dialog we have?
			isSettings = target is SettingsMetaCategory;

			int[] columns = new int[1 + COLS_COUNT];
			int firstFieldsColumn = 1;
			if (viewCls.GetActionButtonsCount(target) > 0) {
				REAL_COLUMNS_COUNT = COLS_COUNT; //standard number of field columns
				columns[0] = ACTION_COLUMN; //we have the action buttons column	
			} else {
				//there are no action buttons => even the first column will contain the data fields
				REAL_COLUMNS_COUNT = COLS_COUNT + 1; //one more field column
				firstFieldsColumn = 0;
			}
			for (int i = firstFieldsColumn; i <= COLS_COUNT; i++) {
				columns[i] = FieldColumn; //same width for every other datafield column
			}

			if (isSettings) {//settings dialog will have one column-headers line
				if (viewCls.GetActionButtonsCount(target) > 0) {
					AddTable(new GUTATable(1, columns[0], 0));
					//there are subcategories and fields - two header columns
					LastTable[0, 0] = GUTAText.Builder.TextLabel("Subcategories").Build();
					LastTable[0, 1] = GUTAText.Builder.TextLabel("Settings Items").Build();
				} else {
					AddTable(new GUTATable(1, 0));
					//no subcategories - one header column
					LastTable[0, 0] = GUTAText.Builder.TextLabel("Settings Items").Build();
				}
				MakeLastTableTransparent();
			}

			AddTable(new GUTATable(PAGE_ROWS, columns));
			MakeLastTableTransparent();

			//now add subtables to every defined column... first- action table (two subcolumns - button and his label)
			if (viewCls.GetActionButtonsCount(target) > 0) { //do we have the action buttons colum ?
				actionTable = new GUTATable(PAGE_ROWS, ButtonMetrics.D_BUTTON_WIDTH, 0);
				//tbl     row            column        ...adding inner table
				LastTable.Components[0].Components[0].AddComponent(actionTable); //add to the table's-firstrow's-first column
				actionTable.NoWrite = true; //only its columns will be seen, the table itself is just virtual, for operating
				actualActionRow = 0;
				actionTable.Transparent = true;
			}
			//then other tables (3 columns - name(, button), value)
			for (int i = firstFieldsColumn; i <= COLS_COUNT; i++) {
				GUTATable actualTbl = new GUTATable(PAGE_ROWS, FIELD_LABEL, ButtonMetrics.D_BUTTON_WIDTH, 0);
				//tbl     row           column            ...adding inner table
				LastTable.Components[0].Components[i].AddComponent(actualTbl); //first row - i-th column
				actualTbl.Transparent = true;
				actualTbl.NoWrite = true;//also not for writing out!
			}							//(dialogtable first row     first fields column          his inner table
			actualFieldTable = (GUTATable) LastTable.Components[0].Components[firstFieldsColumn].Components[0];
			actualFieldRow = 0; //row counter
			actualFieldColumn = firstFieldsColumn; //column counter (we have REAL_COLUMNS_COUNT columns to write to)
		}

		[Summary("Write a single DataField to the dialog. Target is the infoized object - we will use it to get the proper values of displayed fields")]
		public void WriteDataField(IDataFieldView field, object target, ref int buttonsIndex, ref int editsIndex, ref int detailsIndex) {
			if (field.IsButtonEnabled) { //buttonized field - we need the button index
				actionTable[actualActionRow, 0] = CreateInfoInnerButton(ref buttonsIndex, field);
				actionTable[actualActionRow, 1] = GUTAText.Builder.TextLabel(field.GetName(target)).Build();
				actualActionRow++;
				return;
			}

			//first column holds the type information in brackets() and the name of the field
			actualFieldTable[actualFieldRow, 0] = GUTAText.Builder.TextLabel(GetFieldName(field, target)).Build();
			
			object fieldValue = field.GetValue(target);
			Type fieldValueType = null;
			string thirdColumnText = "";
			bool thirdColumnIsText; //third column is editable or just simple text
			bool secondColIsButton; //second column is with or without button

			if (fieldValue == null) {
				thirdColumnText = "null";
				if (ObjectSaver.IsSimpleSaveableType(field.FieldType)) {
					thirdColumnIsText = field.ReadOnly;
					if (typeof(Enum).IsAssignableFrom(field.FieldType)) {
						secondColIsButton = !field.ReadOnly; //editable enum will have button						
					} else {
						secondColIsButton = false; //everything other will be controlled without button
					}
				} else {
					secondColIsButton = false; //no button here (there is null value by now - we cannot edit that)
					if (ObjectSaver.IsSimpleSaveableOrCoordinated(field.FieldType)) {
						thirdColumnIsText = field.ReadOnly;//it can be editable directly (e.g #someUID etc.)						
					} else {
						thirdColumnIsText = true; //can be modified via the button, but not directly!
					}
				}
			} else {
				fieldValueType = fieldValue.GetType();
				if (ObjectSaver.IsSimpleSaveableType(fieldValueType)) {
					thirdColumnIsText = field.ReadOnly;
					if (typeof(Enum).IsAssignableFrom(field.FieldType)) {
						thirdColumnText = Enum.GetName(field.FieldType, fieldValue);
						if (thirdColumnText == null) { 
							thirdColumnText = Convert.ToInt64(fieldValue).ToString();
						}
						secondColIsButton = !field.ReadOnly; //editable enum will have button						
					} else {
						thirdColumnText = field.GetStringValue(target);
						secondColIsButton = false; //everything other will be controlled without button
					}
				} else {
					secondColIsButton = true; //button is present everytime
					if (ObjectSaver.IsSimpleSaveableOrCoordinated(fieldValueType)) {
						thirdColumnText = field.GetStringValue(target);
						thirdColumnIsText = field.ReadOnly;//it can be editable directly (e.g #someUID etc.)						
					} else {
						thirdColumnText = field.GetName(target); //just informative label
						thirdColumnIsText = true; //can be modified via the button, but not directly!
					}
				}
			}

			//now fill second and third columns
			if (secondColIsButton) {
				actualFieldTable[actualFieldRow, 1] = CreateInfoInnerButton(ref buttonsIndex, field);
			}
			//if the value to be written to the text column is too long, we will add a button for value's detail display
			if (ImprovedDialog.TextLength(thirdColumnText) > FieldColumn - (FIELD_LABEL + ButtonMetrics.D_BUTTON_WIDTH + 2*ImprovedDialog.D_COL_SPACE) ) { //whole single column includes label (,button), field value - we need only the value
				Dictionary<int, IDataFieldView> detailBtnsPairing = (Dictionary<int, IDataFieldView>) instance.InputArgs.GetTag(D_Info.detailIndexPairingTK);
				actualFieldTable[actualFieldRow, 2] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonPaper).Id(detailsIndex).Build();
				//now the shortened text (non editable - this will be done usnig the new button...
				actualFieldTable[actualFieldRow, 2] = GUTAText.Builder.Text(thirdColumnText.Substring(0, 10) + "...").XPos(ButtonMetrics.D_BUTTON_WIDTH).Build();
				detailBtnsPairing.Add(detailsIndex, field);
				detailsIndex++;
			} else { //dispaly it normally
				if (thirdColumnIsText) {
					actualFieldTable[actualFieldRow, 2] = GUTAText.Builder.Text(thirdColumnText).Build();
				} else {
					actualFieldTable[actualFieldRow, 2] = GUTAInput.Builder.Id(editsIndex).Text(thirdColumnText).Build();
					//store the field under the edits index
					Dictionary<int, IDataFieldView> editFieldsPairing = (Dictionary<int, IDataFieldView>) instance.InputArgs.GetTag(D_Info.editFieldsIndexPairingTK);
					editFieldsPairing.Add(editsIndex, field);
					editsIndex++;
				}
			}
			actualFieldRow++;

			//after adding the data field, check whether we haven't reached the last line in the column
			//if so, check also if there are more columns to write to and prepare another one for writing
			if (actualFieldRow == ImprovedDialog.PAGE_ROWS && actualFieldColumn < COLS_COUNT) {
				actualFieldRow = 0;
				actualFieldColumn++;
				//next column to write to (indexing is from 0 and actualFieldColumn starts at 1 so this is correct)
				//Lasttable - the main table containing action buttons columns and COLS_COUNT odf columns for datafields
				//every column for datfields contain the GUTATable for writing fields to -
				//column					table         1st row         desired column               the table inside
				actualFieldTable = (GUTATable) LastTable.Components[0].Components[actualFieldColumn].Components[0];
			}
		}

		[Summary("Create paging for the Info dialog - it looks a little bit different than normal paging")]
		public void CreatePaging(IDataView viewCls, object target, int firstItemButt, int firstItemFld) {
			int buttonLines = viewCls.GetActionButtonsCount(target); //number of action buttons
			int fieldLines = viewCls.GetFieldsCount(target); //number of fields didvided by number of columns per page
			int maxLines = Math.Max(buttonLines, fieldLines);
			//deciding on where is more items, get the determining first item position (we will count the page number from it)
			//shortly - we are paging either according to the buttons (of there are more buttons) or fields (if there are more fields...)
			int pageDeterminingFirstItem = (buttonLines > fieldLines) ? firstItemButt : firstItemFld;
			//more buttons than fields in all columns? - the number of buttons will be the director of paging (or it will be the opposite way)
			int columnsForPagingCreate = (buttonLines > fieldLines) ? 1 : REAL_COLUMNS_COUNT;
			//do we need the paging at all?
			if (maxLines <= ImprovedDialog.PAGE_ROWS * columnsForPagingCreate) {
				//no...
				return;
			}

			int pagesCount = (int) Math.Ceiling((double) maxLines / (ImprovedDialog.PAGE_ROWS * columnsForPagingCreate));
			int actualPage = (pageDeterminingFirstItem / (ImprovedDialog.PAGE_ROWS * columnsForPagingCreate)) + 1;

			bool prevNextColumnAdded = false; //indicator of navigating column
			//last column					//the inner table's first row (the inner table is in the 1st row in the COLS_COUNT-th column
			//									tbl        1st row		 COLS_COUNTth column	inner table	   its first row...
			GUTARow pagingTableRow = (GUTARow) LastTable.Components[0].Components[COLS_COUNT].Components[0].Components[0];
			GUTAColumn pagingCol = new GUTAColumn(ButtonMetrics.D_BUTTON_PREVNEXT_WIDTH);
			pagingCol.IsLast = true;
			if (actualPage > 1) {
				pagingTableRow.AddComponent(pagingCol);

				pagingCol.AddComponent(GUTAButton.Builder.Type(LeafComponentTypes.ButtonPrev).Id(ID_PREV_BUTTON).Build()); //prev
				prevNextColumnAdded = true; //the column has been created				
			}
			if (actualPage < pagesCount) { //there will be a next page
				if (!prevNextColumnAdded) { //the navigating column does not exist (e.g. we are on the 1st page)
					pagingTableRow.AddComponent(pagingCol);
				}
				pagingCol.AddComponent(GUTAButton.Builder.Type(LeafComponentTypes.ButtonNext).YPos(pagingCol.Height - 21).Id(ID_NEXT_BUTTON).Build()); //next
			}
			//MakeLastTableTransparent(); //the row where we added the navigating column
			//add a navigating bar to the bottom (editable field for jumping to the selected page)
			//it looks like this: "Str�nka |__| / 23. <GOPAGE>  where |__| is editable field
			//and <GOPAGE> is confirming button that jumps to the written page.
			GUTATable storedLastTable = LastTable; //store this one :)

			AddTable(new GUTATable(1, 0));
			LastTable[0, 0] = GUTAText.Builder.TextLabel("Str�nka").Build();
			//type if input,x,y,ID, width, height, prescribed text
			LastTable[0, 0] = GUTAInput.Builder.Type(LeafComponentTypes.InputNumber).XPos(65).Id(ID_PAGE_NO_INPUT).Width(30).Text(actualPage.ToString()).Build();
			LastTable[0, 0] = GUTAText.Builder.TextLabel("/" + pagesCount.ToString()).XPos(95).Build();
			LastTable[0, 0] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonOK).XPos(135).Id(ID_JUMP_PAGE_BUTTON).Build();
			MakeLastTableTransparent(); //newly created row
			//restore the last components
			lastTable = storedLastTable;
		}

		[Summary("Check the gump response for the pressed button number and if it is one of the paging buttons, do something")]
		public static bool PagingHandled(Gump gi, GumpResponse gr) {
			DialogArgs args = gi.InputArgs;//arguments of the dialog		
			object target = args[0];
			IDataView viewCls = DataViewProvider.FindDataViewByType(target.GetType());
			int buttonCount = viewCls.GetActionButtonsCount(target);
			int fieldsCount = viewCls.GetFieldsCount(target);
			//how many columns for fields do we have?
			int fieldsColumnsCount = (buttonCount > 0) ? COLS_COUNT : (COLS_COUNT + 1);
			bool pagingHandled = false; //indicator if the pressed button was the paging one.
			switch (gr.PressedButton) {
				case ID_PREV_BUTTON:
					//set the first indexes one page to the back
					args.SetTag(D_Info.pagingFieldsTK, TagMath.IGetTag(args, D_Info.pagingFieldsTK) - PAGE_ROWS * fieldsColumnsCount);
					DialogStacking.ResendAndRestackDialog(gi);
					pagingHandled = true;
					break;
				case ID_NEXT_BUTTON:
					//set the first indexes one page forwards
					args.SetTag(D_Info.pagingFieldsTK, TagMath.IGetTag(args, D_Info.pagingFieldsTK) + PAGE_ROWS * fieldsColumnsCount);
					DialogStacking.ResendAndRestackDialog(gi);
					pagingHandled = true;
					break;
				case ID_JUMP_PAGE_BUTTON:
					//get the selected page number (absolute value - make it a bit idiot proof :) )
					int selectedPage = (int) gr.GetNumberResponse(ID_PAGE_NO_INPUT);
					if (selectedPage < 1) {
						//idiot proof adjustment
						gi.Cont.WriteLine("Nepovolen� ��slo str�nky - povoleny jen kladn� hodnoty");
						selectedPage = 1;
					}
					//count the index of the first item
					int newFirstFldIndex = (selectedPage - 1) * (PAGE_ROWS * fieldsColumnsCount);
					if (newFirstFldIndex > fieldsCount) {
						int lastPage = (fieldsCount / (PAGE_ROWS * fieldsColumnsCount)) + 1; //(int) casted last page number
						newFirstFldIndex = (lastPage - 1) * PAGE_ROWS * fieldsColumnsCount; //counted first item on the last page
					}
					args.SetTag(D_Info.pagingFieldsTK, newFirstFldIndex);//set the index of the first field
					DialogStacking.ResendAndRestackDialog(gi);
					pagingHandled = true;
					break;
			}
			return pagingHandled;
		}

		[Summary("Create a inside-info dialog button (buttons in the columns). Increase the button index and store the field in the map")]
		private GUTAComponent CreateInfoInnerButton(ref int buttonsIndex, IDataFieldView field) {
			GUTAComponent retBut = GUTAButton.Builder.Id(buttonsIndex).Build();
			//store the field under the edits index
			Dictionary<int, IDataFieldView> buttonsPairing = (Dictionary<int, IDataFieldView>) instance.InputArgs.GetTag(D_Info.btnsIndexPairingTK);
			buttonsPairing.Add(buttonsIndex, field);
			buttonsIndex++;
			return retBut;
		}

		[Summary("Return the fields name accompanied with the type information (but sometimes we dont need the type info...)")]
		private static string GetFieldName(IDataFieldView field, object target) {
			object fieldVal = field.GetValue(target);
			string retName = field.GetName(target);
			if (typeof(Enum).IsAssignableFrom(target.GetType())) { } //target is Enum (e.g when infoizing the Enum itself and displaying its items)
			 else if (typeof(Enum).IsAssignableFrom(field.FieldType)) { }//field is of type Enum
			  else {
				retName += SettingsProvider.GetTypePrefix(field.FieldType);
			}
			return retName;
		}

		private int FieldColumn {
			get {
				if (fieldColumnWidth == 0) {//lazily initialized value column width
					if (viewCls.GetActionButtonsCount(target) > 0) {
						fieldColumnWidth = (INFO_WIDTH - //complete dialog width
								ACTION_COLUMN - //action button column
								2 * (ImprovedDialog.D_BORDER + ImprovedDialog.D_SPACE + ImprovedDialog.D_ROW_SPACE) - //left and right tables delimits
											 (COLS_COUNT - 1) * ImprovedDialog.D_COL_SPACE) //one less columns delimit than there are columns
											 / COLS_COUNT;//divide by number of desired data columns
					} else { //no button column...
						fieldColumnWidth = (INFO_WIDTH - //complete dialog width							
								2 * (ImprovedDialog.D_BORDER + ImprovedDialog.D_SPACE + ImprovedDialog.D_ROW_SPACE) - //left and right tables delimits
									 (REAL_COLUMNS_COUNT - 1) * ImprovedDialog.D_COL_SPACE) //one less columns delimit than there are columns
									 / REAL_COLUMNS_COUNT;//divide by number of desired data columns
					}
				}
				return fieldColumnWidth;
			}
		}

		public const int INFO_WIDTH = 1000; //width of the info dialog
		public const int COLS_COUNT = 2; //one column is always reserved for action buttons, two columns for other fields
		private const int ACTION_COLUMN = 180; //action column width
		private const int FIELD_LABEL = 150; //label of the field column										 
	}
}