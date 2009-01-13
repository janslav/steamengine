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
	[Dialogs.ViewableClass]
	[Summary("Rectangle class for dialogs - the mutable one. It will be used for operating with " +
				"rectangles when editing region. After setting to the region it will be transformed to normal RegionRectangle")]
	public class MutableRectangle : AbstractRectangle {
		public ushort minX, minY, maxX, maxY;
		
		public MutableRectangle(AbstractRectangle copiedOne) {
			this.minX = copiedOne.MinX;
			this.minY = copiedOne.MinY;
			this.maxX = copiedOne.MaxX;
			this.maxY = copiedOne.MaxY;
		}

		[Summary("Return a rectangle created from the central point with the specific range around the point" +
				"(square 'around')")]
		public MutableRectangle(ushort x, ushort y, int range) {
			this.minX = (ushort) (x - range);
			this.minY = (ushort) (y - range);
			this.maxX = (ushort) (x + range);
			this.maxY = (ushort) (y + range);
		}

		[Summary("Create a rectangle using the center point and the area around (=>square)")]
		public MutableRectangle(IPoint4D center, ushort range)
			: this(center.X, center.Y, range) {
		}

		public MutableRectangle(ushort startX, ushort startY, ushort endX, ushort endY) {
			Sanity.IfTrueThrow((startX > endX) || (startY > endY),
				"MutableRectangle (" + startX + "," + startY + "," + endX + "," + endY + "). The first two arguments are supposed to be the upper left corner coordinates while the 3rd and 4th arguments coordinates of the lower right corner.");			
			this.minX = startX;
			this.minY = startY;
			this.maxX = endX;
			this.maxY = endY;
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

		[Summary("Alters all four rectangle's position coordinates for specified tiles in X and Y axes.")]
		public MutableRectangle Move(int timesX, int timesY) {
			minX += (ushort)timesX;
			maxX += (ushort)timesX;
			minY += (ushort)timesY;
			maxY += (ushort)timesY;

			return this;
		}

		[Summary("Takes the regions rectagles and makes a list of MutableRectangles for usage (copies the immutable ones)")]
		public static List<MutableRectangle> TakeRectsFromRegion(Region reg) {
			List<MutableRectangle> retList = new List<MutableRectangle>();
			foreach(ImmutableRectangle regRect in reg.Rectangles) {
				retList.Add(new MutableRectangle(regRect));
			}
			return retList;
		}
	}
}