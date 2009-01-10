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
		[Summary("Maximal age [sec] of the footsteps to be tracked at skill 0")]
		private double minFootstepAge = 15;
		[Summary("Maximal age [sec] of the footsteps to be tracked at skill 100")]
		private double maxFootstepAge = 120;

		[Summary("Maximum characters to be recognized at skill 0")]
		private int minToTrack = 3;

		[Summary("Maximum characters to be recognized at skill 100")]
		private int maxToTrack = 20;

		public TrackingSkillDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
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
			ScriptRectangle rect = GetTrackingArea(skillSeqArgs);
			TimeSpan maxAge = GetMaxFootstepsAge(skillSeqArgs);
			switch ((TrackingEnums) skillSeqArgs.Param2) {
				case TrackingEnums.Phase_Characters_Seek: //we will look for chars around
					CharacterTypes charType = (CharacterTypes) skillSeqArgs.Param1;
			
					List<Character> charsAround = ScriptSector.GetCharactersInRectangle(rect, charType, maxAge);

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

					//get the set of characters visible footsteps
					LinkedList<TrackPoint> charsSteps = ScriptSector.GetCharsPath(charToTrack, rect, maxAge);

					//and forward the tracking management to the special plugin
					TrackingPlugin tpl = (TrackingPlugin) TrackingPlugin.defInstance.Create();
					tpl.trackingRange = rect.Range;//for recomputing the rect when OnStep...
					tpl.maxFootstepAge = maxAge;//for refreshing the footsteps when OnStep...
					tpl.footsteps = charsSteps;//initial list of footsteps

					self.AddPlugin(TrackingPlugin.trackingPluginKey, tpl);
					
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
		private ScriptRectangle GetTrackingArea(SkillSequenceArgs ssa) {
			Character self = ssa.Self;
			int range = (int)ssa.SkillDef.GetEffectForChar(self);
			return new ScriptRectangle(self.P(),range);
		}

		//get the maximum age of the footsteps to be found
		private TimeSpan GetMaxFootstepsAge(SkillSequenceArgs ssa) {
			Character self = ssa.Self;
			double maxAge;
			if (self.IsGM) {
				maxAge = ScriptSector.CleaningPeriod; //get the maximal lifetime of the footsteps
			} else {
				maxAge = ScriptUtil.EvalRangePermille(ssa.SkillDef.SkillValueOfChar(self), minFootstepAge, maxFootstepAge);
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
				maxChars = (int)ScriptUtil.EvalRangePermille(ssa.SkillDef.SkillValueOfChar(self), minToTrack, maxToTrack);
			}
			return maxChars;
		}

		[InfoField("Min age [sec.0-skill]")]
		public double MinFootstepAge {
			get {
				return minFootstepAge;
			}
			set {
				minFootstepAge = value;
			}
		}

		[InfoField("Max age[sec.100-skill]")]
		public double MaxFootstepAge {
			get {
				return maxFootstepAge;
			}
			set {
				maxFootstepAge = value;
			}
		}

		[InfoField("Trackables [char.0/skill]")]
		public int MinCharsToTrack {
			get {
				return minToTrack;
			}
			set {
				minToTrack = value;
			}
		}

		[InfoField("Trackables [char.100/skill]")]
		public int MaxCharsToTrack {
			get {
				return maxToTrack;
			}
			set {
				maxToTrack = value;
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

		internal LinkedList<TrackPoint> footsteps = new LinkedList<TrackPoint>();

		//models for footprints (unfortunatelly we have only 4 directions although we need 8 :-/)
		//internal const ushort FOOTPRINT = 0x1e03; //basic (west) footprint
		internal const ushort FOOTPRINT_WEST = 0x1e03;
		internal const ushort FOOTPRINT_NORTH = 0x1e04;
		internal const ushort FOOTPRINT_EAST = 0x1e05;
		internal const ushort FOOTPRINT_SOUTH = 0x1e06;

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
				PacketGroup pgToSend = PacketGroup.AcquireMultiUsePG();
				foreach (TrackPoint tp in footsteps) {
					//check if the tp is not too old...
					if (tp.LastStepTime < worstVisibleAt) {
						RemoveFootstepFromView(tp, pgToSend);
						continue;
					}
					//check if tp has its fake UID assigned and if not, gather one
					if (tp.FakeUID == default(uint)) {
						tp.FakeUID = Thing.GetFakeItemUid();
					}
					ShowFootstep(tp, pgToSend, worstVisibleAt, bestVisibleAt);
				}
				trackersState.Conn.SendPacketGroup(pgToSend);//and send the packets
			}
		}

		//check if the footstep has been displayed and if so, prepare the removal packet
		//otherwise simply remove it from the list of trackpoints
		private void RemoveFootstepFromView(TrackPoint tp, PacketGroup pgToSend) {
			uint uid;
			if ((uid = tp.FakeUID) != default(uint)) {
				//prepare the item removal packet (0x1d)
				pgToSend.AcquirePacket<DeleteObjectOutPacket>().Prepare(uid);
				Thing.DisposeFakeUid(uid);//return the borrowed UID
			}
			footsteps.Remove(tp); //dont show it anymore
		}

		//prepare a packet about the footstep
		private void ShowFootstep(TrackPoint tp, PacketGroup pgToSend, TimeSpan worstVisibleAt, TimeSpan bestVisibleAt) {
			//count the color according to the lastStepTime using a linear dependency
			int color = (int)(WORST_COLOR + (BEST_COLOR-WORST_COLOR)*((tp.LastStepTime.TotalSeconds - worstVisibleAt.TotalSeconds) / (bestVisibleAt.TotalSeconds - worstVisibleAt.TotalSeconds)));
			pgToSend.AcquirePacket<ObjectInfoOutPacket>()
					.PrepareFakeItem(tp.FakeUID, tp.Model, tp.Location, 1, Direction.North, (ushort)(color+1));
					//the color is +1 because for items 0 means default (not black) and other colors start from 1 (black, etc.)
					//while in the .colorsdialog we can see 0 = black but this is true only for text!
		}

		public void On_Assign() {
			//display all footsteps to the player
			RefreshFootsteps();
		}

		public void On_UnAssign(Character formerCont) {
			//formerCont.ManaRegenSpeed -= this.additionalManaRegenSpeed;
			//if (formerCont.Mana >= formerCont.MaxMana) {//meditation finished
			//	formerCont.ClilocSysMessage(501846);//You are at peace.
			//} else {//meditation somehow aborted
			//	formerCont.ClilocSysMessage(501848);//You cannot focus your concentration
			//}
		}

		public void On_Step(ScriptArgs args) {
			//Delete();
		}

		public void On_SkillStart(SkillSequenceArgs skillSeqArgs) {
			Delete();
		}
	}
}