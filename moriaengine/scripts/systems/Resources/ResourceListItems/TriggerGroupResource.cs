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
	[Summary("Resource as TriggerGroup (used only for 'type' checking of char's items), we dont use "+
			"resource triggergroups for checking character's available triggergroups!")]
	public class TriggerGroupResource : IResourceListItemMultiplicable {
		private TriggerGroup triggerGroup;
		private double number;
		private string definition;

		internal TriggerGroupResource(TriggerGroup triggerGroup, double number, string definiton) {
			this.number = number;			
			this.triggerGroup = triggerGroup;
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
			TriggerGroupResource newResource = newOne as TriggerGroupResource;
			if (newResource != null) {
				if (triggerGroup == newResource.triggerGroup) {
					return true;
				}
			}
			return false;
		}

		public bool IsResourcePresent(Character chr, ResourcesLocality where) {
			//we dont care "where", a TG is a TG
			return chr.HasTriggerGroup(triggerGroup);
		}
		#endregion

		#region IResourceListItemMultiplicable Members

		public ResourceCounter GetCounter() {
			return new TriggerGroupsCounter(triggerGroup, number);
		}
		#endregion
	}
}