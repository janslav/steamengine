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
using SteamEngine.Packets;
using SteamEngine.LScript;


namespace SteamEngine.CompiledScripts {
	public partial class RegBox : Item {

		public override void On_DClick(AbstractCharacter from) {
			Character src = from as Character;
			if (src != null) {
				if (src.currentSkill != null) {
					src.AbortSkill();
				}
				src.SysMessage("pico si na me dclick");
				this.Dialog(Dialogs.D_RegBox.Instance);
			}
		}

	}
}

namespace SteamEngine.CompiledScripts.Dialogs {

	[Remark("Surprisingly the dialog that will display the RegBox guts")]
	public class D_RegBox : CompiledGump {
		[Remark("Instance of the D_RegBox, for possible access from other dialogs etc.")]
		private static D_RegBox instance;
		public static D_RegBox Instance {
			get {
				return instance;
			}
		}
        [Remark("Set the static reference to the instance of this dialog")]
		public D_RegBox() {
			instance = this;
		}

		public override void Construct(Thing focus, AbstractCharacter src, object[] sa) {
			RegBox box = (RegBox) focus;
			SetLocation(70, 25);
			ResizePic(0, 0, 5054, 660, 350);
			ResizePic(10, 10, 3000, 640, 330);
			AddButton(10, 25, 4005, 4007, 0, 0, 1);
			AddButton(620, 10, 4017, 4019, 1, 0, 0);	// close dialog
			HTMLGumpA(245, 15, 100, 20, "Bedynka na regy", false, false);
			//AddButton(10, 25, 4005, 4007, 1, 0, 1);		// add reagents
			HTMLGumpA(55, 27, 100, 20, "Pridat regy", false, false);

		}
		public override void OnResponse(GumpInstance gi, GumpResponse gr, object[] args) {
			if (gr.pressedButton == 0) {
				gi.Cont.Message("Nulaaaa");
				return;
			} else if (gr.pressedButton == 1) {
				gi.Cont.Message("Jednaaa");
			}

		}
	}
}