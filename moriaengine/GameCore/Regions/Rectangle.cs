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

		public abstract int MinX { get; }
		public abstract int MaxX { get; }
		public abstract int MinY { get; }
		public abstract int MaxY { get; }

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

		public bool Contains(AbstractInternalItem p) {
			int px = p.X;
			int py = p.Y;
			return ((this.MinX <= px) && (this.MinY <= py) && (this.MaxX >= px) && (this.MaxY >= py));
		}

		public bool Contains(Thing p) {
			int px = p.X;
			int py = p.Y;
			return ((this.MinX <= px) && (this.MinY <= py) && (this.MaxX >= px) && (this.MaxY >= py));
		}

		public bool Contains(Point2D p) {
			int px = p.X;
			int py = p.Y;
			return ((this.MinX <= px) && (this.MinY <= py) && (this.MaxX >= px) && (this.MaxY >= py));
		}

		public bool Contains(int px, int py) {
			return ((this.MinX <= px) && (this.MinY <= py) && (this.MaxX >= px) && (this.MaxY >= py));
		}

		public bool Contains(IPoint2D p) {
			int px = p.X;
			int py = p.Y;
			return ((this.MinX <= px) && (this.MinY <= py) && (this.MaxX >= px) && (this.MaxY >= py));
		}

		public override string ToString() {
			return string.Format("({0}, {1})+({2}, {3})", this.MinX, this.MinY, this.MaxX, this.MaxY);
		}

		[Summary("Does the rectangle contain another rectangle completely?")]
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

		[Summary("Do the two rectangles have any intersection?")]
		public static bool Intersects(ImmutableRectangle a, ImmutableRectangle b) {
			return a.IntersectsWith(b) || b.IntersectsWith(a);
		}


		public int TilesNumber {
			get {
				return ((this.MaxX - this.MinX) * (this.MaxY - this.MinY));
			}
		}
	}

	public class ImmutableRectangle : AbstractRectangle {
		public static readonly ImmutableRectangle voidInstance = new ImmutableRectangle(0, 0, 0, 0);

		private readonly int minX, maxX, minY, maxY;

		public ImmutableRectangle(AbstractRectangle copiedOne) {
			this.minX = copiedOne.MinX;
			this.minY = copiedOne.MinY;
			this.maxX = copiedOne.MaxX;
			this.maxY = copiedOne.MaxY;
		}

		public ImmutableRectangle(int minX, int minY, int maxX, int maxY) {
			Sanity.IfTrueThrow((minX > maxX) || (minY > maxY),
				"Rectangle (" + minX + "," + minY + "," + maxX + "," + maxY + "). The first two arguments are supposed to be the upper left corner coordinates while the 3rd and 4th arguments coordinates of the lower right corner.");
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

		[Summary("Return a rectangle created from the central point with the specific range around the point" +
				"(square 'around')")]
		public ImmutableRectangle(int x, int y, int range) {
			this.minX = x - range;
			this.minY = y - range;
			this.maxX = x + range;
			this.maxY = y + range;
		}

		public ImmutableRectangle(IPoint2D center, int range)
			: this(center.X, center.Y, range) {
		}

		public static ImmutableRectangle GetIntersection(ImmutableRectangle a, ImmutableRectangle b) {
			ushort maxStartX = (ushort) Math.Max(a.minX, b.minX);
			ushort minEndX = (ushort) Math.Min(a.maxX, b.maxX);
			ushort maxStartY = (ushort) Math.Max(a.minY, b.minY);
			ushort minEndY = (ushort) Math.Min(a.maxY, b.maxY);
			if ((minEndX >= maxStartX) && (minEndY >= maxStartY)) {
				return new ImmutableRectangle(maxStartX, maxStartY, minEndX, minEndY);
			}
			return ImmutableRectangle.voidInstance;
		}

		public override sealed int MinX {
			get {
				return this.minX;
			}
		}

		public override sealed int MaxX {
			get {
				return this.maxX;
			}
		}

		public override sealed int MinY {
			get {
				return this.minY;
			}
		}

		public override sealed int MaxY {
			get {
				return this.maxY;
			}
		}
	}
}
