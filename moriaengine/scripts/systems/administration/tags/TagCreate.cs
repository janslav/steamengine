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

	[Remark("A new tag creating dialog")]
	public class D_NewTag : CompiledGump {
		private static int width = 400;
		private static int innerWidth = width - 2 * ImprovedDialog.D_BORDER - 2 * ImprovedDialog.D_SPACE;
		
		public override void Construct(Thing focus, AbstractCharacter sendTo, object[] sa) {
			TagHolder th = (TagHolder)sa[0]; //na koho budeme tag ukladat?

			ImprovedDialog dlg = new ImprovedDialog(this.GumpInstance);
			//pozadi    
			dlg.CreateBackground(width);
			dlg.SetLocation(50, 50);

			//nadpis
			dlg.Add(new GUTATable(1, innerWidth - 2 * ButtonFactory.D_BUTTON_WIDTH - ImprovedDialog.D_COL_SPACE, 0, ButtonFactory.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = TextFactory.CreateHeadline("Vložení nového tagu na "+th.ToString());
			//cudlik na zavreni dialogu
			dlg.LastTable[0, 1] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonPaper, 2);//cudlik na info o hodnotach			
			dlg.LastTable[0, 2] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonCross, 0);
			dlg.MakeTableTransparent();

			//dialozek s inputama
			dlg.Add(new GUTATable(2, 0, 275)); //1.sl - edit nazev, 2.sl - edit hodnota
			//napred napisy 
			dlg.LastTable[0, 0] = TextFactory.CreateLabel("Název tagu");
			dlg.LastTable[1, 0] = TextFactory.CreateLabel("Hodnota");
			dlg.LastTable[0, 1] = InputFactory.CreateInput(LeafComponentTypes.InputText, 10);
			dlg.LastTable[1, 1] = InputFactory.CreateInput(LeafComponentTypes.InputText, 11);
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
				string tagName = gr.GetTextResponse(10);
				string tagValue = gr.GetTextResponse(11);
				//ziskame objektovou reprezentaci vlozene hodnoty. ocekava samozrejme prefixy pokud je potreba!
				object objectifiedValue = SettingsUtilities.ReadValue(tagValue, null);
				((TagHolder)args[0]).SetTag(TagKey.Get(tagName), objectifiedValue);
				//vzit jeste predchozi dialog, musime smazat taglist aby se pregeneroval
				//a obsahoval ten novy tag
				GumpInstance prevStacked = DialogStacking.PopStackedDialog(gi);
				if(prevStacked.def.GetType().IsAssignableFrom(typeof(D_TagList))) {
					//prisli jsme z taglistu - mame zde seznam a muzeme ho smazat
					prevStacked.InputParams[3] = null;
				}
				DialogStacking.ResendAndRestackDialog(prevStacked);
			} else if(gr.pressedButton == 2) {
				GumpInstance newGi = gi.Cont.Dialog(SingletonScript<D_Settings_Help>.Instance);
				DialogStacking.EnstackDialog(gi, newGi); //ulozime dialog do stacku
			}
		}		
	}
}