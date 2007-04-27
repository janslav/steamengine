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
using System.Collections;
using System.Collections.Generic;
using SteamEngine;
using SteamEngine.Common;
using SteamEngine.CompiledScripts;

namespace SteamEngine.CompiledScripts.Dialogs {

	[Remark("Exception thrown when trying to add child to the InExtensible GumpComponents")]
	public class GumpComponentCannotBeExtendedException : SEException {
		public GumpComponentCannotBeExtendedException() : base() { }
		public GumpComponentCannotBeExtendedException(string s) : base(s) { }
		public GumpComponentCannotBeExtendedException(LogStr s)
			: base(s) {
		}
	}

	[Remark("Exception thrown when adding a child of a forbidden type (e.g. Row directly into the TableMatrix)"+
            " omitting the Columns.")]
	public class IllegalGumpComponentExtensionException : SEException {
		public IllegalGumpComponentExtensionException() : base() { }
		public IllegalGumpComponentExtensionException(string s) : base(s) { }
		public IllegalGumpComponentExtensionException(LogStr s)
			: base(s) {
		}
	}

	[Remark("Abstract parent class of all possible gump components. It is implemented as a "+
            "composite design pattern - all gump parts are inserted into another part which is "+
            "inserted into another part... which is inserted into the main parent part (the whole gump)")]
	public abstract class GumpComponent {
		[Remark("Width, height and positions are common to all basic components")]
		protected int width, height, xPos, yPos;

		[Remark("Should the component be  made transparent after writing out?")]
		protected bool transparent = false;

		[Remark("List of all components children")]
		protected List<GumpComponent> components = new List<GumpComponent>();
		protected GumpInstance gump;
		protected GumpComponent parent;

		public List<GumpComponent> Components {
			get {
				return components;
			}
		}

		public GumpComponent Parent {
			get {
				return parent;
			}
			set {
				parent = value;
			}
		}

		public GumpInstance Gump {
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

		public int Height {
			get {
				return height;
			}
			set {
				height = value;
			}
		}

		public bool Transparent {
			get {
				return transparent;
			}
			set {
				transparent = value;
			}
		}

		[Remark("Relative Y-position in the gump")]
		public int YPos {
			set {
				yPos = value;
			}
			get { //mainly for debug purposes
				return yPos;
			}
		}

		[Remark("Relative X-position in the gump")]
		public int XPos {
			set {
				xPos = value;
			}
			get { //mainly for debug purposes
				return xPos;
			}
		}

		[Remark("Prototype of the parent method - provides the basic validation of the inserted component."+
                "The rest of the insertion process is up to the Children.")]
		public abstract void AddComponent(GumpComponent child);

		[Remark("Abstract method of writing the gump to the LScript - typically writes the component"+
                " and then all its children (who can behave in the same way)")]
		public abstract void WriteComponent();

		[Remark("Method called when the component is added to the list - allows some post processing " +
                "including various operations on the parent, for example."+
                "Defaultly is empty, but can be adapted as one wishes.")]
		public virtual void OnAdded(GumpComponent parent) {
		}

		[Remark("Method calls the WriteComponent() method on all children from the list")]
		protected void WriteChildren() {
			foreach (GumpComponent child in components) {
				child.WriteComponent();
			}
		}

		[Remark("Method basically wraps the calling of List.Add() method, but it allows also other "+
                "processing if overriden.")]
		protected void AddNewChild(GumpComponent child) {
			components.Add(child);//simply add to the list
			child.OnAdded(this);//call the postprocessing method            
		}

		[Remark("Move the given component and all of its children a bit (add a new position coordinates "+
                "to the actual ones")]
		public void AdjustPosition(int xPart, int yPart) {
			xPos += xPart;
			yPos += yPart;
			//handle possible negative values if the argujments are negative...
			if (xPos < 0)
				xPos = 0;
			if (yPos < 0)
				yPos = 0;
			foreach (GumpComponent child in components) {
				child.AdjustPosition(xPart, yPart);
			}
		}
	}

	[Remark("Main layout item - the table matrix. Can contain GumpRows as a direct children only")]
	public class GumpMatrix : GumpComponent {
		[Remark("The main table must be initialized with a valid GumpInstance and also with the "+
                "width, we can but needn't provide the xPos and yPos. Height is computed automatically")]
		public GumpMatrix(GumpInstance gump, int width)
			: this(gump, 0, 0, width) {
		}

		[Remark("The main table must be initialized with a valid GumpInstance and also with the " +
               "width, we can but needn't provide the xPos and yPos.  Height is computed automatically")]
		public GumpMatrix(GumpInstance gump, int xPos, int yPos, int width) {
			this.gump = gump;
			this.xPos = xPos;
			this.yPos = yPos;
			this.height = height;
			this.width = width;
		}

		[Remark("When adding a child component, check if it is an instance of the GumpTable!")]
		public override void AddComponent(GumpComponent child) {
			if (!(child is GumpTable)) {
				throw new IllegalGumpComponentExtensionException("Cannot insert " + child.GetType() + " directly into the GumpMatrix");
			}
			//set the child's properties
			child.Parent = this;
			child.Gump = this.Gump;
			AddNewChild(child);
		}

		[Remark("Writing the table means writing the background and calling the writing on all of its rows")]
		public override void WriteComponent() {
			//first count the height from the number of rows (and spaces between them)
			//upper and lower borders   //upper and lower row delimiters
			height += 2 * ImprovedDialog.D_BORDER + 2 * ImprovedDialog.D_SPACE;
			foreach (GumpTable row in components) {
				//row height   //upper and lower inner row border delimiter
				height += row.Height + 2* ImprovedDialog.D_ROW_SPACE;
			}
			//add a smaller space between every row and add the total rows height
			height += (components.Count - 1) * ImprovedDialog.D_ROW_SPACE;

			//make borders
			gump.AddResizePic(xPos, yPos, ImprovedDialog.D_DEFAULT_DIALOG_BORDERS, width, height);
			//make the inner space
			gump.AddGumpPicTiled(xPos+ImprovedDialog.D_BORDER, yPos+ImprovedDialog.D_BORDER,
								 width - 2 * ImprovedDialog.D_BORDER, height - 2 * ImprovedDialog.D_BORDER,
								 ImprovedDialog.D_DEFAULT_DIALOG_BACKGROUND);
			if (transparent) {
				SetTransparency();
			}
			WriteChildren();
		}

		[Remark("Make the whole table transparent inside the borders")]
		public void SetTransparency() {
			gump.AddCheckerTrans(xPos + ImprovedDialog.D_BORDER, yPos + ImprovedDialog.D_BORDER, width - 2*ImprovedDialog.D_BORDER, height - 2*ImprovedDialog.D_BORDER);
		}
	}

	[Remark("Table class - this class can be only added to the basic GumpMatrix")]
	public class GumpTable : GumpComponent {
		[Remark("Number of lines in this part of the dialog")]
		private int rowCount;
		[Remark("Background borders")]
		private int gumpBorders = ImprovedDialog.D_DEFAULT_ROW_BORDERS;
		[Remark("Background whole")]
		private int gumpBackground = ImprovedDialog.D_DEFAULT_ROW_BACKGROUND;
		[Remark("The height of each line in the pixels, defaultly is D_ROW_HEIGHT")]
		private int rowHeight = ImprovedDialog.D_ROW_HEIGHT;

		[Remark("Basic gump table - specify the number of rows only")]
		public GumpTable(int rowCount) {
			this.rowCount = rowCount;
		}

		[Remark("This constructor allows us to specify the sizes of intable columns")]
		public GumpTable(int rowCount, params object[] columnSizes) : this(rowCount) {
			for(int i = 0; i < columnSizes.Length; i++) {
				/*add all columns as specified, unfortunatelly these wont be available for Add 
				  method as there was no setting of "lastcol etc.", use the AddToColumnAndRow 
				  (or similarly called method) instead*/
				if((int)columnSizes[i] == 0) {
					this.AddComponent(new GumpColumn());
				} else {
					this.AddComponent(new GumpColumn((int)columnSizes[i]));
				}
			}
		}

		[Remark("This constructor allows us to specify the sizes of intable columns")]
		public GumpTable(int rowCount, int rowHeight, params object[] columnSizes) : this(rowCount,rowHeight) {
			for(int i = 0; i < columnSizes.Length; i++) {
				/*add all columns as specified, unfortunatelly these wont be available for Add 
				  method as there was no setting of "lastcol etc.", use the AddToColumnAndRow 
				  (or similarly called method) instead*/
				if((int)columnSizes[i] == 0) {
					this.AddComponent(new GumpColumn());
				} else {
					this.AddComponent(new GumpColumn((int)columnSizes[i]));
				}
			}
		}

		[Remark("Allows to customize the row height")]
		public GumpTable(int rowCount, int rowHeight) {
			this.rowCount = rowCount;
			this.rowHeight = rowHeight;
		}

		[Remark("Allows to customize all properties")]
		public GumpTable(int rowCount, int rowHeight, string gumpBorders, string gumpBackground) {
			this.rowCount = rowCount;
			this.rowHeight = rowHeight;
			this.gumpBorders = int.Parse(gumpBorders);
			this.gumpBackground = int.Parse(gumpBackground);
		}

		public int RowCount {
			get {
				return rowCount;
			}
			set {
				rowCount = value;
			}
		}

		public int RowHeight {
			get {
				return rowHeight;
			}
			set {
				rowHeight = value;
			}
		}

		[Remark("Special indexer used for setting components directly to the given column "+
				"to te specific row. Implemented is only setter for adding leaf components "+
				"directly to the specified position."+
				"Both row and column (row, col variables) are counted from zero!")]
		public GumpComponent this[int row, int col] {
			set {
				//first check if we have enough rows
				if(rowCount < row) {
					throw new SEException("Not enough rows in the GumpTable - trying to access " +
										  row + ". row out of " + rowCount);
				}
				if(components.Count <= col) {
					//dont forget that indexing is counted from zero! 
					//so col=6 means we want to access 7th column
					throw new SEException("Not enough columns in the GumpTable - trying to access " +
										   (col+1) + ". column out of " + components.Count);
				}
				//get the column we are adding the component to
				GumpColumn columnToAccess = (GumpColumn)components[col];
				//move the component to the desired row
				value.YPos += row * rowHeight;
				//and add the component
				columnToAccess.AddComponent(value);
			}
			get {
				//just return the desired GumpColumn, ignore the row parameter, it is only for setting
				if(components.Count <= col) {
					//dont forget that indexing is counted from zero! 
					//so col=6 means we want to access 7th column
					throw new SEException("Not enough columns in the GumpTable - trying to access " +
										   (col + 1) + ". column out of " + components.Count);
				}
				return (GumpColumn)components[col];
			}
		}

		[Remark("The method called when the row is added to the table. It will set the rows positions"+
                " and size")]
		public override void OnAdded(GumpComponent parent) {						
			if (parent.Components.Count > 1) { //count from 1 because at least one row is always added when calling this method
				//take the position from the last sibling (not the last item in the field because the last one is "this")
				GumpTable lastTable = (GumpTable)parent.Components[parent.Components.Count - 2];
				//the x position is simple
				this.xPos = lastTable.xPos;
				//the y position is right under the previous row
				//last ypos    height           space to fit the inner grey border and one space to delimit the rows 
				this.yPos = lastTable.yPos + lastTable.height + 2 * ImprovedDialog.D_ROW_SPACE;
			} else {
				//it is the first row we are adding
				this.xPos = parent.XPos + ImprovedDialog.D_BORDER; //behind the left border
				this.xPos += ImprovedDialog.D_SPACE + ImprovedDialog.D_ROW_SPACE; //1 for delimiting the grey border from the table borders, 1 for delimiting the beige border from the inner grey border
				this.yPos = parent.YPos + ImprovedDialog.D_BORDER; //just bellow the top border   
				this.yPos += ImprovedDialog.D_SPACE; //one space to delimit the row from the top border
			}
			//real height of the GumpTable background, including space for a column
			height = rowCount * rowHeight + 2 * ImprovedDialog.D_COL_SPACE;

			width = parent.Width - 2 * ImprovedDialog.D_BORDER; //the row must get between two table borders...
			width -= 2 * ImprovedDialog.D_SPACE + 2 * ImprovedDialog.D_ROW_SPACE; //delimit row from the table borders and fit into the grey row borders

			yPos += ImprovedDialog.D_ROW_SPACE; //one space in the inner row border

			foreach(GumpColumn child in components) {
				/*call this for sure - columns could havew been added in constructor, therefore they
				 *are not set yet
				 */
				child.OnAdded(this); 
			}
		}

		[Remark("When adding a child component, check if it is an instance of the GumpColumn!")]
		public override void AddComponent(GumpComponent child) {
			if (!(child is GumpColumn)) {
				throw new IllegalGumpComponentExtensionException("Cannot insert " + child.GetType() + " directly into the GumpTable");
			}
			child.Parent = this;
			child.Gump = this.Gump;
			AddNewChild(child);
		}

		[Remark("Simply write the row background and continue with the columns)")]
		public override void WriteComponent() {
			//first add the main "grey" - border tile, within the table borders delimited by spaces
			//take one space back (see OnAdded method why...)
			gump.AddGumpPicTiled(xPos - ImprovedDialog.D_ROW_SPACE, yPos - ImprovedDialog.D_ROW_SPACE,
				//size to fit the rows outer grey border
								 width + 2 * ImprovedDialog.D_ROW_SPACE, height + 2 * ImprovedDialog.D_ROW_SPACE,
								 gumpBorders);
			//then add the inner beige tile, delimit it from the inner border by a little space too
			gump.AddGumpPicTiled(xPos, yPos, width, height, gumpBackground);
			if (transparent) {
				SetTransparency();//make it transparent after writing out
			}
			//write columns
			WriteChildren();
		}

		[Remark("Make the whole row transparent, inside the inner borders")]
		public void SetTransparency() {
			gump.AddCheckerTrans(xPos, yPos, width, height);
		}
	}

	[Remark("Column - this can be added only to the GumpTable and it will contain all of the dialog elements")]
	public class GumpColumn : GumpComponent {
		[Remark("Height (in rows) if the column, the row height is specified in the parental GumpTable")]
		private int rowCount;
		[Remark("Basic gump id of the columns background (no more borders here)")]
		private int gumpBackground = ImprovedDialog.D_DEFAULT_COL_BACKGROUND;

		[Remark("Basic column - after it is added to the GumpTable it will take the row's size")]
		public GumpColumn() {
		}

		[Remark("Basic column - after it is added to the GumpTable it will take the row's height "+
                "but we can specify the width")]
		public GumpColumn(int width) {
			this.width = width;
		}

		[Remark("Arguments - width of the given column, number of text rows of height.")]
		public GumpColumn(int width, int rowCount) {
			this.width = width;
			this.rowCount = rowCount;
		}

		[Remark("Allows to customize all column's properties")]
		public GumpColumn(int width, int rowCount, string gumpBackground) {
			this.width = width;
			this.rowCount = rowCount;
			this.gumpBackground = int.Parse(gumpBackground);
		}

		public int RowCount {
			get {
				return rowCount;
			}
			set {
				rowCount = value;
			}
		}

		[Remark("Set the position according to the previous columns (if any) and set the size")]
		public override void OnAdded(GumpComponent parent) {
			/*check this, for sure... (e.g. the Column could have been added to the Table during
			 * the Table's constructor, therefore the table didn't have its "gump" set so we will
			 * make sure that this column has it properly after the OnAdded is called for the 
			 * second time!
			 */
			if(gump == null) { 
				gump = parent.Gump;
			}
			foreach(GumpComponent child in components) {
				child.Gump = gump;
			}
			if (parent.Components.IndexOf(this) > 0) {//this is not the first column
				//find previous sibling
				GumpColumn lastCol = (GumpColumn)parent.Components[parent.Components.IndexOf(this) - 1];
				//the x position is right to the right of the previous sibling
				this.xPos = lastCol.xPos + lastCol.width;
				//the y position is simple
				this.yPos = lastCol.yPos;
				if (width == 0) {//no width - it is probably the last column in the row 
					//take the rest to the end of the row
					width = parent.Width - (xPos - parent.XPos);
				}
				if (rowCount == 0) {
					rowCount = ((GumpTable) parent).RowCount; //take the number of the rows the parent Table has
				}
			} else {
				//first column in the row
				this.xPos = parent.XPos + ImprovedDialog.D_COL_SPACE; //(1 space for column's border)
				this.yPos = parent.YPos + ImprovedDialog.D_COL_SPACE; //(1 space for column's border)
				if (width == 0) {
					//no width was specified in the constructor - probably the only column in the row,
					//set the width from the parent, take one space from the left side
					width = parent.Width - ImprovedDialog.D_COL_SPACE;
				}
				if (rowCount == 0) {
					//no rowcount specified - get it from the parent row
					rowCount = ((GumpTable) parent).RowCount;
				}
			}
			height = rowCount * ((GumpTable) parent).RowHeight; //the real height of the background
		}

		[Remark("Only leaf components or another GumpMatrix can be added here...")]
		public override void AddComponent(GumpComponent child) {
			if ((child is GumpTable) || (child is GumpColumn)) {
				throw new IllegalGumpComponentExtensionException("Cannot insert " + child.GetType() + " into the GumpColumn. Use the GumpMatrix or leaf components instead!");
			}
			child.Parent = this;
			child.Gump = this.Gump;
			AddNewChild(child);
		}

		[Remark("Simply write the columns background and continue with the children)")]
		public override void WriteComponent() {
			//position is specified, remove one space from the width (will appear on the right col. side)
			gump.AddGumpPicTiled(xPos, yPos, width - ImprovedDialog.D_COL_SPACE, height, gumpBackground);
			if (transparent) {
				SetTransparency();//make it transparent after writing out
			}
			//write children (another inner GumpMatrix or leaf components)
			WriteChildren();
		}

		[Remark("Make the whole column transparent")]
		public void SetTransparency() {
			gump.AddCheckerTrans(xPos, yPos, width - ImprovedDialog.D_COL_SPACE, height);
		}
	}
}