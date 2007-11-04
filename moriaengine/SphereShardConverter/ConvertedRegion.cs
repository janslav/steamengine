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
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Globalization;
using SteamEngine.Common;
using System.Configuration;
using SteamEngine.Regions;
	
namespace SteamEngine.Converter {

	public class ConvertedRegion : ConvertedDef {
		private static Dictionary<string, ConvertedRegion> regionsByDefname = new Dictionary<string, ConvertedRegion>(StringComparer.OrdinalIgnoreCase);
		private static List<ConvertedRegion> allRegions = new List<ConvertedRegion>();
		private static ArrayList temp = new ArrayList();
		
		private List<Rectangle2D> rectangles = new List<Rectangle2D>();
		private Point2D[] points;//corners of the rectangles. its not too accurate, but who cares... :)
		private byte mapplane;
		private int hierarchyIndex = -1;
		private ConvertedRegion[] parents;

		private bool mapplaneSet = false;
		private PropsLine mapplaneLine;
	


		private static LineImplTask[] firstStageImpl = new LineImplTask[] {
//				new LineImplTask("event", new LineImpl(WriteAsTG)), 
//				new LineImplTask("events", new LineImpl(WriteAsTG)), 
//				new LineImplTask("tevent", new LineImpl(WriteAsTG)), 
//				new LineImplTask("tevents", new LineImpl(WriteAsTG)), 
//				new LineImplTask("resources", new LineImpl(WriteAsTG)), 

				new LineImplTask("p", new LineImpl(ParseP)), 
				new LineImplTask("rect", new LineImpl(ParseRect)), 
				new LineImplTask("m", new LineImpl(ParseMapplane)), 
				new LineImplTask("mapplane", new LineImpl(ParseMapplane)), 

				new LineImplTask("flags", new LineImpl(ParseFlags)), 
				new LineImplTask("flag_announce", new LineImpl(ParseFlags)), 
				new LineImplTask("flag_antimagic_all", new LineImpl(ParseFlags)), 
				new LineImplTask("flag_antimagic_damage", new LineImpl(ParseFlags)), 
				new LineImplTask("flag_antimagic_gate", new LineImpl(ParseFlags)), 
				new LineImplTask("flag_antimagic_recallin", new LineImpl(ParseFlags)), 
				new LineImplTask("flag_antimagic_recallout", new LineImpl(ParseFlags)), 
				new LineImplTask("flag_antimagic_teleport", new LineImpl(ParseFlags)), 
				new LineImplTask("flag_arena", new LineImpl(ParseFlags)), 
				new LineImplTask("flag_guarded", new LineImpl(ParseFlags)), 
				new LineImplTask("flag_instalogout", new LineImpl(ParseFlags)), 
				new LineImplTask("flag_nobuilding", new LineImpl(ParseFlags)), 
				new LineImplTask("flag_nobuilding", new LineImpl(ParseFlags)), 
				new LineImplTask("flag_nodecay", new LineImpl(ParseFlags)), 
				new LineImplTask("flag_nopvp", new LineImpl(ParseFlags)), 
				new LineImplTask("flag_roof", new LineImpl(ParseFlags)), 
				new LineImplTask("flag_safe", new LineImpl(ParseFlags)), 
				new LineImplTask("flag_ship", new LineImpl(ParseFlags)), 
				new LineImplTask("flag_underground", new LineImpl(ParseFlags)), 
				new LineImplTask("flagsafe", new LineImpl(ParseFlags)), 
				new LineImplTask("guarded", new LineImpl(ParseFlags)), 
				new LineImplTask("nopvp", new LineImpl(ParseFlags)), 
				new LineImplTask("nodecay", new LineImpl(ParseFlags)), 
				new LineImplTask("nobuild", new LineImpl(ParseFlags)), 
				new LineImplTask("underground", new LineImpl(ParseFlags)), 
		};


		static Regex nonCharacterSplitRE = new Regex(@"[^\w]+", RegexOptions.Compiled);

		public ConvertedRegion(PropsSection input) : base(input) {
			this.firstStageImplementations.Add(firstStageImpl);

			Set("createdat", HighPerformanceTimer.TickCount.ToString(), "");

			string name = input.headerName;
			Set("Name", "\""+name+"\"", "");
			if (string.Compare(name, "%servname%", true) == 0) {
				//headerType = "WorldRegion";
				hierarchyIndex = 0;
			//} else {
				//headerType = "Region";
			}
			headerType = "Region";
			//todo: make this strip all non-ascii characters
			string[] splitted = nonCharacterSplitRE.Split(name);
			splitted[0] = "a_"+splitted[0];
			name = string.Join("_", splitted);//we make "a_local_mine" out of "local mine"
			string defname = name;
			int toAdd = 2;
			while (regionsByDefname.ContainsKey(defname)) {
				defname = name+"_"+toAdd;
				toAdd++;
			}
			regionsByDefname[defname] = this;
			headerName = defname;
			allRegions.Add(this);

			PropsLine defnameLine = input.TryPopPropsLine("defname");
			if (defnameLine != null) {
				if (string.Compare(defnameLine.value, "a_world", true) == 0) {
					//headerType = "Worldregion";
					hierarchyIndex = 0;
				}
				regionsByDefname.Remove(headerName);
				headerName = defnameLine.value;
				regionsByDefname[defnameLine.value] = this;//this could overwrite something, but that is not our fault I think :)
			}
		}

		private static string ParseRect(ConvertedDef def, PropsLine line) {
			Match m = Region.rectRE.Match(line.value);
			if (m.Success) {
				GroupCollection gc = m.Groups;
				//Console.WriteLine("args: "+args);
				//Console.WriteLine("parsed as: {0}, {1}, {2}, {3}", gc["x1"], gc["y1"], gc["x2"], gc["y2"]);
				ushort x1 = TagMath.ParseUInt16(gc["x1"].Value);
				ushort y1 = TagMath.ParseUInt16(gc["y1"].Value);
				ushort x2 = TagMath.ParseUInt16(gc["x2"].Value);
				ushort y2 = TagMath.ParseUInt16(gc["y2"].Value);
				ushort minX;
				ushort maxX;
				ushort minY;
				ushort maxY;
				if (x1 < x2) {
					minX = x1;
					maxX = x2;
				} else {
					minX = x2;
					maxX = x1;
				}
				if (y1 < y2) {
					minY = y1;
					maxY = y2;
				} else {
					minY = y2;
					maxY = y1;
				}
				maxX--;maxY--; //this is because sphere has weird system of rectangle coordinates
				string retVal = string.Format("{0},{1},{2},{3}", minX, minY, maxX, maxY);
				def.Set("Rect", retVal, line.comment);
				Point2D startpoint = new Point2D(minX, minY);
				Point2D endpoint = new Point2D(maxX, maxY);
				((ConvertedRegion) def).rectangles.Add(new Rectangle2D(startpoint, endpoint));
				return retVal;
			} else {
				def.Warning(line.line, "Unrecognized Rectangle format ('"+line.value+"')");
			}
			return "";
		}

		private static string ParseMapplane(ConvertedDef def, PropsLine line) {
			ConvertedRegion r = (ConvertedRegion) def;
			r.mapplane = TagMath.ParseByte(line.value);
			r.mapplaneSet = true;
			r.mapplaneLine = line;
			return "";
		}

		private static SteamEngine.CompiledScripts.Point4DSaveImplementor pImplementor = new SteamEngine.CompiledScripts.Point4DSaveImplementor();

		private static string ParseP(ConvertedDef def, PropsLine line) {
			ConvertedRegion r = (ConvertedRegion) def;
			Point4D p = Point4D.Parse(line.value);
			if (!r.mapplaneSet) {
				r.mapplane = p.M;
			}
			string retVal = pImplementor.Save(p);
			def.Set("Spawnpoint", retVal, line.comment);
			return retVal;
		}

		private static string ParseFlags(ConvertedDef def, PropsLine line) {
			string name = line.name;
			switch (line.name.ToLower()) {
				case "flagsafe":
					name = "Flag_safe";
					break;
				case "guarded":
					name = "flag_guarded";
					break;
				case "nopvp":
					name = "Flag_nopvp";
					break;
				case "nodecay":
					name = "Flag_nodecay";
					break;
				case "nobuild":
					name = "Flag_nobuild";
					break;
				case "underground":
					name = "Flag_underground";
					break;
			}

			int value = TagMath.ParseInt32(line.value);
			if (value != 0) {//it is flagged region
				//if (def.headerType.StartsWith("World")) {
				//    def.headerType = "WorldFlaggedRegion";
				//} else {
					def.headerType = "FlaggedRegion";
				//}
				def.Set(name, line.value, line.comment);
				return line.value;
			}
			return "";
		}

		public override void SecondStage() {
			int rectanglesCount = rectangles.Count;
			points = new Point2D[rectanglesCount*4];
			for (int i = 0; i < rectanglesCount; i++) {
				Rectangle2D rect = (Rectangle2D) rectangles[i];
				points[(i*4)+0] = rect.StartPoint; //left upper
				points[(i*4)+1] = new Point2D(rect.StartPoint.X, rect.EndPoint.Y);//left lower
				points[(i*4)+2] = rect.EndPoint;//right lower
				points[(i*4)+3] = new Point2D(rect.EndPoint.X, rect.StartPoint.Y);//right upper
			}
			
			temp.Clear();
			foreach (ConvertedRegion reg in allRegions) {
				if (HasSameMapplane(reg)) {
					int contained = reg.ContainsPoints(this.points);
					temp.Add(new DictionaryEntry(contained, reg));
				}
			}
			int tempCount = temp.Count;
			int highestResult = 0;
			int occurences = 0;
			for (int i = 0; i<tempCount; i++) {
				DictionaryEntry entry = (DictionaryEntry) temp[i];
				int result = (int) entry.Key;
				ConvertedRegion p = (ConvertedRegion) entry.Value;
				if ((this != p) && HasSameMapplane(p)) {
					if (result > highestResult) {
						occurences = 0;
						highestResult = result;
					}
					if (result == highestResult) {
						occurences++;
					}
				}
			}
			if ((occurences == 0) && (hierarchyIndex != 0)) {
				Warning(origData.headerLine, "Region "+this.headerName+" has no parents!");
			}
			parents = new ConvertedRegion[occurences];
			int index = 0;
			for (int i = 0; i<tempCount; i++) {
				DictionaryEntry entry = (DictionaryEntry) temp[i];
				int result = (int) entry.Key;
				ConvertedRegion p = (ConvertedRegion) entry.Value;
				if ((result == highestResult) && (p != this) && HasSameMapplane(p)) {
					parents[index] = p;
					index++;
				}
			}

			//Console.WriteLine("possible parents for "+this.headerName+" are "+Globals.ObjToString(parents));
		}
		
		public override void ThirdStage() {
			if (mapplaneLine != null) {
				Set("Mapplane", mapplane.ToString(), mapplaneLine.comment);
			} else {
				Set("Mapplane", mapplane.ToString(), "");
			}
		}
		
		private bool HasSameMapplane(ConvertedRegion reg) {
			if (this.mapplane == reg.mapplane) {
				return true;
			} else if (this.hierarchyIndex == 0) {
				return true;
			} else if (reg.hierarchyIndex == 0) {
				return true;
			}
			return false;
		}
		
		private int ContainsPoints(Point2D[] ps) {
			int counter = 0;
			for (int i = 0, n = ps.Length; i<n; i++) {
				Point2D p = ps[i];
				foreach (Rectangle2D rect in rectangles) {
					if (rect.Contains(p)) {
						counter ++;
						continue;
					}
				}
			}
			return counter;
		}
		
		private bool TryDefinitiveParent() {
			if (hierarchyIndex != -1) {
				return true;
			}
			int highestHierarchyIndex = -2;
			int highestHierarchyIndexAt = -1;
			for (int i = 0, n = parents.Length; i<n ; i++) {
				ConvertedRegion reg = parents[i];
				if (reg.hierarchyIndex == -1) {
					return false;
				} else if (reg.hierarchyIndex > highestHierarchyIndex) {
					highestHierarchyIndex = reg.hierarchyIndex;
					highestHierarchyIndexAt = i;
				}
			}
			ConvertedRegion definitiveParent = parents[highestHierarchyIndexAt];
			parents = new ConvertedRegion[] {definitiveParent};
			hierarchyIndex = definitiveParent.hierarchyIndex +1;
			Set("Parent", "("+definitiveParent.headerName+")", "calculated by Converter");
			//Console.WriteLine("Parent for "+this.headerName+"set to "+definitiveParent.headerName);
			return true;
		}
		
		//internal static void ResolveRegionsHierarchy() {
		public static void SecondStageFinished() {
			temp = new ArrayList(allRegions);//copy list of all regions
			int lastCount = -1;
			while (temp.Count > 0) {
				if (lastCount == temp.Count) {
					//this will probably never happen
					Logger.WriteError("Region hierarchy not completely resolvable - No regions converted!");
					Logger.WriteInfo(ConverterMain.AdditionalConverterMessages, "These are the unresolved ones:");
					foreach (ConvertedRegion reg in temp) {
						reg.Info(reg.origData.headerLine, reg+", possible parents: "+Tools.ObjToString(reg.parents));
					}
					
					foreach (ConvertedRegion reg in allRegions) {
						reg.DontDump();
					}
					return;
				}
				lastCount = temp.Count;
				for (int i = 0; i<temp.Count;) {
					ConvertedRegion r = (ConvertedRegion) temp[i];
					if (r.TryDefinitiveParent()) {
						temp.RemoveAt(i);
					} else {
						i++;
					}
				}
			}
		}
//		
		public override string ToString() {
			return "ConvertedRegion "+headerName+"("+hierarchyIndex+")";
		}
	}
}