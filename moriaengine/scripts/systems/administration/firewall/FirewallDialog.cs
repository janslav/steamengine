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
using System.Net;
using SteamEngine;
using System.Collections;
using SteamEngine.CompiledScripts;
using SteamEngine.LScript;

namespace SteamEngine.CompiledScripts.Dialogs {

	public class D_BlockedIP : CompiledGump {		
		private static TagKey entriesTK = TagKey.Get("ipentries");

		private static D_BlockedIP instance;
		public static D_BlockedIP Instance {
			get {
				return instance;
			}
		}

		public D_BlockedIP() {
			instance = this;
		}

		public override void Construct(Thing focus, AbstractCharacter sendTo, object[] args) {
			int IPwidth = 120; //velikost okna pro IP adresu 
			int duvod = 350;
			int kdo = 110;
			int width = IPwidth * 2 + duvod + kdo + 2 * ButtonFactory.D_BUTTON_WIDTH;

			ImprovedDialog dialogHandler = new ImprovedDialog(this.GumpInstance);

			SortingCriteria sort = (SortingCriteria) Convert.ToInt32(args[0]);
			ArrayList ipentries = new ArrayList(Firewall.GetSortedBy(sort));

			//zjistit zda bude paging, najit maximalni index na strance
			int firstiVal = Convert.ToInt32(args[1]);   //prvni index na strance
			//maximalni index (20 radku mame) + hlidat konec seznamu...
			int imax = Math.Min(firstiVal + ImprovedDialog.PAGE_ROWS, ipentries.Count);
			
			dialogHandler.CreateBackground(width);
			dialogHandler.SetLocation(40, 30);
			//prvni radek
			dialogHandler.Add(new GUTATable(1));
			dialogHandler.Add(new GUTAColumn());
			dialogHandler.Add(TextFactory.CreateText("Administrace blokovaných IP (" + (firstiVal + 1) + "-" + imax + " z " + ipentries.Count + ")"));
			dialogHandler.AddLast(new GUTAColumn(ButtonFactory.D_BUTTON_WIDTH));
			dialogHandler.Add(ButtonFactory.CreateButton(LeafComponentTypes.ButtonCross, 0)); //exit button
			dialogHandler.MakeTableTransparent();

			//druhy radek
			dialogHandler.Add(new GUTATable(1));			
			dialogHandler.Add(new GUTAColumn(ButtonFactory.D_BUTTON_WIDTH)); //na velikost cudliku
			dialogHandler.Add(TextFactory.CreateText("Del"));
			
			dialogHandler.Add(new GUTAColumn(IPwidth)); //cudlik s IP
			dialogHandler.Add(ButtonFactory.CreateButton(LeafComponentTypes.ButtonSortUp, 1)); //tridit dle blokovane IP vzestupne
			dialogHandler.Add(ButtonFactory.CreateButton(LeafComponentTypes.ButtonSortDown, 0, ButtonFactory.D_SORTBUTTON_LINE_OFFSET, 2)); //tridit dle blokovane IP sestupne     
			dialogHandler.Add(TextFactory.CreateText(ButtonFactory.D_SORTBUTTON_COL_OFFSET, 0, "Rozsah IP od"));

			dialogHandler.Add(new GUTAColumn(IPwidth)); //cudlik s IP
			dialogHandler.Add(TextFactory.CreateText(ButtonFactory.D_SORTBUTTON_COL_OFFSET, 0, "Rozsah IP do"));
			
			dialogHandler.Add(new GUTAColumn(duvod));
			dialogHandler.Add(TextFactory.CreateText("Dùvod"));
			
			dialogHandler.Add(new GUTAColumn(kdo));
			dialogHandler.Add(ButtonFactory.CreateButton(LeafComponentTypes.ButtonSortUp, 3)); //tridit dle GM - Blokare vzestupne (Blocked by)
			dialogHandler.Add(ButtonFactory.CreateButton(LeafComponentTypes.ButtonSortDown, 0, ButtonFactory.D_SORTBUTTON_LINE_OFFSET, 4)); //tridit dle GM - Blokare sestupne(Blocked by)  
			dialogHandler.Add(TextFactory.CreateText(ButtonFactory.D_SORTBUTTON_COL_OFFSET, 0, "Zablokoval"));

			dialogHandler.MakeTableTransparent();

			//seznam blokovaných IP adres
			dialogHandler.Add(new GUTATable(ImprovedDialog.PAGE_ROWS));
			dialogHandler.CopyColsFromLastTable();

			int rowCntr = 0;
			for(int i = firstiVal; i < imax; i++) {
				ISortableIpBlockEntry entry = (ISortableIpBlockEntry) ipentries[i];
				dialogHandler.AddToColumn(0, rowCntr, ButtonFactory.CreateButton(LeafComponentTypes.ButtonCross, i + 10));
				dialogHandler.AddToColumn(1, rowCntr, TextFactory.CreateText(entry.Ip.ToString()));
				dialogHandler.AddToColumn(2, rowCntr, TextFactory.CreateText(entry.toIp));
				dialogHandler.AddToColumn(3, rowCntr, TextFactory.CreateText(entry.Reason));
				dialogHandler.AddToColumn(4, rowCntr, TextFactory.CreateText(entry.Account.Name));
				rowCntr++;
			}			
			dialogHandler.MakeTableTransparent();

			//ted paging
			dialogHandler.CreatePaging(ipentries.Count, firstiVal);

			// nadpis pro blokaci novych
			dialogHandler.Add(new GUTATable(1));
			dialogHandler.Add(new GUTAColumn(ButtonFactory.D_BUTTON_WIDTH));
			dialogHandler.AddToColumn(0, 0, ButtonFactory.CreateButton(LeafComponentTypes.ButtonPaper, 5));
			dialogHandler.Add(new GUTAColumn(width / 2 - ButtonFactory.D_BUTTON_WIDTH));
			dialogHandler.Add(TextFactory.CreateText(0, 0, "Pridat novì blokovanou IP"));
			dialogHandler.Add(new GUTAColumn(ButtonFactory.D_BUTTON_WIDTH));
            dialogHandler.AddToColumn(2, 0, ButtonFactory.CreateButton(LeafComponentTypes.ButtonPaper, 6));
			dialogHandler.Add(new GUTAColumn());
			dialogHandler.Add(TextFactory.CreateText(0, 0, "Pridat novì blokovany rozsah IP"));

			dialogHandler.MakeTableTransparent();

			dialogHandler.WriteOut();

			//uložit info o právì vytvoøeném dialogu pro návrat
			DialogStackItem.EnstackDialog(sendTo, focus, D_BlockedIP.Instance,
					sort, //typ tøídìní
					firstiVal); //cislo zpravy kterou zacina stranka (pro paging)

			GumpInstance.SetTag(entriesTK, ipentries);			
		}

		public override void OnResponse(GumpInstance gi, GumpResponse gr) {
			ArrayList entries = (ArrayList) gi.GetTag(entriesTK);
			
			DialogStackItem dsi = null;
			if(gr.pressedButton < 10) { //ovladaci tlacitka (sorting, paging atd)				
				switch(gr.pressedButton) {
					case 0: //rmouseButton anebo zavreni tlacitkem v rohu
						DialogStackItem.PopStackedDialog(gi.Cont.Conn);	//odstranit ze stacku aktualni dialog
						DialogStackItem.ShowPreviousDialog(gi.Cont.Conn); //zobrazit pripadny predchozi dialog						
						return;
					case 1: //sort ipup
						dsi = DialogStackItem.PopStackedDialog(gi.Cont.Conn);//vem ulozene info o dialogu
						dsi.Args[0] = SortingCriteria.IPAsc; //uprav info o sortovani
						dsi.Show();
						return;
					case 2: // sort ipdown
						dsi = DialogStackItem.PopStackedDialog(gi.Cont.Conn);//vem ulozene info o dialogu
						dsi.Args[0] = SortingCriteria.IPDesc; //uprav info o sortovani
						dsi.Show();
						return;
					case 3: // sort accountup
						dsi = DialogStackItem.PopStackedDialog(gi.Cont.Conn);//vem ulozene info o dialogu
						dsi.Args[0] = SortingCriteria.AccountAsc; //uprav info o sortovani
						dsi.Show();
						return;
					case 4: // sort accountdown
						dsi = DialogStackItem.PopStackedDialog(gi.Cont.Conn);//vem ulozene info o dialogu
						dsi.Args[0] = SortingCriteria.AccountDesc; //uprav info o sortovani
						dsi.Show();
						return;

					case 5: // block single ip
						gi.Focus.Dialog(gi.Cont, D_BlockIP.Instance);
						return;
					case 6: // block iprange
						gi.Focus.Dialog(gi.Cont, D_BlockIPRange.Instance);
						return;
				}
			} else if(ImprovedDialog.PagingButtonsHandled(gi, gr, 1, entries.Count)) {
				//kliknuto na paging? (ta 1 = index parametru nesoucim info o pagingu (zde dsi.Args[1] viz výše)
				return;
			} else {
				//porad jsme zde - klikalo se na tlacitko primo u bloknute IP
				//zjistime kterej cudlik z radku byl zmacknut
				int row = (int)(gr.pressedButton - 10);
				ISortableIpBlockEntry entry = (ISortableIpBlockEntry)entries[row];
				if(entry.toIp == "") {
					Firewall.RemoveBlockedIP(entry.Ip);
					//znovuzavolat dialog
					DialogStackItem.ShowPreviousDialog(gi.Cont.Conn);
					return;
				}
				Firewall.RemoveBlockedIPRange(entry.Ip, IPAddress.Parse(entry.toIp));
				//znovuzavolat dialog
				DialogStackItem.ShowPreviousDialog(gi.Cont.Conn);
				return;
			}
		}
	}

	public class D_BlockIP : CompiledGump {
		private static D_BlockIP instance;
		public static D_BlockIP Instance {
			get {
				return instance;
			}
		}

		public D_BlockIP() {
			instance = this;
		}

		public override void Construct(Thing focus, AbstractCharacter sendTo, object[] args) {
			string ip;

			if ((args != null) && (args.Length > 0)) {
				ip = string.Concat(args[0]);
			} else {
				ip = "";
			}
			int width = 500;
			int labels = 100;
			
			ImprovedDialog dialogHandler = new ImprovedDialog(this.GumpInstance);

			dialogHandler.CreateBackground(width);
			dialogHandler.SetLocation(200, 280);
			//prvni radek
			dialogHandler.Add(new GUTATable(1));
			dialogHandler.Add(new GUTAColumn(width));
			dialogHandler.Add(TextFactory.CreateText((width - 130) / 2, 0, "Pridej blokovanou IP"));
			dialogHandler.AddLast(new GUTAColumn(ButtonFactory.D_BUTTON_WIDTH));
            dialogHandler.Add(ButtonFactory.CreateButton(LeafComponentTypes.ButtonCross, 0)); //exit button
			dialogHandler.MakeTableTransparent();

			//druhy radek IP adresa
			dialogHandler.Add(new GUTATable(1));
			dialogHandler.LastTable.RowHeight = ImprovedDialog.D_ROW_HEIGHT;
			dialogHandler.Add(new GUTAColumn());
			dialogHandler.Add(TextFactory.CreateText("IP adresa:"));
			dialogHandler.AddLast(new GUTAColumn(width-labels));
			dialogHandler.Add(InputFactory.CreateInput(LeafComponentTypes.InputText, 10, ip));
			dialogHandler.MakeTableTransparent();

			//treti radek Duvod
			dialogHandler.Add(new GUTATable(1));
			dialogHandler.LastTable.RowHeight = ImprovedDialog.D_ROW_HEIGHT;
			dialogHandler.Add(new GUTAColumn());
			dialogHandler.Add(TextFactory.CreateText("Dùvod:"));
			dialogHandler.AddLast(new GUTAColumn(width - labels));
			dialogHandler.Add(InputFactory.CreateInput(LeafComponentTypes.InputText, 11, ""));
			dialogHandler.MakeTableTransparent();

			//ctvrty radek Kdo
			dialogHandler.Add(new GUTATable(1));
			dialogHandler.LastTable.RowHeight = ImprovedDialog.D_ROW_HEIGHT;
			dialogHandler.Add(new GUTAColumn());
			dialogHandler.Add(TextFactory.CreateText("Zablokoval:"));
			dialogHandler.AddLast(new GUTAColumn(width - labels));
			dialogHandler.Add(TextFactory.CreateText(sendTo.Account.Name));
			dialogHandler.MakeTableTransparent();

			//paty radek tlacitko OK
			dialogHandler.Add(new GUTATable(1));
			dialogHandler.Add(new GUTAColumn());
            dialogHandler.Add(ButtonFactory.CreateButton(LeafComponentTypes.ButtonOK, width / 2, 0, 1)); //exit button
			dialogHandler.MakeTableTransparent();
			
			dialogHandler.WriteOut();

		}
		public override void OnResponse(GumpInstance gi, GumpResponse gr) {
			if(gr.pressedButton == 1) { //OK button
				Firewall.AddBlockedIP(gr.GetTextResponse(10), gr.GetTextResponse(11), gi.Cont.Account);
				//zavolat stacklej dialog (if any)				
				DialogStackItem.ShowPreviousDialog(gi.Cont.Conn);				
			} else {
				//rovnou se vracime
				DialogStackItem.ShowPreviousDialog(gi.Cont.Conn);						
			}
		}
	}

	public class D_BlockIPRange : CompiledGump {
		private static D_BlockIPRange instance;
		public static D_BlockIPRange Instance {
			get {
				return instance;
			}
		}

		public D_BlockIPRange() {
			instance = this;
		}

		public override void Construct(Thing focus, AbstractCharacter sendTo, object[] args) {
			string ipfrom;
			string ipto;

			if ((args != null) && (args.Length > 0)) {
				ipfrom = string.Concat(args[0]);
				ipto = ipfrom;
			} else { //TODO if there would be 2 arguments.			
				ipfrom = "";
				ipto = ipfrom;
			}
			int width = 500;
			int labels = 130;

			ImprovedDialog dialogHandler = new ImprovedDialog(this.GumpInstance);

			dialogHandler.CreateBackground(width);
			dialogHandler.SetLocation(200, 280);
			//prvni radek
			dialogHandler.Add(new GUTATable(1));
			dialogHandler.Add(new GUTAColumn(width));
			dialogHandler.Add(TextFactory.CreateText((width - 130) / 2, 0, "Pridej blokovany range"));
			dialogHandler.AddLast(new GUTAColumn(ButtonFactory.D_BUTTON_WIDTH));
            dialogHandler.Add(ButtonFactory.CreateButton(LeafComponentTypes.ButtonCross, 0)); //exit button
			dialogHandler.MakeTableTransparent();

			//druhy radek 1.IP adresa
			dialogHandler.Add(new GUTATable(1));
			dialogHandler.LastTable.RowHeight = ImprovedDialog.D_ROW_HEIGHT;
			dialogHandler.Add(new GUTAColumn());
			dialogHandler.Add(TextFactory.CreateText("IP adresa od:"));
			dialogHandler.AddLast(new GUTAColumn(width - labels));
			dialogHandler.Add(InputFactory.CreateInput(LeafComponentTypes.InputText, 9, ipfrom));			            
			dialogHandler.MakeTableTransparent();

			//treti radek 2.IP adresa
			dialogHandler.Add(new GUTATable(1));
			dialogHandler.LastTable.RowHeight = ImprovedDialog.D_ROW_HEIGHT;
			dialogHandler.Add(new GUTAColumn());
			dialogHandler.Add(TextFactory.CreateText("IP adresa do:"));
			dialogHandler.AddLast(new GUTAColumn(width - labels));
			dialogHandler.Add(InputFactory.CreateInput(LeafComponentTypes.InputText, 10, ipto));			            			
            dialogHandler.MakeTableTransparent();

			//ctvrty radek Duvod
			dialogHandler.Add(new GUTATable(1));
			dialogHandler.LastTable.RowHeight = ImprovedDialog.D_ROW_HEIGHT;
			dialogHandler.Add(new GUTAColumn());
			dialogHandler.Add(TextFactory.CreateText("Dùvod:"));
			dialogHandler.AddLast(new GUTAColumn(width - labels));
			dialogHandler.Add(InputFactory.CreateInput(LeafComponentTypes.InputText, 11, ""));			            			            			
			dialogHandler.MakeTableTransparent();

			//ctvrty radek Kdo
			dialogHandler.Add(new GUTATable(1));
			dialogHandler.LastTable.RowHeight = ImprovedDialog.D_ROW_HEIGHT;
			dialogHandler.Add(new GUTAColumn());
			dialogHandler.Add(TextFactory.CreateText("Zablokoval:"));
			dialogHandler.AddLast(new GUTAColumn(width - labels));
			dialogHandler.Add(TextFactory.CreateText(sendTo.Account.Name));
			dialogHandler.MakeTableTransparent();

			//paty radek tlacitko OK
			dialogHandler.Add(new GUTATable(1));
			dialogHandler.Add(new GUTAColumn());
            dialogHandler.Add(ButtonFactory.CreateButton(LeafComponentTypes.ButtonOK, width / 2, 0, 1)); //exit button
			dialogHandler.MakeTableTransparent();

			dialogHandler.WriteOut();

		}
		public override void OnResponse(GumpInstance gi, GumpResponse gr) {
			if(gr.pressedButton == 1) {
				Firewall.AddBlockedIPRange(gr.GetTextResponse(9), gr.GetTextResponse(10), gr.GetTextResponse(11), gi.Cont.Account);
				//rovnou se vracime			
				DialogStackItem.ShowPreviousDialog(gi.Cont.Conn);
			} else {
				//rovnou se vracime
				DialogStackItem.ShowPreviousDialog(gi.Cont.Conn);
			}
		}
	}

	public class FirewallCommands : CompiledScript {
		public void func_BlockedIPs(Character self) {
			ArrayList ipentries = new ArrayList(Firewall.GetSortedBy(SortingCriteria.IPAsc));
			if (ipentries.Count == 0) {
				Globals.SrcWriteLine("Zadna IP neni blokovana");
				return;
			}
			self.Dialog(D_BlockedIP.Instance, SortingCriteria.IPAsc, 0);
		}

		public void func_BlockIP(Character self, ScriptArgs sa) {
			if (sa != null) {
				self.Dialog(D_BlockIP.Instance, sa.Argv);
			} else {
				self.Dialog(D_BlockIP.Instance);
			}
		}
		public void func_BlockIpRange(Character self, ScriptArgs sa) {
			if (sa != null) {
				self.Dialog(D_BlockIPRange.Instance, sa.Argv);
			} else {
				self.Dialog(D_BlockIPRange.Instance);
			}
		}
	}
}