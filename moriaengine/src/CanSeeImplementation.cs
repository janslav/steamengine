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
			return CanSeeImpl(this, target.TopObj(), target);
		}

		internal bool CanSeeForUpdateFrom(IPoint4D fromCoordinates, Thing target) {
			return CanSeeImpl(fromCoordinates, target.TopObj(), target);
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
				return TryReachResult.Succeeded ==
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

		public virtual TryReachResult CanPickUp(AbstractItem item) {
			return TryReachResult.Succeeded;
		}

		[Remark("Determines if I can reach the specified Thing. Checks distance and LOS of the top object and visibility and openness of whole container hierarchy.")]
		public TryReachResult CanReach(Thing target) {
			return CanReachFromAt(this, target.TopPoint, target, true);
		}

		internal TryReachResult CanReachFromAt(IPoint4D fromCoordinates, IPoint4D targetMapCoordinates, Thing target, bool checkTopObj) {
			bool retVal = false;
			Thing topobj = null;

			if (checkTopObj) {
				retVal = CanReachCoordinatesFrom(fromCoordinates, targetMapCoordinates);
				if (!retVal) {
					return TryReachResult.Failed_ThatIsTooFarAway;
				}

				Map map = fromCoordinates.GetMap();
				retVal = map.CanSeeLOSFromTo(fromCoordinates, targetMapCoordinates);
				if (!retVal) {
					return TryReachResult.Failed_ThatIsOutOfSight;
				}

				topobj = target.TopObj();
				retVal = this.CanSeeVisibility(topobj);
				if (!retVal) {
					return TryReachResult.Failed_RemoveFromView;
				}
			}

			if (target != topobj) {
				retVal = this.CanSeeVisibility(target);
				if (!retVal) {
					return TryReachResult.Failed_RemoveFromView;
				}
			} //else we already checked it

			AbstractItem container = target.Cont as AbstractItem;
			if (container != null) {
				GameConn conn = this.Conn;
				if (conn != null) {
					retVal = OpenedContainers.HasContainerOpenFromAt(conn, fromCoordinates, targetMapCoordinates, container, false);//calls this method recursively... false cos we already checked topobj
				} else {
					return TryReachResult.Failed_NoMessage; //only logged-in players can reach stuff in containers
				}
			}

			if (retVal) {
				return TryReachResult.Succeeded;
			} else {
				return TryReachResult.Failed_YouCannotPickThatUp;
			}
		}

		public bool CanReachCoordinates(IPoint4D target) {
			return CanReachCoordinatesFrom(this, target);
		}

		internal bool CanReachCoordinatesFrom(IPoint4D fromCoordinates, IPoint4D target) {
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
	}	
}

namespace SteamEngine.Regions {
	public partial class Map {
		public bool CanSeeLOSFromTo(IPoint3D from, IPoint3D to) {
			return true;
		}
	}
}