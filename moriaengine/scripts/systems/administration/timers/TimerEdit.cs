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
using SteamEngine.Timers;
using SteamEngine.LScript;

namespace SteamEngine.CompiledScripts.Dialogs {

	[Remark("A timer editing dialog")]
	public class D_EditTimer : CompiledGump {
		private static int width = 400;
		
		[Remark("Instance of the D_EditTimer, for possible access from other dialogs etc.")]
        private static D_EditTimer instance;
		public static D_EditTimer Instance {
			get {
				return instance;
			}
		}
        [Remark("Set the static reference to the instance of this dialog")]
		public D_EditTimer() {
			instance = this;
		}

		public override void Construct(Thing focus, AbstractCharacter src, object[] sa) {
			TagHolder th = (TagHolder)sa[0]; //na koho budeme timer ukladat?
			Timer tm = (Timer)sa[1]; //timer ktery editujeme

			ImprovedDialog dlg = new ImprovedDialog(this.GumpInstance);
			//pozadi    
			dlg.CreateBackground(width);
			dlg.SetLocation(50, 50);

			//nadpis
			dlg.Add(new GUTATable(1, 0, ButtonFactory.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = TextFactory.CreateHeadline("�prava timeru "+tm.name+" na "+th.ToString());
			//cudlik na zavreni dialogu
			dlg.LastTable[0, 1] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonCross, 0);
			dlg.MakeTableTransparent();

			//tabulka s inputem
			dlg.Add(new GUTATable(2, 0, 275)); //1.sl - edit nazev, 2.sl - edit hodnota
			//napred napisy 
			dlg.LastTable[0, 0] = TextFactory.CreateLabel("N�zev timeru");
			dlg.LastTable[0, 1] = TextFactory.CreateLabel("�as [s]");
			dlg.LastTable[1, 0] = TextFactory.CreateText(tm.name.ToString());
			dlg.LastTable[1, 1] = InputFactory.CreateInput(LeafComponentTypes.InputNumber, 11, tm.InSeconds.ToString());
			dlg.MakeTableTransparent(); //zpruhledni zbytek dialogu

			//a posledni radek s tlacitkem
			dlg.Add(new GUTATable(1,ButtonFactory.D_BUTTON_WIDTH,0));
			dlg.LastTable[0, 0] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonTick, 1);
			dlg.LastTable[0, 1] = TextFactory.CreateLabel("Potvrdit");
			dlg.MakeTableTransparent(); //zpruhledni posledni radek

			DialogStackItem.EnstackDialog(src, focus, D_EditTimer.Instance,
					th, //tagholder na nejz budeme tag nastavovat, pro priste 
					tm); //timer co editujeme
			dlg.WriteOut();
		}

		public override void OnResponse(GumpInstance gi, GumpResponse gr, object[] args) {
			//vzit "tenhle" dialog ze stacku
			DialogStackItem dsi = DialogStackItem.PopStackedDialog(gi.Cont.Conn);			

			if(gr.pressedButton == 0) {
				DialogStackItem.ShowPreviousDialog(gi.Cont.Conn); //zobrazit pripadny predchozi dialog
				//create_timer dialog jsme uz vytahli ze stacku, nemusime ho tedy dodatecne odstranovat
			} else if(gr.pressedButton == 1) {
				//nacteme obsah input fieldu
				int timerTime = Convert.ToInt32(gr.GetNumberResponse(11));
				Timer tm = (Timer)dsi.Args[1];
				tm.InSeconds = timerTime;
				DialogStackItem prevStacked = DialogStackItem.PopStackedDialog(gi.Cont.Conn);
				if(prevStacked.InstanceType.Equals(typeof(D_TimerList))) {
					//prisli jsme z timerlistu - mame zde seznam a muzeme ho smazat
					prevStacked.Args[3] = null;
				}
				prevStacked.Show();								
			} 
		}		
	}
}