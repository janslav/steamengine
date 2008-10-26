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
	[Dialogs.ViewableClass]
	public partial class RegBox : Item {

		public void EnsureDictionary() {
			if (inBoxReags == null) {
				inBoxReags = new Dictionary<ItemDef,int>();
			}
		}


		public override void On_DClick(AbstractCharacter ac) {
			this.Dialog(ac, SingletonScript<Dialogs.D_RegBox>.Instance);
		}

	}
}

namespace SteamEngine.CompiledScripts.Dialogs {

	[Summary("Surprisingly the dialog that will display the RegBox guts")]
	public class D_RegBox : CompiledGumpDef {

		private static readonly TagKey buttonsForReagsTK = TagKey.Get("_rb_ButtonsForReags_");
		private static readonly TagKey buttonsCountTK = TagKey.Get("_rb_ButtonsCount_");

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			int i;
			Dictionary<int, ItemDef> dictButtonForReags = new Dictionary<int,ItemDef>();
			int buttonsCount = 0;
			int radku = 0;
			RegBox box = (RegBox)focus;
			if (box.inBoxReags == null) {
				radku = 0;
				i = 0;
			} else {
				i = box.inBoxReags.Count;
				radku = (i - 1) / 4;
			}
			int baseX = 20;
			int baseY = 60;
			SetLocation(70, 25);
			ResizePic(0, 0, 5054, 660, 165 + radku * 80);
			ResizePic(10, 10, 3000, 640, 145 + radku * 80);
			Button(15, 25, 4005, 4007, true, 0, 1);		// add reagents
			Button(620, 10, 4017, 4019, true, 0, 0);	// close dialog
			HTMLGumpA(255, 15, 150, 20, "Bed�nka na regy", false, false);
			HTMLGumpA(55, 27, 100, 20, "P�idat regy", false, false);
			if ((radku == 0) && (i == 0)) {
				HTMLGumpA(baseX, 75, 200, 20, "Bedna na regy je pr�zdn�", false, false);
			} else {
				i = 0;
				foreach (KeyValuePair<ItemDef, int> pair in box.inBoxReags) {
					Button(baseX, baseY, 4017, 4019, true, 0, 1000 + buttonsCount);
					HTMLGumpA(baseX + 35, baseY, 110, 20, pair.Key.Name, false, false);
					HTMLGumpA(baseX + 35, baseY + 20, 100, 20, "Pocet:", false, false);
					HTMLGumpA(baseX + 75, baseY + 20, 100, 20, Convert.ToString(pair.Value), false, false);
					CheckBox(baseX, baseY + 38, 9903, 9904, false, buttonsCount);
					HTMLGumpA(baseX + 35, baseY + 38, 50, 20, "Vyndat:", false, false);
					NumberEntryA(baseX + 80, baseY + 38, 65, 20, 0, buttonsCount, 0);
					TilePic(baseX + 110, baseY, pair.Key.Model);
					dictButtonForReags.Add(buttonsCount, pair.Key);
					i++;
					buttonsCount++;
					if (i < 4) {
						baseX += 157;
					} else {
						baseX = 20;
						baseY += 80;
						i = 0;
					}
				}
			}
			args.SetTag(D_RegBox.buttonsCountTK, buttonsCount);
			args.SetTag(D_RegBox.buttonsForReagsTK, dictButtonForReags);
			Button(20, 125 + radku * 80, 4023, 4025, true, 0, 2);		// OK
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			RegBox box = (RegBox)gi.Focus;
			if (!((Player)gi.Cont).CanReachWithMessage(box)) {
				return;
			}
			if (gr.pressedButton == 0) {			// cancel
				return;
			} else if (gr.pressedButton == 1) {		// Add reags
				((Player) gi.Cont).Target(SingletonScript<Targ_RegBox>.Instance, gi.Focus);
			} else if (gr.pressedButton == 2) {		// OK -> give selected reags
				Dictionary<int, ItemDef> buttonShowItemDef = (Dictionary<int, ItemDef>)args.GetTag(D_RegBox.buttonsForReagsTK);
				int buttonsCount = TagMath.IGetTag(args,D_RegBox.buttonsCountTK);
				int i = 0;
				int reagsToGive = 0;
				while (i < buttonsCount) {
					if ( (gr.IsSwitched(i)) && (gr.responseNumbers[i].number > 0) ){	// player wants to take at least one reagent
						if (box.inBoxReags[buttonShowItemDef[i]] < (int)gr.responseNumbers[i].number) {
							((Player)gi.Cont).RedMessage("Sna�� se vyndat p��li� mnoho reg�: " + buttonShowItemDef[i].Name + ". Vynd�v� pln� po�et.");
							reagsToGive = box.inBoxReags[buttonShowItemDef[i]];
						} else {
							reagsToGive = (int)gr.responseNumbers[i].number;
						}
						buttonShowItemDef[i].Create(((Player)gi.Cont).BackpackAsContainer);
						Globals.lastNewItem.Amount = (uint)reagsToGive;
						gi.Cont.SysMessage("Vynd�v� z bedny " + Convert.ToString(reagsToGive) + "ks regu " + buttonShowItemDef[i].Name + ".");
						box.inBoxReags[buttonShowItemDef[i]] -= reagsToGive;
						box.pocetRegu -= reagsToGive;
						if (box.inBoxReags[buttonShowItemDef[i]] == 0) {
							box.inBoxReags.Remove(buttonShowItemDef[i]);
						}
					}
					i++;
				}
			} else if (gr.pressedButton >= 1000) {
				int thisButtonValue = (int)gr.pressedButton - 1000;
				Dictionary<int, ItemDef> buttonShowItemDef = (Dictionary<int, ItemDef>)args.GetTag(D_RegBox.buttonsForReagsTK);
				buttonShowItemDef[thisButtonValue].Create(((Player)gi.Cont).BackpackAsContainer);
				Globals.lastNewItem.Amount = (uint)box.inBoxReags[buttonShowItemDef[thisButtonValue]];
				box.inBoxReags.Remove(buttonShowItemDef[thisButtonValue]);
				box.Dialog(gi.Cont, SingletonScript<Dialogs.D_RegBox>.Instance);
			}
		}
	}

	public class Targ_RegBox : CompiledTargetDef {

		protected override void On_Start(Character self, object parameter) {
			self.SysMessage("Zam�� reagent, kter� chce� vlo�it do bedny.");
			base.On_Start(self, parameter);
		}

		protected override bool On_TargonItem(Character self, Item targetted, object parameter) {
			RegBox focus = parameter as RegBox;
			if ( (!self.CanReachWithMessage(focus)) || (!self.CanReachWithMessage(targetted)) ) {
				return false;
			}
			if (targetted.Type.Defname == "t_reagent") {
				int previousCount;
				focus.EnsureDictionary();
				if (!focus.inBoxReags.TryGetValue(targetted.TypeDef, out previousCount)) {
					previousCount = 0;
				}
				if (focus.pocetRegu + (int)targetted.Amount > focus.TypeDef.Capacity) {	// poresime prekroceni nosnosti bedny -> do bedny se prida jen tolik regu, kolik skutecne lze pridat
					int reagsToTake = focus.TypeDef.Capacity - focus.pocetRegu;
					targetted.Amount -= (uint)reagsToTake;
					focus.pocetRegu += reagsToTake;
					focus.inBoxReags[targetted.TypeDef] = previousCount + reagsToTake;
				} else {
					focus.pocetRegu += (int)targetted.Amount;
					focus.inBoxReags[targetted.TypeDef] = previousCount + (int)targetted.Amount;
					targetted.Delete();
				}
			} else {
				self.SysMessage("Do bedny m��e� p�idat jen regy.");
			}
			return true;
		}

		protected override void On_TargonCancel(Character self, object parameter) {
			RegBox focus = parameter as RegBox;
			focus.Dialog(self, SingletonScript<Dialogs.D_RegBox>.Instance);
		}
	}
}