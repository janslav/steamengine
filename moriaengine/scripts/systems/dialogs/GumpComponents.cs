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
using System.Globalization;
using System.Collections;
using System.Collections.Generic;
using SteamEngine;
using SteamEngine.Common;
using SteamEngine.CompiledScripts;

namespace SteamEngine.CompiledScripts.Dialogs {

	[Summary("Exception thrown when trying to add child to the InExtensible GUTAComponents")]
	public class GUTAComponentCannotBeExtendedException : SEException {
		public GUTAComponentCannotBeExtendedException() : base() {
		}
		public GUTAComponentCannotBeExtendedException(string s) : base(s) {
		}
		public GUTAComponentCannotBeExtendedException(LogStr s)
			: base(s) {
		}
	}

	[Summary("Exception thrown when adding a child of a forbidden type (e.g. Row directly into the TableMatrix)" +
			" omitting the Columns.")]
	public class IllegalGUTAComponentExtensionException : SEException {
		public IllegalGUTAComponentExtensionException() : base() {
		}
		public IllegalGUTAComponentExtensionException(string s) : base(s) {
		}
		public IllegalGUTAComponentExtensionException(LogStr s)
			: base(s) {
		}
	}

	[Summary("Abstract parent class of all possible GUTA components. It is implemented as a " +
			"composite design pattern - all gump parts are inserted into another part which is " +
			"inserted into another part... which is inserted into the main parent part (the whole gump)")]
	public abstract class GUTAComponent {
		[Summary("Width, height and positions are common to all basic components")]
		protected int width, height, xPos, yPos;

		[Summary("List of all components children")]
		protected List<GUTAComponent> components = new List<GUTAComponent>();
		protected Gump gump;
		protected GUTAComponent parent;
		protected int level;
		private bool noWrite = false;

		[Summary("Level of the item in the dialog - for inner purposes")]
		public int Level {
			get {
				return level;
			}
			set {
				level = value;
			}
		}

		[Summary("Will the component be written ? If false, this component won't be written (but its children will)")]
		public bool NoWrite {
			get {
				return noWrite;
			}
			set {
				noWrite = value;
			}
		}

		public List<GUTAComponent> Components {
			get {
				return components;
			}
		}

		public GUTAComponent Parent {
			get {
				return parent;
			}
			set {
				parent = value;
			}
		}

		public Gump Gump {
			get {
				return gump;
			}
			set {
				gump = value;
			}
		}

		public int Width {
			get {
				return width;
			}
			set {
				width = value;
			}
		}

		public virtual int Height {
			get {
				return height;
			}
			set {
				height = value;
			}
		}

		[Summary("Relative Y-position in the gump")]
		public int YPos {
			set {
				yPos = value;
			}
			get { //mainly for debug purposes
				return yPos;
			}
		}

		[Summary("Relative X-position in the gump")]
		public int XPos {
			set {
				xPos = value;
			}
			get { //mainly for debug purposes
				return xPos;
			}
		}

		[Summary("Prototype of the parent method - provides the basic validation of the inserted component." +
				"The rest of the insertion process is up to the Children.")]
		internal abstract void AddComponent(GUTAComponent child);

		[Summary("Abstract method of writing the gump to the LScript - typically writes the component" +
				" and then all its children (who can behave in the same way)")]
		internal abstract void WriteComponent();

		[Summary("Method called just before the component is written to the gump - allows some post processing " +
				"including various operations on the parent, for example." +
				"Defaultly is empty, but can be adapted as one wishes." +
				"By now it is used for setting the components position according to the parent's one")]
		protected virtual void OnBeforeWrite(GUTAComponent parent) {
		}

		[Summary("Method calls the WriteComponent() method on all children from the list")]
		protected void WriteChildren() {
			//first prepare all childrens size, positions etc - it is necessary to separate this from 
			//writing all children because one child can have influcence to the previous children 
			//(which must be considered before writing them)
			foreach (GUTAComponent child in components) {
				child.parent = this;
				child.gump = this.gump;
				child.OnBeforeWrite(this);
			}
			//now write them
			foreach (GUTAComponent child in components) {
				child.WriteComponent();
			}
		}

		[Summary("Method basically wraps the calling of List.Add() method, but it allows also other " +
				"processing if overriden.")]
		protected void AddNewChild(GUTAComponent child) {
			components.Add(child);//simply add to the list			
		}

		[Summary("Move the given component and all of its children a bit (add a new position coordinates " +
				"to the actual ones")]
		public void AdjustPosition(int xPart, int yPart) {
			xPos += xPart;
			yPos += yPart;
			//handle possible negative values if the argujments are negative...
			if (xPos < 0)
				xPos = 0;
			if (yPos < 0)
				yPos = 0;
			foreach (GUTAComponent child in components) {
				child.AdjustPosition(xPart, yPart);
			}
		}
	}

	[Summary("Main layout item - the table matrix. Can contain GUTATables as a direct children only")]
	public class GUTAMatrix : GUTAComponent {
		[Summary("The main table must be initialized with a valid Gump and also with the " +
				"width, we can but needn't provide the xPos and yPos. Height is computed automatically")]
		public GUTAMatrix(Gump gump, int width)
			: this(gump, 0, 0, width) {
		}

		[Summary("The main table must be initialized with a valid Gump and also with the " +
			   "width, we can but needn't provide the xPos and yPos.  Height is computed automatically")]
		public GUTAMatrix(Gump gump, int xPos, int yPos, int width) {
			this.gump = gump;
			this.xPos = xPos;
			this.yPos = yPos;
			this.height = height;
			this.width = width;
			this.level = 0; //basically it is the 0th level item in the dialog
		}

		[Summary("When adding a child component, check if it is an instance of the GUTATable!")]
		internal override void AddComponent(GUTAComponent child) {
			if (!(child is GUTATable)) {
				throw new IllegalGUTAComponentExtensionException("Cannot insert " + child.GetType() + " directly into the GUTAMatrix");
			}
			AddNewChild(child);
		}

		[Summary("Writing the table means writing the background and calling the writing on all of its rows")]
		internal override void WriteComponent() {
			//first count the height from the number of rows (and spaces between them)
			//upper and lower borders   //upper and lower row delimiters
			height += 2 * ImprovedDialog.D_BORDER + 2 * ImprovedDialog.D_SPACE;
			foreach (GUTATable row in components) {
				//row height   //upper and lower inner row border delimiter
				height += row.Height + 2 * ImprovedDialog.D_ROW_SPACE;
			}
			//add a smaller space between every row and add the total rows height
			height += (components.Count - 1) * ImprovedDialog.D_ROW_SPACE;

			if (!NoWrite) {
				//make borders
				gump.AddResizePic(xPos, yPos, ImprovedDialog.D_DEFAULT_DIALOG_BORDERS, width, height);
				//make the inner space
				gump.AddGumpPicTiled(xPos + ImprovedDialog.D_BORDER, yPos + ImprovedDialog.D_BORDER,
									 width - 2 * ImprovedDialog.D_BORDER, height - 2 * ImprovedDialog.D_BORDER,
									 ImprovedDialog.D_DEFAULT_DIALOG_BACKGROUND);
			}
			WriteChildren();
		}

		[Summary("Result will look like this:" +
				"'Dialog" +
				"	-> children'")]
		public override string ToString() {
			StringBuilder retStr = new StringBuilder("Dialog");
			foreach (GUTAComponent child in components) {
				retStr.Append(child.ToString());
			}
			return retStr.ToString();
		}
	}

	[Summary("Table class - this class can be only added to the basic GUTAMatrix")]
	public class GUTATable : GUTAComponent {
		[Summary("Shall the table's columns be made as transparent after writing out?")]
		private bool transparent;
		public bool Transparent {
			get {
				return transparent;
			}
			set {
				transparent = value;
			}
		}

		[Summary("Basic gump table - with nothing specified - GUTARows will be added later")]
		public GUTATable() {
		}

		[Summary("Basic gump table - single virtual row - specify the number of innerrows only")]
		public GUTATable(int rowCount) {
			GUTARow oneRow = new GUTARow(rowCount); //virtual Row the columns are all inside
			AddComponent(oneRow); //this will serve as component[0] in the following constructor (if necessary)
		}

		[Summary("This constructor allows us to specify the sizes of intable columns." +
				"0 as the size means the column that takes the rest of the width" +
				"If 0 is the one-before-last column size, then the last column will be added as " +
				"'AddLastColumn' - its position will be counted from the right side")]
		public GUTATable(int rowCount, params int[] columnSizes)
			: this(rowCount) {
			bool shallBeLast = false;
			for (int i = 0; i < columnSizes.Length; i++) {
				if (shallBeLast) { //are we adding the last column?					
					GUTAColumn lastCol = new GUTAColumn(columnSizes[i]);
					lastCol.IsLast = true;
					components[0].AddComponent(lastCol);
				} else {
					if (columnSizes[i] == 0) {
						components[0].AddComponent(new GUTAColumn());
					} else {
						components[0].AddComponent(new GUTAColumn(columnSizes[i]));
					}
				}
				//if the one-before-last column is zero sized, the next column will be added 'as last'				
				shallBeLast = (columnSizes[i] == 0) && (i == columnSizes.Length - 2);
			}
		}

		[Summary("Row Height is set automatically to the default value, other values are to be " +
				"changed manually and are propagated to the inner virtual row. If the virtul row is" +
				" not yet present, the exception is thrown")]
		public int RowHeight {
			get {
				return ((GUTARow) components[0]).RowHeight;
			}
			set {
				((GUTARow) components[0]).RowHeight = value;
			}
		}

		[Summary("Similar behaviour as RowHeight - propagate to the virtual row inside")]
		public int RowCount {
			get {
				return ((GUTARow) components[0]).RowCount;
			}
			set {
				((GUTARow) components[0]).RowCount = value;
			}
		}

		[Summary("Similar behaviour as RowHeight - propagate to the virtual row inside")]
		public bool InnerRowsDelimited {
			get {
				return ((GUTARow) components[0]).InnerRowsDelimited;
			}
			set {
				((GUTARow) components[0]).InnerRowsDelimited = value;
			}
		}

		[Summary("Real height of the GUTATable background, including space for a column. " +
				" This value can be computed and accessed everywhere")]
		public override int Height {
			get {
				if (height == 0) {
					//height has not yet been computed, compute it now, sumarize it from the GUTARows inside
					foreach (GUTARow virtualRow in components) {
						height += virtualRow.RowCount * virtualRow.RowHeight +
							//if the inner rows of the virtual row are to be delimited, add corresponding number of space for it
							(virtualRow.InnerRowsDelimited ? (virtualRow.RowCount - 1) * ImprovedDialog.D_COL_SPACE : 0);
						//if the GUTARow row is to be separated from the other GUTARows then add the separating space
						//(but dont separate the last row!)
						if(components.IndexOf(virtualRow) < (components.Count - 1)) {
							height += (virtualRow.InTableSeparated ? 1 : 0) * ImprovedDialog.D_ROW_SPACE;
						}
					}
					return height;
				} else {
					//the height has been computed, use it instead of computing it again...
					return height;
				}
			}
		}

		[Summary("Add a single user-defined GUTARow to the table")]
		public void AddRow(GUTARow row) {
			AddComponent(row);
		}

		[Summary("Special indexer used for setting components directly to the given column " +
				"to te specific row. Implemented is only setter for adding leaf components " +
				"directly to the specified position." +
				"Both row and column (row, col variables) are counted from zero!" +
				"Usage is: table[x,y] = LeafComponent which means that to the xth row in the " +
				"yth column is _added_ the specified LeafComponent. The leaf components size and " +
				"position must be specified manually. Perpetual usage of the same x,y coordinates does " +
				"not overwrite anything(!) it just _adds_ the component to the existing column to the " +
				"specified row position." +
				"The getter method returns the specified GUTAColumn (ignoring the row parameter)." +
				"Newly we are able to add also the texts - they will be transformed automatically." +

				"THIS FUNCTIONALITY WILL ADD EVERYTHING TO THE FIRST VIRTUAL ROW IN THE TABLE - THIS IS THE " +
				"CASE OF MOST OF THE DIALOGS THAT USUALLY USE ONLY ONE VIRTUAL ROW FOR EVERYTHING")]
		public GUTAComponent this[int row, int col] {
			set {
				//set it on the 1st virtual row
				((GUTARow) components[0])[row, col] = value;
			}
			get {
				//get the value from the first virtual row
				GUTARow virtualRow = (GUTARow) components[0];
				return ((GUTARow) components[0])[row, col];
			}
		}

		[Summary("ALternative way to add something to the desired place in the GUTATable. " +
				"Used from LSCript as LSCript cannot handle 'this[x,y]' notation yet...")]
		public void AddToCell(int row, int col, GUTAComponent comp) {
			this[row, col] = comp;
		}

		[Summary("The method called when the row is added to the table. It will set the rows positions" +
				" and size")]
		protected override void OnBeforeWrite(GUTAComponent parent) {
			//set the level
			level = parent.Level + 1;
			if (parent.Components.IndexOf(this) > 0) { //this is not the first row
				//take the position from the previous sibling table
				GUTATable lastTable = (GUTATable) parent.Components[parent.Components.IndexOf(this) - 1];
				//the x position is simple
				this.xPos = lastTable.xPos;
				//the y position is right under the previous row
				//last ypos    height           space to fit the inner grey border and one space to delimit the rows 
				this.yPos = lastTable.yPos + lastTable.Height + 2 * ImprovedDialog.D_ROW_SPACE;
			} else {
				this.xPos = parent.XPos;
				this.yPos = parent.YPos;
				//it is the first row we are adding
				//if parent is GUTAMAtrix make indentions otherwise let it be (adding to the Column e.g.)
				if (parent is GUTAMatrix) {
					this.xPos += ImprovedDialog.D_BORDER; //add it behind the left border
					this.xPos += ImprovedDialog.D_SPACE + ImprovedDialog.D_ROW_SPACE; //1 for delimiting the grey border from the table borders, 1 for delimiting the beige border from the inner grey border
					this.yPos += ImprovedDialog.D_BORDER; //just bellow the top border   
					this.yPos += ImprovedDialog.D_SPACE; //one space to delimit the row from the top border
				} else {
					width = parent.Width - ImprovedDialog.D_COL_SPACE; //substract the column delimiter since all parent columns get this substracted...
					GUTAColumn parCol = (GUTAColumn)parent;
					//parentalGUTAColumn->GUTARow.Components.IndexOf(parentalGUTAColumn) > 0
					//if (parCol.Parent.Components.IndexOf(parCol) == parCol.Parent.Components.Count - 1) {
					//	//this table is in the last column
					//	
					//}
					//this.yPos -= ImprovedDialog.D_COL_SPACE;//do not delimit inside the column (the tableservs for layout purposes only, no displaying usually...)
				}
			}
			//if adding to GUTAMatrix, resize it to fit, otherwise take simply the size of the parent (usually GUTAColumn)
			if (parent is GUTAMatrix) {
				width = parent.Width - 2 * ImprovedDialog.D_BORDER; //the row must get between two table borders...
				width -= 2 * ImprovedDialog.D_SPACE + 2 * ImprovedDialog.D_ROW_SPACE; //delimit row from the table borders and fit into the grey row borders

				yPos += ImprovedDialog.D_ROW_SPACE; //one space in the inner row border			
			}
		}

		[Summary("When adding a child component, check if it is an instance of the GUTARow!")]
		internal override void AddComponent(GUTAComponent child) {
			if (!(child is GUTARow)) {
				throw new IllegalGUTAComponentExtensionException("Cannot insert " + child.GetType() + " directly into the GUTATable");
			}
			//set the row's parent now (we may need it for computing the columns height) e.g. when
			//adding a prev/next buttons to the bottom of the column (we need to know its height)
			child.Parent = this;
			AddNewChild(child);
		}

		[Summary("Simply write the row background and continue with the virtual rows)")]
		internal override void WriteComponent() {
			if (!NoWrite) { //dont write the grey background (we dont need the borders)
				//first add the main "grey" - border tile, within the table borders delimited by spaces
				//take one space back (see OnBeforeWrite method why...)
				gump.AddGumpPicTiled(xPos - ImprovedDialog.D_ROW_SPACE, yPos - ImprovedDialog.D_ROW_SPACE,
					//size to fit the rows outer grey border
									 width + 2 * ImprovedDialog.D_ROW_SPACE, Height + 2 * ImprovedDialog.D_ROW_SPACE,
									 ImprovedDialog.D_DEFAULT_ROW_BORDERS);
			}
			////then add the inner beige tile, delimit it from the inner border by a little space too
			//gump.AddGumpPicTiled(xPos, yPos, width, Height, gumpBackground);
			//Globals.SrcCharacter.SysMessage("Table: " + (xPos - ImprovedDialog.D_ROW_SPACE) + "," + (yPos - ImprovedDialog.D_ROW_SPACE) + "," + (width + 2 * ImprovedDialog.D_ROW_SPACE) + "," + (height + 2 * ImprovedDialog.D_ROW_SPACE));
			//write GUTARows
			WriteChildren();
		}

		public override string ToString() {
			string offset = "\r\n";
			for (int i = 0; i < level; i++) {
				offset += "\t";
			}
			StringBuilder retStr = new StringBuilder(offset + "->Table");
			foreach (GUTAComponent child in components) {
				retStr.Append(child.ToString());
			}
			return retStr.ToString();
		}
	}

	public class GUTARow : GUTAComponent {
		[Summary("Number of inner lines in this virtual row of the dialog")]
		private int rowCount;

		[Summary("The height of each line in the pixels, defaultly is D_BUTTON_HEIGHT. Other values " +
				"can be specified using the appropriate setter")]
		private int rowHeight = ButtonMetrics.D_BUTTON_HEIGHT;

		[Summary("Should the inner rows of every column in this virtual row be delimited by thin line?")]
		private bool innerRowsDelimited = false;
		[Summary("Should this GUTARow be separated in the GUTATable from the following GUTARow by a thin line?")]
		private bool inTableSeparated = true;

		public GUTARow() {
		}

		public GUTARow(int rowCount) {
			this.rowCount = rowCount;
		}

		[Summary("This constructor allows us to specify the sizes of inner columns." +
				"0 as the size means the column that takes the rest of the width" +
				"If 0 is the one-before-last column size, then the last column will be added as " +
				"'AddLastColumn' - its position will be counted from the right side")]
		public GUTARow(int rowCount, params int[] columnSizes)
			: this(rowCount) {
			bool shallBeLast = false;
			for (int i = 0; i < columnSizes.Length; i++) {
				if (shallBeLast) { //are we adding the last column?					
					GUTAColumn lastCol = new GUTAColumn(columnSizes[i]);
					lastCol.IsLast = true;
					AddComponent(lastCol);
				} else {
					if (columnSizes[i] == 0) {
						AddComponent(new GUTAColumn());
					} else {
						AddComponent(new GUTAColumn(columnSizes[i]));
					}
				}
				//if the one-before-last column is zero sized, the next column will be added 'as last'				
				shallBeLast = (columnSizes[i] == 0) && (i == columnSizes.Length - 2);
			}
		}

		public GUTAComponent this[int row, int col] {
			get {
				//just return the desired GUTAColumn if present
				if (Components.Count <= col) {
					//dont forget that indexing is counted from zero! 
					//so col=6 means we want to access 7th column
					throw new SEException("Not enough columns in the GUTARow - trying to access " +
										   (col + 1) + ". column out of " + Components.Count);
				}
				return Components[col];
			}
			set {
				//first check if we have enough columns
				if (RowCount < row) {
					throw new SEException("Not enough rows in the GUTARow - trying to access " +
										  (row + 1) + ". row out of " + RowCount);
				}
				if (Components.Count <= col) {
					//dont forget that indexing is counted from zero! 
					//so col=6 means we want to access 7th column
					throw new SEException("Not enough columns in the GUTARow - trying to access " +
										   (col + 1) + ". column out of " + Components.Count);
				}
				//get the column we are adding the component to
				GUTAColumn columnToAccess = (GUTAColumn) Components[col];
				//now check what is the component:
				GUTAComponent addedObj = value as GUTAComponent;//the component will be added directly	
				if (addedObj == null) {
					string strVal;
					if (ConvertTools.TryConvertToString(value, out strVal)) {
						addedObj = GUTAText.Builder.Text(strVal).Build(); //create the text component now					
					} else {
						throw new SEException("Unhandled object type for dialog column " + value.GetType());
					}
				}

				//move the component to the desired row
				addedObj.YPos += row * RowHeight;
				if (InnerRowsDelimited) { //add the proper space !
					addedObj.YPos += (row - 1) * ImprovedDialog.D_COL_SPACE; //from the second row we must add some pixels...
				}
				//and add the component
				columnToAccess.AddComponent(addedObj);
			}
		}

		[Summary("ALternative way to add something to the desired place in the GUTATable. " +
				"Used from LSCript as LSCript cannot handle 'this[x,y]' notation yet...")]
		public void AddToRow(int row, int col, GUTAComponent comp) {
			this[row, col] = comp;
		}

		public int RowCount {
			get {
				return rowCount;
			}
			set {//the setter may be needed in 'setting' dialog - we may need to add some rows...
				rowCount = value;
			}
		}

		[Summary("Row Height is set automatically to the default value, other values are to be " +
				"changed manually")]
		public int RowHeight {
			get {				
				return rowHeight;
			}
			set {
				rowHeight = value;
			}
		}

		public override int Height {
			get {
				if (height == 0) {
					//height has not yet been computed, compute it now from the RowHeight and RowCount
					height += RowCount * RowHeight +
						//if the inner rows of the virtual row are to be delimited, add corresponding number of space for it
							(InnerRowsDelimited ? (RowCount - 1) * ImprovedDialog.D_COL_SPACE : 0);

					return height;
				} else {
					//the height has been computed, use it instead of computing it again...
					return height;
				}
			}
		}

		public bool InnerRowsDelimited {
			get {
				return innerRowsDelimited;
			}
			set {
				innerRowsDelimited = value;
				if (innerRowsDelimited && (rowCount > 1)) { //makes sense only for more than 1 row...
					foreach (GUTAColumn child in components) {
						child.DelimitRows = true;
					}
				}
			}
		}

		public bool InTableSeparated {
			get {
				return inTableSeparated;
			}
			set {
				inTableSeparated = value;
			}
		}

		[Summary("The method called when the virtual row is added to the table. It will set the rows positions" +
				" and size")]
		protected override void OnBeforeWrite(GUTAComponent parent) {
			//set the level
			level = parent.Level + 1;
			if (parent.Components.IndexOf(this) > 0) { //this is not the first virtual row
				//take the position from the previous sibling table
				GUTARow lastRow = (GUTARow) parent.Components[parent.Components.IndexOf(this) - 1];
				//the x position is simple
				this.xPos = lastRow.xPos;
				//the y position is right under the previous virtual row
				//last ypos  +  height  +  if the previous GUTARow is to be separated then separate it...
				this.yPos = lastRow.yPos + lastRow.Height + (lastRow.InTableSeparated ? ImprovedDialog.D_ROW_SPACE : 0);
			} else {
				//it is the first virtual row we are adding - take the same setttings as the parent has
				this.xPos = parent.XPos;
				this.yPos = parent.YPos;
			}
			width = parent.Width;
		}

		internal override void AddComponent(GUTAComponent child) {
			//set the column's parent now (we may need it for computing the columns height) e.g. when
			//adding a prev/next buttons to the bottom of the column (we need to know its height)
			child.Parent = this;
			AddNewChild(child);
		}

		internal override void WriteComponent() {
			//add the inner-GUTATabular beige tile, delimit it from the table's inner border by a little space too
			gump.AddGumpPicTiled(xPos, yPos, width, Height, ImprovedDialog.D_DEFAULT_ROW_BACKGROUND);

			//and also check the delimiting space for GUTARows in the GUTATable (if demanded)
			if( parent.Components.IndexOf(this) < (parent.Components.Count - 1)) { //it is not the last GUTARow in the table
				if (inTableSeparated) {//and this GUTARow is to be separated
					//add the delimiting beige line (delimiting one GUTARow from the other
					gump.AddGumpPicTiled(xPos, yPos + Height, width, ImprovedDialog.D_ROW_SPACE, ImprovedDialog.D_DEFAULT_ROW_BACKGROUND);
				}
			}

			//write GUTAColumns
			WriteChildren();
		}

		public override string ToString() {
			string offset = "\r\n";
			for (int i = 0; i < level; i++) {
				offset += "\t";
			}
			StringBuilder retStr = new StringBuilder(offset + "->Row");
			foreach (GUTAComponent child in components) {
				retStr.Append(child.ToString());
			}
			return retStr.ToString();
		}
	}

	[Summary("Column - this can be added only to the GUTARow and it will contain all of the dialog elements")]
	public class GUTAColumn : GUTAComponent {
		[Summary("Height (in rows) if the column, the innerrow height is specified in the parental GUTARow")]
		private int rowCount;

		[Summary("Is this column last in the row? - previous column will be recomputed in time this " +
				"last column is being written so this last column will fit to the table " +
				"'from the right side'")]
		private bool isLast;

		[Summary("Should the inner 'rows' be delimited by thin line?")]
		private bool delimitRows;

		[Summary("Basic column - after it is added to the GUTATable it will take the row's size")]
		public GUTAColumn() {
		}

		[Summary("Basic column - after it is added to the GUTATable it will take the row's height " +
				"but we can specify the width")]
		public GUTAColumn(int width) {
			this.width = width;
		}

		public int RowCount {
			get {
				if (rowCount == 0) {
					//row count is not yet set - get it from the parent
					rowCount = ((GUTARow) parent).RowCount;
					return rowCount;
				} else {
					return rowCount;
				}
			}
			set {
				rowCount = value;
			}
		}

		[Summary("Return the real columns height - it is computed on the fly from the rowcount and" +
				" the row height (it should be well known when accessing the height as the column " +
				" is expected to be properly added to some column")]
		public override int Height {
			get {
				if (height == 0) {
					//height is not yet set
					height = RowCount * ((GUTARow) parent).RowHeight +
						//in case of delimiting the rows, add the delimiting spaces
						(delimitRows ? (rowCount - 1) * ImprovedDialog.D_COL_SPACE : 0);
					return height;
				} else {
					return height;
				}
			}
		}

		public bool IsLast {
			get {
				return isLast;
			}
			set {
				isLast = value;
			}
		}

		public bool DelimitRows {
			get {
				return delimitRows;
			}
			set {
				delimitRows = value;
			}
		}

		[Summary("Set the position according to the previous columns (if any) and set the size")]
		protected override void OnBeforeWrite(GUTAComponent parent) {
			//set the level
			level = parent.Level + 1;

			if (parent.Components.IndexOf(this) > 0) {//this is not the first column
				//get the previous column
				GUTAColumn prevCol = (GUTAColumn) parent.Components[parent.Components.IndexOf(this) - 1];
				bool lastInRow = parent.Components.IndexOf(this) == parent.Components.Count - 1;
				if (isLast) { //last column and the previous sibling had 0 width specified - it will be recomputed now
					//adjust the previous columns size to the space between the twiceprevious column 
					//and the newly added column (it can be also negative value...)              
					prevCol.Width += parent.Width - (prevCol.XPos - parent.XPos) - prevCol.Width - width;
					//now the size is recomputed, the new column will fit right to the end of the row...
				}
				//the x position is right to the right of the previous sibling
				this.xPos = prevCol.xPos + prevCol.width;
				//the y position is simple
				this.yPos = prevCol.yPos;
				if (width == 0) {//no width - it is probably the last column in the row 
					//take the rest to the end of the row
					width = parent.Width - (xPos - parent.XPos);
				}
				if (lastInRow) { //really last column (no looking on the prev. col. width as in "isLast" case
					prevCol.width += ImprovedDialog.D_COL_SPACE; //widen the previous column
					this.xPos += ImprovedDialog.D_COL_SPACE; //and move the last column one space to stick to the right border
				}
			} else {
				//first column in the table
				this.xPos = parent.XPos;// + ImprovedDialog.D_COL_SPACE; //(1 space for column's border)
				this.yPos = parent.YPos;// + ImprovedDialog.D_COL_SPACE; //(1 space for column's border)
				if (width == 0) {
					//no width was specified in the constructor - this is probably the only column in the row,
					//set the width from the parent
					width = parent.Width;
				}
				if (parent.Components.Count == 1) {//only one column in the row (so this is both the first and the last one)
					width += ImprovedDialog.D_COL_SPACE; //add the colspace to it (it is always substracted in the WriteComponent method and it would be missing in this particular case...)
				}
			}
		}

		[Summary("Only leaf components, texts or another GUTATable can be added here...")]
		internal override void AddComponent(GUTAComponent child) {
			if ((child is GUTAMatrix) || (child is GUTAColumn)) {
				throw new IllegalGUTAComponentExtensionException("Cannot insert " + child.GetType() + " into the GUTAColumn. Use the GUTATable, leaf components or texts instead!");
			}
			AddNewChild(child);
		}

		[Summary("Simply write the columns background and continue with the children)")]
		internal override void WriteComponent() {
			if (!NoWrite) {
				//position is specified, remove one space from the width (will appear on the right col. side)
				gump.AddGumpPicTiled(xPos, yPos, width - ImprovedDialog.D_COL_SPACE, Height, ImprovedDialog.D_DEFAULT_COL_BACKGROUND);
			}
			//parent = GUTARow, Parent = rows.parent...
			if (((GUTATable) parent.Parent).Transparent) {//the grandparent table is set to be transparent
				SetTransparency();//make it transparent after writing out
			}
			if (!NoWrite) {
				//and also check the delimiting spaces for rows..., after the transparency check
				if (delimitRows) {
					int rowHeight = ((GUTARow) parent).RowHeight;
					for (int i = 0; i < rowCount - 1; i++) {
						//add after each "row" one pixel beige line...
						gump.AddGumpPicTiled(xPos, yPos + (i + 1) * rowHeight + (i) * ImprovedDialog.D_COL_SPACE, width - ImprovedDialog.D_COL_SPACE, 1, ImprovedDialog.D_DEFAULT_ROW_BACKGROUND);
					}
				}
			}
			//Globals.SrcCharacter.SysMessage("Col: " + xPos + "," + yPos + "," + width + "," + height);
			//write children (another inner GUTATable or leaf components)
			WriteChildren();
		}

		[Summary("Make the whole column transparent")]
		private void SetTransparency() {
			gump.AddCheckerTrans(xPos, yPos, width - ImprovedDialog.D_COL_SPACE, Height);
		}

		public override string ToString() {
			string offset = "\r\n";
			for (int i = 0; i < level; i++) {
				offset += "\t";
			}
			StringBuilder retStr = new StringBuilder(offset + "->Column");
			foreach (GUTAComponent child in components) {
				retStr.Append(child.ToString());
			}
			return retStr.ToString();
		}
	}
}