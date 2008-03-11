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
using System.Reflection;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts.Dialogs {
	[Summary("Dialog zobrazuj�c� informace a vysv�tlivky symbol� pro nastaven�")]
	public class D_Settings_Help : CompiledGumpDef {

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			ImprovedDialog dlg = new ImprovedDialog(GumpInstance);
			dlg.CreateBackground(1100);
			dlg.SetLocation(0, 20);

			dlg.AddTable(new GUTATable(1, 0, ButtonFactory.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = TextFactory.CreateHeadline("Informace a vysv�tlivky");
			dlg.LastTable[0, 1] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonCross, 0);
			dlg.MakeLastTableTransparent();

			dlg.AddTable(new GUTATable(1, 85, 50, 40, 0, 185));			
			dlg.LastTable[0, 0] = TextFactory.CreateLabel("Popis hodnoty");
			dlg.LastTable[0, 1] = TextFactory.CreateLabel("Zkratka typu");
			dlg.LastTable[0, 2] = TextFactory.CreateLabel("Prefix");
			dlg.LastTable[0, 3] = TextFactory.CreateLabel("Z�pis hodnoty (ur�eno informac� o typu - viz zkratky)");
			dlg.LastTable[0, 4] = TextFactory.CreateLabel("P��klad");
			dlg.MakeLastTableTransparent();

			//��sla
			dlg.AddTable(new GUTATable(1));
			dlg.CopyColsFromLastTable();
			dlg.LastTable[0, 0] = TextFactory.CreateText("��slo");
			dlg.LastTable[0, 1] = TextFactory.CreateText("(Num)");
			dlg.LastTable[0, 3] = TextFactory.CreateText("Norm�ln� numericky");
			dlg.LastTable[0, 4] = TextFactory.CreateText("25; 13.3 apd.");
			dlg.MakeLastTableTransparent();

			//�et�zce
			dlg.AddTable(new GUTATable(1));
			dlg.CopyColsFromLastTable();
			dlg.LastTable[0, 0] = TextFactory.CreateText("String");
			dlg.LastTable[0, 1] = TextFactory.CreateText("(Str)");
			dlg.LastTable[0, 3] = TextFactory.CreateText("Text v �vozovk�ch");
			dlg.LastTable[0, 4] = TextFactory.CreateText("\"foobar\"");
			dlg.MakeLastTableTransparent();

			//Thingy
			dlg.AddTable(new GUTATable(1));
			dlg.CopyColsFromLastTable();
			dlg.LastTable[0, 0] = TextFactory.CreateText("Thing");
			dlg.LastTable[0, 1] = TextFactory.CreateText("(Thg)");
			dlg.LastTable[0, 2] = TextFactory.CreateText(Hues.Blue, "#");
			dlg.LastTable[0, 3] = TextFactory.CreateText("Existuj�c� UID");
			dlg.LastTable[0, 4] = TextFactory.CreateText("#094dfd; #01");
			dlg.MakeLastTableTransparent();

			//Regiony
			dlg.AddTable(new GUTATable(1));
			dlg.CopyColsFromLastTable();
			dlg.LastTable[0, 0] = TextFactory.CreateText("Region");
			dlg.LastTable[0, 1] = TextFactory.CreateText("(Reg)");
			dlg.LastTable[0, 3] = TextFactory.CreateText("Defname regionu v z�vork�ch");
			dlg.LastTable[0, 4] = TextFactory.CreateText("(a_Edoras)");
			dlg.MakeLastTableTransparent();

			//Accounty
			dlg.AddTable(new GUTATable(1));
			dlg.CopyColsFromLastTable();
			dlg.LastTable[0, 0] = TextFactory.CreateText("Account");
			dlg.LastTable[0, 1] = TextFactory.CreateText("($)");
			dlg.LastTable[0, 2] = TextFactory.CreateText(Hues.Blue, "$");
			dlg.LastTable[0, 3] = TextFactory.CreateText("N�zev accountu");
			dlg.LastTable[0, 4] = TextFactory.CreateText("$kandelabr");
			dlg.MakeLastTableTransparent();

			//Abstract defy
			dlg.AddTable(new GUTATable(1));
			dlg.CopyColsFromLastTable();
			dlg.LastTable[0, 0] = TextFactory.CreateText("AbstractScript");
			dlg.LastTable[0, 1] = TextFactory.CreateText("(Scp)");
			dlg.LastTable[0, 2] = TextFactory.CreateText(Hues.Blue, "#");
			dlg.LastTable[0, 3] = TextFactory.CreateText("Defname existuj�c�ho skriptu");
			dlg.LastTable[0, 4] = TextFactory.CreateText("#c_man; #c_0x123, kde 0x123 je n�jak� model");
			dlg.MakeLastTableTransparent();

			//TimeSpan
			dlg.AddTable(new GUTATable(1));
			dlg.CopyColsFromLastTable();
			dlg.LastTable[0, 0] = TextFactory.CreateText("TimeSpan");
			dlg.LastTable[0, 1] = TextFactory.CreateText("(:)");
			dlg.LastTable[0, 2] = TextFactory.CreateText(Hues.Blue, ":");
			dlg.LastTable[0, 3] = TextFactory.CreateText("-d.hh:mm:ss.ff, kde '-', sekundy a desetiny sekundy jsou nepovinn�. P�esnost max 7 m�st.");
			dlg.LastTable[0, 4] = TextFactory.CreateText(":14.12:15; :0.1:13:12; :1.1:11:11.1111111");
			dlg.MakeLastTableTransparent();

			//DateTime
			dlg.AddTable(new GUTATable(1));
			dlg.CopyColsFromLastTable();
			dlg.LastTable[0, 0] = TextFactory.CreateText("DateTime");
			dlg.LastTable[0, 1] = TextFactory.CreateText("(::)");
			dlg.LastTable[0, 2] = TextFactory.CreateText(Hues.Blue, "::");
			dlg.LastTable[0, 3] = TextFactory.CreateText("dd.MM.yyyy HH:mm:ss.FF, kde desetiny sekundy, sekundy nebo cel� hodinov� �daj jsou nepovinn�. P�esnost max 7 m�st.");
			dlg.LastTable[0, 4] = TextFactory.CreateText("::11.12.1913 15:16:17.18");
			dlg.MakeLastTableTransparent();

			//Pozice
			dlg.AddTable(new GUTATable(1));
			dlg.CopyColsFromLastTable();
			dlg.LastTable[0, 0] = TextFactory.CreateText("Pozice");
			dlg.LastTable[0, 1] = TextFactory.CreateText("(nD)");
			dlg.LastTable[0, 2] = TextFactory.CreateText(Hues.Blue, "(nD)");
			dlg.LastTable[0, 3] = TextFactory.CreateText("x,y,z,m (po�et ��sel z�le�� na typu pozice)");
			dlg.LastTable[0, 4] = TextFactory.CreateText("(2D)991,996; (4D)1000,1000,0,1");
			dlg.MakeLastTableTransparent();

			//IP�ka
			dlg.AddTable(new GUTATable(1));
			dlg.CopyColsFromLastTable();
			dlg.LastTable[0, 0] = TextFactory.CreateText("IP");
			dlg.LastTable[0, 1] = TextFactory.CreateText("(IP)");
			dlg.LastTable[0, 2] = TextFactory.CreateText(Hues.Blue, "(IP)");
			dlg.LastTable[0, 3] = TextFactory.CreateText("IP adresa v korektn� podob�");
			dlg.LastTable[0, 4] = TextFactory.CreateText("(IP)127.0.0.1");
			dlg.MakeLastTableTransparent();

			//Timer key
			dlg.AddTable(new GUTATable(1));
			dlg.CopyColsFromLastTable();
			dlg.LastTable[0, 0] = TextFactory.CreateText("Timer key");
			dlg.LastTable[0, 1] = TextFactory.CreateText("(%)");
			dlg.LastTable[0, 2] = TextFactory.CreateText(Hues.Blue, "%");
			dlg.LastTable[0, 3] = TextFactory.CreateText("Timer key");
			dlg.LastTable[0, 4] = TextFactory.CreateText("%SpawnTimer");
			dlg.MakeLastTableTransparent();

			//Triggery
			dlg.AddTable(new GUTATable(1));
			dlg.CopyColsFromLastTable();
			dlg.LastTable[0, 0] = TextFactory.CreateText("Trigger key");
			dlg.LastTable[0, 1] = TextFactory.CreateText("(@)");
			dlg.LastTable[0, 2] = TextFactory.CreateText(Hues.Blue, "@");
			dlg.LastTable[0, 3] = TextFactory.CreateText("Trigger key");
			dlg.LastTable[0, 4] = TextFactory.CreateText("@create");
			dlg.MakeLastTableTransparent();

			//Enumerace
			dlg.AddTable(new GUTATable(1));
			dlg.CopyColsFromLastTable();
			dlg.LastTable[0, 0] = TextFactory.CreateText("Enumerace");
			dlg.LastTable[0, 1] = TextFactory.CreateText("(Enum)");
			dlg.LastTable[0, 3] = TextFactory.CreateText("Norm�ln� numericky (tak�ka nepou��van�)");
			dlg.MakeLastTableTransparent();

			//Objekty
			dlg.AddTable(new GUTATable(1));
			dlg.CopyColsFromLastTable();
			dlg.LastTable[0, 0] = TextFactory.CreateText("Object");
			dlg.LastTable[0, 1] = TextFactory.CreateText("(Obj)");
			dlg.LastTable[0, 3] = TextFactory.CreateText("Libovoln� typ zapsan� v korektn� podob� (viz v��e)");
			dlg.LastTable[0, 4] = TextFactory.CreateText("#1234; \"barbar\"; 15 apd.");
			dlg.MakeLastTableTransparent();

			//Globals
			dlg.AddTable(new GUTATable(1));
			dlg.CopyColsFromLastTable();
			dlg.LastTable[0, 0] = TextFactory.CreateText("Globals");
			dlg.LastTable[0, 1] = TextFactory.CreateText("(Glob)");
			dlg.LastTable[0, 2] = TextFactory.CreateText(Hues.Blue, "#");
			dlg.LastTable[0, 3] = TextFactory.CreateText("Globals (nepou��van� v norm�ln�m nastaven�)");
			dlg.LastTable[0, 4] = TextFactory.CreateText("#globals (doslova)");			
			dlg.MakeLastTableTransparent();

			dlg.WriteOut();//a vykresl�me ten info dialog
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			//seznam nastavenych nebo zkousenych polozek
			if(gr.pressedButton == 0) { //end
				DialogStacking.ShowPreviousDialog(gi);
			}
		}
	}	
}