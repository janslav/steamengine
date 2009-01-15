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
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using SteamEngine;
using SteamEngine.Common;
using SteamEngine.Regions;
using SteamEngine.Networking;
using SteamEngine.Communication;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {
	[ViewableClass]
	public class TrackingSkillDef : SkillDef {
		//tag key for the tag with the list of trackers to the tracked character
		internal static TagKey trackedByTK = TagKey.Get("_tracked_by_");

		[Summary("Maximal age [sec] of the footsteps to be tracked at skill 0")]
		private FieldValue minFootstepAge;
		[Summary("Maximal age [sec] of the footsteps to be tracked at skill 100")]
		private FieldValue maxFootstepAge;

		[Summary("Maximum characters to be recognized at skill 0")]
		private FieldValue minCharsToTrack;
		[Summary("Maximum characters to be recognized at skill 100")]
		private FieldValue maxCharsToTrack;

		[Summary("Max steps before tracking chance is recomputed at skill 0")]
		private FieldValue minSafeSteps;
		[Summary("Max steps before tracking chance is recomputed at skill 100")]
		private FieldValue maxSafeSteps;

		public TrackingSkillDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			minFootstepAge = InitField_Typed("minFootstepAge", 15, typeof(double));
			maxFootstepAge = InitField_Typed("maxFootstepAge", 120, typeof(double));
			minCharsToTrack = InitField_Typed("minCharsToTrack", 3, typeof(int));
			maxCharsToTrack = InitField_Typed("maxCharsToTrack", 20, typeof(int));
			minSafeSteps = InitField_Typed("minSafeSteps", 1, typeof(int));
			maxSafeSteps = InitField_Typed("maxSafeSteps", 10, typeof(int));
		}
		
		protected override bool On_Select(SkillSequenceArgs skillSeqArgs) {
			//todo: paralyzed state etc.
			if (!CheckPrerequisities(skillSeqArgs)) {
				return true;//finish now
			}
			Character self = skillSeqArgs.Self;
			self.ClilocSysMessage(1011350);//What do you wish to track?
			self.Dialog(self,SingletonScript<D_Tracking_Categories>.Instance, new DialogArgs(skillSeqArgs));
			return true; //stop it, other triggers will be run from the tracking dialog
		}

		protected override bool On_Start(SkillSequenceArgs skillSeqArgs) {
			return false; //continue to delay, then @stroke
		}

		protected override bool On_Stroke(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;

			if (!CheckPrerequisities(skillSeqArgs)) {
				return true;//stop
			}
			switch ((TrackingEnums) skillSeqArgs.Param2) {
				case TrackingEnums.Phase_Characters_Seek:
					skillSeqArgs.Success = this.CheckSuccess(self, Globals.dice.Next(700)); //normally check for success...
					break;
				case TrackingEnums.Phase_Character_Track: //we will try to display the chars path
					skillSeqArgs.Success = true;//select the character to display its footsteps, no need to check for success
					break;
			}

			return false; //continue to @success or @fail
		}

		protected override bool On_Success(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			MutableRectangle rect = GetTrackingArea(skillSeqArgs);
			TimeSpan maxAge = GetMaxFootstepsAge(skillSeqArgs);
			switch ((TrackingEnums) skillSeqArgs.Param2) {
				case TrackingEnums.Phase_Characters_Seek: //we will look for chars around
					CharacterTypes charType = (CharacterTypes) skillSeqArgs.Param1;
			
					List<Character> charsAround = ScriptSector.GetCharactersInRectangle(rect, charType, maxAge, self.M);

					//check if tracking is possible (with message)
					//i.e. too many chars or none at all
					if (CheckTrackImpossible(skillSeqArgs, charsAround, charType)) {
						return true;
					}

					//display a dialog with the found trackable characters
					self.ClilocSysMessage(1018093);//Select the one you would like to track.
					self.Dialog(self, SingletonScript<D_Tracking_Characters>.Instance, new DialogArgs(skillSeqArgs, charsAround));
					break;
				case TrackingEnums.Phase_Character_Track: //we will try to display the chars path
					Character charToTrack = (Character)skillSeqArgs.Param1;

					//get the list of characters visible footsteps
					List<TrackPoint> charsSteps = ScriptSector.GetCharsPath(charToTrack, rect, maxAge, self.M);

					//and forward the tracking management to the special plugin
					TrackingPlugin tpl = (TrackingPlugin) TrackingPlugin.defInstance.Create();
					tpl.trackingRectangle = rect; //for recomputing the rect when OnStep...
					tpl.maxFootstepAge = maxAge; //for refreshing the footsteps when OnStep...
					tpl.whoToTrack = charToTrack; //for refreshing the footsteps when OnStep...
					tpl.footsteps = charsSteps; //initial list of footsteps
					tpl.safeSteps = GetMaxSafeSteps(skillSeqArgs); //number of safe steps
					tpl.Timer = TrackingPlugin.refreshTimeout;//set the first timer
					self.AddPlugin(TrackingPlugin.trackingPluginKey, tpl);
					
					//to the tracked char's list add the actual tracker
					List<Character> tbList = (List<Character>) charToTrack.GetTag(TrackingSkillDef.trackedByTK);
					if (tbList == null) {
						tbList = new List<Character>();
						charToTrack.SetTag(TrackingSkillDef.trackedByTK, tbList);
					}
					if (!tbList.Contains(self)) {
						tbList.Add(self);
					}

					break;
			}
			return false;
		}

		protected override bool On_Fail(SkillSequenceArgs skillSeqArgs) {
			skillSeqArgs.Self.ClilocSysMessage(502989);//Tracking failed.
			return false;
		}

		protected override void On_Abort(SkillSequenceArgs skillSeqArgs) {
			skillSeqArgs.Self.SysMessage("Tracking aborted");
		}

		[Remark("Check if we are alive, have enuogh stamina etc.... Return false if the trigger above"+
				" should be cancelled or true if we can continue")]
		private bool CheckPrerequisities(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			if (!self.CheckAliveWithMessage()) {
				return false;//no message needed, it's been already sent in the called method
			}
			if (self.Stam <= self.MaxStam / 10) {
				self.ClilocSysMessage(502988);//You are too weary to make anything from the clues of nature.
				return false; //stop
			}			
			return true;
		}

		//check if there isn't too much or none chars to track, return true if impossible
		private bool CheckTrackImpossible(SkillSequenceArgs ssa, List<Character> charsAround, CharacterTypes charType) {
			Character self = ssa.Self;
			if (charsAround.Count == 0) {
				switch (charType) {
					case CharacterTypes.Animals:
						self.ClilocSysMessage(502991); //You see no evidence of animals in the area.
						return true;
					case CharacterTypes.Monsters:
						self.ClilocSysMessage(502993); //You see no evidence of creatures in the area.
						return true;
					case CharacterTypes.NPCs:
						self.ClilocSysMessage(502995); //You see no evidence of people in the area. 
						return true;
					case CharacterTypes.Players:
						self.ClilocSysMessage(502995); //You see no evidence of people in the area. 
						return true;
					//1018092	You see no evidence of those in the area.
				}
			}
			if (charsAround.Count > GetMaxTrackableChars(ssa)) {
				switch (charType) {
					case CharacterTypes.Animals:
						self.ClilocSysMessage(502990); //This area is too crowded to track any individual animal.
						return true;
					case CharacterTypes.Monsters:
						self.ClilocSysMessage(502992); //This area is too crowded to track any individual creature.
						return true;
					case CharacterTypes.NPCs:
						self.ClilocSysMessage(502994); //This area is too crowded to track any individual.
						return true;
					case CharacterTypes.Players:
						self.ClilocSysMessage(502994); //This area is too crowded to track any individual.
						return true;
					//1018091	This area is too crowded to track anything.
				}
			}
			return false;//its OK, we can continue with tracking
		}

		//get the area to look for any footsteps in
		private MutableRectangle GetTrackingArea(SkillSequenceArgs ssa) {
			Character self = ssa.Self;
			ushort range = (ushort)ssa.SkillDef.GetEffectForChar(self);
			return new MutableRectangle(self.P(), range);
		}

		//get the maximum age of the footsteps to be found
		private TimeSpan GetMaxFootstepsAge(SkillSequenceArgs ssa) {
			Character self = ssa.Self;
			double maxAge;
			if (self.IsGM) {
				maxAge = ScriptSector.CleaningPeriod; //get the maximal lifetime of the footsteps
			} else {
				maxAge = ScriptUtil.EvalRangePermille(ssa.SkillDef.SkillValueOfChar(self), MinFootstepAge, MaxFootstepAge);
			}
			return TimeSpan.FromSeconds(maxAge);
		}

		//get the maximum age of the footsteps to be found
		private int GetMaxTrackableChars(SkillSequenceArgs ssa) {
			Character self = ssa.Self;
			int maxChars;
			if (self.IsGM) {
				maxChars = int.MaxValue; //unlimited, GM sees everything
			} else {
				maxChars = (int)ScriptUtil.EvalRangePermille(ssa.SkillDef.SkillValueOfChar(self), MinCharsToTrack, MaxCharsToTrack);
			}
			return maxChars;
		}

		//get the maximum number of safe steps the tracker will be able to do (before another tracking chance recomputing)
		private int GetMaxSafeSteps(SkillSequenceArgs ssa) {
			Character self = ssa.Self;
			int maxSteps;
			if (self.IsGM) {
				maxSteps = int.MaxValue; //unlimited, GM can go as far as he wants
			} else {
				maxSteps = (int) ScriptUtil.EvalRangePermille(ssa.SkillDef.SkillValueOfChar(self), MinSafeSteps, MaxSafeSteps);
			}
			return maxSteps;
		}

		[InfoField("Min age [sec.0-skill]")]
		public double MinFootstepAge {
			get {
				return (double)minFootstepAge.CurrentValue;
			}
			set {
				minFootstepAge.CurrentValue = value;
			}
		}

		[InfoField("Max age[sec.100-skill]")]
		public double MaxFootstepAge {
			get {
				return (double)maxFootstepAge.CurrentValue;
			}
			set {
				maxFootstepAge.CurrentValue = value;
			}
		}

		[InfoField("Trackables [char.0/skill]")]
		public int MinCharsToTrack {
			get {
				return (int)minCharsToTrack.CurrentValue;
			}
			set {
				minCharsToTrack.CurrentValue = value;
			}
		}

		[InfoField("Trackables [char.100/skill]")]
		public int MaxCharsToTrack {
			get {
				return (int)maxCharsToTrack.CurrentValue;
			}
			set {
				maxCharsToTrack.CurrentValue = value;
			}
		}

		[InfoField("SafeSteps [char.0/skill]")]
		public int MinSafeSteps {
			get {
				return (int) minSafeSteps.CurrentValue;
			}
			set {
				minSafeSteps.CurrentValue = value;
			}
		}

		[InfoField("SafeSteps [char.100/skill]")]
		public int MaxSafeSteps {
			get {
				return (int) maxSafeSteps.CurrentValue;
			}
			set {
				maxSafeSteps.CurrentValue = value;
			}
		}

		[SteamFunction]
		public static void Track(Character self) {
			self.SelectSkill(SkillName.Tracking);
		}
	}

	[ViewableClass]
	public partial class TrackingPlugin {
		public static readonly TrackingPluginDef defInstance = new TrackingPluginDef("p_tracking", "C#scripts", -1);
		internal static PluginKey trackingPluginKey = PluginKey.Get("_tracking_");

		internal static int refreshTimeout = 10; //number of seconds after which all displayed footsteps will be refreshed (if necessary)

		internal List<TrackPoint> footsteps = new List<TrackPoint>();

		private int stepsCntr;

		internal const ushort WORST_COLOR = 1827; //worst visible footsteps
		internal const ushort BEST_COLOR = 1835; //best visible footsteps

		//Get all available TrackPoints and send a fake item packet to the Cont about it
		private void RefreshFootsteps() {
			//lower bound of the footsteps visibility-age (upper bound is Globals.TimeAsSpan)
			//the footsteps LastStepTime must lie between these two bounds for the footprint to be visible
			//the maxFootstepAge is computed from the tracker's skill
			TimeSpan worstVisibleAt = Globals.TimeAsSpan - maxFootstepAge;
			TimeSpan bestVisibleAt = Globals.TimeAsSpan;

			GameState trackersState = ((Character) Cont).GameState;
			if (trackersState != null) {//only if the player is connected (otherwise it makes no sense)
				PacketGroup pgToSend = PacketGroup.AcquireSingleUsePG();
				List<TrackPoint> fsToRemove = new List<TrackPoint>();
				foreach (TrackPoint tp in footsteps) {
					//check if the tp is not too old...
					if (tp.LastStepTime < worstVisibleAt) {
						if (RemoveFootstepFromView(tp, pgToSend)) {
							fsToRemove.Add(tp);
						}
						continue;
					}
					//check if tp has its fake UID assigned and if not, gather one
					if (tp.FakeUID == 0) {
						tp.FakeUID = Thing.GetFakeItemUid();
					}
					ShowFootstep(tp, pgToSend, worstVisibleAt, bestVisibleAt);
				}
				if (!pgToSend.IsEmpty) {
					trackersState.Conn.SendPacketGroup(pgToSend);//send the packets
				}
				foreach (TrackPoint toBeRemoved in fsToRemove) {
					footsteps.Remove(toBeRemoved); //remove removed footsteps :-)
				}
			}
			if (footsteps.Count == 0) {
				Delete();//no footsteps left - no need to continue
			}
		}

		//check if the footstep has been displayed and if so, prepare the removal packet
		//return the TrackPoint if it is to be removed
		private bool RemoveFootstepFromView(TrackPoint tp, PacketGroup pgToSend) {
			uint uid;
			if ((uid = tp.FakeUID) != 0) {
				//prepare the item removal packet (0x1d)
				pgToSend.AcquirePacket<DeleteObjectOutPacket>().Prepare(uid);
				Thing.DisposeFakeUid(uid);//return the borrowed UID
				return true; //will be removed from footsteps list
			}
			return false;//dont remove it
		}

		//prepare a packet about the footstep
		private void ShowFootstep(TrackPoint tp, PacketGroup pgToSend, TimeSpan worstVisibleAt, TimeSpan bestVisibleAt) {
			//count the color according to the lastStepTime using a linear dependency
			ushort color = (ushort)(WORST_COLOR + (BEST_COLOR-WORST_COLOR)*((tp.LastStepTime.TotalSeconds - worstVisibleAt.TotalSeconds) / (bestVisibleAt.TotalSeconds - worstVisibleAt.TotalSeconds)));
			if (tp.Color != color) {
				tp.Color = color; //store the color and we will prepare the packet
				pgToSend.AcquirePacket<ObjectInfoOutPacket>()
					.PrepareFakeItem(tp.FakeUID, tp.Model, tp.Location, 1, Direction.North, (ushort) (color + 1));
				//the color is +1 because for items 0 means default (not black) and other colors start from 1 (black, etc.)
				//in the .colorsdialog we can see 0 = black but this is true only for text!
			}
		}

		public void On_Assign() {
			//display all footsteps to the player
			RefreshFootsteps();
			stepsCntr = safeSteps; //set the counter
		}

		public void On_UnAssign(Character formerCont) {
			formerCont.ClilocSysMessage(502989);//Tracking failed.

			GameState trackersState = formerCont.GameState;
			if (trackersState != null) {//only if the player is connected (otherwise it makes no sense)
				PacketGroup pgToSend = PacketGroup.AcquireSingleUsePG();
				List<TrackPoint> fsToRemove = new List<TrackPoint>();
				foreach (TrackPoint tp in this.footsteps) {
					if (Point2D.GetSimpleDistance(formerCont, tp.Location) <= trackersState.UpdateRange) {
						if (RemoveFootstepFromView(tp, pgToSend)) {
							fsToRemove.Add(tp);
						}
					}
				}
				if (!pgToSend.IsEmpty) {
					trackersState.Conn.SendPacketGroup(pgToSend);//and send the packets
				}
				foreach (TrackPoint toBeRemoved in fsToRemove) {
					footsteps.Remove(toBeRemoved); //remove removed footsteps
				}
				
			}
			//remove from the trackedBy list on the tracked character
			List<Character> tbList = (List<Character>)whoToTrack.GetTag(TrackingSkillDef.trackedByTK);
			tbList.Remove(formerCont);
			if (tbList.Count == 0) {
				whoToTrack.RemoveTag(TrackingSkillDef.trackedByTK);
			}
		}

		public void On_Step(ScriptArgs args) {//1st arg = direction (byte), 2nd arg = running (bool)
			//check the steps counter
			stepsCntr--;
			if (stepsCntr == 0) {//force another check of tracking success
				if (!SkillDef.ById(SkillName.Tracking).CheckSuccess((Character) Cont, Globals.dice.Next(700))) { //the same success check as in On_Stroke phase
					Delete();
					return;
				} else {
					stepsCntr = safeSteps; //reset the counter
				}
			}
			//now recompute the rectangle
			int moveX = 0, moveY = 0;
			switch ((Direction) args.argv[0]) {
				case Direction.North:
					moveY = -1;
					moveX = 0;
					break;
				case Direction.NorthWest:
					moveY = -1;
					moveX = -1;
					break;
				case Direction.NorthEast:
					moveY = -1;
					moveX = 1;
					break;
				case Direction.East:
					moveY = 0;
					moveX = 1;
					break;
				case Direction.West:
					moveY = 0;
					moveX = -1;
					break;
				case Direction.South:
					moveY = 1;
					moveX = 0;
					break;
				case Direction.SouthWest:
					moveY = 1;
					moveX = -1;
					break;
				case Direction.SouthEast:
					moveY = 1;
					moveX = 1;
					break;
			}
			trackingRectangle.Move(moveX, moveY);//alter the rectangle
			List<TrackPoint> newFootsteps =	ScriptSector.GetCharsPath(whoToTrack, trackingRectangle, maxFootstepAge, ((Character)Cont).M);//get the actual list of steps
			List<TrackPoint> oldFootsteps = new List<TrackPoint>();
			foreach (TrackPoint fs in footsteps) {
				if (!newFootsteps.Contains(fs)) {
					oldFootsteps.Add(fs); //this footstep is to be removed
				}
			}
			RemoveFootsteps(oldFootsteps);
			RefreshFootsteps();//and refresh all necessary footsteps
		}

		//remove specified list of footsteps (usually "@onStep")
		private void RemoveFootsteps(List<TrackPoint> which) {
			GameState trackersState = ((Character)Cont).GameState;
			if (trackersState != null) {//only if the player is connected (otherwise it makes no sense)
				PacketGroup pgToSend = PacketGroup.AcquireSingleUsePG();
				foreach (TrackPoint tp in which) {
					RemoveFootstepFromView(tp, pgToSend);//this will also remove it from the 'footsteps' list in the plugin
				}
				trackersState.Conn.SendPacketGroup(pgToSend);//and send the packets
			}
		}


		public void On_SkillStart(SkillSequenceArgs skillSeqArgs) {
			Delete();
		}

		public void On_Timer() {
			RefreshFootsteps(); //force to recompute the displayed footsteps color and send the necessary refresh packets
			this.Timer = TrackingPlugin.refreshTimeout;
		}
	}
}