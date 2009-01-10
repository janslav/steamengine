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
using SteamEngine;
using SteamEngine.Common;
using SteamEngine.Timers;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {
	
	[Dialogs.ViewableClass]
	[Summary("Sector-defining class used in various scripts")]
	public class ScriptSector {
		[Remark("Dictionary mapping the computed Point4D to the ScriptSector "+
			    "we use computed X and Y (byte shift) and the normal M for determining the ScriptSector ")]
		internal static Dictionary<Point4D, ScriptSector> scriptSectors = new Dictionary<Point4D, ScriptSector>();

		[Remark("Power of 2 determining the size of the sector i.e. size 5 means a rectangle 32*32 fields.")]
		private const int scriptSectorSize = 5;

		[Remark("Time in seconds for how long we allow all information to remain in the ScriptSector "+
				"(if not refreshed) - e.g. player's TrackPoints etc.")]
		private static int cleaningTime = 180;

		[Summary("The dictionary containing all characters that passed through this sector. Everyone leaves an information "+
				"about the fields he stepped onto. For character Tracking purposes.")]
		private Dictionary<Character, Dictionary<Point2D, TrackPoint>> charsPassing = new Dictionary<Character, Dictionary<Point2D, TrackPoint>>();

		private ScriptSectorTimer sectorTimer;
		
		[Remark("Computed Point4D determining a set of map fields that belong to this sector - "+
				" see the GetScriptSector method")]
		private Point4D sectorIdentifier;

		public ScriptSector(Point4D sectorIdentifier) {
			this.sectorIdentifier = sectorIdentifier;
			sectorTimer = new ScriptSectorTimer(this);
			sectorTimer.DueInSeconds = cleaningTime;//set the first timeout for checking
			sectorTimer.PeriodInSeconds = cleaningTime;//set the period for periodic checking
		}


		[Remark("Perform all check necesarry to do on timeout")]
		internal void CheckOnTimeout() {
			//check and clean all old TrackPoints left by passing characters
			TimeSpan boundaryTime = Globals.TimeAsSpan - TimeSpan.FromSeconds(cleaningTime);
			
			List<Character> charsToRemove = new List<Character>();
			foreach(Character oneChar in charsPassing.Keys) {
				Dictionary<Point2D, TrackPoint> charsPath = charsPassing[oneChar];

				List<Point2D> pointsToRemove = new List<Point2D>();
				foreach (TrackPoint tPoint in charsPath.Values) {
					if (tPoint.LastStepTime <= boundaryTime) {
						pointsToRemove.Add(tPoint.Location); //we will remove the point later
					}
				}
				foreach (Point2D pointToRemove in pointsToRemove) {//now remove all old TrackPoints
					charsPath.Remove(pointToRemove);
				}

				if (charsPath.Count == 0) {
					charsToRemove.Add(oneChar); //we will remove the char later
				}
			}
			foreach (Character charToRemove in charsToRemove) {
				charsPassing.Remove(charToRemove);
			}

			//check if the sector contains any information, otherwise remove it from the global list
			if (charsPassing.Count == 0) {
				this.Remove();
			} 
		}

		private void Remove() {
			//delete the timer
			this.CleaningTimer.Delete();
			//and remove the sector from the overall dictionary
			scriptSectors.Remove(this.sectorIdentifier);
		}

		[Summary("For the given map Point4D compute and return the corresponding ScriptSector for further purposes")]
		public static ScriptSector GetScriptSector(Point4D forPoint) {
			Point4D determiningPoint = new Point4D((ushort)(forPoint.x >> scriptSectorSize), (ushort)(forPoint.y >> scriptSectorSize), 0, forPoint.m);
			ScriptSector retSector;
			if (!scriptSectors.TryGetValue(determiningPoint, out retSector)) {
				retSector = new ScriptSector(determiningPoint);//create new
				scriptSectors.Add(determiningPoint, retSector); //and store it in the dictionary
			}
			return retSector;
		}

		[Summary("Get all ScriptSectors that intersect with the given rectangle, then check all "+
			"characters contained inside if they also belong to the rectangle, check if the "+
			"character in the rect. is of the desired type and if its footsteps are not too old. "+
			"Return the list of found characters.")]
		public static List<Character> GetCharactersInRectangle(MutableRectangle rect, CharacterTypes charType, TimeSpan maxAge) {
			List<Character> retChars = new List<Character>();
			List<ScriptSector> intersectingSectors = GetScriptSectorsInRectangle(rect);
			TimeSpan now = Globals.TimeAsSpan; //actual server time
			foreach (ScriptSector sSec in intersectingSectors) {
				foreach (Character candidate in sSec.CharsPassing.Keys) {
					//check if the character is of the desired type (first filter)
					switch (charType) {
						case CharacterTypes.All:
							break; //always OK
						case CharacterTypes.Animals:
							if (!candidate.IsAnimal) {
								continue;
							}
							break;
						case CharacterTypes.Monsters:
							if (!candidate.IsMonster) {
								continue;
							}
							break;
						case CharacterTypes.Players:
							if (!(candidate is Player)) {
								continue;
							}
							break;
						case CharacterTypes.NPCs:
							if (!candidate.IsHuman || (candidate is Player)) { 
								continue;
							}
							break;
					}
					//now get the characters' TrackPoints and check if they belong go the rectangle
					//and that they are not too old
					foreach(TrackPoint passingPoint in sSec.CharsPassing[candidate].Values) {
						if((now - passingPoint.LastStepTime) <= maxAge) {//the footstep is not too old
							if(rect.Contains(passingPoint.Location)) {//footstep lies in the rectangle
								if (!retChars.Contains(candidate)) {//add him to the list only if he is not yet inside... (e.g. for more than 1 sector this can happen...)
									retChars.Add(candidate);
								}
								break;//one point is enough, this character can be tracked
							}
						}
					}
				}
			}
			return retChars;
		}

		[Summary("Method stolen from the Map class. Find all ScriptSectors that intersect the given "+
				"ImmutableRectangle (can be ScriptRectangle etc)")]
		public static List<ScriptSector> GetScriptSectorsInRectangle(MutableRectangle rectangle) {
			List<ScriptSector> retSectors = new List<ScriptSector>();
			ushort startX = rectangle.MinX;
			ushort startY = rectangle.MinY;
			ushort endX = rectangle.MaxX;
			ushort endY = rectangle.MaxY;

			//get first and last computed ScriptSector point for the given rectangle
			//(i.e. the sectors where the top left and bottom right rectangle points lies)
			Point2D ssPointStart = new Point2D((ushort)(Math.Min(startX, endX) >> scriptSectorSize), (ushort)(Math.Min(startY, endY) >> scriptSectorSize));
			Point2D ssPointEnd = new Point2D((ushort)(Math.Max(startX, endX) >> scriptSectorSize), (ushort)(Math.Max(startY, endY) >> scriptSectorSize));

			ScriptSector oneSector;
			//check all computed sector identifiers if some ScriptSector exists for them and if so, return it
			for (ushort sx = ssPointStart.x; sx <= ssPointEnd.x; sx++) {
				for (ushort sy = ssPointStart.y; sy <= ssPointEnd.y; sy++) {
					if (scriptSectors.TryGetValue(new Point4D(sx, sy, 0, rectangle.Map), out oneSector)) {
						retSectors.Add(oneSector);
					}	
				}
			}
			return retSectors;
		}

		[Summary("For the given character, get the set of all his footsteps belonging to the given rectangle "+
				"and which are not older than specified")]
		public static LinkedList<TrackPoint> GetCharsPath(Character whose, MutableRectangle rect, TimeSpan maxAge) {
			List<TrackPoint> footsteps = new List<TrackPoint>();
			TimeSpan allowedAge = Globals.TimeAsSpan - maxAge;
			List<ScriptSector> sectorsInRect = GetScriptSectorsInRectangle(rect);//get only relevant ScriptSectors
			foreach (ScriptSector relevantSec in sectorsInRect) {
				Dictionary<Point2D, TrackPoint> charPoints = relevantSec.CharsPassing[whose];
				foreach (TrackPoint onePoint in charPoints.Values) {
					if (rect.Contains(onePoint.Location)) {//the point is in the rectangle
						if (onePoint.LastStepTime >= allowedAge) { //the footprint is not too old
							footsteps.Add(onePoint);
						}
					}
				}
			}
			//sort the list by footsteps' age (first - the oldest, last - the newest footstep)
			footsteps.Sort(delegate(TrackPoint a, TrackPoint b) {
								return a.LastStepTime.CompareTo(b.LastStepTime);
							}); 
			return new LinkedList<TrackPoint>(footsteps);
		}

		[Remark("Used especially when 'resizing' the sectors - everything needs to be recomputed")]
		internal static void Reset() {
			//computed sectors are no longer available
			foreach (ScriptSector sec in scriptSectors.Values) {
				sec.sectorTimer.Delete();//remove all timers
			}
			scriptSectors.Clear();
		}

		[Dialogs.InfoField("Sector size")]
		public static int ScriptSectorSize {
			get {
				return scriptSectorSize;
			}
		}

		[Dialogs.InfoField("Cleaning period [s]")]
		public static int CleaningPeriod {
			get {
				return cleaningTime;
			}
			set {
				cleaningTime = value;
			}
		}

		public Point2D Identifier {
			get {
				return sectorIdentifier;
			}
		}

		public ScriptSectorTimer CleaningTimer {
			get {
				return sectorTimer;
			}
		}

		public Dictionary<Character, Dictionary<Point2D, TrackPoint>> CharsPassing {
			get {
				return charsPassing;
			}
		}
	}

	public class ScriptSectorTimer : Timer {
		ScriptSector ownerSector;

		internal ScriptSectorTimer(ScriptSector ownerSector) {
			this.ownerSector = ownerSector;
		}

		protected override void OnTimeout() {
			if (ownerSector != null) {
				ownerSector.CheckOnTimeout();
			}
		}
	}

	[Summary("Simple rectangle used e.g. in tracking to mark some area around the tracking player")]
	public class ScriptRectangle : ImmutableRectangle {
		private int range;

		public ScriptRectangle(Point2D center, int range)
			: base(center, (ushort) range) {
			this.range = range;
		}

		public int Range {
			get {
				return range;
			}
		}
	}
}