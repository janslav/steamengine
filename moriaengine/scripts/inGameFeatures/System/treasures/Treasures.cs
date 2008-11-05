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
	[Dialogs.ViewableClass]
	public partial class TreasureChest : Item
	{
		public override void On_DClick(AbstractCharacter ac) {
			Player p = ac as Player;
			if (p.IsGM) {
				this.Dialog(ac, SingletonScript<Dialogs.D_TreasureChest>.Instance);
			} else {
				p.SysMessage("otvirame poklad");
			}
		}

		public override void On_Create() {
			SetLastopen();
		}
		
		public void SetLastopen() {
			lastOpen=(long) Globals.TimeInSeconds;
		}

		[Summary("Assigns a time in seconds to the lastOpen attribute representing the serverTime 'secBack' seconds before.")]
		public void SetLastopen(int secBack) {
			lastOpen = (long) Globals.TimeInSeconds - secBack;
		}

		[Summary("Returns a time in seconds from the last treasure opening.")]
		public long GetLastopenTimeDifference() {
			return (long) Globals.TimeInSeconds - lastOpen;
		}

		[Summary("Returns a string containing easy to read info on how long it is from the last treasure opening.")]
		public string GetLastopenTimeDifference(bool isTimeString) {
			if (isTimeString) {
				long d,h,m,s;
				s = GetLastopenTimeDifference();
				d = s / 86400;
				s -= d * 86400;
				h = s / 3600;
				s-= h*3600;
				m = s / 60;
				s-= m*60;
				return d.ToString()+"d "+h.ToString()+"h "+m.ToString()+"min "+s.ToString()+"s";
			} else {
				return Convert.ToString(Globals.TimeInSeconds - lastOpen);
			}
		}
		
		[Summary("Adds new item into TreasureItem List")]
		public void AddTreasureItem(ItemDef item, int amount, int chance, int periodic) {
			TreasureItemEntry newItem = new TreasureItemEntry();
			newItem.itemID = item;
			newItem.amount = amount;
			newItem.chance = chance;
			newItem.periodic = periodic;
			treasureItems.Add(newItem);
		}

		[Summary("Overwrites attributes of one paticular TreasureItemEntry in the list of treasureItems")]
		public void OverwriteTreasureItem(int itemIndex, ItemDef item, int amount, int chance, int periodic) {
			treasureItems[itemIndex].itemID = item;
			treasureItems[itemIndex].amount = amount;
			treasureItems[itemIndex].chance = chance;
			treasureItems[itemIndex].periodic = periodic;
		}

		[Summary("Removes an item from the list of treasureItems")]
		public void RemoveTreasureItem(int itemIndex) {
			treasureItems.RemoveAt(itemIndex);
		}

		[Summary("Adds new item into treasureSpawns List")]
		public void AddTreasureSpawn(CharacterDef charDef, int amount) {
			TreasureSpawnEntry newSpawn = new TreasureSpawnEntry();
			newSpawn.charDef = charDef;
			newSpawn.amount = amount;
			treasureSpawns.Add(newSpawn);
		}

		[Summary("Overwrites attributes of one paticular TreasureSpawnEntry in the list of treasureSpawns")]
		public void OverwriteTreasureSpawn(int spawnIndex, CharacterDef charDef, int amount) {
			treasureSpawns[spawnIndex].charDef = charDef;
			treasureSpawns[spawnIndex].amount = amount;
		}

		[Summary("Removes an item from the list of treasureSpawns")]
		public void RemoveTreasureSpawn(int spawnIndex) {
			treasureSpawns.RemoveAt(spawnIndex);
		}
	}

	[SaveableClass][Dialogs.ViewableClass]
    public class TreasureItemEntry {
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
    }

	[SaveableClass][Dialogs.ViewableClass]
    public class TreasureSpawnEntry {
        [LoadingInitializer]
        public TreasureSpawnEntry() {
        }

        [SaveableData]
        public CharacterDef charDef;
        [SaveableData]
        public int amount;
    }
}

namespace SteamEngine.CompiledScripts.Dialogs {

	[Summary("The dialog that will display the treasure menu")]
	public class D_TreasureChest : CompiledGumpDef {

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			ImprovedDialog dialogHandler = new ImprovedDialog(this.GumpInstance);
			TreasureChest treasure = focus as TreasureChest;
			dialogHandler.CreateBackground(300);
			dialogHandler.SetLocation(70, 50);

			// Headline
			dialogHandler.AddTable(new GUTATable(1,0));
			dialogHandler.LastTable[0, 0] = TextFactory.CreateHeadline("Menu Pokladu");
			dialogHandler.MakeLastTableTransparent();

			// First table
			dialogHandler.LastTable.RowHeight = ButtonFactory.D_BUTTON_HEIGHT;
			dialogHandler.AddTable(new GUTATable(6, 100, 0, ButtonFactory.D_BUTTON_WIDTH));

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
			dialogHandler.LastTable[3, 2] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonSend,13); // goto treasureLockpickHlp gump

			dialogHandler.LastTable[4, 0] = TextFactory.CreateText("LastOpened:");
			dialogHandler.LastTable[4, 1] = InputFactory.CreateInput(LeafComponentTypes.InputNumber, 5, Convert.ToString(treasure.GetLastopenTimeDifference()));
			dialogHandler.LastTable[4, 2] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonSend,14); // goto treasureLastOpenHlp gump

			dialogHandler.LastTable[5, 1] = TextFactory.CreateText(treasure.GetLastopenTimeDifference(true));
			dialogHandler.MakeLastTableTransparent();
			
			// Second table
				// Head
			dialogHandler.AddTable(new GUTATable(1, ButtonFactory.D_BUTTON_WIDTH + 100, 0));
			dialogHandler.LastTable[0, 1] = TextFactory.CreateText(" pocet itemu:");
				// Body
			dialogHandler.AddTable(new GUTATable(2, ButtonFactory.D_BUTTON_WIDTH, 100, 0));
			dialogHandler.LastTable[0, 0] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonTick,2); // goto treasureBounty gump
			dialogHandler.LastTable[0, 1] = TextFactory.CreateText("Poklad");
			dialogHandler.LastTable[0, 2] = TextFactory.CreateText(" "+Convert.ToString(treasure.treasureItems.Count));
			dialogHandler.LastTable[1, 0] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonTick,3); // goto treasureSpawns gump
			dialogHandler.LastTable[1, 1] = TextFactory.CreateText("Spawny");
			dialogHandler.LastTable[1, 2] = TextFactory.CreateText(" "+Convert.ToString(treasure.treasureSpawns.Count));

			dialogHandler.MakeLastTableTransparent();

			dialogHandler.AddTable(new GUTATable(1,0));
			dialogHandler.LastTable[0, 0] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonOK, 1); //OK
			dialogHandler.LastTable[0, 0] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonCross, ButtonFactory.D_BUTTON_WIDTH, 0, 0); // Cancel

			//finish creating
			dialogHandler.WriteOut();

		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			TreasureChest treasure = (TreasureChest) gi.Focus;
			Player p = gi.Cont as Player;
			uint button = gr.pressedButton;
			switch(gr.pressedButton) {
				case 0:	// cancel
					p.SysMessage("Nastaveni zustava nezmeneno.");
					return;
				case 1:	// OK
					if (gr.GetNumberResponse(1) > 0) {
						treasure.prachy = (int) gr.GetNumberResponse(1);
					} else {
						p.RedMessage("Hodnota Prachy mus� b�t kladn� !");
					}
					if (gr.GetNumberResponse(2) >= 0) {
						treasure.check = (int) gr.GetNumberResponse(2);
					} else {
						p.RedMessage("Hodnota Check mus� b�t kladn� !");
					}
					if (gr.GetNumberResponse(3) > 0) {
						treasure.periode = (int) gr.GetNumberResponse(3);
					} else {
						p.RedMessage("Hodnota Perioda mus� b�t kladn� !");
					}
					if (gr.GetNumberResponse(4) >= 0) {
						treasure.lockpick = (int) gr.GetNumberResponse(4);
					} else {
						p.RedMessage("Hodnota lockpick mus� b�t kladn� !");
					}
					if (gr.GetNumberResponse(5) >= 0) {
						treasure.SetLastopen((int) gr.GetNumberResponse(5));
					} else {
						p.RedMessage("Hodnota lastOpened mus� b�t kladn� !");
					}
					return;

				// opening of setting Dialogs
				case 2:	// treasureBounty gump
					treasure.Dialog(p, SingletonScript<Dialogs.D_TreasureBounty>.Instance);
					p.SysMessage("Open treasureBounty gump...");
					return;
				case 3:	// treasureSpawns gump
					treasure.Dialog(p, SingletonScript<Dialogs.D_TreasureSpawns>.Instance);
					p.SysMessage("Open treasureSpawns gump...");
					return;

				// Help dialogy
				case 10:	//help prachy
					p.Dialog(SingletonScript<Dialogs.D_Display_Text>.Instance, "Help - Prachy", "Prom�nn� 'Prachy' ud�v� po�et pen�z generovan�ch st���m pokladu");
					break;
				case 11:	//help check
					p.Dialog(SingletonScript<Dialogs.D_Display_Text>.Instance, "Help - Check",	"Prom�nn� 'Check' je konstantn� po�et pen�z, kter� bude poka�d� do pokladu vlo�en.<br>"+
																								"Nem� vliv na hodnotu 'prachy'. Suma vypo��tan� z t�to slo�ky se prost� p�i�te k celkov�mu v�nosu.");
					break;
				case 12:	//help perioda
					p.Dialog(SingletonScript<Dialogs.D_Display_Text>.Instance, "Help - Prachy",	"Prom�nn� 'perioda' je �as, po kter�m se n�sob� periodic itemy. �as posledn�ho otev�en� pokladu je ulo�en "+
																								"a po dal��m otev�en� hr��i se rozd�l �as� pod�l� periodic konstantou. V�sledn� hodnota ud�v� po�et cykl�, "+
																								"kter� budou generovat itemy z pokladu s kladn�m attributem periodic.");
					break;
				case 13:	//help lockpick
					p.Dialog(SingletonScript<Dialogs.D_Display_Text>.Instance, "Help - Lockpick",	"Prom�nn� 'lockpick' ud�v� minim�ln� pot�ebn� skill pro odem�en� pokladu. Nulov� hodnota nech� poklad"+
																								"otev�en�, bez nutnosti pou��vat lockpicky.");
					break;
				case 14:	//help lastOpened
					p.Dialog(SingletonScript<Dialogs.D_Display_Text>.Instance, "Help - LastOpened", "Prom�nn� 'lastOpened' ud�v�, p�ed jakou dobou v sekund�ch byl naposledy poklad otev�en. " +
																									"P�epo�et na dny je mo�n� vid�t o ��dek n�.");
					break;
				default:
					p.RedMessage("Nestandardn� button ! Pi� scripter�m!");
					break;
			}
			treasure.Dialog(p, this);
		}
	}


	[Summary("The dialog that will display items generated it the trasure.")]
	public class D_TreasureBounty : CompiledGumpDef {

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			ImprovedDialog dialogHandler = new ImprovedDialog(this.GumpInstance);
			List<TreasureItemEntry> trItems = (focus as TreasureChest).treasureItems;
			int rowCount=trItems.Count;
			int i=0;
			//TreasureChest treasure = focus as TreasureChest;
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
			dialogHandler.LastTable[0, 3] = TextFactory.CreateText(" Periodic");
			
			// Second row
			dialogHandler.AddTable(new GUTATable(rowCount, 220, 70, 70, 80, ButtonFactory.D_BUTTON_WIDTH));
			foreach (TreasureItemEntry tie in trItems) {
				dialogHandler.LastTable[i, 0] = InputFactory.CreateInput(LeafComponentTypes.InputText, (i * 10) + 1, tie.itemID.PrettyDefname);
				dialogHandler.LastTable[i, 1] = InputFactory.CreateInput(LeafComponentTypes.InputNumber, (i * 10) + 2, tie.amount.ToString());
				dialogHandler.LastTable[i, 2] = InputFactory.CreateInput(LeafComponentTypes.InputNumber, (i * 10) + 3, tie.chance.ToString());
				dialogHandler.LastTable[i, 3] = InputFactory.CreateInput(LeafComponentTypes.InputNumber, (i * 10) + 4, tie.periodic.ToString());
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
			TreasureChest treasure = (TreasureChest) gi.Focus;
			switch (gr.pressedButton) {
				case 0:	//exit
					p.SysMessage("Nastaven� nezm�n�no.");
					break;
				default:
					bool err=false;
					string thisDef;
					ItemDef thisItem;
					int ignore=-1;					// variable ignore has assigned a safe value, which garantees that all values will be modified
					if (gr.pressedButton > 2) {	// not OK or add button - i.e. removing button
						ignore = Convert.ToInt32(gr.pressedButton) - 10;	// item with this index will be removed anyway, so we'll skip it's modifications
					}
					for(int i = 0; i < treasure.treasureItems.Count; i++) {
						if (i != ignore) {
							thisDef = gr.GetTextResponse(i * 10 + 1);
							thisItem = ItemDef.Get(thisDef) as ItemDef;
							if (thisItem == null) {
								p.RedMessage("'" + thisDef + "' neni platny defname!");
								err = true;
							} else if (gr.GetNumberResponse(i * 10 + 2) < 1) {
								p.RedMessage("Amount pro '" + thisDef + "' musi byt kladny!");
								err = true;
							} else if (gr.GetNumberResponse(i * 10 + 3) < 1) {
								p.RedMessage("Chance pro '" + thisDef + "' musi byt kladny!");
								err = true;
							} else if (gr.GetNumberResponse(i * 10 + 4) < 0) {
								p.RedMessage("Periodic pro '" + thisDef + "' nesmi byt zaporny!");
								err = true;
							} else {
								treasure.OverwriteTreasureItem(i, thisItem, (int) gr.GetNumberResponse(i * 10 + 2), (int) gr.GetNumberResponse(i * 10 + 3), (int) gr.GetNumberResponse(i * 10 + 4));
							}
						}
					}
					if (gr.pressedButton > 2) { // not OK or add button
						treasure.RemoveTreasureItem(Convert.ToInt32(gr.pressedButton) - 10);	//Gets the value correspondent to the position of an item in the arrayList of treasureItems
						treasure.Dialog(p, this);
						return;
					}
					if (gr.pressedButton == 2) { //add
						p.SysMessage("P�id�n defaultn� item.");
						treasure.AddTreasureItem((ItemDef) ItemDef.Get("i_bag"), 1, 100, 0);
						treasure.Dialog(p, this);
						return;
					}
					if (err) {
						treasure.Dialog(p, this);
						return;
					}
					p.SysMessage("Potvrzeno zad�n� hodnot.");
					break;
			}
			treasure.Dialog(p, SingletonScript<D_TreasureChest>.Instance);
		}
	}


	[Summary("The dialog that will display spawns guarding the trasure.")]
	public class D_TreasureSpawns : CompiledGumpDef {

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			ImprovedDialog dialogHandler = new ImprovedDialog(this.GumpInstance);
			List<TreasureSpawnEntry> trSpawns = ((TreasureChest) focus).treasureSpawns;
			int rowCount = trSpawns.Count;
			int i=0;
			//TreasureChest treasure = focus as TreasureChest;
			dialogHandler.CreateBackground(430);
			dialogHandler.SetLocation(70, 50);

			// Headline
			dialogHandler.AddTable(new GUTATable(1, 0));
			dialogHandler.LastTable[0, 0] = TextFactory.CreateHeadline("Seznam guard spawn�");
			dialogHandler.MakeLastTableTransparent();

			// First row
			dialogHandler.LastTable.RowHeight = ButtonFactory.D_BUTTON_HEIGHT;
			dialogHandler.AddTable(new GUTATable(1, 300, 70, ButtonFactory.D_BUTTON_WIDTH));
			dialogHandler.LastTable[0, 0] = TextFactory.CreateText(" Character Def");
			dialogHandler.LastTable[0, 1] = TextFactory.CreateText(" Amount");
			
			// Second row
			dialogHandler.AddTable(new GUTATable(rowCount, 300, 70, ButtonFactory.D_BUTTON_WIDTH));
			foreach (TreasureSpawnEntry tse in trSpawns) {
				dialogHandler.LastTable[i, 0] = InputFactory.CreateInput(LeafComponentTypes.InputText, (i * 10) + 1, tse.charDef.PrettyDefname);
				dialogHandler.LastTable[i, 1] = InputFactory.CreateInput(LeafComponentTypes.InputNumber, (i * 10) + 2, tse.amount.ToString());
				dialogHandler.LastTable[i, 2] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonCross, 10 + i);
				i++;
			}

			// last row
			dialogHandler.AddTable(new GUTATable(1, 270, 100, ButtonFactory.D_BUTTON_WIDTH));
			dialogHandler.LastTable[0, 0] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonOK, 1);	// OK
			dialogHandler.LastTable[0, 1] = TextFactory.CreateText(" Add spawn");
			dialogHandler.LastTable[0, 2] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonTick, 2);	// Add button

			dialogHandler.WriteOut();
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			Player p = gi.Cont as Player;
			TreasureChest treasure = (TreasureChest) gi.Focus;
			switch (gr.pressedButton) {
				case 0:	//Cancel
					p.SysMessage("Nastaven� nezm�n�no.");
					break;
				default:
					bool err=false;
					string thisDef;
					CharacterDef thisChar;
					int ignore=-1;					// variable ignore has assigned a safe value, which garantees that all values will be modified
					if (gr.pressedButton > 2) {	// not OK or Add button - i.e. removing button
						ignore = Convert.ToInt32(gr.pressedButton) - 10;	// item with this index will be removed anyway, so we'll skip it's modifications
					}
					for(int i = 0; i < treasure.treasureSpawns.Count; i++) {
						if (i != ignore) {
							thisDef = gr.GetTextResponse(i * 10 + 1);
							thisChar = CharacterDef.Get(thisDef) as CharacterDef;
							if (thisChar == null) {
								p.RedMessage("'" + thisDef + "' neni platny characterDef!");
								err = true;
							} else if (gr.GetNumberResponse(i * 10 + 2) < 1) {
								p.RedMessage("Amount pro characterDef '" + thisDef + "' musi byt kladny!");
								err = true;
							} else {
								treasure.OverwriteTreasureSpawn(i, thisChar, (int) gr.GetNumberResponse(i * 10 + 2));
							}
						}
					}
					if (gr.pressedButton > 2) { // not OK or Add button
						treasure.RemoveTreasureSpawn(Convert.ToInt32(gr.pressedButton) - 10);	//Gets the value correspondent to the position of an item in the arrayList of treasureItems
						treasure.Dialog(p, this);
						return;
					}
					if (gr.pressedButton == 2) { // pressed Add button
						p.SysMessage("P�id�n defaultn� spawn.");
						treasure.AddTreasureSpawn((CharacterDef) CharacterDef.Get("c_ostard_zostrich"), 1);
						treasure.Dialog(p, this);
						return;
					}
					if (err) {
						treasure.Dialog(p, this);
						return;
					}

					p.SysMessage("Potvrzeno zad�n� hodnot.");
					break;
			}
			treasure.Dialog(p, SingletonScript<D_TreasureChest>.Instance);
		}
	}
}