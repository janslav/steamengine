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
	public abstract class Builder<T> where T : LeafGUTAComponent{
		public abstract T Build();
	}
	
	[Summary("Leaf GUTA components cannot have any children, these are e.g buttons, inputs, texts etc.")]
	public abstract class LeafGUTAComponent : GUTAComponent {
		[Summary("This is the ID many gump items have - buttons number, input entries number...")]
		protected int id;

		[Summary("Which row in the column this component lies in?")]
		protected int columnRow;

		[Summary("Adding any children to the leaf is prohibited...")]
		internal override sealed void AddComponent(GUTAComponent child) {
			throw new GUTAComponentCannotBeExtendedException("GUTAcomponent " + this.GetType() + " cannot have any children");
		}
	}

	[Summary("A staic class holding all necessary button constants")]
	public static class ButtonMetrics {
		public const int D_BUTTON_WIDTH = 31;
		public const int D_BUTTON_HEIGHT = 22;
		public const int D_CHECKBOX_WIDTH = 19;
		public const int D_CHECKBOX_HEIGHT = 20;
		public const int D_RADIO_WIDTH = 21;
		public const int D_RADIO_HEIGHT = 21;
		public const int D_BUTTON_PREVNEXT_WIDTH = 16;
		public const int D_BUTTON_PREVNEXT_HEIGHT = 21;

		[Summary("Number of pixels to move the button in the line so it is in the middle")]
		public const int D_SORTBUTTON_LINE_OFFSET = 9;
		[Summary("Number of pixels to move the text to the right so it is readable next to the sort buttons")]
		public const int D_SORTBUTTON_COL_OFFSET = 11;

		//not used and not necessary so far...
		//[Summary("The TiledButton component class - it handles the tiled button writing to the client")]
		//public class TiledButton : LeafGUTAComponent {
		//    protected static string stringDescription = "Tiled Button";

		//    private ButtonGump gumps;
		//    private int page = 0;
		//    private bool active = true;

		//    private int itemID, hue;

		//    [Summary("Button vertical alignment")]
		//    protected DialogAlignment valign = DialogAlignment.Valign_Top;
		//    [Summary("Button horizontal alignment")]
		//    protected DialogAlignment align = DialogAlignment.Align_Left;

		//    internal TiledButton(int id, int xPos, int yPos, ButtonGump gumps, bool active, int page, DialogAlignment valign, int itemID, int hue, int width, int height) {
		//        this.id = id;
		//        this.xPos = xPos;
		//        this.yPos = yPos;
		//        this.gumps = gumps;
		//        this.active = active;
		//        this.page = page;
		//        this.valign = valign;

		//        this.itemID = itemID;
		//        this.width = width;
		//        this.height = height;
		//        this.hue = hue;
		//    }

		//    [Summary("Basic constructor awaiting the buttons ID and string value of UP and DOWN gump graphic ids")]
		//    internal TiledButton(int id, int xPos, int yPos, ButtonGump gumps, bool active, int page, DialogAlignment valign, GumpIDs itemID, Hues hue, int width, int height)
		//        :
		//        this(id, xPos, yPos, gumps, active, page, valign, (int) itemID, (int) hue, width, height) {
		//    }

		//    [Summary("When added, we must recompute the TiledButtons absolute position in the dialog (we " +
		//            " were provided only relative positions")]
		//    protected override void OnBeforeWrite(GUTAComponent parent) {
		//        //set the level
		//        level = parent.Level + 1;

		//        //get the grandparent (GUTARow) (parent is GUTAColumn!)
		//        GUTARow grandpa = (GUTARow) parent.Parent;
		//        //set the column row (counted from the relative position and the grandpa's inner-row height)
		//        columnRow = xPos / grandpa.RowHeight;

		//        int valignOffset = 0;
		//        switch (valign) {
		//            case DialogAlignment.Valign_Center:
		//                valignOffset = grandpa.RowHeight / 2 - height / 2; //moves the button to the middle of the column
		//                break;
		//            case DialogAlignment.Valign_Bottom:
		//                valignOffset = grandpa.RowHeight - height; //moves the button to the bottom
		//                break;
		//        }
		//        //no space here, the used button gumps have themselves some space...
		//        xPos += parent.XPos;
		//        yPos += parent.YPos + valignOffset;
		//    }

		//    [Summary("Simply write the tiled button (send the method request to the underlaying gump)")]
		//    internal override void WriteComponent() {
		//        gump.AddTiledButton(xPos, yPos, gumps.GumpDown, gumps.GumpUp, active, page, id, itemID, hue, width, height);
		//    }

		//    public override string ToString() {
		//        string linesTabsOffset = "\r\n"; //at least one row
		//        //add as much rows as is the row which this item lies in
		//        for (int i = 0; i < columnRow; i++) {
		//            linesTabsOffset += "\r\n";
		//        }
		//        for (int i = 0; i < level; i++) {
		//            linesTabsOffset += "\t";
		//        }
		//        return linesTabsOffset + "->" + stringDescription;
		//    }
		//}
	}

	[Summary("The Button component class - it handles the button writing to the client")]
	public class GUTAButton : LeafGUTAComponent {
		protected static string stringDescription = "Button";

		protected static Dictionary<LeafComponentTypes, ButtonGump> buttonGumps = new Dictionary<LeafComponentTypes, ButtonGump>();

		static GUTAButton() {
			//0fb1, 0fb3 Cross button
			buttonGumps.Add(LeafComponentTypes.ButtonCross, new ButtonGump(4017, 4019));
			//0fb7, 0fb9 OK button
			buttonGumps.Add(LeafComponentTypes.ButtonOK, new ButtonGump(4023, 4025));
			//0fa5, 0fa7 Tick button
			buttonGumps.Add(LeafComponentTypes.ButtonTick, new ButtonGump(4005, 4007));
			//0fae, 0fb0 Back button
			buttonGumps.Add(LeafComponentTypes.ButtonBack, new ButtonGump(4014, 4016));
			//0fab, 0fad Paper button
			buttonGumps.Add(LeafComponentTypes.ButtonPaper, new ButtonGump(4011, 4013));
			//0fbd, 0fbf Send button
			buttonGumps.Add(LeafComponentTypes.ButtonSend, new ButtonGump(4029, 4031));
			//0fa, 0fb Previous page button
			buttonGumps.Add(LeafComponentTypes.ButtonPrev, new ButtonGump(250, 251));
			//0fc, 0fd Next page button
			buttonGumps.Add(LeafComponentTypes.ButtonNext, new ButtonGump(252, 253));
			//0fa8, 0faa People button
			buttonGumps.Add(LeafComponentTypes.ButtonPeople, new ButtonGump(4008, 4010));
			//0983, 0984 Sort up button
			buttonGumps.Add(LeafComponentTypes.ButtonSortUp, new ButtonGump(2435, 2436));
			//0985, 0986 Sort down button
			buttonGumps.Add(LeafComponentTypes.ButtonSortDown, new ButtonGump(2437, 2438));
			//0fb4, 0fb6 Crossed circle button 
			buttonGumps.Add(LeafComponentTypes.ButtonNoOperation, new ButtonGump(4020, 4022));

			//0d2, 0d3 Checkbox (unchecked, checked)
			buttonGumps.Add(LeafComponentTypes.CheckBox, new ButtonGump(210, 211));
			//0d0, 0d1 Radiobutton (unselected, selected)
			buttonGumps.Add(LeafComponentTypes.RadioButton, new ButtonGump(208, 209));
		}

		[Summary("Flyweight struct carrying info about the two button gumps (pressed and released)" +
				"it will be used when building the dialog buttons for storing info about the gumps.")]
		protected struct ButtonGump {
			private int gumpUp, //also unchecked checkbox and unselected radiobutton
						gumpDown; //also checked checkbox and selected radiobutton

			internal ButtonGump(int gumpUp, int gumpDown) {
				this.gumpUp = gumpUp;
				this.gumpDown = gumpDown;
			}

			internal int GumpUp {
				get {
					return gumpUp;
				}
			}

			internal int GumpDown {
				get {
					return gumpDown;
				}
			}
		}

		protected ButtonGump gumps;
		private int page = 0;
		private bool active = true;

		[Summary("Button vertical alignment")]
		protected DialogAlignment valign = DialogAlignment.Valign_Top;
		
		public static ButtonBuilder Builder {
			get {
				return new ButtonBuilder();
			}
		}

		[Summary("Builder class for the Text LeafGUTAComponent. Allows to set some or all necessary parameters via methods")]
		public class ButtonBuilder : Builder<GUTAButton> {
			//prepare the default values
			internal int xPos = 0;
			internal int yPos = 0;
			internal int id = 0;
			internal DialogAlignment valign = DialogAlignment.Valign_Top;
			internal bool active = true;
			internal int page = 0;
			internal LeafComponentTypes type = LeafComponentTypes.ButtonTick;

			internal ButtonBuilder() {
			}			

			[Summary("Set the button's relative X position")]
			public ButtonBuilder XPos(int val) {
				xPos = val;
				return this;
			}

			[Summary("Set the button's relative Y position")]
			public ButtonBuilder YPos(int val) {
				yPos = val;
				return this;
			}
			
			[Summary("Set the button's ID")]
			public ButtonBuilder Id(int val) {
				id = val;
				return this;
			}

			[Summary("Set the button's state (active = clickable)")]
			public ButtonBuilder Active(bool val) {
				active = val;
				return this;
			}

			[Summary("Set the button's referenced page")]
			public ButtonBuilder Page(int val) {
				page = val;
				return this;
			}

			[Summary("Set the button's up and down graphics (using enumeration of types)")]
			public ButtonBuilder Type(LeafComponentTypes val) {
				type = val;
				return this;
			}
			
			[Summary("Set the button's vertical algiment")]
			public ButtonBuilder Valign(DialogAlignment val) {
				if (val != DialogAlignment.Valign_Bottom && val != DialogAlignment.Valign_Center && val != DialogAlignment.Valign_Top) {
					throw new SEException(LogStr.Error("Wrong valign used for GUTAButton field: " + val.ToString()));
				}
				valign = val;
				return this;
			}

			[Summary("Create the GUTAButton instance")]
			public override GUTAButton Build() {
				GUTAButton retVal = new GUTAButton(this);
				return retVal;
			}
		}

		protected GUTAButton() {
			//constructor for children
		}

		private GUTAButton(ButtonBuilder builder) {
			this.xPos = builder.xPos;
			this.yPos = builder.yPos;
			this.id = builder.id;
			this.gumps = buttonGumps[builder.type];
			this.active = builder.active;
			this.page = builder.page;
			this.valign = builder.valign;
		}

		[Summary("When added, we must recompute the Buttons absolute position in the dialog (we " +
				" were provided only relative positions")]
		protected override void OnBeforeWrite(GUTAComponent parent) {
			//set the level
			level = parent.Level + 1;

			//get the grandparent (GUTARow) (parent is GUTAColumn!)
			GUTARow grandpa = (GUTARow) parent.Parent;
			//set the column row (counted from the relative position and the grandpa's inner-row height)
			columnRow = xPos / grandpa.RowHeight;

			int valignOffset = 0;
			switch (valign) {
				case DialogAlignment.Valign_Center:
					valignOffset = grandpa.RowHeight / 2 - ButtonMetrics.D_BUTTON_HEIGHT / 2; //moves the button to the middle of the column
					break;
				case DialogAlignment.Valign_Bottom:
					valignOffset = grandpa.RowHeight - ButtonMetrics.D_BUTTON_HEIGHT; //moves the button to the bottom
					break;
			}
			//no space here, the used button gumps have themselves some space...
			xPos += parent.XPos;
			yPos += parent.YPos + valignOffset;
		}

		[Summary("Simply write the button (send the method request to the underlaying gump)")]
		internal override void WriteComponent() {
			gump.AddButton(xPos, yPos, gumps.GumpDown, gumps.GumpUp, active, 0, id);
		}

		public override string ToString() {
			string linesTabsOffset = "\r\n"; //at least one row
			//add as much rows as is the row which this item lies in
			for (int i = 0; i < columnRow; i++) {
				linesTabsOffset += "\r\n";
			}
			for (int i = 0; i < level; i++) {
				linesTabsOffset += "\t";
			}
			return linesTabsOffset + "->" + stringDescription;
		}
	}

	[Summary("Create a nice small checkbox")]
	public class GUTACheckBox : GUTAButton {
		protected new static string stringDescription = "CheckBox";

		private bool isChecked;
		[Summary("Checkbox's horizontal alignment")]
		private DialogAlignment align = DialogAlignment.Align_Center;

		public new static CheckBoxBuilder Builder {
			get {
				return new CheckBoxBuilder();
			}
		}

		[Summary("Builder class for the Checkbox LeafGUTAComponent. Allows to set some or all necessary parameters via methods")]
		public class CheckBoxBuilder : Builder<GUTACheckBox> {
			//prepare the default values
			internal int xPos = 0;
			internal int yPos = 0;
			internal int id = 0;
			internal DialogAlignment valign = DialogAlignment.Valign_Center;
			internal DialogAlignment align = DialogAlignment.Align_Center;
			internal bool isChecked = false;
			internal LeafComponentTypes type = LeafComponentTypes.CheckBox;

			internal CheckBoxBuilder() {
			}

			[Summary("Set the checkbox's relative X position")]
			public CheckBoxBuilder XPos(int val) {
				xPos = val;
				return this;
			}

			[Summary("Set the checkbox's relative Y position")]
			public CheckBoxBuilder YPos(int val) {
				yPos = val;
				return this;
			}

			[Summary("Set the checkbox's ID")]
			public CheckBoxBuilder Id(int val) {
				id = val;
				return this;
			}

			[Summary("Set the checkbox's state (checked/unchecked)")]
			public CheckBoxBuilder Checked(bool val) {
				isChecked = val;
				return this;
			}

			[Summary("Set the checkbox's up and down graphics (using enumeration of types)")]
			public CheckBoxBuilder Type(LeafComponentTypes val) {
				type = val;
				return this;
			}
			
			[Summary("Set the checkbox's vertical algiment")]
			public CheckBoxBuilder Valign(DialogAlignment val) {
				if (val != DialogAlignment.Valign_Bottom && val != DialogAlignment.Valign_Center && val != DialogAlignment.Valign_Top) {
					throw new SEException(LogStr.Error("Wrong valign used for GUTACheckBox field: " + val.ToString()));
				}
				valign = val;
				return this;
			}

			[Summary("Set the checkbox's horizontal algiment")]
			public CheckBoxBuilder Align(DialogAlignment val) {
				if (val != DialogAlignment.Align_Center && val != DialogAlignment.Align_Left && val != DialogAlignment.Align_Right) {
					throw new SEException(LogStr.Error("Wrong align used for GUTACheckBox field: " + val.ToString()));
				}
				align = val;
				return this;
			}

			[Summary("Create the GUTACheckBox instance")]
			public override GUTACheckBox Build() {
				GUTACheckBox retVal = new GUTACheckBox(this);
				return retVal;
			}
		}

		private GUTACheckBox(CheckBoxBuilder builder) {
			this.xPos = builder.xPos;
			this.yPos = builder.yPos;
			this.gumps = buttonGumps[builder.type];
			this.isChecked = builder.isChecked;
			this.id = builder.id;
			this.valign = builder.valign;
			this.align = builder.align;
		}

		[Summary("When added, we must recompute the Checkbox's absolute position in the dialog (we " +
				" were provided only relative positions")]
		protected override void OnBeforeWrite(GUTAComponent parent) {
			//set the level
			level = parent.Level + 1;

			//get the grandparent (GUTARow) (parent is GUTAColumn!)
			GUTARow grandpa = (GUTARow) parent.Parent;
			//set the column row (counted from the relative position and the grandpa's inner-row height)
			columnRow = xPos / grandpa.RowHeight;

			int valignOffset = 0;
			int alignOffset = 0;
			
			switch (valign) {
				case DialogAlignment.Valign_Center:
					valignOffset = grandpa.RowHeight / 2 - ButtonMetrics.D_CHECKBOX_HEIGHT / 2 + 1; //moves the button to the middle of the column
					break;
				case DialogAlignment.Valign_Bottom:
					valignOffset = grandpa.RowHeight - ButtonMetrics.D_CHECKBOX_HEIGHT; //moves the button to the bottom
					break;
			}
			int parentWidth = parent.Width;
			switch (align) {
				case DialogAlignment.Align_Center:
					alignOffset = parentWidth / 2 - ButtonMetrics.D_CHECKBOX_WIDTH / 2; //moves the text to the middle of the column
					break;
				case DialogAlignment.Align_Right:
					alignOffset = parentWidth - ButtonMetrics.D_CHECKBOX_WIDTH;
					break;
			}
			//no space here, the used button gumps have themselves some space...
			xPos += parent.XPos + alignOffset;
			yPos += parent.YPos + valignOffset;
		}

		[Summary("Simply call the gumps method for writing the checkbox")]
		internal override void WriteComponent() {
			//unchecked!!!,    checked !!!
			gump.AddCheckBox(xPos, yPos, gumps.GumpUp, gumps.GumpDown, isChecked, id);
		}
	}

	[Summary("A class representing the radio button")]
	public class GUTARadioButton : GUTAButton {
		protected new static string stringDescription = "Radio";

		[Summary("Radiobutton's horizontal alignment")]
		private DialogAlignment align = DialogAlignment.Align_Center;
		private bool isChecked;

		public new static RadioBuilder Builder {
			get {
				return new RadioBuilder();
			}
		}

		[Summary("Builder class for the Radiobutton LeafGUTAComponent. Allows to set some or all necessary parameters via methods")]
		public class RadioBuilder : Builder<GUTARadioButton> {
			//prepare the default values
			internal int xPos = 0;
			internal int yPos = 0;
			internal int id = 0;
			internal DialogAlignment valign = DialogAlignment.Valign_Center;
			internal DialogAlignment align = DialogAlignment.Align_Center;
			internal bool isChecked = false;
			internal LeafComponentTypes type = LeafComponentTypes.RadioButton;

			internal RadioBuilder() {
			}

			[Summary("Set the radiobutton's relative X position")]
			public RadioBuilder XPos(int val) {
				xPos = val;
				return this;
			}

			[Summary("Set the radiobutton's relative Y position")]
			public RadioBuilder YPos(int val) {
				yPos = val;
				return this;
			}

			[Summary("Set the radiobutton's ID")]
			public RadioBuilder Id(int val) {
				id = val;
				return this;
			}

			[Summary("Set the radiobutton's state (checked/unchecked)")]
			public RadioBuilder Checked(bool val) {
				isChecked = val;
				return this;
			}

			[Summary("Set the radiobutton's checked/unchecked graphics (using enumeration of types)")]
			public RadioBuilder Type(LeafComponentTypes val) {
				type = val;
				return this;
			}

			[Summary("Set the radiobutton's vertical algiment")]
			public RadioBuilder Valign(DialogAlignment val) {
				if (val != DialogAlignment.Valign_Bottom && val != DialogAlignment.Valign_Center && val != DialogAlignment.Valign_Top) {
					throw new SEException(LogStr.Error("Wrong valign used for GUTARadioButton field: " + val.ToString()));
				}
				valign = val;
				return this;
			}

			[Summary("Set the radiobutton's horizontal algiment")]
			public RadioBuilder Align(DialogAlignment val) {
				if (val != DialogAlignment.Align_Center && val != DialogAlignment.Align_Left && val != DialogAlignment.Align_Right) {
					throw new SEException(LogStr.Error("Wrong align used for GUTARadioButton field: " + val.ToString()));
				}
				align = val;
				return this;
			}

			[Summary("Create the GUTARadioButton instance")]
			public override GUTARadioButton Build() {
				GUTARadioButton retVal = new GUTARadioButton(this);
				return retVal;
			}
		}

		private GUTARadioButton(RadioBuilder builder) {
			this.xPos = builder.xPos;
			this.yPos = builder.yPos;
			this.gumps = buttonGumps[builder.type];
			this.isChecked = builder.isChecked;
			this.id = builder.id;
			this.valign = builder.valign;
			this.align = builder.align;
		}

		[Summary("When added, we must recompute the Radiobutton's absolute position in the dialog (we " +
				" were provided only relative positions")]
		protected override void OnBeforeWrite(GUTAComponent parent) {
			//set the level
			level = parent.Level + 1;

			//get the grandparent (GUTARow) (parent is GUTAColumn!)
			GUTARow grandpa = (GUTARow) parent.Parent;
			//set the column row (counted from the relative position and the grandpa's inner-row height)
			columnRow = xPos / grandpa.RowHeight;

			int valignOffset = 0;
			int alignOffset = 0;

			switch (valign) {
				case DialogAlignment.Valign_Center:
					valignOffset = grandpa.RowHeight / 2 - ButtonMetrics.D_RADIO_HEIGHT / 2 + 1; //moves the button to the middle of the column
					break;
				case DialogAlignment.Valign_Bottom:
					valignOffset = grandpa.RowHeight - ButtonMetrics.D_RADIO_HEIGHT; //moves the button to the bottom
					break;
			}
			int parentWidth = parent.Width;
			switch (align) {
				case DialogAlignment.Align_Center:
					alignOffset = parentWidth / 2 - ButtonMetrics.D_RADIO_WIDTH / 2; //moves the text to the middle of the column
					break;
				case DialogAlignment.Align_Right:
					alignOffset = parentWidth - ButtonMetrics.D_RADIO_WIDTH;
					break;
			}
			//no space here, the used button gumps have themselves some space...
			xPos += parent.XPos + alignOffset;
			yPos += parent.YPos + valignOffset;
		}

		[Summary("Simply call the gumps method for writing the radiobutton")]
		internal override void WriteComponent() {
			//unselected!!!, selected!!!
			gump.AddRadio(xPos, yPos, gumps.GumpUp, gumps.GumpDown, isChecked, id);
		}
	}

	[Summary("The Button component class - it handles the button writing to the client")]
	public class GUTAInput : LeafGUTAComponent {
		private LeafComponentTypes type;
		[Summary("We have either ID of the used (pre-)text, or the text string itself")]
		private int textId;
		private string text;
		[Summary("The text hue in the input field - if not specified, the default will be used.")]
		private int textHue;

		[Summary("Input field vertical alignment")]
		protected DialogAlignment valign = DialogAlignment.Valign_Top;
		[Summary("Input field horizontal alignment")]
		protected DialogAlignment align = DialogAlignment.Align_Left;

		public static InputBuilder Builder {
			get {
				return new InputBuilder();
			}
		}

		[Summary("Builder class for the Text LeafGUTAComponent. Allows to set some or all necessary parameters via methods")]
		public class InputBuilder : Builder<GUTAInput> {
			//prepare the default values
			internal LeafComponentTypes type = LeafComponentTypes.InputText;
			internal int xPos = 0;
			internal int yPos = 0;
			internal int width = 0;
			internal int id = 100; //some default ID (it will be usually specified as it is necessary for Response implementation...)
			internal int height = ButtonMetrics.D_BUTTON_HEIGHT; //default height is to fit to the rows with buttons (majority of rows use this)
			internal int hue = (int) Hues.WriteColor;
			internal DialogAlignment align = DialogAlignment.Align_Left;
			internal DialogAlignment valign = DialogAlignment.Valign_Bottom;
			internal string text = "";
			internal int textId = 0;

			internal InputBuilder() {
			}			

			[Summary("Set the input field's relative X position")]
			public InputBuilder XPos(int val) {
				xPos = val;
				return this;
			}

			[Summary("Set the input field's relative Y position")]
			public InputBuilder YPos(int val) {
				yPos = val;
				return this;
			}

			[Summary("Set the input field's ID for inside-Response recognition")]
			public InputBuilder Id(int val) {
				id = val;
				return this;
			}

			[Summary("Set the input field's width")]
			public InputBuilder Width(int val) {
				width = val;
				return this;
			}

			[Summary("Set the input field's height")]
			public InputBuilder Height(int val) {
				height = val;
				return this;
			}

			[Summary("Set the input type")]
			public InputBuilder Type(LeafComponentTypes val) {
				type = val;
				return this;
			}

			[Summary("Set the input field's text hue")]
			public InputBuilder Hue(int val) {
				hue = val;
				return this;
			}

			[Summary("Set the input field's text hue usign enum")]
			public InputBuilder Hue(Hues val) {
				hue = (int) val;
				return this;
			}

			[Summary("Set the input field's value")]
			public InputBuilder Text(string val) {
				text = val;
				return this;
			}

			[Summary("Set the input field's text id (for prepared texts)")]
			public InputBuilder TextId(int val) {
				textId = val;
				return this;
			}

			[Summary("Set the input field's text's horizontal algiment")]
			public InputBuilder Align(DialogAlignment val) {
				if (val != DialogAlignment.Align_Center && val != DialogAlignment.Align_Left && val != DialogAlignment.Align_Right) {
					throw new SEException(LogStr.Error("Wrong align used for GUTAInput field: " + val.ToString()));
				}
				align = val;
				return this;
			}

			[Summary("Set the input field's text's vertical algiment")]
			public InputBuilder Valign(DialogAlignment val) {
				if (val != DialogAlignment.Valign_Bottom && val != DialogAlignment.Valign_Center && val != DialogAlignment.Valign_Top) {
					throw new SEException(LogStr.Error("Wrong valign used for GUTAInput field: " + val.ToString()));
				}
				valign = val;
				return this;
			}

			[Summary("Create the GUTAInput instance")]
			public override GUTAInput Build() {
				GUTAInput retVal = new GUTAInput(this);
				return retVal;
			}
		}

		private GUTAInput(InputBuilder builder) {
			this.type = builder.type;
			this.xPos = builder.xPos;
			this.yPos = builder.yPos;
			this.id = builder.id;
			this.width = builder.width;
			this.height = builder.height;
			this.textHue = builder.hue;
			this.align = builder.align;
			this.valign = builder.valign;
			this.text = builder.text;
			this.textId = builder.textId;
		}

		[Summary("When added, we must recompute the Input Field's absolute position in the dialog (we " +
				" were provided only relative positions")]
		protected override void OnBeforeWrite(GUTAComponent parent) {
			//set the level
			level = parent.Level + 1;

			//get the grandparent (GUTARow) (parent is GUTAColumn!)
			GUTARow grandpa = (GUTARow) parent.Parent;
			//set the column row (counted from the relative position and the grandpa's inner-row height)
			columnRow = xPos / grandpa.RowHeight;

			xPos += parent.XPos;
			yPos += parent.YPos;
			if (width == 0) {
				//no width - get it from the parent
				width = parent.Width;
				width -= ImprovedDialog.D_COL_SPACE; //put it between the borders of the column with a little spaces
				//substract also the space from the xPos adjustment of this field (it can be shorter to fit to the column)
				//this makes  sense, if the input field is not at the beginning pos. of the column... - it will shorten it 
				//of the space it is indented from the left border
				width -= (xPos - parent.XPos);
			}

			int valignOffset = 0;
			int alignOffset = 0;
			switch (valign) {
				case DialogAlignment.Valign_Center:
					valignOffset = grandpa.RowHeight / 2 - height / 2 + 1; //moves the field to the middle of the column
					break;
				case DialogAlignment.Valign_Bottom:
					valignOffset = grandpa.RowHeight - height + 1; //moves the field to the bottom
					break;
			}
			int parentWidth = parent.Width;
			switch (align) {
				case DialogAlignment.Align_Center:
					alignOffset = parentWidth / 2 - width / 2; //moves the field to the middle of the column
					break;
				case DialogAlignment.Align_Right:
					alignOffset = parentWidth - width;
					break;
			}
			xPos += alignOffset;
			yPos += valignOffset;
		}

		[Summary("Simply write the input (send the method request to the underlaying gump)" +
				" it will determine also what parameters to send")]
		internal override void WriteComponent() {
			//first of all add a different background
			gump.AddGumpPicTiled(xPos, yPos, width, height, ImprovedDialog.D_DEFAULT_INPUT_BACKGROUND);
			//and make it immediately transparent
			gump.AddCheckerTrans(xPos, yPos, width, height);
			switch (type) {
				case LeafComponentTypes.InputText: {
						if (textId == 0) {//no text ID was specified, use the text version
							gump.AddTextEntry(xPos, yPos, width, height, textHue, id, text);
						} else {
							gump.AddTextEntry(xPos, yPos, width, height, textHue, id, textId);
						}
						break;
					}
				case LeafComponentTypes.InputNumber: {
						if (textId == 0) {//no text ID was specified, use the text version (but send it as double!)
							//if the text is empty (the input field will be empty), then display zero
							double textToDisp = text.Equals("") ? default(double) : double.Parse(text);
							gump.AddNumberEntry(xPos, yPos, width, height, textHue, id, textToDisp);
						} else {
							gump.AddNumberEntry(xPos, yPos, width, height, textHue, id, textId);
						}
						break;
					}
			}
		}

		public override string ToString() {
			string linesTabsOffset = "\r\n";
			//add as much rows as is the row which this item lies in
			for (int i = 0; i < columnRow; i++) {
				linesTabsOffset += "\r\n";
			}
			for (int i = 0; i < level; i++) {
				linesTabsOffset += "\t";
			}
			return linesTabsOffset + "->Input";
		}
	}

	[Summary("The text component class - it handles the text writing to the underlaying gump")]
	public class GUTAText : LeafGUTAComponent {
		[Summary("The text hue in the input field - if not specified, the default will be used.")]
		private int textHue;
		[Summary("We have either ID of the used text, or the text string itself")]
		private int textId;
		private string text;

		[Summary("Text horizontal alignment")]
		private DialogAlignment align;
		[Summary("Text vertical alignment")]
		private DialogAlignment valign;

		public static TextBuilder Builder {
			get {
				return new TextBuilder();
			}
		}

		[Summary("Builder class for the Text LeafGUTAComponent. Allows to set some or all necessary parameters via methods")]
		public class TextBuilder : Builder<GUTAText> {
			//prepare the default values
			internal int xPos = 0;
			internal int yPos = 0;
			internal int hue = (int) Hues.WriteColor;
			internal DialogAlignment align = DialogAlignment.Align_Left;
			internal DialogAlignment valign = DialogAlignment.Valign_Top;
			internal string text = "";
			internal int textId = 0;

			internal TextBuilder() {
			}			

			[Summary("Set the text's relative X position")]
			public TextBuilder XPos(int val) {
				xPos = val;
				return this;
			}

			[Summary("Set the text's relative Y position")]
			public TextBuilder YPos(int val) {
				yPos = val;
				return this;
			}

			[Summary("Set the text's hue")]
			public TextBuilder Hue(int val) {
				hue = val;
				return this;
			}

			[Summary("Set the text's hue usign enum")]
			public TextBuilder Hue(Hues val) {
				hue = (int) val;
				return this;
			}

			[Summary("Create the text as label (set the hue also)")]
			public TextBuilder TextLabel(string val) {
				text = val;
				hue = (int) Hues.LabelColor;
				return this;
			}

			[Summary("Create the text as headline (set the hue also)")]
			public TextBuilder TextHeadline(string val) {
				text = val;
				hue = (int) Hues.HeadlineColor;
				return this;
			}

			[Summary("Set the text value")]
			public TextBuilder Text(string val) {
				text = val;
				return this;
			}

			[Summary("Set the text id (for prepared texts)")]
			public TextBuilder TextId(int val) {
				textId = val;
				return this;
			}

			[Summary("Set the text's horizontal algiment")]
			public TextBuilder Align(DialogAlignment val) {
				if (val != DialogAlignment.Align_Center && val != DialogAlignment.Align_Left && val != DialogAlignment.Align_Right) {
					throw new SEException(LogStr.Error("Wrong align used for GUTAText field: " + val.ToString()));
				}
				align = val;
				return this;
			}

			[Summary("Set the text's vertical algiment")]
			public TextBuilder Valign(DialogAlignment val) {
				if (val != DialogAlignment.Valign_Bottom && val != DialogAlignment.Valign_Center && val != DialogAlignment.Valign_Top) {
					throw new SEException(LogStr.Error("Wrong valign used for GUTAText field: " + val.ToString()));
				}
				valign = val;
				return this;
			}

			[Summary("Create the GUTAText instance")]
			public override GUTAText Build() {
				GUTAText retVal = new GUTAText(this);
				return retVal;
			}
		}

		private GUTAText(TextBuilder builder) {
			this.xPos = builder.xPos;
			this.yPos = builder.yPos;
			this.textHue = builder.hue;
			this.align = builder.align;
			this.valign = builder.valign;
			this.text = builder.text;
			this.textId = builder.textId;
		}

		[Summary("When added to the column we have to specify the position (count the absolute)")]
		protected override void OnBeforeWrite(GUTAComponent parent) {
			//set the level
			level = parent.Level + 1;

			//get the grandparent (GUTARow) (parent is GUTAColumn!)
			GUTARow grandpa = (GUTARow) parent.Parent;
			//set the column row (counted from the relative position and the grandpa's inner-row height)
			columnRow = xPos / grandpa.RowHeight;

			int alignOffset = 0;
			int valignOffset = 0;
			if (text != null) { //we are not using the ID of the text, we can do some alignment computings if necessary
				int parentWidth = parent.Width;
				int textWidth = ImprovedDialog.TextLength(text);
				switch (align) {
					case DialogAlignment.Align_Center:
						alignOffset = parentWidth / 2 - textWidth / 2; //moves the text to the middle of the column
						break;
					case DialogAlignment.Align_Right:
						alignOffset = parentWidth - textWidth - 1; //moves the text to the right (1 pix added - it is the border)
						break;
				}
				switch (valign) {
					case DialogAlignment.Valign_Center:
						valignOffset = grandpa.RowHeight / 2 - ImprovedDialog.D_TEXT_HEIGHT / 2; //moves the text to the middle of the column
						break;
					case DialogAlignment.Valign_Bottom:
						valignOffset = grandpa.RowHeight - ImprovedDialog.D_CHARACTER_HEIGHT; //moves the text to the bottom
						break;
				}
			}
			xPos += parent.XPos + alignOffset;
			yPos += parent.YPos + valignOffset;

			if (text == null) {
				text = "null"; //we cannot display null so stringify it
			}
		}

		[Summary("Call the underlaying gump istance's methods")]
		internal override void WriteComponent() {
			if (textId == 0) { //no text ID was specified, use the text version
				gump.AddText(xPos, yPos, textHue, text);
			} else {
				gump.AddText(xPos, yPos, textHue, textId);
			}
		}

		public override string ToString() {
			string linesTabsOffset = "\r\n";
			//add as much rows as is the row which this item lies in
			for (int i = 0; i < columnRow; i++) {
				linesTabsOffset += "\r\n";
			}
			for (int i = 0; i < level; i++) {
				linesTabsOffset += "\t";
			}
			return linesTabsOffset + "->GUTAText(" + text + ")";
		}
	}

	
	[Summary("The HTML text component class - allows making the text scrollable")]
	public class GUTAHTMLText : LeafGUTAComponent {
		private bool isScrollable, hasBoundBox;
		[Summary("The text is either specified or passed as a text id")]
		private string text;
		private int textId;

		public static HTMLTextBuilder Builder {
			get {
				return new HTMLTextBuilder();
			}
		}

		[Summary("Builder class for the Text LeafGUTAComponent. Allows to set some or all necessary parameters via methods")]
		public class HTMLTextBuilder : Builder<GUTAHTMLText> {
			//prepare the default values
			internal int xPos = 0;
			internal int yPos = 0;
			internal bool isScrollable = false;
			internal bool hasBoundBox = false;
			internal string text = "";
			internal int textId = 0;

			internal HTMLTextBuilder() {
			}			

			[Summary("Set the text's relative X position")]
			public HTMLTextBuilder XPos(int val) {
				xPos = val;
				return this;
			}

			[Summary("Set the text's relative Y position")]
			public HTMLTextBuilder YPos(int val) {
				yPos = val;
				return this;
			}
			
			[Summary("Set the text's scrollable state")]
			public HTMLTextBuilder Scrollable(bool val) {
				isScrollable = val;
				return this;
			}

			[Summary("Set the text's scrollable state")]
			public HTMLTextBuilder HasBoundBox(bool val) {
				hasBoundBox = val;
				return this;
			}
			
			[Summary("Set the text value")]
			public HTMLTextBuilder Text(string val) {
				text = val;
				return this;
			}

			[Summary("Set the text id (for prepared texts)")]
			public HTMLTextBuilder TextId(int val) {
				textId = val;
				return this;
			}
			
			[Summary("Create the GUTAText instance")]
			public override GUTAHTMLText Build() {
				GUTAHTMLText retVal = new GUTAHTMLText(this);
				return retVal;
			}
		}
		
		private GUTAHTMLText(HTMLTextBuilder builder) {
			this.xPos = builder.xPos;
			this.yPos = builder.yPos;
			this.hasBoundBox = builder.hasBoundBox;
			this.isScrollable = builder.isScrollable;
			this.text = builder.text;
			this.textId = builder.textId;
		}


		private GUTAHTMLText(int x, int y, int width, int height, bool hasBoundBox, bool isScrollable) {
			this.xPos = x;
			this.yPos = y;
			this.width = width;
			this.height = height;
			this.hasBoundBox = hasBoundBox;
			this.isScrollable = isScrollable;
		}		

		[Summary("When added to the column we have to specify the position (count the absolute)")]
		protected override void OnBeforeWrite(GUTAComponent parent) {
			//set the level
			level = parent.Level + 1;

			//get the grandparent (GUTARow) (parent is GUTAColumn!)
			GUTARow grandpa = (GUTARow) parent.Parent;
			//set the column row (counted from the relative position and the grandpa's inner-row height)
			columnRow = xPos / grandpa.RowHeight;

			//dont use spaces here or the text is glued to the bottom of the line on the single lined inputs
			xPos += parent.XPos;
			yPos += parent.YPos;
			//if not specified, take the size from the parent
			if (height == 0) {
				height = parent.Height;
			}
			if (width == 0) {
				width = parent.Width;
			}
		}

		[Summary("Call the underlaying gump istance's methods")]
		internal override void WriteComponent() {
			if (textId == 0) { //no text ID was specified, use the text version
				gump.AddHTMLGump(xPos, yPos, width, height, text, hasBoundBox, isScrollable);
			} else {
				gump.AddHTMLGump(xPos, yPos, width, height, textId, hasBoundBox, isScrollable);
			}
		}

		public override string ToString() {
			string linesTabsOffset = "\r\n";
			//add as much rows as is the row which this item lies in
			for (int i = 0; i < columnRow; i++) {
				linesTabsOffset += "\r\n";
			}
			for (int i = 0; i < level; i++) {
				linesTabsOffset += "\t";
			}
			return linesTabsOffset + "->HTMLText(" + text + ")";
		}
	}

	public class GUTAImage : LeafGUTAComponent {
		private int gumpId;
		private int color;

		[Summary("Image horizontal alignment")]
		private DialogAlignment align;
		[Summary("Image vertical alignment")]
		private DialogAlignment valign;

		public static ImageBuilder Builder {
			get {
				return new ImageBuilder();
			}
		}

		[Summary("Builder class for the GUTAImage LeafGUTAComponent. Allows to set some or all necessary parameters via methods")]
		public class ImageBuilder : Builder<GUTAImage> {
			//prepare the default values
			internal int xPos = 0;
			internal int yPos = 0;
			internal DialogAlignment align = DialogAlignment.Align_Center;
			internal DialogAlignment valign = DialogAlignment.Valign_Center;
			internal int gumpId = 0;
			internal int color = 0;

			internal ImageBuilder() {
			}

			[Summary("Set the image's relative X position")]
			public ImageBuilder XPos(int val) {
				xPos = val;
				return this;
			}

			[Summary("Set the image's relative Y position")]
			public ImageBuilder YPos(int val) {
				yPos = val;
				return this;
			}

			[Summary("Set the image's horizontal alignment")]
			public ImageBuilder Align(DialogAlignment val) {
				if (val != DialogAlignment.Align_Center && val != DialogAlignment.Align_Left && val != DialogAlignment.Align_Right) {
					throw new SEException(LogStr.Error("Wrong align used for GUTAImage field: " + val.ToString()));
				}
				align = val;
				return this;
			}

			[Summary("Set the image's vertical alignment")]
			public ImageBuilder Valign(DialogAlignment val) {
				if (val != DialogAlignment.Valign_Bottom && val != DialogAlignment.Valign_Center && val != DialogAlignment.Valign_Top) {
					throw new SEException(LogStr.Error("Wrong valign used for GUTAImage field: " + val.ToString()));
				}
				valign = val;
				return this;
			}

			[Summary("Set the image's gump")]
			public ImageBuilder Gump(int val) {
				gumpId = val;
				return this;
			}

			[Summary("Set the image's gump using enumeration")]
			public ImageBuilder NamedGump(GumpIDs val) {
				gumpId = (int) val;
				return this;
			}

			[Summary("Set the image's hue (color)")]
			public ImageBuilder Hue(int val) {
				this.color = (int) val;
				return this;
			}

			[Summary("Set the image's hue (color)")]
			public ImageBuilder Color(int val) {
				this.color = (int) val;
				return this;
			}

			[Summary("Create the GUTAImage instance")]
			public override GUTAImage Build() {
				GUTAImage retVal = new GUTAImage(this);
				return retVal;
			}
		}

		private GUTAImage(ImageBuilder builder) {
			this.xPos = builder.xPos;
			this.yPos = builder.yPos;
			this.align = builder.align;
			this.valign = builder.valign;
			this.gumpId = builder.gumpId;
			this.color = builder.color;
		}

		[Summary("When added to the column we have to specify the position (count the absolute)")]
		protected override void OnBeforeWrite(GUTAComponent parent) {
			//set the level
			level = parent.Level + 1;

			//get the grandparent (GUTARow) (parent is GUTAColumn!)
			GUTARow grandpa = (GUTARow) parent.Parent;
			//set the column row (counted from the relative position and the grandpa's inner-row height)
			columnRow = xPos / grandpa.RowHeight;

			GumpArtDimension picDim = GumpDimensions.Table[gumpId];

			int alignOffset = -picDim.X; //at least...
			int valignOffset = -picDim.Y; //at least...

			switch (align) {
				case DialogAlignment.Align_Center:
					alignOffset += parent.Width / 2 - picDim.Width / 2; //moves the image to the middle of the column
					break;
				case DialogAlignment.Align_Right:
					alignOffset += parent.Width - picDim.Width - 1; //moves the image to the right (1 pix added - it is the border)
					break;
			}
			switch (valign) {
				case DialogAlignment.Valign_Center:
					valignOffset += grandpa.RowHeight / 2 - picDim.Height / 2; //moves the image to the middle of the column
					break;
				case DialogAlignment.Valign_Bottom:
					valignOffset += grandpa.RowHeight - picDim.Height; //moves the image to the bottom
					break;
			}
			xPos += parent.XPos + alignOffset;
			yPos += parent.YPos + valignOffset;
		}

		[Summary("Call the underlaying gump istance's methods")]
		internal override void WriteComponent() {
			if (this.color == 0) {
				gump.AddTilePic(xPos, yPos, gumpId);
			} else {
				gump.AddTilePicHue(this.xPos, this.yPos, this.gumpId, this.color);
			}
		}

		public override string ToString() {
			string linesTabsOffset = "\r\n";
			//add as much rows as is the row which this item lies in
			for (int i = 0; i < columnRow; i++) {
				linesTabsOffset += "\r\n";
			}
			for (int i = 0; i < level; i++) {
				linesTabsOffset += "\t";
			}
			return linesTabsOffset + "->Image(" + gumpId + ")";
		}
	}
}