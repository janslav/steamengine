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
using System.Collections.Generic;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Scripting.Objects;

namespace SteamEngine.CompiledScripts {
	[ViewableClass]
	public partial class RegBox : Item {

		public void EnsureDictionary() {
			if (this.inBoxReags == null) {
				this.inBoxReags = new Dictionary<ItemDef, int>();
			}
		}


		public override void On_DClick(AbstractCharacter ac) {
			this.Dialog(ac, SingletonScript<D_RegBox>.Instance);
		}

	}

	[ViewableClass]
	public partial class RegBoxDef {
	}
}

namespace SteamEngine.CompiledScripts.Dialogs {

	/// <summary>Surprisingly the dialog that will display the RegBox guts</summary>
	public class D_RegBox : CompiledGumpDef {

		private static readonly TagKey buttonsForReagsTK = TagKey.Acquire("_rb_ButtonsForReags_");
		private static readonly TagKey buttonsCountTK = TagKey.Acquire("_rb_ButtonsCount_");

		public override void Construct(CompiledGump gi, Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			int i;
			var dictButtonForReags = new Dictionary<int, ItemDef>();
			var buttonsCount = 0;
			var radku = 0;
			var box = (RegBox) focus;
			if (box.inBoxReags == null) {
				radku = 0;
				i = 0;
			} else {
				i = box.inBoxReags.Count;
				radku = (i - 1) / 4;
			}
			var baseX = 20;
			var baseY = 60;
			gi.SetLocation(70, 25);
			gi.ResizePic(0, 0, 5054, 660, 165 + radku * 80);
			gi.ResizePic(10, 10, 3000, 640, 145 + radku * 80);
			gi.Button(15, 25, 4005, 4007, true, 0, 1);		// add reagents
			gi.Button(620, 10, 4017, 4019, true, 0, 0);	// close dialog
			gi.HtmlGumpA(255, 15, 150, 20, "Bed�nka na regy", false, false);
			gi.HtmlGumpA(55, 27, 100, 20, "P�idat regy", false, false);
			if ((radku == 0) && (i == 0)) {
				gi.HtmlGumpA(baseX, 75, 200, 20, "Bedna na regy je pr�zdn�", false, false);
			} else {
				i = 0;
				foreach (var pair in box.inBoxReags) {
					gi.Button(baseX, baseY, 4017, 4019, true, 0, 1000 + buttonsCount);
					gi.HtmlGumpA(baseX + 35, baseY, 110, 20, pair.Key.Name, false, false);
					gi.HtmlGumpA(baseX + 35, baseY + 20, 100, 20, "Pocet:", false, false);
					gi.HtmlGumpA(baseX + 75, baseY + 20, 100, 20, Convert.ToString(pair.Value), false, false);
					gi.CheckBox(baseX, baseY + 38, 9903, 9904, false, buttonsCount);
					gi.HtmlGumpA(baseX + 35, baseY + 38, 50, 20, "Vyndat:", false, false);
					gi.NumberEntryA(baseX + 80, baseY + 38, 65, 20, 0, buttonsCount, 0);
					gi.TilePic(baseX + 110, baseY, pair.Key.Model);
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
			args.SetTag(buttonsCountTK, buttonsCount);
			args.SetTag(buttonsForReagsTK, dictButtonForReags);
			gi.Button(20, 125 + radku * 80, 4023, 4025, true, 0, 2);		// OK
		}

		public override void OnResponse(CompiledGump gi, Thing focus, GumpResponse gr, DialogArgs args) {
			var box = (RegBox) gi.Focus;
			if (!((Player) gi.Cont).CanPickUpWithMessage(box)) {
				return;
			}
			if (gr.PressedButton == 0) {			// cancel
			} else if (gr.PressedButton == 1) {		// Add reags
				((Player) gi.Cont).Target(SingletonScript<Targ_RegBox>.Instance, gi.Focus);
			} else if (gr.PressedButton == 2) {		// OK -> give selected reags
				var buttonShowItemDef = (Dictionary<int, ItemDef>) args.GetTag(buttonsForReagsTK);
				var buttonsCount = TagMath.IGetTag(args, buttonsCountTK);
				var i = 0;
				var reagsToGive = 0;
				while (i < buttonsCount) {
					var desiredCount = (int) gr.GetNumberResponse(i);
					if ((gr.IsSwitched(i)) && (desiredCount > 0)) {	// player wants to take at least one reagent
						if (box.inBoxReags[buttonShowItemDef[i]] < desiredCount) {
							((Player) gi.Cont).RedMessage("Sna�� se vyndat p��li� mnoho reg�: " + buttonShowItemDef[i].Name + ". Vynd�v� pln� po�et.");
							reagsToGive = box.inBoxReags[buttonShowItemDef[i]];
						} else {
							reagsToGive = desiredCount;
						}
						buttonShowItemDef[i].Create(((Player) gi.Cont).Backpack);
						Globals.LastNewItem.Amount = reagsToGive;
						gi.Cont.SysMessage("Vynd�v� z bedny " + Convert.ToString(reagsToGive) + "ks regu " + buttonShowItemDef[i].Name + ".");
						box.inBoxReags[buttonShowItemDef[i]] -= reagsToGive;
						box.pocetRegu -= reagsToGive;
						if (box.inBoxReags[buttonShowItemDef[i]] == 0) {
							box.inBoxReags.Remove(buttonShowItemDef[i]);
						}
					}
					i++;
				}
			} else if (gr.PressedButton >= 1000) {
				var thisButtonValue = gr.PressedButton - 1000;
				var buttonShowItemDef = (Dictionary<int, ItemDef>) args.GetTag(buttonsForReagsTK);
				buttonShowItemDef[thisButtonValue].Create(((Player) gi.Cont).Backpack);
				Globals.LastNewItem.Amount = box.inBoxReags[buttonShowItemDef[thisButtonValue]];
				box.inBoxReags.Remove(buttonShowItemDef[thisButtonValue]);
				box.Dialog(gi.Cont, SingletonScript<D_RegBox>.Instance);
			}
		}
	}

	public class Targ_RegBox : CompiledTargetDef {

		protected override void On_Start(Player self, object parameter) {
			self.SysMessage("Zam�� reagent, kter� chce� vlo�it do bedny.");
			base.On_Start(self, parameter);
		}

		protected override TargetResult On_TargonItem(Player self, Item targetted, object parameter) {
			var regBox = (RegBox) parameter;

			if (!self.CanReachWithMessage(regBox)) {
				return TargetResult.Done;
			}

			if (self.CanPickUpWithMessage(targetted)) {
				if (targetted.Type == SingletonScript<t_reagent>.Instance) {
					int previousCount;
					regBox.EnsureDictionary();
					if (!regBox.inBoxReags.TryGetValue(targetted.TypeDef, out previousCount)) {
						previousCount = 0;
					}
					if (regBox.pocetRegu + targetted.Amount > regBox.TypeDef.Capacity) {	// poresime prekroceni nosnosti bedny -> do bedny se prida jen tolik regu, kolik skutecne lze pridat
						var reagsToTake = regBox.TypeDef.Capacity - regBox.pocetRegu;
						targetted.Amount -= reagsToTake;
						regBox.pocetRegu += reagsToTake;
						regBox.inBoxReags[targetted.TypeDef] = previousCount + reagsToTake;
					} else {
						regBox.pocetRegu += targetted.Amount;
						regBox.inBoxReags[targetted.TypeDef] = previousCount + targetted.Amount;
						targetted.Delete();
					}
				} else {
					self.SysMessage("Do bedny m��e� p�idat jen regy.");

				}
			}
			return TargetResult.RestartTargetting; //pridavame dal a dal, krome pripadu kdy jsme box ztratili z dosahu
		}

		protected override void On_TargonCancel(Player self, object parameter) {
			var focus = parameter as RegBox;
			focus.Dialog(self, SingletonScript<D_RegBox>.Instance);
		}
	}
}