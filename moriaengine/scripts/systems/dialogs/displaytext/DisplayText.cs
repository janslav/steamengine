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
using SteamEngine.Common;
using SteamEngine.LScript;
using SteamEngine.CompiledScripts;

namespace SteamEngine.CompiledScripts.Dialogs {

	[Remark("Dialog that will display a desired text with a desired label (e.g. displaying larger texts "+
            "in pages dialog etc.")]
	public class D_Display_Text : CompiledGump {
		private string label;
		private string dispText;

		[Remark("Instance of the D_Display_Text, for possible access from other dialogs, buttons etc.")]
		private static D_Display_Text instance;
		public static D_Display_Text Instance {
			get {
				return instance;
			}
		}
		[Remark("Set the static reference to the instance of this dialog")]
		public D_Display_Text() {
			instance = this;
		}

		public override void Construct(Thing focus, AbstractCharacter sendTo, object[] args) {
			label = string.Concat(args[0]); //the gump's label
			dispText = string.Concat(args[1]); //the text to be displayed
			ShowDialog();
		}

		[Remark("Simply display the labeled text.")]
		private void ShowDialog() {
			ImprovedDialog dialogHandler = new ImprovedDialog(this.GumpInstance);
			//create the background GumpMatrix and set its size an transparency            
			dialogHandler.CreateBackground(400);
			dialogHandler.SetLocation(400, 300);

			//first row - the label of the dialog
			dialogHandler.Add(new GumpTable(1, ButtonFactory.D_BUTTON_HEIGHT));
			dialogHandler.Add(new GumpColumn());
			dialogHandler.Add(TextFactory.CreateText(label));
			dialogHandler.AddLast(new GumpColumn(ButtonFactory.D_BUTTON_WIDTH));
			dialogHandler.Add(ButtonFactory.CreateButton(LeafComponentTypes.ButtonCross, 0));
			dialogHandler.MakeTableTransparent();

			//count how many rows we may need
			int rows = (dispText.Length * ImprovedDialog.D_CHARACTER_WIDTH) / dialogHandler.LastTable.Width;

			//at least three rows of a button height (scrollbar has some demands)
			dialogHandler.Add(new GumpTable((rows < 3 ? 3 : rows), ButtonFactory.D_BUTTON_HEIGHT));
			dialogHandler.Add(new GumpColumn());
			dialogHandler.MakeTableTransparent();
			//unbounded, scrollable
			dialogHandler.Add(TextFactory.CreateHTML(dispText, false, true));

			dialogHandler.WriteOut();
		}

		public override void OnResponse(GumpInstance gi, GumpResponse gr) {
			switch (gr.pressedButton) {
				case 0: //exit
					//look if some dialog is not stored in the dialogs stack and possibly display it
					DialogStackItem.ShowPreviousDialog(gi.Cont.Conn);
					break;
			}
		}
	}
}