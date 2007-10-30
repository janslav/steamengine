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

namespace SteamEngine {
	public class DynamicRegion : Region {

		public DynamicRegion() {
			throw new NotSupportedException("The constructor without paramaters is not supported");
		}

		public DynamicRegion(Point4D p, Rectangle2D[] newRects) 
				: base() {
			this.p = p;

			int n = newRects.Length;
			rectangles = new RegionRectangle[n];
			for (int i = 0; i<n; i++) {
				rectangles[i] = new RegionRectangle(newRects[i], this);
			}
			Map map = Map.GetMap(p.M);
			this.parent = map.GetRegionFor(p);
			map.AddDynamicRegion(this);
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
				bool xyChanged = (p.X != value.X || p.Y != p.Z);
				bool mapChanged = p.M != value.M;

				if (xyChanged) {
					int diffX = value.X - p.X;
					int diffY = value.Y - p.Y;
					Map map = Map.GetMap(p.M);
					map.RemoveDynamicRegion(this);
					int n = rectangles.Length;
					RegionRectangle[] newRects = new RegionRectangle[n];
					for (int i = 0; i<n; i++) {
						RegionRectangle oldRect = rectangles[i];
						Point2D oldStart = oldRect.StartPoint;
						Point2D oldEnd = oldRect.EndPoint;
						newRects[i] = new RegionRectangle(
							new Point2D((ushort) (oldStart.X + diffX), (ushort) (oldStart.Y + diffY)),
							new Point2D((ushort) (oldEnd.X + diffX), (ushort) (oldEnd.Y + diffY)),
							this);
					}
					this.rectangles = newRects;
					if (mapChanged) {
						map = Map.GetMap(value.M);
						this.parent = map.GetRegionFor(value);
						map.AddDynamicRegion(this);
					} else {
						this.parent = map.GetRegionFor(value);
						map.AddDynamicRegion(this);
					}
				} else if (mapChanged) {
					Map oldMap = Map.GetMap(p.M);
					Map newMap = Map.GetMap(value.M);
					oldMap.RemoveDynamicRegion(this);
					this.parent = newMap.GetRegionFor(value);
					newMap.AddDynamicRegion(this);
				}
				
				base.P = value;
			}
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