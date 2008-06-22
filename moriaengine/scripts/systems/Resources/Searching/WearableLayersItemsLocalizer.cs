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
	[Summary("Class for finding items in characters wearable layers")]
	public class WearableLayersItemsLocalizer : ItemsLocalizer {
		private static WearableLayersItemsLocalizer instance;

		private WearableLayersItemsLocalizer() {
			//private constructor, it is enough to have this class only once as a singleton
		}

		[Summary("Iterate through all items in character's wearable layers check if it is in the resource items list " +
				"and if so, prepare the ResourceCounter object for it")]
		internal override void LocalizeItems(Character chr, List<ResourceCounter> resCountersList) {
			CheckByCounters((Item) chr.FindLayer(LayerNames.Arms), resCountersList);
			CheckByCounters((Item) chr.FindLayer(LayerNames.Bracelet), resCountersList);
			CheckByCounters((Item) chr.FindLayer(LayerNames.Cape), resCountersList);
			CheckByCounters((Item) chr.FindLayer(LayerNames.Collar), resCountersList);
			CheckByCounters((Item) chr.FindLayer(LayerNames.Dragging), resCountersList);
			CheckByCounters((Item) chr.FindLayer(LayerNames.Earrings), resCountersList);
			CheckByCounters((Item) chr.FindLayer(LayerNames.Gloves), resCountersList);
			CheckByCounters((Item) chr.FindLayer(LayerNames.Gorget), resCountersList);
			CheckByCounters((Item) chr.FindLayer(LayerNames.Half_apron), resCountersList);
			CheckByCounters((Item) chr.FindLayer(LayerNames.Hand1), resCountersList);
			CheckByCounters((Item) chr.FindLayer(LayerNames.Hand2), resCountersList);
			CheckByCounters((Item) chr.FindLayer(LayerNames.Helmet), resCountersList);
			CheckByCounters((Item) chr.FindLayer(LayerNames.Chest), resCountersList);
			CheckByCounters((Item) chr.FindLayer(LayerNames.Leggins), resCountersList);
			CheckByCounters((Item) chr.FindLayer(LayerNames.Light), resCountersList);
			CheckByCounters((Item) chr.FindLayer(LayerNames.Pants), resCountersList);
			CheckByCounters((Item) chr.FindLayer(LayerNames.Ring), resCountersList);
			CheckByCounters((Item) chr.FindLayer(LayerNames.Robe), resCountersList);
			CheckByCounters((Item) chr.FindLayer(LayerNames.Shirt), resCountersList);
			CheckByCounters((Item) chr.FindLayer(LayerNames.Shoes), resCountersList);
			CheckByCounters((Item) chr.FindLayer(LayerNames.Skirt), resCountersList);
			CheckByCounters((Item) chr.FindLayer(LayerNames.Tunic), resCountersList);			
		}

		internal static WearableLayersItemsLocalizer GetInstance {
			get {
				if (instance == null) {
					instance = new WearableLayersItemsLocalizer();
				}
				return instance;
			}
		}
	}
}