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
		protected ushort x;	//Map X coordinate (Low is west, high is east)
		protected ushort y;

		public static int GetSimpleDistance(ushort ax, ushort ay, ushort bx, ushort by) {
			return Math.Max(Math.Abs(ax-bx), Math.Abs(ay-by));
		}

		public static int GetSimpleDistance(Point2D a, Point2D b) {
			return Math.Max(Math.Abs(a.X-b.X), Math.Abs(a.Y-b.Y));
		}

		public static int GetSimpleDistance(IPoint2D a, IPoint2D b) {
			return Math.Max(Math.Abs(a.X-b.X), Math.Abs(a.Y-b.Y));
		}

		public static Direction GetDirFromTo(IPoint2D start, IPoint2D target) {
			// Get the 2D direction between points.
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
		protected sbyte z;

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
			this.x = x;
			this.y = y;
			this.z = z;
			this.m = m;
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

		internal static MutablePoint4D Parse(string value) {
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
				return new MutablePoint4D(thisx, thisy, thisz, thism);
			} else {
				throw new SEException("Invalid input string for Point4D parse: '"+value+"'");
			}
		}

	}

	public class Point4D : Point3D, IPoint4D {
		protected byte m;

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

	//public class Point5D : Point4D, IPoint5D {
	//    public int height;			//The height to consider this to be. This is used for LOS checking. 
	//    //People are about 10 units (Height is in z units) in height.

	//    public Point5D(ushort x, ushort y, sbyte z, byte m, int height)
	//        : base(x, y, z, m) {
	//        this.height=height;
	//    }

	//    //public Point5D (Thing t):base(t) {
	//    //	this.height=t.height;
	//    //}
	//    //public Point5D (Static t):base(t) {
	//    //	this.height=t.height;
	//    //}

	//    internal Point5D(MutablePoint4D t, int height)
	//        : this(t.x, t.y, t.z, t.m, height) {
	//    }

	//    public Point5D(Point4D t, int height)
	//        : base(t) {
	//        this.height=height;
	//    }
	//    public Point5D(IPoint4D t, int height)
	//        : base(t) {
	//        this.height=height;
	//    }

	//    public Point5D(Point5D t)
	//        : base(t) {
	//        this.height=t.height;
	//    }

	//    public Point5D(IPoint5D t)
	//        : base(t) {
	//        this.height=t.Height;
	//    }

	//    public int Height {
	//        get {
	//            return height;
	//        }
	//    }

	//    public override string ToString() {
	//        return "("+x+","+y+","+z+","+m+", height:"+height+")";
	//    }

	//    public static bool operator==(Point5D first, Point5D second) {
	//        if (object.ReferenceEquals(first, null)) {
	//            if (object.ReferenceEquals(second, null)) {
	//                return true;
	//            }
	//            return false;
	//        } else if (object.ReferenceEquals(second, null)) {
	//            return false;
	//        }
	//        return ((first.X == second.X) && (first.Y == second.Y)&& (first.Z == second.Z) && (first.M == second.M));
	//    }

	//    public override bool Equals(object o) {
	//        if (o is Point5D) {
	//            Point5D c=(Point5D) o;
	//            return ((this.X == c.X) && (this.Y == c.Y) && (this.Z == c.Z) && (this.M == c.M) && (this.height == c.height));
	//        } else if (o is IPoint5D) {
	//            IPoint5D p = (IPoint5D) o;
	//            return ((this.X == p.X) && (this.Y == p.Y) && (this.Z == p.Z) && (this.M == p.M) && (this.height == p.Height));
	//        }
	//        return false;
	//    }

	//    public static bool operator!=(Point5D first, Point5D second) {
	//        return !(first==second);
	//    }

	//    public override int GetHashCode() {
	//        throw new NotSupportedException();
	//    }
	//}

	//public class Point6D : Point5D, IPoint6D {
	//    private Thing topObj;
	//    private Thing cont;
	//    private IPoint5D topPoint;
	//    private bool isInvisible;

	//    public Point6D(ushort x, ushort y, sbyte z, byte m, int height, Thing topObj, Thing cont, bool isInvisible)
	//        : base(x, y, z, m, height) {
	//        this.topObj=topObj;
	//        this.cont=cont;
	//        this.isInvisible=isInvisible;
	//    }

	//    public Point6D(IPoint5D t, Thing topObj, Thing cont, bool isInvisible)
	//        : base(t) {
	//        this.topObj=topObj;
	//        this.topPoint = topObj.P5D;
	//        this.cont=cont;
	//        this.isInvisible=isInvisible;
	//    }

	//    //Used to create a P6D for Effect.
	//    internal Point6D(MutablePoint4D pt)
	//        : base(pt.x, pt.y, pt.z, 255, 1) {
	//        this.topObj=null;
	//        this.cont=null;
	//        this.topPoint = this;// :)
	//        this.isInvisible=false;
	//    }

	//    //Used to create a P6D for Effect.
	//    public Point6D(IPoint3D pt)
	//        : base(pt.X, pt.Y, pt.Z, 255, 1) {
	//        this.topObj=null;
	//        this.cont=null;
	//        this.topPoint = this;// :)
	//        this.isInvisible=false;
	//    }

	//    public Point6D(IPoint6D t)
	//        : base(t) {
	//        this.topObj=t.TopObj();
	//        this.topPoint = new Point5D(t.TopPoint);
	//        this.cont=t.Cont;
	//        this.isInvisible=t.IsInvisible;
	//    }

	//    public IPoint5D TopPoint {
	//        get {
	//            return new Point5D(topPoint);
	//        }
	//    }
	//    public IPoint5D TopPos {
	//        get {
	//            return new Point5D(topPoint);
	//        }
	//    }

	//    public Thing TopObj() {
	//        return topObj;
	//    }
	//    public Thing Cont {
	//        get {
	//            return cont;
	//        }
	//        set {
	//            cont=value;
	//        }
	//    }

	//    public bool IsEquipped {
	//        get {
	//            return (cont!=null && cont.IsChar);
	//        }
	//    }

	//    public bool IsOnGround {
	//        get {
	//            return (cont==null);
	//        }
	//    }

	//    public bool IsInContainer {
	//        get {
	//            return (cont!=null && cont.IsItem);
	//        }
	//    }
	//    public bool IsInvisible {
	//        get {
	//            return (isInvisible);
	//        }
	//    }

	//    public override string ToString() {
	//        return "("+x+","+y+","+z+","+m+", height:"+height+", topObj:"+topObj+", cont:"+cont+", isInvisible:"+isInvisible+")";
	//    }

	//    public static bool operator==(Point6D first, Point6D second) {
	//        if (object.ReferenceEquals(first, null)) {
	//            if (object.ReferenceEquals(second, null)) {
	//                return true;
	//            }
	//            return false;
	//        } else if (object.ReferenceEquals(second, null)) {
	//            return false;
	//        }
	//        return ((first.X == second.X) && (first.Y == second.Y)&& (first.Z == second.Z) && (first.M == second.M) && (first.TopObj()==second.TopObj()) && (first.Cont==second.Cont) && (first.IsInvisible==second.IsInvisible));
	//    }

	//    public override bool Equals(object o) {
	//        if (o is Point6D) {
	//            Point6D c=(Point6D) o;
	//            return ((this.X == c.X) && (this.Y == c.Y) && (this.Z == c.Z) && (this.M == c.M) && (this.height == c.height) && (this.TopObj()==c.TopObj()) && (this.Cont==c.Cont) && (this.IsInvisible==c.IsInvisible));
	//        } else if (o is IPoint6D) {
	//            IPoint6D p = (IPoint6D) o;
	//            return ((this.X == p.X) && (this.Y == p.Y) && (this.Z == p.Z) && (this.M == p.M) && (this.height == p.Height) && (this.TopObj()==p.TopObj()) && (this.Cont==p.Cont) && (this.IsInvisible==p.IsInvisible));
	//        }
	//        return false;
	//    }

	//    public static bool operator!=(Point6D first, Point6D second) {
	//        return !(first==second);
	//    }

	//    public override int GetHashCode() {
	//        throw new NotSupportedException();
	//    }
	//}
}