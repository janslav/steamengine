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
using System.Collections.Generic;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {
	[Summary("Class for holding one parsed resource list")]
	public class ResourcesList {
		//complete list of resources (separated into number-value pairs in special class)
		private List<IResourceListItem> resourceItemsList = new List<IResourceListItem>();
		//sublist of resources allowing us to use the resource list more than once (consuming) at a time (typically "itemdef" resources)
		private List<IResourceListItemMultiplicable> multiplicablesSubList = new List<IResourceListItemMultiplicable>();
		//sublist of other resources used usually only for "is present" check
		private List<IResourceListItem> nonMultiplicablesSubList = new List<IResourceListItem>();
			
		[Summary("Add new ResourceListItem to the list")]
		internal void Add(IResourceListItem newItem) {			
			if (!Contains(newItem)) {//check if the new resource is not present in the list					
				resourceItemsList.Add(newItem);
				//put the new item also to the special sublist
				if(newItem is IResourceListItemMultiplicable) {
					multiplicablesSubList.Add((IResourceListItemMultiplicable) newItem);
				} else {
					nonMultiplicablesSubList.Add(newItem);
				}
			}
		}

		[Summary("Take a look to the previously added items if any of them is not of the same definition, "+
				 " if yes add just the number value to the found item")]
		private bool Contains(IResourceListItem newItem) {
			//there won't be many items in the list hence repeated iterating is not such a big deal
			foreach (IResourceListItem item in resourceItemsList) {
				if (item.IsSameResource(newItem)) {
					item.DesiredCount += newItem.DesiredCount; //just add the number
					LogStr.Warning("Resource list contains duplicity ("+newItem.Definition+")");
					return true;
				}
			}
			return false;
		}

		[Summary("Check if character has all resources from the resource list")]
		public bool HasResourcesPresent(Character chr, ResourcesLocality where) {
			//first check non-multiplicables (these are easy to check (usually some "Has..." method))
			if (!CheckNonMultiplicableItems(chr)) {
				return false;
			}
			
			//list of resource counters corresponding to the list of "multiplicable" resources
			List<ResourceCounter> resourceCounters = PrepareResourceCounters();
			//then check multiplicables (these may desire some items iterating e.t.c)
			if (!CheckMultiplicableItems(chr, where, resourceCounters)) {
				return false;
			}
			
			return true; //all resources present
		}

		[Summary("Consume the whole resource list from the character (once)")]
		public bool ConsumeResourcesOnce(Character chr, ResourcesLocality where) {
			if (!CheckNonMultiplicableItems(chr)) {
				return false;
			}
			List<ResourceCounter> resourceCounters = PrepareResourceCounters();
			//then check multiplicables (these may desire some items iterating e.t.c)
			if (!CheckMultiplicableItems(chr, where, resourceCounters)) {
				return false;
			}
			//if we are here then there is for every ResourceCounter the multiplicity at least 1 (which is enough for us)
			foreach (ResourceCounter ctr in resourceCounters) {
				ctr.ConsumeItems(1);
			}
			return true;
		}

		[Summary("Consume the whole resource list from the character as many times as possible, return information "+
				"about how many times it has been consumed")]
		public uint ConsumeResources(Character chr, ResourcesLocality where) {
			if (!CheckNonMultiplicableItems(chr)) {
				return 0;
			}
			List<ResourceCounter> resourceCounters = PrepareResourceCounters();
			//then check multiplicables (these may desire some items iterating e.t.c)
			if (!CheckMultiplicableItems(chr, where, resourceCounters)) {
				return 0;
			}
			uint availableOnly = ResListAvailableTimes(resourceCounters);			
			foreach (ResourceCounter ctr in resourceCounters) {
				ctr.ConsumeItems(availableOnly);
			}
			return availableOnly;
		}

		[Summary("Get all item multiplicable resources from the list separated in their own sublist")]		
		public List<IResourceListItemMultiplicable> MultiplicablesSublist {
			get {
				return multiplicablesSubList;
			}
		}

		[Summary("Get all non-multiplicable resources from the list separated in their own sublist")]
		public List<IResourceListItem> NonMultiplicablesSublist {
			get {
				return nonMultiplicablesSubList;
			}
		}
		
		//look to the resource counters and find out how many times we can consume the resource list
		private uint ResListAvailableTimes(List<ResourceCounter> resourceCounters) {
			uint leastMultiplicity = uint.MaxValue;
			foreach (ResourceCounter ctr in resourceCounters) {
				//check every resource counter and get their multiplicities (we will use the lowest one)
				leastMultiplicity = Math.Min(leastMultiplicity, ctr.Multiplicity);
			}
			return leastMultiplicity;
		}

		private List<ResourceCounter> PrepareResourceCounters() {
			List<ResourceCounter> retList = new List<ResourceCounter>();
			foreach (IResourceListItemMultiplicable rli in multiplicablesSubList) {
				retList.Add(rli.GetCounter());
			}
			return retList;
		}

		private bool CheckNonMultiplicableItems(Character chr) {
			foreach (IResourceListItem rli in nonMultiplicablesSubList) {
				if (!rli.IsResourcePresent(chr)) { //first not found resource ends the cycle 
					return false;
				}
			}
			return true;
		}

		//check multiplicable items for their presence in the specified ResourcesLocality, initializes also the 
		//list of ResourceCounters
		private bool CheckMultiplicableItems(Character chr, ResourcesLocality where, List<ResourceCounter> resourceCounters) {
			ResourceItemFinder.LocalizeItems(chr, where, resourceCounters);
			//now check if all resources has been found in adequate amount
			foreach (ResourceCounter ctr in resourceCounters) {
				if (ctr.Multiplicity == 0) {//the desired resource cannot be consumed in desired amount
					return false;
				}
			}
			return true;
		}
	}

	[Summary("Interface for single resource stored in resource lists")]
	public interface IResourceListItem {
		[Summary("Number specified in the script (85.5 hiding, 5 i_apples etc)")]
		double DesiredCount {
			get;
			set;
		}

		[Summary("Original string defining the resource, will be used for finding the resource's def (or stat or whatever)")]
		string Definition {
			get;
		}

		[Summary("Check if the 'newOne' is the same resource as the actual one.")]
		bool IsSameResource(IResourceListItem newOne);

		[Summary("Does the character have this particular resource? (in desired amount). Check only the presence "+
				"do not consume or anything else...")]
		bool IsResourcePresent(Character chr);
	}

	[Summary("Interface for single resource stored in resource lists. These items can be multiplicable - "+
			"e.g. itemdefs, allowing us to say 'how many times the resourcelist has been found at the char's"+
			"(usable for crafting more than 1 item at a time e.t.c)")]
	public interface IResourceListItemMultiplicable : IResourceListItem {
		[Summary("Return the resource counter object for this resource")]
		ResourceCounter GetCounter();
	}		
}