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
using SteamEngine.Communication;
using SteamEngine.Networking;

namespace SteamEngine.CompiledScripts {
	
	[Dialogs.ViewableClass]
	[Summary("Sector-defining class used in various scripts")]
	public class ScriptSector {
		[Remark("Dictionary mapping the computed SectorKey to the ScriptSector " +
			    "we use computed X and Y (byte shift) and the normal M for determining the ScriptSector ")]
		internal static Dictionary<SectorKey, ScriptSector> scriptSectors = new Dictionary<SectorKey, ScriptSector>();

		[Remark("Power of 2 determining the size of the sector i.e. size 5 means a rectangle 32*32 fields.")]
		private const int scriptSectorSize = 5;

		[Remark("Time in seconds for how long we allow all information to remain in the ScriptSector "+
				"(if not refreshed) - e.g. player's TrackPoints etc.")]
		private static int cleaningTime = 180;

		[Summary("The dictionary containing all characters that passed through this sector. Everyone leaves an information "+
				"about the fields he stepped onto. For character Tracking purposes.")]
		//private Dictionary<Character, Dictionary<Point2D, TrackPoint>> charsPassing = new Dictionary<Character, Dictionary<Point2D, TrackPoint>>();
		private Dictionary<Character, Queue<TrackPoint>> charsPassing = new Dictionary<Character, Queue<TrackPoint>>();

		private ScriptSectorTimer sectorTimer;
		
		[Remark("Computed SEctorKey determining a set of map fields that belong to this sector - "+
				" see the GetScriptSector method")]
		private SectorKey sectorIdentifier;

		private ScriptSector(SectorKey sectorIdentifier) {
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
				Queue<TrackPoint> charsPath = charsPassing[oneChar];

				while (charsPath.Count > 0) {
					TrackPoint oneTP = charsPath.Peek();
					if (oneTP.LastStepTime <= boundaryTime) {//check the oldest Queue element
						charsPath.Dequeue();//too old -> remove it
						oneTP.Dispose();
					} else {
						break;
					}
				}
			
				if (charsPath.Count == 0) {//nothing is left in the queue
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
			SectorKey determiningPoint = new SectorKey((ushort) (forPoint.x >> scriptSectorSize), (ushort) (forPoint.y >> scriptSectorSize), forPoint.m);
			ScriptSector retSector;
			if (!scriptSectors.TryGetValue(determiningPoint, out retSector)) {
				retSector = new ScriptSector(determiningPoint);//create new
				scriptSectors.Add(determiningPoint, retSector); //and store it in the dictionary
			}
			return retSector;
		}

		[Summary("Get all ScriptSectors that intersect with the given rectangle (lying in the give mapplane), then check all "+
			"characters contained inside if they also belong to the rectangle, check if the "+
			"character in the rect. is of the desired type and if its footsteps are not too old. "+
			"Return the list of found characters.")]
		public static List<Character> GetCharactersInRectangle(AbstractRectangle rect, CharacterTypes charType, TimeSpan maxAge, byte mapplane) {
			List<Character> retChars = new List<Character>();
			List<ScriptSector> intersectingSectors = GetScriptSectorsInRectangle(rect, mapplane);
			TimeSpan now = Globals.TimeAsSpan; //actual server time
			foreach (ScriptSector sSec in intersectingSectors) {
				foreach (Character candidate in sSec.charsPassing.Keys) {
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
					foreach(TrackPoint passingPoint in sSec.charsPassing[candidate]) {
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
				"Rectangle (can be ScriptRectangle etc) lying in the given mapplane")]
		public static List<ScriptSector> GetScriptSectorsInRectangle(AbstractRectangle rectangle, byte mapplane) {
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
					if (scriptSectors.TryGetValue(new SectorKey(sx, sy, mapplane), out oneSector)) {
						retSectors.Add(oneSector);
					}	
				}
			}
			return retSectors;
		}

		[Summary("For the given character, get the set of all his footsteps belonging to the given rectangle in the given mapplane "+
				"and which are not older than specified")]
		public static List<TrackPoint> GetCharsPath(Character whose, AbstractRectangle rect, TimeSpan maxAge, byte mapplane) {
			List<TrackPoint> footsteps = new List<TrackPoint>();
			TimeSpan allowedAge = Globals.TimeAsSpan - maxAge;
			List<ScriptSector> sectorsInRect = GetScriptSectorsInRectangle(rect, mapplane);//get only relevant ScriptSectors
			foreach (ScriptSector relevantSec in sectorsInRect) {
				Queue<TrackPoint> charPoints;
				if (relevantSec.charsPassing.TryGetValue(whose, out charPoints)) {//only if the char is still loaded on sector (i.e. it was not yet cleaned)
					foreach (TrackPoint onePoint in charPoints) {
						if (rect.Contains(onePoint.Location)) {//the point is in the rectangle
							if (onePoint.LastStepTime >= allowedAge) { //the footprint is not too old
								footsteps.Add(onePoint);
							}
						}
					}
				}
			}
			//sort the list by footsteps' age (first - the oldest, last - the newest footstep)
			//(important for displaying as in the list there can exist more TPs for the same position so we need
			//the newest one to be displayed last (most fresh footstep)
			footsteps.Sort(delegate(TrackPoint a, TrackPoint b) {
								return a.LastStepTime.CompareTo(b.LastStepTime);
							}); 
			return footsteps;
		}

		[Summary("For the given player make a record of his actual position as a new tracking step")]
		internal static void AddTrackingStep(Player whose, byte direction) {
			//get actual sector
			Point4D hisPos = whose.P();
			ScriptSector hisSector = ScriptSector.GetScriptSector(hisPos);

			//check if we already have this any of char's trackpoints in this sector
			Queue<TrackPoint> hisTrackPoints;
			if (!hisSector.charsPassing.TryGetValue(whose, out hisTrackPoints)) {
				hisTrackPoints = new Queue<TrackPoint>();
				hisSector.charsPassing.Add(whose, hisTrackPoints);
			}

			//add the actually stepped point to the queue (no matter if we have stepped on it previously)
			TrackPoint hisActualPoint = new TrackPoint(hisPos, whose);
			hisActualPoint.LastStepTime = Globals.TimeAsSpan; //set the last step time on this position
			hisTrackPoints.Enqueue(hisActualPoint);

			switch ((Direction)direction) {
				case Direction.North://0
				case Direction.NorthEast://1
					hisActualPoint.Model = (ushort) GumpIDs.Footprint_North; //0x1e04
					break;
				case Direction.East://2
				case Direction.SouthEast://3
					hisActualPoint.Model = (ushort) GumpIDs.Footprint_East; //0x1e05
					break;
				case Direction.South://4
				case Direction.SouthWest://5
					hisActualPoint.Model = (ushort) GumpIDs.Footprint_South; //0x1e06
					break;
				case Direction.West://6
				case Direction.NorthWest://7
					hisActualPoint.Model = (ushort) GumpIDs.Footprint_West; //0x1e03
					break;
			}
			
			//check if we are being tracked and in this case, send the information about the new step made
			List<Character> tbList = (List<Character>) whose.GetTag(TrackingSkillDef.trackedByTK);
			if(tbList != null && tbList.Count > 0) {
				SendStepToTrackers(tbList, hisActualPoint);
			}
		}

		//send the information about the step to the people(trackers) who are tracking
		private static void SendStepToTrackers(List<Character> trackers, TrackPoint whichPoint) {
			PacketGroup pg = null;
			TrackingPlugin trackersPlugin = null;
			List<GameState> whoToSend = new List<GameState>();
			foreach (Character tracker in trackers) {
				GameState trackerState = tracker.GameState;
				if(trackerState != null) {
					trackersPlugin = (TrackingPlugin)tracker.GetPlugin(TrackingPlugin.trackingPluginKey);
					if (trackersPlugin.trackingRectangle.Contains(whichPoint.Location)) {//send the position only if it fits to the tracker's tracking area
						if (pg == null) {//if not yet prepared, prepare it now (only once!)
							pg = PacketGroup.AcquireMultiUsePG();
							//check if tp has its fake UID assigned and if not, gather one
							if (whichPoint.FakeUID == 0) {
								whichPoint.FakeUID = Thing.GetFakeItemUid();
							}
							pg.AcquirePacket<ObjectInfoOutPacket>()
								.PrepareFakeItem(whichPoint.FakeUID, whichPoint.Model, whichPoint.Location, 1, Direction.North, TrackingPlugin.BEST_COLOR);
						}
						trackersPlugin.footsteps.Add(whichPoint);//add the new point to the monitored list...
						whoToSend.Add(trackerState);
					}
				}
			}
			foreach (GameState oneState in whoToSend) {
				oneState.Conn.SendPacketGroup(pg);//if any GameState is in the list then the pg is not null
			}
			pg.Dispose();
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

		public SectorKey Identifier {
			get {
				return sectorIdentifier;
			}
		}

		public ScriptSectorTimer CleaningTimer {
			get {
				return sectorTimer;
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

	[Summary("Special immutable key for determining ScriptSectors. It is specified by computed X, Y (byteshifted position) "+
			"and the mapplane it lies in")]
	public struct SectorKey {
		private readonly ushort x, y;
		private readonly byte m;

		public SectorKey(ushort x, ushort y, byte m) {
			this.x = x;
			this.y = y;
			this.m = m;
		}

		public ushort X {
			get {
				return x;
			}
		}

		public ushort Y {
			get {
				return y;
			}
		}

		public byte M {
			get {
				return m;
			}
		}

		public override bool Equals(object obj) {
			if (obj is SectorKey) {
				SectorKey sk = (SectorKey) obj;
				return ((this.x == sk.x) && (this.y == sk.y) && (this.m == sk.m));
			}
			return false;
		}

		//stolen from PointXD
		public override int GetHashCode() {
			return ((37*17^x)^y)^m;
		}
	}
}