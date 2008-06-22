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
using System.Collections;
using System.Collections.Generic;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {
	[Summary("Class for finding items in characters bank")]
	public class BankItemsLocalizer : ItemsLocalizer {
		private static BankItemsLocalizer instance;

		private BankItemsLocalizer() {
			//private constructor, it is enough to have this class only once as a singleton
		}

		[Summary("Iterate through all items in character's bank, check if it is in the resource items list " +
				"and if so, prepare the ResourceCounter object for it. Do it recurcively for inner containers")]
		internal override void LocalizeItems(Character chr, List<ResourceCounter> resCountersList) {
			CheckItemsInside((Item)chr.FindLayer(LayerNames.Bankbox), resCountersList);
		}

		internal static BankItemsLocalizer GetInstance {
			get {
				if (instance == null) {
					instance = new BankItemsLocalizer();
				}
				return instance;
			}
		}
	}
}