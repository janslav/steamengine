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

using System.Collections.Generic;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {

	[Dialogs.ViewableClass]
	public partial class PlayerVendor {

		public override void On_Create() {
			base.On_Create();


		}

		//public void TradeOperationStarted(Player player) {
		//    //TODO: Ensure initialisation?
		//}

		public bool CanTradeWithMessage(Player player) {
			//TODO: check realm, etc.
			if (!this.IsOperational) {
				player.WriteLineLoc<PlayerVendorLoc>(l => l.VendorOffline);
				return false;
			}

			return true;
		}

		public bool CanBeControlledBy(Player player) {
			//override pet system?
			return this.IsPetOf(player);
		}

		public bool IsOperational {
			get { return this.isOperational; }
			set { this.isOperational = value; }
		}

		#region Stock methods
		public Container StockRoot {
			get {
				var def = PlayerVendorDef.StockContainerDef;
				var root = this.FindLayer(def.Layer);

				if (root == null) {
					def.Create(this);
				}

				return (Container) root;
			}
		}

		/// <summary>
		/// Determines whether the specified player can stock the specified item within this vendor.
		/// This is the aggregate method, checking everything needed including vendor ownership and reachable state.
		/// In the negative fall, sends a sysmessage to the player.
		/// </summary>
		/// <param name="player">The player.</param>
		/// <param name="thingBeingStocked">The thing being stocked.</param>
		/// <returns>
		///   <c>true</c> if the specified player can stock the specified item within this vendor; otherwise, <c>false</c>.
		/// </returns>
		public bool CanStockWithMessage(Player player, Thing thingBeingStocked) {

			//does player own the vendor?
			if (!this.CanBeControlledBy(player)) {
				player.WriteLineLoc<PlayerVendorLoc>(l => l.VendorIsntYours);
				return false;
			}

			//does the vendor want to talk with the player?
			if (!this.CanTradeWithMessage(player)) {
				return false;
			}

			//is player close to vendor?
			if (!player.CanReachWithMessage(this)) {
				return false;
			}

			var asItem = thingBeingStocked as Item;

			if (asItem != null) {
				//selling item. Check allowed amount and type
				if (asItem.Type is t_gold) {
					player.WriteLineLoc<PlayerVendorLoc>(l => l.YouCantStockMoney);
					return false;
				} else if (asItem.RecursiveCount > 500) {
					player.WriteLineLoc<PlayerVendorLoc>(l => l.StockedContainerTooFull);
					return false;
				}

				//most important check - whether the item is in players reach/possession
				return player.CanPickUpWithMessage(asItem);

			} else {
				//pet being sold - check ownership
				var pet = (Character) thingBeingStocked;

				if (!pet.IsPetOf(player)) {
					player.WriteLineLoc<PlayerVendorLoc>(l => l.ThisNpcIsntYours);
					return false;
				}

				if (!pet.IsAnimal) {
					player.WriteLineLoc<PlayerVendorLoc>(l => l.CanOnlySellAnimals);
					return false;
				}

				//is the pet close enough
				return player.CanReachWithMessage(pet);
			}
		}


		/// <summary>
		/// Adds new stock section.
		/// </summary>
		/// <param name="parentSection">The parent section.</param>
		/// <param name="description">The description.</param>
		/// <param name="model">The model.</param>
		/// <param name="color">The color.</param>
		public void AddNewStockSection(Container parentSection, string description, int model, int color) {
			Sanity.IfTrueThrow(parentSection.TopObj() != this, "parentSection.TopObj() != this");

			var section = PlayerVendorDef.StockContainerDef.Create(parentSection);
			section.Name = description;
			section.Model = model;
			section.Color = color;
		}

		/// <summary>
		/// Stocks a new thing - i.e. puts it into vendor to be sold. CanStockWithMessage or equivalent is supposed to be already called on this.
		/// </summary>
		/// <param name="player">The player.</param>
		/// <param name="thingToStock">The thing to stock.</param>
		/// <param name="parentCategory">The parent category.</param>
		/// <param name="description">The description.</param>
		/// <param name="price">The price.</param>
		public void StockThing(Player player, Thing thingToStock, Container parentCategory, string description, decimal price) {
			var itemToStock = thingToStock as Item;

			var entry = (PlayerVendorStockEntry) PlayerVendorDef.StockEntryDef.Create(parentCategory);
			if (itemToStock != null) {
				try {
					entry.origDescription = description;
					entry.Name = description;
					entry.soldByUnits = false;
					entry.price = price;
					entry.Model = itemToStock.Model;
					entry.Color = itemToStock.Color;

					//we make the player pick up the item and put it down into the entry container already in the vendor, so that all logs and stuff are consistent. We don't use the "Try" methods since CanPickup was supposedly already run
					player.PickupItem(itemToStock, itemToStock.Amount);
					int x, y;
					entry.GetRandomXYInside(out x, out y);
					player.PutItemInItem(entry, x, y, false);

				} catch {
					if (entry.Count == 0) {
						entry.Delete();
					}
					throw;
				}

			} else {
				try {
					var petToStock = (Character) thingToStock;
					petToStock.LogoutFully();

					entry.Link = petToStock;
					entry.soldByUnits = false;
					entry.Name = description;
					entry.origDescription = description;
					entry.price = price;
					entry.Model = petToStock.TypeDef.Icon;
					entry.Color = petToStock.Color;

				} catch {
					if (entry.Link == null) {
						entry.Delete();
					}
				}
			}
		}

		/// <summary>
		/// Stocks a new container of things to be sold by unit. "CanPickup" or similar checks are not being run here, are supposed to be run already.
		/// </summary>
		/// <param name="player">The player.</param>
		/// <param name="units">The units.</param>
		/// <param name="parentCategory">The parent category.</param>
		/// <param name="description">The description.</param>
		/// <param name="price">The price.</param>
		public void StockThingSoldByUnit(Player player, Container units, Container parentCategory, string description, decimal price) {
			int model = -1;
			int color = -1;
			int counter = 0;

			List<Item> approvedItems = new List<Item>(units.Count);

			foreach (Item unit in units) {
				if (player.CanPickup(unit).Allow) {
					approvedItems.Add(unit);

					if (approvedItems.Count == 1) {
						model = unit.Model;
						color = unit.Color;
					}

					counter += unit.Amount;
				}
			}

			if (approvedItems.Count == 0) {
				player.WriteLineLoc<PlayerVendorLoc>(l => l.NoUnitAvailable);
				return;
			}

			var entry = (PlayerVendorStockEntry) PlayerVendorDef.StockEntryDef.Create(parentCategory);
			try {
				entry.origDescription = description;
				entry.Name = SoldByUnitEntryName(counter, description);
				entry.soldByUnits = true;
				entry.price = price;
				entry.Model = model;
				entry.Color = color;

				foreach (var approved in approvedItems) {
					//we make the player pick up every item and put it down into the entry container already in the vendor, so that all logs and stuff are consistent. We don't use the "Try" methods since we already ran CanPickup
					player.PickupItem(approved, approved.Amount);
					int x, y;
					entry.GetRandomXYInside(out x, out y);
					player.PutItemInItem(entry, x, y, false);
				}

			} catch {
				if (entry.Count == 0) {
					entry.Delete();
				}
				throw;
			}
		}

		private static string SoldByUnitEntryName(int counter, string description) {
			return string.Concat(
				counter.ToString(System.Globalization.CultureInfo.InvariantCulture),
				"x ", description);
		}
		#endregion
	}

	[Dialogs.ViewableClass]
	public partial class PlayerVendorDef {

		private static ContainerDef i_playervendor_stock_container;
		public static ContainerDef StockContainerDef {
			get {
				if (i_playervendor_stock_container == null) {
					i_playervendor_stock_container = (ContainerDef) ThingDef.GetByDefname("i_playervendor_stock_container");
				}
				return i_playervendor_stock_container;
			}
		}

		private static PlayerVendorStockEntryDef i_playervendor_stock_entry;
		public static PlayerVendorStockEntryDef StockEntryDef {
			get {
				if (i_playervendor_stock_entry == null) {
					i_playervendor_stock_entry = (PlayerVendorStockEntryDef) ThingDef.GetByDefname("i_playervendor_stock_entry");
				}
				return i_playervendor_stock_entry;
			}
		}
	}

	public class PlayerVendorLoc : CompiledLocStringCollection {
		public string YouCantStockMoney = "Nemùžeš prodávat hotovost";
		public string StockedContainerTooFull = "Nemùžeš prodávat kontejner s více jak 500ks zboží";

		public string NoUnitAvailable = "Kontejner pro jednotkový prodej musíš mít otevøený a v nìm alespoò 1 pøedmìt.";

		public string VendorOffline = "Tento prodejce má zavøeno";
		public string VendorIsntYours = "Tento prodejce ti není podøízen.";

		public string ThisNpcIsntYours = "Tento tvor ti nepatøí.";
		public string CanOnlySellAnimals = "Prodávat lze jen zvíøata";
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