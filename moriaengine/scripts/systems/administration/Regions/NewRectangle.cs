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
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts.Dialogs {
	[Remark("Dialog for creating a new region rectangle")]
	public class D_New_Rectangle : CompiledGump {
		private static int width = 600;

		[Remark("Seznam parametru: 0 - list s rectanglama kam to pak pridame "+
				"1-4 - pripadne predvyplnene souradnice (zobrazuji se jen pri chybach")]
		public override void Construct(Thing focus, AbstractCharacter sendTo, object[] args) {
			string minX, minY, maxX, maxY; //predzadane hodnoty (if any)
			minX = (args[1] != null ? args[1].ToString() : "");
			minY = (args[1] != null ? args[2].ToString() : "");
			maxX = (args[1] != null ? args[3].ToString() : "");
			maxY = (args[1] != null ? args[4].ToString() : "");
			
			ImprovedDialog dlg = new ImprovedDialog(this.GumpInstance);
			//pozadi    
			dlg.CreateBackground(width);
			dlg.SetLocation(100, 100);

			//nadpis
			dlg.Add(new GUTATable(1, 0, ButtonFactory.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = TextFactory.CreateHeadline("Vložení nového rectanglu");
			dlg.LastTable[0, 1] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonCross, 0);//cudlik na zavreni dialogu
			dlg.MakeTableTransparent();

			//textiky a editfieldy na zadani souradnic
			dlg.Add(new GUTATable(2, 150, 150, 0, 150));
			dlg.LastTable[0, 0] = TextFactory.CreateLabel("MinX");
			dlg.LastTable[0, 1] = InputFactory.CreateInput(LeafComponentTypes.InputText, 31, minX);
			dlg.LastTable[0, 2] = TextFactory.CreateLabel("MinY");
			dlg.LastTable[0, 3] = InputFactory.CreateInput(LeafComponentTypes.InputText, 32, minY);
			dlg.LastTable[1, 0] = TextFactory.CreateLabel("MaxX");
			dlg.LastTable[1, 1] = InputFactory.CreateInput(LeafComponentTypes.InputText, 33, maxX);
			dlg.LastTable[1, 2] = TextFactory.CreateLabel("MaxY");
			dlg.LastTable[1, 3] = InputFactory.CreateInput(LeafComponentTypes.InputText, 34, maxY);
			dlg.MakeTableTransparent();
	
			//send button
			dlg.Add(new GUTATable(1, ButtonFactory.D_BUTTON_WIDTH, 0));
			dlg.LastTable[0, 0] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonOK, 1);
			dlg.LastTable[0, 1] = TextFactory.CreateText("Uložit");
			dlg.MakeTableTransparent();

			dlg.WriteOut();
		}

		public override void OnResponse(GumpInstance gi, GumpResponse gr, object[] args) {
			//seznam rectanglu bereme z parametru (jsou to ty mutable)
			List<MutableRectangle> rectsList = (List<MutableRectangle>)args[0];
			int firstOnPage = Convert.ToInt32(args[1]);
			if(gr.pressedButton == 0) { //exit
				DialogStacking.ShowPreviousDialog(gi); //zobrazit pripadny predchozi dialog
			} else if(gr.pressedButton == 1) { //ulozit
				//precteme parametry a zkusime vytvorit rectangle
				string sMinX = "", sMinY = "", sMaxX = "", sMaxY = "";
				int minX, minY, maxX, maxY;
				try {
					sMinX = gr.GetTextResponse(31);
					sMinY = gr.GetTextResponse(32);
					sMaxX = gr.GetTextResponse(33);
					sMaxY = gr.GetTextResponse(34);
					minX = Convert.ToInt32(gr.GetTextResponse(31));
					minY = Convert.ToInt32(gr.GetTextResponse(32));
					maxX = Convert.ToInt32(gr.GetTextResponse(33));
					maxY = Convert.ToInt32(gr.GetTextResponse(34));
				} catch {
					//pripravime zadane hodnoty k poslani
					args[1] = sMinX;
					args[2] = sMinY;
					args[3] = sMaxX;
					args[4] = sMaxY;
					//stackneme a zobrazime chybu
					GumpInstance newGi = D_Display_Text.ShowError("Nìjaké èíslo jsi buï nezadal nebo zadal špatnì");
					DialogStacking.EnstackDialog(gi, newGi);
					return;
				}
				MutableRectangle newRect = null;
				try {
					newRect = new MutableRectangle((ushort)minX, (ushort)minY, (ushort)maxX, (ushort)maxY);
				} catch {
					//tady se octneme pokud zadal blbe ty souradnice (napred pravy horni, pak levy dolni roh!)
					//pripravime zadane hodnoty k poslani
					args[1] = sMinX;
					args[2] = sMinY;
					args[3] = sMaxX;
					args[4] = sMaxY;
					//stackneme a zobrazime chybu
					GumpInstance newGi = D_Display_Text.ShowError("MinX/Y ma byt levy horni roh, MaxX/Y ma byt pravy dolni");
					DialogStacking.EnstackDialog(gi, newGi);
					return;
				}
				//vsef poradku tak hura zpatky (nezapomenout pridat do puvodniho seznamu!)
				rectsList.Add(newRect);
				GumpInstance previousGi = DialogStacking.PopStackedDialog(gi);
				previousGi.InputParams[2] = rectsList;
				DialogStacking.ResendAndRestackDialog(previousGi);
			}
		}
	}
}