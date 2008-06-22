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
		//list of resource counters corresponding to the list of "multiplicable" resources
		private List<ResourceCounter> resourceCounters = new List<ResourceCounter>();
			
		[Summary("Add new ResourceListItem to the list")]
		internal void Add(IResourceListItem newItem) {			
			if (!Contains(newItem)) {//check if the new resource is not present in the list					
				resourceItemsList.Add(newItem);
				//put the new item also to the special sublist
				if(newItem.GetType().IsAssignableFrom(typeof(IResourceListItemMultiplicable))) {
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
			foreach (IResourceListItem rli in resourceItemsList) {
				if (!rli.IsResourcePresent(chr, where)) { //first not found resource ends the cycle 
					return false;
				}				
			}
			
			//then check multiplicables (these may desire some items iterating e.t.c)
			ResourceItemFinder finder = ResourceItemFinder.GetInstance(where);
			finder.LocalizeItems(chr, resourceCounters);
			//now check if all resources has been found in adequate amount
			foreach (ResourceCounter ctr in resourceCounters) {
				if (ctr.Multiplicity == 0) {//the desired resource cannot be consumed in desired amount
					return false; 
				}
			}
			return true; //all resources present
		}

		[Summary("Consume the whole resource list from the character as many times as specified")]
		public void ConsumeResources(uint howManyTimes) {
			uint availableOnly = ResListAvailableTimes();
			if (howManyTimes > availableOnly) {
				//sanity check, this should not happen if the scripter is wise
				throw new SEException(LogStr.Error("Resource list demanded "+howManyTimes+" times but is available only "+availableOnly+" times")); 
			}
			foreach (ResourceCounter ctr in resourceCounters) {
				ctr.ConsumeItems(howManyTimes);
			}
		}

		[Summary("How many times do we have the whole resource list available? (Usable e.g for massive "+
				"items crafting e.t.c), consuming the resorces more than once")]
		public uint ResListAvailableTimes() {
			uint leastMultiplicity = uint.MaxValue;
			foreach (ResourceCounter ctr in resourceCounters) {
				//check every resource counter and get their multiplicities (we will use the lowest one)
				leastMultiplicity = Math.Min(leastMultiplicity, ctr.Multiplicity);
			}
			return leastMultiplicity;
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

		internal void PrepareResourceCounters() {
			foreach (IResourceListItemMultiplicable rli in multiplicablesSubList) {
				resourceCounters.Add(rli.GetCounter());
			}
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
		bool IsResourcePresent(Character chr, ResourcesLocality where);
	}

	[Summary("Interface for single resource stored in resource lists. These items can be multiplicable - "+
			"e.g. itemdefs, allowing us to say 'how many times the resourcelist has been found at the char's"+
			"(usable for crafting more than 1 item at a time e.t.c)")]
	public interface IResourceListItemMultiplicable {
		[Summary("Return the resource counter object for this resource")]
		ResourceCounter GetCounter();
	}		
}