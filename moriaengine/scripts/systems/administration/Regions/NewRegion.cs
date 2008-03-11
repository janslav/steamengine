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
	[Summary("Dialog for creating a new region")]
	public class D_New_Region : CompiledGumpDef {
		private static readonly TagKey defNameTK = TagKey.Get("_new_region_defname_");
		private static readonly TagKey nameTK = TagKey.Get("_new_region_name_");
		private static readonly TagKey homeposTK = TagKey.Get("_new_region_homepos_");
		public static readonly TagKey parentDefTK = TagKey.Get("_new_region_parent_defname_");
		private static int width = 450;

		[Summary("V argumentech (args) mohou prijit parametry pro dialogove editfieldy")]
		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			string minX, minY, maxX, maxY, name, defname, home, parent; //predzadane hodnoty (if any)

			minX = (args.ArgsArray[0] != null ? args.ArgsArray[0].ToString() : "");
			minY = (args.ArgsArray[1] != null ? args.ArgsArray[1].ToString() : "");
			maxX = (args.ArgsArray[2] != null ? args.ArgsArray[2].ToString() : "");
			maxY = (args.ArgsArray[3] != null ? args.ArgsArray[3].ToString() : "");
            name = TagMath.SGetTag(args, nameTK);
            if (name == null) name = "";

            defname = TagMath.SGetTag(args, defNameTK);
            if (defname == null) defname = "";

            home = TagMath.SGetTag(args, homeposTK);
            if (home == null) home = "";

            parent = TagMath.SGetTag(args, parentDefTK);
            if (parent == null) parent = "";

			ImprovedDialog dlg = new ImprovedDialog(this.GumpInstance);
			//pozadi    
			dlg.CreateBackground(width);
			dlg.SetLocation(150, 150);

			//nadpis
			dlg.AddTable(new GUTATable(1, 0, ButtonFactory.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = TextFactory.CreateHeadline("Zalo�en� nov�ho regionu");
			dlg.LastTable[0, 1] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonCross, 0);//cudlik na zavreni dialogu
			dlg.MakeLastTableTransparent();

			//navod
			dlg.AddTable(new GUTATable(3, 0));
			dlg.LastTable[0, 0] = TextFactory.CreateHeadline("Vypl� v�e v�etn� jednoho rectanglu (dal�� lze p�idat pozd�ji)");
			dlg.LastTable[1, 0] = TextFactory.CreateHeadline("(MinX,MinY) - levy horni roh, (MaxX,MaxY) - pravy dolni roh.");
			dlg.MakeLastTableTransparent();

			//textiky a editfieldy na zadani vseho a souradnic rectanglu
			dlg.AddTable(new GUTATable(2, 100, 100, 0, 100));
			dlg.LastTable[0, 0] = TextFactory.CreateLabel("Name");
			dlg.LastTable[0, 1] = InputFactory.CreateInput(LeafComponentTypes.InputText, 21, name);
			dlg.LastTable[0, 2] = TextFactory.CreateLabel("Defname");
			dlg.LastTable[0, 3] = InputFactory.CreateInput(LeafComponentTypes.InputText, 22, defname);
			dlg.LastTable[1, 0] = TextFactory.CreateLabel("Home (4D)");
			dlg.LastTable[1, 1] = InputFactory.CreateInput(LeafComponentTypes.InputText, 23, home);
			dlg.LastTable[1, 2] = TextFactory.CreateLabel("Parent");
			dlg.LastTable[1, 3] = InputFactory.CreateInput(LeafComponentTypes.InputText, 24, parent);
			dlg.LastTable[1, 3] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonPaper,100-ButtonFactory.D_BUTTON_WIDTH,0,2);
			dlg.MakeLastTableTransparent();
			dlg.AddTable(new GUTATable(2, 100, 100, 0, 100));
			dlg.LastTable[0, 0] = TextFactory.CreateLabel("MinX");
			dlg.LastTable[0, 1] = InputFactory.CreateInput(LeafComponentTypes.InputNumber, 31, minX);
			dlg.LastTable[0, 2] = TextFactory.CreateLabel("MinY");
			dlg.LastTable[0, 3] = InputFactory.CreateInput(LeafComponentTypes.InputNumber, 32, minY);
			dlg.LastTable[1, 0] = TextFactory.CreateLabel("MaxX");
			dlg.LastTable[1, 1] = InputFactory.CreateInput(LeafComponentTypes.InputNumber, 33, maxX);
			dlg.LastTable[1, 2] = TextFactory.CreateLabel("MaxY");
			dlg.LastTable[1, 3] = InputFactory.CreateInput(LeafComponentTypes.InputNumber, 34, maxY);
			dlg.MakeLastTableTransparent();

			//send button
			dlg.AddTable(new GUTATable(1, ButtonFactory.D_BUTTON_WIDTH, 0));
			dlg.LastTable[0, 0] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonOK, 1);
			dlg.LastTable[0, 1] = TextFactory.CreateText("Vytvo�it");
			dlg.MakeLastTableTransparent();

			dlg.WriteOut();
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			if(gr.pressedButton == 0) { //exit
				DialogStacking.ShowPreviousDialog(gi); //zobrazit pripadny predchozi dialog
			} else if(gr.pressedButton == 1) { //ulozit				
				//zkusime precist vsechny parametry
				args.ArgsArray[0] = Convert.ToUInt16(gr.GetNumberResponse(31));
				args.ArgsArray[1] = Convert.ToUInt16(gr.GetNumberResponse(32));
				args.ArgsArray[2] = Convert.ToUInt16(gr.GetNumberResponse(33));
				args.ArgsArray[3] = Convert.ToUInt16(gr.GetNumberResponse(34));
				args.SetTag(nameTK, gr.GetTextResponse(21)); //name
				args.SetTag(defNameTK, gr.GetTextResponse(22)); //defname
				args.SetTag(homeposTK, gr.GetTextResponse(23)); //home pozice
				args.SetTag(parentDefTK, gr.GetTextResponse(24)); //parentuv defname				

				string name, defname;
				name = gr.GetTextResponse(21);
				defname = gr.GetTextResponse(22);
				//precteme parametry a zkusime vytvorit rectangle
				MutableRectangle newRect = null;
				try {
					newRect = new MutableRectangle((ushort)args.ArgsArray[0], (ushort)args.ArgsArray[1], (ushort)args.ArgsArray[2], (ushort)args.ArgsArray[3]);
				} catch {
					//tady se octneme pokud zadal blbe ty souradnice (napred levy horni, pak pravy dolni roh!)
					//stackneme a zobrazime chybu
					Gump newGi = D_Display_Text.ShowError("MinX/Y ma byt levy horni roh, MaxX/Y ma byt pravy dolni");
					DialogStacking.EnstackDialog(gi, newGi);
					return;
				}
				Point4D home = null;				
				try {
					home = (Point4D)ObjectSaver.Load(gr.GetTextResponse(23));//homepos
				} catch {
					//podelal homepos
					//stackneme a zobrazime chybu
					Gump newGi = D_Display_Text.ShowError("Chybne zadana home position - ocekavano '(4D)x,y,z,m'");
					DialogStacking.EnstackDialog(gi, newGi);
					return;
				}
				StaticRegion parent = StaticRegion.GetByDefname(gr.GetTextResponse(24));//parent defname
				if(name == null || name.Equals("") || defname == null || defname.Equals("")) {
					//neco blbe s namem nebo defnamem
					//stackneme a zobrazime chybu
					Gump newGi = D_Display_Text.ShowError("Name i defname musi byt zadano");
					DialogStacking.EnstackDialog(gi, newGi);
					return;
				} else if(!newRect.Contains(home)) {
					//homepos by nesedla
					//stackneme a zobrazime chybu
					Gump newGi = D_Display_Text.ShowError("Home pozice ("+gr.GetTextResponse(23)+") musi lezet v zadanem rectanglu ("+args.ArgsArray[0]+","+args.ArgsArray[1]+")-("+args.ArgsArray[2]+","+args.ArgsArray[3]+")");
					DialogStacking.EnstackDialog(gi, newGi);
					return;
				} else if(StaticRegion.GetByName(name) != null) {
					//jmeno uz existuje
					//stackneme a zobrazime chybu
					Gump newGi = D_Display_Text.ShowError("Region se jmenem "+name+" uz existuje");
					DialogStacking.EnstackDialog(gi, newGi);
					return;
				} else if(StaticRegion.GetByDefname(defname) != null) {
					//defname uz existuje
					//stackneme a zobrazime chybu
					Gump newGi = D_Display_Text.ShowError("Region s defnamem "+defname+" uz existuje");
					DialogStacking.EnstackDialog(gi, newGi);
					return;
				} else if(parent == null) {
					//parent je povinnost!
					Gump newGi = D_Display_Text.ShowError("Rodi�ovsk� region "+gr.GetTextResponse(24)+" neexistuje");
					DialogStacking.EnstackDialog(gi, newGi);
					return;
				}

				List<MutableRectangle> oneRectList = new List<MutableRectangle>();
				oneRectList.Add(newRect);
				//vsef poradku tak hura vytvorit novy region
				StaticRegion newRegion = new FlaggedRegion(defname, parent);
				if(newRegion.InitializeNewRegion(name, home, oneRectList)) {
					D_Display_Text.ShowInfo("Vytvo�en� regionu bylo �sp�n�");
				} else {
					D_Display_Text.ShowError("P�i vyt�v�en� regionu do�lo k probl�m�m - viz konzole");					
				}
				return; //konec at uz se stackem nebo ne, dalsi navigace bude z infotextu
				//DialogStacking.ShowPreviousDialog(gi); //zobrazit pripadny predchozi dialog
			} else if(gr.pressedButton == 2) {//vyber regionu parenta                         dialog,vyhledavani,prvni index, seznam regionu, trideni
				//zkusime precist vsechyn parametry abychom je kdyztak meli
				args.ArgsArray[0] = Convert.ToUInt16(gr.GetNumberResponse(31));
				args.ArgsArray[1] = Convert.ToUInt16(gr.GetNumberResponse(32));
				args.ArgsArray[2] = Convert.ToUInt16(gr.GetNumberResponse(33));
				args.ArgsArray[3] = Convert.ToUInt16(gr.GetNumberResponse(34));
				args.SetTag(nameTK,gr.GetTextResponse(21)); //name
				args.SetTag(defNameTK,gr.GetTextResponse(22)); //defname
				args.SetTag(homeposTK,gr.GetTextResponse(23)); //home pozice
				args.SetTag(parentDefTK,gr.GetTextResponse(24)); //parentuv defname

				DialogArgs newArgs = new DialogArgs();
				newArgs.SetTag(D_Regions.regsSortingTK, RegionsSorting.NameAsc);//zakladni trideni			
				Gump newGi = gi.Cont.Dialog(SingletonScript<D_SelectParent>.Instance,newArgs);
				DialogStacking.EnstackDialog(gi, newGi); //vlozime napred dialog do stacku				
			}
		}

		[Summary("Create a new region. Function accessible from the game." +
				"The function is designed to be triggered using .NewRegion" +
				"but it can be also called from other dialogs - such as regions list...")]
		[SteamFunction]
		public static void NewRegion(Thing self, ScriptArgs text) {
			//Parametry dialogu: - jen 4 zakladni souradnice rectanglu
			self.Dialog(SingletonScript<D_New_Region>.Instance, new DialogArgs(0,0,0,0));
		}
	}
}