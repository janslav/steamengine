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
	[Summary("Counter of triggergroups resources")]
	public class ItemsCounter : ResourceCounter {
		private ItemDef toLookFor;

		//internal ItemsCounter(ItemDef iDefToLookFor, double desiredCount)
		//	: base(desiredCount) {
		//	this.toLookFor = iDefToLookFor;
		//}

		[Summary("Method for setting the counters parameters (used after acquiring from pool)")]
		internal void SetParameters(ItemDef toLookFor, double desiredCount) {
			this.toLookFor = toLookFor;
			this.desiredCount = desiredCount;
		}

		internal override bool ItemCorresponds(Item itm) {
			return (itm.TypeDef == toLookFor);			
		}

		//clear the item def reference (the rest of the clearing is on parent)
		protected override void On_Reset() {
			base.On_Reset();
			toLookFor = null;
		}
	}
}