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

		[Summary("Similar as Effect but for tracking monsters,animals and NPCs")]
		private FieldValue pvmEffect;

		public TrackingSkillDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			minFootstepAge = InitField_Typed("minFootstepAge", 15, typeof(double));
			maxFootstepAge = InitField_Typed("maxFootstepAge", 120, typeof(double));
			minCharsToTrack = InitField_Typed("minCharsToTrack", 3, typeof(int));
			maxCharsToTrack = InitField_Typed("maxCharsToTrack", 20, typeof(int));
			minSafeSteps = InitField_Typed("minSafeSteps", 1, typeof(int));
			maxSafeSteps = InitField_Typed("maxSafeSteps", 10, typeof(int));
			pvmEffect = InitField_Typed("pvmEffect", new double[]{16.0,64,0}, typeof(double[]));
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
					skillSeqArgs.Success = true;//select the character to display its footsteps or to navigate to it, no need to check for success
					break;
			}

			return false; //continue to @success or @fail
		}

		protected override bool On_Success(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			CharacterTypes charType = (CharacterTypes) skillSeqArgs.Param1;
			AbstractRectangle rect = GetTrackingArea(skillSeqArgs);

			if (charType == CharacterTypes.Players) {//tracking Players
				TimeSpan maxAge = GetMaxFootstepsAge(skillSeqArgs);
				switch ((TrackingEnums) skillSeqArgs.Param2) {
					case TrackingEnums.Phase_Characters_Seek: //we will look for chars around
						List<AbstractCharacter> charsAround = ScriptSector.GetCharactersInRectangle(rect, maxAge, self.M);
						
						//check if tracking is possible (with message)
						//i.e. too many chars or none at all
						if (CheckTrackImpossible(skillSeqArgs, charsAround.Count, charType)) {
							return true;
						}

						//display a dialog with the found trackable characters
						self.ClilocSysMessage(1018093);//Select the one you would like to track.
						self.Dialog(self, SingletonScript<D_Tracking_Characters>.Instance, new DialogArgs(skillSeqArgs, charsAround));
						break;
					case TrackingEnums.Phase_Character_Track: //we will try to display the chars path
						Character charToTrack = (Character) skillSeqArgs.Target1;

						//get the list of characters visible footsteps
						List<WatchedTrackPoint> charsSteps = ScriptSector.GetCharsPath(charToTrack, rect, maxAge, self.M);

						//and forward the tracking management to the special plugin
						PlayerTrackingPlugin tpl = (PlayerTrackingPlugin) PlayerTrackingPlugin.defInstance.Create();
						tpl.trackingRectangle = (MutableRectangle)rect; //for recomputing the rect when OnStep...
						tpl.maxFootstepAge = maxAge; //for refreshing the footsteps when OnStep...
						tpl.whoToTrack = charToTrack; //for refreshing the footsteps when OnStep...
						tpl.footsteps = charsSteps; //initial list of footsteps
						tpl.safeSteps = GetMaxSafeSteps(skillSeqArgs); //number of safe steps
						tpl.Timer = PlayerTrackingPlugin.refreshTimeout;//set the first timer
						self.AddPlugin(PlayerTrackingPlugin.trackingPluginKey, tpl);

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
			} else {//tracking animals, monsters or NPCs
				switch ((TrackingEnums) skillSeqArgs.Param2) {
					case TrackingEnums.Phase_Characters_Seek: //we will look for chars around
						List<AbstractCharacter> trackables = null;
						switch (charType) {
							case CharacterTypes.Animals:
								trackables = new List<AbstractCharacter>();
								foreach (Character chr in Map.GetMap(self.M).GetNPCsInRectangle((ImmutableRectangle) rect)) {
									if(chr.IsAnimal) trackables.Add(chr);
								}
								break;
							case CharacterTypes.Monsters:
								trackables = new List<AbstractCharacter>();
								foreach (Character chr in Map.GetMap(self.M).GetNPCsInRectangle((ImmutableRectangle) rect)) {
									if(chr.IsMonster) trackables.Add(chr);
								}
								break;
							case CharacterTypes.NPCs:
								trackables = new List<AbstractCharacter>();
								foreach (Character chr in Map.GetMap(self.M).GetNPCsInRectangle((ImmutableRectangle) rect)) {
									if(chr.IsHuman) trackables.Add(chr);
								}
								break;
							case CharacterTypes.All:
								//monsters, animals and human NPCs
								trackables = new List<AbstractCharacter>(Map.GetMap(self.M).GetNPCsInRectangle((ImmutableRectangle) rect));
								break;
						}
						
						//check if tracking is possible (with message) - i.e. too many chars or none at all
						if (CheckTrackImpossible(skillSeqArgs, trackables.Count, charType)) {
							return true;
						}

						//display a dialog with the found trackable characters
						self.ClilocSysMessage(1018093);//Select the one you would like to track.
						self.Dialog(self, SingletonScript<D_Tracking_Characters>.Instance, new DialogArgs(skillSeqArgs, trackables));
						break;
					case TrackingEnums.Phase_Character_Track: //we will try to display the chars path
						Character charToTrack = (Character) skillSeqArgs.Target1;

						NPCTrackingPlugin npctpl = (NPCTrackingPlugin) NPCTrackingPlugin.defInstance.Create();
						npctpl.maxAllowedDist = GetMaxRange(self, charType); //maximal distance before the tracked character disappears...						
						npctpl.safeSteps = GetMaxSafeSteps(skillSeqArgs); //number of safe steps
						npctpl.whoToTrack = charToTrack;
						npctpl.Timer = NPCTrackingPlugin.refreshTimeout;//set the first timer
						
						self.AddPlugin(NPCTrackingPlugin.npcTrackingPluginKey, npctpl);
						break;
				}
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
		private bool CheckTrackImpossible(SkillSequenceArgs ssa, int charsAroundCount, CharacterTypes charType) {
			Character self = ssa.Self;
			if (charsAroundCount == 0) {
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
			if (charsAroundCount > GetMaxTrackableChars(ssa)) {
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
		private AbstractRectangle GetTrackingArea(SkillSequenceArgs ssa) {
			Character self = ssa.Self;
			CharacterTypes trackMode = (CharacterTypes) ssa.Param1;
			if (trackMode == CharacterTypes.Players) {
				return new MutableRectangle(self.P(), GetMaxRange(self, trackMode));
			} else {//animals, monsters, npcs
				return new ImmutableRectangle(self.P(), GetMaxRange(self, trackMode));
			}
		}

		//get the Tracking range  - either for computing the scanned Rectangle or determining the maximal tracking distance
		private ushort GetMaxRange(Character forWho, CharacterTypes trackMode) {
			if (trackMode == CharacterTypes.Players) {
				return (ushort) this.GetEffectForChar(forWho); //tracking players - use the Effect field
			} else {
				//tracking other types of characters (animals, monsters, NPCs) - use the PVMEffect field
				if (forWho.IsGM) {
					return (ushort) ScriptUtil.EvalRangePermille(1000.0, this.PVMEffect);
				} else {
					return (ushort) ScriptUtil.EvalRangePermille(SkillValueOfChar(forWho), this.PVMEffect);
				}
			}
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

		[InfoField("PVM Effect")]
		public double[] PVMEffect {
			get {
				return (double[]) pvmEffect.CurrentValue;
			}
			set {
				pvmEffect.CurrentValue = value;
			}
		}

		[SteamFunction]
		public static void Track(Character self) {
			self.SelectSkill(SkillName.Tracking);
		}

		[SteamFunction]
		public static void StopTrack(Character self) {
			self.RemovePlugin(PlayerTrackingPlugin.trackingPluginKey);
			self.RemovePlugin(NPCTrackingPlugin.npcTrackingPluginKey);
		}
	}
}