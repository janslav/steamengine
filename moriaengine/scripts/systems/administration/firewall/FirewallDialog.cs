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

	public class D_BlockedIP : CompiledGumpDef {
		internal static readonly TagKey ipSortingTK = TagKey.Get("_blocked_ips_sorting_");
		internal static readonly TagKey ipsListTK = TagKey.Get("_blocked_ips_list_");

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			int IPwidth = 120; //velikost okna pro IP adresu 
			int duvod = 350;
			int kdo = 110;
			int width = IPwidth * 2 + duvod + kdo + 2 * ButtonMetrics.D_BUTTON_WIDTH;

			ImprovedDialog dialogHandler = new ImprovedDialog(this.GumpInstance);
			ArrayList ipentries = (ArrayList) args.GetTag(D_BlockedIP.ipsListTK); //regionlist si posilame v argumentu (napriklad pri pagingu)
			if (ipentries == null) {
				//vzit seznam a pripadne ho setridit...
				//toto se provede jen pri prvnim zobrazeni nebo zmene kriteria!
				SortingCriteria sort = (SortingCriteria) TagMath.IGetTag(args, D_BlockedIP.ipSortingTK);
				ipentries = new ArrayList(Firewall.GetSortedBy(sort));
				args.SetTag(D_BlockedIP.ipsListTK, ipentries); //ulozime je mezi gumpovni parametry
			}

			//zjistit zda bude paging, najit maximalni index na strance
			int firstiVal = TagMath.IGetTag(args, ImprovedDialog.pagingIndexTK);   //prvni index na strance
			//maximalni index (20 radku mame) + hlidat konec seznamu...
			int imax = Math.Min(firstiVal + ImprovedDialog.PAGE_ROWS, ipentries.Count);

			dialogHandler.CreateBackground(width);
			dialogHandler.SetLocation(40, 30);
			//prvni radek
			dialogHandler.AddTable(new GUTATable(1, 0, ButtonMetrics.D_BUTTON_WIDTH));
			dialogHandler.LastTable[0, 0] = GUTAText.Builder.TextHeadline("Administrace blokovaných IP (" + (firstiVal + 1) + "-" + imax + " z " + ipentries.Count + ")").Build();
			dialogHandler.LastTable[0, 1] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonCross).Id(0).Build(); //exit button
			dialogHandler.MakeLastTableTransparent();

			//druhy radek
			dialogHandler.AddTable(new GUTATable(1, ButtonMetrics.D_BUTTON_WIDTH, IPwidth, IPwidth, duvod, kdo));
			dialogHandler.LastTable[0, 0] = GUTAText.Builder.TextLabel("Del").Build();
			//cudlik s IP
			dialogHandler.LastTable[0, 1] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonSortUp).Id(1).Build(); //tridit dle blokovane IP vzestupne
			dialogHandler.LastTable[0, 1] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonSortDown).YPos(ButtonMetrics.D_SORTBUTTON_LINE_OFFSET).Id(2).Build(); //tridit dle blokovane IP sestupne     
			dialogHandler.LastTable[0, 1] = GUTAText.Builder.TextLabel("Rozsah IP od").XPos(ButtonMetrics.D_SORTBUTTON_COL_OFFSET).Build();
			//cudlik s IP
			dialogHandler.LastTable[0, 2] = GUTAText.Builder.TextLabel("Rozsah IP do").XPos(ButtonMetrics.D_SORTBUTTON_COL_OFFSET).Build();
			//duvod
			dialogHandler.LastTable[0, 3] = GUTAText.Builder.TextLabel("Dùvod").Build();
			//kdo blokoval
			dialogHandler.LastTable[0, 4] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonSortUp).Id(3).Build(); //tridit dle GM - Blokare vzestupne (Blocked by)
			dialogHandler.LastTable[0, 4] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonSortDown).YPos(ButtonMetrics.D_SORTBUTTON_LINE_OFFSET).Id(4).Build(); //tridit dle GM - Blokare sestupne(Blocked by)  
			dialogHandler.LastTable[0, 4] = GUTAText.Builder.TextLabel("Zablokoval").XPos(ButtonMetrics.D_SORTBUTTON_COL_OFFSET).Build();

			dialogHandler.MakeLastTableTransparent();

			//seznam blokovaných IP adres
			dialogHandler.AddTable(new GUTATable(ImprovedDialog.PAGE_ROWS));
			dialogHandler.CopyColsFromLastTable();

			int rowCntr = 0;
			for (int i = firstiVal; i < imax; i++, rowCntr++) {
				ISortableIpBlockEntry entry = (ISortableIpBlockEntry) ipentries[i];
				dialogHandler.LastTable[rowCntr, 0] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonCross).Id(i + 10).Build();
				dialogHandler.LastTable[rowCntr, 1] = GUTAText.Builder.Text(entry.Ip.ToString()).Build();
				dialogHandler.LastTable[rowCntr, 2] = GUTAText.Builder.Text(entry.toIp).Build();
				dialogHandler.LastTable[rowCntr, 3] = GUTAText.Builder.Text(entry.Reason).Build();
				dialogHandler.LastTable[rowCntr, 4] = GUTAText.Builder.Text(entry.Account.Name).Build();
			}
			dialogHandler.MakeLastTableTransparent();

			//ted paging
			dialogHandler.CreatePaging(ipentries.Count, firstiVal, 1);

			// nadpis pro blokaci novych
			dialogHandler.AddTable(new GUTATable(1, ButtonMetrics.D_BUTTON_WIDTH, width / 2 - ButtonMetrics.D_BUTTON_WIDTH, ButtonMetrics.D_BUTTON_WIDTH, 0));
			dialogHandler.LastTable[0, 0] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonPaper).Id(5).Build();
			dialogHandler.LastTable[0, 1] = GUTAText.Builder.TextLabel("Pøidat novì blokovanou IP").Build();
			dialogHandler.LastTable[0, 2] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonPaper).Id(6).Build();
			dialogHandler.LastTable[0, 3] = GUTAText.Builder.TextLabel("Pøidat novì blokovaný rozsah IP").Build();

			dialogHandler.MakeLastTableTransparent();

			dialogHandler.WriteOut();
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			ArrayList entries = (ArrayList) args.GetTag(D_BlockedIP.ipsListTK);//tam jsou ulozeny

			if (gr.pressedButton < 10) { //ovladaci tlacitka (sorting, paging atd)				
				switch (gr.pressedButton) {
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
						Gump newGi = gi.Cont.Dialog(SingletonScript<D_BlockIP>.Instance);//a zobrazime novy
						DialogStacking.EnstackDialog(gi, newGi);
						return;
					case 6: // block iprange
						//stackneme aktualni dialog pro navrat
						newGi = gi.Cont.Dialog(SingletonScript<D_BlockIPRange>.Instance);//a zobrazime novy
						DialogStacking.EnstackDialog(gi, newGi);
						return;
				}
			} else if (ImprovedDialog.PagingButtonsHandled(gi, gr, entries.Count, 1)) {//kliknuto na paging?
				return;
			} else {
				//porad jsme zde - klikalo se na tlacitko primo u bloknute IP
				//zjistime kterej cudlik z radku byl zmacknut
				int row = (int) (gr.pressedButton - 10);
				ISortableIpBlockEntry entry = (ISortableIpBlockEntry) entries[row];
				if (entry.toIp == "") {
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
			if (ipentries.Count == 0) {
				D_Display_Text.ShowInfo("Zadna IP neni blokovana");
				return;
			}
			//zaverecnej null - sem se ulozi seznam blokovanych IP
			DialogArgs newArgs = new DialogArgs();
			newArgs.SetTag(D_BlockedIP.ipSortingTK, SortingCriteria.IPAsc);
			self.Dialog(SingletonScript<D_BlockedIP>.Instance, newArgs);
		}
	}

	public class D_BlockIP : CompiledGumpDef {
		internal static readonly TagKey ipToBlockTK = TagKey.Get("_ip_to_block_");

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			string ip = TagMath.SGetTag(args, D_BlockIP.ipToBlockTK);
			if (ip == null) ip = "";

			int width = 500;
			int labels = 130;

			ImprovedDialog dialogHandler = new ImprovedDialog(this.GumpInstance);

			dialogHandler.CreateBackground(width);
			dialogHandler.SetLocation(200, 280);
			//prvni radek
			dialogHandler.AddTable(new GUTATable(1, width, ButtonMetrics.D_BUTTON_WIDTH));
			dialogHandler.LastTable[0, 0] = GUTAText.Builder.TextHeadline("Pridej blokovanou IP").XPos((width - 130) / 2).Build();
			dialogHandler.LastTable[0, 1] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonCross).Id(0).Build(); //exit button
			dialogHandler.MakeLastTableTransparent();

			//druhy radek IP adresa
			dialogHandler.AddTable(new GUTATable(1, 0, width - labels));
			dialogHandler.LastTable.RowHeight = ImprovedDialog.D_ROW_HEIGHT;
			dialogHandler.LastTable[0, 0] = GUTAText.Builder.Text("IP adresa:").Build();
			dialogHandler.LastTable[0, 1] = GUTAInput.Builder.Id(10).Text(ip).Build();
			dialogHandler.MakeLastTableTransparent();

			//treti radek Duvod
			dialogHandler.AddTable(new GUTATable(1, 0, width - labels));
			dialogHandler.LastTable.RowHeight = ImprovedDialog.D_ROW_HEIGHT;
			dialogHandler.LastTable[0, 0] = GUTAText.Builder.TextLabel("Dùvod:").Build();
			dialogHandler.LastTable[0, 1] = GUTAInput.Builder.Id(11).Build();
			dialogHandler.MakeLastTableTransparent();

			//ctvrty radek Kdo
			dialogHandler.AddTable(new GUTATable(1, 0, width - labels));
			dialogHandler.LastTable.RowHeight = ImprovedDialog.D_ROW_HEIGHT;
			dialogHandler.LastTable[0, 0] = GUTAText.Builder.TextLabel("Zablokoval:").Build();
			dialogHandler.LastTable[0, 1] = GUTAText.Builder.Text(sendTo.Account.Name).Build();
			dialogHandler.MakeLastTableTransparent();

			//paty radek tlacitko OK
			dialogHandler.AddTable(new GUTATable(1, 0));
			dialogHandler.LastTable[0, 0] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonOK).XPos(width / 2).Id(1).Build(); //exit button
			dialogHandler.MakeLastTableTransparent();

			dialogHandler.WriteOut();

		}
		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			if (gr.pressedButton == 1) { //OK button
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
			if (sa != null) {
				DialogArgs newArgs = new DialogArgs();
				newArgs.SetTag(D_BlockIP.ipToBlockTK, sa.argv[0]);
				self.Dialog(SingletonScript<D_BlockIP>.Instance, newArgs);
			} else {
				self.Dialog(SingletonScript<D_BlockIP>.Instance);
			}
		}
	}

	public class D_BlockIPRange : CompiledGumpDef {
		internal static readonly TagKey ipFromRangeTK = TagKey.Get("_ip_from_range_");
		internal static readonly TagKey ipToRangeTK = TagKey.Get("_ip_to_range_");

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
			dialogHandler.AddTable(new GUTATable(1, 0, ButtonMetrics.D_BUTTON_WIDTH));
			dialogHandler.LastTable[0, 0] = GUTAText.Builder.TextHeadline("Pridej blokovany range").XPos((width - 130) / 2).Build();
			dialogHandler.LastTable[0, 1] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonCross).Id(0).Build(); //exit button
			dialogHandler.MakeLastTableTransparent();

			//druhy radek 1.IP adresa
			dialogHandler.AddTable(new GUTATable(1, 0, width - labels));
			dialogHandler.LastTable.RowHeight = ImprovedDialog.D_ROW_HEIGHT;
			dialogHandler.LastTable[0, 0] = GUTAText.Builder.TextLabel("IP adresa od:").Build();
			dialogHandler.LastTable[0, 1] = GUTAInput.Builder.Id(9).Text(ipfrom).Build();
			dialogHandler.MakeLastTableTransparent();

			//treti radek 2.IP adresa
			dialogHandler.AddTable(new GUTATable(1, 0, width - labels));
			dialogHandler.LastTable.RowHeight = ImprovedDialog.D_ROW_HEIGHT;
			dialogHandler.LastTable[0, 0] = GUTAText.Builder.TextLabel("IP adresa do:").Build();
			dialogHandler.LastTable[0, 1] = GUTAInput.Builder.Id(10).Text(ipto).Build();
			dialogHandler.MakeLastTableTransparent();

			//ctvrty radek Duvod
			dialogHandler.AddTable(new GUTATable(1, 0, width - labels));
			dialogHandler.LastTable.RowHeight = ImprovedDialog.D_ROW_HEIGHT;
			dialogHandler.LastTable[0, 0] = GUTAText.Builder.TextLabel("Dùvod:").Build();
			dialogHandler.LastTable[0, 1] = GUTAInput.Builder.Id(11).Build();
			dialogHandler.MakeLastTableTransparent();

			//ctvrty radek Kdo
			dialogHandler.AddTable(new GUTATable(1, 0, width - labels));
			dialogHandler.LastTable.RowHeight = ImprovedDialog.D_ROW_HEIGHT;
			dialogHandler.LastTable[0, 0] = GUTAText.Builder.TextLabel("Zablokoval:").Build();
			dialogHandler.LastTable[0, 1] = GUTAText.Builder.Text(sendTo.Account.Name).Build();
			dialogHandler.MakeLastTableTransparent();

			//paty radek tlacitko OK
			dialogHandler.AddTable(new GUTATable(1, 0));
			dialogHandler.LastTable[0, 0] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonOK).XPos(width / 2).Id(1).Build();
			dialogHandler.MakeLastTableTransparent();

			dialogHandler.WriteOut();

		}
		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			if (gr.pressedButton == 1) {
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
			if (sa != null) {
				DialogArgs newArgs = new DialogArgs();
				if (sa.argv != null && sa.argv.Length == 1) {
					newArgs.SetTag(D_BlockIPRange.ipFromRangeTK, sa.argv[0]);
				} else if (sa.argv != null && sa.argv.Length == 2) {
					newArgs.SetTag(D_BlockIPRange.ipFromRangeTK, sa.argv[0]);
					newArgs.SetTag(D_BlockIPRange.ipToRangeTK, sa.argv[1]);
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