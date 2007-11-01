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
	[Remark("Class implementing saving/loading of regions, controlling unique defnames etc.")]
	public class StaticRegion : Region {
		private static List<RegionRectangle> tempRectangles;//for loading purposes...		
		private static Dictionary<string, StaticRegion> byName;
		private static Dictionary<string, StaticRegion> byDefname;

		[Remark("Clearing of the lists of all regions")]
		public new static void ClearAll() {
			byName = new Dictionary<string, StaticRegion>(StringComparer.OrdinalIgnoreCase);
			byDefname = new Dictionary<string, StaticRegion>(StringComparer.OrdinalIgnoreCase);
			tempRectangles = new List<RegionRectangle>();
		}

		public StaticRegion()
			: base() {
		}

		#region Saving/Loading
		[LoadSection]
		public StaticRegion(PropsSection input) {
			string defName = input.headerName;

			if(!byDefname.ContainsKey(defName)) {
				byDefname[defName] = this;
				this.defname = defName;
			} else {
				throw new OverrideNotAllowedException("Region " + LogStr.Ident(defName) + " loaded multiple times.");
			}

			tempRectangles.Clear();
			this.LoadSectionLines(input);
			this.rectangles = tempRectangles.ToArray();
		}

		public sealed class StaticRegionSaveCoordinator : IBaseClassSaveCoordinator {
			public static readonly Regex regionNameRE = new Regex(@"^\(\s*(?<value>\w*)\s*\)\s*$",
				RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

			public string FileNameToSave {
				get {
					return "regions";
				}
			}

			public void StartingLoading() {

			}

			public void SaveAll(SaveStream output) {
				Logger.WriteDebug("Saving Static Regions.");
				output.WriteComment("Static Regions");
				output.WriteLine();

				foreach(StaticRegion region in byDefname.Values) {
					region.SaveWithHeader(output);
				}

				Logger.WriteDebug("Saved " + byDefname.Count + " static regions.");
			}

			public void LoadingFinished() {
				if(highestHierarchyIndex != -1) {
					return;
				}
				try {
					Logger.WriteDebug("Resolving loaded static regions");

					foreach(StaticRegion region in byDefname.Values) {
						if(region.parent == null) {
							if(worldRegion == voidRegion) {
								worldRegion = region;
							} else {
								throw new SEException("Parent missing for the region " + LogStr.Ident(region.defname));
							}
						}
					}

					if(worldRegion == voidRegion) {
						throw new SEException("No world region defined.");
					}

					List<StaticRegion> tempList = new List<StaticRegion>(byDefname.Values);//copy list of all regions
					int lastCount = -1;
					while(tempList.Count > 0) {
						if(lastCount == tempList.Count) {
							//this will probably never happen
							throw new SEException("Region hierarchy not completely resolvable.");
						}
						lastCount = tempList.Count;
						StaticRegion r = tempList[lastCount - 1];
						r.SetHierarchyIndex(tempList);
					}

					foreach(StaticRegion region in byDefname.Values) {
						byName[region.Name] = region;
					}

					//and now the (cpu) intensive part :)  check if the declared hierarchy is right
					//it has no real effect, only showing warnings about overlapping regions
					//it's optional - part of the "resolveEverythingAtStart" option in .ini
					if(Globals.resolveEverythingAtStart) {
						CheckAllRegions();
					}

					//and now finally activate the regions - spread the references to their rectangles to the map sectors :)
					List<StaticRegion>[] regionsByMapplane = new List<StaticRegion>[0x100];
					foreach(StaticRegion region in byDefname.Values) {
						List<StaticRegion> list = regionsByMapplane[region.Mapplane];
						if(list == null) {
							list = new List<StaticRegion>();
							regionsByMapplane[region.Mapplane] = list;
						}
						if(!region.IsWorldRegion) { //we dont want the world region in sectors...
							list.Add(region);
						}
					}
					for(int i = 0, n = regionsByMapplane.Length; i < n; i++) {
						List<StaticRegion> list = regionsByMapplane[i];
						if(list != null) {
							Map map = Map.GetMap((byte)i);
							map.ActivateRegions(list);
						}
					}
				} catch(FatalException) {
					throw;
				} catch(Exception e) {
					Logger.WriteCritical("Regions not used.", e);
					ClearAll();
				}
			}

			public Type BaseType {
				get {
					return typeof(StaticRegion);
				}
			}

			public string GetReferenceLine(object value) {
				return "(" + ((Region)value).Defname + ")";
			}

			public Regex ReferenceLineRecognizer {
				get {
					return regionNameRE;
				}
			}

			public object Load(Match m) {
				return StaticRegion.GetByDefname(m.Groups["value"].Value);
			}
		}

		private int SetHierarchyIndex(List<StaticRegion> tempList) {
			if(parent == null) {
				if(IsWorldRegion) {
					hierarchyIndex = 0;
					tempList.Remove(this);
					return 0;
				} else {
					throw new SEException("Region " + this + " has no parent region set");
				}
			} else if(!this.HasSameMapplane(parent)) {
				throw new SEException("Region " + this + " has as parent set " + parent + ", which is on another mapplane.");
			} else {
				int parentIndex = ((StaticRegion)parent).SetHierarchyIndex(tempList);
				hierarchyIndex = parentIndex + 1;
				highestHierarchyIndex = Math.Max(highestHierarchyIndex, hierarchyIndex);
				tempList.Remove(this);
				return hierarchyIndex;
			}
		}

		//at some point, we could make this use ObjectSaver
		internal void LoadParent_Delayed(object resolvedObject, string filename, int line) {
			Region reg = resolvedObject as Region;
			if(reg != null) {
				parent = reg;
			} else {
				Logger.WriteWarning(LogStr.FileLine(filename, line) + "'" + LogStr.Ident(resolvedObject) + "' is not a valid Region. Referenced as parent by '" + LogStr.Ident(Defname) + "'.");
			}
		}

		public override void LoadLine(string filename, int line, string param, string args) {
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
					if(m.Success) {
						GroupCollection gc = m.Groups;
						ushort x1 = TagMath.ParseUInt16(gc["x1"].Value);
						ushort y1 = TagMath.ParseUInt16(gc["y1"].Value);
						ushort x2 = TagMath.ParseUInt16(gc["x2"].Value);
						ushort y2 = TagMath.ParseUInt16(gc["y2"].Value);
						Point2D point1 = new Point2D(x1, y1);
						Point2D point2 = new Point2D(x2, y2);
						RegionRectangle rr = new RegionRectangle(point1, point2, this);//throws sanityExcepton if the points are not the correct corners. Or should we check it here? as in RegionImporter?
						StaticRegion.tempRectangles.Add(rr);//tempRectangles are then resolved statically (arraylist to array)
					} else {
						throw new SEException("Unrecognized Rectangle format ('" + args + "')");
					}
					break;
				case "p":
				case "spawnpoint":
					p = (Point4D)ObjectSaver.Load(args);
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
					if(ma.Success) {
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

		[Save]
		public void SaveWithHeader(SaveStream output) {
			output.WriteSection(this.GetType().Name, this.defname);
			this.Save(output);
			output.WriteLine();
		}

		public override void Save(SaveStream output) {
			base.Save(output);//tagholder save

			if(!string.IsNullOrEmpty(this.name)) {
				output.WriteValue("name", name);
			}
			output.WriteValue("p", p);
			output.WriteValue("createdat", createdAt);
			if(mapplane != 0) {
				output.WriteValue("mapplane", mapplane);
			}
			if(parent != null) {
				output.WriteValue("parent", this.parent);
			}
			//RECT=2300,3612,3264,4096
			foreach(RegionRectangle rect in this.rectangles) {
				Point2D start = rect.StartPoint;
				Point2D end = rect.EndPoint;
				output.WriteLine("rect=" + start.X + "," + start.Y + "," + end.X + "," + end.Y);
			}
		}
		#endregion

		#region Name/defname labouring methods
		public static StaticRegion Get(string nameOrDefName) {
			StaticRegion retVal;
			if(!byDefname.TryGetValue(nameOrDefName, out retVal)) {
				byName.TryGetValue(nameOrDefName, out retVal);
			}
			return retVal;
		}

		public static StaticRegion GetByName(string name) {
			StaticRegion region;
			byName.TryGetValue(name, out region);
			return region;
		}

		public static StaticRegion GetByDefname(string defname) {
			StaticRegion region;
			byDefname.TryGetValue(defname, out region);
			return region;
		}

		public static IEnumerable<StaticRegion> AllRegions {
			get {
				return byDefname.Values;
			}
		}
		#endregion

		#region Other regions mutual positions checks
		private bool ContainsRectangle(Rectangle2D rect) {
			return (IsWorldRegion || (ContainsPoint(rect.StartPoint)//left upper
					&& ContainsPoint(new Point2D(rect.StartPoint.X, rect.EndPoint.Y)) //left lower
					&& ContainsPoint(rect.EndPoint) //right lower
					&& ContainsPoint(new Point2D(rect.EndPoint.X, rect.StartPoint.Y))));//right upper
		}

		private bool ContainsRectanglePartly(Rectangle2D rect) {
			return (IsWorldRegion || (ContainsPoint(rect.StartPoint)//left upper
					|| ContainsPoint(new Point2D(rect.StartPoint.X, rect.EndPoint.Y)) //left lower
					|| ContainsPoint(rect.EndPoint) //right lower
					|| ContainsPoint(new Point2D(rect.EndPoint.X, rect.StartPoint.Y))));//right upper
		}

		private void CheckHasAllRectanglesIn(StaticRegion other) {
			for(int i = 0, n = rectangles.Length; i < n; i++) {
				Rectangle2D rect = rectangles[i];
				if(!other.ContainsRectangle(rect)) {
					Logger.WriteWarning("Rectangle " + LogStr.Ident(rect) + " of region " + LogStr.Ident(defname) + " should be contained within region " + LogStr.Ident(other.defname) + ", but is not.");
				}
			}
		}

		private void CheckHasNoRectanglesIn(StaticRegion other) {
			for(int i = 0, n = rectangles.Length; i < n; i++) {
				Rectangle2D rect = rectangles[i];
				if(other.ContainsRectanglePartly(rect)) {
					Logger.WriteWarning("Rectangle " + LogStr.Ident(rect) + " of region " + LogStr.Ident(defname) + " overlaps with " + LogStr.Ident(other.defname) + ", but should not.");
				}
			}
		}

		public static void CheckAllRegions() {
			int i = 0, n = byDefname.Count;

			foreach(StaticRegion region in byDefname.Values) {
				if((i % 20) == 0) {
					Logger.SetTitle("Checking regions: " + ((i * 100) / n) + " %");
				}
				region.CheckConflictsAndWarn();
				i++;
			}
			Logger.SetTitle("");
		}

		private void CheckConflictsAndWarn() {
			if(parent != null) {
				CheckHasAllRectanglesIn((StaticRegion)parent);
			}
			foreach(StaticRegion other in byDefname.Values) {
				//Console.WriteLine("CheckConflictsAndWarn for "+this+" and "+other);
				if((other != this) && this.HasSameMapplane(other)) {
					if(other.hierarchyIndex == this.hierarchyIndex) {
						//Console.WriteLine("CheckConflictsAndWarn in progress");
						this.CheckHasNoRectanglesIn(other);
					}
				}
			}
		}
		#endregion

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
	}
}