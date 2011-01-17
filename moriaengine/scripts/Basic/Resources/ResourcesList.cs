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
using System.Linq;
using System.Collections.Generic;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {
	[Summary("Class for holding one parsed resource list")]
	public class ResourcesList {
		//complete list of resources (separated into number-value pairs in special class)
		private readonly IResourceListEntry[] resourceItemsList;

		//sublist of resources allowing us to use the resource list more than once (consuming) at a time (typically "itemdef" resources)
		private readonly IResourceListEntry_ItemCounter[] multiplicablesSubList;
		//sublist of other resources used usually only for "is present" check
		private readonly IResourceListEntry_Simple[] nonMultiplicablesSubList;


		public ResourcesList(IEnumerable<IResourceListEntry> entries) {
			var list = new List<IResourceListEntry>();
			foreach (var entry in entries) {
				if (list.Any(i => i.IsSameResource(entry))) {
					throw new SEException("Resource list contains duplicity (" + entry.ParsableString + ")");
				}
				list.Add(entry);
			}

			this.resourceItemsList = list.ToArray();

			//put the new items also to the specialised sublists
			this.multiplicablesSubList = list.OfType<IResourceListEntry_ItemCounter>().OrderBy(e => -e.DesiredCount).ToArray();
			this.nonMultiplicablesSubList = list.OfType<IResourceListEntry_Simple>().OrderBy(e => -e.DesiredCount).ToArray();
		}

		[Summary("Check if character has all resources from the resource list. " +
				"in case some resource is missing, it is set to the output variable.")]
		public bool HasResourcesPresent(Character chr, ResourcesLocality where, out IResourceListEntry firstMissingResource) {
			//first check non-multiplicables (these are easy to check (usually some "Has..." method))
			if (!CheckSimpleEntries(chr, out firstMissingResource)) {
				return false;
			}

			//list of resource counters corresponding to the list of "multiplicable" resources
			var resourceCounters = PrepareResourceCounters();
			try {
				//then check multiplicables (these may desire some items iterating e.t.c)
				if (!CheckEntriesWithCounters(chr, where, resourceCounters, out firstMissingResource)) {
					return false;
				}
				return true; //all resources present

			} finally {
				//dispose counters
				DisposeAll(resourceCounters);
			}
		}

		[Summary("Consume the whole resource list from the character (once) " +
				"in case some resource is missing, it is set to the output variable.")]
		public bool ConsumeResourcesOnce(Character chr, ResourcesLocality where, out IResourceListEntry firstMissingResource) {
			if (!CheckSimpleEntries(chr, out firstMissingResource)) {
				return false;
			}
			var resourceCounters = PrepareResourceCounters();
			try {
				//then check multiplicables (these may desire some items iterating e.t.c)
				if (!CheckEntriesWithCounters(chr, where, resourceCounters, out firstMissingResource)) {
					return false;
				}
				//if we are here then there is for every ResourceCounter the multiplicity at least 1 (which is enough for us)
				foreach (ItemCounter ctr in resourceCounters) {
					ctr.ConsumeItems(1);
				}
				return true;

			} finally {
				//dispose counters
				DisposeAll(resourceCounters);
			}
		}

		[Summary("Consume the whole resource list from the character as many times as possible, return information " +
				"about how many times it has been consumed " +
				".In case some resource is missing, it is set to the output variable.")]
		public double ConsumeResources(Character chr, ResourcesLocality where, out IResourceListEntry firstMissingResource) {
			if (!CheckSimpleEntries(chr, out firstMissingResource)) {
				return 0;
			}
			var resourceCounters = PrepareResourceCounters();
			try {
				//then check multiplicables (these may desire some items iterating e.t.c)
				if (!CheckEntriesWithCounters(chr, where, resourceCounters, out firstMissingResource)) {
					//dispose counters
					return 0;
				}
				double availableOnly = Math.Floor(ResListAvailableTimes(resourceCounters));
				foreach (ItemCounter ctr in resourceCounters) {
					ctr.ConsumeItems(availableOnly);
				}
				return availableOnly;

			} finally {
				//dispose counters
				DisposeAll(resourceCounters);
			}
		}

		[Summary("Consume some resources from the resource list from the character " +
				"the resource is consumed only if present, the amount consumed will vary between 0 and the amount available (but not more than desired amount in the list)")]
		public void ConsumeSomeResources(Character chr, ResourcesLocality where) {

			var resourceCounters = PrepareResourceCounters();
			try {
				//find the resources references
				ResourceItemFinder.LocalizeItems(chr, where, resourceCounters);

				//now for each counter consume 0-available items
				foreach (ItemCounter ctr in resourceCounters) {
					ctr.ConsumeSomeItems();
				}

			} finally {
				//dispose counters
				DisposeAll(resourceCounters);
			}
		}

		private static void DisposeAll<T>(T[] disposables) where T : IDisposable {
			foreach (IDisposable disposable in disposables) {
				disposable.Dispose();
			}
		}

		[Summary("Get all item multiplicable resources from the list separated in their own sublist")]
		public IEnumerable<IResourceListEntry_ItemCounter> MultiplicablesSublist {
			get {
				return this.multiplicablesSubList;
			}
		}

		[Summary("Get all non-multiplicable resources from the list separated in their own sublist")]
		public IEnumerable<IResourceListEntry_Simple> NonMultiplicablesSublist {
			get {
				return nonMultiplicablesSubList;
			}
		}

		//look to the resource counters and find out how many times we can consume the resource list
		private double ResListAvailableTimes(ItemCounter[] resourceCounters) {
			double leastMultiplicity = int.MaxValue;
			foreach (ItemCounter ctr in resourceCounters) {
				//check every resource counter and get their multiplicities (we will use the lowest one)
				leastMultiplicity = Math.Min(leastMultiplicity, ctr.Multiplicity);
			}
			return leastMultiplicity;
		}

		private ItemCounter[] PrepareResourceCounters() {
			int n = this.multiplicablesSubList.Length;
			ItemCounter[] retList = new ItemCounter[n];

			for (int i = 0; i < n; i++) {
				IResourceListEntry_ItemCounter rli = this.multiplicablesSubList[i];
				retList[i] = rli.GetCounter();
			}
			return retList;
		}

		private bool CheckSimpleEntries(Character chr, out IResourceListEntry missingResource) {
			foreach (IResourceListEntry_Simple rli in this.nonMultiplicablesSubList) {
				if (!rli.IsResourcePresent(chr)) { //first not found resource ends the cycle
					missingResource = rli; //set the missing resource as the returning value for further purposes (such as message)
					return false;
				}
			}
			missingResource = null;
			return true;
		}

		//check multiplicable items for their presence in the specified ResourcesLocality, initializes also the 
		//list of ResourceCounters
		private bool CheckEntriesWithCounters(Character chr, ResourcesLocality where, ItemCounter[] resourceCounters, out IResourceListEntry missingResource) {
			ResourceItemFinder.LocalizeItems(chr, where, resourceCounters);
			//now check if all resources has been found in adequate amount
			foreach (ItemCounter ctr in resourceCounters) {
				if (ctr.Multiplicity < 1) {//the desired resource cannot be consumed in desired amount
					missingResource = ctr.listEntry; //return the missing resource item
					return false;
				}
			}
			missingResource = null;
			return true;
		}

		[Summary("Make a string containing counts and names of all resource list items")]
		public override string ToString() {
			return this.ToParsableString();
		}

		[Summary("Make a string containing counts and (pretty)defnames of all resource list items")]
		public string ToParsableString() {
			int n = this.resourceItemsList.Length;
			string[] arr = new string[n];
			for (int i = 0; i < n; i++) {
				var entry = this.resourceItemsList[i];
				arr[n] = string.Concat(entry.DesiredCount.ToString(System.Globalization.CultureInfo.InvariantCulture), " ",
					entry.AsPercentage ? "%" : "", entry.ParsableString);
			}
			return string.Join(", ", arr);
		}

		//[Summary("From the given resources list get the first resourcelistitem that is of the desired (T)ype and " +
		//        " that's underlaying resource fulfils the given criteria - for SkillResource the it is the skill key, for " +
		//        " AbilityResource or ItemResource it is the defname etc.")]
		//public static T GetResource<T, U>(List<U> list, string criteria)
		//    where T : IResourceListItem //type of resource we are looking for (specific)
		//    where U : IResourceListItem {//type of resources that are in the list (multiplicable or nonmultiplicable as an interface...)
		//    foreach (IResourceListItem itm in list) {
		//        if (typeof(T) == itm.GetType()) {
		//            if (criteria == null) {
		//                return (T) itm; //no criteria, return the first corresponding resource found
		//            }
		//            SkillDef skl;
		//            if (ResourcesListParser.IsSkillResource(criteria, out skl)) {
		//                return (T) itm;//this resource is a skill of a correct key ('criteria')
		//            }
		//            ItemDef itdef;
		//            if (ResourcesListParser.IsItemResource(criteria, out itdef)) {
		//                return (T) itm;//this resource is an item of a correct defname ('criteria')
		//            }
		//            TriggerGroup tgr;
		//            if (ResourcesListParser.IsTriggerGroupResource(criteria, out tgr)) {
		//                return (T) itm;//this resource is a trigger group of a correct defname ('criteria')
		//            }
		//            AbilityDef abl;
		//            if (ResourcesListParser.IsAbilityResource(criteria, out abl)) {
		//                return (T) itm;//this resource is an ability of a correct defname ('criteria')
		//            }
		//            //try stats
		//            if ((criteria.Equals("str", StringComparison.InvariantCultureIgnoreCase)) ||
		//                (criteria.Equals("dex", StringComparison.InvariantCultureIgnoreCase)) ||
		//                (criteria.Equals("int", StringComparison.InvariantCultureIgnoreCase)) ||
		//                (criteria.Equals("vit", StringComparison.InvariantCultureIgnoreCase))) {
		//                return (T) itm;
		//            }
		//        }
		//    }
		//    return default(T);
		//}
	}
}
