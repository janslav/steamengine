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
		private Hues textColor;

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
			if(args.Length == 3) {
				textColor = (Hues)args[2]; //barva titulku volitelna
			} else {
				textColor = Hues.HeadlineColor; //normalni nadpisek
			}
			ShowDialog();
		}

		[Remark("Simply display the labeled text.")]
		private void ShowDialog() {
			ImprovedDialog dialogHandler = new ImprovedDialog(this.GumpInstance);
			//create the background GUTAMatrix and set its size an transparency            
			dialogHandler.CreateBackground(400);
			dialogHandler.SetLocation(400, 300);

			//first row - the label of the dialog
			dialogHandler.Add(new GUTATable(1, 0, ButtonFactory.D_BUTTON_WIDTH));
			dialogHandler.LastTable[0,0] = TextFactory.CreateHeadline(label, textColor);
			dialogHandler.LastTable[0,1] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonCross, 0);
			dialogHandler.MakeTableTransparent();

			//at least three rows of a button height (scrollbar has some demands)
			dialogHandler.Add(new GUTATable(3,0));
			dialogHandler.LastTable.RowHeight = ImprovedDialog.D_ROW_HEIGHT;						
			//unbounded, scrollable html text area
			dialogHandler.LastTable[0,0] = TextFactory.CreateHTML(dispText, false, true);
			dialogHandler.MakeTableTransparent();

			dialogHandler.WriteOut();
		}

		public override void OnResponse(GumpInstance gi, GumpResponse gr, object[] args) {
			switch (gr.pressedButton) {
				case 0: //exit
					//look if some dialog is not stored in the dialogs stack and possibly display it
					DialogStacking.ShowPreviousDialog(gi);
					break;
			}
		}

		[SteamFunction]
		[Remark("Zobraz� dialog s voliteln�m labelem a textem v n�m")]
		public static void DisplayText(Thing self, ScriptArgs args) {
			if (args != null && args.Args.Length != 2) {
				self.Dialog(SingletonScript<D_Display_Text>.Instance, args.Args[0], args.Args[1]);
			} else {
				Globals.SrcCharacter.Message("DisplayText mus� b�t vol�na se dv�ma parametry - label + text", (int)Hues.Red);
			}
		}

		[SteamFunction]
		[Remark("Zobraz� dialog s nadpisem CHYBA a textov�m popisem chyby")]
		public static void ShowError(Thing self, ScriptArgs args) {
			if (args != null && args.Args.Length != 1) {
				self.Dialog(SingletonScript<D_Display_Text>.Instance, "CHYBA", args.Args[0]);
			} else {
				Globals.SrcCharacter.Message("ShowError mus� b�t vol�na s parametrem - text chyby", (int)Hues.Red);
			}
		}

		[Remark("Obdoba show erroru pou�itlen� jendodu�e z C# - vraci GumpInstanci (napriklad pro stacknuti)")]
		public static GumpInstance ShowError(string text) {
			return Globals.SrcCharacter.Dialog(SingletonScript<D_Display_Text>.Instance, "CHYBA", text, Hues.Red);
		}

		[Remark("Zobrazen� infa pou�itlen� jendodu�e z C# - vraci GumpInstanci (napriklad pro stacknuti)")]
		public static GumpInstance ShowInfo(string text) {
			return Globals.SrcCharacter.Dialog(SingletonScript<D_Display_Text>.Instance, "INFO", text);
		}
	}
}