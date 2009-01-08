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

	[Summary("Factory class for creating buttons according to the given button type. Including special buttons"+
            " such as Checkbox or Radio Button")]
	public class ButtonFactory {
        internal static Dictionary<LeafComponentTypes,ButtonGump> buttonGumps = new Dictionary<LeafComponentTypes,ButtonGump>();

        static ButtonFactory() {
            //0fb1, 0fb3 Cross button
            buttonGumps.Add(LeafComponentTypes.ButtonCross, new ButtonGump(4017, 4019));
            //0fb7, 0fb9 OK button
            buttonGumps.Add(LeafComponentTypes.ButtonOK, new ButtonGump(4023, 4025));
            //0fa5, 0fa7 Tick button
            buttonGumps.Add(LeafComponentTypes.ButtonTick, new ButtonGump(4005, 4007));
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

		public const int D_BUTTON_WIDTH = 31;
		public const int D_BUTTON_HEIGHT = 22;
		public const int D_BUTTON_PREVNEXT_WIDTH = 16;
		public const int D_BUTTON_PREVNEXT_HEIGHT = 21;

		[Summary("Number of pixels to move the button in the line so it is in the middle")]
		public const int D_SORTBUTTON_LINE_OFFSET =	9;
		[Summary("Number of pixels to move the text to the right so it is readable next to the sort buttons")]
		public const int D_SORTBUTTON_COL_OFFSET = 11;

        [Summary("Flyweight class carrying info about the two button gumps (pressed and released)"+
                "it will be used when building the dialog buttons for storing info about the gumps.")]
        internal class ButtonGump {
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

		[Summary("Factory method for creating a given type of the button. We have to provide the _relative_"+
                "x and y position (relative to the parent column) and the button ID, we have to also specify the page "+
                "opened by this button and whether the button is active or not. Allows specifying the valign")]
        public static Button CreateButton(LeafComponentTypes type, int xPos, int yPos, bool active, int page, int id, DialogAlignment valign) {
			return new Button(id, xPos, yPos, buttonGumps[type], active, page, valign);
		}

		[Summary("Basic method - it adds the button automatically to the beginning of the column.")]
		public static Button CreateButton(LeafComponentTypes type, int id) {
			return CreateButton(type, 0, 0, true, 0, id, DialogAlignment.Valign_Top);
		}

		[Summary("Basic method - it adds the button automatically to the beginning of the column. "+
				 "Allows specifying the valign")]
		public static Button CreateButton(LeafComponentTypes type, int id, DialogAlignment valign) {
			return CreateButton(type, 0, 0, true, 0, id, valign);
		}

		[Summary("Basic method - allows to specify the button position")]
        public static Button CreateButton(LeafComponentTypes type, int xPos, int yPos, int id) {
			return CreateButton(type, xPos, yPos, true, 0, id, DialogAlignment.Valign_Top);
		}

		[Summary("Basic method - allows to specify the button position and activity")]
        public static Button CreateButton(LeafComponentTypes type, int xPos, int yPos, bool active, int id) {
			return CreateButton(type, xPos, yPos, active, 0, id, DialogAlignment.Valign_Top);
		}

		[Summary("Basic method - it adds the button automatically to the beginning of the column"+
                "allows us to specify if the button is active or not")]
        public static Button CreateButton(LeafComponentTypes type, bool active, int id) {
			return CreateButton(type, 0, 0, active, 0, id, DialogAlignment.Valign_Top);
		}

		[Summary("Basic method - it adds the button automatically to the beginning of the column" +
                "allows us to specify if the button is active or not and the page opened")]
        public static Button CreateButton(LeafComponentTypes type, bool active, int page, int id) {
			return CreateButton(type, 0, 0, active, page, id, DialogAlignment.Valign_Top);
		}

		[Summary("Create a checkbox using the _relative_ position in the column, the check/unchecked flag"+
                "and the id")]
		public static CheckBox CreateCheckbox(int xPos, int yPos, bool isChecked, int id, DialogAlignment valign) {
			return new CheckBox(xPos, yPos, buttonGumps[LeafComponentTypes.CheckBox], isChecked, id, DialogAlignment.Valign_Top);
		}

		[Summary("Create checkbox using the 1 or 0 as a marker if the checkbox is checked or not")]
		public static CheckBox CreateCheckbox(int xPos, int yPos, int isChecked, int id) {
			return CreateCheckbox(xPos, yPos, isChecked == 1 ? true : false, id, DialogAlignment.Valign_Top);
		}

		[Summary("Create a radio button using the _relative_ position in the column, the check/unchecked flag" +
                "and the id")]
		public static RadioButton CreateRadio(int xPos, int yPos, bool isChecked, int id, DialogAlignment valign) {
			return new RadioButton(xPos, yPos, buttonGumps[LeafComponentTypes.RadioButton], isChecked, id, DialogAlignment.Valign_Top);
		}

		[Summary("Create radio button using the 1 or 0 as a marker if the checkbox is checked or not")]
		public static RadioButton CreateRadio(int xPos, int yPos, int isChecked, int id) {
			return CreateRadio(xPos, yPos, isChecked == 1 ? true : false, id, DialogAlignment.Valign_Top);
		}

		[Summary("The Button component class - it handles the button writing to the client")]
		public class Button : LeafGUTAComponent {
			protected static string stringDescription = "Button";

			private ButtonGump gumps;
			private int page = 0;
			private bool active = true;

			[Summary("Text vertical alignment")]
			protected DialogAlignment valign = DialogAlignment.Valign_Top;

			[Summary("This constructor is here only for the buttons children classes")]
			internal Button() {
			}

			[Summary("Basic constructor awaiting the buttons ID and string value of UP and DOWN gump graphic ids")]
			internal Button(int id, int xPos, int yPos, ButtonGump gumps, bool active, int page, DialogAlignment valign) {
				this.id = id;
				this.xPos = xPos;
				this.yPos = yPos;
                this.gumps = gumps;
                this.active = active;
				this.page = page;
				this.valign = valign;
			}

			[Summary("When added, we must recompute the Buttons absolute position in the dialog (we "+
                    " were provided only relative positions")]
			protected override void OnBeforeWrite(GUTAComponent parent) {
				//set the level
				level = parent.Level + 1;

				//get the grandparent (GUTATable) (parent is GUTAColumn!)
				GUTATable grandpa = (GUTATable) parent.Parent;
				//set the column row (counted from the relative position and the grandpa's inner-row height)
				columnRow = xPos / grandpa.RowHeight;

				int valignOffset = 0;
				switch (valign) {
					case DialogAlignment.Valign_Center:
						valignOffset = grandpa.RowHeight / 2 - ButtonFactory.D_BUTTON_HEIGHT / 2; //moves the button to the middle of the column
						break;
					case DialogAlignment.Valign_Bottom:
						valignOffset = grandpa.RowHeight - ButtonFactory.D_BUTTON_HEIGHT; //moves the button to the bottom
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
				for(int i = 0; i < columnRow; i++) {
					linesTabsOffset += "\r\n";
				}
				for(int i = 0; i < level; i++) {
					linesTabsOffset += "\t";
				}
				return linesTabsOffset+"->"+stringDescription;
			}
		}

		[Summary("Create a nice small checkbox")]
		public class CheckBox : Button {
			protected new static string stringDescription = "CheckBox";

			private ButtonGump gumps;
			private bool isChecked;

			[Summary("Creates a checkbox with the given format")]
			internal CheckBox(int xPos, int yPos, ButtonGump gumps, bool isChecked, int id, DialogAlignment valign) {
				this.xPos = xPos;
				this.yPos = yPos;
                this.gumps = gumps;
				this.isChecked = isChecked;
				this.id = id;
				this.valign = valign;
			}
			
			[Summary("Simply call the gumps method for writing the checkbox")]
			internal override void WriteComponent() {
                                             //unchecked!!!,    checked !!!
				gump.AddCheckBox(xPos, yPos, gumps.GumpUp, gumps.GumpDown, isChecked, id);
			}
		}

		[Summary("A class representing the radio button")]
		public class RadioButton : Button {
			protected new static string stringDescription = "Radio";

            private ButtonGump gumps;
			private bool isChecked;

			[Summary("Creates a radio button")]
			internal RadioButton(int xPos, int yPos, ButtonGump gumps, bool isChecked, int id, DialogAlignment valign) {
				this.xPos = xPos;
				this.yPos = yPos;
                this.gumps = gumps;
				this.isChecked = isChecked;
				this.id = id;
				this.valign = valign;
			}			

			[Summary("Simply call the gumps method for writing the radiobutton")]
			internal override void WriteComponent() {
                                         //unselected!!!, selected!!!
				gump.AddRadio(xPos, yPos, gumps.GumpUp,gumps.GumpDown, isChecked, id);
			}
		}
	}

	[Summary("InputFactory creates different type of input fields (basicly TEXT or NUMBER)")]
	public class InputFactory {
		[Summary("Position from the parent, no pre-text specified, default color")]
        public static Input CreateInput(LeafComponentTypes type, int id, int pixelWidth, int height) {
            return new Input(type, 0, 0, id, pixelWidth, height, Hues.WriteColor, "");
		}

		[Summary("Position and width from the parent, no pre-text specified, default color ")]
        public static Input CreateInput(LeafComponentTypes type, int id) {
            return new Input(type, 0, 0, id, 0, 0, Hues.WriteColor, "");
		}

		[Summary("Position and width from the parent, default color but the pre-text is specified here")]
        public static Input CreateInput(LeafComponentTypes type, int id, string text) {
            return new Input(type, 0, 0, id, 0, 0, Hues.WriteColor, text);
		}

		[Summary("Factory method for creating a given type fo the button. We have to specify the button type, the entry ID, pixel and "+
                "character width and optionally the text hue and the text itself "+
                "the position will be determined from the parent")]
        public static Input CreateInput(LeafComponentTypes type, int id, int pixelWidth, int height, Hues textHue, string text) {
			return new Input(type, 0, 0, id, pixelWidth, height, textHue, text);
		}

		[Summary("Position will be determined from the parent, the color will be default")]
        public static Input CreateInput(LeafComponentTypes type, int id, int pixelWidth, int height, string text) {
            return new Input(type, 0, 0, id, pixelWidth, height, Hues.WriteColor, text);
		}

		[Summary("Position will be determined from the parent, the color will be default, the text may be only the specified Id")]
        public static Input CreateInput(LeafComponentTypes type, int id, int pixelWidth, int height, int textId) {
            return new Input(type, 0, 0, id, pixelWidth, height, Hues.WriteColor, textId);
		}

		[Summary("Factory method for creating a given type fo the button. We have to specify the x and y pos but we neednt specify the width, height and color here !")]
		public static Input CreateInput(LeafComponentTypes type, Hues textHue, string text, int xPos, int yPos, int id) {
			return new Input(type, xPos, yPos, id, 0, 0, textHue, text);
		}

		[Summary("Factory method for creating a given type fo the button. We have to specify the x and y pos but we neednt specify the width and height here !")]
		public static Input CreateInput(LeafComponentTypes type, string text, int xPos, int yPos, int id) {
			return new Input(type, xPos, yPos, id, 0, 0, Hues.WriteColor, text);
		}

		[Summary("Factory method for creating a given type fo the button. We have to specify the button type, provide "+
                " the relative positions, the entry ID, pixel and chracter width and optionally the text hue and the text itself")]
        public static Input CreateInput(LeafComponentTypes type, int xPos, int yPos, int id, int pixelWidth, int height, Hues textHue, string text) {
			return new Input(type, xPos, yPos, id, pixelWidth, height, textHue, text);
		}

		[Summary("Factory method for creating a given type fo the button. We have to specify the button type, provide "+
                " the relative positions, the entry ID, pixel and chracter width and optionally the text hue and the textId")]
        public static Input CreateInput(LeafComponentTypes type, int xPos, int yPos, int id, int pixelWidth, int height, Hues textHue, int textId) {
			return new Input(type, xPos, yPos, id, pixelWidth, height, textHue, textId);
		}

		[Summary("Factory method for creating a given type fo the button. We have to specify the button type, provide "+
                " the relative positions, the entry ID, pixel and chracter width.")]
        public static Input CreateInput(LeafComponentTypes type, int xPos, int yPos, int id, int pixelWidth, int height) {
            return InputFactory.CreateInput(type, xPos, yPos, id, pixelWidth, height, Hues.WriteColor, "");
		}

		[Summary("Factory method for creating a given type fo the button. We have to specify the button type, provide "+
                " the relative positions, the entry ID, pixel and chracter width, the pre-text.")]
        public static Input CreateInput(LeafComponentTypes type, int xPos, int yPos, int id, int pixelWidth, int height, string text) {
			return InputFactory.CreateInput(type, xPos, yPos, id, pixelWidth, height, Hues.WriteColor, text);
		}

		[Summary("Factory method for creating a given type fo the button. We have to specify the button type, provide "+
                " the relative positions, the entry ID, pixel and chracter width, the pre-text id.")]
        public static Input CreateInput(LeafComponentTypes type, int xPos, int yPos, int id, int pixelWidth, int height, int textId) {
            return InputFactory.CreateInput(type, xPos, yPos, id, pixelWidth, height, Hues.WriteColor, textId);
		}

		[Summary("The Button component class - it handles the button writing to the client")]
		public class Input : LeafGUTAComponent {
			private LeafComponentTypes type;
			[Summary("We have either ID of the used (pre-)text, or the text string itself")]
			private int textId;
			private string text;
			[Summary("The text hue in the input field - if not specified, the default will be used.")]
			private Hues textHue;

			[Summary("First complete constructor - awaits the necessary things and the pre-text in the string form")]
            internal Input(LeafComponentTypes type, int xPos, int yPos, int id, int widthPix, int height, Hues textHue, string text) {
				this.id = id;
				this.xPos = xPos;
				this.yPos = yPos;
				this.type = type;
				this.width = widthPix;
				this.height = height;
				this.textHue = textHue;
				this.text = text;
			}

			[Summary("Second complete constructor - awaits the necessary things and the pre-text as the string id")]
            internal Input(LeafComponentTypes type, int xPos, int yPos, int id, int widthPix, int height, Hues textHue, int textId) {
				this.id = id;
				this.xPos = xPos;
				this.yPos = yPos;
				this.type = type;
				this.width = widthPix;
				this.height = height;
				this.textHue = textHue;
				this.textId = textId;
			}

			[Summary("When added, we must recompute the Input Field's absolute position in the dialog (we " +
                    " were provided only relative positions")]
			protected override void OnBeforeWrite(GUTAComponent parent) {
				//set the level
				level = parent.Level + 1;

				//get the grandparent (GUTATable) (parent is GUTAColumn!)
				GUTATable grandpa = (GUTATable) parent.Parent;
				//set the column row (counted from the relative position and the grandpa's inner-row height)
				columnRow = xPos / grandpa.RowHeight;

//				//set the column row (counted from the relative position
//				columnRow = xPos / ImprovedDialog.D_ROW_HEIGHT;

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
				if (height == 0) {
					//no height specified, give it the default one row height (which is the height of the buttons)
					height = ButtonFactory.D_BUTTON_HEIGHT;
				}
			}

			[Summary("Simply write the input (send the method request to the underlaying gump)"+
                    " it will determine also what parameters to send")]
			internal override void WriteComponent() {
				//first of all add a different background
				gump.AddGumpPicTiled(xPos, yPos, width, height, ImprovedDialog.D_DEFAULT_INPUT_BACKGROUND);
				//and make it immediately transparent
				gump.AddCheckerTrans(xPos, yPos, width, height);
                switch(type) {
                    case LeafComponentTypes.InputText: {
                            if(textId == 0) {//no text ID was specified, use the text version
                                gump.AddTextEntry(xPos, yPos, width, height, (int)textHue, id, text);
                            } else {
                                gump.AddTextEntry(xPos, yPos, width, height, (int)textHue, id, textId);
                            }
                            break;
                        }
                    case LeafComponentTypes.InputNumber: {
                            if(textId == 0) {//no text ID was specified, use the text version (but send it as double!)
								//if the text is empty (the input field will be empty), then display zero
								double textToDisp = text.Equals("") ? default(double) : double.Parse(text);
								gump.AddNumberEntry(xPos, yPos, width, height, (int)textHue, id, textToDisp);
                            } else {
                                gump.AddNumberEntry(xPos, yPos, width, height, (int)textHue, id, textId);
                            }
                            break;
                        }
                }
			}

			public override string ToString() {
				string linesTabsOffset = "\r\n";
				//add as much rows as is the row which this item lies in
				for(int i = 0; i < columnRow; i++) {
					linesTabsOffset += "\r\n";
				}
				for(int i = 0; i < level; i++) {
					linesTabsOffset += "\t";
				}
				return linesTabsOffset + "->Input";
			}
		}
	}

	[Summary("TextFactory creates a text displayed in the gump according to the specified position and hue)")]
	public class TextFactory {
		[Summary("Creating labels - of columns, of input fields etc")]
		public static Text CreateLabel(string text) {
			return CreateText(Hues.LabelColor, text);
		}

		[Summary("Creating labels - of columns, of input fields etc. Allows alignment specifying")]
		public static Text CreateLabel(string text, DialogAlignment align, DialogAlignment valign) {
			return CreateText(Hues.LabelColor, text, align, valign);
		}

		[Summary("Creating labels - of columns, of input fields etc")]
		public static Text CreateLabel(int xPos, int yPos, string text) {
			return CreateText(xPos, yPos, Hues.LabelColor, text);
		}

		[Summary("Bezny titulek tabulky")]
		public static Text CreateHeadline(string text) {
			return CreateText(Hues.HeadlineColor, text);
		}

		[Summary("Titulek tabulky, ovsem s volbou barvy")]
		public static Text CreateHeadline(string text, Hues color) {
			return CreateText(color, text, DialogAlignment.Align_Left, DialogAlignment.Valign_Top);
		}

		[Summary("Simple factory method allows us to let the dialog to determine the text's position in the column")]
		public static Text CreateText(int hue, string text) {
			return new Text(0, 0, hue, text, DialogAlignment.Align_Left, DialogAlignment.Valign_Top);
		}

		[Summary("Simple factory method allows us to let the dialog to determine the text's position in the column")]
		public static Text CreateText(Hues hue, string text) {
			return new Text(0, 0, (int)hue, text, DialogAlignment.Align_Left, DialogAlignment.Valign_Top);
		}

		[Summary("Simple factory method allows us to let the dialog to determine the text's position in the column")]
		public static Text CreateText(Hues hue, string text, DialogAlignment align, DialogAlignment valign) {
			return new Text(0, 0, (int)hue, text, align, valign);
		}

		[Summary("The simplest factory method just to display the desired text with the basic color")]
		public static Text CreateText(string text) {
			return new Text(0, 0, (int)Hues.WriteColor, text, DialogAlignment.Align_Left, DialogAlignment.Valign_Top);
		}

		[Summary("The simplest factory method lets the dialog to determine the position of the text")]
		public static Text CreateText(string text, DialogAlignment align, DialogAlignment valign) {
			return new Text(0, 0, (int) Hues.WriteColor, text, align, valign);
		}

		[Summary("Basic factory method creates the text field with a given _relative_ position in the column"+
			   " and a specified color (if color is null then default color is used)")]
		public static Text CreateText(int xPos, int yPos, Hues hue, string text) {
			return new Text(xPos, yPos, (int)hue, text, DialogAlignment.Align_Left, DialogAlignment.Valign_Top);
		}

		[Summary("Basic factory method creates the text field form the previously added text (by ID) with a given _relative_ " +
                " position in the column and a specified color (if color is null then default color is used)")]
		public static Text CreateText(int xPos, int yPos, Hues hue, int textId) {
			return new Text(xPos, yPos, (int)hue, textId, DialogAlignment.Align_Left, DialogAlignment.Valign_Top);
		}

		[Summary("Factory method awaiting the text string but no special hue (the default will be used)")]
		public static Text CreateText(int xPos, int yPos, string text) {
			return new Text(xPos, yPos, (int)Hues.WriteColor, text, DialogAlignment.Align_Left, DialogAlignment.Valign_Top);
		}

		[Summary("Factory method awaiting the textId but no special hue (the default will be used)")]
		public static Text CreateText(int xPos, int yPos, int textId) {
			return new Text(xPos, yPos, (int)Hues.WriteColor, textId, DialogAlignment.Align_Left, DialogAlignment.Valign_Top);
		}

		[Summary("Basic fafctory method for building a html gump - we can specify all")]
		public static HTMLText CreateHTML(int xPos, int yPos, int width, int height, string text, bool hasBoundBox, bool scrollBar) {
			return new HTMLText(xPos, yPos, width, height, text, hasBoundBox, scrollBar);
		}

		[Summary("Basic fafctory method for building a html gump - we can specify all but pass the text as ID")]
		public static HTMLText CreateHTML(int xPos, int yPos, int width, int height, int textId, bool hasBoundBox, bool scrollBar) {
			return new HTMLText(xPos, yPos, width, height, textId, hasBoundBox, scrollBar);
		}

		[Summary("Basic fafctory method for building a html gump - on a relative position")]
		public static HTMLText CreateHTML(int width, int height, string text, bool hasBoundBox, bool scrollBar) {
			return new HTMLText(0, 0, width, height, text, hasBoundBox, scrollBar);
		}

		[Summary("Create a html gump which takes the size and position from the parent")]
		public static HTMLText CreateHTML(string text, bool hasBoundBox, bool scrollable) {
			return new HTMLText(0, 0, 0, 0, text, hasBoundBox, scrollable);
		}

		[Summary("Create a html gump which takes the size and position from the parent (using textID)")]
		public static HTMLText CreateHTML(int textId, bool hasBoundBox, bool scrollable) {
			return new HTMLText(0, 0, 0, 0, textId, hasBoundBox, scrollable);
		}

		[Summary("Create a html gump which takes the size and position from the parent "+
                "using default values for boundaries and scrollability (false both)")]
		public static HTMLText CreateHTML(string text) {
			return new HTMLText(0, 0, 0, 0, text, false, false);
		}

		[Summary("Create a html gump which takes the size and position from the parent (using textID) "+
                "using default values for boundaries and scrollability (false both)")]
		public static HTMLText CreateHTML(int textId) {
			return new HTMLText(0, 0, 0, 0, textId, false, false);
		}

		[Summary("The text component class - it handles the text writing to the underlaying gump")]
		public class Text : LeafGUTAComponent {
			[Summary("The text hue in the input field - if not specified, the default will be used.")]
			private int textHue;
			[Summary("We have either ID of the used text, or the text string itself")]
			private int textId;
			private string text;

			[Summary("Text horizontal alignment")]
			private DialogAlignment align = DialogAlignment.Align_Left;
			[Summary("Text vertical alignment")]
			private DialogAlignment valign = DialogAlignment.Valign_Top;

			private Text(int xPos, int yPos, int hue, DialogAlignment align, DialogAlignment valign) {
				this.xPos = xPos;
				this.yPos = yPos;
				this.textHue = hue;
				this.align = align;
				this.valign = valign;
			}

			[Summary("First complete constructor - awaits the text in the string form."+
					"Using color as int and not Hue enumeration. Allows specifying the alignment")]
			public Text(int xPos, int yPos, int hue, string text, DialogAlignment align, DialogAlignment valign) 
					: this(xPos, yPos, hue, align, valign) {
				this.text = text;				
			}

			[Summary("First complete constructor - awaits the text in the textId. Allows specifying the alignment")]
			public Text(int xPos, int yPos, int hue, int textId, DialogAlignment align, DialogAlignment valign) 
					: this(xPos, yPos, hue, align, valign) {
				this.textId = textId;
			}

			[Summary("When added to the column we have to specify the position (count the absolute)")]
			protected override void OnBeforeWrite(GUTAComponent parent) {
				//set the level
				level = parent.Level + 1;
				
				//get the grandparent (GUTATable) (parent is GUTAColumn!)
				GUTATable grandpa = (GUTATable)parent.Parent;
				//set the column row (counted from the relative position and the grandpa's inner-row height)
				columnRow = xPos / grandpa.RowHeight;

				int alignOffset = 0;
				int valignOffset = 0;
				if (text != null) { //we are not using the ID of the text, we can do some alignment computings if necessary
					int parentWidth = parent.Width;
					int textWidth = ImprovedDialog.TextLength(text);
					switch (align) {
						case DialogAlignment.Align_Center:
							alignOffset = parentWidth/2 - textWidth/2; //moves the text to the middle of the column
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

				if(text == null) {
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
				for(int i = 0; i < columnRow; i++) {
					linesTabsOffset += "\r\n";
				}
				for(int i = 0; i < level; i++) {
					linesTabsOffset += "\t";
				}
				return linesTabsOffset + "->Text(" + text + ")";
			}
		}

		[Summary("The HTML text component class - allows making the text scrollable")]
		public class HTMLText : LeafGUTAComponent {
			private bool isScrollable, hasBoundBox;
			[Summary("The text is either specified or passed as a text id")]
			private string text;
			private int textId;

			private HTMLText(int x, int y, int width, int height, bool hasBoundBox, bool isScrollable) {
				this.xPos = x;
				this.yPos = y;
				this.width = width;
				this.height = height;
				this.hasBoundBox = hasBoundBox;
				this.isScrollable = isScrollable;
			}

			[Summary("First complete constructor - awaits the text in the string form")]
			public HTMLText(int x, int y, int width, int height, string text, bool hasBoundBox, bool isScrollable) 
					: this(x, y, width, height, hasBoundBox, isScrollable) {
				this.text = text;
			}

			[Summary("Second complete constructor - awaits the text in the textId")]
			public HTMLText(int x, int y, int width, int height, int textId, bool hasBoundBox, bool isScrollable) 
					: this(x, y, width, height, hasBoundBox, isScrollable) {
				this.textId = textId;
			}

			[Summary("When added to the column we have to specify the position (count the absolute)")]
			protected override void OnBeforeWrite(GUTAComponent parent) {
				//set the level
				level = parent.Level + 1;

				//get the grandparent (GUTATable) (parent is GUTAColumn!)
				GUTATable grandpa = (GUTATable) parent.Parent;
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
				for(int i = 0; i < columnRow; i++) {
					linesTabsOffset += "\r\n";
				}
				for(int i = 0; i < level; i++) {
					linesTabsOffset += "\t";
				}
				return linesTabsOffset + "->HTMLText(" + text + ")";
			}
		}
	}

	[Summary("ImageFactory creates an image field in the dialog containing the specified image")]
	public class ImageFactory {
		[Summary("Factory method for creating a simple image, position is fully taken from the parent")]
		public static Image CreateImage(GumpIDs gumpId) {
			return new Image(0, 0, gumpId);
		}

		[Summary("Factory method for creating a given type of the image. Allows specifying also its X and Y position (relative to the parents)")]
		public static Image CreateImage(int xPos, int yPos, GumpIDs gumpId) {
			return new Image(xPos, yPos, gumpId);
		}

		//[Summary("Factory method for creating a simple tiled image, position and measures are fully taken from the parent")]
		//public static ImageTiled CreateImageTiled(GumpIDs gumpId) {
		//    return new ImageTiled(0, 0, gumpId, 0, 0);
		//}

		//[Summary("Factory method for creating a tiled image with predefined measures, position is taken from the parents")]
		//public static ImageTiled CreateImageTiled(GumpIDs gumpId, int width, int height) {
		//    return new ImageTiled(0, 0, gumpId, width, height);
		//}

		//[Summary("Factory method for creating a tiled image with predefined position, measures is taken from the parents")]
		//public static ImageTiled CreateImageTiled(int xPos, int yPos, GumpIDs gumpId) {
		//    return new ImageTiled(xPos, yPos, gumpId, 0, 0);
		//}

		//[Summary("Factory method for creating a tiled image with everything defined")]
		//public static ImageTiled CreateImageTiled(int xPos, int yPos, GumpIDs gumpId, int width, int height) {
		//    return new ImageTiled(xPos, yPos, gumpId, width, height);
		//}

		public class Image : LeafGUTAComponent {
			protected GumpIDs gumpId;

			[Summary("This constructor is here only for the image's children classes")]
			internal Image() {
			}

			[Summary("Basic constructor awaiting the gump ID and its position")]
			internal Image(int xPos, int yPos, GumpIDs gumpId) {
				this.xPos = xPos;
				this.yPos = yPos;
                this.gumpId = gumpId;
			}

			[Summary("When added to the column we have to specify the position (count the absolute)")]
			protected override void OnBeforeWrite(GUTAComponent parent) {
				//set the level
				level = parent.Level + 1;

				//get the grandparent (GUTATable) (parent is GUTAColumn!)
				GUTATable grandpa = (GUTATable) parent.Parent;
				//set the column row (counted from the relative position and the grandpa's inner-row height)
				columnRow = xPos / grandpa.RowHeight;

				//dont use spaces here or the text is glued to the bottom of the line on the single lined inputs
				xPos += parent.XPos;
				yPos += parent.YPos;
			}

			[Summary("Call the underlaying gump istance's methods")]
			internal override void WriteComponent() {
				gump.AddTilePic(xPos, yPos, (int) gumpId);
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

		//public class ImageTiled : Image {
		//    [Summary("Creates a checkbox with the given format")]
		//    internal ImageTiled(int xPos, int yPos, GumpIDs gumpId, int width, int height) {
		//        this.xPos = xPos;
		//        this.yPos = yPos;
		//        this.gumpId = gumpId;
		//        this.width = width;
		//        this.height = height;
		//    }

		//    [Summary("When added to the column we have to specify the position (count the absolute)")]
		//    protected override void OnBeforeWrite(GUTAComponent parent) {
		//        //set the level
		//        level = parent.Level + 1;
		//        //set the column row (counted from the relative position
		//        columnRow = xPos / ImprovedDialog.D_ROW_HEIGHT;

		//        //dont use spaces here or the text is glued to the bottom of the line on the single lined inputs
		//        xPos += parent.XPos;
		//        yPos += parent.YPos;

		//        //if not specified, take the size from the parent
		//        if (height == 0) {
		//            height = parent.Height;
		//        }
		//        if (width == 0) {
		//            width = parent.Width;
		//        }
		//    }

		//    [Summary("Call the underlaying gump istance's methods")]
		//    internal override void WriteComponent() {
		//        gump.AddGumpPicTiled(xPos, yPos, width, height, (int)gumpId);				
		//    }

		//    public override string ToString() {
		//        string linesTabsOffset = "\r\n";
		//        //add as much rows as is the row which this item lies in
		//        for (int i = 0; i < columnRow; i++) {
		//            linesTabsOffset += "\r\n";
		//        }
		//        for (int i = 0; i < level; i++) {
		//            linesTabsOffset += "\t";
		//        }
		//        return linesTabsOffset + "->ImageTiled(" + gumpId + ")";
		//    }
		//}
	}
}