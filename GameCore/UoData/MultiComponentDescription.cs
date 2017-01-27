/*
	This program is free software; you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation; either version 2 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See then
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program; if not, write to the Free Software
	Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
	Or visit http://www.gnu.org/copyleft/gpl.html
*/

using SteamEngine.Regions;

namespace SteamEngine.UoData
{
	public class MultiComponentDescription {
		public MultiComponentDescription(int id, int offsetX, int offsetY, int offsetZ, int flags) {
			this.ItemId = id;
			this.OffsetX = offsetX;
			this.OffsetY = offsetY;
			this.OffsetZ = offsetZ;
			this.Flags = flags;
		}

		public int ItemId { get; }

		public int OffsetX { get; }

		public int OffsetY { get; }

		public int OffsetZ { get; }

		public int Flags { get; }

		internal MultiItemComponent Create(int centerX, int centerY, int centerZ, Map map) {
			var retVal = new MultiItemComponent(this, this.ItemId, map, this.Flags);
			retVal.SetRelativePos(centerX, centerY, centerZ);
			return retVal;
		}
	}
}