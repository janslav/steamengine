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
using SteamEngine.Regions;
using System.Diagnostics;

namespace SteamEngine {
	public interface IMovementSettings {
		bool CanCrossLand {
			get;
		}
		bool CanSwim {
			get;
		}
		bool CanCrossLava {
			get;
		}
		bool CanFly {
			get;
		}
		bool IgnoreDoors {
			get;
		}
		int ClimbPower {
			get;
		} //max positive difference in 1 step
	}
}

namespace SteamEngine.Regions {
	
	//I think the movement implementation is important enough to move it into another 
	//file even though it's still the Map class
	public partial class Map {

		private const int PersonHeight = 16;
		//private const int StepHeight = 2;
		private const uint ImpassableSurface = TileData.flag_impassable | TileData.flag_surface;

		//private static bool m_AlwaysIgnoreDoors;
		//private static bool m_IgnoreMovableImpassables;


		private bool IsOk(bool ignoreDoors, int ourZ, int ourTop, List<Static> tiles, List<AbstractItem> items) {
			for (int i = 0, n = tiles.Count; i<n; i++) {
				Static check = tiles[i];
				ItemDispidInfo itemData = check.dispidInfo;

				if ((itemData.flags & ImpassableSurface) != 0) {// Impassable || Surface
					int checkZ = check.Z;
					int checkTop = checkZ + itemData.calcHeight;

					if (checkTop > ourZ && ourTop > checkZ)
						return false;
				}
			}

			for (int i = 0; i < items.Count; ++i) {
				AbstractItem item = items[i];

				ushort model = item.Model;
				ItemDispidInfo idi = ItemDispidInfo.Get(model);
				uint flags = idi.flags;

				if ((flags & ImpassableSurface) != 0) {// Impassable || Surface
					if (ignoreDoors && ((flags & TileData.flag_door) != 0 
							|| model == 0x692 || model == 0x846 || model == 0x873 || (model >= 0x6F5 && model <= 0x6F6)))
						//^^^^ ve standartnich tiledata.mul nemaj tyhle modely flag_door i kdyz sou to dvere
						continue;

					int checkZ = item.Z;
					int checkTop = checkZ + idi.calcHeight;

					if (checkTop > ourZ && ourTop > checkZ)
						return false;
				}
			}

			return true;
		}

		private List<AbstractItem>[] m_Pools = new List<AbstractItem>[] {
			new List<AbstractItem>(), new List<AbstractItem>(),new List<AbstractItem>(), new List<AbstractItem>(),
		};

		private List<Sector> m_Sectors = new List<Sector>();

		private List<Static> staticsPool = new List<Static>();

		private bool Check(IPoint3D point, IMovementSettings settings, List<AbstractItem> items, int x, int y, int startTop, int startZ, out int newZ) {
			newZ = 0;

			ushort landTile = this.GetTileId(x, y);
			int landZ = 0, landCenter = 0, landTop = 0;
			uint tileFlags = TileData.landFlags[landTile];

			bool canSwim = settings.CanSwim;
			bool canFly = settings.CanFly;
			bool canCrossLand = settings.CanCrossLand;
			bool canCrossLava = settings.CanCrossLava;
			bool ignoreDoors = settings.IgnoreDoors;

			bool landBlocks = TileData.HasFlag(tileFlags, TileData.flag_impassable);
			bool considerLand = !TileData.IsIgnoredByMovement(landTile);

			int pointZ = point.Z;

			//we can't go over land or it's no land...let's try swimming, lavawalking or flying
			if ((landBlocks) || (!canCrossLand)) {
				landBlocks = true; //land blocks us if we can't cross land
				bool isWater = ((tileFlags & TileData.flag_wet) == TileData.flag_wet);
				bool isLava = t_lava.IsTypeOfMapTile(landTile);
				if ((canSwim && isWater) ||
						(canCrossLava && isLava) ||
						(canFly)) {
					landBlocks = false;
				}
			}



			this.GetAverageZ(x, y, ref landZ, ref landCenter, ref landTop);

			bool moveIsOk = false;

			int stepHeight = settings.ClimbPower;
			int stepTop = startTop + stepHeight;
			int checkTop = startZ + PersonHeight;

			staticsPool.Clear();
			foreach (Static staticItem in this.GetStaticsOnCoords(x, y)) {
				staticsPool.Add(staticItem);
			}

			for (int i = 0, n = staticsPool.Count; i<n; i++) {
				Static staticItem = staticsPool[i];

				ItemDispidInfo idi = staticItem.dispidInfo;
				uint flags = idi.flags;

				bool staticIsWater = ((flags & TileData.flag_wet) == TileData.flag_wet);
				bool staticIsLava = t_lava.IsTypeOfMapTile(idi.id);

				if ((flags & ImpassableSurface) == TileData.flag_surface || // Surface && !Impassable
						(canSwim && staticIsWater) || //je to voda a my umime plavat
						(canCrossLava && staticIsLava) || //je to lava a nam nevadi
						(canFly)) {//umime litat a tak nam nevadi nic

					if (!canFly && !canCrossLand && !staticIsWater && !staticIsLava)
						continue;//neumime chodit/litat a neni to voda/lava (he?)

					int itemZ = staticItem.Z;
					int itemTop = itemZ;
					int ourZ = itemZ + idi.calcHeight;
					int ourTop = ourZ + PersonHeight;
					int testTop = checkTop;

					if (moveIsOk) {
						int cmp = Math.Abs(ourZ - pointZ) - Math.Abs(newZ - pointZ);

						if (cmp > 0 || (cmp == 0 && ourZ > newZ))
							continue;
					}

					if (ourZ + PersonHeight > testTop)
						testTop = ourZ + PersonHeight;

					if ((flags & TileData.flag_bridge) == 0)
						itemTop += idi.height;

					if (stepTop >= itemTop) {
						int landCheck = itemZ;

						if (idi.height >= stepHeight)
							landCheck += stepHeight;
						else
							landCheck += idi.height;

						if (considerLand && landCheck < landCenter && landCenter > ourZ && testTop > landZ)
							continue;

						if (IsOk(ignoreDoors, ourZ, testTop, staticsPool, items)) {
							newZ = ourZ;
							moveIsOk = true;
						}
					}
				}
			}

			for (int i = 0; i < items.Count; ++i) {
				AbstractItem item = items[i];
				ItemDispidInfo idi = ItemDispidInfo.Get(item.Model);
				uint flags = idi.flags;

				bool itemIsWater = ((flags & TileData.flag_wet) == TileData.flag_wet);
				bool itemIsLava = t_lava.IsTypeOfMapTile(idi.id);

				if (/*item.Flag_NeverMovable && */((flags & ImpassableSurface) == TileData.flag_surface || // Surface && !Impassable && !Movable
						(canSwim && itemIsWater) || //je to voda a my umime plavat
						(canCrossLava && itemIsLava) || //je to lava a nam nevadi
						(canFly))) {//umime litat a tak nam nevadi nic

					if (!canFly && !canCrossLand && !itemIsWater && !itemIsLava)
						continue;//neumime chodit/litat a neni to voda/lava (he?)

					int itemZ = item.Z;
					int itemTop = itemZ;
					int ourZ = itemZ + idi.calcHeight;
					int ourTop = ourZ + PersonHeight;
					int testTop = checkTop;

					if (moveIsOk) {
						int cmp = Math.Abs(ourZ - pointZ) - Math.Abs(newZ - pointZ);

						if (cmp > 0 || (cmp == 0 && ourZ > newZ))
							continue;
					}

					if (ourZ + PersonHeight > testTop)
						testTop = ourZ + PersonHeight;

					if ((flags & TileData.flag_bridge) == 0)
						itemTop += idi.height;

					if (stepTop >= itemTop) {
						int landCheck = itemZ;

						if (idi.height >= stepHeight)
							landCheck += stepHeight;
						else
							landCheck += idi.height;

						if (considerLand && landCheck < landCenter && landCenter > ourZ && testTop > landZ)
							continue;

						if (IsOk(ignoreDoors, ourZ, testTop, staticsPool, items)) {
							newZ = ourZ;
							moveIsOk = true;
						}
					}
				}
			}

			if (considerLand && !landBlocks && stepTop >= landZ) {
				int ourZ = landCenter;
				int ourTop = ourZ + PersonHeight;
				int testTop = checkTop;

				if (ourZ + PersonHeight > testTop)
					testTop = ourZ + PersonHeight;

				bool shouldCheck = true;

				if (moveIsOk) {
					int cmp = Math.Abs(ourZ - pointZ) - Math.Abs(newZ - pointZ);

					if (cmp > 0 || (cmp == 0 && ourZ > newZ))
						shouldCheck = false;
				}

				if (shouldCheck && IsOk(ignoreDoors, ourZ, testTop, staticsPool, items)) {
					newZ = ourZ;
					moveIsOk = true;
				}
			}

			return moveIsOk;
		}

		public bool CheckMovement(IPoint3D point, IMovementSettings settings, Direction d, bool hackMove, out int xForward, out int yForward, out int newZ) {
			int xStart = point.X;
			int yStart = point.Y;
			xForward = xStart;
			yForward = yStart;
			int xRight = xStart, yRight = yStart;
			int xLeft = xStart, yLeft = yStart;

			bool checkDiagonals = ((int) d & 0x1) == 0x1;

			Offset(d, ref xForward, ref yForward);

			Offset((Direction) (((int) d - 1) & 0x7), ref xLeft, ref yLeft);
			Offset((Direction) (((int) d + 1) & 0x7), ref xRight, ref yRight);

			if (xForward < 0 || yForward < 0 || xForward >= this.sizeX || yForward >= this.sizeY) {
				newZ = 0;
				return false;
			}

			int startZ, startTop;

			List<AbstractItem> itemsStart = m_Pools[0];
			List<AbstractItem> itemsForward = m_Pools[1];
			List<AbstractItem> itemsLeft = m_Pools[2];
			List<AbstractItem> itemsRight = m_Pools[3];

			uint reqFlags = ImpassableSurface;

			if (settings.CanSwim)
				reqFlags |= TileData.flag_wet;

			if (checkDiagonals) {
				Sector sectorStart = this.GetSector(xStart>>sectorFactor, yStart>>sectorFactor);
				Sector sectorForward = this.GetSector(xForward>>sectorFactor, yForward>>sectorFactor);
				Sector sectorLeft = this.GetSector(xLeft>>sectorFactor, yLeft>>sectorFactor);
				Sector sectorRight = this.GetSector(xRight>>sectorFactor, yRight>>sectorFactor);

				List<Sector> sectors = m_Sectors;

				sectors.Add(sectorStart);

				if (!sectors.Contains(sectorForward))
					sectors.Add(sectorForward);

				if (!sectors.Contains(sectorLeft))
					sectors.Add(sectorLeft);

				if (!sectors.Contains(sectorRight))
					sectors.Add(sectorRight);

				for (int i = 0; i < sectors.Count; ++i) {
					Sector sector = sectors[i];

					foreach (Thing t in sector.things) {
						AbstractItem item = t as AbstractItem;
						if (item == null) {
							continue;
						}

						ushort model = item.Model;
						ItemDispidInfo idi = ItemDispidInfo.Get(model);
						if ((idi.flags & reqFlags) == 0)
							continue;
						int itemX = item.X;
						int itemY = item.Y;

						if (sector == sectorStart && (itemX == xStart && itemY == yStart) && model < 0x4000)
							itemsStart.Add(item);
						else if (sector == sectorForward && (itemX == xForward && itemY == yForward) && model < 0x4000)
							itemsForward.Add(item);
						else if (sector == sectorLeft && (itemX == xLeft && itemY == yLeft) && model < 0x4000)
							itemsLeft.Add(item);
						else if (sector == sectorRight && (itemX == xRight && itemY == yRight) && model < 0x4000)
							itemsRight.Add(item);
					}
				}

				if (m_Sectors.Count > 0)
					m_Sectors.Clear();
			} else {
				Sector sectorStart = this.GetSector(xStart>>sectorFactor, yStart>>sectorFactor);
				Sector sectorForward = this.GetSector(xForward>>sectorFactor, yForward>>sectorFactor);

				if (sectorStart == sectorForward) {
					foreach (Thing t in sectorStart.things) {
						AbstractItem item = t as AbstractItem;
						if (item == null) {
							continue;
						}

						ushort model = item.Model;
						ItemDispidInfo idi = ItemDispidInfo.Get(model);
						if ((idi.flags & reqFlags) == 0)
							continue;
						int itemX = item.X;
						int itemY = item.Y;

						if (itemX == xStart && itemY == yStart && model < 0x4000)
							itemsStart.Add(item);
						else if (itemX == xForward && itemY == yForward && model < 0x4000)
							itemsForward.Add(item);
					}
				} else {
					foreach (Thing t in sectorForward.things) {
						AbstractItem item = t as AbstractItem;
						if (item == null) {
							continue;
						}

						ushort model = item.Model;
						ItemDispidInfo idi = ItemDispidInfo.Get(model);
						if ((idi.flags & reqFlags) == 0)
							continue;
						int itemX = item.X;
						int itemY = item.Y;

						if (itemX == xForward && itemY == yForward && model < 0x4000)
							itemsForward.Add(item);
					}

					foreach (Thing t in sectorStart.things) {
						AbstractItem item = t as AbstractItem;
						if (item == null) {
							continue;
						}

						ushort model = item.Model;
						ItemDispidInfo idi = ItemDispidInfo.Get(model);
						if ((idi.flags & reqFlags) == 0)
							continue;
						int itemX = item.X;
						int itemY = item.Y;

						if (itemX == xStart && itemY == yStart && model < 0x4000)
							itemsStart.Add(item);
					}
				}
			}

			GetStartZ(settings, point, itemsStart, out startZ, out startTop);

			bool moveIsOk = Check(point, settings, itemsForward, xForward, yForward, startTop, startZ, out newZ);
			if (moveIsOk && checkDiagonals) {
				int hold;
				//ani monstra ani hraci nemuzou projit sikmo pres roh, natoz pres diagonalni zed
				if (!Check(point, settings, itemsLeft, xLeft, yLeft, startTop, startZ, out hold) || !Check(point, settings, itemsRight, xRight, yRight, startTop, startZ, out hold))
					moveIsOk = false;
			}

			if (!moveIsOk) {
				if (hackMove) {
					moveIsOk = true;
				}
			}

			for (int i = 0; i < (checkDiagonals ? 4 : 2); ++i) {
				if (m_Pools[i].Count > 0)
					m_Pools[i].Clear();
			}

			if (!moveIsOk)
				newZ = startZ;

			return moveIsOk;
		}

		private void GetStartZ(IMovementSettings settings, IPoint3D point, List<AbstractItem> itemList, out int zLow, out int zTop) {
			int xCheck = point.X, yCheck = point.Y;

			ushort landTile = this.GetTileId(xCheck, yCheck);
			int landZ = 0, landCenter = 0, landTop = 0;
			uint tileFlags = TileData.landFlags[landTile];

			bool canSwim = settings.CanSwim;
			bool canFly = settings.CanFly;
			bool canCrossLand = settings.CanCrossLand;
			bool canCrossLava = settings.CanCrossLava;

			bool landBlocks = TileData.HasFlag(tileFlags, TileData.flag_impassable);

			//we can't go over land or it's no land...let's try swimming, lavawalking or flying
			if ((landBlocks) || (!canCrossLand)) {
				landBlocks = true; //land blocks us if we can't cross land
				bool isWater = ((tileFlags & TileData.flag_wet) == TileData.flag_wet);
				bool isLava = t_lava.IsTypeOfMapTile(landTile);
				if ((canSwim && isWater) ||
						(canCrossLava && isLava) ||
						(canFly)) {
					landBlocks = false;
				}
			}

			this.GetAverageZ(xCheck, yCheck, ref landZ, ref landCenter, ref landTop);

			bool considerLand = !TileData.IsIgnoredByMovement(landTile);

			int zCenter = zLow = zTop = 0;
			bool isSet = false;

			int pointZ = point.Z;

			if (considerLand && !landBlocks && pointZ >= landCenter) {
				zLow = landZ;
				zCenter = landCenter;

				if (landTop > zTop)
					zTop = landTop;

				isSet = true;
			}

			foreach (Static staticItem in this.GetStaticsOnCoords(xCheck, yCheck)) {

				ItemDispidInfo idi = staticItem.dispidInfo;

				int calcTop = (staticItem.Z + idi.calcHeight);

				bool staticIsWater = ((idi.flags & TileData.flag_wet) == TileData.flag_wet);
				bool staticIsLava = t_lava.IsTypeOfMapTile(idi.id);

				if ((!isSet || calcTop >= zCenter) && 
						((idi.flags & TileData.flag_surface) != 0 || //je to stul (eh?)
							(canSwim && staticIsWater) || //je to voda a my umime plavat
							(canCrossLava && staticIsLava) || //je to lava a nam nevadi
							(canFly) //umime litat a tak nam nevadi nic
							)						
						&& pointZ >= calcTop) {

					if (!canFly && !canCrossLand && !staticIsWater && !staticIsLava)
						continue;//neumime chodit/litat a neni to voda/lava (he?)

					zLow = staticItem.Z;
					zCenter = calcTop;

					int top = staticItem.Z + idi.height;

					if (!isSet || top > zTop)
						zTop = top;

					isSet = true;
				}
			}

			for (int i = 0; i < itemList.Count; ++i) {
				AbstractItem item = itemList[i];

				ItemDispidInfo idi = ItemDispidInfo.Get(item.Model);

				bool itemIsWater = ((idi.flags & TileData.flag_wet) == TileData.flag_wet);
				bool itemIsLava = t_lava.IsTypeOfMapTile(idi.id);

				int calcTop = item.Z + idi.calcHeight;

				if ((!isSet || calcTop >= zCenter) && 
						((idi.flags & TileData.flag_surface) != 0 || //je to stul (eh?)
							(canSwim && itemIsWater) || //je to voda a my umime plavat
							(canCrossLava && itemIsLava) || //je to lava a nam nevadi
							(canFly) //umime litat a tak nam nevadi nic
							)						
						&& pointZ >= calcTop) {

					if (!canFly && !canCrossLand && !itemIsWater && !itemIsLava)
						continue;//neumime chodit/litat a neni to voda/lava (he?)

					zLow = item.Z;
					zCenter = calcTop;

					int top = item.Z + idi.height;

					if (!isSet || top > zTop)
						zTop = top;

					isSet = true;
				}
			}

			if (!isSet)
				zLow = zTop = pointZ;
			else if (pointZ > zTop)
				zTop = pointZ;
		}

		public void Offset(Direction d, ref int x, ref int y) {
			switch (d) {
				case Direction.North: --y; break;
				case Direction.South: ++y; break;
				case Direction.West: --x; break;
				case Direction.East: ++x; break;
				case Direction.NorthEast: ++x; --y; break;
				case Direction.SouthWest: --x; ++y; break;
				case Direction.SouthEast: ++x; ++y; break;
				case Direction.NorthWest: --x; --y; break;
			}
		}

		private void GetAverageZ(int x, int y, ref int z, ref int avg, ref int top) {
			int zTop = this.GetTileZ(x, y);
			int zLeft = this.GetTileZ(x, y + 1);
			int zRight = this.GetTileZ(x + 1, y);
			int zBottom = this.GetTileZ(x + 1, y + 1);
			z = zTop;
			if (zLeft < z) {
				z = zLeft;
			}
			if (zRight < z) {
				z = zRight;
			}
			if (zBottom < z) {
				z = zBottom;
			}
			top = zTop;
			if (zLeft > top) {
				top = zLeft;
			}
			if (zRight > top) {
				top = zRight;
			}
			if (zBottom > top) {
				top = zBottom;
			}
			if (Math.Abs(zTop - zBottom) > Math.Abs(zLeft - zRight)) {
				avg = FloorAverage(zLeft, zRight);
			} else {
				avg = FloorAverage(zTop, zBottom);
			}
		}

		private static int FloorAverage(int a, int b) {
			int v = a + b;
			if (v < 0) {
				v--;
			}
			return (v / 2);
		}

		public void GetFixedZ(IPoint3D point, out sbyte newZ) {
			int oldZ = point.Z;
			int x = point.X;
			int y = point.Y;
			if (IsValidPos(x, y)) {
				newZ = this.GetTileZ(x, y);
			}
			newZ = (sbyte) oldZ;
		}
	}
}