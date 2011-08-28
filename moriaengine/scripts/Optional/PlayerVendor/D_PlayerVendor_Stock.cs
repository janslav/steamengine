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


using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;
namespace SteamEngine.CompiledScripts {

	public class D_PlayerVendor_Stock : CompiledGumpDef {
		const int inputId_Price = 1;
		const int inputId_Description = 2;
		const int inputId_SoldByUnit = 3;

		const int buttonId_Sell = 1;
		const int buttonId_SellByUnit = 2;
		const int buttonId_NewSection = 3;


		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {

			var vendor = (PlayerVendor) args[0];
			var stockEntry = (PlayerVendorStockEntry) args[1];

			decimal price = 0;
			int amount = 1;
			string header, description;
			bool enableSoldByUnit = false;

			var asItem = focus as Item;

			if (stockEntry != null) {
				price = stockEntry.Price;
				description = stockEntry.Name;

				if (asItem != null) {
					amount = stockEntry.RecursiveCount;
				}
			} else if (asItem != null) {
				amount = asItem.Amount;
			}

			if (asItem != null) {
				enableSoldByUnit = asItem.IsContainer;

				header = string.Format(Loc<Loc_PlayerVendor_Stock_Input>.Get(sendTo.Language).Header_Item,
					amount, asItem.Name);
				if (amount == 1) {
					description = asItem.Name;
				} else {
					description = string.Concat(amount, " ", asItem.Name);
				}
			} else {
				header = string.Format(Loc<Loc_PlayerVendor_Stock_Input>.Get(sendTo.Language).Header_Char,
					focus.Def.Name);
				description = focus.Def.Name;
			}


			var dialogHandler = new ImprovedDialog(this.GumpInstance);

			//create the background GUTAMatrix and set its size       
			dialogHandler.CreateBackground(400);
			dialogHandler.SetLocation(100, 175);

			//header
			var t = dialogHandler.AddTable(new GUTATable(1, 0, ButtonMetrics.D_BUTTON_WIDTH));
			t.AddToCell(0, 0, GUTAText.Builder.TextHeadline(header).Build());
			t.AddToCell(0, 1, GUTAButton.Builder.Type(LeafComponentTypes.ButtonCross).Id(0).Build());
			t.Transparent = true;

			//textentries: description, price
			t = dialogHandler.AddTable(new GUTATable(2, 80, 0));
			t.RowHeight = ImprovedDialog.D_ROW_HEIGHT;
			t.AddToCell(0, 0, GUTAText.Builder.Text(Loc<Loc_PlayerVendor_Stock_Input>.Get(sendTo.Language).Label_Description).Build());
			t.AddToCell(0, 1, GUTAInput.Builder.Id(inputId_Description).Text(description).Build());
			t.AddToCell(1, 0, GUTAText.Builder.Text(Loc<Loc_PlayerVendor_Stock_Input>.Get(sendTo.Language).Label_Price).Build());
			t.AddToCell(1, 1, GUTAInput.Builder.Id(inputId_Price).Text(price.ToString()).Type(LeafComponentTypes.InputNumber).Build());
			t.Transparent = true;

			//last row with buttons
			t = dialogHandler.AddTable(new GUTATable(1, 30, 70, 30, 120, 30, 0));
			t.AddToCell(0, 0, GUTAButton.Builder.Id(buttonId_Sell).Build());
			t.AddToCell(0, 1, GUTAText.Builder.Text(Loc<Loc_PlayerVendor_Stock_Input>.Get(sendTo.Language).Label_Sell).Build());
			t.AddToCell(0, 2, GUTAButton.Builder.Id(buttonId_Sell).Build());
			t.AddToCell(0, 3, GUTAText.Builder.Text(Loc<Loc_PlayerVendor_Stock_Input>.Get(sendTo.Language).Label_SoldByUnits).Build());
			t.AddToCell(0, 4, GUTAButton.Builder.Id(buttonId_Sell).Build());
			t.AddToCell(0, 5, GUTAText.Builder.Text(Loc<Loc_PlayerVendor_Stock_Input>.Get(sendTo.Language).Label_NewSection).Build());
			t.Transparent = true;


			dialogHandler.WriteOut();

			//100,175
			//argo.tag(sirka,400)
			//argo.tag(vyska,<eval (4*d_def_radek_vyska)+(5*d_def_skvira)+(2*d_def_okraj)>)
			//argo.dialog_prvni
			//argo.dialog_pozadi(<argo.tag(nexty)>,1)
			//argo.dialog_pozadi(<argo.tag(nexty)>,1,80)
			//argo.dialog_pozadi(<argo.tag(nexty)>,1,80)
			//argo.dialog_pozadi(<argo.tag(nexty)>,1)
			//argo.dialog_zpruhledni
			//if (isitem)
			// argo.texta(<argo.dialog_textpos(0,0)>,0481,Na prodej: <amount> <name>)
			// argo.settext(10,<qval((amount==1), ,<amount>)><name>)
			//else
			// argo.texta(<argo.dialog_textpos(0,0)>,0481,Na prodej: <name>)
			// argo.settext(10,<typedef.name>)
			//endif
			//argo.text(<argo.dialog_textpos(0,0)>,0481,100)
			//
			//argo.settext(110,<?EVAL <src.findID(i_vendor_stock).tag.PRICE>?>)
			//argo.texta(<argo.dialog_textpos(1,0)>,0481,Popis)
			//argo.textentry(<argo.dialog_textpos(1,1)>,argo.tag(sirka)-(d_Def_odsazeni+(2*(d_def_okraj+d_def_skvira))),d_def_radek_vyska,0481,0,10)
			//argo.texta(<argo.dialog_textpos(2,0)>,0481,Cena)
			//argo.textentry(<argo.dialog_textpos(2,1)>,argo.tag(sirka)-(d_Def_odsazeni+(2*(d_def_okraj+d_def_skvira))),d_def_radek_vyska,0481,1,110)
			//
			//button((<argo.tag(sirka)>-<d_def_okraj>)-33,<argo.tag(obj_y[0])>,0fb1,0fb3,1,0,9)//cancel
			//
			//argo.dialog_textpos(3,0,1)
			//argo.button(lastxpos,lastypos-1,9905,9904,1,0,3)
			//argo.textA(lastxpos+30,lastypos,2301,Prodat)
			//if (iscontainer) && !(ID==i_truhla_heslo)
			// argo.button(lastxpos+100,lastypos-1,9905,9904,1,0,1)
			// argo.textA(lastxpos+130,lastypos,2301,Jednotkovy prodej)
			// argo.button(lastxpos+250,lastypos-1,9905,9904,1,0,2)
			// argo.textA(lastxpos+280,lastypos,2301,Nova sekce)
			//endif
		}
		//[DIALOG d_playerVendor_stock_input TEXT]

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {

		}
	}

	public class Loc_PlayerVendor_Stock_Input : CompiledLocStringCollection {
		public string Header_Item = "Na prodej: {0} {1}";
		public string Header_Char = "Na prodej: {0}";
		public string Label_Price = "Cena";
		public string Label_Description = "Popis";
		public string Label_Sell = "Prodej";
		public string Label_SoldByUnits = "Jednotkov� prodej";
		public string Label_NewSection = "Nov� sekce";
	}
}
