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
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using SteamEngine;
using SteamEngine.Common;
using SteamEngine.Persistence;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {
	
	public class TrackPoint {
		private Point4D location;
		private Player owner;
		private TimeSpan lastStepTime;
		private ushort model;//model of the "footprint"

		private uint fakeUID;

		public TrackPoint(Point4D location, Player owner) {
			this.location = location;
			this.owner = owner;
		}

		public Point4D Location {
			get {
				return location;
			}
		}

		public ushort Model {
			get {
				return model;
			}
			set {//we need the setter for refreshing
				model = value;
			}
		}

		public Player Owner {
			get {
				return Owner;
			}
		}

		public TimeSpan LastStepTime {
			get {
				return lastStepTime;
			}
			set {//we need the setter for refreshing
				lastStepTime = value;
			}
		}

		public uint FakeUID {
			get {
				return fakeUID;
			}

			internal set {
				fakeUID = value;
			}
		}

		public override bool Equals(object o) {
			TrackPoint tp = o as TrackPoint;
			if (tp != null) {
				return (location.Equals(tp.location) && //same point
						(owner == tp.owner)); //for the same Character
			}
			return false;
		}

		public override int GetHashCode() {
			return location.GetHashCode();
		}
	}
}