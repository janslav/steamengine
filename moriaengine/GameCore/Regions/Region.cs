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
	
	public class Region : PluginHolder {
		public static Regex rectRE = new Regex(@"(?<x1>(0x)?\d+)\s*(,|/s+)\s*(?<y1>(0x)?\d+)\s*(,|/s+)\s*(?<x2>(0x)?\d+)\s*(,|/s+)\s*(?<y2>(0x)?\d+)",
			RegexOptions.IgnoreCase|RegexOptions.CultureInvariant|RegexOptions.Compiled);

		protected string defname; //protected, we will make use of it in StaticRegion loading part...
		protected Point4D p; //spawnpoint
		protected string name; //this is typically not unique, containing spaces etc.
		
		internal IList<RegionRectangle> rectangles = new List<RegionRectangle>();
		protected Region parent;
		protected byte mapplane = 0; //protected, we will make use of it in StaticRegion loading part...
		protected bool mapplaneIsSet;
		protected int hierarchyIndex = -1;
		protected DateTime createdAt = DateTime.Now;

		

		//private readonly static Type[] constructorTypes = new Type[] {typeof(string), typeof(string), typeof(int)};
		public Region() : base() {
			this.p = new Point4D(0,0,0,0); //spawnpoint
			this.name = ""; //this is typically not unique, containing spaces etc.
			this.inactivated = false; //defaultly is activated
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

		public DateTime CreatedAt { 
			get {
				return createdAt;
			} 
		}

		public IList<ImmutableRectangle> Rectangles { 
			get {
				RegionRectangle[] arr = new RegionRectangle[this.rectangles.Count];
				rectangles.CopyTo(arr, 0);
				return arr;
			} 
		}

		public bool IsWorldRegion {
			get {
				return (this == StaticRegion.WorldRegion);
			}
		}

		public int HierarchyIndex { 
			get {
				return hierarchyIndex;
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
				ThrowIfDeleted();
				if(!ContainsPoint((Point2D)value)) {
					throw new SEException("Spawnpoint "+value.ToString()+" is not contained in the region "+ToString());
				}
				p = value;
			}
		}

		public bool IsChildOf(Region tested) {
			ThrowIfDeleted();
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
			ThrowIfDeleted();
			return false;
		}

		internal bool Trigger_DenyPutItemOn(DenyPutOnGroundArgs args) {
			ThrowIfDeleted();
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
			ThrowIfDeleted();
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
			ThrowIfDeleted();
		}

		public virtual void On_ItemEnter(ItemOnGroundArgs args) {
			ThrowIfDeleted();
		}
		
		public bool ContainsPoint(Point2D point) {
			foreach(ImmutableRectangle rect in Rectangles) {
				if(rect.Contains(point)) {
					return true;
				}
			}			
			return false;
		}

		public bool ContainsPoint(ushort x, ushort y) {
			foreach(ImmutableRectangle rect in Rectangles) {
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
			ThrowIfDeleted();
			if (!TryCancellableTrigger(TriggerKey.enter, new ScriptArgs(ch, 0))) {
				if (!On_Enter(ch, false)) {
					return true;
				}
			}
			return false;
		}
		
		public bool TryExit(AbstractCharacter ch) {
			ThrowIfDeleted();
			if (!TryCancellableTrigger(TriggerKey.exit, new ScriptArgs(ch, 0))) {
				if (!On_Exit(ch, false)) {
					return true;
				}
			}
			return false;
		}
		
		public void Enter(AbstractCharacter ch) {
			ThrowIfDeleted();
			TryTrigger(TriggerKey.enter, new ScriptArgs(ch, 1));
			On_Enter(ch, true);
		}
		
		public void Exit(AbstractCharacter ch) {
			ThrowIfDeleted();
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

		protected bool inactivated = false;
		//this will be set to false when the region is going to be edited
		//after succesful editing (changing of P or changing rectnagles) it will be then reset to true
		//without this, the region won't be activated (Error will occur)
		protected bool canBeActivated = true;		

		public bool IsInactivated {
			get {
				return inactivated;
			}
		}		

		public override void LoadLine(string filename, int line, string valueName, string valueString) {
			switch (valueName) {
				case "category":
				case "subsection":
				case "description":
					return;
				case "event":
				case "events":
				case "type":
				case "triggergroup":
				case "resources"://in sphere, resources are the same like events... is it gonna be that way too in SE?
					base.LoadLine(filename, line, "triggergroup", valueString);
					break;
				case "rect":
				case "rectangle": //RECT=2300,3612,3264,4096
					Match m = rectRE.Match(valueString);
					if (m.Success) {
						GroupCollection gc = m.Groups;
						ushort x1 = TagMath.ParseUInt16(gc["x1"].Value);
						ushort y1 = TagMath.ParseUInt16(gc["y1"].Value);
						ushort x2 = TagMath.ParseUInt16(gc["x2"].Value);
						ushort y2 = TagMath.ParseUInt16(gc["y2"].Value);
						//Point2D point1 = new Point2D(x1, y1);
						//Point2D point2 = new Point2D(x2, y2);
						RegionRectangle rr = new RegionRectangle(x1,y1,x2,y2, this);
						//RegionRectangle rr = new RegionRectangle(point1, point2, this);//throws sanityExcepton if the points are not the correct corners. Or should we check it here? as in RegionImporter?
						this.rectangles.Add(rr);
					} else {
						throw new SEException("Unrecognized Rectangle format ('" + valueString + "')");
					}
					break;
				case "p":
				case "spawnpoint":
					p = (Point4D) ObjectSaver.Load(valueString);
					break;
				case "mapplane":
					mapplane = TagMath.ParseByte(valueString);
					mapplaneIsSet = true;
					break;
				case "parent":
					ObjectSaver.Load(valueString, LoadParent_Delayed, filename, line);
					break;
				case "name":
					Match ma = ConvertTools.stringRE.Match(valueString);
					if (ma.Success) {
						this.name = String.Intern(ma.Groups["value"].Value);
					} else {
						this.name = String.Intern(valueString);
					}
					break;
				case "createdat":
					this.createdAt = (DateTime) ObjectSaver.OptimizedLoad_SimpleType(valueString, typeof(DateTime));
					break;
				default:
					base.LoadLine(filename, line, valueName, valueString);
					break;
			}
		}

		private void LoadParent_Delayed(object resolvedObject, string filename, int line) {
			Region reg = resolvedObject as Region;
			if (reg != null) {
				parent = reg;
			} else {
				Logger.WriteWarning(LogStr.FileLine(filename, line) + "'" + LogStr.Ident(resolvedObject) + "' is not a valid Region. Referenced as parent by '" + LogStr.Ident(Defname) + "'.");
			}
		}

		public override void Save(SaveStream output) {
			if (!string.IsNullOrEmpty(this.name)) {
				output.WriteValue("name", name);
			}
			output.WriteValue("p", p);
			output.WriteValue("createdat", createdAt);
			if (mapplane != 0) {
				output.WriteValue("mapplane", mapplane);
			}
			if (parent != null) {
				output.WriteValue("parent", this.parent);
			}
			//RECT=2300,3612,3264,4096
			foreach (RegionRectangle rect in this.rectangles) {
				output.WriteLine("rect=" + rect.minX + "," + rect.minY + "," + rect.maxX + "," + rect.maxY);
			}

			base.Save(output);
		}
	}

	internal class RegionRectangle : ImmutableRectangle {
		internal static readonly RegionRectangle[] emptyArray = new RegionRectangle[0];

		internal readonly Region region;
		internal RegionRectangle(ushort minX, ushort minY, ushort maxX, ushort maxY, Region region)
			: base(minX, minY, maxX, maxY) {
			this.region = region;
		}

		/*internal RegionRectangle(Point2D start, Point2D end, Region region)
			: base(start, end) {
			this.region = region;
		}*/

		internal RegionRectangle(AbstractRectangle rect, Region region)
			: base(rect.MinX, rect.MinY, rect.MaxX, rect.MaxY) {
			this.region = region;
		}

		[Remark("Alters all four rectangle's position coordinates for specified tiles in X and Y axes." +
				"Returns a new (moved) instance")]
		internal RegionRectangle Move(int timesX, int timesY) {
			return new RegionRectangle((ushort)(minX + timesX), (ushort)(minY + timesY),
									   (ushort)(maxX + timesX), (ushort)(maxY + timesY), region);
		}
	}
}
