namespace SteamEngine.CompiledScripts {
	[Dialogs.ViewableClass]
	public partial class ShiningItem {
		public override void On_DClick(AbstractCharacter dclicker) {
            this.Say("Shine");
            if (this.Amount > 1) {
                //vytvori hromadku dalsich pochodni s amount-1
                Item restOfTorches = (Item)this.Dupe();
				restOfTorches.Amount = restOfTorches.Amount - 1;
				this.Amount = 1;
            }
			this.Model = 0xa12;
			base.On_DClick(dclicker);
		}
	}

	[Dialogs.ViewableClass]
	public partial class ShiningItemdef {
	}
}