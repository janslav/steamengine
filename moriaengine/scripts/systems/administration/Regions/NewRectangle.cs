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
		private static int width = 450;

		[Remark("Seznam parametru: 0 - list s rectanglama kam to pak pridame "+
				"1-4 - pripadne predvyplnene souradnice (zobrazuji se jen pri chybach)")]
		public override void Construct(Thing focus, AbstractCharacter sendTo, object[] args) {
			string minX, minY, maxX, maxY; //predzadane hodnoty (if any)
			minX = (args[1] != null ? args[1].ToString() : "");
			minY = (args[2] != null ? args[2].ToString() : "");
			maxX = (args[3] != null ? args[3].ToString() : "");
			maxY = (args[4] != null ? args[4].ToString() : "");
			
			ImprovedDialog dlg = new ImprovedDialog(this.GumpInstance);
			//pozadi    
			dlg.CreateBackground(width);
			dlg.SetLocation(100, 100);

			//nadpis
			dlg.Add(new GUTATable(1, 0, ButtonFactory.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = TextFactory.CreateHeadline("Vložení nového rectanglu");
			dlg.LastTable[0, 1] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonCross, 0);//cudlik na zavreni dialogu
			dlg.MakeTableTransparent();

			//navod
			dlg.Add(new GUTATable(2, 0));
			dlg.LastTable[0, 0] = TextFactory.CreateHeadline("(MinX,MinY) - levy horni roh, (MaxX,MaxY) - pravy dolni roh");
			dlg.LastTable[1, 0] = TextFactory.CreateHeadline("Mapa v UO zacina vlevo nahore bodem (0,0) a konci vpravo dole!");
			dlg.MakeTableTransparent();

			//textiky a editfieldy na zadani souradnic
			dlg.Add(new GUTATable(2, 100, 100, 0, 100));
			dlg.LastTable[0, 0] = TextFactory.CreateLabel("MinX");
			dlg.LastTable[0, 1] = InputFactory.CreateInput(LeafComponentTypes.InputNumber, 31, minX);
			dlg.LastTable[0, 2] = TextFactory.CreateLabel("MinY");
			dlg.LastTable[0, 3] = InputFactory.CreateInput(LeafComponentTypes.InputNumber, 32, minY);
			dlg.LastTable[1, 0] = TextFactory.CreateLabel("MaxX");
			dlg.LastTable[1, 1] = InputFactory.CreateInput(LeafComponentTypes.InputNumber, 33, maxX);
			dlg.LastTable[1, 2] = TextFactory.CreateLabel("MaxY");
			dlg.LastTable[1, 3] = InputFactory.CreateInput(LeafComponentTypes.InputNumber, 34, maxY);
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
				MutableRectangle newRect = null;
				try {
					args[1] = Convert.ToUInt16(gr.GetNumberResponse(31));
					args[2] = Convert.ToUInt16(gr.GetNumberResponse(32));
					args[3] = Convert.ToUInt16(gr.GetNumberResponse(33));
					args[4] = Convert.ToUInt16(gr.GetNumberResponse(34));
							
					newRect = new MutableRectangle((ushort)args[1], (ushort)args[2], (ushort)args[3], (ushort)args[4]);
				} catch {
					//tady se octneme pokud zadal blbe ty souradnice (napred levy horni, pak pravy dolni roh!)
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