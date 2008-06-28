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
	[Summary("Abstract class - parent of counter classes for multiplicable resources (such as Items, TriggerGroups) " +
			"used for storing information about found char's corresponding items and their amount " +
			"allowing us also to consume the desired amount of found item's")]
	public abstract class ResourceCounter : Poolable {
		protected double desiredCount; //how many item occurences we need? (e.g. 5 i_apple)
		private List<Item> foundItems = new List<Item>();
		private uint foundCount; //how many occurences have been found so far? (counted from amounts of found items)

		internal ResourceCounter() {
			//non parametric constructor for pooling
			this.foundCount = 0;
		}

		//protected ResourceCounter(double desiredCount) : this() {
		//	this.desiredCount = desiredCount;			
		//}

		[Summary("Check if the given item corresponds to this particular resource item (i.e. is of the same " +
				"itemdef, has the desired triggergroup (as type) etc)")]
		internal abstract bool ItemCorresponds(Item itm);

		[Summary("Add the item to the found items list and count its amount for resource's multiplicity determining")]
		internal void IncludeItem(Item itm) {
			foundItems.Add(itm); //add to the list
			foundCount += itm.Amount;
		}

		[Summary("How many times can we consume the desired amount of resources?")]
		internal uint Multiplicity {
			get {
				return (uint) (foundCount / desiredCount);
			}
		}

		[Summary("Look to the foundItems list and consume desiredCount*howManyTimes items found inside. "+
				"These should be available via their 'amounts'")]
		internal void ConsumeItems(uint howManyTimes) {
			uint toConsume = (uint)(desiredCount * howManyTimes);
			foreach (Item itm in foundItems) {
				//try consume as much as possible of this item
				uint wasConsumed = itm.Consume(toConsume);
				toConsume -= wasConsumed;
				if (toConsume == 0) {
					break; //desired amount has been already consumed, stop iterating
				}
			}
		}

		//clear the desired count, the found count information and clear the 
		//list of found items
		protected override void On_Reset() {
			base.On_Reset();
			desiredCount = 0;
			foundCount = 0;
			foundItems.Clear();//clear the list (but let it keep its actual size!)
		}
	}
}