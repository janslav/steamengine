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
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts.Dialogs {
	[Remark("Dialog for creating a new region")]
	public class D_New_Region : CompiledGump {
		private static int width = 450;

		[Remark("V argumentech (args) mohou prijit parametry pro dialogove editfieldy")]
		public override void Construct(Thing focus, AbstractCharacter sendTo, object[] args) {
			string minX, minY, maxX, maxY, name, defname, home, parent; //predzadane hodnoty (if any)

			minX = (args[0] != null ? args[0].ToString() : "");
			minY = (args[1] != null ? args[1].ToString() : "");
			maxX = (args[2] != null ? args[2].ToString() : "");
			maxY = (args[3] != null ? args[3].ToString() : "");
			name = (args[4] != null ? args[4].ToString() : "");
			defname = (args[5] != null ? args[5].ToString() : "");
			home = (args[6] != null ? args[6].ToString() : "");
			parent = (args[7] != null ? args[7].ToString() : "");


			ImprovedDialog dlg = new ImprovedDialog(this.GumpInstance);
			//pozadi    
			dlg.CreateBackground(width);
			dlg.SetLocation(150, 150);

			//nadpis
			dlg.Add(new GUTATable(1, 0, ButtonFactory.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = TextFactory.CreateHeadline("Založení nového regionu");
			dlg.LastTable[0, 1] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonCross, 0);//cudlik na zavreni dialogu
			dlg.MakeTableTransparent();

			//navod
			dlg.Add(new GUTATable(3, 0));
			dlg.LastTable[0, 0] = TextFactory.CreateHeadline("Vyplò vše vèetnì jednoho rectanglu (další lze pøidat pozdìji)");
			dlg.LastTable[1, 0] = TextFactory.CreateHeadline("(MinX,MinY) - levy horni roh, (MaxX,MaxY) - pravy dolni roh.");
			dlg.MakeTableTransparent();

			//textiky a editfieldy na zadani vseho a souradnic rectanglu
			dlg.Add(new GUTATable(2, 100, 100, 0, 100));
			dlg.LastTable[0, 0] = TextFactory.CreateLabel("Name");
			dlg.LastTable[0, 1] = InputFactory.CreateInput(LeafComponentTypes.InputText, 21, name);
			dlg.LastTable[0, 2] = TextFactory.CreateLabel("Defname");
			dlg.LastTable[0, 3] = InputFactory.CreateInput(LeafComponentTypes.InputText, 22, defname);
			dlg.LastTable[1, 0] = TextFactory.CreateLabel("Home (4D)");
			dlg.LastTable[1, 1] = InputFactory.CreateInput(LeafComponentTypes.InputText, 23, home);
			dlg.LastTable[1, 2] = TextFactory.CreateLabel("Parent");
			dlg.LastTable[1, 3] = InputFactory.CreateInput(LeafComponentTypes.InputText, 24, parent);
			dlg.LastTable[1, 3] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonPaper,100-ButtonFactory.D_BUTTON_WIDTH,0,2);
			dlg.MakeTableTransparent();
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
			dlg.LastTable[0, 1] = TextFactory.CreateText("Vytvoøit");
			dlg.MakeTableTransparent();

			dlg.WriteOut();
		}

		public override void OnResponse(GumpInstance gi, GumpResponse gr, object[] args) {
			if(gr.pressedButton == 0) { //exit
				DialogStacking.ShowPreviousDialog(gi); //zobrazit pripadny predchozi dialog
			} else if(gr.pressedButton == 1) { //ulozit				
				//zkusime precist vsechyn parametry
				args[0] = Convert.ToUInt16(gr.GetNumberResponse(31));
				args[1] = Convert.ToUInt16(gr.GetNumberResponse(32));
				args[2] = Convert.ToUInt16(gr.GetNumberResponse(33));
				args[3] = Convert.ToUInt16(gr.GetNumberResponse(34));
				args[4] = gr.GetTextResponse(21); //name
				args[5] = gr.GetTextResponse(22); //defname
				args[6] = gr.GetTextResponse(23); //home pozice
				args[7] = gr.GetTextResponse(24); //parentuv defname

				string name, defname;
				name = (string)args[4];
				defname = (string)args[5];
				//precteme parametry a zkusime vytvorit rectangle
				MutableRectangle newRect = null;
				try {					
					newRect = new MutableRectangle((ushort)args[0], (ushort)args[1], (ushort)args[2], (ushort)args[3]);
				} catch {
					//tady se octneme pokud zadal blbe ty souradnice (napred levy horni, pak pravy dolni roh!)
					//stackneme a zobrazime chybu
					GumpInstance newGi = D_Display_Text.ShowError("MinX/Y ma byt levy horni roh, MaxX/Y ma byt pravy dolni");
					DialogStacking.EnstackDialog(gi, newGi);
					return;
				}
				Point4D home = null;				
				try {
					home = (Point4D)ObjectSaver.Load((string)args[6]);
				} catch {
					//podelal homepos
					//stackneme a zobrazime chybu
					GumpInstance newGi = D_Display_Text.ShowError("Chybne zadana home position - ocekavano '(4D)x,y,z,m'");
					DialogStacking.EnstackDialog(gi, newGi);
					return;
				}
				StaticRegion parent = StaticRegion.GetByDefname((string)args[7]);
				if(name == null || name.Equals("") || defname == null || defname.Equals("")) {
					//neco blbe s namem nebo defnamem
					//stackneme a zobrazime chybu
					GumpInstance newGi = D_Display_Text.ShowError("Name i defname musi byt zadano");
					DialogStacking.EnstackDialog(gi, newGi);
					return;
				} else if(!newRect.Contains(home)) {
					//homepos by nesedla
					//stackneme a zobrazime chybu
					GumpInstance newGi = D_Display_Text.ShowError("Home pozice ("+args[6]+") musi lezet v zadanem rectanglu ("+args[0]+","+args[1]+")-("+args[2]+","+args[3]+")");
					DialogStacking.EnstackDialog(gi, newGi);
					return;
				} else if(StaticRegion.GetByName(name) != null) {
					//jmeno uz existuje
					//stackneme a zobrazime chybu
					GumpInstance newGi = D_Display_Text.ShowError("Region se jmenem " + args[4] + " uz existuje");
					DialogStacking.EnstackDialog(gi, newGi);
					return;
				} else if(StaticRegion.GetByDefname(defname) != null) {
					//defname uz existuje
					//stackneme a zobrazime chybu
					GumpInstance newGi = D_Display_Text.ShowError("Region s defnamem " + args[5] + " uz existuje");
					DialogStacking.EnstackDialog(gi, newGi);
					return;
				} else if(parent == null) {
					//parent je povinnost!
					GumpInstance newGi = D_Display_Text.ShowError("Rodièovský region "+ args[7]+" neexistuje");
					DialogStacking.EnstackDialog(gi, newGi);
					return;
				}

				List<MutableRectangle> oneRectList = new List<MutableRectangle>();
				oneRectList.Add(newRect);
				//vsef poradku tak hura vytvorit novy region
				StaticRegion newRegion = new FlaggedRegion(defname, parent);
				newRegion.InitializeNewRegion(name, home, oneRectList);
				
				DialogStacking.ShowPreviousDialog(gi); //zobrazit pripadny predchozi dialog
			} else if(gr.pressedButton == 2) {//vyber regionu parenta                         dialog,vyhledavani,prvni index, seznam regionu, trideni
				//zkusime precist vsechyn parametry abychom je kdyztak meli
				args[0] = Convert.ToUInt16(gr.GetNumberResponse(31));
				args[1] = Convert.ToUInt16(gr.GetNumberResponse(32));
				args[2] = Convert.ToUInt16(gr.GetNumberResponse(33));
				args[3] = Convert.ToUInt16(gr.GetNumberResponse(34));
				args[4] = gr.GetTextResponse(21); //name
				args[5] = gr.GetTextResponse(22); //defname
				args[6] = gr.GetTextResponse(23); //home pozice
				args[7] = gr.GetTextResponse(24); //parentuv defname

				GumpInstance newGi = gi.Cont.Dialog(SingletonScript<D_SelectParent>.Instance,"",0, null, null);
				DialogStacking.EnstackDialog(gi, newGi); //vlozime napred dialog do stacku				
			}
		}

		[Remark("Create a new region. Function accessible from the game." +
				"The function is designed to be triggered using .NewRegion" +
				"but it can be also called from other dialogs - such as regions list...")]
		[SteamFunction]
		public static void NewRegion(Thing self, ScriptArgs text) {
			//Parametry dialogu: - zatim nejsou
			self.Dialog(SingletonScript<D_New_Region>.Instance,"","","","","","","","");
		}
	}
}