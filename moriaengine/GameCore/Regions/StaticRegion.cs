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
	[Summary("Class implementing saving/loading of regions, controlling unique defnames etc.")]
	public class StaticRegion : Region {
		internal static readonly StaticRegion voidRegion = new StaticRegion("", "void");
		private static StaticRegion worldRegion = voidRegion;

		private static Dictionary<string, StaticRegion> byName = new Dictionary<string, StaticRegion>(StringComparer.OrdinalIgnoreCase);
		private static Dictionary<string, StaticRegion> byDefname = new Dictionary<string, StaticRegion>(StringComparer.OrdinalIgnoreCase);
		private static int highestHierarchyIndex = -1;

		private string name = ""; //this is typically not unique, containing spaces etc.

		private bool isDeleted;

		[Summary("Clearing of the lists of all regions")]
		public static void ClearAll() {
			byName = new Dictionary<string, StaticRegion>(StringComparer.OrdinalIgnoreCase);
			byDefname = new Dictionary<string, StaticRegion>(StringComparer.OrdinalIgnoreCase);

			worldRegion = voidRegion;
			highestHierarchyIndex = -1;
		}

		//only used by the void region
		private StaticRegion(string defname, string name) {
			this.name = name;
			base.Defname = defname;
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
			string defName = input.HeaderName;

			if (!byDefname.ContainsKey(defName)) {
				//byDefname[defName] = this;
				this.Defname = defName;
			} else {
				throw new OverrideNotAllowedException("Region " + LogStr.Ident(defName) + " loaded multiple times.");
			}

			this.LoadSectionLines(input);
			this.inactivated = false;
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
		internal sealed class StaticRegionSaveCoordinator : IBaseClassSaveCoordinator {
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

				foreach (StaticRegion region in AllRegions) {
					region.SaveWithHeader(output);
				}

				Logger.WriteDebug("Saved " + byDefname.Count + " static regions.");
			}

			[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
			public void LoadingFinished() {
				if (highestHierarchyIndex != -1) {
					return;
				}
				try {
					Logger.WriteDebug("Resolving loaded static regions");

					StaticRegion.ResolveParents();

					StaticRegion.ResolveRegionsHierarchy();

					foreach (StaticRegion region in AllRegions) {
						byName[region.Name] = region;
					}

					//and now the (cpu) intensive part :)  check if the declared hierarchy is right
					//it has no real effect, only showing warnings about overlapping regions
					//it's optional - part of the "resolveEverythingAtStart" option in .ini
					if (Globals.ResolveEverythingAtStart) {
						CheckAllRegions();
					}

					//and now finally activate the regions - spread the references to their rectangles to the map sectors :)
					StaticRegion.InitRectanglesToMaps(AllRegions);//use list of all available regions
				} catch (FatalException) {
					throw;
				} catch (Exception e) {
					Logger.WriteCritical("Regions not used.", e);
					ClearAll();
				}
			}

			public Type BaseType {
				get {
					return typeof(StaticRegion);
				}
			}

			[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
			public string GetReferenceLine(object value) {
				return "(" + ((Region) value).Defname + ")";
			}

			public Regex ReferenceLineRecognizer {
				get {
					return regionNameRE;
				}
			}

			[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
			public object Load(Match m) {
				return StaticRegion.GetByDefname(m.Groups["value"].Value);
			}
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
		public override void Save(SaveStream output) {
			if (!string.IsNullOrEmpty(this.name)) {
				output.WriteValue("name", this.name);
			}
			base.Save(output);
		}

		public override void LoadLine(string filename, int line, string valueName, string valueString) {
			switch (valueName) {
				case "name":
					Match ma = ConvertTools.stringRE.Match(valueString);
					if (ma.Success) {
						this.name = String.Intern(ma.Groups["value"].Value);
					} else {
						this.name = String.Intern(valueString);
					}
					break;
				default:
					base.LoadLine(filename, line, valueName, valueString);
					break;
			}
		}

		private int SetHierarchyIndex(ICollection<StaticRegion> tempList) {
			if (Parent == null) {
				if (IsWorldRegion) {
					HierarchyIndex = 0;
					tempList.Remove(this);
					return 0;
				} else {
					throw new SEException("Region " + this + " has no parent region set");
				}
			} else if (!this.HasSameMapplane(Parent)) {
				throw new SEException("Region " + this + " has as parent set " + Parent + ", which is on another mapplane.");
			} else {
				int parentIndex = ((StaticRegion) Parent).SetHierarchyIndex(tempList);
				HierarchyIndex = parentIndex + 1;
				highestHierarchyIndex = Math.Max(highestHierarchyIndex, HierarchyIndex);
				tempList.Remove(this);
				return HierarchyIndex;
			}
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods"), Save]
		public void SaveWithHeader(SaveStream output) {
			ThrowIfDeleted();
			output.WriteSection(this.GetType().Name, this.Defname);
			this.Save(output);
			output.WriteLine();
		}

		[Summary("Useful when editing regions - we need to manipulate with their rectangles which can be done only in inactivated state")]
		private static void InactivateAll() {
			foreach (Map map in Map.AllMaps) {
				map.InactivateRegions(false); //false - omit dynamic regions from clearing
			}
			foreach (StaticRegion reg in AllRegions) {
				reg.inactivated = true; //inactivate
			}
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes"), Summary("Useful when editing regions - we need to manipulate with their rectangles which can be done only in unloaded state" +
				"called after manipulation is successfully done." +
				"Activates only activable regions")]
		private static void ActivateAll() {
			List<StaticRegion> activeRegs = new List<StaticRegion>();
			foreach (StaticRegion reg in AllRegions) {
				if (reg.canBeActivated) {
					activeRegs.Add(reg);
				}
			}
			//now perform something like "LoadingFinished" method - resetting the regions hierarchy etc				
			try {
				Logger.WriteDebug("Resolving reactivating of static regions");
				ResolveParents();

				ResolveRegionsHierarchy();

				//we omit the "byName" dictionary resetting - this is not necessary here, it is filled already
				//if the regions name is changed (by setter property) then the byName dict is updated properly 

				//we also omit the "CheckAllRegions()" part here (if we realy need it, we will call it from the
				//code where the ActivateAll is being run)

				//and now finally reactivate the activable regions - spread the references to their rectangles to the map sectors :)
				InitRectanglesToMaps(activeRegs); //use only the regions from "activeRegs" - those that can be activated
			} catch (FatalException) {
				throw;
			} catch (Exception e) {
				Logger.WriteCritical("Regions reloading failed!", e);
				//ClearAll(); dont ClearAll it here - this would delete all regions !! (rather we will allow the user to fix his errors)
			}
			foreach (StaticRegion reg in activeRegs) {
				reg.inactivated = false; //activate the activated regions
			}
		}

		[Summary("Take the regions from the given list and set their rectangles to every mapplane")]
		private static void InitRectanglesToMaps(IEnumerable<StaticRegion> regionList) {
			List<StaticRegion>[] regionsByMapplane = new List<StaticRegion>[0x100];
			foreach (StaticRegion region in regionList) {
				List<StaticRegion> list = regionsByMapplane[region.Mapplane];
				if (list == null) {
					list = new List<StaticRegion>();
					regionsByMapplane[region.Mapplane] = list;
				}
				if (!region.IsWorldRegion) { //we dont want the world region in sectors...
					list.Add(region);
				}
			}
			for (int i = 0, n = regionsByMapplane.Length; i < n; i++) {
				List<StaticRegion> list = regionsByMapplane[i];
				if (list != null) {
					Map map = Map.GetMap((byte) i);
					map.ActivateRegions(list);
				}
			}
		}

		[Summary("Check if all regions (except for the world region) have parents set")]
		private static void ResolveParents() {
			foreach (StaticRegion region in AllRegions) {
				//(pri opakovanem reloadu - napr. po editaci regionu uz je worldregion nasetovan
				//a tudiz by tento kus kodu skoncil tou vyjimkou =>uprava pred vyjimkou
				if (region.Parent == null) {
					if (worldRegion == voidRegion) {
						worldRegion = region; //world region jeste neni nastaven - prave jsme ho nasli
					} else {
						//world region je nastaven a ten co zkoumame nema parenta - neni to naohdou worldregion?
						if (!region.IsWorldRegion) {
							//neni a nema parenta -> chyba!
							throw new SEException("Parent missing for the region " + LogStr.Ident(region.Defname));
						}
					}
				}
			}
			//all was OK, but we need to have also the world region!
			if (worldRegion == voidRegion) {
				throw new SEException("No world region defined.");
			}
		}

		[Summary("Itearate through all regions and set their hierarchy indices")]
		private static void ResolveRegionsHierarchy() {
			LinkedList<StaticRegion> tempList = new LinkedList<StaticRegion>(AllRegions);//copy list of all regions
			int lastCount = -1;
			while (tempList.Count > 0) {
				if (lastCount == tempList.Count) {
					//this will probably never happen
					throw new SEException("Region hierarchy not completely resolvable.");
				}
				lastCount = tempList.Count;
				StaticRegion r = tempList.Last.Value;
				r.SetHierarchyIndex(tempList);
			}
		}
		#endregion

		#region Name/defname labouring methods
		public static StaticRegion Get(string nameOrDefName) {
			StaticRegion retVal;
			if (!byDefname.TryGetValue(nameOrDefName, out retVal)) {
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

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists"), Summary("Searches through all regions and returns the list of StaticRegions thats name contains the " +
				"criteria string")]
		public static List<StaticRegion> FindByString(string criteria) {
			List<StaticRegion> regList = new List<StaticRegion>();
			foreach (StaticRegion reg in AllRegions) {
				if (string.IsNullOrEmpty(criteria)) {
					regList.Add(reg);//bereme vse
				} else if (reg.Name.ToUpper(System.Globalization.CultureInfo.InvariantCulture).Contains(
					criteria.ToUpper(System.Globalization.CultureInfo.InvariantCulture))) {
					regList.Add(reg);//jinak jen v pripade ze kriterium se vyskytuje v nazvu regionu
				}
			}
			return regList;
		}
		#endregion

		#region Other regions mutual positions checks
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

		private bool ContainsRectangle(ImmutableRectangle rect) {
			return (IsWorldRegion || (ContainsPoint(rect.MinX, rect.MinY)//left upper
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
			for (int i = 0, n = rectangles.Count; i < n; i++) {
				ImmutableRectangle rect = rectangles[i];
				if (!other.ContainsRectangle(rect)) {
					Logger.WriteWarning("Rectangle " + LogStr.Ident(rect) + " of region " + LogStr.Ident(Defname) + " should be contained within region " + LogStr.Ident(other.Defname) + ", but is not.");
					retState = false; //problem
				}
			}
			return retState;
		}

		private bool CheckHasNoRectanglesIn(StaticRegion other) {
			bool retState = true;
			for (int i = 0, n = rectangles.Count; i < n; i++) {
				ImmutableRectangle rect = rectangles[i];
				if (other.ContainsRectanglePartly(rect)) {
					Logger.WriteWarning("Rectangle " + LogStr.Ident(rect) + " of region " + LogStr.Ident(Defname) + " overlaps with " + LogStr.Ident(other.Defname) + ", but should not.");
					retState = false; //problem
				}
			}
			return retState;
		}

		public static void CheckAllRegions() {
			int i = 0, n = byDefname.Count;
			int countPerCent = n / 200;
			foreach (StaticRegion region in byDefname.Values) {
				if ((i % countPerCent) == 0) {
					Logger.SetTitle("Checking regions: " + ((i * 100) / n) + " %");
				}
				region.CheckConflictsAndWarn();
				i++;
			}
			Logger.SetTitle("");
		}

		private bool CheckConflictsAndWarn() {
			bool retState = true;
			if (Parent != null) {
				if (!CheckHasAllRectanglesIn((StaticRegion) Parent)) {
					retState = false; //problem
				}
			}
			foreach (StaticRegion other in byDefname.Values) {
				//Console.WriteLine("CheckConflictsAndWarn for "+this+" and "+other);
				if ((other != this) && this.HasSameMapplane(other)) {
					if (other.HierarchyIndex == this.HierarchyIndex) {
						//Console.WriteLine("CheckConflictsAndWarn in progress");
						if (!this.CheckHasNoRectanglesIn(other)) {
							retState = false; //problem
						}
					}
				}
			}
			return retState;
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists"), Summary("Looks through the list of all regions and check if the region is not a chidlren of the specified one")]
		public static List<StaticRegion> FindRegionsChildren(StaticRegion reg) {
			//first search the static ones (houses, stalls etc.)
			List<StaticRegion> retList = new List<StaticRegion>();
			foreach (StaticRegion stReg in AllRegions) {
				if (stReg.Parent == reg) {
					retList.Add(stReg); //parent is the one we are searching its choldren
				}
			}
			return retList;
		}
		#endregion

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter"), Summary("Take the list of rectangles and make an array of RegionRectangles of it")]
		public bool SetRectangles<T>(IList<T> list) where T : AbstractRectangle {
			bool result = true;
			RegionRectangle[] newArr = new RegionRectangle[list.Count];
			for (int i = 0; i < list.Count; i++) {
				//take the start/end point from the IRectangle and create a new RegionRectangle
				newArr[i] = new RegionRectangle(list[i], this);
			}
			//now the checking phase!
			//IList<RegionRectangle> oldRects = rectangles; //save
			StaticRegion.InactivateAll(); //unload regions - it 'locks' them for every usage except for rectangles operations
			this.canBeActivated = false;
			try {
				rectangles = newArr; //switch the rectangles			
				if (!this.CheckConflictsAndWarn()) { //check the edited region for possible problems
					//rectangles = oldRects; //return the previous set of rectangles
					result = false; //some problem
				}
				this.canBeActivated = true; //if we are here, everythin went fine or with simple warnings, which does not cause the fatal problem -)
			} finally {
				StaticRegion.ActivateAll();//all OK
			}
			return result;
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "name"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter"), Summary("Initializes newly created region - set the name, home position and list of rectangles")]
		public bool InitializeNewRegion<T>(string name, Point4D home, IList<T> rects) where T : AbstractRectangle {
			bool retval;
			this.Name = name; //nove jmeno, a zaroven ho to ulozi do prislusneho seznamu
			//jeste nez nasetujeme rectangly (coz vyvola zaroven i celou kontrolu vsech regionu)
			//tak musime zvlast vyresit zarazeni do hierarchie
			StaticRegion.ResolveRegionsHierarchy();//to se resi jen na urovni parentu (bez rectanglu)
			retval = SetRectangles(rects); //nastavit rect - ulozi se
			P = home; //homepos muzeme az kdyz mame region i s rectangly 

			return retval; //jak to dopadlo
		}

		public override string Name {
			get {
				return name;
			}
			set {
				ThrowIfDeleted();
				byName.Remove(name);
				name = String.Intern(value);
				if (this != voidRegion) {
					byName[value] = this;
				}
			}
		}

		public override string Defname {
			get {
				return base.Defname;
			}
			protected set {
				//zkontrolujeme, jestli stejny defname uz neexistuje... (pro jistotu)
				if (byDefname.ContainsKey(value)) {
					throw new OverrideNotAllowedException("Region with defname " + LogStr.Ident(value) + " already exists.");
				} 
				this.ThrowIfDeleted();
				value = String.Intern(value);
				base.Defname = value;
				if (this != voidRegion) {
					byDefname[value] = this;
				}
			}
		}

		public override bool IsDeleted {
			get {
				return isDeleted;
			}
		}

		[Summary("Vezme region. vsechny jeho deti, napoji deti na parenta a sam sebe odstrani")]
		public override void Delete() {
			if (this == worldRegion) { //world region nesmime smazat
				throw new SEException("Attempted to delete the 'world region'");
			} else if (this == voidRegion) { //a void taky ne...
				throw new SEException("Attempted to delete the 'void region'");
			}
			//najdeme deti a prepojime je na parenta
			List<StaticRegion> children = FindRegionsChildren(this);
			StaticRegion.InactivateAll(); //unload regions - it 'locks' them for every usage except for rectangles operations			
			try {
				foreach (StaticRegion child in children) {
					child.Parent = this.Parent; //reset parents!
					if (child is StaticRegion) {
						//if child is StaticRegion), make it activable - we dont care for DynamicRegions etc				
						((StaticRegion) child).canBeActivated = true;
					}
				}
				byName.Remove(this.name); //remove it from the byNames dict
				byDefname.Remove(this.Defname);//and from the byDefnames dict this means that the region is now completely removed
			} finally {
				StaticRegion.ActivateAll();//all OK
			}
			this.inactivated = true;
			this.isDeleted = true;
			base.Delete(); //call other delete processing (such as deleting plugins, timers etc)
		}
	}
}
