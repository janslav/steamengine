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
		internal static readonly StaticRegion voidRegion = new StaticRegion();
		private static StaticRegion worldRegion = voidRegion;
		
		private static Dictionary<string, StaticRegion> byName;
		private static Dictionary<string, StaticRegion> byDefname;
		private static int highestHierarchyIndex = -1;

		static StaticRegion() {
			ClearAll();
		}

		[Remark("Clearing of the lists of all regions")]
		public static void ClearAll() {
			byName = new Dictionary<string, StaticRegion>(StringComparer.OrdinalIgnoreCase);
			byDefname = new Dictionary<string, StaticRegion>(StringComparer.OrdinalIgnoreCase);

			worldRegion = voidRegion;
			highestHierarchyIndex = -1;

			voidRegion.defname = "";
			voidRegion.name = "void";
		}

		public StaticRegion()
			: base() {
		}

		public static int HighestHierarchyIndex {
			get {
				return highestHierarchyIndex;
			}
		}

		public static StaticRegion WorldRegion {
			get {
				return worldRegion;
			}
		}

		#region Saving/Loading/Manipulating
		[LoadSection]
		public StaticRegion(PropsSection input) {
			string defName = input.headerName;

			if(!byDefname.ContainsKey(defName)) {
				byDefname[defName] = this;
				this.defname = defName;
			} else {
				throw new OverrideNotAllowedException("Region " + LogStr.Ident(defName) + " loaded multiple times.");
			}

			this.LoadSectionLines(input);
			this.inactivated = false;
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

					LinkedList<StaticRegion> tempList = new LinkedList<StaticRegion>(byDefname.Values);//copy list of all regions
					int lastCount = -1;
					while(tempList.Count > 0) {
						if(lastCount == tempList.Count) {
							//this will probably never happen
							throw new SEException("Region hierarchy not completely resolvable.");
						}
						lastCount = tempList.Count;
						StaticRegion r = tempList.Last.Value;
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

		private int SetHierarchyIndex(ICollection<StaticRegion> tempList) {
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

		[Save]
		public void SaveWithHeader(SaveStream output) {
			ThrowIfDeleted();
			output.WriteSection(this.GetType().Name, this.defname);
			this.Save(output);
			output.WriteLine();
		}

		[Remark("Useful when editing regions - we need to manipulate with their rectangles which can be done only in inactivated state")]
		private static void InactivateAll() {			
			foreach(Map map in Map.AllMaps) {
				map.InactivateRegions();
			}
			foreach(StaticRegion reg in AllRegions) {
				reg.inactivated = true; //inactivate
			}
		}

		[Remark("Useful when editing regions - we need to manipulate with their rectangles which can be done only in unloaded state"+
				"called after manipulation is successfully done."+
				"Activates only activable regions")]
		private static void ActivateAll() {
			List<StaticRegion> activeRegs = new List<StaticRegion>();
			foreach(StaticRegion reg in AllRegions) {
				if(reg.canBeActivated) {
					activeRegs.Add(reg);
				}
			}
			foreach(Map map in Map.AllMaps) {
				map.ActivateRegions(activeRegs);
			}
			foreach(StaticRegion reg in activeRegs) {
				reg.inactivated = false; //activate the activated regions
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

		[Remark("Searches through all regions and returns the list of StaticRegions thats name contains the "+
				"criteria string")]
		public static List<StaticRegion> FindByString(string criteria) {
			List<StaticRegion> regList = new List<StaticRegion>();
			foreach(StaticRegion reg in AllRegions) {
				if(criteria.Equals("")) {
					regList.Add(reg);//bereme vse
				} else if(reg.Name.ToUpper().Contains(criteria.ToUpper())) {
					regList.Add(reg);//jinak jen v pripade ze kriterium se vyskytuje v nazvu regionu
				}
			}
			return regList;
		}
		#endregion

		#region Other regions mutual positions checks
		private bool HasSameMapplane(Region reg) {
			if(this.Mapplane == reg.Mapplane) {
				return true;
			} else if(this.IsWorldRegion) {
				return true;
			} else if(reg.IsWorldRegion) {
				return true;
			}
			return false;
		}

		private bool ContainsRectangle(ImmutableRectangle rect) {
			return (IsWorldRegion || (ContainsPoint(rect.MinX,rect.MinY)//left upper
					&& ContainsPoint(rect.MinX, rect.MaxY) //left lower
					&& ContainsPoint(rect.MaxX, rect.MaxY) //right lower
					&& ContainsPoint(rect.MaxX, rect.MinY)));//right upper
		}

		private bool ContainsRectanglePartly(ImmutableRectangle rect) {
			return (IsWorldRegion || (ContainsPoint(rect.MinX, rect.MinY)//left upper
					|| ContainsPoint(rect.MinX, rect.MaxY) //left lower
					|| ContainsPoint(rect.MaxX, rect.MaxY) //right lower
					|| ContainsPoint(rect.MaxX, rect.MinY)));//right upper
		}

		private bool CheckHasAllRectanglesIn(StaticRegion other) {
			bool retState = true;
			for(int i = 0, n = rectangles.Count; i < n; i++) {
				ImmutableRectangle rect = rectangles[i];
				if(!other.ContainsRectangle(rect)) {
					Logger.WriteWarning("Rectangle " + LogStr.Ident(rect) + " of region " + LogStr.Ident(defname) + " should be contained within region " + LogStr.Ident(other.defname) + ", but is not.");
					retState = false; //problem
				}
			}
			return retState;
		}

		private bool CheckHasNoRectanglesIn(StaticRegion other) {
			bool retState = true;
			for(int i = 0, n = rectangles.Count; i < n; i++) {
				ImmutableRectangle rect = rectangles[i];
				if(other.ContainsRectanglePartly(rect)) {
					Logger.WriteWarning("Rectangle " + LogStr.Ident(rect) + " of region " + LogStr.Ident(defname) + " overlaps with " + LogStr.Ident(other.defname) + ", but should not.");
					retState = false; //problem
				}
			}
			return retState;
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

		private bool CheckConflictsAndWarn() {
			bool retState = true;			
			if(parent != null) {
				if(!CheckHasAllRectanglesIn((StaticRegion)parent)) {
					retState = false; //problem
				}
			}
			foreach(StaticRegion other in byDefname.Values) {
				//Console.WriteLine("CheckConflictsAndWarn for "+this+" and "+other);
				if((other != this) && this.HasSameMapplane(other)) {
					if(other.hierarchyIndex == this.hierarchyIndex) {
						//Console.WriteLine("CheckConflictsAndWarn in progress");
						if(!this.CheckHasNoRectanglesIn(other)) {
							retState = false; //problem
						}
					}
				}
			}
			return retState;
		}
		#endregion

		[Remark("Take the list of rectangles and make an array of RegionRectangles of it")]
		public bool SetRectangles<T>(IList<T> list) where T : AbstractRectangle {
			bool result = true;
			RegionRectangle[] newArr = new RegionRectangle[list.Count];
			for(int i = 0; i < list.Count; i++) {
				//take the start/end point from the IRectangle and create a new RegionRectangle
				newArr[i] = new RegionRectangle(list[i], this);				
			}
			//now the checking phase!
			IList<RegionRectangle> oldRects = rectangles; //save
			StaticRegion.InactivateAll(); //unload regions - it 'locks' them for every usage except for rectangles operations
			this.canBeActivated = false;
			try {
				rectangles = newArr; //switch the rectangles			
				if(!this.CheckConflictsAndWarn()) { //check the edited region for possible problems
					rectangles = oldRects; //return the previous set of rectangles
					result = false; //some problem
				}
				this.canBeActivated = true; //if we are here, everythin went fine or with simple warnings, which does not cause the fatal problem -)
			}  finally {				
				StaticRegion.ActivateAll();//all OK
			}
			return result;
		}

		public override string Name {
			get {
				return name;
			}
			set {
				ThrowIfDeleted();
				byName.Remove(name);
				name = String.Intern(value);
				byName[value] = this;
			}
		}

		public override void Delete() {
			///TODO / patrne bude potreba mazat i jeho deti (protoze kdybych ho pak chtel pridat zpet do sveta
			///tak jeho deti mi tam budou prekazet a tak vubec). kazdopadne je potreba to minimalne promyslet
			///a prokonzultovat s Tramtarem
 			base.Delete();
		}
	}
}
