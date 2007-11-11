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
using System.Collections;
using System.Reflection;
using System.Globalization;
using System.Text.RegularExpressions;
using SteamEngine.Packets;
using SteamEngine.Common;
using SteamEngine.Persistence;
using System.Threading;
using System.Configuration;
using SteamEngine.CompiledScripts;
using SteamEngine.Regions;

namespace SteamEngine {
	public abstract partial class AbstractCharacter {

		/**
			Returns the current UpdateRange of the character. For players, this is either their requested update range
			or vision range, whichever is lower. Scripts can modify PCs' vision range with
			IncreaseVisionRangeBy(int amount) and DecreaseVisionRangeBy(int amount).
			
			For NPCs, this calls NPCUpdateRange.
		*/
		public byte UpdateRange {
			get {
				GameConn c = Conn;
				if (c != null) {
					return c.UpdateRange;
				} else {
					return Globals.MaxUpdateRange;
				}
			}
		}
		public byte RequestedUpdateRange {
			get {
				GameConn c = Conn;
				if (c != null) {
					return c.RequestedUpdateRange;
				} else {
					return Globals.MaxUpdateRange;
				}
			}
		}

		/**
			Increases or decreases the vision range of this character. For NPCs, this calls NPCIncreaseVisionRangeBy.
			
			For PCs, you don't need to worry about whether this will go below 0 or above MaxUpdateRange,
			those are checked. UpdateRange will never actually be <0 or >MaxUpdateRange, and will also always
			be RequestedUpdateRange if VisionRange is higher, so if a player has their updaterange set
			low, you can't actually increase their update range above what they've requested by playing with VisionRange.
		*/
		public int VisionRange {
			get {
				GameConn c = Conn;
				if (c != null) {
					return c.VisionRange;
				} else {
					return Globals.MaxUpdateRange;
				}
			}
			set {
				GameConn c = Conn;
				if (c != null) {
					int oldValue = c.VisionRange;
					c.VisionRange = value;
					if (value > oldValue) {
						this.SendNearbyStuff();//could be optimalized but... how often do you change vision range anyway ;)
					}
				} else {
					throw new SanityCheckException("You can't set NPC's update range.");
				}
			}
		}

		[Remark("Returns true if this character can see that target. This works on items in containers, etc, as well.")]
		public virtual bool CanSeeForUpdate(Thing target) {
			return CanSeeImpl(this, target.TopPoint, target);
		}

		internal bool CanSeeForUpdateFrom(IPoint4D fromCoordinates, Thing target) {
			return CanSeeImpl(fromCoordinates, target.TopPoint, target);
		}

		internal bool CanSeeForUpdateAt(IPoint4D targetMapCoordinates, Thing target) {
			return CanSeeImpl(this, targetMapCoordinates, target);
		}

		private bool CanSeeImpl(IPoint4D fromCoordinates, IPoint4D targetMapCoordinates, Thing target) {
			if (target.IsOnGround) {
				bool success = CanSeeCoordinatesFrom(fromCoordinates, targetMapCoordinates);
				if (!success) {
					return false;
				}

				success = this.CanSeeVisibility(target);
				if (!success) {
					return false;
				}

				return success;
			} else if (target.IsEquipped) {
				if (target.Z<AbstractCharacter.sentLayers) {
					Thing container = target.TopObj();//the char that has this item equipped
					if (CanSeeImpl(fromCoordinates, targetMapCoordinates, container)) {
						return this.CanSeeVisibility(target);
					}
				}
				return false;
			} else {
				return DenyResult.Allow ==
					this.CanReachFromAt(fromCoordinates, targetMapCoordinates, target, true);
			}
		}

		public virtual bool CanSeeVisibility(Thing target) {
			ThrowIfDeleted();
			if (target == null) {
				return false;
			}
			if (target.IsDeleted) {
				return false;
			}
			if (target.IsNotVisible) {
				return false;
			}
			return true;
		}

		public bool CanSeeCoordinates(IPoint4D target) {
			return CanSeeCoordinatesFrom(this, target);
		}

		internal bool CanSeeCoordinatesFrom(IPoint4D fromCoordinates, IPoint4D target) {
			ThrowIfDeleted();
			if (target == null) {
				return false;
			}
			if (fromCoordinates.M != target.M) {
				return false;
			}
			int dist = Point2D.GetSimpleDistance(fromCoordinates, target);
			return dist <= this.UpdateRange;
		}

		//public virtual DenyResult CanPickUp(AbstractItem item) {
		//    return DenyResult.Allow;
		//}

		[Remark("Determines if I can reach the specified Thing. Checks distance and LOS of the top object and visibility and openness of whole container hierarchy.")]
		public DenyResult CanReach(Thing target) {
			return CanReachFromAt(this, target.TopPoint, target, true);
		}

		internal DenyResult CanReachFromAt(IPoint4D fromCoordinates, IPoint4D targetMapCoordinates, Thing target, bool checkTopObj) {
			Thing topobj = null;

			if (checkTopObj) {
				if (!CanReachMapRangeFrom(fromCoordinates, targetMapCoordinates)) {
					return DenyResult.Deny_ThatIsTooFarAway;
				}

				Map map = fromCoordinates.GetMap();
				if (!map.CanSeeLOSFromTo(fromCoordinates, targetMapCoordinates)) {
					return DenyResult.Deny_ThatIsOutOfSight;
				}

				topobj = target.TopObj();
				if (!this.CanSeeVisibility(topobj)) {
					return DenyResult.Deny_RemoveFromView;
				}
			}

			if (target != topobj) {
				if (!this.CanSeeVisibility(target)) {
					return DenyResult.Deny_RemoveFromView;
				}
			} //else we already checked it

			AbstractItem container = target.Cont as AbstractItem;
			if (container != null) {
				GameConn conn = this.Conn;
				if (conn != null) {
					return OpenedContainers.HasContainerOpenFromAt(conn, fromCoordinates, targetMapCoordinates, container, false);//calls this method recursively... false cos we already checked topobj
				} else {
					return DenyResult.Deny_NoMessage; //only logged-in players can reach stuff in containers
				}
			}

			return DenyResult.Allow;
		}

		public DenyResult CanReachCoordinates(IPoint4D target) {
			target = target.TopPoint;
			if (!CanReachMapRangeFrom(this, target)) {
				return DenyResult.Deny_ThatIsTooFarAway;
			}
			Map m = this.GetMap();
			if (!m.CanSeeLOSFromTo(this, target)) {
				return DenyResult.Deny_ThatIsOutOfSight;
			}
			return DenyResult.Allow;
		}

		internal bool CanReachMapRangeFrom(IPoint4D fromCoordinates, IPoint4D target) {
			ThrowIfDeleted();
			if (target == null) {
				return false;
			}
			if (fromCoordinates.M != target.M) {
				return false;
			}

			int dist = Point2D.GetSimpleDistance(fromCoordinates, target);
			return dist <= Globals.reachRange;
		}

		public virtual DenyResult CanOpenContainer(AbstractItem targetContainer) {
			return DenyResult.Allow;
		}
	}	
}

namespace SteamEngine.Regions {
	public partial class Map {
		public bool CanSeeLOSFromTo(IPoint3D from, IPoint3D to) {
			return true;
		}
	}
}