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
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts.Dialogs {

	[Summary("A new ability adding dialog")]
	public class D_NewAbility : CompiledGumpDef {
		private static int width = 400;
		private static int innerWidth = width - 2 * ImprovedDialog.D_BORDER - 2 * ImprovedDialog.D_SPACE;

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			Character abiliter = (Character) args.GetTag(D_CharsAbilitiesList.abiliterTK); //na koho budeme abilitu zakladat?

			ImprovedDialog dlg = new ImprovedDialog(this.GumpInstance);
			//pozadi    
			dlg.CreateBackground(width);
			dlg.SetLocation(50, 50);

			//nadpis
			dlg.AddTable(new GUTATable(1, innerWidth - 2 * ButtonMetrics.D_BUTTON_WIDTH - ImprovedDialog.D_COL_SPACE, 0, ButtonMetrics.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = GUTAText.Builder.TextHeadline("Pøidání ability na " + abiliter.ToString()).Build();
			//cudlik na zavreni dialogu
			dlg.LastTable[0, 1] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonPaper).Id(2).Build();//cudlik na info o hodnotach			
			dlg.LastTable[0, 2] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonCross).Id(0).Build();
			dlg.MakeLastTableTransparent();

			//dialozek s inputama
			dlg.AddTable(new GUTATable(2, 0, 275)); //1.sl - ability defname, 2.sl - pocet bodu
			//napred napisy 
			dlg.LastTable[0, 0] = GUTAText.Builder.TextLabel("Ability defname").Build();
			dlg.LastTable[1, 0] = GUTAText.Builder.TextLabel("Poèet bodù").Build();
			dlg.LastTable[0, 1] = GUTAInput.Builder.Id(10).Build();
			dlg.LastTable[1, 1] = GUTAInput.Builder.Id(11).Type(LeafComponentTypes.InputNumber).Build();
			dlg.MakeLastTableTransparent(); //zpruhledni zbytek dialogu

			//a posledni radek s tlacitkem
			dlg.AddTable(new GUTATable(1, ButtonMetrics.D_BUTTON_WIDTH, 0));
			dlg.LastTable[0, 0] = GUTAButton.Builder.Id(1).Build();
			dlg.LastTable[0, 1] = GUTAText.Builder.TextLabel("Potvrdit").Build();
			dlg.MakeLastTableTransparent(); //zpruhledni posledni radek

			dlg.WriteOut();
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			if (gr.PressedButton == 0) {
				DialogStacking.ShowPreviousDialog(gi); //zobrazit pripadny predchozi dialog
				return;
			} else if (gr.PressedButton == 1) {
				//nacteme obsah obou input fieldu
				string abilityDefname = gr.GetTextResponse(10);
				double abilityPoints = gr.GetNumberResponse(11);
				//ziskame objektovou reprezentaci vlozene hodnoty. ocekava samozrejme prefixy pokud je potreba!
				AbilityDef abDef = AbilityDef.GetByDefname(abilityDefname);
				if(abDef == null) {
					//zadal neexistujici abilitydefname
					Gump newGi = D_Display_Text.ShowError("Chybne zadano, neznamy abilitydefname: " + abilityDefname);
					DialogStacking.EnstackDialog(gi, newGi);
					return;
				}
				Character abiliter = (Character) gi.Focus;
				abiliter.SetRealAbilityPoints(abDef, (int)abilityPoints); //zalozi novou / zmodifikuje hodnotu existujici ability

				//vzit jeste predchozi dialog, musime smazat abilitieslist aby se pregeneroval a obsahoval pripadnou novou abilitu
				Gump prevStacked = DialogStacking.PopStackedDialog(gi);
				prevStacked.InputArgs.RemoveTag(D_CharsAbilitiesList.listTK);
				DialogStacking.ResendAndRestackDialog(prevStacked);
			} else if (gr.PressedButton == 2) {
				Gump newGi = gi.Cont.Dialog(SingletonScript<D_Settings_Help>.Instance);
				DialogStacking.EnstackDialog(gi, newGi); //ulozime dialog do stacku
			}
		}
	}
}