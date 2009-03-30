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
using System.Diagnostics;
using System.Configuration;
using SteamEngine.Common;
using SteamEngine.CompiledScripts;
using SteamEngine.Networking;
using SteamEngine.Communication.TCP;

namespace SteamEngine.Regions {

	public partial class Map {
		public static bool MapTracingOn = TagMath.ParseBoolean(ConfigurationManager.AppSettings["Map Trace Messages"]);

		public const int sectorFactor = 4;
		public const int sectorWidth = 1 << sectorFactor; //16 for sectorfactor 4, 32 for 5, etc. set as const for speed
		public const int sectorAnd = ~(sectorWidth - 1);	//Used to quickly determine whether the sector of a thing has changed
		//Sector size is required to be a power of 2 for speed & efficiency. -SL
		public const int mulSectorAnd = ~7;

		//sectorAnd and mulSectorAnd are also used to calculate relative x/y coordinates - much faster than using shifts.

		private static Map[] maps = new Map[0x100];
		private static ArrayList mapsList = new ArrayList();

		private readonly int numXSectors;
		private readonly int numYSectors;
		private readonly int sizeX;
		private readonly int sizeY;

		private Sector[,] sectors;
		internal StaticRegion[] regions;
		public readonly byte m;
		private SimpleQueue<Region> dynamicRegions = new SimpleQueue<Region>();

		private static GroundTileType t_rock;
		private static GroundTileType t_grass;
		private static GroundTileType t_lava;
		private static GroundTileType t_dirt;

		internal static void UnLoadScripts() {
			t_rock = null;
			t_grass = null;
			t_lava = null;
			t_dirt = null;
		}

		internal static void StartingLoading() {

		}

		//MapTileType
		internal static void LoadingFinished() {
			t_rock = GroundTileType.Get("t_rock");
			t_grass = GroundTileType.Get("t_grass");
			t_lava = GroundTileType.Get("t_lava");
			t_dirt = GroundTileType.Get("t_dirt");
			Sanity.IfTrueThrow(t_rock == null, "Could not find script for t_rock!");
			Sanity.IfTrueThrow(t_grass == null, "Could not find script for t_grass!");
			Sanity.IfTrueThrow(t_lava == null, "Could not find script for t_lava!");
			Sanity.IfTrueThrow(t_dirt == null, "Could not find script for t_dirt!");
		}

		/**
			this returns the sector at given coordinates in the sector matrix (i.e. not real map coordinates!)
			if there was no sector, it initializes it.
		*/
		private Sector GetSector(int sx, int sy) {
			Sector retVal = sectors[sx, sy];
			if (retVal == null) {
				retVal = new Sector((ushort) sx, (ushort) sy, this.m);
				sectors[sx, sy] = retVal;
			}
			return retVal;
		}

		/**
			This determines if the specified x/y coordinates are within the specified mapplane.
			It does not check if the tile is walkable, or anything else.
		*/
		public static bool IsValidPos(int x, int y, int m) {
			return (x >= 0 && y >= 0 && x < GetMapSizeX(m) && y < GetMapSizeY(m));
		}

		/**
			This determines if the specified x/y coordinates are within this mapplane.
			It does not check if the tile is walkable, or anything else.
		*/
		public bool IsValidPos(int x, int y) {
			return (x >= 0 && y >= 0 && x < this.sizeX && y < this.sizeY);
		}

		/**
			This determines if the specified x/y coordinates are within this mapplane.
			It does not check if the tile is walkable, or anything else.
		*/
		public bool IsValidPos(IPoint2D point) {
			int x = point.X;
			int y = point.Y;
			return (x >= 0 && y >= 0 && x < this.sizeX && y < this.sizeY);
		}

		/**
			This determines if the specified x/y coordinates are within the specified mapplane.
			It does not check if the tile is walkable, or anything else (including z).
		*/
		public static bool IsValidPos(Point4D point) {
			return (point.X >= 0 && point.Y >= 0 && point.X < GetMapSizeX(point.M) && point.Y < GetMapSizeY(point.M));
		}

		public static bool IsValidPos(IPoint4D point) {
			return (point.X >= 0 && point.Y >= 0 && point.X < GetMapSizeX(point.M) && point.Y < GetMapSizeY(point.M));
		}

		/**
			Returns the Map for the specified mapplane.
		*/
		public static Map GetMap(int m) {
			if (m >= 0 && m < 0x100) {
				Map map = maps[m];
				if (map == null) {
					map = new Map((byte) m);
					maps[m] = map;
					mapsList.Add(map);
				}
				return map;
			}
			throw new SEException("Mapplanes only have numbers >= 0 && < 0x100");
		}

		public static ArrayList AllMaps {
			get {
				return ArrayList.ReadOnly(mapsList);
			}
		}

		/**
			Clears all the maps of all dynamic objects.
		*/
		public static void ClearAll() {
			foreach (Map map in mapsList) {
				map.ClearThings();
				map.InactivateRegions(true); //true - clear also the dynamic regions
			}
		}

		#region facets

		//private static readonly ushort[] mapSizeX = { 6144 }; //7168x4096 for the new map0.mul
		//private static readonly ushort[] mapSizeY = { 4096 };

		/**
			Returns the number of X tiles in the specified mapplane.
		*/
		public static int GetMapSizeX(int mapplane) {
			//if (mapplane >= 0 && mapplane < mapSizeX.Length) {
			//    return mapSizeX[mapplane];
			//} else {
			//	return mapSizeX[0];
			//}
			return 6144;
		}

		/**
			Returns the number of Y tiles in the specified mapplane.
		*/
		public static int GetMapSizeY(int mapplane) {
			//if (mapplane >= 0 && mapplane < mapSizeY.Length) {
			//    return mapSizeY[mapplane];
			//} else {
			//    return mapSizeY[0];
			//}
			return 4096;
		}

		/**
			This returns the real map number for a particular mapplane. Currently, it's 0 for all mapplanes.
		*/
		public byte Facet {
			get {
				return 0;//this will be variable once (if) we have support for more mapxx.mul
			}
		}

		public static int GetFacetCount() {
			return 1;
		}

		public static int GetFacetPatchesMapCount(int facet) {
			return 0; //we have no support for map patches (yet?) so we ignore them
		}

		public static int GetFacetPatchesStaticsCount(int facet) {
			return 0; //we have no support for map patches (yet?) so we ignore them
		}
		#endregion

		/**
			This returns the number of X sectors in the MUL files. This is based on MUL sectors being 8x8.

				It is important to know the difference between GetMulNum*Sectors and GetMapNum*Sectors:
				You use the *Mul* methods when you need to know the number of X or Y sectors in the MUL files.
				You use the *Map* methods when you need to know the number of X or Y sectors in SE's internal memory.
				By default (at present), sectorFactor is 4, which means that SE internally uses 16x16 sectors, whereas
				the MUL files use 8x8 sectors. So it's very important to use the proper methods for the proper
				things.
		*/
		public static int GetMulNumXSectors(int mapplane) {
			return (GetMapSizeX(mapplane) >> 3);
		}

		/**
			This returns the number of Y sectors in the MUL files. This is based on MUL sectors being 8x8.

				It is important to know the difference between GetMulNum*Sectors and GetMapNum*Sectors:
				You use the *Mul* methods when you need to know the number of X or Y sectors in the MUL files.
				You use the *Map* methods when you need to know the number of X or Y sectors in SE's internal memory.
				By default (at present), sectorFactor is 4, which means that SE internally uses 16x16 sectors, whereas
				the MUL files use 8x8 sectors. So it's very important to use the proper methods for the proper
				things.
		*/
		public static int GetMulNumYSectors(int mapplane) {
			return (GetMapSizeY(mapplane) >> 3);
		}

		/**
			This returns the number of X sectors in SE's internal map. SE sectors' sizes depend on sectorFactor.
			
				It is important to know the difference between GetMulNum*Sectors and GetMapNum*Sectors:
				You use the *Mul* methods when you need to know the number of X or Y sectors in the MUL files.
				You use the *Map* methods when you need to know the number of X or Y sectors in SE's internal memory.
				By default (at present), sectorFactor is 4, which means that SE internally uses 16x16 sectors, whereas
				the MUL files use 8x8 sectors. So it's very important to use the proper methods for the proper
				things.
		*/
		public static int GetMapNumXSectors(int mapplane) {
			return (GetMapSizeX(mapplane) >> sectorFactor);
		}

		/**
			This returns the number of Y sectors in SE's internal map. SE sectors' sizes depend on sectorFactor.
			
				It is important to know the difference between GetMulNum*Sectors and GetMapNum*Sectors:
				You use the *Mul* methods when you need to know the number of X or Y sectors in the MUL files.
				You use the *Map* methods when you need to know the number of X or Y sectors in SE's internal memory.
				By default (at present), sectorFactor is 4, which means that SE internally uses 16x16 sectors, whereas
				the MUL files use 8x8 sectors. So it's very important to use the proper methods for the proper
				things.
		*/
		public static int GetMapNumYSectors(int mapplane) {
			return (GetMapSizeY(mapplane) >> sectorFactor);
		}

		private Map(byte m) {
			this.m = m;
			this.sizeX = GetMapSizeX(m);
			this.sizeY = GetMapSizeY(m);
			Logger.WriteDebug("Initializing map " + m);
			numXSectors = sizeX >> sectorFactor;
			numYSectors = sizeY >> sectorFactor;
			sectors = new Sector[numXSectors, numYSectors];

			//we dont do this, because individual sectors are now initialised on-demand
			//for (ushort sx=0; sx<numXSectors; sx++) {
			//	for (ushort sy=0; sy<numYSectors; sy++) {
			//		sectors[sx,sy] = new Sector(sx, sy);
			//	}
			//}
		}


		public int NumXSectors {
			get { return numXSectors; }
		}

		public int NumYSectors {
			get { return numYSectors; }
		}

		public int SizeX {
			get { return sizeX; }
		}

		public int SizeY {
			get { return sizeY; }
		} 

		/**
			If the specified thing is on the ground, this tells the sector it is in to move
			it to the Disconnected list.
		*/
		internal void Disconnected(Thing t) {
			Sanity.IfTrueThrow(t == null, "You can't tell us a NULL thing has disconnected!");
			Logger.WriteInfo(MapTracingOn, this + ".Disconnected " + t);
			if (t.IsOnGround) {
				int sx = t.X >> sectorFactor;
				int sy = t.Y >> sectorFactor;
				GetSector(sx, sy).Disconnected(t);
				if (t.IsMulti) {
					RemoveMulti((AbstractItem) t);
				}
			}
		}

		/**
			If the specified thing is on the ground, this tells the sector it is in to take
			it out of the Disconnected list and put it back in the appropriate list(s).
		*/
		internal void Reconnected(Thing t) {
			Sanity.IfTrueThrow(t == null, "You can't tell us a NULL thing has reconnected!");
			Logger.WriteInfo(MapTracingOn, this + ".Reconnected " + t);
			if (t.IsOnGround) {
				int sx = t.X >> sectorFactor;
				int sy = t.Y >> sectorFactor;
				GetSector(sx, sy).Reconnected(t);
				if (t.IsMulti) {
					AddMulti((AbstractItem) t);
				}
			}
		}

		/**
			This adds a specified thing to the sector that its coordinates indicate it should be in.
		*/
		internal void Add(Thing t) {
			Sanity.IfTrueThrow(t == null, "You can't tell us to add a NULL thing to the map!");
			Logger.WriteInfo(MapTracingOn, this + ".Add " + t);
			int sx = t.X >> sectorFactor;
			int sy = t.Y >> sectorFactor;
			GetSector(sx, sy).Add(t);
			if (t.IsMulti) {
				AddMulti((AbstractItem) t);
			}
		}

		private void AddMulti(AbstractItem multiItem) {
			MultiItemComponent[] components = multiItem.contentsOrComponents as MultiItemComponent[];
			Sanity.IfTrueThrow(components != null, "MultiItem being added to map when it already has it's components instantiated!");
			MultiData data = multiItem.Def.multiData;
			Sanity.IfTrueThrow(data == null, "MultiItem wihtout MultiData on it's Def?!");
			MutablePoint4D p = multiItem.point4d;
			components = data.Create(p.x, p.y, p.z, p.m);
			multiItem.contentsOrComponents = components;
			foreach (MultiItemComponent mic in components) {
				int sx = mic.X >> sectorFactor;
				int sy = mic.Y >> sectorFactor;
				GetSector(sx, sy).AddMultiComponent(mic);
			}
		}

		/**
			This removes a specified thing from the sector that its coordinates indicate it should be in.
		*/
		internal void Remove(Thing t) {
			int x = t.X;
			int y = t.Y;
			//if (!this.IsValidPos(x, y)) {
			//    return;
			//}
			Sanity.IfTrueThrow(t == null, "You can't tell us to remove a NULL thing from the map!");
			Logger.WriteInfo(MapTracingOn, this + ".Remove " + t);
			int sx = x >> sectorFactor;
			int sy = y >> sectorFactor;
			GetSector(sx, sy).Remove(t);
			if (t.IsMulti) {
				RemoveMulti((AbstractItem) t);
			}
		}

		private void RemoveMulti(AbstractItem multiItem) {
			MultiItemComponent[] components = multiItem.contentsOrComponents as MultiItemComponent[];
			Sanity.IfTrueThrow(components == null, "MultiItem being removed from map when it doesn't have it's components instantiated!");
			MutablePoint4D p = multiItem.point4d;
			foreach (MultiItemComponent mic in components) {
				int sx = mic.X >> sectorFactor;
				int sy = mic.Y >> sectorFactor;
				GetSector(sx, sy).RemoveMultiComponent(mic);
			}
		}

		/**
			This removes a character from their sector's list of players.
		*/
		internal void MadeIntoNonPlayer(AbstractCharacter t) {
			Sanity.IfTrueThrow(t == null, "You can't tell us a NULL character is now a non-player!");
			Logger.WriteInfo(MapTracingOn, this + ".MadeIntoNonPlayer " + t);
			int sx = t.X >> sectorFactor;
			int sy = t.Y >> sectorFactor;
			GetSector(sx, sy).MadeIntoNonPlayer(t);
		}

		/**
			This adds a character to their sector's list of players.
		*/
		internal void MadeIntoPlayer(AbstractCharacter t) {
			Sanity.IfTrueThrow(t == null, "You can't tell us a NULL character is now a player!");
			Logger.WriteInfo(MapTracingOn, this + ".MadeIntoPlayer " + t);
			int sx = t.X >> sectorFactor;
			int sy = t.Y >> sectorFactor;
			GetSector(sx, sy).MadeIntoPlayer(t);
		}

		/**
			This handles giving sectors the appropriate mesages when a thing is moved, and it 
			correctly handles situations where one or both coordinates (old or new) are invalid
			(outside the map), and when coordinates are on different mapplanes.
			
			This expects items moving to or from containers to have BeingDroppedFromContainer
			and/or BeingPutInContainer called as appropriate - That is done by the methods which do the
			dropping or moving (They're in Container, AbstractCharacter, and also Equippable's Trigger_Equip).
			InPackets also calls BeingDroppedFromContainer from HandleDropItem and HandleWearItem, after
			it is confirmed that the character is holding the item, before the item is actually moved.
		*/
		internal static void ChangedP(Thing thing, Point4D oldP) {
			bool oldPValid = Map.IsValidPos(oldP);
			bool newPValid = Map.IsValidPos(thing);
			Map oldM = Map.GetMap(oldP.M);
			Map newM = Map.GetMap(thing.M);
			if (!oldPValid) {
				if (newPValid) {
					newM.Add(thing);
				}
			} else {	//oldPValid
				if (newPValid) {	//both old and new are valid
					if (oldM == newM) {
						newM.ChangedPImpl(thing, oldP);
					} else {			//Thing changed maps.
						oldM.RemoveFromPImpl(thing, oldP);
						newM.Add(thing);
					}
				} else {	//New isn't valid, but old is, so we just remove the thing from the map.
					oldM.RemoveFromPImpl(thing, oldP);
				}
			}
		}


		private void RemoveFromPImpl(Thing thing, Point4D oldP) {
			Logger.WriteInfo(MapTracingOn, this + ".RemoveFromPImpl(" + thing + "," + oldP + ")");
			int oldSx = oldP.X >> sectorFactor;
			int oldSy = oldP.Y >> sectorFactor;
			GetSector(oldSx, oldSy).Remove(thing);
			if (thing.IsMulti) {
				RemoveMulti((AbstractItem) thing);
			}
		}

		/*
		Why the &sectorAnd thing in ChangedPImpl exists:
		We have a precalculated value named 'sectorAnd' which we can & into an x or y coordinate,
		which would remove the in-sector coordinate parts from the coordinate.
		In other words, if I have an old coordinate of 37 and a new one of 39, and sectorFactor is 4, then:
		sectorWidth = 1<<4, which is 16,
		sectorAnd = ~(16-1), which is ~0xf, which is 0xfffffff0.
		37 is 0x25, 39 is 0x27.
		If we do 0x25&0xfffffff0, we get 0x20.
		If we do 0x27&0xfffffff0, we get 0x20.
		So those two x coordinates are in the same x sector.
		
		The whole point of this is that & is faster (0.5 latency, 0.5 throughput, uses ALU)
		than shifting (4 latency, 1 throughput).
		Doing two &s in a row like this would take 1 cycle, doing two >>s would take anywhere from 2 to 8
		cycles - at least 2 because the issue ports can only do a shift once every cycle, but it takes
		4 cycles to complete each shift. If .NET can manage to arrange the operations to cover the latency of
		8 cycles that the CPU can do those while simultaneously doing others too, it would only take 2 cycles.
		
		Anyways, using 1 cycle instead of 2-8 is good, but we will use 3-9 instead when we DO change sectors.
		But this is a good tradeoff because the majority of the time, we will be moving inside a sector
		instead of between sectors. The larger the sectorFactor, the more rarely we move between sectors.
		If we consider only normal movement, going in a straight direction towards a particular target, then
		with a sectorFactor of 4 (sectors are 16x16), then 15 out of every 16 moves will be within a sector,
		and one to a new sector, and if we just look at the costs of our &s and >>s, if we go with the best-case
		for our >>s and assume they take 2 cycles total, then we use 1*15 cycles for normal moves and 3 for
		the extra, for 18 cycles. If we assumed our >>s to be worst-case, then 15+1+8=24.
		Now, if we were doing shifts every time instead of &s, and assuming the best-case situation so our
		shifts took only two cycles, then we would be taking 2*16 cycles, or 32 cycles.
		
		As you can see, this way is better. The only extra problem would be the cost of storing to our
		temporary oldXPre and newXPre variables and doing the compare, which would be 1 cycle more
		than what you'd have without our &s (the comparison would exist still, but would check different vars,
		so only the stores count, and they also are 0.5/0.5/ALU (so are compares for that matter)).
		
		Redoing our calculations, worst-case with our & algorithm, we'd do 2*16+8=40 cycles,
		best-case 2*17=34 cycles.
		Worst case for >>s is 8*16=128, so while our best-case for the shifts is slightly faster than our
		best-case for the & algorithm (32 vs 34), it should be pretty clear that the & algorithm is
		still better in the end.
		
		And now, if you've read all that, you've learned a little about optimization! But not much. And
		I didn't even explain when latency or throughput are important either - plus this probably
		isn't the same for AMD chips, and Pentium M chips have a latency of 1 for shifts, but do things
		differently anyways.
		
		One more bit of information: The CPU may decide to calculate one or more of the shifts
		at the same time as it is calculating the &s and doing compares, if it has spare issue ports etc, and
		if it turns out that the result of the shifts aren't needed, it discards the result without having
		used any extra cycles for it. If it is needed, well, then it just managed to get the result without
		sitting for 8 cycles, because it did it early before anything needed the result. In practice,
		the code before the >>s will only take around 2.5 cycles, except for ChangedXY, which would need
		more because it does twice as many &s, but also does twice as many >>s. It would still save some
		CPU time, though. Bear in mind that there's no way that you could force the CPU to do this, and it's
		not something that's represented even in assembly - Besides, the CPU does it all itself. If it supports
		that, that is. Pentium 4's and Pentium M's do, I don't know if others do, but considering how
		AMD's chips are faster than Intel's on the same clock speed, I'd bet they do (among other things).
		
		-SL
		
		Great. I don't care :P
		-tar
		
		*/
		private void ChangedPImpl(Thing thing, Point2D oldP) {
			MutablePoint4D newP = thing.point4d;
			Logger.WriteInfo(MapTracingOn, this + ".ChangedPImpl(" + newP + "," + oldP + ")");
			int oldXPre = oldP.X & sectorAnd;
			int newXPre = newP.x & sectorAnd;
			int oldYPre = oldP.Y & sectorAnd;
			int newYPre = newP.y & sectorAnd;
			if (oldXPre != newXPre || oldYPre != newYPre) {
				int oldSx = oldP.X >> sectorFactor;
				int newSx = newP.x >> sectorFactor;
				int oldSy = oldP.Y >> sectorFactor;
				int newSy = newP.y >> sectorFactor;
				Sector oldSector = GetSector(oldSx, oldSy);
				Sector newSector = GetSector(newSx, newSy);
				Sanity.IfTrueThrow(oldSector == newSector, "oldSector==newSector! Apparently our &sectorAnd algorithm doesn't work. :(");	//P.S. This doesn't ever happen currently, but it's here in case someone breaks it. -SL
				Logger.WriteInfo(MapTracingOn, "Remove from sector " + oldSx + "," + oldSy + " and add to sector " + newSx + "," + newSy);
				oldSector.Remove(thing);
				newSector.Add(thing);
			}
			if (thing.IsMulti) {
				AbstractItem multiItem = (AbstractItem) thing;
				MultiItemComponent[] components = multiItem.contentsOrComponents as MultiItemComponent[];
				if (components != null) {
					foreach (MultiItemComponent mic in components) {
						int oldX = mic.X, oldY = mic.Y;
						mic.SetRelativePos(newP.x, newP.y, newP.z);
						ChangedMultiCOmponentPImpl(mic, oldX, oldY);
					}
				}
			}
		}

		private void ChangedMultiCOmponentPImpl(MultiItemComponent mic, int oldX, int oldY) {
			Logger.WriteInfo(MapTracingOn, this + ".ChangedMultiCOmponentPImpl(" + mic + "," + oldX + "," + oldY + ")");
			int oldXPre = oldX & sectorAnd;
			int newXPre = mic.X & sectorAnd;
			int oldYPre = oldY & sectorAnd;
			int newYPre = mic.Y & sectorAnd;
			if (oldXPre != newXPre || oldYPre != newYPre) {
				int oldSx = oldX >> sectorFactor;
				int newSx = mic.X >> sectorFactor;
				int oldSy = oldY >> sectorFactor;
				int newSy = mic.Y >> sectorFactor;
				Sector oldSector = GetSector(oldSx, oldSy);
				Sector newSector = GetSector(newSx, newSy);
				Sanity.IfTrueThrow(oldSector == newSector, "oldSector==newSector! Apparently our &sectorAnd algorithm doesn't work. :(");	//P.S. This doesn't ever happen currently, but it's here in case someone breaks it. -SL
				Logger.WriteInfo(MapTracingOn, "Remove from sector " + oldSx + "," + oldSy + " and add to sector " + newSx + "," + newSy);
				oldSector.RemoveMultiComponent(mic);
				newSector.AddMultiComponent(mic);
			}
		}

		internal void ClearThings() {
			foreach (Sector sector in sectors) {
				if (sector != null) {
					sector.ClearThings();
				}
			}
		}

		internal static void Init() {
			Logger.WriteDebug("Categorizing sector contents.");

			foreach (Thing t in Thing.AllThings) {
				t.FixWeight();
				if (t.IsOnGround) {
					if (Map.IsValidPos(t)) {
						t.GetMap().Add(t);
						continue;
					}
					t.CheckAfterLoad();
				}
			}
			Logger.WriteDebug("Done categorizing sector contents.");
		}

		/**
			Returns true if there are any characters within 32 tiles of the specified coordinates, false
			otherwise.
		*/
		//public bool PlayersInNearbySectors(ushort x, ushort y) {
		//    EnumeratorOfPlayers enumer = GetPlayersInRange(x,y,32);
		//    return (enumer.MoveNext());	//return true if there is a player somewhere, false if not.

		//}
		
		public IEnumerable<TCPConnection<GameState>> GetConnectionsInRange(int x, int y, int range) {
			return GetConnectionsInRectangle(new ImmutableRectangle(x, y, range));
		}

		//public IEnumerable<GameConn> GetGameConnsInRange(ushort x, ushort y, int range) {
		//    return GetGameConnsInRectangle(new ImmutableRectangle(x, y, range));
		//}
		public IEnumerable<AbstractCharacter> GetPlayersInRange(int x, int y, int range) {
			return GetPlayersInRectangle(new ImmutableRectangle(x, y, range));
		}

		public IEnumerable<Thing> GetThingsInRange(int x, int y, int range) {
			return GetThingsInRectangle(new ImmutableRectangle(x, y, range));
		}

		public IEnumerable<AbstractItem> GetItemsInRange(int x, int y, int range) {
			return GetItemsInRectangle(new ImmutableRectangle(x, y, range));
		}

		public IEnumerable<AbstractCharacter> GetCharsInRange(int x, int y, int range) {
			return GetCharsInRectangle(new ImmutableRectangle(x, y, range));
		}

		public IEnumerable<Static> GetStaticsInRange(int x, int y, int range) {
			return GetStaticsInRectangle(new ImmutableRectangle(x, y, range));
		}

		public IEnumerable<Thing> GetDisconnectsInRange(int x, int y, int range) {
			return GetDisconnectsInRectangle(new ImmutableRectangle(x, y, range));
		}

		public IEnumerable<MultiItemComponent> GetMultiComponentsInRange(int x, int y, int range) {
			return GetMultiComponentsInRectangle(new ImmutableRectangle(x, y, range));
		}

		public IEnumerable<TCPConnection<GameState>> GetConnectionsInRange(int x, int y) {
			return GetConnectionsInRectangle(new ImmutableRectangle(x, y, Globals.MaxUpdateRange));
		}

		//public IEnumerable<GameConn> GetGameConnsInRange(ushort x, ushort y) {
		//    return GetGameConnsInRectangle(new ImmutableRectangle(x, y, Globals.MaxUpdateRange));
		//}

		public IEnumerable<AbstractCharacter> GetPlayersInRange(int x, int y) {
			return GetPlayersInRectangle(new ImmutableRectangle(x, y, Globals.MaxUpdateRange));
		}

		public IEnumerable<Thing> GetThingsInRange(int x, int y) {
			return GetThingsInRectangle(new ImmutableRectangle(x, y, Globals.MaxUpdateRange));
		}

		public IEnumerable<AbstractItem> GetItemsInRange(int x, int y) {
			return GetItemsInRectangle(new ImmutableRectangle(x, y, Globals.MaxUpdateRange));
		}

		public IEnumerable<AbstractCharacter> GetCharsInRange(int x, int y) {
			return GetCharsInRectangle(new ImmutableRectangle(x, y, Globals.MaxUpdateRange));
		}

		public IEnumerable<Static> GetStaticsInRange(int x, int y) {
			return GetStaticsInRectangle(new ImmutableRectangle(x, y, Globals.MaxUpdateRange));
		}

		public IEnumerable<Thing> GetDisconnectsInRange(int x, int y) {
			return GetDisconnectsInRectangle(new ImmutableRectangle(x, y, Globals.MaxUpdateRange));
		}

		public IEnumerable<MultiItemComponent> GetMultiComponentsInRange(int x, int y) {
			return GetMultiComponentsInRectangle(new ImmutableRectangle(x, y, Globals.MaxUpdateRange));
		}

		//public IEnumerable<GameConn> GetGameConnsInRectangle(ImmutableRectangle rectangle) {
		//    foreach (Sector sector in this.GetSectorsInRectangle(rectangle)) {
		//        foreach (AbstractCharacter player in sector.players) {
		//            GameConn conn = player.Conn;
		//            if ((conn != null) && (rectangle.Contains(player))) {
		//                yield return conn;
		//            }
		//        }
		//    }
		//}

		public IEnumerable<TCPConnection<GameState>> GetConnectionsInRectangle(ImmutableRectangle rectangle) {
			foreach (Sector sector in this.GetSectorsInRectangle(rectangle)) {
				foreach (AbstractCharacter player in sector.players) {
					GameState state = player.GameState;
					if ((state != null) && (rectangle.Contains(player))) {
						yield return state.Conn;
					}
				}
			}
		}

		public IEnumerable<AbstractCharacter> GetPlayersInRectangle(ImmutableRectangle rectangle) {
			foreach (Sector sector in this.GetSectorsInRectangle(rectangle)) {
				foreach (AbstractCharacter player in sector.players) {
					if (rectangle.Contains(player)) {
						yield return player;
					}
				}
			}
		}
		/**
			This is used by Thing's similarly named methods, but this version allows
			you to explicitly specify a rectangle to look in.
		*/
		public IEnumerable<Thing> GetThingsInRectangle(ImmutableRectangle rectangle) {
			foreach (Sector sector in this.GetSectorsInRectangle(rectangle)) {
				foreach (Thing thing in sector.things) {
					if (rectangle.Contains(thing)) {
						yield return thing;
					}
				}
			}
		}
		/**
			This is used by Thing's similarly named methods, but this version allows
			you to explicitly specify a rectangle to look in.
		*/
		public IEnumerable<AbstractItem> GetItemsInRectangle(ImmutableRectangle rectangle) {
			foreach (Sector sector in this.GetSectorsInRectangle(rectangle)) {
				foreach (Thing thing in sector.things) {
					AbstractItem i = thing as AbstractItem;
					if ((i != null) && (rectangle.Contains(i))) {
						yield return i;
					}
				}
			}
		}
		/**
			This is used by Thing's similarly named methods, but this version allows
			you to explicitly specify a rectangle to look in.
		*/
		public IEnumerable<AbstractCharacter> GetCharsInRectangle(ImmutableRectangle rectangle) {
			foreach (Sector sector in this.GetSectorsInRectangle(rectangle)) {
				foreach (Thing thing in sector.things) {
					AbstractCharacter ch = thing as AbstractCharacter;
					if ((ch != null) && (rectangle.Contains(ch))) {
						yield return ch;
					}
				}
			}
		}
		/**
			This is used by Thing's similarly named methods, but this version allows
			you to explicitly specify a rectangle to look in.
		*/
		public IEnumerable<AbstractCharacter> GetNPCsInRectangle(ImmutableRectangle rectangle) {
			foreach (Sector sector in this.GetSectorsInRectangle(rectangle)) {
				foreach (Thing thing in sector.things) {
					AbstractCharacter ch = thing as AbstractCharacter;
					//Account == null is from "IsPlayer" property (but the property also Logs the query - who knows why...?)
					if ((ch != null) && (ch.Account == null) && (rectangle.Contains(ch))) {
						yield return ch;
					}
				}
			}
		}
		/**
			This is used by Thing's similarly named methods, but this version allows
			you to explicitly specify a rectangle to look in.
		*/
		public IEnumerable<Static> GetStaticsInRectangle(ImmutableRectangle rectangle) {
			foreach (Sector sector in this.GetSectorsInRectangle(rectangle)) {
				foreach (Static s in sector.Statics) {
					if (rectangle.Contains(s)) {
						yield return s;
					}
				}
				if (sector.multiComponents != null) {
					foreach (Static s in sector.multiComponents) {
						if (rectangle.Contains(s)) {
							yield return s;
						}
					}
				}
			}
		}

		public IEnumerable<Static> GetStaticsOnCoords(int x, int y) {
			int sx = x >> sectorFactor;
			int sy = y >> sectorFactor;
			Sector sector = GetSector(sx, sy);

			foreach (Static s in sector.Statics) {
				if ((s.X == x) && (s.Y == y)) {
					yield return s;
				}
			}
			if (sector.multiComponents != null) {
				foreach (Static s in sector.multiComponents) {
					if ((s.X == x) && (s.Y == y)) {
						yield return s;
					}
				}
			}
		}
		/**
			This is used by Thing's similarly named methods, but this version allows
			you to explicitly specify a rectangle to look in.
		*/
		public IEnumerable<Thing> GetDisconnectsInRectangle(ImmutableRectangle rectangle) {
			foreach (Sector sector in this.GetSectorsInRectangle(rectangle)) {
				foreach (Thing thing in sector.disconnects) {
					if (rectangle.Contains(thing)) {
						yield return thing;
					}
				}
			}
		}

		/**
			This is used by Thing's similarly named methods, but this version allows
			you to explicitly specify a rectangle to look in.
		*/
		public IEnumerable<MultiItemComponent> GetMultiComponentsInRectangle(ImmutableRectangle rectangle) {
			foreach (Sector sector in this.GetSectorsInRectangle(rectangle)) {
				foreach (MultiItemComponent mic in sector.multiComponents) {
					if (rectangle.Contains(mic)) {
						yield return mic;
					}
				}
			}
		}

		///**
		//    This is used by Thing's similarly named method, but this version allows
		//    you to explicitly specify an IPoint6D to find clients who can see :).

		//    This checks LOS and the actual UpdateRange of each client, etc. It calls
		//    each client character's CanSee. If the point is in a container, it
		//    checks that too and whether the client has it open.
		//    (This is all done by CanSee, mind you)
		//*/
		//public IEnumerable<GameConn> GetGameConnsWhoCanSee(Thing thing) {
		//    Thing t = thing.TopObj();
		//    ImmutableRectangle rectangle = new ImmutableRectangle(t.X, t.Y, Globals.MaxUpdateRange);
		//    foreach (Sector sector in this.GetSectorsInRectangle(rectangle)) {
		//        foreach (AbstractCharacter player in sector.players) {
		//            GameConn conn = player.Conn;
		//            if ((conn != null) && (rectangle.Contains(player)) && (player.CanSeeForUpdate(thing))) {
		//                yield return conn;
		//            }
		//        }
		//    }
		//}

		public IEnumerable<TCPConnection<GameState>> GetConnectionsWhoCanSee(Thing thing) {
			Thing top = thing.TopObj();
			ImmutableRectangle rectangle = new ImmutableRectangle(top.X, top.Y, Globals.MaxUpdateRange);
			foreach (Sector sector in this.GetSectorsInRectangle(rectangle)) {
				foreach (AbstractCharacter player in sector.players) {
					GameState state = player.GameState;
					if ((state != null) && (rectangle.Contains(player)) && (player.CanSeeForUpdate(thing))) {
						yield return state.Conn;
					}
				}
			}
		}

		private IEnumerable<Sector> GetSectorsInRectangle(ImmutableRectangle rectangle) {
			//rectangle.Crop(0, 0, (ushort) (map.sizeX - 1), (ushort) (map.sizeY - 1));
			int startX = rectangle.MinX;
			int startY = rectangle.MinY;
			int endX = rectangle.MaxX;
			int endY = rectangle.MaxY;

			int xSectorStart, ySectorStart, xSectorEnd, ySectorEnd;
			this.GetSectorXY(Math.Min(startX, endX), Math.Min(startY, endY), out xSectorStart, out ySectorStart);
			this.GetSectorXY(Math.Max(startX, endX), Math.Max(startY, endY), out xSectorEnd, out ySectorEnd);

			for (int sx = xSectorStart; sx <= xSectorEnd; sx++) {
				for (int sy = ySectorStart; sy <= ySectorEnd; sy++) {
					yield return GetSector(sx, sy);
				}
			}
		}

		/**
			Returns the MapTileType for the map tile at the specified coordinates.
			MapTileType is an enum of {Water, Dirt, Lava, Rock, Grass, Other}.
			
			Use GetMapTileType when you would have to call more than one of the IsMapTile* methods.
		*/
		public MapTileType GetMapTileType(int x, int y) {
			int id = GetTileId(x, y);
			return GetMapTileType(id);
		}

		public MapTileType GetMapTileType(int id) {
			MapTileType type = MapTileType.Other;
			if ((TileData.landFlags[id] & TileFlag.Wet) == TileFlag.Wet) {
				type = MapTileType.Water;
			} else if (t_dirt.IsTypeOfMapTile(id)) {
				type = MapTileType.Dirt;
			} else if (t_lava.IsTypeOfMapTile(id)) {
				type = MapTileType.Lava;
			} else if (t_rock.IsTypeOfMapTile(id)) {
				type = MapTileType.Rock;
			} else if (t_grass.IsTypeOfMapTile(id)) {
				type = MapTileType.Grass;
			}
			return type;
		}
		/**
			Returns true if the map tile at the specified specified coordinates is water.
			Use GetMapTileType when you would have to call more than one of the IsMapTile* methods.
		*/
		public bool IsMapTileWater(int x, int y) {
			int id = GetTileId(x, y);
			return (TileData.landFlags[id] & TileFlag.Wet) == TileFlag.Wet;
		}
		/**
			Returns true if the map tile at the specified specified coordinates is dirt.
			Use GetMapTileType when you would have to call more than one of the IsMapTile* methods.
		*/
		public bool IsMapTileDirt(int x, int y) {
			int id = GetTileId(x, y);
			return (t_dirt.IsTypeOfMapTile(id));
		}
		/**
			Returns true if the map tile at the specified specified coordinates is lava.
			Use GetMapTileType when you would have to call more than one of the IsMapTile* methods.
		*/
		public bool IsMapTileLava(int x, int y) {
			int id = GetTileId(x, y);
			return (t_lava.IsTypeOfMapTile(id));
		}
		/**
			Returns true if the map tile at the specified specified coordinates is rock.
			Use GetMapTileType when you would have to call more than one of the IsMapTile* methods.
		*/
		public bool IsMapTileRock(int x, int y) {
			int id = GetTileId(x, y);
			return (t_rock.IsTypeOfMapTile(id));
		}
		/**
			Returns true if the map tile at the specified specified coordinates is grass.
			Use GetMapTileType when you would have to call more than one of the IsMapTile* methods.
		*/
		public bool IsMapTileGrass(int x, int y) {
			int id = GetTileId(x, y);
			return (t_grass.IsTypeOfMapTile(id));
		}

		/**
			Returns the TileID for the map tile at the specified coordinates.
		*/
		public int GetTileId(int x, int y) {
			if (Map.IsValidPos(x, y, m)) {
				int sx = x >> sectorFactor;
				int sy = y >> sectorFactor;
				return this.GetSector(sx, sy).GetTileId(x, y);
			}
			throw new SEException("Invalid x/y position " + x + "," + y + " on mapplane " + m + ".");
		}

		/**
			Returns the z level of the map tile at the specified coordinates.
		*/
		public int GetTileZ(int x, int y) {
			if (Map.IsValidPos(x, y, m)) {
				int sx = x >> sectorFactor;
				int sy = y >> sectorFactor;
				return this.GetSector(sx, sy).GetTileZ(x, y);
			}
			throw new SEException("Invalid x/y position " + x + "," + y + " on mapplane " + m + ".");
		}

		public void GetTile(int x, int y, out int z, out int id) {
			if (Map.IsValidPos(x, y, m)) {
				int sx = x >> sectorFactor;
				int sy = y >> sectorFactor;
				this.GetSector(sx, sy).GetTile(x, y, out z, out id);
			} else {
				throw new SEException("Invalid x/y position " + x + "," + y + " on mapplane " + m + ".");
			}
		}

		public Static GetStatic(int x, int y, int z, int staticId) {
			if (Map.IsValidPos(x, y, m)) {
				int sx = x >> sectorFactor;
				int sy = y >> sectorFactor;
				return GetSector(sx, sy).GetStatic(x, y, z, staticId);
			} else {
				Logger.WriteInfo(MapTracingOn, "GetStatic(" + x + "," + y + "): Invalid pos.");
				return null;
			}
		}

		public MultiItemComponent GetMultiComponent(int x, int y, int z, int staticId) {
			if (Map.IsValidPos(x, y, m)) {
				int sx = x >> sectorFactor;
				int sy = y >> sectorFactor;
				return GetSector(sx, sy).GetMultiComponent(x, y, z, staticId);
			} else {
				Logger.WriteInfo(MapTracingOn, "GetMultiComponent(" + x + "," + y + "): Invalid pos.");
				return null;
			}
		}

		/**
			Returns true if there is a static whose dispid matches 'staticId' at the specified coordinates.
		*/
		public bool HasStaticId(int staticId, int x, int y) {
			if (Map.IsValidPos(x, y, m)) {
				int sx = x >> sectorFactor;
				int sy = y >> sectorFactor;
				return GetSector(sx, sy).HasStaticId(x, y, staticId);
			} else {
				Logger.WriteInfo(MapTracingOn, "HasStaticId(" + x + "," + y + "): Invalid pos.");
				return false;
			}
		}

		#region Regions
		internal Region GetRegionFor(MutablePoint4D point) {
			return GetSector(point.x >> sectorFactor, point.y >> sectorFactor).GetRegionFor(point.x, point.y);
		}

		public Region GetRegionFor(Point2D point) {
			return GetSector(point.X >> sectorFactor, point.Y >> sectorFactor).GetRegionFor(point);
		}

		public Region GetRegionFor(int x, int y) {
			return GetSector(x >> sectorFactor, y >> sectorFactor).GetRegionFor(x, y);
		}

		internal void ActivateRegions(List<StaticRegion> list) {
			//we dont add the rectangles directly to sectors, we first create a "matrix" of arraylists which are then "Staticed" to arrays and assigned to sectors
			regions = list.ToArray();

			List<RegionRectangle>[,] matrix = new List<RegionRectangle>[numXSectors, numYSectors];
			foreach (StaticRegion region in regions) {
				foreach (RegionRectangle rect in region.Rectangles) {
					int minXs = rect.MinX >> sectorFactor;
					int maxXs = rect.MaxX >> sectorFactor;
					maxXs = (int) Math.Min(maxXs, numXSectors - 1);
					int minYs = rect.MinY >> sectorFactor;
					int maxYs = rect.MaxY >> sectorFactor;
					maxYs = (int) Math.Min(maxYs, numYSectors - 1);
					for (int sx = minXs, topx = maxXs + 1; sx < topx; sx++) {
						for (int sy = minYs, topy = maxYs + 1; sy < topy; sy++) {
							List<RegionRectangle> al = matrix[sx, sy];
							if (al == null) {
								al = new List<RegionRectangle>();
								matrix[sx, sy] = al;
							}
							al.Add(rect);
						}
					}
				}
			}
			for (int sx = 0; sx < numXSectors; sx++) {
				for (int sy = 0; sy < numYSectors; sy++) {
					List<RegionRectangle> thislist = matrix[sx, sy];
					if (thislist != null) {
						GetSector(sx, sy).SetRegionRectangles(thislist);
					}
				}
			}
			//ArrayList[,] matrix = new ArrayList[numXSectors, numYSectors];
			//foreach(StaticRegion region in regions) {
			//    foreach (RegionRectangle rect in region.Rectangles) {
			//        int minXs = rect.MinX >> sectorFactor;
			//        int maxXs = rect.MaxX >> sectorFactor;
			//        maxXs = (int) Math.Min(maxXs, numXSectors - 1);
			//        int minYs = rect.MinY >> sectorFactor;
			//        int maxYs = rect.MaxY >> sectorFactor;
			//        maxYs = (int) Math.Min(maxYs, numYSectors - 1);
			//        for (int sx = minXs, topx = maxXs+1; sx<topx; sx++) {
			//            for (int sy = minYs, topy = maxYs+1; sy<topy; sy++) {
			//                ArrayList al = matrix[sx, sy];
			//                if (al == null) {
			//                    al = new ArrayList();
			//                    matrix[sx, sy] = al;
			//                }
			//                al.Add(rect);
			//            }
			//        }
			//    }
			//}
			//for (int sx = 0; sx<numXSectors; sx++) {
			//    for (int sy = 0; sy<numYSectors; sy++) {
			//        ArrayList thislist = matrix[sx, sy];
			//        if (thislist != null) {
			//            GetSector(sx, sy).SetRegionRectangles(thislist);
			//        }
			//    }
			//}
		}

		[Summary("Inactivate regions - unload their rectangles, boolean parameter allows us to omit dynamic regions...")]
		internal void InactivateRegions(bool dynamicsToo) {
			for (int sx = 0; sx < numXSectors; sx++) {
				for (int sy = 0; sy < numYSectors; sy++) {
					Sector se = sectors[sx, sy];
					if (se != null) {
						se.ClearRegionRectangles(dynamicsToo);
					}
				}
			}
		}
		#endregion Regions

		internal void GetSectorXY(int x, int y, out int sx, out int sy) {
			if (x < 0) {
				x = 0;
			} else if (x >= sizeX) {
				x = (ushort) (sizeX - 1);
			}
			sx = x >> sectorFactor;
			if (y < 0) {
				y = 0;
			} else if (y >= sizeY) {
				y = (ushort) (sizeY - 1);
			}
			sy = y >> sectorFactor;

			//Logger.WriteDebug("x/y=("+x+","+y+") sx/sy=("+sx+","+sy+") and numsect=("+GetMapNumXSectors(0)+","+GetMapNumYSectors(0)+")");
		}

		public bool AddDynamicRegion(DynamicRegion region, bool performControls) {
			if (performControls) {
				bool addingOK = true;
				foreach (RegionRectangle rect in region.Rectangles) {
					foreach (Sector sector in GetSectorsInRectangle(rect)) {
						addingOK = sector.AddDynamicRegionRect(rect, performControls);
						if (!addingOK) { //there was an error during inserting 
							RemoveDynamicRegion(region); //immediatelly remove - removes all so far inserted rects...
							return false;//stop trying immediatelly
						}
					}
				}
			} else { //no controls
				foreach (RegionRectangle rect in region.Rectangles) {
					foreach (Sector sector in GetSectorsInRectangle(rect)) {
						sector.AddDynamicRegionRect(rect, false);
					}
				}
			}
			return true; //OK
		}

		public void RemoveDynamicRegion(DynamicRegion region) {
			foreach (RegionRectangle rect in region.Rectangles) {
				foreach (Sector sector in GetSectorsInRectangle(rect)) {
					sector.RemoveDynamicRegionRect(rect);
				}
			}
		}
	}
}
