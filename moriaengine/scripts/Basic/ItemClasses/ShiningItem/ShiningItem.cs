using System;
using System.Collections;

namespace SteamEngine.CompiledScripts {
	[Dialogs.ViewableClass]
	public partial class ShiningItem {
		public override void On_DClick(AbstractCharacter dclicker) {
                        this.Say("Shine");  
		 //	this.Consume(1);
		 //	this.CreateEmptyFlask(((Character) dclicker).Backpack);
		 //	//TODO? some sound and/or visual effect?
                   
		 // 	base.On_DClick(dclicker);
		}


	}

	[Dialogs.ViewableClass]
	public partial class ShiningItemdef {
	}
}