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

namespace SteamEngine {
	
	//todo: make some members virtual?
	public class Region : TagHolder, IImportable {
		public static Regex rectRE = new Regex(@"(?<x1>(0x)?\d+)\s*(,|/s+)\s*(?<y1>(0x)?\d+)\s*(,|/s+)\s*(?<x2>(0x)?\d+)\s*(,|/s+)\s*(?<y2>(0x)?\d+)",
			RegexOptions.IgnoreCase|RegexOptions.CultureInvariant|RegexOptions.Compiled);

		internal static readonly Region voidRegion = new Region();
		private static Dictionary<string, Region> byName;
		private static Dictionary<string, Region> byDefname;
		private static List<RegionRectangle> tempRectangles;
		private static Dictionary<string,ConstructorInfo> constructorsByName = new Dictionary<string,ConstructorInfo>(StringComparer.OrdinalIgnoreCase);
		private static Region worldRegion = voidRegion;
		private static int highestHierarchyIndex = -1;
		
		static Region() {
			ClearAll();
		}

		public static void ClearAll() {
			byName = new Dictionary<string, Region>(StringComparer.OrdinalIgnoreCase);
			byDefname = new Dictionary<string, Region>(StringComparer.OrdinalIgnoreCase);
			tempRectangles = new List<RegionRectangle>();
			worldRegion = voidRegion;
			highestHierarchyIndex = -1;

			voidRegion.defname = "";
			voidRegion.name = "void";
		}

		private string defname;
		protected Point4D p; //spawnpoint
		private string name; //this is typically not unique, containing spaces etc.
		
		protected RegionRectangle[] rectangles;
		protected Region parent;
		private byte mapplane = 0;
		private bool mapplaneIsSet;
		private int hierarchyIndex = -1;
		private long createdAt;

		//private readonly static Type[] constructorTypes = new Type[] {typeof(string), typeof(string), typeof(int)};
		public Region() : base() {
			this.p = new Point4D(0,0,0,0); //spawnpoint
			this.name = ""; //this is typically not unique, containing spaces etc.
			this.createdAt = HighPerformanceTimer.TickCount;
		}

		public Region Parent { get {
			return parent;
		} }

		public string Defname { get {
			return defname;
		} }

		public long CreatedAt { get {
			return createdAt;
		} }

		public RegionRectangle[] Rectangles { get {
			return rectangles;
		} }
		
		public static Region WorldRegion { get {
			return worldRegion;
		} }
		
		public bool IsWorldRegion { get {
			return (this == worldRegion);
		} }
		
		public int HierarchyIndex { get {
			return hierarchyIndex;
		} }
		
		public static int HighestHierarchyIndex { get {
			return highestHierarchyIndex;
		} }
		
		public byte Mapplane { get {
			if (!mapplaneIsSet) {
				mapplane = P.M;
				mapplaneIsSet = true;
			}
			return mapplane;
		} }
		
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
		
		public static Region Get(string nameOrDefName) {
			Region retVal;
			if (!byDefname.TryGetValue(nameOrDefName, out retVal)) {
				byName.TryGetValue(nameOrDefName, out retVal);
			}
			return retVal;
		}

		public static Region GetByName(string name) {
			Region region;
			byName.TryGetValue(name, out region);
			return region;
		}

		public static Region GetByDefname(string defname) {
			Region region;
			byDefname.TryGetValue(defname, out region);
			return region;
		}

		public static IEnumerable<Region> AllRegions { get {
			return byDefname.Values;
		} }

		public static IEnumerable<IImportable> AllRegionsAsImportables {
			get {
				List<IImportable> retVal = new List<IImportable>(byDefname.Count);
				foreach (IImportable i in byDefname.Values) {
					retVal.Add(i);
				}
				return retVal;
			}
		}
		
		public override string Name { 
			get {
				return name;
			} 
			set {
				byName.Remove(name);
				name = String.Intern(value);
				byName[value] = this;
			}
		}

		public static void UnloadScripts() {
			ConstructorInfo[] ctors = new ConstructorInfo[constructorsByName.Count];
			constructorsByName.Values.CopyTo(ctors, 0);
			Assembly coreAssembly = CompiledScripts.ClassManager.CoreAssembly;

			foreach (ConstructorInfo cw in ctors) {
				Type type = cw.DeclaringType;
				if (coreAssembly != type.Assembly) {
					constructorsByName.Remove(type.Name);
				}
			}
		}
		
		internal static void StartingLoading() {
		}
		
		internal static bool IsRegionHeaderName(string header) {
			if (header.StartsWith("world")) {
				header = header.Substring(5);
			}
			if (header == "area") {
				header = "region";
			}
			return constructorsByName.ContainsKey(header);
		}
		
		internal static void RegisterRegionType(Type type) {
			ConstructorInfo cw = FindConstructor(type.GetConstructors(
				BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance));
			string typeName = type.Name;
			if (constructorsByName.ContainsKey(typeName)) {
				throw new SEException("Region class called '"+LogStr.Ident(typeName)+"' already exists.");
			}
			constructorsByName[typeName] = cw;
		}
		
		private static ConstructorInfo FindConstructor(ConstructorInfo[] ci) {
			if (ci.Length>0) {
				int match = -1;
				for (int a=0; a<ci.Length; a++) {
					ParameterInfo[] pi = ci[a].GetParameters();
					if (pi.Length==0) {
						match = a;
						break;
					}
				}
				if (match!=-1) {
					return MemberWrapper.GetWrapperFor(ci[match]);
				}
			}
			throw new SEException("No proper constructor.");
		}
		
		internal static void ClearRegisteredType() {
			constructorsByName.Clear();
		}
		
		internal static void Load(PropsSection input) {
			string typeName = input.headerType.ToLower();

			string defName = input.headerName;

			Region region;
			byDefname.TryGetValue(defName, out region);
			if (region == null) {
				ConstructorInfo cw = constructorsByName[typeName];
				region = (Region) cw.Invoke(null);
				byDefname[defName] = region;
				region.defname = defName;
			} else {
				throw new OverrideNotAllowedException("Region "+LogStr.Ident(defName)+" loaded multiple times.");	
			}

			tempRectangles.Clear();
			region.LoadSectionLines(input);
			region.rectangles = tempRectangles.ToArray();
		}
		
		internal static void LoadingFinished() {
			if (highestHierarchyIndex != -1) {
				return;
			}
			try {
				Logger.WriteDebug("Resolving loaded regions");

				foreach (Region region in byDefname.Values) {
					if (region.parent == null) {
						if (worldRegion == voidRegion) {
							worldRegion = region;
						} else {
							throw new SEException("Parent missing for the region "+LogStr.Ident(region.defname));
						}
					}
				}

				if (worldRegion == voidRegion) {
					throw new SEException("No world region defined.");
				}
				
				List<Region> tempList = new List<Region>(byDefname.Values);//copy list of all regions
				int lastCount = -1;
				while (tempList.Count > 0) {
					if (lastCount == tempList.Count) {
						//this will probably never happen
						throw new SEException("Region hierarchy not completely resolvable.");
					}
					lastCount = tempList.Count;
					Region r = tempList[lastCount - 1];
					r.SetHierarchyIndex(tempList);
				}

				foreach (Region region in byDefname.Values) {
					byName[region.Name] = region;
				}
				
				//and now the (cpu) intensive part :)  check if the declared hierarchy is right
				//it has no real effect, only showing warnings about overlapping regions
				//it's optional - part of the "resolveEverythingAtStart" option in .ini
				if (Globals.resolveEverythingAtStart) {
					CheckAllRegions();
				}
				
				//and now finally activate the regions - spread the references to their rectangles to the map sectors :)
				List<Region>[] regionsByMapplane = new List<Region>[0x100];
				foreach (Region region in byDefname.Values) {
					List<Region> list = regionsByMapplane[region.Mapplane];
					if (list == null) {
						list = new List<Region>();
						regionsByMapplane[region.Mapplane] = list;
					}
					if (!region.IsWorldRegion) { //we dont want the world region in sectors...
						list.Add(region);
					}
				}
				for (int i = 0, n = regionsByMapplane.Length; i<n; i++) {
					List<Region> list = regionsByMapplane[i];
					if (list != null) {
						Map map = Map.GetMap((byte) i);
						map.ActivateRegions(list);
					}
				}
			} catch (FatalException) {
				throw;
			} catch (Exception e) {
				Logger.WriteCritical("Regions not used.", e);
				ClearAll();
			}
		}
		
		private bool ContainsRectangle(Rectangle2D rect) {
			return (IsWorldRegion || (ContainsPoint(rect.StartPoint)//left upper
					&& ContainsPoint(new Point2D(rect.StartPoint.X, rect.EndPoint.Y)) //left lower
					&& ContainsPoint(rect.EndPoint) //right lower
					&& ContainsPoint(new Point2D(rect.EndPoint.X, rect.StartPoint.Y))));//right upper
		}
		
		private bool ContainsRectanglePartly(Rectangle2D rect) {
			return (IsWorldRegion ||(ContainsPoint(rect.StartPoint)//left upper
					|| ContainsPoint(new Point2D(rect.StartPoint.X, rect.EndPoint.Y)) //left lower
					|| ContainsPoint(rect.EndPoint) //right lower
					|| ContainsPoint(new Point2D(rect.EndPoint.X, rect.StartPoint.Y))));//right upper
		}
		
		private void CheckHasAllRectanglesIn(Region other) {
			for (int i = 0, n = rectangles.Length; i<n; i++) {
				Rectangle2D rect = rectangles[i];
				if (!other.ContainsRectangle(rect)) {
					Logger.WriteWarning("Rectangle "+LogStr.Ident(rect)+" of region "+LogStr.Ident(defname)+" should be contained within region "+LogStr.Ident(other.defname)+", but is not.");
				}
			}
		}
		
		private void CheckHasNoRectanglesIn(Region other) {
			for (int i = 0, n = rectangles.Length; i<n; i++) {
				Rectangle2D rect = rectangles[i];
				if (other.ContainsRectanglePartly(rect)) {
					Logger.WriteWarning("Rectangle "+LogStr.Ident(rect)+" of region "+LogStr.Ident(defname)+" overlaps with "+LogStr.Ident(other.defname)+", but should not.");
				}
			}
		}
		
		public static void CheckAllRegions() {
			int i = 0, n = byDefname.Count;

			foreach (Region region in byDefname.Values) {
				if ((i%20)==0) {
					Logger.SetTitle("Checking regions: "+((i*100)/n)+" %");
				}
				region.CheckConflictsAndWarn();
				i++;
			}
			Logger.SetTitle("");
		}
		
		private void CheckConflictsAndWarn() {
			if (parent != null) {
				CheckHasAllRectanglesIn(parent);
			}
			foreach (Region other in byDefname.Values) {
				//Console.WriteLine("CheckConflictsAndWarn for "+this+" and "+other);
				if ((other != this) && this.HasSameMapplane(other)) {
					if (other.hierarchyIndex == this.hierarchyIndex) {
						//Console.WriteLine("CheckConflictsAndWarn in progress");
						this.CheckHasNoRectanglesIn(other);
					}
				}
			}
		}
		
		private bool HasSameMapplane(Region reg) {
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
		
		private int SetHierarchyIndex(List<Region> tempList) {
			if (parent == null) {
				if (IsWorldRegion) {
					hierarchyIndex = 0;
					tempList.Remove(this);
					return 0;
				} else {
					throw new SEException("Region "+this+" has no parent region set");
				}
			} else if (!this.HasSameMapplane(parent)) {
				throw new SEException("Region "+this+" has as parent set "+parent+", which is on another mapplane.");
			} else {
				int parentIndex = parent.SetHierarchyIndex(tempList);
				hierarchyIndex = parentIndex+1;
				highestHierarchyIndex = Math.Max(highestHierarchyIndex, hierarchyIndex);
				tempList.Remove(this);
				return hierarchyIndex;
			}
		}
	
		//at some point, we could make this use ObjectSaver
		internal void LoadParent_Delayed(object resolvedObject, string filename, int line) {
			Region reg = resolvedObject as Region;
			if (reg != null) {
				parent = reg;
			} else {
				Logger.WriteWarning(LogStr.FileLine(filename, line)+"'"+LogStr.Ident(resolvedObject)+"' is not a valid Region. Referenced as parent by '"+LogStr.Ident(defname)+"'.");
			}
		}
		
		public override string ToString() {
			return GetType().Name+" "+defname;
		}
		
		public string HierarchyName { get {
			if (parent == null) {
				return Name;
			} else {
				return Name+" in "+parent.HierarchyName;
			}
		} }
		
		protected override void LoadLine(string filename, int line, string param, string args) {
			switch(param) {
				case "category":
				case "subsection":
				case "description": 
					return;
					//axis props are ignored
				case "event":
				case "events":
				case "type":
				case "triggergroup":
				case "resources"://in sphere, resources are the same like events... is it gonna be that way too in SE?
					base.LoadLine(filename, line, "triggergroup", args);
					break;
				case "rect":
				case "rectangle": //RECT=2300,3612,3264,4096
					Match m = rectRE.Match(args);
					if (m.Success) {
						GroupCollection gc = m.Groups;
						ushort x1 = TagMath.ParseUInt16(gc["x1"].Value);
						ushort y1 = TagMath.ParseUInt16(gc["y1"].Value);
						ushort x2 = TagMath.ParseUInt16(gc["x2"].Value);
						ushort y2 = TagMath.ParseUInt16(gc["y2"].Value);
						Point2D point1 = new Point2D(x1, y1);
						Point2D point2 = new Point2D(x2, y2);
						RegionRectangle rr = new RegionRectangle(point1, point2, this);//throws sanityExcepton if the points are not the correct corners. Or should we check it here? as in RegionImporter?
						Region.tempRectangles.Add(rr);//tempRectangles are then resolved statically (arraylist to array)
					} else {
						throw new SEException("Unrecognized Rectangle format ('"+args+"')");
					}
					break;
				case "p":
				case "spawnpoint":
					p = (Point4D) ObjectSaver.Load(args);
					break;
				case "mapplane":
					mapplane = TagMath.ParseByte(args);
					mapplaneIsSet = true;
					break;
				case "parent":
					ObjectSaver.Load(args, LoadParent_Delayed, filename, line);
					break;
				case "name":
					Match ma = ConvertTools.stringRE.Match(args);
					if (ma.Success) {
						name = String.Intern(ma.Groups["value"].Value);
					} else {
						name = String.Intern(args);
					}
					break;
				case "createdat":
					this.createdAt = ConvertTools.ParseInt64(args);
					break;
				default:
					base.LoadLine(filename, line, param, args);//the AbstractDef Loadline
					break;
			}
		}

		public static void SaveRegions(SaveStream output) {
			Logger.WriteDebug("Saving Regions.");
			output.WriteComment("Regions");
			output.WriteLine();

			foreach (Region region in byDefname.Values) {
				output.WriteSection(region.GetType().Name, region.defname);
				region.Save(output);
				output.WriteLine();
			}

			Logger.WriteDebug("Saved "+byDefname.Count+" regions.");
		}

		public override void Save(SaveStream output) {
			base.Save(output);//tagholder save

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
				Point2D start = rect.StartPoint;
				Point2D end = rect.EndPoint;
				output.WriteLine("rect="+start.X+","+start.Y+","+end.X+","+end.Y);
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

		private static bool someRegionWasImported = false;
		public void Import(ImportHelper helper) {
			someRegionWasImported = true;
			tempRectangles.Clear();
			this.LoadSectionLines(helper.Section);
			this.rectangles = tempRectangles.ToArray();
			byDefname[this.defname] = this;
		}

		public void Export(SaveStream output) {
			output.WriteSection(this.GetType().Name, this.defname);
			this.Save(output);
			output.WriteLine();
			ObjectSaver.FlushCache(output);
		}

		[ImportAssignMethod]
		public static void ImportAssign(ImportHelperCollection collection) {
			foreach (ImportHelper helper in collection.Enumerate(typeof(Region))) {
				PropsSection input = helper.Section;

				string typeName = input.headerType.ToLower();
				string defName = input.headerName;

				long createdAt = ConvertTools.ParseInt64(input.PopPropsLine("createdat").value);

				Region region;
				byDefname.TryGetValue(defName, out region);
				bool isNewAndRenamed = false;

				string origDefname = defName;
				if ((region != null) && (region.createdAt != createdAt)) {
					int i = 1; 
					while (byDefname.ContainsKey(defName)) {
						defName = origDefname+"_"+i; 
						i++;
					}
					Logger.WriteWarning("Imported region "+LogStr.Ident(origDefname)+" identified as new, and renamed to "+LogStr.Ident(defName));
					isNewAndRenamed = true;//a new one will be created with this defname
				}

				if ((region == null) || isNewAndRenamed) {
					ConstructorInfo cw;
					constructorsByName.TryGetValue(typeName, out cw);
					if (cw == null) {
						throw new SEException(input.filename, input.headerLine, " Unknown region type "+LogStr.Ident(helper.Section.headerType)+" in imported file.");
					}
					region = (Region) cw.Invoke(null);
					if (isNewAndRenamed) {
						region.defname = origDefname;
						collection.SetReplacedRegion(region);
					}
					region.defname = defName;
				}
				region.createdAt = createdAt;

				helper.instance = region;
			}
		}

		[ImportingFinishedMethod]
		public static void ImportingFinished(ImportHelperCollection collection) {
			if (someRegionWasImported) {
				try {
					foreach (Region region in byDefname.Values) {
						region.hierarchyIndex = -1;
					}

					worldRegion = voidRegion;
					highestHierarchyIndex = -1;

					LoadingFinished();
				} catch (FatalException) {
					throw;
				} catch (Exception e) {//kill!
					throw new FatalException("Error while importing Regions. Unable to revert changes, exiting.", e);
				}
			}
			someRegionWasImported = false;
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
	}
}
