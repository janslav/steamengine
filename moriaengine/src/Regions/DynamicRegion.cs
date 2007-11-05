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

		public DynamicRegion(Point4D p, Rectangle2D[] newRects, out bool success) 
				: base() {
			this.p = p;

			int n = newRects.Length;
			rectangles = new RegionRectangle[n];
			for (int i = 0; i<n; i++) {
				rectangles[i] = new RegionRectangle(newRects[i], this);
			}
			Map map = Map.GetMap(p.M);
			this.parent = map.GetRegionFor(p);
			success = map.AddDynamicRegion(this, true); //add it to the map, but try if there is no other obstacle!

			if(!success) {
				Delete();
			}			
		}

		public override string Name {
			get {
				return "DynamicRegion";
			}
			set {
			}
		}

		public override Point4D P {
			get {
				return p;
			}
			set {
				if (value == null) {
					throw new ArgumentNullException("P");
				}
				if(Step(value)) { //first move to the desired position after performing necessary checks
					//(trying to move over another dynamic region causes movement to fail!)
					base.P = value;
				} 
			}
		}

		[Remark("Method called on position change - it recounts the region's rectnagles' position and also makes "+
				"sure that no confilicts with other dynamic regions occurs when moving!")]
		private bool Step(Point4D newP) {
			Point4D oldPos = p; //store the old position for case the movement fails!
			RegionRectangle[] oldRects = rectangles;

			bool xyChanged = (p.X != newP.X || p.Y != newP.Y);
			bool mapChanged = p.M != newP.M;

			Map oldMap = Map.GetMap(p.M); //the dynamic region's old Map
			oldMap.RemoveDynamicRegion(this);//remove it anyways
			bool movingOK = true;//indicator if the movement success
			if(xyChanged) {
				int diffX = newP.X - p.X;
				int diffY = newP.Y - p.Y;
				RegionRectangle[] movedRects = MoveRectangles(diffX, diffY);
				this.rectangles = movedRects;
				if(mapChanged) {
					Map newMap = Map.GetMap(newP.M);
					this.parent = newMap.GetRegionFor(newP);
					movingOK = newMap.AddDynamicRegion(this, true);//try to place region to the new location
				} else {
					this.parent = oldMap.GetRegionFor(newP);
					movingOK = oldMap.AddDynamicRegion(this, true);//try to place region to the new location
				}
			} else if(mapChanged) {
				Map newMap = Map.GetMap(newP.M);
				this.parent = newMap.GetRegionFor(newP);
				movingOK = newMap.AddDynamicRegion(this, true);//try to place region to the new location
			}
			if(!movingOK) {
				//return the parent and rectangles and place the region to the old position to the map without checkings!
				this.parent = oldMap.GetRegionFor(oldPos);
				this.rectangles = oldRects;
				oldMap.AddDynamicRegion(this,false);
			}
			return movingOK;
		}

		[Remark("Use the diffPos (difference point) to move every rectangle of the dynamic region. "+
				"The used diffPos is added to the rectangle's position."+
				"New array of rectangles is returned..."+
				"The diff coordinates may also be negative!")]
		internal RegionRectangle[] MoveRectangles(int diffX, int diffY) {
			RegionRectangle[] newRects = new RegionRectangle[rectangles.Length];
			for(int i = 0; i < rectangles.Length; i++) {
				newRects[i] = new RegionRectangle(
								rectangles[i].StartPoint.Add(diffX, diffY), 
								rectangles[i].EndPoint.Add(diffX, diffY), this);				        
			}
			return newRects;
		}

		public override void Delete() {
			Map.GetMap(p.M).RemoveDynamicRegion(this);
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