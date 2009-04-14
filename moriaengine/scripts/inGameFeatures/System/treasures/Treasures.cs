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



/* TO DO !:
 * ? Exceptions in getters of TreasureChest's private fields ? // dunno whether it should be there or not
 * System.Collections.ObjectModel.ReadOnlyCollection for Spawn&ItemEntry - I guess I'll pend this one... at least for now
 * adding Gold -> goldBags and goldBoxes are needed to be done first...
 * Clean unnecessary functions ... are those functions in english ? or is there a word like 'methode' or sth ... grh..
 * Add lockpicking
 * ?Add chest hiding? and probably re-seting after timer (I'll think this over, there may be another aproach..)
 * Add monster spawning and guarding system
 */


namespace SteamEngine.CompiledScripts {
	[Dialogs.ViewableClass]
	public partial class TreasureChest : Container {
		private static CharacterDef defaultTreasureSpawn = null;
		private static ItemDef defaultTreasureItem = null;

		public CharacterDef DefaultTreasureSpawn {
			get {
				if (defaultTreasureSpawn == null) {
					defaultTreasureSpawn = (CharacterDef) CharacterDef.Get("c_ostard_zostrich");
				}
				return defaultTreasureSpawn;
			}
		}

		public ItemDef DefaultTreasureItem {
			get {
				if (defaultTreasureItem == null) {
					defaultTreasureItem = (ItemDef) ItemDef.Get("i_bag");
				}
				return defaultTreasureItem;
			}
		}

		public override void On_DClick(AbstractCharacter ac) {
			Player p = ac as Player;
			if (p.IsGM) {
				this.Dialog(ac, SingletonScript<Dialogs.D_TreasureChest>.Instance);
			} else {
				p.SysMessage("otvirame poklad");
				EnsureListItemEntry();
				EnsureListSpawnEntry();
				if (GetLastopenTimeDifference() / cycleTime >= 1) {	//it's time to generate another treasure;
					/*if (isLocked) { // some isLocked condition and locked container condidito and blah blah ...
					 * ac.SysMessage("");
					}
					if (!wasSpawned) {
					 * ac.SysMessage("");
					 * SpawnMonsters();
					}*/
					int per;
					//removeGuts of the container so that in the treasure will be just those items I really want to..
					foreach (TreasureItemEntry tie in treasureItems) {
						if (tie.periodic > 0) {
							per = (int) (GetLastopenTimeDifference() / cycleTime) * tie.periodic;
							p.SysMessage("Periode is counted as: " + per.ToString());

						} else {
							per = 1;
						}
						for (int i = 0; i < tie.amount; i++) {
							for (int j = 0; j < per; j++) {	//periodes
								if (tie.chance > 0) {	// zero chance equals 100% chance...
									if (!(tie.chance > Globals.dice.Next(0, 99))) {	//chance failed
										continue;
									}
								}
								tie.itemID.Create(this);
							}
						}
					}
					SetLastopen();	// we set the lastOpen field at the very time in which we generate the treasure.
					OpenTo(ac);
				} else {
					OpenTo(ac);	//Too early, there is no reward to give yet.
				}
			}
		}

		public override void On_Create() {
			SetLastopen();
		}

		public void SetLastopen() {
			lastOpen = (long) Globals.TimeInSeconds;
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
				long d, h, m, s;
				s = GetLastopenTimeDifference();
				d = s / 86400;
				s -= d * 86400;
				h = s / 3600;
				s -= h * 3600;
				m = s / 60;
				s -= m * 60;
				return d.ToString() + "d " + h.ToString() + "h " + m.ToString() + "min " + s.ToString() + "s";
			} else {
				return Convert.ToString(Globals.TimeInSeconds - lastOpen);
			}
		}

		public List<TreasureItemEntry> TreasureItems {
			get {
				EnsureListItemEntry();
				return treasureItems;
			}
		}

		public List<TreasureSpawnEntry> TreasureSpawns {
			get {
				EnsureListSpawnEntry();
				return treasureSpawns;
			}
		}

		public int MoneyCoefficient {
			get {
				return moneyCoefficient;
			}
			set {
				if (value > 0) {
					moneyCoefficient = value;
				} else {
					// exception
				}
			}
		}

		public int Check {
			get {
				return check;
			}
			set {
				if (value > 0) {
					check = value;
				} else {
					// exception
				}
			}
		}

		public int CycleTime {
			get {
				return cycleTime;
			}
			set {
				if (value > 0) {
				} else {
					//exception
				}
			}
		}

		public int Lockpick {
			get {
				return lockpick;
			}
			set {
				if (value > 0) {
					lockpick = value;
				} else {
					// exception
				}
			}
		}

		public void EnsureListItemEntry() {
			if (treasureItems == null) {
				treasureItems = new List<TreasureItemEntry>();
			}
		}

		public void EnsureListSpawnEntry() {
			if (treasureSpawns == null) {
				treasureSpawns = new List<TreasureSpawnEntry>();
			}
		}

		[Summary("Adds new item into TreasureItem List")]
		public void AddTreasureItem(ItemDef item, int amount, int chance, int periodic) {
			EnsureListItemEntry();
			TreasureItemEntry newItem = new TreasureItemEntry();
			newItem.itemID = item;
			newItem.amount = amount;
			newItem.chance = chance;
			newItem.periodic = periodic;
			treasureItems.Add(newItem);
		}

		[Summary("Overwrites attributes of one paticular TreasureItemEntry in the list of treasureItems")]
		public void OverwriteTreasureItem(int itemIndex, ItemDef item, int amount, int chance, int periodic) {
			if (treasureItems.Count <= itemIndex) {
				// chyba !
				return;
			}
			treasureItems[itemIndex].itemID = item;
			treasureItems[itemIndex].amount = amount;
			treasureItems[itemIndex].chance = chance;
			treasureItems[itemIndex].periodic = periodic;
		}

		[Summary("Removes an item from the list of treasureItems")]
		public void RemoveTreasureItem(int itemIndex) {
			if (treasureItems.Count <= itemIndex) {
				// chyba !
				return;
			}
			treasureItems.RemoveAt(itemIndex);
		}

		[Summary("Adds new item into treasureSpawns List")]
		public void AddTreasureSpawn(CharacterDef charDef, int amount) {
			EnsureListSpawnEntry();
			TreasureSpawnEntry newSpawn = new TreasureSpawnEntry();
			newSpawn.charDef = charDef;
			newSpawn.amount = amount;
			treasureSpawns.Add(newSpawn);
		}

		[Summary("Overwrites attributes of one paticular TreasureSpawnEntry in the list of treasureSpawns")]
		public void OverwriteTreasureSpawn(int spawnIndex, CharacterDef charDef, int amount) {
			if (treasureSpawns.Count <= spawnIndex) {
				// chyba !
				return;
			}
			treasureSpawns[spawnIndex].charDef = charDef;
			treasureSpawns[spawnIndex].amount = amount;
		}

		[Summary("Removes an item from the list of treasureSpawns")]
		public void RemoveTreasureSpawn(int spawnIndex) {
			if (treasureSpawns.Count <= spawnIndex) {
				// chyba !
				return;
			}
			treasureSpawns.RemoveAt(spawnIndex);
		}
	}

	[SaveableClass]
	[Dialogs.ViewableClass]
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

	[SaveableClass]
	[Dialogs.ViewableClass]
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
			treasure.EnsureListItemEntry();
			treasure.EnsureListSpawnEntry();
			dialogHandler.CreateBackground(300);
			dialogHandler.SetLocation(70, 50);

			// Headline
			dialogHandler.AddTable(new GUTATable(1, 0));
			dialogHandler.LastTable[0, 0] = GUTAText.Builder.TextHeadline("Menu Pokladu").Build();
			dialogHandler.MakeLastTableTransparent();

			// First table
			dialogHandler.LastTable.RowHeight = ButtonMetrics.D_BUTTON_HEIGHT;
			dialogHandler.AddTable(new GUTATable(6, 100, 0, ButtonMetrics.D_BUTTON_WIDTH));

			dialogHandler.LastTable[0, 0] = GUTAText.Builder.TextLabel("Pen�ze (exp):").Build();
			dialogHandler.LastTable[0, 1] = GUTAInput.Builder.Type(LeafComponentTypes.InputNumber).Id(1).Text(Convert.ToString(treasure.MoneyCoefficient)).Build();
			dialogHandler.LastTable[0, 2] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonSend).Id(10).Build(); // goto treasuremoneyCoefficientHlp gump

			dialogHandler.LastTable[1, 0] = GUTAText.Builder.TextLabel("�ek:").Build();
			dialogHandler.LastTable[1, 1] = GUTAInput.Builder.Type(LeafComponentTypes.InputNumber).Id(2).Text(Convert.ToString(treasure.Check)).Build();
			dialogHandler.LastTable[1, 2] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonSend).Id(11).Build(); // goto treasureCheckHlp gump

			dialogHandler.LastTable[2, 0] = GUTAText.Builder.TextLabel("Perioda:").Build();
			dialogHandler.LastTable[2, 1] = GUTAInput.Builder.Type(LeafComponentTypes.InputNumber).Id(3).Text(Convert.ToString(treasure.CycleTime)).Build();
			dialogHandler.LastTable[2, 2] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonSend).Id(12).Build(); // goto treasureperiodicHlp gump

			dialogHandler.LastTable[3, 0] = GUTAText.Builder.TextLabel("Lockpick:").Build();
			dialogHandler.LastTable[3, 1] = GUTAInput.Builder.Type(LeafComponentTypes.InputNumber).Id(4).Text(Convert.ToString(treasure.Lockpick)).Build();
			dialogHandler.LastTable[3, 2] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonSend).Id(13).Build(); // goto treasureLockpickHlp gump

			dialogHandler.LastTable[4, 0] = GUTAText.Builder.TextLabel("LastOpened:").Build();
			dialogHandler.LastTable[4, 1] = GUTAInput.Builder.Type(LeafComponentTypes.InputNumber).Id(5).Text(Convert.ToString(treasure.GetLastopenTimeDifference())).Build();
			dialogHandler.LastTable[4, 2] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonSend).Id(14).Build(); // goto treasureLastOpenHlp gump

			dialogHandler.LastTable[5, 1] = GUTAText.Builder.TextLabel(treasure.GetLastopenTimeDifference(true)).Build();
			dialogHandler.MakeLastTableTransparent();

			// Second table
			// Head
			dialogHandler.AddTable(new GUTATable(1, ButtonMetrics.D_BUTTON_WIDTH + 100, 0));
			dialogHandler.LastTable[0, 1] = GUTAText.Builder.TextLabel(" pocet itemu:").Build();
			// Body
			dialogHandler.AddTable(new GUTATable(2, ButtonMetrics.D_BUTTON_WIDTH, 100, 0));
			dialogHandler.LastTable[0, 0] = GUTAButton.Builder.Id(2).Build(); // goto treasureBounty gump
			dialogHandler.LastTable[0, 1] = GUTAText.Builder.TextLabel("Poklad").Build();
			dialogHandler.LastTable[0, 2] = GUTAText.Builder.Text(" " + Convert.ToString(treasure.TreasureItems.Count)).Build();
			dialogHandler.LastTable[1, 0] = GUTAButton.Builder.Id(3).Build(); // goto treasureSpawns gump
			dialogHandler.LastTable[1, 1] = GUTAText.Builder.TextLabel("Spawny").Build();
			dialogHandler.LastTable[1, 2] = GUTAText.Builder.Text(" " + Convert.ToString(treasure.TreasureSpawns.Count)).Build();

			dialogHandler.MakeLastTableTransparent();

			dialogHandler.AddTable(new GUTATable(1, 0));
			dialogHandler.LastTable[0, 0] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonOK).Id(1).Build(); //OK
			dialogHandler.LastTable[0, 0] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonCross).XPos(ButtonMetrics.D_BUTTON_WIDTH).Id(0).Build(); // Cancel

			//finish creating
			dialogHandler.WriteOut();

		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			TreasureChest treasure = (TreasureChest) gi.Focus;
			Player p = gi.Cont as Player;
			int button = gr.pressedButton;
			switch (gr.pressedButton) {
				case 0:	// cancel
					p.SysMessage("Nastaveni zustava nezmeneno.");
					return;
				case 1:	// OK
					if (gr.GetNumberResponse(1) > 0) {
						treasure.MoneyCoefficient = (int) gr.GetNumberResponse(1);
					} else {
						p.RedMessage("Hodnota moneyCoefficient mus� b�t kladn� !");
					}
					if (gr.GetNumberResponse(2) >= 0) {
						treasure.Check = (int) gr.GetNumberResponse(2);
					} else {
						p.RedMessage("Hodnota Check mus� b�t kladn� !");
					}
					if (gr.GetNumberResponse(3) > 0) {
						treasure.CycleTime = (int) gr.GetNumberResponse(3);
					} else {
						p.RedMessage("Hodnota Perioda mus� b�t kladn� !");
					}
					if (gr.GetNumberResponse(4) >= 0) {
						treasure.Lockpick = (int) gr.GetNumberResponse(4);
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
					treasure.Dialog(p, SingletonScript<D_TreasureBounty>.Instance);
					p.SysMessage("Open treasureBounty gump...");
					return;
				case 3:	// treasureSpawns gump
					treasure.Dialog(p, SingletonScript<D_TreasureSpawns>.Instance);
					p.SysMessage("Open treasureSpawns gump...");
					return;

				// Help dialogy
				case 10:	//help moneyCoefficient
					p.Dialog(SingletonScript<D_Display_Text>.Instance, "Help - Pen�ze", "Prom�nn� 'Pen�ze' ud�v� po�et pen�z generovan�ch st���m pokladu");
					break;
				case 11:	//help check
					p.Dialog(SingletonScript<D_Display_Text>.Instance, "Help - �ek", "Prom�nn� '�ek' je konstantn� po�et pen�z, kter� bude poka�d� do pokladu vlo�en.<br>" +
																								"Nem� vliv na hodnotu 'moneyCoefficient'. Suma vypo��tan� z t�to slo�ky se prost� p�i�te k celkov�mu v�nosu.");
					break;
				case 12:	//help perioda
					p.Dialog(SingletonScript<D_Display_Text>.Instance, "Help - Perioda", "Prom�nn� 'perioda' je �as v sekund�ch, po kter�m se n�sob� periodic itemy. �as posledn�ho otev�en� pokladu " +
																								"je ulo�en a po dal��m otev�en� hr��i se rozd�l �as� pod�l� periodic konstantou. V�sledn� hodnota ud�v� po�et cykl�, " +
																								"kter� budou generovat itemy z pokladu s kladn�m attributem periodic.<br>" +
																								" vzorec pro periodic itemy:<br><\t> (lastOpened/Perioda)*itemPeriodic");
					break;
				case 13:	//help lockpick
					p.Dialog(SingletonScript<D_Display_Text>.Instance, "Help - Lockpick", "Prom�nn� 'lockpick' ud�v� minim�ln� pot�ebn� skill pro odem�en� pokladu. Nulov� hodnota nech� poklad" +
																								"otev�en�, bez nutnosti pou��vat lockpicky.");
					break;
				case 14:	//help lastOpened
					p.Dialog(SingletonScript<D_Display_Text>.Instance, "Help - LastOpened", "Prom�nn� 'lastOpened' ud�v�, p�ed jakou dobou v sekund�ch byl naposledy poklad otev�en. " +
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
			TreasureChest treasure = focus as TreasureChest;
			List<TreasureItemEntry> trItems = (focus as TreasureChest).TreasureItems;
			int rowCount = treasure.TreasureItems.Count;
			int i = 0;
			//TreasureChest treasure = focus as TreasureChest;
			dialogHandler.CreateBackground(500);
			dialogHandler.SetLocation(70, 50);

			// Headline
			dialogHandler.AddTable(new GUTATable(1, 0));
			dialogHandler.LastTable[0, 0] = GUTAText.Builder.TextHeadline("Obsah Pokladu").Build();
			dialogHandler.MakeLastTableTransparent();

			// First row
			dialogHandler.LastTable.RowHeight = ButtonMetrics.D_BUTTON_HEIGHT;
			dialogHandler.AddTable(new GUTATable(1, 220, 70, 70, 80, ButtonMetrics.D_BUTTON_WIDTH));
			dialogHandler.LastTable[0, 0] = GUTAText.Builder.TextLabel(" Itemdef").Build();
			dialogHandler.LastTable[0, 1] = GUTAText.Builder.TextLabel(" Amount").Build();
			dialogHandler.LastTable[0, 2] = GUTAText.Builder.TextLabel(" Chance").Build();
			dialogHandler.LastTable[0, 3] = GUTAText.Builder.TextLabel(" Periodic").Build();

			// Second row
			dialogHandler.AddTable(new GUTATable(rowCount, 220, 70, 70, 80, ButtonMetrics.D_BUTTON_WIDTH));
			foreach (TreasureItemEntry tie in trItems) {
				dialogHandler.LastTable[i, 0] = GUTAInput.Builder.Id((i * 10) + 1).Text(tie.itemID.PrettyDefname).Build();
				dialogHandler.LastTable[i, 1] = GUTAInput.Builder.Type(LeafComponentTypes.InputNumber).Id((i * 10) + 2).Text(tie.amount.ToString()).Build();
				dialogHandler.LastTable[i, 2] = GUTAInput.Builder.Type(LeafComponentTypes.InputNumber).Id((i * 10) + 3).Text(tie.chance.ToString()).Build();
				dialogHandler.LastTable[i, 3] = GUTAInput.Builder.Type(LeafComponentTypes.InputNumber).Id((i * 10) + 4).Text(tie.periodic.ToString()).Build();
				dialogHandler.LastTable[i, 4] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonCross).Id(10 + i).Build();
				i++;
			}

			// last row
			dialogHandler.AddTable(new GUTATable(1, 360, 80, ButtonMetrics.D_BUTTON_WIDTH));
			dialogHandler.LastTable[0, 0] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonOK).Id(1).Build();	// OK
			dialogHandler.LastTable[0, 1] = GUTAText.Builder.TextLabel(" Add item").Build();
			dialogHandler.LastTable[0, 2] = GUTAButton.Builder.Id(2).Build();	// Add button

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
					bool err = false;
					string thisDef;
					ItemDef thisItem;
					int ignore = -1;					// variable ignore has assigned a safe value, which garantees that all values will be modified
					if (gr.pressedButton > 2) {	// not OK or add button - i.e. removing button
						ignore = Convert.ToInt32(gr.pressedButton) - 10;	// item with this index will be removed anyway, so we'll skip it's modifications
					}
					for (int i = 0; i < treasure.TreasureItems.Count; i++) {
						if (i != ignore) {
							thisDef = gr.GetTextResponse(i * 10 + 1);
							thisItem = ItemDef.Get(thisDef) as ItemDef;
							if (thisItem == null) {
								p.RedMessage("'" + thisDef + "' neni platny defname!");
								err = true;
							} else if (gr.GetNumberResponse(i * 10 + 2) < 1) {
								p.RedMessage("amount pro '" + thisDef + "' musi byt kladny!");
								err = true;
							} else if (gr.GetNumberResponse(i * 10 + 3) < 1) {
								p.RedMessage("chance pro '" + thisDef + "' musi byt kladny!");
								err = true;
							} else if (gr.GetNumberResponse(i * 10 + 4) < 0) {
								p.RedMessage("periodic pro '" + thisDef + "' nesmi byt zaporny!");
								err = true;
							} else {
								treasure.OverwriteTreasureItem(i, thisItem, (int) gr.GetNumberResponse(i * 10 + 2), (int) gr.GetNumberResponse(i * 10 + 3), (int) gr.GetNumberResponse(i * 10 + 4));
							}
						}
					}
					if (gr.pressedButton > 2) { // not OK or add button
						treasure.RemoveTreasureItem(Convert.ToInt32(gr.pressedButton) - 10);	//Gets the value correspondent to the position of an item in the List of treasureItems
						treasure.Dialog(p, this);
						return;
					}
					if (gr.pressedButton == 2) { //add
						p.SysMessage("P�id�n defaultn� item.");
						treasure.AddTreasureItem(treasure.DefaultTreasureItem, 1, 100, 0);
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
			List<TreasureSpawnEntry> trSpawns = ((TreasureChest) focus).TreasureSpawns;
			TreasureChest treasure = focus as TreasureChest;
			int rowCount = treasure.TreasureSpawns.Count;
			int i = 0;
			//TreasureChest treasure = focus as TreasureChest;
			dialogHandler.CreateBackground(430);
			dialogHandler.SetLocation(70, 50);

			// Headline
			dialogHandler.AddTable(new GUTATable(1, 0));
			dialogHandler.LastTable[0, 0] = GUTAText.Builder.TextHeadline("Seznam guard spawn�").Build();
			dialogHandler.MakeLastTableTransparent();

			// First row
			dialogHandler.LastTable.RowHeight = ButtonMetrics.D_BUTTON_HEIGHT;
			dialogHandler.AddTable(new GUTATable(1, 300, 70, ButtonMetrics.D_BUTTON_WIDTH));
			dialogHandler.LastTable[0, 0] = GUTAText.Builder.TextLabel(" Character Def").Build();
			dialogHandler.LastTable[0, 1] = GUTAText.Builder.TextLabel(" Amount").Build();

			// Second row
			dialogHandler.AddTable(new GUTATable(rowCount, 300, 70, ButtonMetrics.D_BUTTON_WIDTH));
			foreach (TreasureSpawnEntry tse in trSpawns) {
				dialogHandler.LastTable[i, 0] = GUTAInput.Builder.Id((i * 10) + 1).Text(tse.charDef.PrettyDefname).Build();
				dialogHandler.LastTable[i, 1] = GUTAInput.Builder.Type(LeafComponentTypes.InputNumber).Id((i * 10) + 2).Text(tse.amount.ToString()).Build();
				dialogHandler.LastTable[i, 2] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonCross).Id(10 + i).Build();
				i++;
			}

			// last row
			dialogHandler.AddTable(new GUTATable(1, 270, 100, ButtonMetrics.D_BUTTON_WIDTH));
			dialogHandler.LastTable[0, 0] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonOK).Id(1).Build();	// OK
			dialogHandler.LastTable[0, 1] = GUTAText.Builder.TextLabel(" Add spawn").Build();
			dialogHandler.LastTable[0, 2] = GUTAButton.Builder.Id(2).Build();	// Add button

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
					bool err = false;
					string thisDef;
					CharacterDef thisChar;
					int ignore = -1;					// a safe value was assigned to the variable ignore, which garantees that all values will be modified
					if (gr.pressedButton > 2) {	// not OK or Add button - i.e. removing button
						ignore = Convert.ToInt32(gr.pressedButton) - 10;	// item with this index will be removed anyway, so we'll skip it's modifications
					}
					for (int i = 0; i < treasure.TreasureSpawns.Count; i++) {
						if (i != ignore) {
							thisDef = gr.GetTextResponse(i * 10 + 1);
							thisChar = CharacterDef.Get(thisDef) as CharacterDef;
							if (thisChar == null) {
								p.RedMessage("'" + thisDef + "' neni platny characterDef!");
								err = true;
							} else if (gr.GetNumberResponse(i * 10 + 2) < 1) {
								p.RedMessage("amount pro characterDef '" + thisDef + "' musi byt kladny!");
								err = true;
							} else {
								treasure.OverwriteTreasureSpawn(i, thisChar, (int) gr.GetNumberResponse(i * 10 + 2));
							}
						}
					}
					if (gr.pressedButton > 2) { // not OK or Add button
						treasure.RemoveTreasureSpawn(Convert.ToInt32(gr.pressedButton) - 10);	//Gets the value correspondent to the position of an item in the List of treasureItems
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