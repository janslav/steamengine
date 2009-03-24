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
using System.Collections.Generic;
using SteamEngine.Common;
using System.Reflection;
using System.Text.RegularExpressions;

namespace SteamEngine.Regions {
	public class DynamicRegion : Region {
		public DynamicRegion() {
			throw new SEException("The constructor without paramaters is not supported");
		}

		public DynamicRegion(ImmutableRectangle[] newRects)
			: base() {

			int n = newRects.Length;
			rectangles = new RegionRectangle[n];
			for (int i = 0; i < n; i++) {
				rectangles[i] = new RegionRectangle(newRects[i], this);
			}
		}

		public override string Name {
			get {
				return "DynamicRegion";
			}
		}

		[Summary("Serves to place the region to the map for the first time (after creation)")]
		public bool Place(Point4D p) {
			ThrowIfDeleted();
			if (p != null) { //already placed!
				throw new SEException("This Dynamic region has already been placed to the map. For movement try setting its P");
			}
			this.InternalSetP(p);

			Map map = Map.GetMap(p.m);
			this.Parent = map.GetRegionFor(p);

			return map.AddDynamicRegion(this, true); //add it to the map, but try if there is no other obstacle!
		}

		public override Point4D P {
			get {
				return base.P;
			}
			set {
				ThrowIfDeleted();
				if (value == null) {
					throw new SEException("P is null");
				}
				if (Step(value)) { //first move to the desired position after performing necessary checks
					//(trying to move over another dynamic region causes movement to fail!)
					base.P = value;
				}
			}
		}

		[Summary("Tries to move the specified amount of fields in X and Y axes. First examines if it is possible" +
				"to move that way to the desired location and if so, it moves every rectangle there." +
				"We expect the timesX and timesY parameteres to be small numbers")]
		public bool Step(int xDiff, int yDiff) {
			//a new list of changed (moved) rectangles
			List<RegionRectangle> movedRects = new List<RegionRectangle>();
			foreach (RegionRectangle oneRect in rectangles) {
				movedRects.Add(oneRect.CloneMoved(xDiff, yDiff));
			}
			Point4D oldP = this.P;
			Map oldMap = Map.GetMap(oldP.m); //the dynamic region's old Map
			oldMap.RemoveDynamicRegion(this);//remove it anyways
			bool result = SetRectangles(movedRects, oldMap);
			//add it without checks (these were performed when setting the rectangles)
			//we will add either the new array of rectangles or the old one if there were problems
			oldMap.AddDynamicRegion(this, false);
			if (result) { //OK - alter the position also
				this.InternalSetP(new Point4D((ushort) (oldP.X + xDiff), (ushort) (oldP.Y + yDiff), oldP.z, oldP.m));
			}
			return result;
		}

		[Summary("Method called on position change - it recounts the region's rectangles' position and also makes " +
				"sure that no confilicts with other dynamic regions occurs when moving!")]
		private bool Step(Point4D newP) {
			Point4D oldPos = this.P; //store the old position for case the movement fails!
			IList<RegionRectangle> oldRects = rectangles;

			bool xyChanged = (oldPos.X != newP.X || oldPos.Y != newP.Y);
			bool mapChanged = oldPos.m != newP.m;

			Map oldMap = Map.GetMap(oldPos.m); //the dynamic region's old Map
			oldMap.RemoveDynamicRegion(this);//remove it anyways
			bool movingOK = true;//indicator if the movement success
			if (xyChanged) {
				int diffX = newP.X - oldPos.X;
				int diffY = newP.Y - oldPos.Y;
				List<RegionRectangle> movedRects = new List<RegionRectangle>();
				foreach (RegionRectangle oneRect in rectangles) {
					movedRects.Add(oneRect.CloneMoved(diffX, diffY));
				}
				if (mapChanged) {
					Map newMap = Map.GetMap(newP.m);
					this.Parent = newMap.GetRegionFor(newP);
					movingOK = SetRectangles(movedRects, newMap);
					newMap.AddDynamicRegion(this, false);//place the region to the map (no checks, they were already performed in SetRectangles)
				} else {
					this.Parent = oldMap.GetRegionFor(newP);
					movingOK = SetRectangles(movedRects, oldMap);
					oldMap.AddDynamicRegion(this, false);//and place (no checks as well)
				}
			} else if (mapChanged) {
				Map newMap = Map.GetMap(newP.m);
				this.Parent = newMap.GetRegionFor(newP);
				movingOK = SetRectangles(rectangles, newMap); //here set the old rectangles (but to the new map)
				newMap.AddDynamicRegion(this, false);//place, still no checks
			} else { //nothing at all :) - set to the same position
				oldMap.AddDynamicRegion(this, false);//return it			
			}
			if (!movingOK) {
				//return the parent and place the region to the old position to the map without checkings!
				this.Parent = oldMap.GetRegionFor(oldPos);
				oldMap.AddDynamicRegion(this, false);//return
			}
			return movingOK;
		}

		[Summary("Take the list of rectangles and make an array of RegionRectangles of it." +
				"The purpose is the same as for StaticRegion but the checks are different." +
				"The map parameter allows us to specifiy the map where the region should be")]
		public bool SetRectangles<T>(IList<T> list, Map map) where T : ImmutableRectangle {
			RegionRectangle[] newArr = new RegionRectangle[list.Count];
			for (int i = 0; i < list.Count; i++) {
				//take the start/end point from the IRectangle and create a new RegionRectangle
				newArr[i] = new RegionRectangle(list[i].MinX, list[i].MinY, list[i].MaxX, list[i].MaxY, this);
			}
			//now the checking phase!
			inactivated = true;
			foreach (RegionRectangle rect in newArr) {
				if (!map.CheckDynRectIntersection(rect)) {
					//check the intercesction of the dynamic region, in case of any trouble immediatelly finish
					return false;
				}
			}
			//everything is OK, we can swith the lists
			rectangles = newArr;
			inactivated = false;
			return true;
		}

		public override void Delete() {
			Map.GetMap(this.P.m).RemoveDynamicRegion(this);
			base.Delete();
		}

		public override sealed void Save(SteamEngine.Persistence.SaveStream output) {
			throw new SEException("Dynamic regions are not supposed to be saved");
		}

		public override sealed void LoadLine(string filename, int line, string valueName, string valueString) {
			throw new SEException("Dynamic regions are not supposed to be loaded");
		}
	}
}
