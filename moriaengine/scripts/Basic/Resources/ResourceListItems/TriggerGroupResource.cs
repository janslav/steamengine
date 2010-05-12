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
	[Summary("Resource as TriggerGroup (used only for 'type' checking of char's items), we dont use " +
			"resource triggergroups for checking character's available triggergroups!")]
	public class TriggerGroupResource : AbstractResourceListItemMultiplicable {
		private TriggerGroup triggerGroup;
		private string definition;

		internal TriggerGroupResource(TriggerGroup triggerGroup, double number, string definition, bool isPercent) : base(number, isPercent) {
			this.triggerGroup = triggerGroup;
			this.definition = definition;
		}

		#region IResourceListItem Members
		public override string Definition {
			get {
				return definition;
			}
		}

		public override string Name {
			get {
				return triggerGroup.PrettyDefname;
			}
		}

		public override bool IsSameResource(IResourceListItem newOne) {
			TriggerGroupResource newResource = newOne as TriggerGroupResource;
			if (newResource != null) {
				if (triggerGroup == newResource.triggerGroup) {
					return true;
				}
			}
			return false;
		}

		public override void SendMissingMessage(Character toWho) {
			toWho.SysMessage("Je vyžadována pøítomnost typu " + this.Name + " (poèet alespoò " + this.DesiredCount + ")");
		}
		#endregion

		#region IResourceListItemMultiplicable Members
		public override ResourceCounter GetCounter() {
			TriggerGroupsCounter tgc = Pool<TriggerGroupsCounter>.Acquire();//get from pool
			tgc.SetParameters(triggerGroup, number, this);//initialize
			return tgc;
		}
		#endregion
	}
}