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

	public class D_PlayerVendor_Stock_Input : CompiledGumpDef {
		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {

			string header, defaultName;

			var asItem = focus as Item;
			if (asItem != null) {
				int amount = asItem.Amount;
				header = string.Format(Loc<Loc_PlayerVendor_Stock_Input>.Get(sendTo.Language).StockDialogHeader_Item,
					amount, asItem.Name);
				if (amount == 1) {
					defaultName = asItem.Name;
				} else {
					defaultName = string.Concat(amount, " ", asItem.Name);
				}
			} else {
				header = string.Format(Loc<Loc_PlayerVendor_Stock_Input>.Get(sendTo.Language).StockDialogHeader_Char,
					focus.Name);
				defaultName = focus.Def.Name;
			}


			var dialogHandler = new ImprovedDialog(this.GumpInstance);

			//create the background GUTAMatrix and set its size       
			dialogHandler.CreateBackground(400);
			dialogHandler.SetLocation(100, 175);

			//first row - the label of the dialog
			dialogHandler.AddTable(new GUTATable(1, 0, ButtonMetrics.D_BUTTON_WIDTH));
			dialogHandler.LastTable.AddToCell(0, 0, GUTAText.Builder.TextHeadline(header).Build());
			dialogHandler.LastTable.AddToCell(0, 1, GUTAButton.Builder.Type(LeafComponentTypes.ButtonCross).Id(0).Build());
			dialogHandler.MakeLastTableTransparent();

			//second row - the basic, whole row, input field
			dialogHandler.AddTable(new GUTATable(1, 0));
			dialogHandler.LastTable.RowHeight = ImprovedDialog.D_ROW_HEIGHT;
			dialogHandler.LastTable.AddToCell(0, 0, GUTAInput.Builder.Id(1).Text(defaultName).Build());
			dialogHandler.MakeLastTableTransparent();

			//last row with buttons
			dialogHandler.AddTable(new GUTATable(1, ButtonMetrics.D_BUTTON_WIDTH, 0));
			dialogHandler.LastTable.AddToCell(0, 0, GUTAButton.Builder.Type(LeafComponentTypes.ButtonOK).Id(1).Build());
			dialogHandler.MakeLastTableTransparent();

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
		public string StockDialogHeader_Item = "Na prodej: {0} {1}";
		public string StockDialogHeader_Char = "Na prodej: {0}";
	}
}
