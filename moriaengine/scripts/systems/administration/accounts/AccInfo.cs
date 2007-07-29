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

	[Remark("Account info dialog")]
	public class D_AccInfo : CompiledGump {
		//private string[] charColumnDescs = new string[] {

		[Remark("Instance of the D_AccInfo, for possible access from other dialogs etc.")]
        private static D_AccInfo instance;
		public static D_AccInfo Instance {
			get {
				return instance;
			}
		}
        [Remark("Set the static reference to the instance of this dialog")]
		public D_AccInfo() {
			instance = this;
		}

		public override void Construct(Thing focus, AbstractCharacter src, object[] sa) {
			GameAccount displayed = (GameAccount)sa[0];

			ImprovedDialog dlg = new ImprovedDialog(this.GumpInstance);
			//pozadi    
			dlg.CreateBackground(800);
			dlg.SetLocation(50, 50);

			//nadpis
			dlg.Add(new GUTATable(1,0,ButtonFactory.D_BUTTON_WIDTH));
			dlg.LastTable[0,0] = TextFactory.CreateText("Account info - "+displayed.Name);
			//cudlik na zavreni dialogu
			dlg.LastTable[0,1] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonCross, 0);
			dlg.MakeTableTransparent();

			
			//uložit info o právì vytvoøeném dialogu pro návrat
			DialogStackItem.EnstackDialog(src, focus, D_AccInfo.Instance);
					
			dlg.WriteOut();
		}

		public override void OnResponse(GumpInstance gi, GumpResponse gr) {
			
		}
	}   
}