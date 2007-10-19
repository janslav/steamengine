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

namespace SteamEngine.CompiledScripts.Dialogs {

	[Remark("An account creating dialog")]
	public class D_NewAccount : CompiledGump {
		[Remark("Instance of the D_NewAccount, for possible access from other dialogs etc.")]
        private static D_NewAccount instance;
		public static D_NewAccount Instance {
			get {
				return instance;
			}
		}
        [Remark("Set the static reference to the instance of this dialog")]
		public D_NewAccount() {
			instance = this;
		}

		public override void Construct(Thing focus, AbstractCharacter sendTo, object[] sa) {
			
			ImprovedDialog dlg = new ImprovedDialog(this.GumpInstance);
			//pozadi    
			dlg.CreateBackground(500);
			dlg.SetLocation(50, 50);

			//nadpis
			dlg.Add(new GUTATable(1,0,ButtonFactory.D_BUTTON_WIDTH));
			dlg.LastTable[0,0] = TextFactory.CreateHeadline("Vytvoøení nového hráèského úètu");
			//cudlik na zavreni dialogu
			dlg.LastTable[0,1] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonCross, 0);
			dlg.MakeTableTransparent();

			//dvousloupeckovy dialozek
			dlg.Add(new GUTATable(3,0,300)); //1.sl - napisy, 2.sl - editacni pole
			//napred napisy 
			dlg.LastTable[0, 0] = TextFactory.CreateLabel("Jméno úètu");
			dlg.LastTable[1, 0] = TextFactory.CreateLabel("Heslo");
			dlg.LastTable[2, 0] = TextFactory.CreateLabel("Registraèní e-mail");

			//ted editacni pole
			dlg.LastTable[0, 1] = InputFactory.CreateInput(LeafComponentTypes.InputText, 10);
			dlg.LastTable[1, 1] = InputFactory.CreateInput(LeafComponentTypes.InputText, 11);
			dlg.LastTable[2, 1] = InputFactory.CreateInput(LeafComponentTypes.InputText, 12);
			dlg.MakeTableTransparent(); //zpruhledni zbytek dialogu

			//a posledni radek s tlacitkem
			dlg.Add(new GUTATable(1,ButtonFactory.D_BUTTON_WIDTH,0));
			dlg.LastTable[0, 0] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonTick, 1);
			dlg.LastTable[0, 1] = TextFactory.CreateLabel("Potvrdit");
			dlg.MakeTableTransparent(); //zpruhledni posledni radek

			dlg.WriteOut();
		}

		public override void OnResponse(GumpInstance gi, GumpResponse gr, object[] args) {
			if(gr.pressedButton == 0) {
				DialogStacking.ShowPreviousDialog(gi); //zobrazit pripadny predchozi dialog				
			} else if(gr.pressedButton == 1) {
				//nacteme obsah input fieldu
				string accName = gr.GetTextResponse(10);
				string pass = gr.GetTextResponse(11);
				string email = gr.GetTextResponse(12);
				//zavolat metodu, ktera oznami uspech ci neuspech pri vytvoreni
				ScriptedAccount.CreateGameAccount(accName, pass, email);
				DialogStacking.ShowPreviousDialog(gi); //zobrazit pripadny predchozi dialog			
			}
		}

		[Remark("Create a new gm account using the dialog. Function accessible from the game")]
		[SteamFunction]
		public static void NewAcc(AbstractCharacter sender, ScriptArgs text) {
			sender.Dialog(D_NewAccount.Instance);
		}
	}
}