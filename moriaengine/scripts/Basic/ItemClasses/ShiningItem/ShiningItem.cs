using System;
using System.Collections;

namespace SteamEngine.CompiledScripts {
	[Dialogs.ViewableClass]
	public partial class ShiningItem {
		public override void On_DClick(AbstractCharacter dclicker) {
            this.Say("Shine");
            if (this.Amount > 1) {
                //vytvori hromadku dalsich pochodni s amount-1
                //ItemDef shiningItemDef = this.TypeDef.ShiningItemDef;
                this.NewItem(ShiningItemDef,this.Amount-1); //(shiningItemDef, this.Amount-1);//(ShiningItemdef, this.Amount-1);
            }
		}
	}

	[Dialogs.ViewableClass]
	public partial class ShiningItemdef {
	}
}