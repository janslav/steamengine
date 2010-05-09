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
using SteamEngine.Common;
using SteamEngine.Persistence;
using System.Threading;
using System.Configuration;
using SteamEngine.CompiledScripts;
using SteamEngine.Regions;
using SteamEngine.Networking;


namespace SteamEngine {
	public abstract partial class AbstractCharacter {

		/**
			Returns the current UpdateRange of the character. For players, this is either their requested update range
			or vision range, whichever is lower. Scripts can modify PCs' vision range with
			IncreaseVisionRangeBy(int amount) and DecreaseVisionRangeBy(int amount).
			
			For NPCs, this calls NPCUpdateRange.
		*/
		public int UpdateRange {
			get {
				GameState state = this.GameState;
				if (state != null) {
					return state.UpdateRange;
				} else {
					return Globals.MaxUpdateRange;
				}
			}
		}

		public byte RequestedUpdateRange {
			get {
				GameState state = this.GameState;
				if (state != null) {
					return state.RequestedUpdateRange;
				} else {
					return Globals.MaxUpdateRange;
				}
			}
		}

		static TagKey visionRangeTK = TagKey.Acquire("_visionRange_");
		/**
			Increases or decreases the vision range of this character.
			
			It also changes the char's UpdateRange, but only within allowed limits (Globals.Max/MinUpdateRange)		  
		
			The value is stored in a tag, but in the override it's expected to be stored in a field.
		 */
		public virtual int VisionRange {
			get {
				object value = this.GetTag(visionRangeTK);
				if (value == null) {
					return Globals.MaxUpdateRange;
				} else {
					return TagMath.ToInt32(value);
				}
			}
			set {
				if (value != Globals.MaxUpdateRange) {
					this.SetTag(visionRangeTK, value);
				} else {
					this.RemoveTag(visionRangeTK);
				}

				GameState state = this.GameState;
				if (state != null) {
					state.SyncUpdateRange();
				}
			}
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods"), Summary("Returns true if this character can see that target. This works on items in containers, etc, as well.")]
		public virtual bool CanSeeForUpdate(Thing target) {
			return this.CanSeeImpl(this, target.TopPoint, target);
		}

		internal bool CanSeeForUpdateFrom(IPoint4D fromCoordinates, Thing target) {
			return this.CanSeeImpl(fromCoordinates, target.TopPoint, target);
		}

		internal bool CanSeeForUpdateAt(IPoint4D targetMapCoordinates, Thing target) {
			return this.CanSeeImpl(this, targetMapCoordinates, target);
		}

		private bool CanSeeImpl(IPoint4D fromCoordinates, IPoint4D targetMapCoordinates, Thing target) {
			if (target.IsOnGround) {
				bool success = this.CanSeeCoordinatesFrom(fromCoordinates, targetMapCoordinates);
				if (!success) {
					return false;
				}

				success = this.CanSeeVisibility(target);
				if (!success) {
					return false;
				}

				return success;
			} else if (target.IsEquipped) {
				if (target.Z < AbstractCharacter.sentLayers) {
					Thing container = target.TopObj();//the char that has this item equipped
					if (this.CanSeeImpl(fromCoordinates, targetMapCoordinates, container)) {
						return this.CanSeeVisibility(target);
					}
				}
				return false;
			} else { //in container - we must be able to reach the container
				return DenyResult.Allow ==
					this.CanReachFromAt(fromCoordinates, targetMapCoordinates, target, true);
			}
		}

		public virtual bool CanSeeVisibility(Thing target) {
			this.ThrowIfDeleted();
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
			return this.CanSeeCoordinatesFrom(this, target);
		}

		public bool CanSeeCoordinates(int targetX, int targetY, byte targetM) {
			return this.CanSeeCoordinatesFrom(this, targetX, targetY, targetM);
		}

		internal bool CanSeeCoordinatesFrom(IPoint4D fromCoordinates, IPoint4D target) {
			if (target == null) {
				return false;
			}

			return CanSeeCoordinatesFrom(fromCoordinates, target.X, target.Y, target.M);
		}

		private bool CanSeeCoordinatesFrom(IPoint4D fromCoordinates, int targetX, int targetY, byte targetM) {
			this.ThrowIfDeleted();
			if (fromCoordinates.M != targetM) {
				return false;
			}
			int dist = Point2D.GetSimpleDistance(fromCoordinates.X, fromCoordinates.Y, targetX, targetY);
			return dist <= this.UpdateRange;
		}

		//public virtual DenyResult CanPickUp(AbstractItem item) {
		//    return DenyResult.Allow;
		//}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods"), Summary("Determines if I can reach the specified Thing. Checks distance and LOS of the top object and visibility and openness of whole container hierarchy.")]
		public DenyResult CanReach(Thing target) {
			if (target.IsDeleted) {
				return DenyResult.Deny_RemoveFromView;
			}
			return this.CanReachFromAt(this, target.TopPoint, target, true);
		}

		internal DenyResult CanReachFromAt(IPoint4D fromCoordinates, IPoint4D targetMapCoordinates, Thing target, bool checkTopObj) {
			Thing topobj = null;

			if (checkTopObj) {
				if (!CanReachMapRangeFrom(fromCoordinates, targetMapCoordinates)) {
					return DenyResult.Deny_ThatIsTooFarAway;
				}

				Map map = fromCoordinates.GetMap();
				if (!map.CanSeeLosFromTo(fromCoordinates, targetMapCoordinates)) {
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
				if (this.IsOnline) {
					return OpenedContainers.HasContainerOpenFromAt(this, fromCoordinates, targetMapCoordinates, container, false);//calls this method recursively... false cos we already checked topobj
				} else {
					return DenyResult.Deny_NoMessage; //only logged-in players can reach stuff in containers
				}
			}

			return DenyResult.Allow;
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
		public DenyResult CanReachCoordinates(IPoint4D target) {
			target = target.TopPoint;
			if (!CanReachMapRangeFrom(this, target)) {
				return DenyResult.Deny_ThatIsTooFarAway;
			}
			Map m = this.GetMap();
			if (!m.CanSeeLosFromTo(this, target)) {
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
			return dist <= Globals.ReachRange;
		}

		public virtual DenyResult CanOpenContainer(AbstractItem targetContainer) {
			return DenyResult.Allow;
		}
	}
}

namespace SteamEngine.Regions {
	public partial class Map {
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "to"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "from"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
		public bool CanSeeLosFromTo(IPoint3D from, IPoint3D to) {
			return true;
		}
	}
}