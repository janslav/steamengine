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
	[Summary("Character's level as a resource")]
	public class LevelResource : IResourceListItemNonMultiplicable {
		private int number;

		internal LevelResource(int number) {
			this.number = number;
		}

		#region IResourceListItem Members
		public double DesiredCount {
			get {
				return this.number;
			}
			set {
				this.number = (int) value;
			}
		}

		public string Definition {
			get {
				return "level";
			}
		}

		public string Name {
			get {
				return "Level";
			}
		}

		public bool IsSameResource(IResourceListItem newOne) {
			return ((newOne as LevelResource) != null);
		}

		public void SendMissingMessage(Character toWho) {
			toWho.SysMessage("Je pot�eba m�t alespo� " + this.DesiredCount + " " + this.Name);
		}

		#endregion

		#region IResourceListItemNonMultiplicable Members

		public bool IsResourcePresent(Character chr) {
			return ((Player) chr).Level >= DesiredCount;
		}

		#endregion	
		
	}
}