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
using System.Diagnostics.CodeAnalysis;
using System.IO;
using SteamEngine.Common;
using SteamEngine.UoData;

namespace SteamEngine.Regions {

	internal class StaticSector {
		[SuppressMessage("Microsoft.Performance", "CA1814:PreferJaggedArraysOverMultidimensional", MessageId = "Member")]
		private ushort[,] tileMatrix;
		[SuppressMessage("Microsoft.Performance", "CA1814:PreferJaggedArraysOverMultidimensional", MessageId = "Member")]
		private sbyte[,] zMatrix;
		internal StaticItem[] statics;

		private static List<StaticSector[,]> staticMaps = new List<StaticSector[,]>(1);
		//with only one map (map0.mul), this has only 1 entry

		//Map.sectorWidth>>3 is the number of MUL sectors per SE sector. (Map.sectorFactor-3 would NOT be.)
		private static StaticSector emptySector = new StaticSector(new ushort[0, 0], new sbyte[0, 0], new StaticItem[0]);

		private StaticSector(ushort[,] tile, sbyte[,] z, StaticItem[] statics) {
			this.tileMatrix = tile;
			this.zMatrix = z;
			this.statics = statics;
		}

		//internal static StaticSector GetStaticSectorFromRealCoords(int x, int y, int m) {
		//    return GetStaticSector((x >> Map.sectorFactor), (y >> Map.sectorFactor), m);
		//}

		[SuppressMessage("Microsoft.Performance", "CA1814:PreferJaggedArraysOverMultidimensional", MessageId = "Body"), SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		internal static StaticSector GetStaticSector(int sx, int sy, int m) {
			var facet = Map.GetMap(m).Facet;

			var exists = (staticMaps.Count > facet);
			StaticSector[,] staticMap = null;
			if (exists) {
				staticMap = staticMaps[facet];
				exists = (staticMap != null);
			}

			if (!exists) {
				while (staticMaps.Count <= facet) {
					staticMaps.Add(null);
				}
				staticMap = new StaticSector[Map.GetMapNumXSectors(facet), Map.GetMapNumYSectors(facet)];
				staticMaps[facet] = staticMap;
			}

			//Console.WriteLine("Getting StaticSector "+sx+","+sy);

			var sector = staticMap[sx, sy];
			if (sector == null) {
				if (Globals.useMap) {
					try {
						Logger.WriteDebug("Loading map sector " + sx + "," + sy);

						var numMulSectors = Map.sectorWidth >> 3;

						//Console.WriteLine("Map.sectorWidth="+Map.sectorWidth+" and numMulSectors="+numMulSectors);
						var mulsx = (ushort) ((sx << Map.sectorFactor) >> 3);
						var mulsy = (ushort) ((sy << Map.sectorFactor) >> 3);
						Logger.WriteDebug("Load sector sx,sy=" + sx + "," + sy + " mulsx,mulsy=" + mulsx + "," + mulsy);
						uint[,] versions;
						var statics = LoadStaticsSector(mulsx, mulsy, (ushort) (sx << Map.sectorFactor),
							(ushort) (sy << Map.sectorFactor), numMulSectors, facet, out versions);

						ushort[,] tile; 
						sbyte[,] z;
						LoadMapSector(mulsx, mulsy, numMulSectors, facet, out tile, out z);
						sector = new StaticSector(tile, z, statics);
					} catch (Exception e) {
						Logger.WriteCritical("Exception while loading map sector " + sx + "," + sy, e + ". Disabling map use.");
						Globals.useMap = false;
					}
					staticMap[sx, sy] = sector;
				}
			}
			if (sector == null) {
				sector = emptySector;
			}
			return sector;
		}

		//(TODO): Loading other map MULs.
		[SuppressMessage("Microsoft.Performance", "CA1814:PreferJaggedArraysOverMultidimensional", MessageId = "Body")]
		private static void LoadMapSector(int sx, int sy, int numMulSectors, int facet, out ushort[,] tile, out sbyte[,] z) {
			var mulFileP = Path.Combine(Globals.MulPath, "map0.mul");

			if (File.Exists(mulFileP)) {
				using (var mulbr = new BinaryReader(new FileStream(mulFileP, FileMode.Open, FileAccess.Read))) {
					var numTiles = numMulSectors << 3;
					tile = new ushort[numTiles, numTiles];
					z = new sbyte[numTiles, numTiles];
					//a single 8x8 block is 196 bytes.

					for (var xblock = 0; xblock < numMulSectors; xblock++) {
						long partialFilePos = ((xblock + sx) * Map.GetMulNumYSectors(facet));
						long xblockamt = xblock << 3;
						for (var yblock = 0; yblock < numMulSectors; yblock++) {
							long yblockamt = yblock << 3;
							var filePos = partialFilePos + yblock + sy;
							filePos = 4 + (filePos << 2) + (filePos << 6) + (filePos << 7);	//That's filePos*196. The 4 is to skip the header.
							mulbr.BaseStream.Seek(filePos, SeekOrigin.Begin);
							for (var y = 0; y < 8; y++) {
								for (var x = 0; x < 8; x++) {
									//Console.WriteLine("{0},{1} / {2}",xblockamt+x,yblockamt+y,numTiles);
									tile[xblockamt + x, yblockamt + y] = mulbr.ReadUInt16();
									z[xblockamt + x, yblockamt + y] = mulbr.ReadSByte();
								}
							}
						}
					}
				}
			} else {
				throw new SEException("Unable to locate map file.");
			}
		}

		//(TODO): Loading other static MULs.
		[SuppressMessage("Microsoft.Performance", "CA1814:PreferJaggedArraysOverMultidimensional", MessageId = "Body")]
		private static StaticItem[] LoadStaticsSector(int mulsX, int mulsY, int sx, int sy, int numMulSectors, int facet, out uint[,] versions) {
			versions = new uint[numMulSectors, numMulSectors];
			var mulFilePI = Path.Combine(Globals.MulPath, "staidx0.mul");
			var mulFileP = Path.Combine(Globals.MulPath, "statics0.mul");

			if (File.Exists(mulFilePI) && File.Exists(mulFileP)) {
				var statics = new List<StaticItem>();

				using (var idxfs = new FileStream(mulFilePI, FileMode.Open, FileAccess.Read)) {
					using (var mulfs = new FileStream(mulFileP, FileMode.Open, FileAccess.Read)) {
						var idxbr = new BinaryReader(idxfs);
						var mulbr = new BinaryReader(mulfs);
						//int numTiles=numMulSectors<<3;
						
						for (var xblock = 0; xblock < numMulSectors; xblock++) {
							long partialFilePos = ((xblock + mulsX) * Map.GetMulNumYSectors(facet));
							long xblockamt = xblock << 3;
							for (var yblock = 0; yblock < numMulSectors; yblock++) {
								long yblockamt = yblock << 3;
								var filePos = partialFilePos + yblock + mulsY;
								filePos = (filePos << 3) + (filePos << 2);
								idxbr.BaseStream.Seek(filePos, SeekOrigin.Begin);
								var blockStart = idxbr.ReadUInt32();
								var blockLen = idxbr.ReadUInt32();
								versions[xblock, yblock] = idxbr.ReadUInt32();
								//Console.WriteLine("blockStart=[0x"+blockStart.ToString("x")+"] blockLen=[0x"+blockLen.ToString("x")+"] unk=[0x"+unk.ToString("x")+"]");
								if (blockStart != 0xffffffff) {
									mulbr.BaseStream.Seek(blockStart, SeekOrigin.Begin);
									var blockNumStatics = blockLen / 7;
									for (var a = 0; a < blockNumStatics; a++) {
										var tileID = mulbr.ReadUInt16();
										var relX = mulbr.ReadByte();
										var relY = mulbr.ReadByte();
										var z = mulbr.ReadSByte();
										var color = mulbr.ReadUInt16();
										var x = (ushort) (xblockamt + relX + sx);
										var y = (ushort) (yblockamt + relY + sy);
										statics.Add(new StaticItem(tileID, x, y, z, 255, color));
										//the diff files would have statics which aren't on mapplane 255. Etc.
									}
								}
							}
						}
					}
				}

				return statics.ToArray();
			}
			throw new SEException("Unable to locate statics0.mul or staidx0.mul. Using map disabled.");
		}

		internal bool HasStaticId(int x, int y, int staticId) {
			for (var a = 0; a < this.statics.Length; a++) {
				if (this.statics[a] != null) {
					var sta = this.statics[a];
					if (x == sta.X && y == sta.Y) {
						if (sta.Id == staticId) {
							return true;
						}
					}
				}
			}
			return false;
		}

		internal StaticItem GetStatic(int x, int y, int z, int tileID) {
			foreach (var stat in this.statics) {
				//Logger.WriteDebug("Compare static ("+stat.X+","+stat.Y+","+stat.Z+","+stat.M+":"+stat.id+" ("+stat.Name+")) to ("+x+","+y+","+z+":"+tileID+")");
				if (stat.X == x && stat.Y == y && stat.Z == z && stat.Id == tileID) {
					return stat;
				}
			}
			return null;
		}

		//internal void GetRMS(int x, int y, out int rmsx, out int rmsy) {
		//    //basex/basey are the base x/y coordinates of this sector.
		//    int basex = x & Map.sectorAnd;
		//    int basey = y & Map.sectorAnd;

		//    //mbasex/mbasey are the base x/y coordinates of this MUL sector.
		//    int mbasex = x & Map.mulSectorAnd;
		//    int mbasey = y & Map.mulSectorAnd;

		//    //diffbasex/y are the difference between the base x/y coords,
		//    //which we need to determine the relative map sector inside this SE sector.
		//    int diffbasex = mbasex - basex;
		//    int diffbasey = mbasey - basey;

		//    //rmsx/rmsy are the coordinates to Richard M. Stallman's homepage.
		//    //Actually, they specify what MUL map sector inside this SE sector we're working on,
		//    //their value being relative, from 0 to the maximum number of MUL sectors per SE sector.
		//    rmsx = diffbasex >> 3;
		//    rmsy = diffbasey >> 3;
		//}

		internal int GetTileId(int relX, int relY)
		{
			if (Globals.useMap) {
				return this.tileMatrix[relX, relY];
			}
			return 0;
		}

		internal int GetTileZ(int relX, int relY)
		{
			if (Globals.useMap) {
				return this.zMatrix[relX, relY];
			}
			return 0;
		}

		//static ArrayList numStaticsPerSector = new ArrayList();
		//static int maxStaticsPerSector = 0;
		//internal void CollectStatistics(Static[] statics) {
		//	numStaticsPerSector.Add(statics.Length);
		//	if (statics.Length>maxStaticsPerSector) {
		//		maxStaticsPerSector=statics.Length;
		//	}
		//	//determine number of sectors with 0 statics, with 1, with more than 1.
		//	//determine average # of statics per sector
		//
		//}
		//
		//static internal void AnalyzeStatistics() {
		//	Console.WriteLine("maxStaticsPerSector: "+maxStaticsPerSector);
		//	int change=maxStaticsPerSector/100;
		//	// >>12 = /4096
		//	Console.WriteLine("# sectors with 0 statics: "+CountNumSectorsWithStaticsBetween(-1,0));
		//	int ns=0;
		//	int lastns=-change;
		//
		//	while (ns<=maxStaticsPerSector) {
		//		lastns=ns;
		//		ns+=change;
		//		Console.WriteLine("# sectors with >"+lastns+" and <="+ns+" statics: "+CountNumSectorsWithStaticsBetween(lastns, ns));
		//	}
		//}
		//
		//static private int CountNumSectorsWithStaticsBetween(int min, int max) {
		//	int total=0;
		//	for (int ss=0; ss<numStaticsPerSector.Count; ss++) {
		//		int i=(int) numStaticsPerSector[ss];
		//		if (i>min && i<=max) total++;
		//	}
		//	return total;
		//}

	}
}
