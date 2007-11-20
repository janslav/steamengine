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

namespace SteamEngine.Regions {
	public interface IRectangle {
		ushort MinX { get; }
		ushort MaxX { get; }
		ushort MinY { get; }
		ushort MaxY { get; }
	}

	public class Rectangle2D : IRectangle {
		public static readonly Rectangle2D voidInstance = new Rectangle2D(0, 0, 0, 0);

		protected ushort minX, maxX, minY, maxY;

		public Rectangle2D(ushort minX, ushort minY, ushort maxX, ushort maxY) {
			this.minX = minX;
			this.minY = minY;
			this.maxX = maxX;
			this.maxY = maxY;
		}

		public Rectangle2D(IPoint2D start, IPoint2D end) {
			Sanity.IfTrueThrow( (start.X > end.X) || (start.Y > end.Y), 
				"The first argument is supposed to be the upper left corner and the second the lower right corner.");
			this.minX = start.X;
			this.minY = start.Y;
			this.maxX = end.X;
			this.maxY = end.Y;
		}
		
		public Rectangle2D(Point2D start, Point2D end) : this((IPoint2D)start, (IPoint2D)end) {			
		}

		[Remark("Return a rectangle created from the central point with the specific range around the point"+
				"(square 'around')")]
		public Rectangle2D(ushort x, ushort y, ushort range) {
			this.minX = (ushort)(x - range);
			this.minY = (ushort)(y - range);
			this.maxX = (ushort)(x + range);
			this.maxY = (ushort)(y + range);
		}
		
		public Rectangle2D(IPoint2D center, ushort range) : this(center.X, center.Y, range) {			
		}
		
		public Rectangle2D Crop(ushort minx, ushort miny, ushort maxx, ushort maxy) {
			ushort newStartX = this.minX;
			ushort newStartY = this.minY;
			ushort newEndX = this.maxX;
			ushort newEndY = this.maxY;
			if (this.StartPoint.x<minx) {
				newStartX = minx;
			}
			if (this.EndPoint.x<minx) {
				newEndX = minx;
			}
			if (this.StartPoint.y<miny) {
				newStartY = miny;
			}
			if (this.EndPoint.y<miny) {
				newEndY = miny;
			}
			if (this.StartPoint.x>maxx) {
				newStartX = maxx;
			}
			if (this.EndPoint.x>maxx) {
				newEndX = maxx;
			}
			if (this.StartPoint.y>maxy) {
				newStartY = maxy;
			}
			if (this.EndPoint.y>maxy) {
				newEndY = maxy;
			}
			//return the new rectangle with possible changes in its strat/end positions
			return new Rectangle2D(newStartX, newStartY, newEndX, newEndY);
		}
		
		public ushort MinX { get {
		    return minX;
		} }
		public ushort MinY { get {
			return minY;
		} }
		public ushort MaxX { get {
			return maxX;
		} }
		public ushort MaxY { get {
			return maxY;
		} }		
		
		public int Width { get {
			return maxX - minX;
		} }
		
		public int Height { get {
			return maxY - minY;
		} }

		public Point2D StartPoint { 
			get {
				return new Point2D(minX, minY);
			} 
		}

		public Point2D EndPoint { 
			get {
				return new Point2D(maxX, maxY);
			}
		}
		
		public override string ToString() {
		   return string.Format("({0}, {1})+({2}, {3})", minX, minY, maxX, maxY);   
		}
		
		public bool Contains(Static p) {
			ushort px = p.X;
			ushort py = p.Y;
			return ((minX <= px) && (minY <= py) && (maxX >= px) && (maxY >= py));
		}
		
		public bool Contains(Thing p) {
			ushort px = p.X;
			ushort py = p.Y;
			return ((minX <= px) && (minY <= py) && (maxX >= px) && (maxY >= py)); 
		}
		
		public bool Contains(Point2D p) {
			ushort px = p.x;
			ushort py = p.y;
			return ((minX <= px) && (minY <= py) && (maxX >= px) && (maxY >= py));
		}

		public bool Contains(int px, int py) {
			return ((minX <= px) && (minY <= py) && (maxX >= px) && (maxY >= py));
		}
		
		public bool Contains(IPoint2D p) {
			ushort px = p.X;
			ushort py = p.Y;
			return ((minX <= px) && (minY <= py) && (maxX >= px) && (maxY >= py));
		}

		[Remark("Does the rectangle contain another rectangle completely?")]
		public bool Contains(IRectangle rect) {
			return Contains(rect.MinX, rect.MinY)//left lower
					&& Contains(rect.MinX, rect.MaxY) //left upper
					&& Contains(rect.MaxX, rect.MaxY) //right upper
					&& Contains(rect.MaxX, rect.MinY);//right lower
		}

		public bool IntersectsWith(IRectangle rect) {
			return Contains(rect.MinX, rect.MinY)//left lower
					|| Contains(rect.MinX, rect.MaxY) //left upper
					|| Contains(rect.MaxX, rect.MaxY) //right upper
					|| Contains(rect.MaxX, rect.MinY);//right lower
		}		

		[Remark("Do the two rectangles have any intersection?")]
		public static bool Intersects(Rectangle2D a, Rectangle2D b) {
			return a.IntersectsWith(b) || b.IntersectsWith(a);
		}

		public static Rectangle2D GetIntersection(Rectangle2D a, Rectangle2D b) {
			ushort maxStartX = (ushort)Math.Max(a.minX, b.minX);
			ushort minEndX = (ushort)Math.Min(a.maxX, b.maxX);
			ushort maxStartY = (ushort)Math.Max(a.minY, b.minY);
			ushort minEndY = (ushort)Math.Min(a.maxY, b.maxY);
			if ((minEndX >= maxStartX) && (minEndY >= maxStartY)) {
				return new Rectangle2D(maxStartX, maxStartY, minEndX, minEndY);
			}
			return Rectangle2D.voidInstance;
		}
		
		public int TilesNumber { 
			get {
				return ((maxX - minX)*(maxY - minY));
			} 
		}
	}	
}