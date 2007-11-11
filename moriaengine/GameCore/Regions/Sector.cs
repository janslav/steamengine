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
using System.Text;
using System.Collections;
using System.Collections.Generic;
using SteamEngine.Common;
using System.Diagnostics;

namespace SteamEngine.Regions {
	public partial class Map {
		[Remark("Take the rectangle, find all sectors it belongs to and chek every dynamic region rectangle in the"+
				"sector that the do not intersect")]
		internal bool CheckDynRectIntersection(RegionRectangle rect) {
			foreach(Sector sector in GetSectorsInRectangle(rect)) {//all sectors the examined rectangle belongs to
				foreach(RegionRectangle existingRect in sector.RegionRectangles) {//all dynamic regions from the sector
					if(existingRect.IntersectsWith(rect)) { //intersection check
						return false; //problem here, stop trying !
					}
				}				
			}
			return true;
		}

		private class Sector {
			internal ThingLinkedList things;
			internal LinkedList<AbstractCharacter> players;
			internal ThingLinkedList disconnects;//disconnected players/mounts/whatever
			internal MultiComponentLinkedList multiComponents;
			private StaticSector staticSector;
			public readonly int sx;//first index in the sector 2d array
			public readonly int sy;//second index
			public readonly byte m;//mapplane
			private RegionRectangle[] rectangles = RegionRectangle.emptyArray;
			private LinkedList<RegionRectangle> dynamicRegionRects;

			//internal static Sector voidSector;
			//static Sector() {
			//    voidSector = new Sector(ushort.MaxValue, ushort.MaxValue);
			//    voidSector.players = List<AbstractCharacter>.ReadOnly(voidSector.players.read);
			//    voidSector.things = ArrayList.ReadOnly(voidSector.things);
			//    voidSector.disconnects = ArrayList.ReadOnly(voidSector.disconnects);
			//    //the void sector will complain if someone tries to add/remove things to/from it
			//}

			internal Sector(int sx, int sy, byte m) {
				this.sx = sx;
				this.sy = sy;
				this.m = m;
				ClearThings();
			}

			public override string ToString() {
				return string.Format("sector with coords {0}, {1}, mapplane {2}", sx, sy, m);
			}

			#region dynamic stuff
			internal void ClearThings() {
				things = new ThingLinkedList(this);
				players = new LinkedList<AbstractCharacter>();
				disconnects = new ThingLinkedList(this);
				multiComponents = new MultiComponentLinkedList();
			}

			internal void Add(Thing thing) {
				Sanity.IfTrueThrow(thing==null, "You can't tell us to add a NULL thing to the sector!");
				if (thing.Flag_Disconnected) {
					AddDisconnected(thing);
				} else {
					if (thing.IsPlayer) {
						AddPlayer((AbstractCharacter) thing);
					}
					AddThing(thing);
				}
			}

			internal void Remove(Thing thing) {
				Sanity.IfTrueThrow(thing==null, "You can't tell us to remove a NULL thing from the sector!");
				if (thing.contOrTLL == disconnects) {
					RemoveDisconnected(thing);
				} else {
					if (thing.IsPlayer) {
						RemovePlayer((AbstractCharacter) thing);
					}
					RemoveThing(thing);
				}
			}

			internal void Disconnected(Thing thing) {
				Sanity.IfTrueThrow(thing==null, "You can't tell us a NULL thing has disconnected!");
				Sanity.IfTrueThrow(thing.contOrTLL == disconnects, "Disconnected("+thing+") was called, but that thing is already in our list of disconnected stuff!");
				if (thing.IsPlayer) {
					RemovePlayer((AbstractCharacter) thing);
				}
				RemoveThing(thing);
				AddDisconnected(thing);
			}

			internal void Reconnected(Thing thing) {
				Sanity.IfTrueThrow(thing==null, "You can't tell us a NULL thing has reconnected!");
				Sanity.IfTrueThrow(thing.contOrTLL != disconnects, "Reconnected("+thing+") was called, but that thing is not in our list of disconnected stuff!");
				RemoveDisconnected(thing);
				if (thing.IsPlayer) {
					AddPlayer((AbstractCharacter) thing);
				}
				AddThing(thing);
			}

			internal void MadeIntoNonPlayer(AbstractCharacter cre) {
				Sanity.IfTrueThrow(cre==null, "You can't tell us a NULL character is now a non-player!");
				Sanity.IfTrueThrow(cre.IsPlayer, "MadeIntoNonPlayer("+cre+") was called, but that character is actually still a player!");
				RemovePlayer(cre);
			}

			internal void MadeIntoPlayer(AbstractCharacter cre) {
				Sanity.IfTrueThrow(cre==null, "You can't tell us a NULL character is now a player!");
				Sanity.IfTrueThrow(!cre.IsPlayer, "MadeIntoPlayer("+cre+") was called, but that character isn't actually a player!");
				AddPlayer(cre);
			}

			private void AddThing(Thing thing) {
				Sanity.IfTrueThrow(thing.contOrTLL == things, "Sector's AddThing("+thing+") was called, but that thing is ALREADY in our list of things!");
				things.Add(thing);
			}

			private void AddPlayer(AbstractCharacter player) {
				Sanity.IfTrueThrow(players.Contains(player), "Sector's AddPlayer("+player+") was called, but that character is ALREADY in our list of players!");
				players.AddLast(player);
			}

			private void AddDisconnected(Thing thing) {
				Sanity.IfTrueThrow(thing.contOrTLL == disconnects, "Sector's AddDisconnected("+thing+") was called, but that thing is ALREADY in our list of disconnected things!");
				disconnects.Add(thing);
			}

			private void RemoveThing(Thing thing) {
				Sanity.IfTrueThrow(thing.contOrTLL != things,
					"Sector's RemoveThing("+thing+") was called, but that thing isn't in our list of things.");

				things.Remove(thing);
				thing.contOrTLL = null;
			}

			private void RemovePlayer(AbstractCharacter thing) {
				if (players.Remove(thing)) {
					Sanity.IfTrueThrow(players.Contains(thing), "Removed player still exists in ArrayList - It was in there multiple times?");
				} else {
					Sanity.IfTrueSay(true, "Sector's RemovePlayer("+thing+") was called, but that character isn't in our list of players.");
				}
			}

			internal void AddMultiComponent(MultiItemComponent component) {
				Sanity.IfTrueThrow(component==null, "You can't tell us to add a NULL MultiItemComponent to the sector!");
				Sanity.IfTrueThrow(component.collection != null, "You can't tell us to add a MultiItemComponent which is added elsewhere already!");
				
				this.multiComponents.Add(component);
			}

			internal void RemoveMultiComponent(MultiItemComponent component) {
				Sanity.IfTrueThrow(component==null, "You can't tell us to remove a NULL MultiItemComponent to the sector!");
				Sanity.IfTrueThrow(component.collection != this.multiComponents, "You can't tell us to remove a MultiItemComponent which is added elsewhere!");

				this.multiComponents.Remove(component);
			}

			private void RemoveDisconnected(Thing thing) {
				Sanity.IfTrueThrow(thing.contOrTLL != disconnects,
					"Sector's RemoveThing("+thing+") was called, but that thing isn't in our list of disconnects.");

				disconnects.Remove(thing);
				thing.contOrTLL = null;
			}
			#endregion dynamic stuff

			#region Static stuff
			private void LoadStatics() {
				if (staticSector == null) {
					staticSector = StaticSector.GetStaticSector(sx, sy, m);
				}
			}

			internal Static[] Statics {
				get {
					LoadStatics();
					return staticSector.statics;
				}
			}

			internal Static GetStatic(int x, int y, int z, int staticId) {
				LoadStatics();
				return staticSector.GetStatic(x, y, z, staticId);
			}

			internal bool HasStaticId(int x, int y, int staticId) {
				LoadStatics();
				return staticSector.HasStaticId(x, y, staticId);
			}

			internal ushort GetTileId(int x, int y) {
				LoadStatics();
				int basex= x&Map.sectorAnd;
				int basey= y&Map.sectorAnd;

				int relX= x-basex;
				int relY= y-basey;
				return staticSector.GetTileId(relX, relY);
			}

			internal sbyte GetTileZ(int x, int y) {
				LoadStatics();
				int basex= x&Map.sectorAnd;
				int basey= y&Map.sectorAnd;

				int relX= x-basex;
				int relY= y-basey;
				return staticSector.GetTileZ(relX, relY);
			}
			#endregion Static stuff

			#region Regions
			public Region GetRegionFor(int x, int y) {
				if (dynamicRegionRects != null) {
					foreach (RegionRectangle dynamicRect in dynamicRegionRects) {
						if (dynamicRect.Contains(x, y)) {
							return dynamicRect.region;
						}
					}
				}
				for (int i=rectangles.Length - 1; i>=0; i--) {
					RegionRectangle rect = rectangles[i];
					if (rect.Contains(x, y)) {
						return rect.region;
					}
				}
				return Region.WorldRegion;
			}

			public Region GetRegionFor(Point2D point) {
				if (dynamicRegionRects != null) {
					foreach (RegionRectangle dynamicRect in dynamicRegionRects) {
						if (dynamicRect.Contains(point)) {
							return dynamicRect.region;
						}
					}
				}
				for (int i=rectangles.Length - 1; i>=0; i--) {
					RegionRectangle rect = rectangles[i];
					if (rect.Contains(point)) {
						return rect.region;
					}
				}
				return Region.WorldRegion;
			}

			internal void SetRegionRectangles(ArrayList list) {
				if (list == null) {
					rectangles = RegionRectangle.emptyArray;
				} else {
					rectangles = (RegionRectangle[]) list.ToArray(typeof(RegionRectangle));
					Rectangle2D sectorRect = new Rectangle2D((ushort) (sx<<Map.sectorFactor), (ushort) (sy<<Map.sectorFactor),
						Map.sectorWidth, Map.sectorWidth);
					SectRectComparer comparer = new SectRectComparer(sectorRect);
					Array.Sort(rectangles, comparer);
				}
			}

			internal MultiItemComponent GetMultiComponent(int x, int y, int z, int staticId) {
				return multiComponents.Find(x, y, z, staticId);
			}

			private class SectRectComparer : IComparer {
				private Rectangle2D sectorRectangle;
				internal SectRectComparer(Rectangle2D sectorRectangle) {
					this.sectorRectangle = sectorRectangle;
				}

				//Less than zero: x is less than y.
				//Greater than zero: x is greater than y.
				public int Compare(object x, object y) {
					RegionRectangle a = (RegionRectangle) x;
					RegionRectangle b = (RegionRectangle) y;
					if (a.region.HierarchyIndex < b.region.HierarchyIndex) {
						return -1;
					} else if (a.region.HierarchyIndex > b.region.HierarchyIndex) {
						return 1;
					} else {
						Rectangle2D intersectA = Rectangle2D.GetIntersection(sectorRectangle, a);
						Rectangle2D intersectB = Rectangle2D.GetIntersection(sectorRectangle, b);
						return intersectA.TilesNumber.CompareTo(intersectB.TilesNumber);
					}
				}
			}

			internal void ClearRegionRectangles() {
				rectangles = RegionRectangle.emptyArray;
				dynamicRegionRects = null;
			}

			//Simple getter
			internal LinkedList<RegionRectangle> RegionRectangles {
				get {
					return dynamicRegionRects;
				}
			}
			#endregion Regions

			#region Dynamic Regions
			internal bool AddDynamicRegionRect(RegionRectangle rect, bool performControls) {
				if (dynamicRegionRects == null) {
					dynamicRegionRects = new LinkedList<RegionRectangle>();
				}
				if(performControls) {
					//check whethre other dynamic region rectangles do not intersect with the currently inserted one
					foreach(RegionRectangle existingRect in dynamicRegionRects) {
						if(existingRect.IntersectsWith(rect)) {
							return false; //problem here, stop trying !
						}
					}
					dynamicRegionRects.AddFirst(rect);
					return true;
				} else {
					dynamicRegionRects.AddFirst(rect);
					return true; //always OK
				}
			}

			internal void RemoveDynamicRegionRect(RegionRectangle rect) {
				if (dynamicRegionRects == null) {
					return;
				}
				dynamicRegionRects.Remove(rect);
				if (dynamicRegionRects.Count == 0) {
					dynamicRegionRects = null;
				}
			}
			#endregion
		}
	}
}