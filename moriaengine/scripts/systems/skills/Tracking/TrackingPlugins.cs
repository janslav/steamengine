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
	public partial class PlayerTrackingPlugin {
		public static readonly PlayerTrackingPluginDef defInstance = new PlayerTrackingPluginDef("p_tracking", "C#scripts", -1);
		internal static PluginKey trackingPluginKey = PluginKey.Get("_tracking_");

		internal static int refreshTimeout = 10; //number of seconds after which all displayed footsteps will be refreshed (if necessary)

		internal List<WatchedTrackPoint> footsteps = new List<WatchedTrackPoint>();

		private int stepsCntr;

		internal const ushort WORST_COLOR = 1827; //worst visible footsteps
		internal const ushort BEST_COLOR = 1835; //best visible footsteps

		//Get all available TrackPoints and send a fake item packet to the Cont about it
		private void RefreshFootsteps() {
			//lower bound of the footsteps visibility-age (upper bound is Globals.TimeAsSpan)
			//the footsteps LastStepTime must lie between these two bounds for the footprint to be visible
			//the maxFootstepAge is computed from the tracker's skill
			TimeSpan bestVisibleAt = Globals.TimeAsSpan;
			TimeSpan worstVisibleAt = bestVisibleAt - maxFootstepAge;

			GameState trackersState = ((Character) Cont).GameState;
			if (trackersState != null) {//only if the player is connected (otherwise it makes no sense)
				PacketGroup pg = PacketGroup.AcquireSingleUsePG();
				List<WatchedTrackPoint> fsToRemove = new List<WatchedTrackPoint>();
				int i = 0, j = 0;
				foreach (WatchedTrackPoint tp in footsteps) {
					//check if the tp is not too old...
					if (tp.LastStepTime < worstVisibleAt) {
						if (Point2D.GetSimpleDistance((Character) Cont, tp.Location) <= trackersState.UpdateRange) {
							//remove it explicitely only if it's not too far....
							DeleteObjectOutPacket doop = Pool<DeleteObjectOutPacket>.Acquire();
							doop.Prepare(tp.FakeUID);
							trackersState.Conn.SendSinglePacket(doop);
							tp.TrackPoint.TryDisposeFakeUID();
							j++;
							//if (RemoveFootstepFromView(tp, pg)) {
							fsToRemove.Add(tp);
							//}
						}
						continue;
					}
					//check if tp has its fake UID assigned and if not, gather one
					tp.TrackPoint.TryGetFakeUID();
					i++;
					ShowFootstep(tp, pg, worstVisibleAt, bestVisibleAt);
				}
				//if (!pg.IsEmpty) {
				((Character) Cont).SysMessage("Sending " + i + " refresh packets and " + j + " removal packets");
				//	trackersState.Conn.SendPacketGroup(pg);//send the packets
				//} else {
				pg.Dispose(); //not used - dispose
				//}
				foreach (WatchedTrackPoint toBeRemoved in fsToRemove) {
					footsteps.Remove(toBeRemoved); //remove removed footsteps :-)
				}
			}
			if (footsteps.Count == 0) {
				Delete();//no footsteps left - no need to continue
			}
		}

		//check if the footstep has been displayed and if so, prepare the removal packet
		//return the TrackPoint if it is to be removed
		private bool RemoveFootstepFromView(WatchedTrackPoint tp, PacketGroup pg) {
			uint uid = tp.FakeUID;
			if (uid != 0) {
				//prepare the item removal packet (0x1d)
				pg.AcquirePacket<DeleteObjectOutPacket>().Prepare(uid);
				tp.TrackPoint.TryDisposeFakeUID();
				return true; //will be removed from footsteps list
			}
			return false;//dont remove it
		}

		//prepare a packet about the footstep
		private void ShowFootstep(WatchedTrackPoint tp, PacketGroup pg, TimeSpan worstVisibleAt, TimeSpan bestVisibleAt) {
			Character tracker = (Character) Cont;
			//count the color according to the lastStepTime using a linear dependency
			ushort color = (ushort) (WORST_COLOR + (BEST_COLOR - WORST_COLOR) * ((tp.LastStepTime.TotalSeconds - worstVisibleAt.TotalSeconds) / (bestVisibleAt.TotalSeconds - worstVisibleAt.TotalSeconds)));
			if (tp.Color == color) {//color has not changed...
				return;
			}
			//otherwise (no color was set yet or the new color is different):
			tp.Color = color; //store the new color and we will prepare the packet
			ObjectInfoOutPacket oiop = Pool<ObjectInfoOutPacket>.Acquire();
			oiop.PrepareFakeItem(tp.FakeUID, tp.Model, tp.Location, 1, Direction.North, (ushort) (color + 1));
			tracker.GameState.Conn.SendSinglePacket(oiop);
			//pg.AcquirePacket<ObjectInfoOutPacket>()
			//	.PrepareFakeItem(tp.FakeUID, tp.Model, tp.Location, 1, Direction.North, (ushort) (color + 1));
			//the color is +1 because for items 0 means default (not black) and other colors start from 1 (black, etc.)
			//in the .colorsdialog we can see 0 = black but this is true only for text!

		}

		public void On_Assign() {
			//display all footsteps to the player
			RefreshFootsteps();
			stepsCntr = safeSteps; //set the counter
		}

		public void On_UnAssign(Character formerCont) {
			formerCont.ClilocSysMessage(502989);//Tracking failed.

			RemoveFootsteps(footsteps, formerCont, false); //false - don't bother with removing footsteps from the list...
			//remove from the trackedBy list on the tracked character
			List<Character> tbList = (List<Character>) whoToTrack.GetTag(TrackingSkillDef.trackedByTK);
			tbList.Remove(formerCont);
			if (tbList.Count == 0) {
				whoToTrack.RemoveTag(TrackingSkillDef.trackedByTK);
			}
		}

		public void On_Step(ScriptArgs args) {//1st arg = direction (byte), 2nd arg = running (bool)
			Character tracker = (Character) Cont;
			//check the steps counter
			stepsCntr--;
			if (stepsCntr == 0) {//force another check of tracking success
				if (!SkillDef.ById(SkillName.Tracking).CheckSuccess(tracker, Globals.dice.Next(700))) { //the same success check as in On_Stroke phase
					Delete();
					return;
				} else {
					stepsCntr = safeSteps; //reset the counter
				}
			}

			//now recompute the rectangle
			int moveX = 0, moveY = 0;
			Map.Offset((Direction) args.argv[0], ref moveX, ref moveY); //prepare the movement X and Y modifications (1, 0 or -1 for both directions)

			trackingRectangle.Move(moveX, moveY);//alter the rectangle
			List<WatchedTrackPoint> newFootsteps = ScriptSector.GetCharsPath(whoToTrack, trackingRectangle, maxFootstepAge, tracker.M);//get the actual list of steps
			List<WatchedTrackPoint> oldFootsteps = new List<WatchedTrackPoint>();
			foreach (WatchedTrackPoint fs in footsteps) {
				if (!newFootsteps.Contains(fs)) {
					oldFootsteps.Add(fs); //this footstep is to be removed
				}
			}
			RemoveFootsteps(oldFootsteps, tracker, false); //false - not necessary to remove anything from the footsteps list as we will replace it whole
			footsteps = newFootsteps; //replace the list of footsteps
			RefreshFootsteps();//and refresh all necessary footsteps
		}

		//remove specified list of footsteps (usually "@onStep" or "@unassign")
		//clearlist - shall the removed footstep be aslo removed fro mthe footsteps list? (this is not necessary on Unassign 
		//because the plugin is disposed anyways...
		private void RemoveFootsteps(List<WatchedTrackPoint> which, Character forWho, bool clearList) {
			GameState trackersState = forWho.GameState;
			if (trackersState != null) {//only if the player is connected (otherwise it makes no sense)
				//PacketGroup pg = PacketGroup.AcquireSingleUsePG();
				List<WatchedTrackPoint> fsToRemove = new List<WatchedTrackPoint>();
				int i = 0;
				foreach (WatchedTrackPoint tp in which) {
					if (Point2D.GetSimpleDistance(forWho, tp.Location) <= trackersState.UpdateRange) {
						DeleteObjectOutPacket doop = Pool<DeleteObjectOutPacket>.Acquire();
						doop.Prepare(tp.FakeUID);
						trackersState.Conn.SendSinglePacket(doop);
						tp.TrackPoint.TryDisposeFakeUID();
						i++;
						//if (RemoveFootstepFromView(tp, pg)) {
						if (clearList) {
							fsToRemove.Add(tp);
						}
						//}
					}
				}
				//if (!pg.IsEmpty) {
				//	trackersState.Conn.SendPacketGroup(pg);//and send the packets
				//} else {
				//	pg.Dispose(); //not used - dispose
				//}
				forWho.SysMessage("Sending " + i + " removal packets");
				if (clearList) {
					foreach (WatchedTrackPoint toBeRemoved in fsToRemove) {
						footsteps.Remove(toBeRemoved); //remove removed footsteps :-)
					}
				}
			}
		}

		public void On_SkillStart(SkillSequenceArgs skillSeqArgs) {
			Delete();
		}

		public void On_Timer() {
			RefreshFootsteps(); //force to recompute the displayed footsteps color and send the necessary refresh packets
			this.Timer = PlayerTrackingPlugin.refreshTimeout;
		}
	}

	[ViewableClass]
	public partial class NPCTrackingPlugin {
		public static readonly NPCTrackingPluginDef defInstance = new NPCTrackingPluginDef("p_NPCtracking", "C#scripts", -1);
		internal static PluginKey npcTrackingPluginKey = PluginKey.Get("_NPCtracking_");
		internal static int refreshTimeout = 10; //number of seconds after which all displayed footsteps will be refreshed (if necessary)
		
		private int stepsCntr;

		private void CheckDistance(Player tracker) {
			//check the distance to the tracked target
			int currentDist = Point2D.GetSimpleDistance(tracker, whoToTrack);
			if (currentDist > maxAllowedDist) {
				Delete();
			}
		}

		public void On_Step(ScriptArgs args) {//1st arg = direction (byte), 2nd arg = running (bool)
			Player tracker = (Player) Cont;
			//check the steps counter
			stepsCntr--;
			if (stepsCntr == 0) {//force another check of tracking success
				if (!SkillDef.ById(SkillName.Tracking).CheckSuccess(tracker, Globals.dice.Next(700))) { //the same success check as in On_Stroke phase
					Delete();
					return;
				} else {
					stepsCntr = safeSteps; //reset the counter
				}
			}

			//now check the arrow
			CheckDistance(tracker);
		}

		public void On_Assign() {
			//send the QuestArrow displaying packet
			((Player) Cont).QuestArrow(true, whoToTrack);
			stepsCntr = safeSteps; //set the counter
		}

		public void On_UnAssign(Character formerCont) {
			formerCont.ClilocSysMessage(502989);//Tracking failed

			//send the QuestArrow removal packet
			((Player) formerCont).QuestArrow(false, formerCont);
		}

		public void On_Timer() {
			CheckDistance((Player) Cont); //force check the distance to the target
			this.Timer = PlayerTrackingPlugin.refreshTimeout;
		}

		public void On_SkillStart(SkillSequenceArgs skillSeqArgs) {
			Delete();
		}
	}
}