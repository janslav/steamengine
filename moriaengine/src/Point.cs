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
using System.Text.RegularExpressions;
//using System.IO;
using System.Collections;
using System.Collections.Generic;
using SteamEngine.Regions;

namespace SteamEngine {
	public interface IPoint2D {
		ushort X { get; } //Map X coordinate (Low is west, high is east)
		ushort Y { get; } //Map Y coordinate (Low is north, high is south)
		IPoint2D TopPoint { get; }
	}

	public interface IPoint3D : IPoint2D {
		sbyte Z { get; } //Z coordinate (Low is lower, high is higher)
		new IPoint3D TopPoint { get; }
	}

	public interface IPoint4D : IPoint3D {
		byte M { get; } //Mapplane (0 is default, 255 can see and be seen by all planes (theoretically -tar))

		new IPoint4D TopPoint { get; }
		Map GetMap();

		//IEnumerable ThingsInRange();
		//IEnumerable ItemsInRange();
		//IEnumerable CharsInRange();
		//IEnumerable PlayersInRange();
		//IEnumerable StaticsInRange();
		//IEnumerable DisconnectsInRange();
		//IEnumerable MultiComponentsInRange();

		//IEnumerable ThingsInRange(ushort range);
		//IEnumerable ItemsInRange(ushort range);
		//IEnumerable CharsInRange(ushort range);
		//IEnumerable PlayersInRange(ushort range);
		//IEnumerable StaticsInRange(ushort range);
		//IEnumerable DisconnectsInRange(ushort range);
	}

	//public interface IPoint5D : IPoint4D {
	//    int Height { get; }
	//    The height to consider this to be. This is used for LOS checking. 
	//    People are about 10 units (Height is in z units) in height.
	//}
	//public interface IPoint6D : IPoint5D {
	//    Thing TopObj();
	//    IPoint5D TopPos { get; }
	//    new IPoint5D TopPoint { get; }
	//    bool IsOnGround { get; }
	//    bool IsInContainer { get; }
	//    bool IsInvisible { get; }
	//    Thing Cont { get; }
	//}

	public class Point2D : IPoint2D {
		internal ushort x;	//Map X coordinate (Low is west, high is east)
		internal ushort y;

		[Common.Remark("One static point used for all possible checkings (such as Rectangle2D.Contains(Point2D)"+
					   "where we dont care for the point itself but rather for its position."+
					   "NEVER use it in any object since it can be modified anywhere and anyhow!"+
					   "Its purpose is to avoid creating of many instances od Point2D during some processing "+
					   "(such as moving the dynamic region, determining safe location, distance etc.)")]
		private static Point2D common = new Point2D(0, 0);

		public static int GetSimpleDistance(ushort ax, ushort ay, ushort bx, ushort by) {
			return Math.Max(Math.Abs(ax-bx), Math.Abs(ay-by));
		}

		public static int GetSimpleDistance(Point2D a, Point2D b) {
			return Math.Max(Math.Abs(a.X-b.X), Math.Abs(a.Y-b.Y));
		}

		public static int GetSimpleDistance(IPoint2D a, IPoint2D b) {
			a = a.TopPoint;
			b = b.TopPoint;
			return Math.Max(Math.Abs(a.X-b.X), Math.Abs(a.Y-b.Y));
		}

		public static Direction GetDirFromTo(IPoint2D start, IPoint2D target) {
			// Get the 2D direction between points.
			start = start.TopPoint;
			target = target.TopPoint;

			int dx = (start.X-target.X);
			int dy = (start.Y-target.Y);

			int ax = Math.Abs(dx);
			int ay = Math.Abs(dy);

			if (ay > ax) {
				if (ax == 0) {
					return ((dy > 0) ? Direction.North : Direction.South);
				}
				int slope = ay / ax;
				if (slope > 2) {
					return ((dy > 0) ? Direction.North : Direction.South);
				}
				if (dx > 0) {	// westish
					return ((dy > 0) ? Direction.NorthWest : Direction.SouthWest);
				}
				return ((dy > 0) ? Direction.NorthEast : Direction.SouthEast);
			} else {
				if (ay == 0) {
					if (dx == 0)
						return (Direction.North);	// here ?
					return ((dx > 0) ? Direction.West : Direction.East);
				}
				int slope = ax / ay;
				if (slope > 2) {
					return ((dx > 0) ? Direction.West : Direction.East);
				}
				if (dy > 0) {
					return ((dx > 0) ? Direction.NorthWest : Direction.NorthEast);
				}
				return ((dx > 0) ? Direction.SouthWest : Direction.SouthEast);
			}
		}

		[Common.Remark("Add the diff's X and Y to owns X and Y")]
		public Point2D Add(int diffX, int diffY) {
			return new Point2D((ushort)(x + diffX), (ushort)(y + diffY));
		}

		[Common.Remark("Set the 'commons' coordinates and return it - e.g. for providing some checkings."+
					   "This method is NOT intended to be used for obtaining a usable instance of Point2D!!!"+
					   "The Point2D that is returned is to be used only immediately and never stored anywhere!"+
					   "The singletonized instance of Point2D is used to avoid creating many small objects during"+
					   "some processing - such as dynamic region movement etc")]
		public static Point2D GetPosition(ushort x, ushort y) {
			common.x = x;
			common.y = y;
			return common;
		}

		public Point2D(ushort x, ushort y) {
			this.x = x;
			this.y = y;
		}

		public Point2D(Point2D p) {
			this.x = p.x;
			this.y = p.y;
		}

		public Point2D(IPoint2D p) {
			this.x = p.X;
			this.y = p.Y;
		}

		public static bool Equals(Point2D a, Point2D b) {
			return ((a.x == b.x) && (a.y == b.y));
		}

		public static bool Equals(IPoint2D a, IPoint2D b) {
			return ((a.X == b.X) && (a.Y == b.Y));
		}

		public ushort X {
			get {
				return x;
			}
		}

		public ushort Y {
			get {
				return y;
			}
		}

		public override string ToString() {
			return "("+x+","+y+")";
		}

		public static bool operator==(Point2D first, Point2D second) {
			if (object.ReferenceEquals(first, null)) {
				if (object.ReferenceEquals(second, null)) {
					return true;
				}
				return false;
			} else if (object.ReferenceEquals(second, null)) {
				return false;
			}
			return ((first.x == second.x) && (first.y == second.y));
		}

		public override bool Equals(object o) {
			Point2D p = o as Point2D;
			if (p != null) {
				return ((x == p.x) && (y == p.y));
			}
			IPoint2D ip = o as IPoint2D;
			if (ip != null) {
				return ((x == ip.X) && (y == ip.Y));
			}
			return false;
		}

		public static bool operator!=(Point2D first, Point2D second) {
			return !(first==second);
		}

		public override int GetHashCode() {
			return (37*17^x)^y;
		}

		public IPoint2D TopPoint {
			get { return this; }
		}
	}

	public class Point3D : Point2D, IPoint3D {
		internal sbyte z;

		public Point3D(ushort x, ushort y, sbyte z)
			: base(x, y) {
			this.z = z;
		}

		public Point3D(Point3D p)
			: base(p) {
			this.z = p.z;
		}

		public Point3D(IPoint3D p)
			: base(p) {
			this.z = p.Z;
		}

		[Common.Remark("Add the diff's X, Y and Z to owns X, Y and Z")]
		public Point3D Add(int diffX, int diffY, int diffZ) {
			return new Point3D((ushort)(x + diffX), (ushort)(y + diffY), (sbyte)(z + diffZ));
		}

		public static bool Equals(Point3D a, Point3D b) {
			return ((a.x == b.x) && (a.y == b.y) && (a.z == b.z));
		}

		public static bool Equals(IPoint3D a, IPoint3D b) {
			return ((a.X == b.X) && (a.Y == b.Y) && (a.Z == b.Z));
		}

		public sbyte Z {
			get {
				return z;
			}
		}

		public override string ToString() {
			return "("+x+","+y+","+z+")";
		}

		public static bool operator==(Point3D first, Point3D second) {
			if (object.ReferenceEquals(first, null)) {
				if (object.ReferenceEquals(second, null)) {
					return true;
				}
				return false;
			} else if (object.ReferenceEquals(second, null)) {
				return false;
			}
			return ((first.x == second.x) && (first.y == second.y) && (first.z == second.z));
		}

		public override bool Equals(object o) {
			Point3D p = o as Point3D;
			if (p != null) {
				return ((x == p.X) && (y == p.Y) && (z == p.Z));
			}
			IPoint3D ip = o as IPoint3D;
			if (ip != null) {
				return ((x == ip.X) && (y == ip.Y) && (z == ip.Z));
			}
			return false;
		}

		public static bool operator!=(Point3D first, Point3D second) {
			return !(first==second);
		}

		public override int GetHashCode() {
			return ((37*17^x)^y)^z;
		}

		public new IPoint3D TopPoint {
			get { return this; }
		}
	}

	internal class MutablePoint4D {
		internal ushort x;
		internal ushort y;
		internal sbyte z;
		internal byte m;

		public MutablePoint4D(ushort x, ushort y, sbyte z, byte m) {
			this.SetP(x, y, z, m);
		}

		public MutablePoint4D(ushort x, ushort y, sbyte z)
			: this(x, y, z, 0) {
		}

		public MutablePoint4D(ushort x, ushort y)
			: this(x, y, 0, 0) {
		}

		public MutablePoint4D(MutablePoint4D p)
			: this(p.x, p.y, p.z, p.m) {
		}

		public MutablePoint4D(Point4D p)
			: this(p.X, p.Y, p.Z, p.M) {
		}

		public MutablePoint4D(IPoint4D p)
			: this(p.X, p.Y, p.Z, p.M) {
		}

		public static bool Equals(MutablePoint4D a, MutablePoint4D b) {
			return ((a.x == b.x) && (a.y == b.y) && (a.z == b.z) && (a.m == b.m));
		}

		internal static void Parse(MutablePoint4D point, string value) {
			Match m = Point4D.positionRE.Match(value);
			if (m.Success) {
				GroupCollection gc=m.Groups;
				ushort thisx = TagMath.ParseUInt16(gc["x"].Value);
				ushort thisy = TagMath.ParseUInt16(gc["y"].Value);
				string zstr=gc["z"].Value;
				string mstr=gc["m"].Value;
				sbyte thisz;
				byte thism;
				if (zstr.Length>0) {
					thisz = TagMath.ParseSByte(zstr);
					if (mstr.Length>0) {
						thism = TagMath.ParseByte(mstr);
					} else {
						thism = 0;
					}
				} else {
					thisz = 0;
					thism = 0;
				}

				point.SetP(thisx, thisy, thisz, thism);
			} else {
				throw new SEException("Invalid input string for Point4D parse: '"+value+"'");
			}
		}

		internal void SetP(ushort x, ushort y, sbyte z, byte m) {
			this.x = x;
			this.y = y;
			this.z = z;
			this.m = m;
		}

		internal void SetP(IPoint4D p) {
			SetP(p.X, p.Y, p.Z, p.M);
		}

		internal void SetP(ushort x, ushort y, sbyte z) {
			this.x = x;
			this.y = y;
			this.z = z;
		}
	}

	public sealed class Point4D : Point3D, IPoint4D {
		internal byte m;

		public Point4D(ushort x, ushort y, sbyte z, byte m)
			: base(x, y, z) {
			this.m = m;
		}

		public Point4D(ushort x, ushort y, sbyte z)
			: this(x, y, z, 0) {
		}

		public Point4D(ushort x, ushort y)
			: this(x, y, 0, 0) {
		}


		internal Point4D(MutablePoint4D p)
			: this(p.x, p.y, p.z, p.m) {
		}

		public Point4D(Point4D p)
			: base(p) {
			this.m = p.M;
		}

		public Point4D(IPoint4D p)
			: base(p) {
			this.m = p.M;
		}

		public static bool Equals(Point4D a, Point4D b) {
			return ((a.x == b.x) && (a.y == b.y) && (a.z == b.z) && (a.m == b.m));
		}

		public static bool Equals(IPoint4D a, IPoint4D b) {
			return ((a.X == b.X) && (a.Y == b.Y) && (a.Z == b.Z) && (a.M == b.M));
		}

		public byte M {
			get {
				return m;
			}
		}

		public static Point4D Parse(string value) {
			Match m = positionRE.Match(value);
			if (m.Success) {
				GroupCollection gc=m.Groups;
				ushort thisx = TagMath.ParseUInt16(gc["x"].Value);
				ushort thisy = TagMath.ParseUInt16(gc["y"].Value);
				string zstr=gc["z"].Value;
				string mstr=gc["m"].Value;
				sbyte thisz;
				byte thism;
				if (zstr.Length>0) {
					thisz = TagMath.ParseSByte(zstr);
					if (mstr.Length>0) {
						thism = TagMath.ParseByte(mstr);
					} else {
						thism = 0;
					}
				} else {
					thisz = 0;
					thism = 0;
				}
				return new Point4D(thisx, thisy, thisz, thism);
			} else {
				throw new SEException("Invalid input string for Point4D parse: '"+value+"'");
			}
		}

		internal static Regex positionRE = new Regex(@"\s*(?<x>\d+)\s*(,|\s)\s*(?<y>\d+)\s*((,|\s)\s*(?<z>-?\d+))?\s*((,|\s)\s*(?<m>\d+))?\s*",
			RegexOptions.IgnoreCase|RegexOptions.CultureInvariant|RegexOptions.Compiled);

		public override string ToString() {
			return "("+x+","+y+","+z+","+m+")";
		}

		public string ToNormalString() {
			return x + "," + y + "," + z + "," + m;
		}

		public static bool operator==(Point4D first, Point4D second) {
			if (object.ReferenceEquals(first, null)) {
				if (object.ReferenceEquals(second, null)) {
					return true;
				}
				return false;
			} else if (object.ReferenceEquals(second, null)) {
				return false;
			}
			return ((first.x == second.x) && (first.y == second.y)&& (first.z == second.z) && (first.m == second.m));
		}

		public override bool Equals(object o) {
			Point4D p = o as Point4D;
			if (p != null) {
				return ((this.X == p.X) && (this.Y == p.Y) && (this.Z == p.Z) && (this.M == p.M));
			}
			IPoint4D ip = o as IPoint4D;
			if (ip != null) {
				return ((this.X == ip.X) && (this.Y == ip.Y) && (this.Z == ip.Z) && (this.M == ip.M));
			}
			return false;
		}

		public static bool operator!=(Point4D first, Point4D second) {
			return !(first==second);
		}

		public override int GetHashCode() {
			return (((37*17^x)^y)^z)^m;
		}

		public Map GetMap() {
			return Map.GetMap(m);
		}

		public new IPoint4D TopPoint {
			get { return this; }
		}
	}
}