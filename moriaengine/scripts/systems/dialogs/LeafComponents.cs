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
	[Remark("Leaf GUTA components cannot have any children, these are e.g buttons, inputs, texts etc.")]
	public abstract class LeafGUTAComponent : GUTAComponent {
		[Remark("This is the ID many gump items have - buttons number, input entries number...")]
		protected int id;

		[Remark("Adding any children to the leaf is prohibited...")]
		public override sealed void AddComponent(GUTAComponent child) {
			throw new GUTAComponentCannotBeExtendedException("GUTAcomponent " + this.GetType() + " cannot have any children");
		}
	}

	[Remark("Factory class for creating buttons according to the given button type. Including special buttons"+
            " such as Checkbox or Radio Button")]
	public class ButtonFactory {
        internal static Hashtable buttonGumps = new Hashtable();

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
            
            //0d2, 0d3 Checkbox (unchecked, checked)
            buttonGumps.Add(LeafComponentTypes.CheckBox, new ButtonGump(210, 211));
            //0d0, 0d1 Radiobutton (unselected, selected)
            buttonGumps.Add(LeafComponentTypes.RadioButton, new ButtonGump(208, 209));
        }       

		public const int D_BUTTON_WIDTH = 31;
		public const int D_BUTTON_HEIGHT = 22;
		public const int D_BUTTON_PREVNEXT_WIDTH = 16;
		public const int D_BUTTON_PREVNEXT_HEIGHT = 21;

		[Remark("Number of pixels to move the button in the line so it is in the middle")]
		public const int D_SORTBUTTON_LINE_OFFSET =	9;
		[Remark("Number of pixels to move the text to the right so it is readable next to the sort buttons")]
		public const int D_SORTBUTTON_COL_OFFSET = 11;

        [Remark("Flyweight class carrying info about the two button gumps (pressed and released)"+
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

		[Remark("Factory method for creating a given type of the button. We have to provide the _relative_"+
                "x and y position (relative to the parent column) and the button ID, we have to also specify the page "+
                "opened by this button and whether the button is active or not")]
        public static Button CreateButton(LeafComponentTypes type, int xPos, int yPos, bool active, int page, int id) {
			return new Button(id, xPos, yPos, (ButtonGump)buttonGumps[type], active, page);
		}

		[Remark("Basic method - it adds the button automatically to the beginning of the column")]
		public static Button CreateButton(LeafComponentTypes type, int id) {
			return CreateButton(type, 0, 0, true, 0, id);
		}

		[Remark("Basic method - allows to specify the button position")]
        public static Button CreateButton(LeafComponentTypes type, int xPos, int yPos, int id) {
			return CreateButton(type, xPos, yPos, true, 0, id);
		}

		[Remark("Basic method - allows to specify the button position and activity")]
        public static Button CreateButton(LeafComponentTypes type, int xPos, int yPos, bool active, int id) {
			return CreateButton(type, xPos, yPos, active, 0, id);
		}

		[Remark("Basic method - it adds the button automatically to the beginning of the column"+
                "allows us to specify if the button is active or not")]
        public static Button CreateButton(LeafComponentTypes type, bool active, int id) {
			return CreateButton(type, 0, 0, active, 0, id);
		}

		[Remark("Basic method - it adds the button automatically to the beginning of the column" +
                "allows us to specify if the button is active or not and the page opened")]
        public static Button CreateButton(LeafComponentTypes type, bool active, int page, int id) {
			return CreateButton(type, 0, 0, active, page, id);
		}

		[Remark("Create a checkbox using the _relative_ position in the column, the check/unchecked flag"+
                "and the id")]
		public static CheckBox CreateCheckbox(int xPos, int yPos, bool isChecked, int id) {
			return new CheckBox(xPos, yPos, (ButtonGump)buttonGumps[LeafComponentTypes.CheckBox], isChecked, id);
		}

		[Remark("Create checkbox using the 1 or 0 as a marker if the checkbox is checked or not")]
		public static CheckBox CreateCheckbox(int xPos, int yPos, int isChecked, int id) {
			return CreateCheckbox(xPos, yPos, isChecked == 1 ? true : false, id);
		}

		[Remark("Create a radio button using the _relative_ position in the column, the check/unchecked flag" +
                "and the id")]
		public static RadioButton CreateRadio(int xPos, int yPos, bool isChecked, int id) {
			return new RadioButton(xPos, yPos, (ButtonGump)buttonGumps[LeafComponentTypes.RadioButton], isChecked, id);
		}

		[Remark("Create radio button using the 1 or 0 as a marker if the checkbox is checked or not")]
		public static RadioButton CreateRadio(int xPos, int yPos, int isChecked, int id) {
			return CreateRadio(xPos, yPos, isChecked == 1 ? true : false, id);
		}

		[Remark("The Button component class - it handles the button writing to the client")]
		public class Button : LeafGUTAComponent {
			private ButtonGump gumps;
			private int page = 0;
			private bool active = true;

			[Remark("This constructor is here only for the buttons children classes")]
			internal Button() {
			}

			[Remark("Basic constructor awaiting the buttons ID and string value of UP and DOWN gump graphic ids")]
			internal Button(int id, int xPos, int yPos, ButtonGump gumps, bool active, int page) {
				this.id = id;
				this.xPos = xPos;
				this.yPos = yPos;
                this.gumps = gumps;
                this.active = active;
				this.page = page;
			}

			[Remark("When added, we must recompute the Buttons absolute position in the dialog (we "+
                    " were provided only relative positions")]
			public override void OnBeforeWrite(GUTAComponent parent) {
				//no space here, the used button gumps have themselves some space...
				xPos += parent.XPos;
				yPos += parent.YPos;
			}

			[Remark("Simply write the button (send the method request to the underlaying gump)")]
			public override void WriteComponent() {
				gump.AddButton(xPos, yPos, gumps.GumpDown, gumps.GumpUp, active, 0, id);
			}
		}

		[Remark("Create a nice small checkbox")]
		public class CheckBox : Button {
			private ButtonGump gumps;
			private bool isChecked;

			[Remark("Creates a checkbox with the given format")]
			internal CheckBox(int xPos, int yPos, ButtonGump gumps, bool isChecked, int id) {
				this.xPos = xPos;
				this.yPos = yPos;
                this.gumps = gumps;
				this.isChecked = isChecked;
				this.id = id;
			}

			[Remark("Set the position which was specified relatively")]
			public override void OnBeforeWrite(GUTAComponent parent) {
				xPos = xPos + parent.XPos;
				yPos = yPos + parent.YPos;
			}

			[Remark("Simply call the gumps method for writing the checkbox")]
			public override void WriteComponent() {
                                             //unchecked!!!,    checked !!!
				gump.AddCheckBox(xPos, yPos, gumps.GumpUp, gumps.GumpDown, isChecked, id);
			}
		}

		[Remark("A class representing the radio button")]
		public class RadioButton : Button {
            private ButtonGump gumps;
			private bool isChecked;

			[Remark("Creates a radio button")]
			internal RadioButton(int xPos, int yPos, ButtonGump gumps, bool isChecked, int id) {
				this.xPos = xPos;
				this.yPos = yPos;
                this.gumps = gumps;
				this.isChecked = isChecked;
				this.id = id;
			}

			[Remark("Set the position which was specified relatively")]
			public override void OnBeforeWrite(GUTAComponent parent) {
				xPos = xPos + parent.XPos;
				yPos = yPos + parent.YPos;
			}

			[Remark("Simply call the gumps method for writing the radiobutton")]
			public override void WriteComponent() {
                                         //unselected!!!, selected!!!
				gump.AddRadio(xPos, yPos, gumps.GumpUp,gumps.GumpDown, isChecked, id);
			}
		}
	}

	[Remark("InputFactory creates different type of input fields (basicly TEXT or NUMBER)")]
	public class InputFactory {
		[Remark("Position from the parent, no pre-text specified, default color")]
        public static Input CreateInput(LeafComponentTypes type, int id, int pixelWidth, int height) {
            return new Input(type, 0, 0, id, pixelWidth, height, Hues.WriteColor, "");
		}

		[Remark("Position and width from the parent, no pre-text specified, default color ")]
        public static Input CreateInput(LeafComponentTypes type, int id) {
            return new Input(type, 0, 0, id, 0, 0, Hues.WriteColor, "");
		}

		[Remark("Position and width from the parent, default color but the pre-text is specified here")]
        public static Input CreateInput(LeafComponentTypes type, int id, string text) {
            return new Input(type, 0, 0, id, 0, 0, Hues.WriteColor, text);
		}

		[Remark("Factory method for creating a given type fo the button. We have to specify the button type, the entry ID, pixel and "+
                "character width and optionally the text hue and the text itself "+
                "the position will be determined from the parent")]
        public static Input CreateInput(LeafComponentTypes type, int id, int pixelWidth, int height, Hues textHue, string text) {
			return new Input(type, 0, 0, id, pixelWidth, height, textHue, text);
		}

		[Remark("Position will be determined from the parent, the color will be default")]
        public static Input CreateInput(LeafComponentTypes type, int id, int pixelWidth, int height, string text) {
            return new Input(type, 0, 0, id, pixelWidth, height, Hues.WriteColor, text);
		}

		[Remark("Position will be determined from the parent, the color will be default, the text may be only the specified Id")]
        public static Input CreateInput(LeafComponentTypes type, int id, int pixelWidth, int height, int textId) {
            return new Input(type, 0, 0, id, pixelWidth, height, Hues.WriteColor, textId);
		}

		[Remark("Factory method for creating a given type fo the button. We have to specify the button type, provide "+
                " the relative positions, the entry ID, pixel and chracter width and optionally the text hue and the text itself")]
        public static Input CreateInput(LeafComponentTypes type, int xPos, int yPos, int id, int pixelWidth, int height, Hues textHue, string text) {
			return new Input(type, xPos, yPos, id, pixelWidth, height, textHue, text);
		}

		[Remark("Factory method for creating a given type fo the button. We have to specify the button type, provide "+
                " the relative positions, the entry ID, pixel and chracter width and optionally the text hue and the textId")]
        public static Input CreateInput(LeafComponentTypes type, int xPos, int yPos, int id, int pixelWidth, int height, Hues textHue, int textId) {
			return new Input(type, xPos, yPos, id, pixelWidth, height, textHue, textId);
		}

		[Remark("Factory method for creating a given type fo the button. We have to specify the button type, provide "+
                " the relative positions, the entry ID, pixel and chracter width.")]
        public static Input CreateInput(LeafComponentTypes type, int xPos, int yPos, int id, int pixelWidth, int height) {
            return InputFactory.CreateInput(type, xPos, yPos, id, pixelWidth, height, Hues.WriteColor, "");
		}

		[Remark("Factory method for creating a given type fo the button. We have to specify the button type, provide "+
                " the relative positions, the entry ID, pixel and chracter width, the pre-text.")]
        public static Input CreateInput(LeafComponentTypes type, int xPos, int yPos, int id, int pixelWidth, int height, string text) {
			return InputFactory.CreateInput(type, xPos, yPos, id, pixelWidth, height, Hues.WriteColor, text);
		}

		[Remark("Factory method for creating a given type fo the button. We have to specify the button type, provide "+
                " the relative positions, the entry ID, pixel and chracter width, the pre-text id.")]
        public static Input CreateInput(LeafComponentTypes type, int xPos, int yPos, int id, int pixelWidth, int height, int textId) {
            return InputFactory.CreateInput(type, xPos, yPos, id, pixelWidth, height, Hues.WriteColor, textId);
		}

		[Remark("The Button component class - it handles the button writing to the client")]
		public class Input : LeafGUTAComponent {
            private LeafComponentTypes type;
			[Remark("We have either ID of the used (pre-)text, or the text string itself")]
			private int textId;
			private string text;
			[Remark("The text hue in the input field - if not specified, the default will be used.")]
			private Hues textHue;

			[Remark("First complete constructor - awaits the necessary things and the pre-text in the string form")]
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

			[Remark("Second complete constructor - awaits the necessary things and the pre-text as the string id")]
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

			[Remark("When added, we must recompute the Buttons absolute position in the dialog (we " +
                    " were provided only relative positions")]
			public override void OnBeforeWrite(GUTAComponent parent) {
				xPos += parent.XPos;
				yPos += parent.YPos;
				if (width == 0) {
					//no width - get it from the parent
					width = parent.Width;
					width -= ImprovedDialog.D_COL_SPACE; //put it between the borders fo the column with a little spaces
				}
				if (height == 0) {
					//no height specified, give it the default one row height
					height = ImprovedDialog.D_ROW_HEIGHT;
				}
			}

			[Remark("Simply write the input (send the method request to the underlaying gump)"+
                    " it will determine also what parameters to send")]
			public override void WriteComponent() {
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
                                gump.AddNumberEntry(xPos, yPos, width, height, (int)textHue, id, double.Parse(text));
                            } else {
                                gump.AddNumberEntry(xPos, yPos, width, height, (int)textHue, id, textId);
                            }
                            break;
                        }
                }
			}
		}
	}

	[Remark("TextFactory creates a text displayed in the gump according to the specified position and hue)")]
	public class TextFactory {
		[Remark("Simple fatctory method allows us to let the dialog to determine the text's position in the column")]
		public static Text CreateText(Hues hue, string text) {
			return new Text(0, 0, hue, text);
		}

		[Remark("The simplest factory method lets the dialog to determine the position and the color of the text")]
		public static Text CreateText(string text) {
			return new Text(0, 0, Hues.WriteColor, text);
		}

		[Remark("Basic factory method creates the text field with a given _relative_ position in the column"+
                " and a specified color")]
		public static Text CreateText(int xPos, int yPos, Hues hue, string text) {
			return new Text(xPos, yPos, hue, text);
		}

		[Remark("Basic factory method creates the text field form the previously added text (by ID) with a given _relative_ " +
                " position in the column and a specified color")]
		public static Text CreateText(int xPos, int yPos, Hues hue, int textId) {
			return new Text(xPos, yPos, hue, textId);
		}

		[Remark("Factory method awaiting the text string but no special hue (the default will be used)")]
		public static Text CreateText(int xPos, int yPos, string text) {
			return new Text(xPos, yPos, Hues.WriteColor, text);
		}

		[Remark("Factory method awaiting the textId but no special hue (the default will be used)")]
		public static Text CreateText(int xPos, int yPos, int textId) {
			return new Text(xPos, yPos, Hues.WriteColor, textId);
		}

		[Remark("Basic fafctory method for building a html gump - we can specify all")]
		public static HTMLText CreateHTML(int xPos, int yPos, int width, int height, string text, bool hasBoundBox, bool scrollBar) {
			return new HTMLText(xPos, yPos, width, height, text, hasBoundBox, scrollBar);
		}

		[Remark("Basic fafctory method for building a html gump - we can specify all but pass the text as ID")]
		public static HTMLText CreateHTML(int xPos, int yPos, int width, int height, int textId, bool hasBoundBox, bool scrollBar) {
			return new HTMLText(xPos, yPos, width, height, textId, hasBoundBox, scrollBar);
		}

		[Remark("Basic fafctory method for building a html gump - on a relative position")]
		public static HTMLText CreateHTML(int width, int height, string text, bool hasBoundBox, bool scrollBar) {
			return new HTMLText(0, 0, width, height, text, hasBoundBox, scrollBar);
		}

		[Remark("Create a html gump which takes the size and position from the parent")]
		public static HTMLText CreateHTML(string text, bool hasBoundBox, bool scrollable) {
			return new HTMLText(0, 0, 0, 0, text, hasBoundBox, scrollable);
		}

		[Remark("Create a html gump which takes the size and position from the parent (using textID)")]
		public static HTMLText CreateHTML(int textId, bool hasBoundBox, bool scrollable) {
			return new HTMLText(0, 0, 0, 0, textId, hasBoundBox, scrollable);
		}

		[Remark("Create a html gump which takes the size and position from the parent "+
                "using default values for boundaries and scrollability (false both)")]
		public static HTMLText CreateHTML(string text) {
			return new HTMLText(0, 0, 0, 0, text, false, false);
		}

		[Remark("Create a html gump which takes the size and position from the parent (using textID) "+
                "using default values for boundaries and scrollability (false both)")]
		public static HTMLText CreateHTML(int textId) {
			return new HTMLText(0, 0, 0, 0, textId, false, false);
		}

		[Remark("The text component class - it handles the text writing to the underlaying gump")]
		public class Text : LeafGUTAComponent {
			[Remark("The text hue in the input field - if not specified, the default will be used.")]
			private Hues textHue;
			[Remark("We have either ID of the used text, or the text string itself")]
			private int textId;
			private string text;

			[Remark("First complete constructor - awaits the text in the string form")]
			public Text(int xPos, int yPos, Hues hue, string text) {
				this.xPos = xPos;
				this.yPos = yPos;
				this.textHue = hue;
				this.text = text;
			}

			[Remark("First complete constructor - awaits the text in the textId")]
			public Text(int xPos, int yPos, Hues hue, int textId) {
				this.xPos = xPos;
				this.yPos = yPos;
				this.textHue = hue;
				this.textId = textId;
			}

			[Remark("When added to the column we have to specify the position (count the absolute)")]
			public override void OnBeforeWrite(GUTAComponent parent) {
				//dont use spaces here or the text is glued to the bottom of the line on the single lined inputs
				xPos += parent.XPos;
				yPos += parent.YPos;
			}

			[Remark("Call the underlaying gump istance's methods")]
			public override void WriteComponent() {
				if (textId == 0) { //no text ID was specified, use the text version
					gump.AddText(xPos, yPos, (int)textHue, text);
				} else {
					gump.AddText(xPos, yPos, (int)textHue, textId);
				}
			}
		}

		[Remark("The HTML text component class - allows making the text scrollable")]
		public class HTMLText : LeafGUTAComponent {
			private bool isScrollable, hasBoundBox;
			[Remark("The text is either specified or passed as a text id")]
			private string text;
			private int textId;

			[Remark("First complete constructor - awaits the text in the string form")]
			public HTMLText(int x, int y, int width, int height, string text, bool hasBoundBox, bool isScrollable) {
				this.xPos = x;
				this.yPos = y;
				this.width = width;
				this.height = height;
				this.text = text;
				this.hasBoundBox = hasBoundBox;
				this.isScrollable = isScrollable;
			}

			[Remark("Second complete constructor - awaits the text in the textId")]
			public HTMLText(int x, int y, int width, int height, int textId, bool hasBoundBox, bool isScrollable) {
				this.xPos = x;
				this.yPos = y;
				this.width = width;
				this.height = height;
				this.textId = textId;
				this.hasBoundBox = hasBoundBox;
				this.isScrollable = isScrollable;
			}

			[Remark("When added to the column we have to specify the position (count the absolute)")]
			public override void OnBeforeWrite(GUTAComponent parent) {
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

			[Remark("Call the underlaying gump istance's methods")]
			public override void WriteComponent() {
				if (textId == 0) { //no text ID was specified, use the text version
					gump.AddHTMLGump(xPos, yPos, width, height, text, hasBoundBox, isScrollable);
				} else {
					gump.AddHTMLGump(xPos, yPos, width, height, textId, hasBoundBox, isScrollable);
				}
			}
		}
	}
}