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
        internal static readonly TagKey ipSortingTK = TagKey.Get("__blocked_ips_sorting_");
        internal static readonly TagKey ipsListTK = TagKey.Get("__blocked_ips_list_");

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			int IPwidth = 120; //velikost okna pro IP adresu 
			int duvod = 350;
			int kdo = 110;
			int width = IPwidth * 2 + duvod + kdo + 2 * ButtonFactory.D_BUTTON_WIDTH;

			ImprovedDialog dialogHandler = new ImprovedDialog(this.GumpInstance);
			ArrayList ipentries = null;
			if(!args.HasTag(D_BlockedIP.ipsListTK)) {
				//vzit seznam a pripadne ho setridit...
				//toto se provede jen pri prvnim zobrazeni nebo zmene kriteria!
				SortingCriteria sort = (SortingCriteria)args.GetTag(D_BlockedIP.ipSortingTK);
				ipentries = new ArrayList(Firewall.GetSortedBy(sort));
				args.SetTag(D_BlockedIP.ipsListTK,ipentries); //ulozime je mezi gumpovni parametry
			} else {
				//regionlist si posilame v argumentu (napriklad pri pagingu)
				ipentries = (ArrayList)args.GetTag(D_BlockedIP.ipsListTK);
			}			
		
			//zjistit zda bude paging, najit maximalni index na strance
			int firstiVal = TagMath.IGetTag(args, ImprovedDialog.pagingIndexTK);   //prvni index na strance
			//maximalni index (20 radku mame) + hlidat konec seznamu...
			int imax = Math.Min(firstiVal + ImprovedDialog.PAGE_ROWS, ipentries.Count);
			
			dialogHandler.CreateBackground(width);
			dialogHandler.SetLocation(40, 30);
			//prvni radek
			dialogHandler.AddTable(new GUTATable(1, 0, ButtonFactory.D_BUTTON_WIDTH));
			dialogHandler.LastTable[0,0] = TextFactory.CreateHeadline("Administrace blokovaných IP (" + (firstiVal + 1) + "-" + imax + " z " + ipentries.Count + ")");
			dialogHandler.LastTable[0,1] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonCross, 0); //exit button
			dialogHandler.MakeLastTableTransparent();

			//druhy radek
			dialogHandler.AddTable(new GUTATable(1, ButtonFactory.D_BUTTON_WIDTH, IPwidth, IPwidth, duvod, kdo));
			dialogHandler.LastTable[0, 0] = TextFactory.CreateLabel("Del");
			//cudlik s IP
			dialogHandler.LastTable[0, 1] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonSortUp, 1); //tridit dle blokovane IP vzestupne
			dialogHandler.LastTable[0, 1] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonSortDown, 0, ButtonFactory.D_SORTBUTTON_LINE_OFFSET, 2); //tridit dle blokovane IP sestupne     
			dialogHandler.LastTable[0, 1] = TextFactory.CreateLabel(ButtonFactory.D_SORTBUTTON_COL_OFFSET, 0, "Rozsah IP od");
			//cudlik s IP
			dialogHandler.LastTable[0, 2] = TextFactory.CreateLabel(ButtonFactory.D_SORTBUTTON_COL_OFFSET, 0, "Rozsah IP do");
			//duvod
			dialogHandler.LastTable[0, 3] = TextFactory.CreateLabel("Dùvod");
			//kdo blokoval
			dialogHandler.LastTable[0, 4] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonSortUp, 3); //tridit dle GM - Blokare vzestupne (Blocked by)
			dialogHandler.LastTable[0, 4] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonSortDown, 0, ButtonFactory.D_SORTBUTTON_LINE_OFFSET, 4); //tridit dle GM - Blokare sestupne(Blocked by)  
			dialogHandler.LastTable[0, 4] = TextFactory.CreateLabel(ButtonFactory.D_SORTBUTTON_COL_OFFSET, 0, "Zablokoval");

			dialogHandler.MakeLastTableTransparent();

			//seznam blokovaných IP adres
			dialogHandler.AddTable(new GUTATable(ImprovedDialog.PAGE_ROWS));
			dialogHandler.CopyColsFromLastTable();

			int rowCntr = 0;
			for(int i = firstiVal; i < imax; i++, rowCntr++) {
				ISortableIpBlockEntry entry = (ISortableIpBlockEntry) ipentries[i];
				dialogHandler.LastTable[rowCntr, 0] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonCross, i + 10);
				dialogHandler.LastTable[rowCntr, 1] = TextFactory.CreateText(entry.Ip.ToString());
				dialogHandler.LastTable[rowCntr, 2] = TextFactory.CreateText(entry.toIp);
				dialogHandler.LastTable[rowCntr, 3] = TextFactory.CreateText(entry.Reason);
				dialogHandler.LastTable[rowCntr, 4] = TextFactory.CreateText(entry.Account.Name);				
			}			
			dialogHandler.MakeLastTableTransparent();

			//ted paging
			dialogHandler.CreatePaging(ipentries.Count, firstiVal,1);

			// nadpis pro blokaci novych
			dialogHandler.AddTable(new GUTATable(1, ButtonFactory.D_BUTTON_WIDTH, width / 2 - ButtonFactory.D_BUTTON_WIDTH, ButtonFactory.D_BUTTON_WIDTH,0));			
			dialogHandler.LastTable[0, 0] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonPaper, 5);
			dialogHandler.LastTable[0, 1] = TextFactory.CreateLabel("Pøidat novì blokovanou IP");
			dialogHandler.LastTable[0, 2] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonPaper, 6);
			dialogHandler.LastTable[0, 3] = TextFactory.CreateLabel("Pøidat novì blokovaný rozsah IP");

			dialogHandler.MakeLastTableTransparent();

			dialogHandler.WriteOut();
		}

		public override void OnResponse(GumpInstance gi, GumpResponse gr, DialogArgs args) {
			ArrayList entries = (ArrayList)args.GetTag(D_BlockedIP.ipsListTK);//tam jsou ulozeny
			
			if(gr.pressedButton < 10) { //ovladaci tlacitka (sorting, paging atd)				
				switch(gr.pressedButton) {
					case 0: //rmouseButton anebo zavreni tlacitkem v rohu
						DialogStacking.ShowPreviousDialog(gi); //zobrazit pripadny predchozi dialog						
						return;
					case 1: //sort ipup
                        args.SetTag(D_BlockedIP.ipSortingTK, SortingCriteria.IPAsc);//uprav info o sortovani
                        args.RemoveTag(D_BlockedIP.ipsListTK);
						DialogStacking.ResendAndRestackDialog(gi);
						return;
					case 2: // sort ipdown
                        args.SetTag(D_BlockedIP.ipSortingTK, SortingCriteria.IPDesc);//uprav info o sortovani
                        args.RemoveTag(D_BlockedIP.ipsListTK);
						DialogStacking.ResendAndRestackDialog(gi);
						return;
					case 3: // sort accountup
						args.SetTag(D_BlockedIP.ipSortingTK, SortingCriteria.AccountAsc);//uprav info o sortovani
                        args.RemoveTag(D_BlockedIP.ipsListTK);
						DialogStacking.ResendAndRestackDialog(gi);
						return;
					case 4: // sort accountdown
						args.SetTag(D_BlockedIP.ipSortingTK, SortingCriteria.AccountDesc);//uprav info o sortovani
                        args.RemoveTag(D_BlockedIP.ipsListTK);
						DialogStacking.ResendAndRestackDialog(gi);
						return;
					case 5: // block single ip
						//stackneme aktualni dialog pro navrat
						GumpInstance newGi = gi.Cont.Dialog(SingletonScript<D_BlockIP>.Instance);//a zobrazime novy
						DialogStacking.EnstackDialog(gi, newGi);
						return;
					case 6: // block iprange
						//stackneme aktualni dialog pro navrat
						newGi = gi.Cont.Dialog(SingletonScript<D_BlockIPRange>.Instance);//a zobrazime novy
						DialogStacking.EnstackDialog(gi, newGi);						
						return;
				}
			} else if(ImprovedDialog.PagingButtonsHandled(gi, gr, entries.Count,1)) {//kliknuto na paging?
				return;
			} else {
				//porad jsme zde - klikalo se na tlacitko primo u bloknute IP
				//zjistime kterej cudlik z radku byl zmacknut
				int row = (int)(gr.pressedButton - 10);
				ISortableIpBlockEntry entry = (ISortableIpBlockEntry)entries[row];
				if(entry.toIp == "") {
					Firewall.RemoveBlockedIP(entry.Ip);
					//znovuzavolat dialog
					DialogStacking.ResendAndRestackDialog(gi);
				} else {
					Firewall.RemoveBlockedIPRange(entry.Ip, IPAddress.Parse(entry.toIp));
					//znovuzavolat dialog
					DialogStacking.ResendAndRestackDialog(gi);
				}
				return;
			}
		}

		[SteamFunction]
		public static void BlockedIPs(Character self) {
			ArrayList ipentries = new ArrayList(Firewall.GetSortedBy(SortingCriteria.IPAsc));
			if(ipentries.Count == 0) {
				D_Display_Text.ShowInfo("Zadna IP neni blokovana");
				return;
			}
			//zaverecnej null - sem se ulozi seznam blokovanych IP
			DialogArgs newArgs = new DialogArgs();
			newArgs.SetTag(D_BlockedIP.ipSortingTK, SortingCriteria.IPAsc);
			self.Dialog(SingletonScript<D_BlockedIP>.Instance, newArgs);
		}
	}

	public class D_BlockIP : CompiledGump {
		internal static readonly TagKey ipToBlockTK = TagKey.Get("__ip_to_block_");
        
		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			string ip = TagMath.SGetTag(args,D_BlockIP.ipToBlockTK);			
            if (ip == null) ip = "";

			int width = 500;
			int labels = 130;
			
			ImprovedDialog dialogHandler = new ImprovedDialog(this.GumpInstance);

			dialogHandler.CreateBackground(width);
			dialogHandler.SetLocation(200, 280);
			//prvni radek
			dialogHandler.AddTable(new GUTATable(1, width, ButtonFactory.D_BUTTON_WIDTH));
			dialogHandler.LastTable[0,0] = TextFactory.CreateText((width - 130) / 2, 0, "Pridej blokovanou IP");
			dialogHandler.LastTable[0,1] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonCross, 0); //exit button
			dialogHandler.MakeLastTableTransparent();

			//druhy radek IP adresa
			dialogHandler.AddTable(new GUTATable(1, 0, width - labels));
			dialogHandler.LastTable.RowHeight = ImprovedDialog.D_ROW_HEIGHT;			
			dialogHandler.LastTable[0,0] = TextFactory.CreateText("IP adresa:");
			dialogHandler.LastTable[0,1] = InputFactory.CreateInput(LeafComponentTypes.InputText, 10, ip);
			dialogHandler.MakeLastTableTransparent();

			//treti radek Duvod
			dialogHandler.AddTable(new GUTATable(1, 0, width - labels));
			dialogHandler.LastTable.RowHeight = ImprovedDialog.D_ROW_HEIGHT;
			dialogHandler.LastTable[0,0] = TextFactory.CreateText("Dùvod:");
			dialogHandler.LastTable[0,1] = InputFactory.CreateInput(LeafComponentTypes.InputText, 11, "");
			dialogHandler.MakeLastTableTransparent();

			//ctvrty radek Kdo
			dialogHandler.AddTable(new GUTATable(1, 0, width - labels));
			dialogHandler.LastTable.RowHeight = ImprovedDialog.D_ROW_HEIGHT;
			dialogHandler.LastTable[0,0] = TextFactory.CreateText("Zablokoval:");
			dialogHandler.LastTable[0,1] = TextFactory.CreateText(sendTo.Account.Name);
			dialogHandler.MakeLastTableTransparent();

			//paty radek tlacitko OK
			dialogHandler.AddTable(new GUTATable(1,0));
			dialogHandler.LastTable[0,0] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonOK, width / 2, 0, 1); //exit button
			dialogHandler.MakeLastTableTransparent();
			
			dialogHandler.WriteOut();

		}
		public override void OnResponse(GumpInstance gi, GumpResponse gr, DialogArgs args) {
			if(gr.pressedButton == 1) { //OK button
				Firewall.AddBlockedIP(gr.GetTextResponse(10), gr.GetTextResponse(11), gi.Cont.Account);
				//zavolat stacklej dialog (if any)				
				DialogStacking.ShowPreviousDialog(gi);				
			} else {
				//rovnou se vracime
				DialogStacking.ShowPreviousDialog(gi);						
			}
		}

		[SteamFunction]
		public static void BlockIP(Character self, ScriptArgs sa) {
			if(sa != null) {
				DialogArgs newArgs = new DialogArgs();
				newArgs.SetTag(D_BlockIP.ipToBlockTK, sa.argv[0]);
				self.Dialog(SingletonScript<D_BlockIP>.Instance, newArgs);
			} else {
				self.Dialog(SingletonScript<D_BlockIP>.Instance);
			}
		}
	}

	public class D_BlockIPRange : CompiledGump {
		internal static readonly TagKey ipFromRangeTK = TagKey.Get("__ip_from_range_");
		internal static readonly TagKey ipToRangeTK = TagKey.Get("__ip_to_range_");
        
		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
            string ipfrom = TagMath.SGetTag(args, D_BlockIPRange.ipFromRangeTK);
            if (ipfrom == null) ipfrom = "";

            string ipto = TagMath.SGetTag(args, D_BlockIPRange.ipToRangeTK);
            if (ipto == null) ipto = "";

			int width = 500;
			int labels = 150;

			ImprovedDialog dialogHandler = new ImprovedDialog(this.GumpInstance);

			dialogHandler.CreateBackground(width);
			dialogHandler.SetLocation(200, 280);
			//prvni radek
			dialogHandler.AddTable(new GUTATable(1, 0, ButtonFactory.D_BUTTON_WIDTH));			
			dialogHandler.LastTable[0,0] = TextFactory.CreateText((width - 130) / 2, 0, "Pridej blokovany range");
			dialogHandler.LastTable[0,1] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonCross, 0); //exit button
			dialogHandler.MakeLastTableTransparent();

			//druhy radek 1.IP adresa
			dialogHandler.AddTable(new GUTATable(1, 0, width - labels));
			dialogHandler.LastTable.RowHeight = ImprovedDialog.D_ROW_HEIGHT;			
			dialogHandler.LastTable[0,0] = TextFactory.CreateText("IP adresa od:");
			dialogHandler.LastTable[0,1] = InputFactory.CreateInput(LeafComponentTypes.InputText, 9, ipfrom);			            
			dialogHandler.MakeLastTableTransparent();

			//treti radek 2.IP adresa
			dialogHandler.AddTable(new GUTATable(1, 0, width - labels));
			dialogHandler.LastTable.RowHeight = ImprovedDialog.D_ROW_HEIGHT;			
			dialogHandler.LastTable[0,0] = TextFactory.CreateText("IP adresa do:");			
			dialogHandler.LastTable[0,1] = InputFactory.CreateInput(LeafComponentTypes.InputText, 10, ipto);			            			
            dialogHandler.MakeLastTableTransparent();

			//ctvrty radek Duvod
			dialogHandler.AddTable(new GUTATable(1, 0, width - labels));
			dialogHandler.LastTable.RowHeight = ImprovedDialog.D_ROW_HEIGHT;
			dialogHandler.LastTable[0,0] = TextFactory.CreateText("Dùvod:");
			dialogHandler.LastTable[0,1] = InputFactory.CreateInput(LeafComponentTypes.InputText, 11, "");			            			            			
			dialogHandler.MakeLastTableTransparent();

			//ctvrty radek Kdo
			dialogHandler.AddTable(new GUTATable(1, 0, width - labels));
			dialogHandler.LastTable.RowHeight = ImprovedDialog.D_ROW_HEIGHT;
			dialogHandler.LastTable[0,0] = TextFactory.CreateText("Zablokoval:");
			dialogHandler.LastTable[0,1] = TextFactory.CreateText(sendTo.Account.Name);
			dialogHandler.MakeLastTableTransparent();

			//paty radek tlacitko OK
			dialogHandler.AddTable(new GUTATable(1,0));
            dialogHandler.LastTable[0,0] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonOK, width / 2, 0, 1); //exit button
			dialogHandler.MakeLastTableTransparent();

			dialogHandler.WriteOut();

		}
		public override void OnResponse(GumpInstance gi, GumpResponse gr, DialogArgs args) {
			if(gr.pressedButton == 1) {
				Firewall.AddBlockedIPRange(gr.GetTextResponse(9), gr.GetTextResponse(10), gr.GetTextResponse(11), gi.Cont.Account);
				//rovnou se vracime			
				DialogStacking.ShowPreviousDialog(gi);
			} else {
				//rovnou se vracime
				DialogStacking.ShowPreviousDialog(gi);
			}
		}

		[SteamFunction]
		public static void BlockIpRange(Character self, ScriptArgs sa) {
			if(sa != null) {
				DialogArgs newArgs = new DialogArgs();
				if(sa.argv != null && sa.argv.Length == 1) {
					newArgs.SetTag(D_BlockIPRange.ipFromRangeTK,sa.argv[0]);
				} else if(sa.argv != null && sa.argv.Length == 2) {
					newArgs.SetTag(D_BlockIPRange.ipFromRangeTK,sa.argv[0]);
					newArgs.SetTag(D_BlockIPRange.ipToRangeTK,sa.argv[1]);
				} else {
					D_Display_Text.ShowError("Oèekávány 2 parametry: od IP - do IP");
					return;
				}
				self.Dialog(SingletonScript<D_BlockIPRange>.Instance, newArgs);
			} else {
				self.Dialog(SingletonScript<D_BlockIPRange>.Instance);
			}
		}
	}
}