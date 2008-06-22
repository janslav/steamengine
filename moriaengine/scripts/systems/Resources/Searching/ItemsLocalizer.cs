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
	[Summary("Parent for all possible item-searching and resource counters preparing classes")]
	public abstract class ItemsLocalizer {
		[Summary("Go through all items in the specified locality and check them by every ResourceCounter for " +
				"every resource list item from the list")]
		internal abstract void LocalizeItems(Character chr, List<ResourceCounter> resCountersList);

		//check all items in the specified item if they are among the reslist items in the specified list
		//can be called recursively (useful for containers :) )
		protected void CheckItemsInside(Item itm, List<ResourceCounter> resCountersList) {
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
		protected void CheckByCounters(Item itm, List<ResourceCounter> resCountersList) {
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
						throw new SEException(LogStr.Error("Item "+itm.ToString()+" corresponds to more than one resource from the list"));
					}
					counterAlreadyFound = true;
					resCntr.IncludeItem(itm);
				}
			}
		}
	}
}