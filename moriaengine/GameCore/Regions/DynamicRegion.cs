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
			throw new NotSupportedException("The constructor without paramaters is not supported");
		}

		public DynamicRegion(ImmutableRectangle[] newRects) 
				: base() {
			
			int n = newRects.Length;
			rectangles = new RegionRectangle[n];
			for (int i = 0; i<n; i++) {
				rectangles[i] = new RegionRectangle(newRects[i], this);
			}						
		}

		public override string Name {
			get {
				return "DynamicRegion";
			}
		}

		[Remark("Serves to place the region to the map for the first time (after creation)")]
		public bool Place(Point4D p) {
			ThrowIfInactivated();
			if(p != null) { //already placed!
				throw new SEException("This Dynamic region has already been placed to the map. For movement try setting its P");
			} 
			this.p = p;

			Map map = Map.GetMap(p.m);
			this.parent = map.GetRegionFor(p);

			return map.AddDynamicRegion(this, true); //add it to the map, but try if there is no other obstacle!
		}

		public override Point4D P {
			get {
				return p;
			}
			set {
				ThrowIfInactivated();
				if (value == null) {
					throw new ArgumentNullException("P");
				}
				if(Step(value)) { //first move to the desired position after performing necessary checks
					//(trying to move over another dynamic region causes movement to fail!)
					base.P = value;
				} 
			}
		}

		[Remark("Tries to move the specified amount of fields in X and Y axes. First examines if it is possible"+
				"to move that way to the desired location and if so, it moves every rectangle there."+
				"We expect the timesX and timesY parameteres to be small numbers")]
		public bool Step(int timesX, int timesY) {
			//a new list of changed (moved) rectangles
			List<RegionRectangle> movedRects = new List<RegionRectangle>();
			foreach(RegionRectangle oneRect in rectangles) {
				movedRects.Add(oneRect.Move(timesX, timesY));
			}
			Map oldMap = Map.GetMap(p.m); //the dynamic region's old Map
			oldMap.RemoveDynamicRegion(this);//remove it anyways
			bool result = SetRectangles(movedRects, oldMap);
			//add it without checks (these were performed when setting the rectangles)
			//we will add either the new array of rectangles or the old one if there were problems
			oldMap.AddDynamicRegion(this, false);
			if(result) { //OK - alter the position also
				p = new Point4D((ushort)(p.x + timesX), (ushort)(p.y + timesY), p.z, p.m);
			}
			return result;			
		}

		[Remark("Method called on position change - it recounts the region's rectangles' position and also makes "+
				"sure that no confilicts with other dynamic regions occurs when moving!")]
		private bool Step(Point4D newP) {
			Point4D oldPos = p; //store the old position for case the movement fails!
			IList<RegionRectangle> oldRects = rectangles;

			bool xyChanged = (p.x != newP.x || p.y != newP.y);
			bool mapChanged = p.m != newP.m;

			Map oldMap = Map.GetMap(p.m); //the dynamic region's old Map
			oldMap.RemoveDynamicRegion(this);//remove it anyways
			bool movingOK = true;//indicator if the movement success
			if(xyChanged) {
				int diffX = newP.x - p.x;
				int diffY = newP.y - p.y;
				List<RegionRectangle> movedRects = new List<RegionRectangle>();
				foreach(RegionRectangle oneRect in rectangles) {
					movedRects.Add(oneRect.Move(diffX, diffY));
				}				
				if(mapChanged) {
					Map newMap = Map.GetMap(newP.m);
					this.parent = newMap.GetRegionFor(newP);
					movingOK = SetRectangles(movedRects, newMap);
					newMap.AddDynamicRegion(this, false);//place the region to the map (no checks, they were already performed in SetRectangles)
				} else {
					this.parent = oldMap.GetRegionFor(newP);
					movingOK = SetRectangles(movedRects, oldMap);
					oldMap.AddDynamicRegion(this, false);//and place (no checks as well)
				}
			} else if(mapChanged) {
				Map newMap = Map.GetMap(newP.m);
				this.parent = newMap.GetRegionFor(newP);
				movingOK = SetRectangles(rectangles, newMap); //here set the old rectangles (but to the new map)
				newMap.AddDynamicRegion(this, false);//place, still no checks
			} else { //nothing at all :) - set to the same position
				oldMap.AddDynamicRegion(this, false);//return it			
			}
			if(!movingOK) {
				//return the parent and place the region to the old position to the map without checkings!
				this.parent = oldMap.GetRegionFor(oldPos);
				oldMap.AddDynamicRegion(this, false);//return
			}
			return movingOK;
		}

		[Remark("Take the list of rectangles and make an array of RegionRectangles of it."+
				"The purpose is the same as for StaticRegion but the checks are different."+
				"The map parameter allows us to specifiy the map where the region should be")]
		public bool SetRectangles<T>(IList<T> list, Map map) where T : ImmutableRectangle {
			RegionRectangle[] newArr = new RegionRectangle[list.Count];
			for(int i = 0; i < list.Count; i++) {
				//take the start/end point from the IRectangle and create a new RegionRectangle
				newArr[i] = new RegionRectangle(list[i].StartPoint, list[i].EndPoint, this);
			}
			//now the checking phase!
			Inactivate(); //unload the region - it 'locks' it for every usage except for rectangles operations
			foreach(RegionRectangle rect in newArr) {
				if(!map.CheckDynRectIntersection(rect)) {
					//check the intercesction of the dynamic region, in case of any trouble immediatelly finish
					return false;
				}
			}
			//everything is OK, we can swith the lists
			rectangles = newArr;
			Activate();
			return true;
		}

		[Remark("Use the diffPos (difference point) to move every rectangle of the dynamic region. "+
				"The used diffPos is added to the rectangle's position."+
				"New array of rectangles is returned..."+
				"The diff coordinates may also be negative!")]
		internal RegionRectangle[] MoveRectangles(int diffX, int diffY) {
			int n = rectangles.Count;
			RegionRectangle[] newRects = new RegionRectangle[n];
			for (int i = 0; i < n; i++) {
				newRects[i] = new RegionRectangle(
								rectangles[i].StartPoint.Add(diffX, diffY), 
								rectangles[i].EndPoint.Add(diffX, diffY), this);				        
			}
			return newRects;
		}

		public override void Delete() {
			Map.GetMap(p.m).RemoveDynamicRegion(this);
			base.Delete();
		}

		public override sealed void Save(SteamEngine.Persistence.SaveStream output) {
			throw new NotSupportedException("Dynamic regions are not supposed to be saved");
		}

		public override sealed void LoadLine(string filename, int line, string param, string args) {
			throw new NotSupportedException("Dynamic regions are not supposed to be loaded");
		}
	}
}