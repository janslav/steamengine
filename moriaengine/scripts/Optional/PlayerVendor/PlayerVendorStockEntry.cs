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


using SteamEngine.CompiledScripts.Dialogs;


namespace SteamEngine.CompiledScripts {

	[ViewableClass]
	partial class PlayerVendorStockEntry {

		public decimal Price {
			get { return this.price; }
			set { this.price = value; }
		}


		/// <summary>
		/// Gets or sets a value indicating whether this entry is a stock category.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance is category; otherwise, <c>false</c>.
		/// </value>
		public bool IsCategory {
			get { return this.isCategory; }
			set { this.isCategory = value; }
		}

		public bool SoldByUnits {
			get { return this.soldByUnits; }
			set { this.soldByUnits = value; }
		}
	}

	[ViewableClass]
	partial class PlayerVendorStockEntryDef {

	}
}
