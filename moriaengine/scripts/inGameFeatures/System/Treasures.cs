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
	public partial class TreasureSpawn : Item
	{
		public override void On_DClick(AbstractCharacter ac) {
			Player p = ac as Player;
			if (p.IsGM()) {
				this.Dialog(ac, SingletonScript<Dialogs.D_TreasureSpawn>.Instance);
			} else {
				p.SysMessage("otvirame poklad");
			}
		}

		public override void On_Create() {
			Color = 2448;
		}
	}
}

namespace SteamEngine.CompiledScripts.Dialogs {

	[Summary("The dialog that will display the treasure menu")]
	public class D_TreasureSpawn : CompiledGumpDef {

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			ImprovedDialog dialogHandler = new ImprovedDialog(this.GumpInstance);
			TreasureSpawn treasure = focus as TreasureSpawn;
			dialogHandler.CreateBackground(260);
			dialogHandler.SetLocation(70, 50);

			// Headline
			dialogHandler.AddTable(new GUTATable(1,0));
			dialogHandler.LastTable[0, 0] = TextFactory.CreateHeadline("Menu Pokladu");
			dialogHandler.MakeLastTableTransparent();

			// First table
			dialogHandler.LastTable.RowHeight = ButtonFactory.D_BUTTON_HEIGHT;
			dialogHandler.AddTable(new GUTATable(4, 100, 0, ButtonFactory.D_BUTTON_WIDTH));

			dialogHandler.LastTable[0, 0] = TextFactory.CreateText("Prachy (exp):");
			dialogHandler.LastTable[0, 1] = InputFactory.CreateInput(LeafComponentTypes.InputNumber, 1, Convert.ToString(treasure.prachy));
			dialogHandler.LastTable[0, 2] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonSend,10); // goto treasurePrachyHlp gump

			dialogHandler.LastTable[1, 0] = TextFactory.CreateText("Check:");
			dialogHandler.LastTable[1, 1] = InputFactory.CreateInput(LeafComponentTypes.InputNumber, 2, Convert.ToString(treasure.check));
			dialogHandler.LastTable[1, 2] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonSend,11); // goto treasureCheckHlp gump
			
			dialogHandler.LastTable[2, 0] = TextFactory.CreateText("Perioda:");
			dialogHandler.LastTable[2, 1] = InputFactory.CreateInput(LeafComponentTypes.InputNumber, 3, Convert.ToString(treasure.periode));
			dialogHandler.LastTable[2, 2] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonSend,12); // goto treasurePeriodicHlp gump
			
			dialogHandler.LastTable[3, 0] = TextFactory.CreateText("Lockpick:");
			dialogHandler.LastTable[3, 1] = InputFactory.CreateInput(LeafComponentTypes.InputNumber, 4, Convert.ToString(treasure.lockpick));
			dialogHandler.LastTable[3, 2] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonSend,13); // goto treasureLockpick gump

			// Second table
				// Head
			dialogHandler.AddTable(new GUTATable(1, ButtonFactory.D_BUTTON_WIDTH, 100, 0));
			//dialogHandler.LastTable[0, 1] = TextFactory.CreateText("info:");
			dialogHandler.LastTable[0, 2] = TextFactory.CreateText("pocet:");
				// Body
			dialogHandler.AddTable(new GUTATable(2, ButtonFactory.D_BUTTON_WIDTH, 100, 0));
			dialogHandler.LastTable[0, 0] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonTick,2); // goto treasureBounty gump
			dialogHandler.LastTable[0, 1] = TextFactory.CreateText("Poklad");
			dialogHandler.LastTable[0, 2] = TextFactory.CreateText("<val>");
			dialogHandler.LastTable[1, 0] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonTick,3); // goto treasureSpawns gump
			dialogHandler.LastTable[1, 1] = TextFactory.CreateText("Spawny");
			dialogHandler.LastTable[1, 2] = TextFactory.CreateText("<val>");

			dialogHandler.AddTable(new GUTATable(1,0));
			dialogHandler.LastTable[0, 0] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonOK, 1); //OK
			dialogHandler.LastTable[0, 0] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonCross, ButtonFactory.D_BUTTON_WIDTH, 0, 0); // Cancel

			//finish creating
			dialogHandler.WriteOut();

		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			TreasureSpawn treasure = (TreasureSpawn) gi.Focus;
			Player p = gi.Cont as Player;
			uint button = gr.pressedButton;
			if (button == 0) {			// cancel
				p.SysMessage("Nastaveni zustava nezmeneno.");
				return;
			} else if (button == 1) {		// OK
				if (gr.GetNumberResponse(1) > 0) {
					treasure.prachy = Convert.ToInt32(gr.GetNumberResponse(1));
				} else {
					p.RedMessage("Hodnota Prachy musi byt kladna !");
				}
				if (gr.GetNumberResponse(2) >= 0) {
					treasure.check = Convert.ToInt32(gr.GetNumberResponse(2));
				} else {
					p.RedMessage("Hodnota Check musi byt kladna !");
				}
				if (gr.GetNumberResponse(3) > 0) {
					treasure.periode = Convert.ToInt32(gr.GetNumberResponse(3));
				} else {
					p.RedMessage("Hodnota Perioda musi byt kladna !");
				}
				if (gr.GetNumberResponse(4) >= 0) {
					treasure.lockpick = Convert.ToInt32(gr.GetNumberResponse(4));
				} else {
					p.RedMessage("Hodnota lockpick musi byt kladna !");
				}
				return;

			// opening of setting Dialogs
			} else if (button == 2) { // treasureBounty gump
				p.SysMessage("Open treasureBounty gump...");
			} else if (button == 3) { // treasureSpawns gump
				p.SysMessage("Open treasureSpawns gump...");

			// Help dialogy
			} else if (button == 10) { //help prachy
				p.Dialog(SingletonScript<Dialogs.D_Display_Text>.Instance, "Help - Prachy", "Promìnná 'Prachy' udává poèet penìz generovaných stáøím pokladu");
			} else if (button == 11) { //help check

			} else if (button == 12) { //help perioda

			} else if (button == 13) { //help lockpick

			}
			treasure.Dialog(p, SingletonScript<Dialogs.D_TreasureSpawn>.Instance);
			//((Player)gi.Cont).Target(SingletonScript<Targ_GemBox>.Instance, gi.Focus);
		}
	}

	//[Summary("The dialog that will display Help")]
	//public class D_TreasureSpawn : CompiledGumpDef {
	//}
}