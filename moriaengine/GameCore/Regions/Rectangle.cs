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
	public abstract class AbstractRectangle {
		public abstract ushort MinX { get; }
		public abstract ushort MaxX { get; }
		public abstract ushort MinY { get; }
		public abstract ushort MaxY { get; }

		public int Width {
			get {
				return this.MaxX - this.MinX;
			}
		}

		public int Height {
			get {
				return this.MaxY - this.MinY;
			}
		}		

		public bool Contains(Static p) {
			ushort px = p.X;
			ushort py = p.Y;
			return ((this.MinX <= px) && (this.MinY <= py) && (this.MaxX >= px) && (this.MaxY >= py));
		}

		public bool Contains(Thing p) {
			ushort px = p.X;
			ushort py = p.Y;
			return ((this.MinX <= px) && (this.MinY <= py) && (this.MaxX >= px) && (this.MaxY >= py));
		}

		public bool Contains(Point2D p) {
			ushort px = p.x;
			ushort py = p.y;
			return ((this.MinX <= px) && (this.MinY <= py) && (this.MaxX >= px) && (this.MaxY >= py));
		}

		public bool Contains(int px, int py) {
			return ((this.MinX <= px) && (this.MinY <= py) && (this.MaxX >= px) && (this.MaxY >= py));
		}

		public bool Contains(IPoint2D p) {
			ushort px = p.X;
			ushort py = p.Y;
			return ((this.MinX <= px) && (this.MinY <= py) && (this.MaxX >= px) && (this.MaxY >= py));
		}

		public override string ToString() {
			return string.Format("({0}, {1})+({2}, {3})", this.MinX, this.MinY, this.MaxX, this.MaxY);
		}

		[Remark("Does the rectangle contain another rectangle completely?")]
		public bool Contains(AbstractRectangle rect) {
			return Contains(rect.MinX, rect.MinY)//left lower
					&& Contains(rect.MinX, rect.MaxY) //left upper
					&& Contains(rect.MaxX, rect.MaxY) //right upper
					&& Contains(rect.MaxX, rect.MinY);//right lower
		}

		public bool IntersectsWith(AbstractRectangle rect) {
			return Contains(rect.MinX, rect.MinY)//left lower
					|| Contains(rect.MinX, rect.MaxY) //left upper
					|| Contains(rect.MaxX, rect.MaxY) //right upper
					|| Contains(rect.MaxX, rect.MinY);//right lower
		}

		[Remark("Do the two rectangles have any intersection?")]
		public static bool Intersects(ImmutableRectangle a, ImmutableRectangle b) {
			return a.IntersectsWith(b) || b.IntersectsWith(a);
		}


		public int TilesNumber {
			get {
				return ((this.MaxX - this.MinX)*(this.MaxY - this.MinY));
			}
		}
	}

	public class ImmutableRectangle : AbstractRectangle {
		public static readonly ImmutableRectangle voidInstance = new ImmutableRectangle(0, 0, 0, 0);

		public readonly ushort minX, maxX, minY, maxY;

		public ImmutableRectangle(AbstractRectangle copiedOne) {
			this.minX = copiedOne.MinX;
			this.minY = copiedOne.MinY;
			this.maxX = copiedOne.MaxX;
			this.maxY = copiedOne.MaxY;
		}

		public ImmutableRectangle(ushort minX, ushort minY, ushort maxX, ushort maxY) {
			Sanity.IfTrueThrow((minX > maxX) || (minY > maxY),
				"Rectangle ("+minX+","+minY+","+maxX+","+maxY+"). The first two arguments are supposed to be the upper left corner coordinates while the 3rd and 4th arguments coordinates of the lower right corner.");
			this.minX = minX;
			this.minY = minY;
			this.maxX = maxX;
			this.maxY = maxY;
		}

		/*public ImmutableRectangle(IPoint2D start, IPoint2D end) {
			Sanity.IfTrueThrow( (start.X > end.X) || (start.Y > end.Y), 
				"The first argument is supposed to be the upper left corner and the second the lower right corner.");
			this.minX = start.X;
			this.minY = start.Y;
			this.maxX = end.X;
			this.maxY = end.Y;
		}*/		

		[Remark("Return a rectangle created from the central point with the specific range around the point"+
				"(square 'around')")]
		public ImmutableRectangle(ushort x, ushort y, ushort range) {
			this.minX = (ushort)(x - range);
			this.minY = (ushort)(y - range);
			this.maxX = (ushort)(x + range);
			this.maxY = (ushort)(y + range);
		}
		
		public ImmutableRectangle(IPoint2D center, ushort range) : this(center.X, center.Y, range) {			
		}		

		public static ImmutableRectangle GetIntersection(ImmutableRectangle a, ImmutableRectangle b) {
			ushort maxStartX = (ushort)Math.Max(a.minX, b.minX);
			ushort minEndX = (ushort)Math.Min(a.maxX, b.maxX);
			ushort maxStartY = (ushort)Math.Max(a.minY, b.minY);
			ushort minEndY = (ushort)Math.Min(a.maxY, b.maxY);
			if ((minEndX >= maxStartX) && (minEndY >= maxStartY)) {
				return new ImmutableRectangle(maxStartX, maxStartY, minEndX, minEndY);
			}
			return ImmutableRectangle.voidInstance;
		}

		public override sealed ushort MinX {
			get {
				return this.minX;
			}
		}

		public override sealed ushort MaxX {
			get {
				return this.maxX;
			}
		}

		public override sealed ushort MinY {
			get {
				return this.minY;
			}
		}

		public override sealed ushort MaxY {
			get { 
				return this.maxY; 
			}
		}
	}	
}
