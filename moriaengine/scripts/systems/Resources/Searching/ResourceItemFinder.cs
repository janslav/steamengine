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
	public class ResourceItemFinder {
		private static ResourceItemFinder instance = null;
		private ItemsLocalizer localizer;

		//do not instantiate
		private ResourceItemFinder() {

		}

		//according to the specified locality where to look for the items, set the particular ItemsLocalizer
		//to be used
		private void SetLocalizer(ResourcesLocality where) {
			switch (where) {
				case ResourcesLocality.WearableLayers:
					localizer = WearableLayersItemsLocalizer.GetInstance;
					break;
				case ResourcesLocality.Backpack:
					localizer = BackpackItemsLocalizer.GetInstance;
					break;
				case ResourcesLocality.Bank:
					localizer = BankItemsLocalizer.GetInstance;
					break;
				case ResourcesLocality.BackpackAndBank:
					localizer = BackpackAndBankItemsLocalizer.GetInstance;
					break;
				case ResourcesLocality.Everywhere:
					break;
				case ResourcesLocality.NonSpecified:
				default:
					break;
			}
		}

		[Summary("Get instance of ItemFinder, allowing us to specify 'where' to look for the item")]
		public static ResourceItemFinder GetInstance(ResourcesLocality where) {
			if (instance == null) {
				instance = new ResourceItemFinder();
			}
			instance.SetLocalizer(where);
			return instance;
		}
		
		[Summary("Get all items from the localizer (e.g. all items from chars bank) and look if they are desired "+
				"as resources in the list")]
		public void LocalizeItems(Character chr, List<ResourceCounter> resCountersList) {
			if (localizer != null) {
				localizer.LocalizeItems(chr, resCountersList);
			} else {
				//this should not occur, if we do not have the localizer specified (ResourcesLocality.NonSpecified) 
				//then we shouldn't want to search for any items from the resource list!
				throw new SEException("Unexpected resources searching");
			}
		}
	}	
}