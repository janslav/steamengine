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
	public sealed class MultiItemComponent : AbstractInternalItem, IPoint4D {
		internal MultiItemComponent prevInList;
		internal MultiItemComponent nextInList;
		internal MultiComponentLinkedList collection;

		private readonly MultiComponentDescription mcd;
		private readonly int multiFlags;
		private byte m;

		internal MultiItemComponent(MultiComponentDescription mcd, int id, Map map, int multiFlags)
			: base(id, map.Facet) {

			this.mcd = mcd;
			this.multiFlags = multiFlags;
			this.m = map.M;
		}

		internal void SetRelativePos(int centerX, int centerY, int centerZ) {
			checked {
				this.X = centerX + this.mcd.OffsetX;
				this.Y = centerY + this.mcd.OffsetY;
				this.Z = centerZ + this.mcd.OffsetZ;
			}
		}

		//useless?
		public int MultiFlags {
			get {
				return this.multiFlags;
			}
		}

		public MultiComponentDescription Mcd {
			get {
				return this.mcd;
			}
		}

		public byte M {
			get {
				return this.m;
			}
			internal set {
				this.m = value;
				this.Facet = Map.GetMap(value).Facet;
			}
		}

		public IPoint4D TopPoint {
			get {
				return this;
			}
		}

		public Map GetMap() {
			return Map.GetMap(this.m);
		}


		IPoint2D IPoint2D.TopPoint {
			get {
				return this;
			}
		}
	}
}