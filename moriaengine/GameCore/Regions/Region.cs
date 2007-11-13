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
using System.Reflection;
using System.Text.RegularExpressions;
using SteamEngine.Common;
using SteamEngine.Persistence;

namespace SteamEngine.Regions {
	
	//todo: make some members virtual?
	public class Region : PluginHolder {
		public static Regex rectRE = new Regex(@"(?<x1>(0x)?\d+)\s*(,|/s+)\s*(?<y1>(0x)?\d+)\s*(,|/s+)\s*(?<x2>(0x)?\d+)\s*(,|/s+)\s*(?<y2>(0x)?\d+)",
			RegexOptions.IgnoreCase|RegexOptions.CultureInvariant|RegexOptions.Compiled);

		internal static readonly Region voidRegion = new Region();
		protected static Region worldRegion = voidRegion;
		protected static int highestHierarchyIndex = -1;
		
		static Region() {
			ClearAll();
		}

		public static void ClearAll() {
			worldRegion = voidRegion;
			highestHierarchyIndex = -1;

			voidRegion.defname = "";
			voidRegion.name =  "void";
		}

		protected string defname; //protected, we will make use of it in StaticRegion loading part...
		protected Point4D p; //spawnpoint
		protected string name; //this is typically not unique, containing spaces etc.
		
		protected RegionRectangle[] rectangles;
		protected Region parent;
		protected byte mapplane = 0; //protected, we will make use of it in StaticRegion loading part...
		protected bool mapplaneIsSet;
		protected int hierarchyIndex = -1;
		protected long createdAt;

		//private readonly static Type[] constructorTypes = new Type[] {typeof(string), typeof(string), typeof(int)};
		public Region() : base() {
			this.p = new Point4D(0,0,0,0); //spawnpoint
			this.name = ""; //this is typically not unique, containing spaces etc.
			this.createdAt = HighPerformanceTimer.TickCount;
		}

		public Region Parent { 
			get {
				return parent;
			}	 
		}

		public string Defname { 
			get {
				return defname;
			}
		}

		public long CreatedAt { 
			get {
				return createdAt;
			} 
		}

		public RegionRectangle[] Rectangles { 
			get {
				return rectangles;
			} 
		}
		
		public static Region WorldRegion { 
			get {
				return worldRegion;
			} 
		}
		
		public bool IsWorldRegion { 
			get {
				return (this == worldRegion);
			} 
		}
		
		public int HierarchyIndex { 
			get {
				return hierarchyIndex;
			} 
		}
		
		public static int HighestHierarchyIndex { 
			get {
				return highestHierarchyIndex;
			} 
		}
		
		public byte Mapplane { 
			get {
				if (!mapplaneIsSet) {
					mapplane = P.m;
					mapplaneIsSet = true;
				}
				return mapplane;
			}
		}
		
		public virtual Point4D P {
			get {
				return p;
			}
			set {
				p = value;
			}
		}

		public bool IsChildOf(Region tested) {
			if (parent == null) {
				return false;
			} else if (tested == parent) {
				return true;
			} else {
				return parent.IsChildOf(tested);
			}
		}

		private static Region FindCommonParent(Region a, Region b) {
			if (b.IsChildOf(a)) {
				return a;
			} else if (a.parent == b) {
				return b;
			} else {
				return FindCommonParent(a.parent, b);
			}
		}
		
		public static bool TryExitAndEnter(Region oldRegion, Region newRegion, AbstractCharacter ch) {
			Region sharedParent = FindCommonParent(oldRegion, newRegion);
			while (oldRegion != sharedParent) {
				if (!oldRegion.TryExit(ch)) {//cancelled
					return false;
				}
				oldRegion = oldRegion.parent;
			}
			if (sharedParent != newRegion) {//otherwise we are not entering anything
				return TryEnterHierarchy(sharedParent, newRegion, ch);
			}
			return true;
		}
		
		private static bool TryEnterHierarchy(Region parent, Region child, AbstractCharacter ch) {//enters the parent first, and then all the children till the given child
			if (child.parent != parent) {
				if (!TryEnterHierarchy(parent, child.parent, ch)) {
					return false;
				}
			}
			return child.TryEnter(ch);
		}
		
		public static void ExitAndEnter(Region oldRegion, Region newRegion, AbstractCharacter ch) {//exit and enter all the regions in hierarchy
			Region sharedParent = FindCommonParent(oldRegion, newRegion);
			while (oldRegion != sharedParent) {
				oldRegion.Exit(ch);
				oldRegion = oldRegion.parent;
			}
			if (sharedParent != newRegion) {//otherwise we are not entering anything
				EnterHierarchy(sharedParent, newRegion, ch);
			}
		}
		
		private static void EnterHierarchy(Region parent, Region child, AbstractCharacter ch) {//enters the parent first, and then all the children till the given child
			if (child.parent != parent) {
				EnterHierarchy(parent, child.parent, ch);
			}
			child.Enter(ch);
		}

		internal static void Trigger_ItemEnter(ItemOnGroundArgs args) {
			Region region = args.region;
			Point4D point = args.point;
			AbstractItem item = args.manipulatedItem;

			do {
				region.TryTrigger(TriggerKey.itemEnter, args);
				ReturnItemOnGroundIfNeeded(item, point);
				try {
					region.On_ItemEnter(args);
				} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
				ReturnItemOnGroundIfNeeded(item, point);

				region = region.parent;
			} while (region != null);
		}

		internal static void Trigger_ItemLeave(ItemOnGroundArgs args) {
			Region region = args.region;
			Point4D point = args.point;
			AbstractItem item = args.manipulatedItem;

			do {
				region.TryTrigger(TriggerKey.itemLeave, args);
				ReturnItemOnGroundIfNeeded(item, point);
				try {
					region.On_ItemLeave(args);
				} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
				ReturnItemOnGroundIfNeeded(item, point);

				region = region.parent;
			} while (region != null);
		}

		internal bool Trigger_DenyPickupItemFrom(DenyPickupArgs args) {
			Region region = this;

			bool cancel = false;
			do {
				if (!cancel) {
					cancel = region.TryCancellableTrigger(TriggerKey.denyPickupItemFrom, args);
					if (!cancel) {
						try {
							cancel = region.On_DenyPickupItemFrom(args);
						} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
					} else {
						break;
					}
				} else {
					break;
				}

				region = region.parent;
			} while (region != null);

			return cancel;
		}

		public virtual bool On_DenyPickupItemFrom(DenyPickupArgs args) {
			return false;
		}

		internal bool Trigger_DenyPutItemOn(DenyPutOnGroundArgs args) {
			Region region = this;

			bool cancel = false;
			do {
				if (!cancel) {
					cancel = region.TryCancellableTrigger(TriggerKey.denyPutItemOn, args);
					if (!cancel) {
						try {
							cancel = region.On_DenyPutItemOn(args);
						} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
					} else {
						break;
					}
				} else {
					break;
				}

				region = region.parent;
			} while (region != null);

			return cancel;
		}

		public virtual bool On_DenyPutItemOn(DenyPutOnGroundArgs args) {
			return false;
		}

		private static void ReturnItemOnGroundIfNeeded(AbstractItem item, Point4D point) {
			if ((item.Cont != null) || (!point.Equals(item))) {
				Logger.WriteWarning(item+" has been moved in the implementation of one of the @LeaveGround triggers. Don't do this. Putting back.");
				item.MakeLimbo();
				item.Trigger_EnterRegion(point.x, point.y, point.z, point.m);
			}
		}

		public virtual void On_ItemLeave(ItemOnGroundArgs args) {
		}

		public virtual void On_ItemEnter(ItemOnGroundArgs args) {
		}
		
		protected bool HasSameMapplane(Region reg) {
			if (this.Mapplane == reg.Mapplane) {
				return true;
			} else if (this.IsWorldRegion) {
				return true;
			} else if (reg.IsWorldRegion) {
				return true;
			}
			return false;
		}
		
		public bool ContainsPoint(Point2D point) {
			for (int i = 0, n = rectangles.Length; i<n; i++) {
				Rectangle2D rect = rectangles[i];
				if (rect.Contains(point)) {
					return true;
				}
			}
			return false;
		}

		public bool ContainsPoint(ushort x, ushort y) {
			for(int i = 0, n = rectangles.Length; i < n; i++) {
				Rectangle2D rect = rectangles[i];
				if(rect.Contains(x,y)) {
					return true;
				}
			}
			return false;
		}
		
		public override string ToString() {
			return GetType().Name+" "+defname;
		}
		
		public string HierarchyName { 
			get {
				if (parent == null) {
					return Name;
				} else {
					return Name+" in "+parent.HierarchyName;
				}
			} 
		}
		
		public bool TryEnter(AbstractCharacter ch) {
			if (!TryCancellableTrigger(TriggerKey.enter, new ScriptArgs(ch, 0))) {
				if (!On_Enter(ch, false)) {
					return true;
				}
			}
			return false;
		}
		
		public bool TryExit(AbstractCharacter ch) {
			if (!TryCancellableTrigger(TriggerKey.exit, new ScriptArgs(ch, 0))) {
				if (!On_Exit(ch, false)) {
					return true;
				}
			}
			return false;
		}
		
		public void Enter(AbstractCharacter ch) {
			TryTrigger(TriggerKey.enter, new ScriptArgs(ch, 1));
			On_Enter(ch, true);
		}
		
		public void Exit(AbstractCharacter ch) {
			TryTrigger(TriggerKey.exit, new ScriptArgs(ch, 1));
			On_Exit(ch, true);
		}
		
		public virtual bool On_Enter(AbstractCharacter ch, bool forced) {//if forced is true, the return value is irrelevant
			Logger.WriteDebug(ch+" entered "+this);
			ch.SysMessage("You have just entered "+this);
			return false;//maybe we could just return false or whatever...
		}
		
		public virtual bool On_Exit(AbstractCharacter ch, bool forced) {
			Logger.WriteDebug(ch+" left "+this);
			ch.SysMessage("You have just left "+this);
			return false;
		}
	}

	public class RegionRectangle : Rectangle2D {
		public static readonly RegionRectangle[] emptyArray = new RegionRectangle[0];
		
		public readonly Region region;
		public RegionRectangle(Point2D start, Point2D end, Region region) : base(start, end) {
			this.region = region;
		}

		public RegionRectangle(Rectangle2D rect, Region region)
			: base(rect.StartPoint, rect.EndPoint) {
			this.region = region;
		}

		public bool IntersectsWith(Rectangle2D rect) {
			return (Contains(rect.StartPoint)//left upper
					|| Contains(rect.StartPoint.x, rect.EndPoint.y) //left lower
					|| Contains(rect.EndPoint) //right lower
					|| Contains(rect.EndPoint.x, rect.StartPoint.y));//right upper
		}
	}
}
