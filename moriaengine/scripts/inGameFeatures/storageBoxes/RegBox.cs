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
		//public Dictionary<ItemDef, int> guts = null;

		public void EnsureDictionary() {
			if (inBoxReags == null) {
				inBoxReags = new Dictionary<ItemDef,int>();
			}
		}


		public override void On_DClick(AbstractCharacter ac) {
			Character dClicker = ac as Character;
			if (dClicker.currentSkill != null) {
				dClicker.AbortSkill();
			}
			dClicker.SysMessage("pièo si na mì dclick");
			this.Dialog(ac, Dialogs.D_RegBox.Instance);			
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

		public override void Construct(Thing focus, AbstractCharacter sendTo, object[] sa) {
			short radek = 1;
			short sloup = 1;
			RegBox box = (RegBox) focus;
			SetLocation(70, 25);
			ResizePic(0, 0, 5054, 660, 350);
			ResizePic(10, 10, 3000, 640, 330);
			Button(10, 25, 4005, 4007, true, 0, 1);
			Button(620, 10, 4017, 4019, true, 0, 0);	// close dialog
			HTMLGumpA(245, 15, 100, 20, "Bedýnka na regy", false, false);
			HTMLGumpA(55, 27, 100, 20, "Pøidat regy", false, false);
			box.EnsureDictionary();
			foreach (KeyValuePair<ItemDef, int> pair in box.inBoxReags) {

				//self.SysMessage(Convert.ToString(pair.Key) + " -> " + Convert.ToString(pair.Value));
				//pair.Key je itemdef
				//pair.Value je cislo
			}
		}

		public override void OnResponse(GumpInstance gi, GumpResponse gr, object[] args) {
			if (gr.pressedButton == 0) {
				gi.Cont.Message("Cancel");
				return;
			} else if (gr.pressedButton == 1) {
				gi.Focus.Message("Posilam se sefe");
				((Player)gi.Cont).Target(Targ_RegBox.Instance, gi.Focus);				
			}
		}
	}

	public class Targ_RegBox : CompiledTargetDef {

		private static Targ_RegBox instance;
		public static Targ_RegBox Instance {
			get {
				return instance;
			}
		}

		public Targ_RegBox() {
			instance = this;
		}

		protected override void On_Start(Character self, object parameter) {
			self.SysMessage("Zamìø reagent, který chceš vložit do bedny.");
			base.On_Start(self, parameter);
		}

		protected override bool On_TargonItem(Character self, Item targetted, object parameter) {
			RegBox focus = parameter as RegBox;
			if (targetted.Type.Defname == "t_reagent") {
				int previousCount;
				self.SysMessage("pøidávám reagenty do bedny ...");
				if (!focus.inBoxReags.TryGetValue(targetted.Def as ItemDef, out previousCount)) {
					previousCount = 0;
				}
				if (focus.pocetRegu + Convert.ToInt32(targetted.Amount) > focus.Def.Capacity) {	// poresime prekroceni nosnosti bedny -> do bedny se prida jen tolik regu, kolik skutecne lze pridat
					int reagsToTake = focus.Def.Capacity - focus.pocetRegu;
					targetted.Amount -= Convert.ToUInt32(reagsToTake);
					focus.pocetRegu += reagsToTake;
					focus.inBoxReags[targetted.Def as ItemDef] = previousCount + reagsToTake;
				} else {
					focus.pocetRegu += Convert.ToInt32(targetted.Amount);
					focus.inBoxReags[targetted.Def as ItemDef] = previousCount + Convert.ToInt32(targetted.Amount);
					targetted.Delete();
				}
			} else {
				self.SysMessage("Do bedny mùžeš pøidat jen regy.");
				return true;
			}
			foreach (KeyValuePair<ItemDef, int> pair in focus.inBoxReags) {
				self.SysMessage(Convert.ToString(pair.Key) + " -> " + Convert.ToString(pair.Value));
				//pair.Key je itemdef
				//pair.Value je cislo
			}
			return false;
		}
	}
}