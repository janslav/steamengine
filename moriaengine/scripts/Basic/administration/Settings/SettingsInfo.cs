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


namespace SteamEngine.CompiledScripts.Dialogs {
	/// <summary>Dialog zobrazuj�c� informace a vysv�tlivky symbol� pro nastaven�</summary>
	public class D_Settings_Help : CompiledGumpDef {

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			ImprovedDialog dlg = new ImprovedDialog(GumpInstance);
			dlg.CreateBackground(1100);
			dlg.SetLocation(0, 20);

			dlg.AddTable(new GUTATable(1, 0, ButtonMetrics.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = GUTAText.Builder.TextHeadline("Informace a vysv�tlivky").Build();
			dlg.LastTable[0, 1] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonCross).Id(0).Build();
			dlg.MakeLastTableTransparent();

			dlg.AddTable(new GUTATable(1, 85, 50, 40, 0, 185));
			dlg.LastTable[0, 0] = GUTAText.Builder.TextLabel("Popis hodnoty").Build();
			dlg.LastTable[0, 1] = GUTAText.Builder.TextLabel("Zkratka typu").Build();
			dlg.LastTable[0, 2] = GUTAText.Builder.TextLabel("Prefix").Build();
			dlg.LastTable[0, 3] = GUTAText.Builder.TextLabel("Z�pis hodnoty (ur�eno informac� o typu - viz zkratky)").Build();
			dlg.LastTable[0, 4] = GUTAText.Builder.TextLabel("P��klad").Build();
			dlg.MakeLastTableTransparent();

			//��sla
			dlg.AddTable(new GUTATable(1));
			dlg.CopyColsFromLastTable();
			dlg.LastTable[0, 0] = GUTAText.Builder.Text("��slo").Build();
			dlg.LastTable[0, 1] = GUTAText.Builder.Text("(Num)").Build();
			dlg.LastTable[0, 3] = GUTAText.Builder.Text("Norm�ln� numericky").Build();
			dlg.LastTable[0, 4] = GUTAText.Builder.Text("25; 13.3 apd.").Build();
			dlg.MakeLastTableTransparent();

			//�et�zce
			dlg.AddTable(new GUTATable(1));
			dlg.CopyColsFromLastTable();
			dlg.LastTable[0, 0] = GUTAText.Builder.Text("String").Build();
			dlg.LastTable[0, 1] = GUTAText.Builder.Text("(Str)").Build();
			dlg.LastTable[0, 3] = GUTAText.Builder.Text("Text v �vozovk�ch").Build();
			dlg.LastTable[0, 4] = GUTAText.Builder.Text("\"foobar\"").Build();
			dlg.MakeLastTableTransparent();

			//Thingy
			dlg.AddTable(new GUTATable(1));
			dlg.CopyColsFromLastTable();
			dlg.LastTable[0, 0] = GUTAText.Builder.Text("Thing").Build();
			dlg.LastTable[0, 1] = GUTAText.Builder.Text("(Thg)").Build();
			dlg.LastTable[0, 2] = GUTAText.Builder.Text("#").Hue(Hues.Blue).Build();
			dlg.LastTable[0, 3] = GUTAText.Builder.Text("Existuj�c� UID").Build();
			dlg.LastTable[0, 4] = GUTAText.Builder.Text("#094dfd; #01").Build();
			dlg.MakeLastTableTransparent();

			//Regiony
			dlg.AddTable(new GUTATable(1));
			dlg.CopyColsFromLastTable();
			dlg.LastTable[0, 0] = GUTAText.Builder.Text("Region").Build();
			dlg.LastTable[0, 1] = GUTAText.Builder.Text("(Reg)").Build();
			dlg.LastTable[0, 3] = GUTAText.Builder.Text("Defname regionu v z�vork�ch").Build();
			dlg.LastTable[0, 4] = GUTAText.Builder.Text("(a_Edoras)").Build();
			dlg.MakeLastTableTransparent();

			//Accounty
			dlg.AddTable(new GUTATable(1));
			dlg.CopyColsFromLastTable();
			dlg.LastTable[0, 0] = GUTAText.Builder.Text("Account").Build();
			dlg.LastTable[0, 1] = GUTAText.Builder.Text("($)").Build();
			dlg.LastTable[0, 2] = GUTAText.Builder.Text("$").Hue(Hues.Blue).Build();
			dlg.LastTable[0, 3] = GUTAText.Builder.Text("N�zev accountu").Build();
			dlg.LastTable[0, 4] = GUTAText.Builder.Text("$kandelabr").Build();
			dlg.MakeLastTableTransparent();

			//Abstract defy
			dlg.AddTable(new GUTATable(1));
			dlg.CopyColsFromLastTable();
			dlg.LastTable[0, 0] = GUTAText.Builder.Text("AbstractScript").Build();
			dlg.LastTable[0, 1] = GUTAText.Builder.Text("(Scp)").Build();
			dlg.LastTable[0, 2] = GUTAText.Builder.Text("#").Hue(Hues.Blue).Build();
			dlg.LastTable[0, 3] = GUTAText.Builder.Text("Defname existuj�c�ho skriptu").Build();
			dlg.LastTable[0, 4] = GUTAText.Builder.Text("#c_man; #c_0x123, kde 0x123 je n�jak� model").Build();
			dlg.MakeLastTableTransparent();

			//TimeSpan
			dlg.AddTable(new GUTATable(1));
			dlg.CopyColsFromLastTable();
			dlg.LastTable[0, 0] = GUTAText.Builder.Text("TimeSpan").Build();
			dlg.LastTable[0, 1] = GUTAText.Builder.Text("(:)").Build();
			dlg.LastTable[0, 2] = GUTAText.Builder.Text(":").Hue(Hues.Blue).Build();
			dlg.LastTable[0, 3] = GUTAText.Builder.Text("-d.hh:mm:ss.ff, kde '-', sekundy a desetiny sekundy jsou nepovinn�. P�esnost max 7 m�st.").Build();
			dlg.LastTable[0, 4] = GUTAText.Builder.Text(":14.12:15; :0.1:13:12; :1.1:11:11.1111111").Build();
			dlg.MakeLastTableTransparent();

			//DateTime
			dlg.AddTable(new GUTATable(1));
			dlg.CopyColsFromLastTable();
			dlg.LastTable[0, 0] = GUTAText.Builder.Text("DateTime").Build();
			dlg.LastTable[0, 1] = GUTAText.Builder.Text("(::)").Build();
			dlg.LastTable[0, 2] = GUTAText.Builder.Text("::").Hue(Hues.Blue).Build();
			dlg.LastTable[0, 3] = GUTAText.Builder.Text("dd.MM.yyyy HH:mm:ss.FF, kde desetiny sekundy, sekundy nebo cel� hodinov� �daj jsou nepovinn�. P�esnost max 7 m�st.").Build();
			dlg.LastTable[0, 4] = GUTAText.Builder.Text("::11.12.1913 15:16:17.18").Build();
			dlg.MakeLastTableTransparent();

			//Pozice
			dlg.AddTable(new GUTATable(1));
			dlg.CopyColsFromLastTable();
			dlg.LastTable[0, 0] = GUTAText.Builder.Text("Pozice").Build();
			dlg.LastTable[0, 1] = GUTAText.Builder.Text("(nD)").Build();
			dlg.LastTable[0, 2] = GUTAText.Builder.Text("(nD)").Hue(Hues.Blue).Build();
			dlg.LastTable[0, 3] = GUTAText.Builder.Text("x,y,z,m (po�et ��sel z�le�� na typu pozice)").Build();
			dlg.LastTable[0, 4] = GUTAText.Builder.Text("(2D)991,996; (4D)1000,1000,0,1").Build();
			dlg.MakeLastTableTransparent();

			//IP�ka
			dlg.AddTable(new GUTATable(1));
			dlg.CopyColsFromLastTable();
			dlg.LastTable[0, 0] = GUTAText.Builder.Text("IP").Build();
			dlg.LastTable[0, 1] = GUTAText.Builder.Text("(IP)").Build();
			dlg.LastTable[0, 2] = GUTAText.Builder.Text("(IP)").Hue(Hues.Blue).Build();
			dlg.LastTable[0, 3] = GUTAText.Builder.Text("IP adresa v korektn� podob�").Build();
			dlg.LastTable[0, 4] = GUTAText.Builder.Text("(IP)127.0.0.1").Build();
			dlg.MakeLastTableTransparent();

			//Resources list
			dlg.AddTable(new GUTATable(1));
			dlg.CopyColsFromLastTable();
			dlg.LastTable[0, 0] = GUTAText.Builder.Text("Resources list").Build();
			dlg.LastTable[0, 1] = GUTAText.Builder.Text("(RL)").Build();
			dlg.LastTable[0, 2] = GUTAText.Builder.Text("(RL)").Hue(Hues.Blue).Build();
			dlg.LastTable[0, 3] = GUTAText.Builder.Text("Seznam resourc�").Build();
			dlg.LastTable[0, 4] = GUTAText.Builder.Text("(RL)5 i_apple, 23.5 Hiding, 3 a_warcry, t_light").Build();
			dlg.MakeLastTableTransparent();

			//Timer key
			dlg.AddTable(new GUTATable(1));
			dlg.CopyColsFromLastTable();
			dlg.LastTable[0, 0] = GUTAText.Builder.Text("Timer key").Build();
			dlg.LastTable[0, 1] = GUTAText.Builder.Text("(%)").Build();
			dlg.LastTable[0, 2] = GUTAText.Builder.Text("%").Hue(Hues.Blue).Build();
			dlg.LastTable[0, 3] = GUTAText.Builder.Text("Timer key").Build();
			dlg.LastTable[0, 4] = GUTAText.Builder.Text("%SpawnTimer").Build();
			dlg.MakeLastTableTransparent();

			//Triggery
			dlg.AddTable(new GUTATable(1));
			dlg.CopyColsFromLastTable();
			dlg.LastTable[0, 0] = GUTAText.Builder.Text("Trigger key").Build();
			dlg.LastTable[0, 1] = GUTAText.Builder.Text("(@)").Build();
			dlg.LastTable[0, 2] = GUTAText.Builder.Text("@").Hue(Hues.Blue).Build();
			dlg.LastTable[0, 3] = GUTAText.Builder.Text("Trigger key").Build();
			dlg.LastTable[0, 4] = GUTAText.Builder.Text("@create").Build();
			dlg.MakeLastTableTransparent();

			//Enumerace
			dlg.AddTable(new GUTATable(1));
			dlg.CopyColsFromLastTable();
			dlg.LastTable[0, 0] = GUTAText.Builder.Text("Enumerace").Build();
			dlg.LastTable[0, 1] = GUTAText.Builder.Text("(Enum)").Build();
			dlg.LastTable[0, 3] = GUTAText.Builder.Text("Norm�ln� numericky (tak�ka nepou��van�)").Build();
			dlg.MakeLastTableTransparent();

			//Objekty
			dlg.AddTable(new GUTATable(1));
			dlg.CopyColsFromLastTable();
			dlg.LastTable[0, 0] = GUTAText.Builder.Text("Object").Build();
			dlg.LastTable[0, 1] = GUTAText.Builder.Text("(Obj)").Build();
			dlg.LastTable[0, 3] = GUTAText.Builder.Text("Libovoln� typ zapsan� v korektn� podob� (viz v��e)").Build();
			dlg.LastTable[0, 4] = GUTAText.Builder.Text("#1234; \"barbar\"; 15 apd.").Build();
			dlg.MakeLastTableTransparent();

			//Globals
			dlg.AddTable(new GUTATable(1));
			dlg.CopyColsFromLastTable();
			dlg.LastTable[0, 0] = GUTAText.Builder.Text("Globals").Build();
			dlg.LastTable[0, 1] = GUTAText.Builder.Text("(Glob)").Build();
			dlg.LastTable[0, 2] = GUTAText.Builder.Text("#").Hue(Hues.Blue).Build();
			dlg.LastTable[0, 3] = GUTAText.Builder.Text("Globals (nepou��van� v norm�ln�m nastaven�)").Build();
			dlg.LastTable[0, 4] = GUTAText.Builder.Text("#globals (doslova)").Build();
			dlg.MakeLastTableTransparent();

			dlg.WriteOut();//a vykresl�me ten info dialog
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			//seznam nastavenych nebo zkousenych polozek
			if (gr.PressedButton == 0) { //end
				DialogStacking.ShowPreviousDialog(gi);
			}
		}
	}
}