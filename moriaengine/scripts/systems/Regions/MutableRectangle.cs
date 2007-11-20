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
using SteamEngine;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {
	[Remark("Rectangle class for dialogs - the mutable one. It will be used for operating with " +
				"rectngles when editing region. After setting to the region it will be transformed to normal RegionRectangle")]
	public class MutableRectangle : AbstractRectangle {
		public ushort minX, maxX, minY, maxY;

		public MutableRectangle(Point2D start, Point2D end) {
			this.minX = start.x;
			this.minY = start.y;
			this.maxX = end.x;
			this.maxY = end.y;
		}

		public MutableRectangle(IPoint2D start, IPoint2D end) {
			this.minX = start.X;
			this.minY = start.Y;
			this.maxX = end.X;
			this.maxY = end.Y;
		}

		public override ushort MinX {
			get {
				return minX;
			}
		}

		public override ushort MinY {
			get {
				return minY;
			}
		}

		public override ushort MaxX {
			get {
				return maxX;
			}
		}

		public override ushort MaxY {
			get {
				return maxY;
			}
		}

		//public Point2D StartPoint {
		//    get {
		//        return StartPoint;
		//    }
		//    set {
		//        minX = value.x;
		//        minY = value.y;
		//    }
		//}

		//public Point2D EndPoint {
		//    get {
		//        return EndPoint;
		//    }
		//    set {
		//        maxX = value.x;
		//        maxY = value.y;
		//    }
		//}

		[Remark("Alters all four rectangle's position coordinates for specified tiles in X and Y axes."+
				"This time it changes 'this'")]
		public MutableRectangle Move(int timesX, int timesY) {
			minX += (ushort)(minX + timesX);
			maxX += (ushort)(maxX + timesX);
			minY += (ushort)(minY + timesY);
			maxY += (ushort)(maxY + timesY);

			return this;
		}

		[Remark("Takes the regions rectagles and makes a list of MutableRectangles for usage (copies the unmutable ones)")]
		public static List<MutableRectangle> TakeRectsFromRegion(Region reg) {
			List<MutableRectangle> retList = new List<MutableRectangle>();
			foreach(ImmutableRectangle regRect in reg.Rectangles) {
				retList.Add(new MutableRectangle(regRect.StartPoint, regRect.EndPoint));
			}
			return retList;
		}
	}
}