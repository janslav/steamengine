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
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {
	[Dialogs.ViewableClass][HasSavedMembers]
	public partial class TreasureSpawn : Item
	{
		[SavedMember]
		private static ArrayList treasureItems = new ArrayList();

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

		[Summary("Returns a copy of the treasureItemEntry ArrayList")]
		public ArrayList TreasureItems {
			get {
				return new ArrayList(treasureItems);
			}
		}
		
		[Summary("Returns a copy of the TreasureItemEntry ArrayList")]
		public void AddTreasureItem(ItemDef item, int amount, int chance, int periodic) {
			TreasureItemEntry newItem = new TreasureItemEntry();
			newItem.itemID = item;
			newItem.amount = amount;
			newItem.chance = chance;
			newItem.periodic = periodic;
			treasureItems.Add(newItem);
		}

		public void AddTreasureItem(int itemIndex, ItemDef item, int amount, int chance, int periodic) {
			TreasureItemEntry newItem = new TreasureItemEntry();
			newItem.itemID = item;
			newItem.amount = amount;
			newItem.chance = chance;
			newItem.periodic = periodic;
			treasureItems.Insert(itemIndex, newItem);
		}

		[Summary("Removes item from athe array of TreasureItems")]
		public void RemoveTreasureItem(int arrayPos) {
			treasureItems.RemoveAt(arrayPos);
		}
	}
	
	[SaveableClass][Dialogs.ViewableClass]
    public class TreasureItemEntry : ITreasureItemsEntry {
        [LoadingInitializer]
        public TreasureItemEntry() {
        }

        [SaveableData]
        public ItemDef itemID;
        [SaveableData]
        public int amount;
        [SaveableData]
        public int chance;
        [SaveableData]
        public int periodic;

        ItemDef ITreasureItemsEntry.itemID {
            get { return itemID; }
        }
        int ITreasureItemsEntry.amount {
            get { return amount; }
        }
        int ITreasureItemsEntry.chance {
            get { return chance; }
        }
        int ITreasureItemsEntry.periodic {
            get { return periodic; }
        }
    }

	public interface ITreasureItemsEntry {
		ItemDef itemID { get; }
        int amount { get; }
        int chance { get; }
        int periodic { get; }
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
			dialogHandler.LastTable[0, 2] = TextFactory.CreateText("pocet itemu:");
				// Body
			dialogHandler.AddTable(new GUTATable(2, ButtonFactory.D_BUTTON_WIDTH, 100, 0));
			dialogHandler.LastTable[0, 0] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonTick,2); // goto treasureBounty gump
			dialogHandler.LastTable[0, 1] = TextFactory.CreateText("Poklad");
			dialogHandler.LastTable[0, 2] = TextFactory.CreateText(Convert.ToString(treasure.TreasureItems.Count));
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
			switch(gr.pressedButton) {
				case 0:	// cancel
					p.SysMessage("Nastaveni zustava nezmeneno.");
					return;
				case 1:	// OK
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
				case 2:	// treasureBounty gump
					treasure.Dialog(p, SingletonScript<Dialogs.D_TreasureBounty>.Instance);
					p.SysMessage("Open treasureBounty gump...");
					return;
				case 3:	// treasureSpawns gump
					p.SysMessage("Open treasureSpawns gump...");
					return;

				// Help dialogy
				case 10:	//help prachy
					p.Dialog(SingletonScript<Dialogs.D_Display_Text>.Instance, "Help - Prachy", "Promìnná 'Prachy' udává poèet penìz generovaných stáøím pokladu");
					break;
				case 11:	//help check
					p.Dialog(SingletonScript<Dialogs.D_Display_Text>.Instance, "Help - Check",	"Promìnná 'Check' je konstantní poèet penìz, který bude pokaždé do pokladu vložen.<br>"+
																								"Nemá vliv na hodnotu 'prachy'. Suma vypoèítaná z této složky se prostì pøiète k celkovému výnosu.");
					break;
				case 12:	//help perioda
					p.Dialog(SingletonScript<Dialogs.D_Display_Text>.Instance, "Help - Prachy",	"Promìnná 'perioda' je èas, po kterém se násobí periodic itemy. Èas posledního otevøení pokladu je uložen "+
																								"a po dalším otevøení hráèi se rozdíl èasù podìlí periodic konstantou. Výsledná hodnota udává poèet cyklù, "+
																								"které budou generovat itemy z pokladu s kladným attributem periodic.");
					break;
				case 13:	//help lockpick
					p.Dialog(SingletonScript<Dialogs.D_Display_Text>.Instance, "Help - Prachy",	"Promìnná 'lockpick' udává minimální potøebný skill pro odemèení pokladu. Nulová hodnota nechá poklad"+
																								"otevøený, bez nutnosti používat lockpicky.");
					break;

				default:
					p.RedMessage("Nestandardní button ! Piš scripterùm!");
					break;
			}
			treasure.Dialog(p, SingletonScript<Dialogs.D_TreasureSpawn>.Instance);
			//((Player)gi.Cont).Target(SingletonScript<Targ_GemBox>.Instance, gi.Focus);
		}
	}


	[Summary("The dialog that will display items generated it the trasure.")]
	public class D_TreasureBounty : CompiledGumpDef {

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			ImprovedDialog dialogHandler = new ImprovedDialog(this.GumpInstance);
			ArrayList trItems = (focus as TreasureSpawn).TreasureItems;
			int rowCount=trItems.Count;
			int i=0;
			//TreasureSpawn treasure = focus as TreasureSpawn;
			dialogHandler.CreateBackground(500);
			dialogHandler.SetLocation(70, 50);

			// Headline
			dialogHandler.AddTable(new GUTATable(1, 0));
			dialogHandler.LastTable[0, 0] = TextFactory.CreateHeadline("Obsah Pokladu");
			dialogHandler.MakeLastTableTransparent();

			// First row
			dialogHandler.LastTable.RowHeight = ButtonFactory.D_BUTTON_HEIGHT;
			dialogHandler.AddTable(new GUTATable(1, 220, 70, 70, 80, ButtonFactory.D_BUTTON_WIDTH));
			dialogHandler.LastTable[0, 0] = TextFactory.CreateText(" Itemdef");
			dialogHandler.LastTable[0, 1] = TextFactory.CreateText(" Amount");
			dialogHandler.LastTable[0, 2] = TextFactory.CreateText(" Chance");
			dialogHandler.LastTable[0, 3] = TextFactory.CreateText(" Period");
			
			// Second row
			dialogHandler.AddTable(new GUTATable(rowCount, 220, 70, 70, 80, ButtonFactory.D_BUTTON_WIDTH));
			foreach (TreasureItemEntry tie in trItems) {
				dialogHandler.LastTable[i, 0] = InputFactory.CreateInput(LeafComponentTypes.InputText, (i * 10) + 1, "Some ItemID");
				dialogHandler.LastTable[i, 1] = InputFactory.CreateInput(LeafComponentTypes.InputText, (i * 10) + 2, "Count");
				dialogHandler.LastTable[i, 2] = InputFactory.CreateInput(LeafComponentTypes.InputText, (i * 10) + 3, "Chance");
				dialogHandler.LastTable[i, 3] = InputFactory.CreateInput(LeafComponentTypes.InputText, (i * 10) + 4, "Period");
				dialogHandler.LastTable[i, 4] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonCross, 10 + i);
				i++;
			}

			// last row
			dialogHandler.AddTable(new GUTATable(1, 360, 80, ButtonFactory.D_BUTTON_WIDTH));
			dialogHandler.LastTable[0, 0] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonOK, 1);	// OK
			dialogHandler.LastTable[0, 1] = TextFactory.CreateText(" Add item");
			dialogHandler.LastTable[0, 2] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonTick, 2);	// Add button

			dialogHandler.WriteOut();
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			Player p = gi.Cont as Player;
			TreasureSpawn treasure = (TreasureSpawn) gi.Focus;
			switch (gr.pressedButton) {
				case 0:	//exit
					p.SysMessage("Storno ...");
					break;
				case 1:	//OK
					p.SysMessage("Potvrzuju zadany hodnoty...");
					break;
				case 2:	//Add item
					p.SysMessage("pridat defaultni item");
					treasure.AddTreasureItem((ItemDef) ItemDef.Get("i_bag"), 1, 100, 0);
					treasure.Dialog(p, SingletonScript<D_TreasureBounty>.Instance);
					return;
				default:
					treasure.RemoveTreasureItem(Convert.ToInt32(gr.pressedButton) - 10);	//Gets the value correspondent to the position of an item in the arrayList of treasureItems
					treasure.Dialog(p, SingletonScript<D_TreasureBounty>.Instance);
					return;
			}
			treasure.Dialog(p, SingletonScript<D_TreasureSpawn>.Instance);
		}
	}
	
}