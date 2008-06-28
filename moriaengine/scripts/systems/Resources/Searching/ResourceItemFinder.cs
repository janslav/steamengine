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
	[Summary("Class for finding items from the resourcelist")]
	public static class ResourceItemFinder {
		[Summary("Get all items from the character in specified locality (e.g. all items from chars bank) and look if they are desired "+
				"as resources in the list - initiate the resource counters list")]
		internal static void LocalizeItems(Character chr, ResourcesLocality where, List<ResourceCounter> resCountersList) {
			switch (where) {
				case ResourcesLocality.WearableLayers:
					LocalizeWearableItems(chr, resCountersList);
					break;
				case ResourcesLocality.Backpack:
					LocalizeBackpackItems(chr, resCountersList);
					break;
				case ResourcesLocality.Bank:
					LocalizeBankItems(chr, resCountersList);
					break;
				case ResourcesLocality.BackpackAndBank:
					//try two locations
					LocalizeBackpackItems(chr, resCountersList);
					LocalizeBankItems(chr, resCountersList);
					break;
				case ResourcesLocality.Everywhere:
					//try all three locations
					LocalizeWearableItems(chr, resCountersList);
					LocalizeBackpackItems(chr, resCountersList);
					LocalizeBankItems(chr, resCountersList);
					break;
				case ResourcesLocality.NonSpecified:
					//dont do anything
				default:
					break;
			}
		}

		[Summary("Iterate through all items in character's wearable layers check if it is in the resource items list " +
				"and if so, prepare the ResourceCounter object for it")]
		private static void LocalizeWearableItems(Character chr, List<ResourceCounter> resCountersList) {
			foreach (Item i in chr.GetVisibleEquip()) {
				switch ((LayerNames) i.Z) {
					case LayerNames.Arms:
					case LayerNames.Bracelet:
					case LayerNames.Cape:
					case LayerNames.Collar:
					case LayerNames.Dragging:
					case LayerNames.Earrings:
					case LayerNames.Gloves:
					case LayerNames.Half_apron:
					case LayerNames.Hand1:
					case LayerNames.Hand2:
					case LayerNames.Helmet:
					case LayerNames.Chest:
					case LayerNames.Leggins:
					case LayerNames.Light:
					case LayerNames.Pants:
					case LayerNames.Ring:
					case LayerNames.Robe:
					case LayerNames.Shirt:
					case LayerNames.Shoes:
					case LayerNames.Skirt:
					case LayerNames.Tunic:
						CheckByCounters(i, resCountersList);
						break;
				}
			}
		}

		[Summary("Iterate through all items in character's backpack check if it is in the resource items list " +
				"and if so, prepare the ResourceCounter object for it. Do it recursively for inner containers")]
		private static void LocalizeBackpackItems(Character chr, List<ResourceCounter> resCountersList) {
			CheckItemsInside(chr.BackpackAsContainer, resCountersList);
		}

		[Summary("Iterate through all items in character's bank, check if it is in the resource items list " +
				"and if so, prepare the ResourceCounter object for it. Do it recurcively for inner containers")]
		private static void LocalizeBankItems(Character chr, List<ResourceCounter> resCountersList) {
			CheckItemsInside((Item) chr.FindLayer(LayerNames.Bankbox), resCountersList);
		}		

		//check all items in the specified item if they are among the reslist items in the specified list
		//can be called recursively (useful for containers :) )
		private static void CheckItemsInside(Item itm, List<ResourceCounter> resCountersList) {
			IEnumerator<AbstractItem> insideItems = itm.GetEnumerator();
			while (insideItems.MoveNext()) {
				Item oneItem = (Item) insideItems.Current;
				CheckByCounters(oneItem, resCountersList);
			}
			insideItems.Reset(); //reset the enumerator to the beginning
			while (insideItems.MoveNext()) {
				Item oneItem = (Item) insideItems.Current;
				if (oneItem.IsContainer) {
					//call recursively searching through the items inside this particular container item
					CheckItemsInside(oneItem, resCountersList);
				}
			}
		}

		//for every ResourceCounter in the list check, if this item corresponds to its resource, if so
		//include this item in the counter...
		//remember that the item can be null if we are checking items from layers
		private static void CheckByCounters(Item itm, List<ResourceCounter> resCountersList) {
			if (itm == null) {
				return;
			}
			bool counterAlreadyFound = false;
			foreach (ResourceCounter resCntr in resCountersList) {
				if (resCntr.ItemCorresponds(itm)) {
					if (counterAlreadyFound) {
						//if this happens, it means that the Item corresponds to some ItemsCounter 
						//and TriggerGroupCounter together which we are not able to handle => this is 
						//error in resource list definition and scripter should be executed!
						//e.g. 5 i_apple, 3 t_fruit e.t.c
						//this will not occur for 5 i_apple, 3 i_apple because this situation is handled during
						//reslist parsing (and result is that we are looking for 8 i_apple) 
						throw new SEException(LogStr.Error("Item " + itm.ToString() + " corresponds to more than one resource from the list"));
					}
					counterAlreadyFound = true;
					resCntr.IncludeItem(itm);
				}
			}
		}
	}	
}