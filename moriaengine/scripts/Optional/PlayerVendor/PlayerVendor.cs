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

namespace SteamEngine.CompiledScripts {

	[Dialogs.ViewableClass]
	public partial class PlayerVendor {

		public override void On_Create() {
			base.On_Create();


		}

		public void TradeOperationStarted(Player player) {
			//TODO: Ensure initialisation?
		}

		public bool CanTradeWith(Player player) {
			//TODO: check realm, etc.
			return this.IsOperational;
		}

		public bool CanBeControlledBy(Player player) {
			//override pet system?
			return this.IsPetOf(player);
		}


		public bool IsOperational { get { return true; } }


		/// <summary>
		/// Determines whether the specified player can stock the specified item within this vendor.
		/// In the negative fall, sends a sysmessage to the player
		/// </summary>
		/// <param name="player">The player.</param>
		/// <param name="itemBeingStocked">The item being stocked.</param>
		/// <returns>
		///   <c>true</c> if the specified player can stock the specified item within this vendor; otherwise, <c>false</c>.
		/// </returns>
		public bool CanStockWithMessage(Player player, Item itemBeingStocked) {
			if (itemBeingStocked.Type is t_gold) {
				player.SysMessage(Loc<PlayerVendorLoc>.Get(player.Language).YouCantStockMoney);
				return false;
			} else if (itemBeingStocked.RecursiveCount > 500) {
				player.SysMessage(Loc<PlayerVendorLoc>.Get(player.Language).StockedContainerTooFull);
				return false;
			}

			return player.CanPickUpWithMessage(itemBeingStocked);
		}


	}

	[Dialogs.ViewableClass]
	public partial class PlayerVendorDef {
	}

	public class PlayerVendorLoc : CompiledLocStringCollection {
		public string YouCantStockMoney = "Nemùžeš prodávat hotovost";
		public string StockedContainerTooFull = "Nemùžeš prodávat kontejner s více jak 500ks zboží";

	}
}

namespace SteamEngine.CompiledScripts.Dialogs {

	/// <summary>Surprisingly the dialog that will display the RegBox guts</summary>
	public class D_PV : CompiledGumpDef {

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
		}
	}

	public class Targ_PV : CompiledTargetDef {

		protected override void On_Start(Player self, object parameter) {
			base.On_Start(self, parameter);
		}
	}
}