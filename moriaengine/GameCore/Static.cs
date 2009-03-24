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
using System.IO;
using System.Collections;
using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.Regions;
using SteamEngine.Networking;

namespace SteamEngine {
	public abstract class Static : IPoint4D {
		public readonly ItemDispidInfo dispidInfo;
		internal ushort x;
		internal ushort y;
		internal sbyte z;
		private readonly byte m;

		internal Static(ushort id, byte m) {
			this.dispidInfo = ItemDispidInfo.Get(id);
			if (dispidInfo == null) {
				throw new SEException("No ItemDispidInfo for id 0x" + id.ToString("x") + ". Something's wrong.");
			}
			this.m = m;
		}

		internal Static(ushort id, ushort x, ushort y, sbyte z, byte m)
			: this(id, m) {
			this.x = x;
			this.y = y;
			this.z = z;
		}

		public string Name {
			get {
				return dispidInfo.singularName;
			}
		}

		public int Id {
			get {
				return dispidInfo.id;
			}
		}

		public int X {
			get {
				return x;
			}
		}

		public int Y {
			get {
				return y;
			}
		}

		public int Z {
			get {
				return z;
			}
		}

		public byte M {
			get {
				return m;
			}
		}

		public int Height {
			get {
				return dispidInfo.height;
			}
		}

		public TileFlag Flags {
			get {
				return dispidInfo.flags;
			}
		}

		public override string ToString() {
			return this.Name + " at " + X + "," + Y + "," + Z + "," + M;
		}

		public Map GetMap() {
			return Map.GetMap(m);
		}

		public void OverheadMessage(string arg) {
			OverheadMessage(arg, 0);
		}

		public void OverheadMessage(string arg, int color) {
			PacketSequences.SendOverheadMessageFrom(Globals.SrcTCPConnection, this, arg, (ushort) color);
		}

		public IPoint4D TopPoint {
			get {
				return this;
			}
		}

		IPoint3D IPoint3D.TopPoint {
			get {
				return this;
			}
		}

		IPoint2D IPoint2D.TopPoint {
			get {
				return this;
			}
		}
	}

	//StaticStatic as opposed to...ummm... you guessed it... DynamicStatic. Not.
	//...as opposed to MultiItemComponent ;)

	public class StaticStatic : Static {
		private readonly int color;

		internal StaticStatic(ushort id, ushort x, ushort y, sbyte z, byte m, int color)
			: base(id, x, y, z, m) {

			this.color = color;
		}

		public int Color {
			get { return color; }
		} 
	}
}
