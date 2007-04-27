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
using SteamEngine.Common;

namespace SteamEngine {
	public class Rectangle2D {//class or struct? what is better in this case? :)
		public static readonly Rectangle2D voidInstance = new Rectangle2D(0, 0, 0);
		
		private Point2D start;
		private Point2D end;
		
		public Rectangle2D(IPoint2D start, IPoint2D end) {
			Sanity.IfTrueThrow( (start.X > end.X) || (start.Y > end.Y), 
				"The first argument is supposed to be the upper left corner and the second the lower right corner.");
			this.start = new Point2D(start);
			this.end = new Point2D(end);
		}
		
		public Rectangle2D(Point2D start, Point2D end) {
			Sanity.IfTrueThrow( (start.X > end.X) || (start.Y > end.Y), 
				"The first argument is supposed to be the upper left corner and the second the lower right corner.");
			this.start = start;
			this.end = end;
		}
		
		public Rectangle2D(ushort x, ushort y, ushort width, ushort height) {
			this.start = new Point2D(x, y);
			this.end = new Point2D((ushort) (x + width), (ushort) (y + height));
		}
		
		public Rectangle2D(ushort x, ushort y, ushort range) {
			this.start = new Point2D((ushort) (x - range), (ushort) (y - range));
			this.end = new Point2D((ushort) (x + range), (ushort) (y + range));
		}
		
		public Rectangle2D(IPoint2D center, ushort range) {
			ushort x = center.X;
			ushort y = center.Y;
			this.start = new Point2D((ushort) (x - range), (ushort) (y - range));
			this.end = new Point2D((ushort) (x + range), (ushort) (y + range));
		}
		
		public void Crop(ushort minx, ushort miny, ushort maxx, ushort maxy) {
			bool newStart = false;
			bool newEnd = false;
			ushort newStartX = start.X;
			ushort newStartY = start.Y;
			ushort newEndX = end.X;
			ushort newEndY = end.Y;
			if (this.StartPoint.X<minx) {
				newStartX = minx;
				newStart = true;
			}
			if (this.EndPoint.X<minx) {
				newEndX = minx;
				newEnd = true;
			}
			if (this.StartPoint.Y<miny) {
				newStartY = miny;
				newStart = true;
			}
			if (this.EndPoint.Y<miny) {
				newEndY = miny;
				newEnd = true;
			}
			if (this.StartPoint.X>maxx) {
				newStartX = maxx;
				newStart = true;
			}
			if (this.EndPoint.X>maxx) {
				newEndX = maxx;
				newEnd = true;
			}
			if (this.StartPoint.Y>maxy) {
				newStartY = maxy;
				newStart = true;
			}
			if (this.EndPoint.Y>maxy) {
				newEndY = maxy;
				newEnd = true;
			}
			if (newStart) {
				this.start = new Point2D(newStartX, newStartY);
			}
			if (newEnd) {
				this.end = new Point2D(newEndX, newEndY);
			}
		}
		
		public int X { get {
		    return start.X;
		} }
		
		public int Y { get {
		    return start.Y;
		} }
		
		public int Width { get {
			return (end.X - start.X);
		} }
		
		public int Height { get {
			return (end.Y - start.Y);
		} }

		public Point2D StartPoint { get {
		    return start;
		} }

		public Point2D EndPoint { get {
		    return end;
		} }
		
		public override string ToString() {
		   return string.Format("({0}, {1})+({2}, {3})", X, Y, Width, Height);   
		}
		
		public bool Contains(Static p) {
			ushort px = p.X;
			ushort py = p.Y;
			return ((start.X <= px) && (start.Y <= py) && (end.X >= px) && (end.Y >= py));
		}
		
		public bool Contains(Thing p) {
			ushort px = p.X;
			ushort py = p.Y;
			return ((start.X <= px) && (start.Y <= py) && (end.X >= px) && (end.Y >= py)); 
		}
		
		public bool Contains(Point2D p) {
			ushort px = p.X;
			ushort py = p.Y;
			return ((start.X <= px) && (start.Y <= py) && (end.X >= px) && (end.Y >= py));
		}

		public bool Contains(int px, int py) {
			return ((start.X <= px) && (start.Y <= py) && (end.X >= px) && (end.Y >= py));
		}
		
		public bool Contains(IPoint2D p) {
			ushort px = p.X;
			ushort py = p.Y;
			return ((start.X <= px) && (start.Y <= py) && (end.X >= px) && (end.Y >= py));
		}
		
		public static Rectangle2D GetIntersection(Rectangle2D a, Rectangle2D b) {
			int maxStartX = Math.Max(a.StartPoint.X, b.StartPoint.X);
			int minEndX = Math.Min(a.EndPoint.X, b.EndPoint.X);
			int maxStartY = Math.Max(a.StartPoint.Y, b.StartPoint.Y);
			int minEndY = Math.Min(a.EndPoint.Y, b.EndPoint.Y);
			if ((minEndX >= maxStartX) && (minEndY >= maxStartY)) {
				return new Rectangle2D((ushort) maxStartX, (ushort) maxStartY, 
					(ushort) (minEndX - maxStartX), (ushort) (minEndY - maxStartY));
			}
			return Rectangle2D.voidInstance;
		}
		
		public int TilesNumber { get {
			return ((end.X - start.X)*(end.Y - start.Y));
		} }
	}
}