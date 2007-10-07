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

		public void EnsureDictionary() {
			if (inBoxReags == null) {
				inBoxReags = new Dictionary<ItemDef,int>();
			}
		}


		public override void On_DClick(AbstractCharacter ac) {
			Character dClicker = ac as Character;
			if (!dClicker.GetMap().CanSeeLOSFromTo(dClicker, this)) {
				ac.SysMessage("ted bys na to hele jako ze nemel videt");
			}
			if (dClicker.currentSkill != null) {
				dClicker.AbortSkill();
			}
			this.Dialog(ac, SingletonScript<Dialogs.D_RegBox>.Instance);
		}

	}
}

namespace SteamEngine.CompiledScripts.Dialogs {

	[Remark("Surprisingly the dialog that will display the RegBox guts")]
	public class D_RegBox : CompiledGump {
		public override void Construct(Thing focus, AbstractCharacter sendTo, object[] sa) {
			int i = 0;
			int radku = 0;
			RegBox box = (RegBox)focus;
			box.EnsureDictionary();
			foreach (KeyValuePair<ItemDef, int> pair in box.inBoxReags) {
				i++;
				if (i > 3) {
					radku++;
					i = 0;
				}
			}
			int baseX = 20;
			int baseY = 60;
			SetLocation(70, 25);
			ResizePic(0, 0, 5054, 660, 160 + radku * 80);
			ResizePic(10, 10, 3000, 640, 140 + radku * 80);
			Button(15, 25, 4005, 4007, true, 0, 1);		// add reagents
			Button(620, 10, 4017, 4019, true, 0, 0);	// close dialog
			HTMLGumpA(255, 15, 100, 20, "Bedýnka na regy", false, false);
			HTMLGumpA(55, 27, 100, 20, "Pøidat regy", false, false);
			if ((radku == 0) && (i == 0)) {
				HTMLGumpA(baseX, 75, 200, 20, "Bedna na regy je prázdná", false, false);
			} else {
				i = 0;
				foreach (KeyValuePair<ItemDef, int> pair in box.inBoxReags) {
					Button(baseX, baseY, 4017, 4019, true, 0, 1000 + i);
					HTMLGumpA(baseX + 35, baseY, 110, 20, pair.Key.Name, false, false);
					HTMLGumpA(baseX + 35, baseY + 20, 100, 20, "Pocet:", false, false);
					HTMLGumpA(baseX + 75, baseY + 20, 100, 20, Convert.ToString(pair.Value), false, false);
					CheckBox(baseX, baseY + 38, 9903, 9904, false, i);
					HTMLGumpA(baseX + 35, baseY + 38, 50, 20, "Vyndat:", false, false);
					NumberEntryA(baseX + 80, baseY + 38, 65, 20, 0, i, 0);
					TilePic(baseX + 110, baseY, pair.Key.Model);
					i++;
					if (i < 4) {
						baseX += 157;
					} else {
						baseX = 20;
						baseY += 80;
						i = 0;
					}
				}
			}
			Button(20, 125 + radku * 80, 4023, 4025, true, 0, 2);		// OK
		}

		public override void OnResponse(GumpInstance gi, GumpResponse gr, object[] args) {
			RegBox box = (RegBox)gi.Focus;
			if (gr.pressedButton == 0) {			// cancel
				return;
			} else if (gr.pressedButton == 1) {		// Add reags
				((Player) gi.Cont).Target(SingletonScript<Targ_RegBox>.Instance, gi.Focus);
			} else if (gr.pressedButton == 2) {		// OK -> give selected reags
				int i = 0;
				int reagsToGive = 0;
				foreach (KeyValuePair<ItemDef, int> pair in box.inBoxReags) {
					if ( (gr.IsSwitched(i)) && (gr.responseNumbers[i].number > 0) ){	// player wants to take at least one reagent
						if (pair.Value < (int)gr.responseNumbers[i].number) {
							((Player)gi.Cont).RedMessage("Snazis se vyndat prilis mnoho regu: " + pair.Key.Name + ". Vyndavas jen tolik, kolik muzes.");
							reagsToGive = pair.Value;
						} else {
							reagsToGive = (int)gr.responseNumbers[i].number;
						}
						pair.Key.Create(((Player)gi.Cont).Backpack);
						Globals.lastNewItem.Amount = (uint)reagsToGive;
						gi.Cont.Message("vyndavam: "+pair.Key.Name);
						box.inBoxReags[pair.Key] -= reagsToGive;
					}
					i++;
				}

				
			}
		}
	}

	public class Targ_RegBox : CompiledTargetDef {

		protected override void On_Start(Character self, object parameter) {
			self.SysMessage("Zamìø reagent, který chceš vložit do bedny.");
			base.On_Start(self, parameter);
		}

		protected override bool On_TargonItem(Character self, Item targetted, object parameter) {
			RegBox focus = parameter as RegBox;
			if (!self.GetMap().CanSeeLOSFromTo(self, targetted)) {
				self.ClilocSysMessage(500876);
			}
			if ((Point2D.GetSimpleDistance(self, focus) > 3) || (Point2D.GetSimpleDistance(self, targetted) > 3)) {
				self.ClilocSysMessage(500295);
				return false;
			}
			if (targetted.Type.Defname == "t_reagent") {
				int previousCount;
				if (!focus.inBoxReags.TryGetValue(targetted.TypeDef, out previousCount)) {
					previousCount = 0;
				}
				if (focus.pocetRegu + Convert.ToInt32(targetted.Amount) > focus.TypeDef.Capacity) {	// poresime prekroceni nosnosti bedny -> do bedny se prida jen tolik regu, kolik skutecne lze pridat
					int reagsToTake = focus.TypeDef.Capacity - focus.pocetRegu;
					targetted.Amount -= Convert.ToUInt32(reagsToTake);
					focus.pocetRegu += reagsToTake;
					focus.inBoxReags[targetted.TypeDef] = previousCount + reagsToTake;
				} else {
					focus.pocetRegu += Convert.ToInt32(targetted.Amount);
					focus.inBoxReags[targetted.TypeDef] = previousCount + Convert.ToInt32(targetted.Amount);
					targetted.Delete();
				}
			} else {
				self.SysMessage("Do bedny mùžeš pøidat jen regy.");
			}
			return true;
		}

		protected override void On_TargonCancel(Character self, object parameter) {
			RegBox focus = parameter as RegBox;
			focus.Dialog(self, SingletonScript<Dialogs.D_RegBox>.Instance);
		}
	}
}