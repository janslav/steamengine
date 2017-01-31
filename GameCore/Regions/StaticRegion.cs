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
using System.Globalization;
using System.Text.RegularExpressions;
using SteamEngine.Common;
using SteamEngine.Parsing;
using SteamEngine.Persistence;

namespace SteamEngine.Regions {
	/// <summary>Class implementing saving/loading of regions, controlling unique defnames etc.</summary>
	public class StaticRegion : Region {
		internal static readonly StaticRegion voidRegion = new StaticRegion("", "void");
		private static StaticRegion worldRegion = voidRegion;

		private static Dictionary<string, StaticRegion> byName = new Dictionary<string, StaticRegion>(StringComparer.OrdinalIgnoreCase);
		private static Dictionary<string, StaticRegion> byDefname = new Dictionary<string, StaticRegion>(StringComparer.OrdinalIgnoreCase);
		private static int highestHierarchyIndex = -1;

		private string name = ""; //this is typically not unique, containing spaces etc.

		private bool isDeleted;

		/// <summary>Clearing of the lists of all regions</summary>
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
		{
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
			var defName = input.HeaderName;

			if (!byDefname.ContainsKey(defName)) {
				//byDefname[defName] = this;
				this.Defname = defName;
			} else {
				throw new OverrideNotAllowedException("Region " + LogStr.Ident(defName) + " loaded multiple times.");
			}

			this.LoadSectionLines(input);
			this.inactivated = false;
		}

		[SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
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

				foreach (var region in AllRegions) {
					region.SaveWithHeader(output);
				}

				Logger.WriteDebug("Saved " + byDefname.Count + " static regions.");
			}

			[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
			public void LoadingFinished() {
				if (highestHierarchyIndex != -1) {
					return;
				}
				try {
					Logger.WriteDebug("Resolving loaded static regions");

					ResolveParents();

					ResolveRegionsHierarchy();

					foreach (var region in AllRegions) {
						byName[region.Name] = region;
					}

					//and now the (cpu) intensive part :)  check if the declared hierarchy is right
					//it has no real effect, only showing warnings about overlapping regions
					//it's optional - part of the "resolveEverythingAtStart" option in .ini
					if (Globals.ResolveEverythingAtStart) {
						CheckAllRegions();
					}

					//and now finally activate the regions - spread the references to their rectangles to the map sectors :)
					InitRectanglesToMaps(AllRegions);//use list of all available regions
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

			[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
			public string GetReferenceLine(object value) {
				return "(" + ((Region) value).Defname + ")";
			}

			public Regex ReferenceLineRecognizer {
				get {
					return regionNameRE;
				}
			}

			[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
			public object Load(Match m) {
				return GetByDefname(m.Groups["value"].Value);
			}
		}

		[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
		public override void Save(SaveStream output) {
			if (!string.IsNullOrEmpty(this.name)) {
				output.WriteValue("name", this.name);
			}
			base.Save(output);
		}

		public override void LoadLine(string filename, int line, string valueName, string valueString) {
			switch (valueName) {
				case "name":
					this.name = string.Intern(ConvertTools.LoadSimpleQuotedString(valueString));
					break;
				default:
					base.LoadLine(filename, line, valueName, valueString);
					break;
			}
		}

		private int SetHierarchyIndex(ICollection<StaticRegion> tempList)
		{
			if (this.Parent == null)
			{
				if (this.IsWorldRegion) {
					this.HierarchyIndex = 0;
					tempList.Remove(this);
					return 0;
				}
				throw new SEException("Region " + this + " has no parent region set");
			}
			if (!this.HasSameMapplane(this.Parent)) {
				throw new SEException("Region " + this + " has as parent set " + this.Parent + ", which is on another mapplane.");
			}
			var parentIndex = ((StaticRegion) this.Parent).SetHierarchyIndex(tempList);
			this.HierarchyIndex = parentIndex + 1;
			highestHierarchyIndex = Math.Max(highestHierarchyIndex, this.HierarchyIndex);
			tempList.Remove(this);
			return this.HierarchyIndex;
		}

		[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods"), Save]
		public void SaveWithHeader(SaveStream output) {
			this.ThrowIfDeleted();
			output.WriteSection(this.GetType().Name, this.Defname);
			this.Save(output);
			output.WriteLine();
		}

		/// <summary>Useful when editing regions - we need to manipulate with their rectangles which can be done only in inactivated state</summary>
		private static void InactivateAll() {
			foreach (var map in Map.AllMaps) {
				map.InactivateRegions(false); //false - omit dynamic regions from clearing
			}
			foreach (var reg in AllRegions) {
				reg.inactivated = true; //inactivate
			}
		}

		/// <summary>
		/// Useful when editing regions - we need to manipulate with their rectangles which can be done only in unloaded state"
		/// called after manipulation is successfully done. 
		/// Activates only activable regions
		/// </summary>
		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		private static void ActivateAll() {
			var activeRegs = new List<StaticRegion>();
			foreach (var reg in AllRegions) {
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
			foreach (var reg in activeRegs) {
				reg.inactivated = false; //activate the activated regions
			}
		}

		/// <summary>Take the regions from the given list and set their rectangles to every mapplane</summary>
		private static void InitRectanglesToMaps(IEnumerable<StaticRegion> regionList) {
			var regionsByMapplane = new List<StaticRegion>[0x100];
			foreach (var region in regionList) {
				var list = regionsByMapplane[region.Mapplane];
				if (list == null) {
					list = new List<StaticRegion>();
					regionsByMapplane[region.Mapplane] = list;
				}
				if (!region.IsWorldRegion) { //we dont want the world region in sectors...
					list.Add(region);
				}
			}
			for (int i = 0, n = regionsByMapplane.Length; i < n; i++) {
				var list = regionsByMapplane[i];
				if (list != null) {
					var map = Map.GetMap((byte) i);
					map.ActivateRegions(list);
				}
			}
		}

		/// <summary>Check if all regions (except for the world region) have parents set</summary>
		private static void ResolveParents() {
			foreach (var region in AllRegions) {
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

		/// <summary>Itearate through all regions and set their hierarchy indices</summary>
		private static void ResolveRegionsHierarchy() {
			var tempList = new LinkedList<StaticRegion>(AllRegions);//copy list of all regions
			var lastCount = -1;
			while (tempList.Count > 0) {
				if (lastCount == tempList.Count) {
					//this will probably never happen
					throw new SEException("Region hierarchy not completely resolvable.");
				}
				lastCount = tempList.Count;
				var r = tempList.Last.Value;
				r.SetHierarchyIndex(tempList);
			}
		}
		#endregion

		#region Name/defname labouring methods
		public static StaticRegion GetByNameOrDefname(string nameOrDefName) {
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

		public static ICollection<StaticRegion> AllRegions {
			get {
				return byDefname.Values;
			}
		}

		/// <summary>
		/// Searches through all regions and returns the list of StaticRegions the name of which contains the 
		/// criteria string
		/// </summary>
		[SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists")]
		public static List<StaticRegion> FindByString(string criteria) {
			var regList = new List<StaticRegion>();
			foreach (var reg in AllRegions) {
				if (string.IsNullOrEmpty(criteria)) {
					regList.Add(reg);//bereme vse
				} else if (reg.Name.ToUpper(CultureInfo.InvariantCulture).Contains(
					criteria.ToUpper(CultureInfo.InvariantCulture))) {
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
			}
			if (this.IsWorldRegion) {
				return true;
			}
			if (reg.IsWorldRegion) {
				return true;
			}
			return false;
		}

		private bool ContainsRectangle(ImmutableRectangle rect) {
			return (this.IsWorldRegion || (this.ContainsPoint(rect.MinX, rect.MinY)//left upper
					&& this.ContainsPoint(rect.MinX, rect.MaxY) //left lower
					&& this.ContainsPoint(rect.MaxX, rect.MaxY) //right lower
					&& this.ContainsPoint(rect.MaxX, rect.MinY)));//right upper
		}

		private bool ContainsRectanglePartly(ImmutableRectangle rect) {
			return (this.IsWorldRegion || (this.ContainsPoint(rect.MinX, rect.MinY)//left upper
					|| this.ContainsPoint(rect.MinX, rect.MaxY) //left lower
					|| this.ContainsPoint(rect.MaxX, rect.MaxY) //right lower
					|| this.ContainsPoint(rect.MaxX, rect.MinY)));//right upper
		}

		private bool CheckHasAllRectanglesIn(StaticRegion other) {
			var retState = true;
			for (int i = 0, n = this.rectangles.Count; i < n; i++) {
				ImmutableRectangle rect = this.rectangles[i];
				if (!other.ContainsRectangle(rect)) {
					Logger.WriteWarning("Rectangle " + LogStr.Ident(rect) + " of region " + LogStr.Ident(this.Defname) + " should be contained within region " + LogStr.Ident(other.Defname) + ", but is not.");
					retState = false; //problem
				}
			}
			return retState;
		}

		private bool CheckHasNoRectanglesIn(StaticRegion other) {
			var retState = true;
			for (int i = 0, n = this.rectangles.Count; i < n; i++) {
				ImmutableRectangle rect = this.rectangles[i];
				if (other.ContainsRectanglePartly(rect)) {
					Logger.WriteWarning("Rectangle " + LogStr.Ident(rect) + " of region " + LogStr.Ident(this.Defname) + " overlaps with " + LogStr.Ident(other.Defname) + ", but should not.");
					retState = false; //problem
				}
			}
			return retState;
		}

		public static void CheckAllRegions() {
			int i = 0, n = byDefname.Count;
			var countPerCent = n / 200;
			foreach (var region in byDefname.Values) {
				if ((i % countPerCent) == 0) {
					Logger.SetTitle("Checking regions: " + ((i * 100) / n) + " %");
				}
				region.CheckConflictsAndWarn();
				i++;
			}
			Logger.SetTitle("");
		}

		private bool CheckConflictsAndWarn() {
			var retState = true;
			if (this.Parent != null) {
				if (!this.CheckHasAllRectanglesIn((StaticRegion) this.Parent)) {
					retState = false; //problem
				}
			}
			foreach (var other in byDefname.Values) {
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

		/// <summary>
		/// Returns a list of regions for which the specified one is parent
		/// </summary>
		[SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists")]
		public static List<StaticRegion> FindRegionsChildren(StaticRegion parent) {
			var retList = new List<StaticRegion>();
			foreach (var stReg in AllRegions) {
				if (stReg.Parent == parent) {
					retList.Add(stReg);
				}
			}
			return retList;
		}
		#endregion

		/// <summary>
		/// Set the specified rectangles as belonging to this region
		/// </summary>
		[SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter")]
		public bool SetRectangles<T>(IList<T> list) where T : AbstractRectangle {
			var result = true;
			var newArr = new RegionRectangle[list.Count];
			for (var i = 0; i < list.Count; i++) {
				//take the start/end point from the IRectangle and create a new RegionRectangle
				newArr[i] = new RegionRectangle(list[i], this);
			}
			//now the checking phase!
			//IList<RegionRectangle> oldRects = rectangles; //save
			InactivateAll(); //unload regions - it 'locks' them for every usage except for rectangles operations
			this.canBeActivated = false;
			try {
				this.rectangles = newArr; //switch the rectangles			
				if (!this.CheckConflictsAndWarn()) { //check the edited region for possible problems
					//rectangles = oldRects; //return the previous set of rectangles
					result = false; //some problem
				}
				this.canBeActivated = true; //if we are here, everythin went fine or with simple warnings, which does not cause the fatal problem -)
			} finally {
				ActivateAll();//all OK
			}
			return result;
		}

		/// <summary>
		/// Initializes a newly created region - set the name, home position and list of rectangles
		/// </summary>
		[SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "name"), SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter")]
		public bool InitializeNewRegion<T>(string name, Point4D home, IList<T> rects) where T : AbstractRectangle {
			bool retval;
			this.Name = name; //nove jmeno, a zaroven ho to ulozi do prislusneho seznamu
			//jeste nez nasetujeme rectangly (coz vyvola zaroven i celou kontrolu vsech regionu)
			//tak musime zvlast vyresit zarazeni do hierarchie
			ResolveRegionsHierarchy();//to se resi jen na urovni parentu (bez rectanglu)
			retval = this.SetRectangles(rects); //nastavit rect - ulozi se
			this.P = home; //homepos muzeme az kdyz mame region i s rectangly 

			return retval; //jak to dopadlo
		}

		public override string Name {
			get {
				return this.name;
			}
			set {
				this.ThrowIfDeleted();
				byName.Remove(this.name);
				this.name = string.Intern(value);
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
				value = string.Intern(value);
				base.Defname = value;
				if (this != voidRegion) {
					byDefname[value] = this;
				}
			}
		}

		public override bool IsDeleted {
			get {
				return this.isDeleted;
			}
		}

		/// <summary>Vezme region. vsechny jeho deti, napoji deti na parenta a sam sebe odstrani</summary>
		public override void Delete() {
			if (this == worldRegion) { //world region nesmime smazat
				throw new SEException("Attempted to delete the 'world region'");
			}
			if (this == voidRegion) { //a void taky ne...
				throw new SEException("Attempted to delete the 'void region'");
			}
			//najdeme deti a prepojime je na parenta
			var children = FindRegionsChildren(this);
			InactivateAll(); //unload regions - it 'locks' them for every usage except for rectangles operations			
			try {
				foreach (var child in children) {
					child.Parent = this.Parent; //reset parents!
					if (child is StaticRegion) {
						//if child is StaticRegion), make it activable - we dont care for DynamicRegions etc				
						child.canBeActivated = true;
					}
				}
				byName.Remove(this.name); //remove it from the byNames dict
				byDefname.Remove(this.Defname);//and from the byDefnames dict this means that the region is now completely removed
			} finally {
				ActivateAll();//all OK
			}
			this.inactivated = true;
			this.isDeleted = true;
			base.Delete(); //call other delete processing (such as deleting plugins, timers etc)
		}
	}
}
