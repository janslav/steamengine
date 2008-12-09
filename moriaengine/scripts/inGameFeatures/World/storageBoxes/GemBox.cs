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
	public partial class GemBox : Item {

		public void EnsureDictionary() {
			if (inBoxGems == null) {
				inBoxGems = new Dictionary<ItemDef, int>();
			}
		}


		public override void On_DClick(AbstractCharacter ac) {
			this.Dialog(ac, SingletonScript<Dialogs.D_GemBox>.Instance);
		}

		public override void On_Create() {
			Color = 2448;
		}

	}
}

namespace SteamEngine.CompiledScripts.Dialogs {

	[Summary("Surprisingly the dialog that will display the GemBox guts")]
	public class D_GemBox : CompiledGumpDef {

		private static readonly TagKey buttonsForGemsTK = TagKey.Get("_rb_ButtonsForGems_");
		private static readonly TagKey buttonsCountTK = TagKey.Get("_rb_ButtonsCount_");

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			int i;
			Dictionary<int, ItemDef> dictButtonForGems = new Dictionary<int, ItemDef>();
			int buttonsCount = 0;
			int radku = 0;
			GemBox box = (GemBox) focus;
			if (box.inBoxGems == null) {
				radku = 0;
				i = 0;
			} else {
				i = box.inBoxGems.Count;
				radku = (i - 1) / 4;
			}
			int baseX = 20;
			int baseY = 60;
			SetLocation(70, 25);
			ResizePic(0, 0, 5054, 660, 165 + radku * 80);
			ResizePic(10, 10, 3000, 640, 145 + radku * 80);
			Button(15, 25, 4005, 4007, true, 0, 1);		// add gems
			Button(620, 10, 4017, 4019, true, 0, 0);	// close dialog
			HTMLGumpA(255, 15, 200, 20, "Bed�nka na drah� kameny", false, false);
			HTMLGumpA(55, 27, 100, 20, "P�idat kameny", false, false);
			if ((radku == 0) && (i == 0)) {
				HTMLGumpA(baseX, 75, 200, 20, "Bedna na drah� kameny je pr�zdn�", false, false);
			} else {
				i = 0;
				foreach (KeyValuePair<ItemDef, int> pair in box.inBoxGems) {
					Button(baseX, baseY, 4017, 4019, true, 0, 1000 + buttonsCount);
					HTMLGumpA(baseX + 35, baseY, 110, 20, pair.Key.Name, false, false);
					HTMLGumpA(baseX + 35, baseY + 20, 100, 20, "Pocet:", false, false);
					HTMLGumpA(baseX + 75, baseY + 20, 100, 20, Convert.ToString(pair.Value), false, false);
					CheckBox(baseX, baseY + 38, 9903, 9904, false, buttonsCount);
					HTMLGumpA(baseX + 35, baseY + 38, 50, 20, "Vyndat:", false, false);
					NumberEntryA(baseX + 80, baseY + 38, 65, 20, 0, buttonsCount, 0);
					TilePic(baseX + 110, baseY, pair.Key.Model);
					dictButtonForGems.Add(buttonsCount, pair.Key);
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
			args.SetTag(D_GemBox.buttonsCountTK, buttonsCount);
			args.SetTag(D_GemBox.buttonsForGemsTK, dictButtonForGems);
			Button(20, 125 + radku * 80, 4023, 4025, true, 0, 2);		// OK
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			GemBox box = (GemBox) gi.Focus;
			if (!((Player) gi.Cont).CanReachWithMessage(box)) {
				return;
			}
			if (gr.pressedButton == 0) {			// cancel
				return;
			} else if (gr.pressedButton == 1) {		// Add gems
				((Player) gi.Cont).Target(SingletonScript<Targ_GemBox>.Instance, gi.Focus);
			} else if (gr.pressedButton == 2) {		// OK -> give selected gems
				Dictionary<int, ItemDef> buttonShowItemDef = (Dictionary<int, ItemDef>) args.GetTag(D_GemBox.buttonsForGemsTK);
				int buttonsCount = TagMath.IGetTag(args, D_GemBox.buttonsCountTK);
				int i = 0;
				int gemsToGive = 0;
				while (i < buttonsCount) {
					if ((gr.IsSwitched(i)) && (gr.responseNumbers[i].number > 0)) {	// player wants to take at least one gem
						if (box.inBoxGems[buttonShowItemDef[i]] < (int) gr.responseNumbers[i].number) {
							((Player) gi.Cont).RedMessage("Sna�� se vyndat p��li� mnoho gem�: " + buttonShowItemDef[i].Name + ". Vynd�v� pln� po�et.");
							gemsToGive = box.inBoxGems[buttonShowItemDef[i]];
						} else {
							gemsToGive = (int) gr.responseNumbers[i].number;
						}
						buttonShowItemDef[i].Create(((Player) gi.Cont).BackpackAsContainer);
						Globals.lastNewItem.Amount = (uint) gemsToGive;
						gi.Cont.SysMessage("Vynd�v� z bedny " + Convert.ToString(gemsToGive) + "ks " + buttonShowItemDef[i].Name + ".");
						box.inBoxGems[buttonShowItemDef[i]] -= gemsToGive;
						box.pocetGemu -= gemsToGive;
						if (box.inBoxGems[buttonShowItemDef[i]] == 0) {
							box.inBoxGems.Remove(buttonShowItemDef[i]);
						}
					}
					i++;
				}
			} else if (gr.pressedButton >= 1000) {
				int thisButtonValue = (int) gr.pressedButton - 1000;
				Dictionary<int, ItemDef> buttonShowItemDef = (Dictionary<int, ItemDef>) args.GetTag(D_GemBox.buttonsForGemsTK);
				buttonShowItemDef[thisButtonValue].Create(((Player) gi.Cont).BackpackAsContainer);
				Globals.lastNewItem.Amount = (uint) box.inBoxGems[buttonShowItemDef[thisButtonValue]];
				box.inBoxGems.Remove(buttonShowItemDef[thisButtonValue]);
				box.Dialog(gi.Cont, SingletonScript<Dialogs.D_GemBox>.Instance);
			}
		}
	}

	public class Targ_GemBox : CompiledTargetDef {

		protected override void On_Start(Player self, object parameter) {
			self.SysMessage("Zam�� drah� k�men, kter� chce� vlo�it do bedny.");
			base.On_Start(self, parameter);
		}

		protected override bool On_TargonItem(Player self, Item targetted, object parameter) {
			GemBox focus = parameter as GemBox;
			if ((!self.CanReachWithMessage(focus)) || (!self.CanReachWithMessage(targetted))) {
				return false;
			}
			if (targetted.Type.Defname == "t_gem") {
				int previousCount;
				focus.EnsureDictionary();
				if (!focus.inBoxGems.TryGetValue(targetted.TypeDef, out previousCount)) {
					previousCount = 0;
				}
				if (focus.pocetGemu + (int) targetted.Amount > focus.TypeDef.Capacity) {	// poresime prekroceni nosnosti bedny -> do bedny se prida jen tolik gemu, kolik skutecne lze pridat
					int gemsToTake = focus.TypeDef.Capacity - focus.pocetGemu;
					targetted.Amount -= (uint) gemsToTake;
					focus.pocetGemu += gemsToTake;
					focus.inBoxGems[targetted.TypeDef] = previousCount + gemsToTake;
				} else {
					focus.pocetGemu += (int) targetted.Amount;
					focus.inBoxGems[targetted.TypeDef] = previousCount + (int) targetted.Amount;
					targetted.Delete();
				}
			} else {
				self.SysMessage("Do bedny m��e� p�idat jen drah� kameny.");
			}
			return true;
		}

		protected override void On_TargonCancel(Player self, object parameter) {
			GemBox focus = parameter as GemBox;
			focus.Dialog(self, SingletonScript<Dialogs.D_GemBox>.Instance);
		}
	}
}