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
	[Summary("Resource as ItemDef")]
	public class ItemResource : IResourceListItemMultiplicable {
		private ItemDef itemDef;
		private double number;
		private string definition;

		internal ItemResource(ItemDef itemDef, double number, string definition) {
			this.number = number;
			this.itemDef = itemDef;
			this.definition = definition;
		}

		#region IResourceListItem Members
		public double DesiredCount {
			get {
				return number;
			}
			set {
				number = value;
			}
		}

		public string Definition {
			get {
				return definition;
			}
		}

		public bool IsSameResource(IResourceListItem newOne) {
			ItemResource newResource = newOne as ItemResource;
			if (newResource != null) {
				if (itemDef == newResource.itemDef) {
					return true;
				}
			}
			return false;
		}
		#endregion

		#region IResourceListItemMultiplicable Members
		public ResourceCounter GetCounter() {
			ItemsCounter ic = Pool<ItemsCounter>.Acquire();//get from pool
			ic.SetParameters(itemDef, number);//initialize
			return ic;
		}
		#endregion
	}
}