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
using System.IO;
using System.Runtime.Serialization;
using SteamEngine;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Configuration;
using System.Diagnostics;
using SteamEngine.Common;
using SteamEngine.Packets;
using System.Reflection;

namespace SteamEngine.Packets {

	public class NetState {
		internal static bool NetStateTracingOn = TagMath.ParseBoolean(ConfigurationManager.AppSettings["NetState Trace Messages"]);
		
		private static NetState stack = null;//"free" instances
		private static NetState queue = null;//queued nistances with assigned Things

		private static bool enabled;
		
		private NetState prev;
		private NetState next;
		
		NSFlags changeflags;
		
		private Thing thing;
		
		private Point4D point;
		private Point4D topPoint;
		private string name;
		private byte flagsToSend;
		//private ushort flags;
		//private uint flaggedUid;
		private AbstractCharacter mount;
		private int mountUid;

		private ushort model;
		private ushort color;
		private Direction direction;
	
		private short hitpoints;
		private short maxHitpoints;
		private short stamina;
		private short maxStamina;
		private short mana;
		private short maxMana;
		private short str;
		private short intel;
		private short dex;
		private bool isFemale;
		private ulong gold;

		private short armorClass;
		private short stat1;
		private short stat2;
		private short stat3;
		private short stat4;
		private float weight;
		private short stat5;
		private short stat6;
		private short stat7;
		private long tithingPoints;

		private ushort[] changedSkills = new ushort[AbstractSkillDef.SkillsCount];
		private int changedSkillsCount;
		
		private NetState() {
		}


		public static void Disable() {
			Logger.WriteInfo(NetStateTracingOn, "NetState.Disable() called");
			enabled = false;
		}
		
		public static void Enable() {
			Logger.WriteInfo(NetStateTracingOn, "NetState.Enable() called");
			enabled = true;
		}
		
		public static bool IsEnabled { get {
			return enabled;
		} }
		
		private static void Enqueue(NetState ns) {
			if (queue != null) {
				queue.prev = ns;
				ns.next = queue;
			}
			queue = ns;
		}
		
		private static void Dequeue(NetState ns) {
			if (ns.prev == null) {
				queue = ns.next;
			} else {
				ns.prev.next = ns.next;
				ns.prev = null;
			}
			if (ns.next != null) {
				ns.next.prev = ns.prev;
				ns.next = null;
			}
		}
		
		private static void Push(NetState ns) {
			if (stack != null) {
				stack.prev = ns;
				ns.next = stack;
			}
			stack = ns;
		}
		
		private static void Pop(NetState ns) {
			if (ns.prev == null) {
				stack = ns.next;
			} else {
				ns.prev.next = ns.next;
				ns.prev = null;
			}
			if (ns.next != null) {
				ns.next.prev = ns.prev;
				ns.next = null;
			}
		}
		
		//get an instance from the "stack", or create a new one
		private static NetState PopAndEnqueueInstance(Thing self) {
			NetState ns = self.netState;
			if (ns != null) {
				return ns;//we assume it's enqueued already and stuff. No one is to touch Thing.netState but this class!
			}
			if (stack == null) {
				ns = new NetState();
			} else {
				ns = stack;
				Pop(ns);
			}
			ns.thing = self;
			self.netState = ns;
			Enqueue(ns);
			return ns;
		}
		
		//return the instance to the "stack"
		private static void DequeueAndPushInstance(NetState ns) {
			Dequeue(ns);
			Push(ns);
			ns.thing.netState = null;
			ns.thing = null;
			ns.mount = null;
			ns.point = null;
			ns.topPoint = null;
			ns.changedSkillsCount = 0;
			ns.changeflags = NSFlags.None;
		}

		public static void AboutToChangeProperty(Thing thing) {
			if (enabled) {
				Logger.WriteInfo(NetStateTracingOn, "AboutToChangeProperty("+thing+") called");
				PopAndEnqueueInstance(thing).changeflags |= NSFlags.Property;
			}
		}

		public static void AboutToChangeSkill(AbstractCharacter thing, ushort skillId) {
			if (enabled) {
				Logger.WriteInfo(NetStateTracingOn, "AboutToChangeSkill("+thing+", "+skillId+") called");
				PopAndEnqueueInstance(thing).AboutToChangeSkill(skillId);
			}
		}
		
		private void AboutToChangeSkill(ushort skillId) {
			for (int i = 0; i<changedSkillsCount; i++) {
				if (changedSkills[i] == skillId) {
					return;//we know about this change already
				}
			}
			changedSkills[changedSkillsCount] = skillId;
			changedSkillsCount++;
		}
		
		private void UpdateSkills(GameConn conn) {
			bool displaySkillCaps = conn.Version.displaySkillCaps;
			for (int i = 0; i<changedSkillsCount; i++) {
				ushort id = changedSkills[i];
				Logger.WriteInfo(NetStateTracingOn, "UpdateSkill: "+AbstractSkillDef.ById(id).Key);
				ISkill curSkill = ((AbstractCharacter) thing).Skills[id];
				PacketSender.PrepareSingleSkillUpdate(curSkill, displaySkillCaps);
				PacketSender.SendTo(conn, true);
			}
		}
		
		[Summary("Call when a thing is about to be created/changed")]
		public static void Resend(Thing thing) {
			if (enabled) {
				Logger.WriteInfo(NetStateTracingOn, "Resend(#0x"+thing.Uid.ToString("x")+") called");
				PopAndEnqueueInstance(thing).changeflags |= NSFlags.Resend;
			}
		}
		
		[Summary("Call when an item is about to be changed")]
		public static void ItemAboutToChange(AbstractItem thing) {
			if (enabled) {
				//Console.WriteLine(new System.Diagnostics.StackTrace());
				Logger.WriteInfo(NetStateTracingOn, "AboutToChange("+thing+") called");
				PopAndEnqueueInstance(thing).ItemAboutToChange();
			}
		}
		
		private void ItemAboutToChange() {
			if (IsNewAndPositiveBit(NSFlags.ItemUpdate)) {
				point = new Point4D(thing);
				topPoint = new Point4D(thing.TopObj());
			}
		}
		
		[Summary("Call when name is about to be changed")]
		public static void AboutToChangeName(AbstractCharacter thing) {
			if (enabled) {
				Logger.WriteInfo(NetStateTracingOn, "AboutToChangeName("+thing+") called");
				PopAndEnqueueInstance(thing).AboutToChangeName();
			}
		}
		private void AboutToChangeName() {
			if (IsNewAndPositiveBit(NSFlags.Name)) {
				name = thing.Name;
			}
		}
		private bool GetNameChanged() {
			bool retVal = (((changeflags & NSFlags.Name) == NSFlags.Name)&&(!name.Equals(thing.Name)));
			Logger.WriteInfo(NetStateTracingOn&&retVal, "NameChanged: "+retVal);
			return retVal;
		}

		[Summary("Call when base properties (model/color) are about to be changed")]
		public static void AboutToChangeBaseProps(AbstractCharacter thing) {
			if (enabled) {
				Logger.WriteInfo(NetStateTracingOn, "AboutToChangeBaseProps("+thing+") called");
				PopAndEnqueueInstance(thing).AboutToChangeBaseProps();
			}
		}
		private void AboutToChangeBaseProps() {
			if (IsNewAndPositiveBit(NSFlags.BaseProps)) {
				model = thing.Model;
				color = thing.Color;
			}
		}
		private bool GetBasePropsChanged() {
			bool retVal = (((changeflags & NSFlags.BaseProps) == NSFlags.BaseProps)
				&&((model != thing.Model)||(color != thing.Color)));
			Logger.WriteInfo(NetStateTracingOn&&retVal, "GetBasePropsChanged (model/color): "+retVal);
			return retVal;
		}

		[Summary("Call when direction is about to be changed")]
		public static void AboutToChangeDirection(AbstractCharacter thing, bool requested) {
			if (enabled) {
				Logger.WriteInfo(NetStateTracingOn, "AboutToChangeDirection("+thing+") called");
				NetState ns = PopAndEnqueueInstance(thing);
				if (ns.IsNewAndPositiveBit(NSFlags.Direction)) {
					ns.direction = thing.Direction;
					if (requested) {
						ns.changeflags |= NSFlags.RequestedStep;
					} else {
						ns.changeflags = ns.changeflags&~NSFlags.RequestedStep;
					}
				}
			}
		}
		private bool GetDirectionChanged() {
			bool retVal = (((changeflags & NSFlags.Direction) == NSFlags.Direction)
				&& (direction != ((AbstractCharacter) thing).Direction));
			Logger.WriteInfo(NetStateTracingOn&&retVal, "GetDirectionChanged: "+retVal);
			return retVal;
		}
		
		[Summary("Call when Flags are about to be changed")]
		public static void AboutToChangeFlags(AbstractCharacter thing) {
			if (enabled) {
				Logger.WriteInfo(NetStateTracingOn, "AboutToChangeFlags("+thing+") called");
				PopAndEnqueueInstance(thing).AboutToChangeFlags();
			}
		}
		private void AboutToChangeFlags() {
			if (IsNewAndPositiveBit(NSFlags.Flags)) {
				AbstractCharacter c = (AbstractCharacter) thing;
				flagsToSend = c.FlagsToSend;
			}
		}
		private bool GetChangedFlags(out bool invisChanges, out bool warModeChanges) {
			invisChanges = false;
			warModeChanges = false;
			bool retVal = false;
			AbstractCharacter c = thing as AbstractCharacter;
			if ((changeflags & NSFlags.Flags) == NSFlags.Flags) {
				byte newFlagsToSend = c.FlagsToSend;

				if ((this.flagsToSend & 0x40) != (newFlagsToSend & 0x40)) {
					warModeChanges = true;
				}

				retVal = ((this.flagsToSend &~ 0x40) != (newFlagsToSend &~ 0x40));

				if ((changeflags & NSFlags.Visibility) == NSFlags.Visibility) {
					invisChanges = true;
				}
				if (!invisChanges) {
					invisChanges = ((this.flagsToSend & 0x80) != (newFlagsToSend & 0x80));
				}
			}

			Logger.WriteInfo(NetStateTracingOn&&retVal&&retVal, "GetFlagsChanged: "+retVal+", invisChanges "+invisChanges);
			return retVal;
		}

		[Summary("Call when visibility is about to be changed")]
		public static void AboutToChangeVisibility(Thing thing) {
			if (enabled) {
				PopAndEnqueueInstance(thing).AboutToChangeVisibility();
			}
		}

		private void AboutToChangeVisibility() {
			changeflags |= NSFlags.Visibility;
			if (IsNewAndPositiveBit(NSFlags.Flags)) {
				AbstractCharacter c = (AbstractCharacter) thing;
				flagsToSend = c.FlagsToSend;
			}
		}
		
		[Summary("Call when position is about to be changed")]
		public static void AboutToChangePosition(AbstractCharacter thing, MovementType movType) {
			if (enabled) {
				int movTypeInt = (int) movType;
				Sanity.IfTrueThrow((movTypeInt < 1 || movTypeInt > 8), "Incorrect MovementType.");
				Logger.WriteInfo(NetStateTracingOn, "AboutToChangePosition("+thing+", "+movType+") called");
				PopAndEnqueueInstance(thing).AboutToChangePosition(movTypeInt);
			}
		}
		private void AboutToChangePosition(int movType) {
			if (IsNewAndPositiveBit(NSFlags.Position)) {
				point = new Point4D(thing);
				topPoint = new Point4D(thing.TopObj());
			}
			changeflags |= (NSFlags) movType;//a dirty change of the enum type....
		}
		private bool GetPositionChanged(out bool teleported, out bool running, out bool requestedStep) {
			teleported = false;
			running = false;
			requestedStep = false;
			bool retVal = false;
			if (((changeflags&NSFlags.Position) == NSFlags.Position)&&(!Point4D.Equals(point, thing))) {
				retVal = true;
				if ((changeflags&NSFlags.Running) == NSFlags.Running) {
					running = ((changeflags&NSFlags.Running) == NSFlags.Running);
				} else if ((changeflags&NSFlags.Teleport) == NSFlags.Teleport) {
					teleported = true;
				}
			}
			if ((changeflags & NSFlags.RequestedStep) == NSFlags.RequestedStep) {
				requestedStep = true;
			}
			Logger.WriteInfo(NetStateTracingOn&&retVal, "GetPositionChanged: "+retVal+", teleported:"
				+teleported+", running:"+running+", requestedStep:"+requestedStep);
			return retVal;
		}
		
		[Summary("Call when mount is about to be changed")]
		public static void AboutToChangeMount(AbstractCharacter thing) {
			if (enabled) {
				Logger.WriteInfo(NetStateTracingOn, "AboutToChangeMount("+thing+") called");
				PopAndEnqueueInstance(thing).AboutToChangeMount();
			}
		}
		private void AboutToChangeMount() {
			if (IsNewAndPositiveBit(NSFlags.Mount)) {
				mount = ((AbstractCharacter) thing).Mount;
				if (mount != null) {
					mountUid = mount.Uid;
				}
			}
		}
		private bool GetMountChanged() {
			bool retVal = (((changeflags & NSFlags.Mount) == NSFlags.Mount)&&(mount != ((AbstractCharacter) thing).Mount));
			Logger.WriteInfo(NetStateTracingOn&&retVal, "GetMountChanged: "+retVal);
			return retVal;
		}
		
		[Summary("Call when highlight (notoriety color) is about to be changed")]
		public static void AboutToChangeHighlight(AbstractCharacter thing) {
			if (enabled) {
				Logger.WriteInfo(NetStateTracingOn, "AboutToChangeHighlight("+thing+") called");
				PopAndEnqueueInstance(thing).changeflags |= NSFlags.Highlight;
			}
		}
		private bool GetHighlightChanged() {
			bool retVal = ((changeflags & NSFlags.Highlight) == NSFlags.Highlight);
			Logger.WriteInfo(NetStateTracingOn&&retVal, "GetHighlightChanged: "+retVal);
			return retVal;
		}

		[Summary("Call when hitpoints are about to be changed")]
		public static void AboutToChangeHitpoints(AbstractCharacter thing) {
			if (enabled) {
				Logger.WriteInfo(NetStateTracingOn, "AboutToChangeHitpoints("+thing+") called");
				PopAndEnqueueInstance(thing).AboutToChangeHitpoints();
			}
		}
		private void AboutToChangeHitpoints() {
			if (IsNewAndPositiveBit(NSFlags.Hits)) {
				AbstractCharacter c = (AbstractCharacter) thing;
				hitpoints = c.Hits;
				maxHitpoints = c.MaxHits;
			}
		}
		private bool GetHitpointsChanged() {
			AbstractCharacter c = (AbstractCharacter) thing;
			bool retVal = (((changeflags & NSFlags.Hits) == NSFlags.Hits)
				&&((hitpoints != c.Hits) || (maxHitpoints != c.MaxHits)));
			Logger.WriteInfo(NetStateTracingOn&&retVal, "GetHitpointsChanged: "+retVal);
			return retVal;
		}
		
		[Summary("Call when mana is about to be changed")]
		public static void AboutToChangeMana(AbstractCharacter thing) {
			if (enabled) {
				Logger.WriteInfo(NetStateTracingOn, "AboutToChangeMana("+thing+") called");
				PopAndEnqueueInstance(thing).AboutToChangeMana();
			}
		}
		private void AboutToChangeMana() {
			if (IsNewAndPositiveBit(NSFlags.Mana)) {
				AbstractCharacter c = (AbstractCharacter) thing;
				mana = c.Mana;
				maxMana = c.MaxMana;
			}
		}
		private bool GetManaChanged() {
			AbstractCharacter c = (AbstractCharacter) thing;
			bool retVal = (((changeflags & NSFlags.Mana) == NSFlags.Mana)
				&&((mana != c.Mana) || (maxMana != c.MaxMana)));
			Logger.WriteInfo(NetStateTracingOn&&retVal, "GetManaChanged: "+retVal);
			return retVal;
		}
		
		[Summary("Call when stamina is about to be changed")]
		public static void AboutToChangeStamina(AbstractCharacter thing) {
			if (enabled) {
				Logger.WriteInfo(NetStateTracingOn, "AboutToChangeStamina("+thing+") called");
				PopAndEnqueueInstance(thing).AboutToChangeStamina();
			}
		}
		private void AboutToChangeStamina() {
			if (IsNewAndPositiveBit(NSFlags.Stam)) {
				AbstractCharacter c = (AbstractCharacter) thing;
				stamina = c.Stam;
				maxStamina = c.MaxStam;
			}
		}
		private bool GetStaminaChanged() {
			AbstractCharacter c = (AbstractCharacter) thing;
			bool retVal = (((changeflags & NSFlags.Stam) == NSFlags.Stam)
				&&((stamina != c.Stam) || (maxStamina != c.MaxStam)));
			Logger.WriteInfo(NetStateTracingOn&&retVal, "GetStaminaChanged: "+retVal);
			return retVal;
		}
		
		[Summary("Call when stats  are about to be changed")]
		[Remark("The stats are following: Strength, Dexterity, Intelligence, Gender, Gold, "
		+ "PhysicalResist (armor), Weight, FireResist, ColdResist, PoisonResist, "
		+ "EnergyResist, Luck, MinDamage, MaxDamage and TithingPoints")]
		public static void AboutToChangeStats(AbstractCharacter thing) {
			if (enabled) {
				Logger.WriteInfo(NetStateTracingOn, "AboutToChangeStats("+thing+") called");
				PopAndEnqueueInstance(thing).AboutToChangeStats();
			}
		}

		private void AboutToChangeStats() {
			if (IsNewAndPositiveBit(NSFlags.Stats)) {
				AbstractCharacter c = (AbstractCharacter) thing;
				str = c.Str;
				dex = c.Dex;
				intel = c.Int;
				isFemale = c.IsFemale;
				gold = c.Gold;
				armorClass = c.StatusArmorClass;
				weight = c.Weight;
				stat1 = c.ExtendedStatusNum1;
				stat2 = c.ExtendedStatusNum2;
				stat3 = c.ExtendedStatusNum3;
				stat4 = c.StatusMindDefense;
				stat5 = c.ExtendedStatusNum5;
				stat6 = c.ExtendedStatusNum6;
				stat7 = c.ExtendedStatusNum7;
				tithingPoints = c.TithingPoints;
			}
		}
		private bool GetStatsChanged() {
			AbstractCharacter c = (AbstractCharacter) thing;
			bool retVal = (((changeflags & NSFlags.Stats) == NSFlags.Stats)
				&&((str != c.Str) || (dex != c.Dex) || (intel != c.Int) ||
				(isFemale != c.IsFemale) || (gold != c.Gold) || 
				(armorClass != c.StatusArmorClass) ||
				(weight != c.Weight) || (stat1 != c.ExtendedStatusNum1) || 
				(stat2 != c.ExtendedStatusNum2) || (stat3 != c.ExtendedStatusNum3) || 
				(stat4 != c.StatusMindDefense) || (stat5 != c.ExtendedStatusNum5) || 
				(stat6 != c.ExtendedStatusNum6) || (stat7 != c.ExtendedStatusNum7) || 
				(tithingPoints != c.TithingPoints)));

			Logger.WriteInfo(NetStateTracingOn&&retVal, "GetStatsChanged: "+retVal);
			return retVal;
		}
	
		private bool IsNewAndPositiveBit(NSFlags flagBeingSet) {
			if ((changeflags&flagBeingSet) != flagBeingSet) {
				changeflags |= flagBeingSet;
				return true;
			}
			return false;
		}
		
		[Flags]
		private enum NSFlags : int {
			None			= 0x00000000,
			Resend			= 0x10000000,	//complete update - after creation
			
			//these are same as in MovementType - do not change
			Walking			= 0x00000001,
			Running			= 0x00000002,
			RequestedStep	= 0x00000004,
			Teleport		= 0x00000008,

			//char updates
			BaseProps		= 0x00000010,	//Model, Color
			Direction		= 0x00000020,
			Name			= 0x00000040,
			Flags			= 0x00000080,
			Position		= 0x00000100,
			Visibility		= 0x00000200,	//we can change visibility even without changing flags (for particular people etc.)
			Mount			= 0x00000400,
			Highlight		= 0x00000800,
			
			//status
			Hits			= 0x00001000,
			Mana			= 0x00002000,	
			Stam			= 0x00004000,
			Stats			= 0x00008000,//str, dex, int + extended status props - gender, gold, resists, luck, damage, tithingpoints, weight, etc.

			//item update
			ItemUpdate		= 0x00010000,

			//Property - for both char and item
			Property		= 0x00020000
		}
		
		public static void ProcessAll() {
			while (queue != null) {
				queue.Process();
			}
		}
		
		public static void ProcessThing(Thing thing) {
			NetState ns = thing.netState;
			if (ns != null) {
				ns.Process();
			}
		}

		private void Process() {
			if (enabled) {
				if (!thing.IsDeleted) {//deleted items are supposed to be removedfromview by the delete code
					if (changeflags == NSFlags.None) {
						//nothing
					} else {
						AbstractItem i = thing as AbstractItem;
						if (i != null) {
							if ((changeflags & NSFlags.Resend) == NSFlags.Resend) {
								ProcessItemResend(i);
							} else {
								ProcessItemUpdate(i);
							}
						} else {//character
							if ((changeflags & NSFlags.Resend) == NSFlags.Resend) {
								ProcessCharResend((AbstractCharacter) thing);
							} else {
								ProcessCharUpdate((AbstractCharacter) thing);
							}
						}
					}
				}
				DequeueAndPushInstance(this);
			}
		}

		private void ProcessItemResend(AbstractItem item) {
			Logger.WriteInfo(NetStateTracingOn, "ProcessItemResend "+item);
			ProcessItem(item, null);
		}

		private void ProcessItemUpdate(AbstractItem item) {
			Logger.WriteInfo(NetStateTracingOn, "ProcessItemUpdate "+item);

			if ((changeflags & NSFlags.ItemUpdate) == NSFlags.ItemUpdate) {
				ProcessItem(item, topPoint);
			} else if (Globals.AOS) {//only new properties
				ProcessItemProperties(item);
			}
		}

		private void ProcessItemProperties(AbstractItem item) {
			IEnumerable<GameConn> enumerator;
			AbstractItem contAsItem = item.Cont as AbstractItem;
			if (contAsItem != null) {
				enumerator = OpenedContainers.GetConnsWithOpened(contAsItem);
				//checkPreviousVisibility = false;
			} else {
				Thing top = item.TopObj();
				enumerator = top.GetMap().GetClientsInRange(top.X, top.Y, Globals.MaxUpdateRange);
			}

			ObjectPropertiesContainer iopc = null;
			bool propertiesExist = true;
			foreach (GameConn conn in enumerator) {
				if (conn.Version.aosToolTips) {
					if (propertiesExist) {
						if (iopc == null) {
							iopc = item.GetProperties();
							if (iopc == null) {
								propertiesExist = false;
								break;
							}
						}
						iopc.SendIdPacket(conn);
					}
				}
			}
		}

		//oldMapPoint can be null if checkPreviousVisibility is false
		private void ProcessItem(AbstractItem item, IPoint4D oldMapPoint/*, bool checkPreviousVisibility*/) {
			bool propertiesExist = true;
			bool isOnGround = item.IsOnGround;
			bool isEquippedAndVisible = false;
			bool isInContainer = false;
			if (!isOnGround) {
				isEquippedAndVisible = item.IsEquipped;
				if (isEquippedAndVisible) {
					if (item.Z >= AbstractCharacter.sentLayers) {
						isEquippedAndVisible = false;
					}
				} else {
					isInContainer = item.IsInContainer;
				}
			}

			Thing newMapPoint = item.TopObj();
			Map newMap = newMapPoint.GetMap();
			//bool moved = false;
			//Rectangle2D rfvRectangle = null;

			//if ((oldMapPoint != null) && (!Point4D.Equals(oldMapPoint, newMapPoint))) {
			//    moved = true;
			//    rfvRectangle = new Rectangle2D(oldMapPoint, Globals.MaxUpdateRange);
			//    RemoveFromViewIfNeeded(oldMapPoint, rfvRectangle);//remove on old point
			//}

			BoundPacketGroup removeFromView = null;
			BoundPacketGroup pg = null;//iteminfo or paperdollinfo or itemincontainer
			BoundPacketGroup allmoveItemInfo = null;
			ObjectPropertiesContainer iopc = null;

			IEnumerable<GameConn> enumerator;
			AbstractItem contAsItem = item.Cont as AbstractItem;
			if (contAsItem != null) {
				enumerator = OpenedContainers.GetConnsWithOpened(contAsItem);
				//checkPreviousVisibility = false;
			} else {
				enumerator = newMap.GetClientsInRange(newMapPoint.X, newMapPoint.Y, Globals.MaxUpdateRange);
			}

			foreach (GameConn viewerConn in enumerator) {
				AbstractCharacter viewer = viewerConn.CurCharacter;
				if (viewer != null) {
					//bool canSeeNow = false;
					//bool canSeeNowChecked = false;
					#region checkPreviousVisibility
					//if (checkPreviousVisibility) {
					//    if (moved) {
					//        if ((rfvRectangle == null) || (!rfvRectangle.Contains(viewer))) {//already removedfromview
					//            canSeeNow = viewer.CanSeeForUpdate(item);
					//            canSeeNowChecked = true;
					//            if (!canSeeNow) {
					//                if (viewer.CanSeeForUpdateAt(oldMapPoint, item)) {//could see before
					//                    if (removeFromView == null) {
					//                        removeFromView = PacketSender.NewBoundGroup();
					//                        PacketSender.PrepareRemoveFromView(thing);
					//                    }
					//                    Logger.WriteInfo(NetStateTracingOn, "Removing item ("+item+") from the view of "+viewer);
					//                    removeFromView.SendTo(conn);
					//                    continue;//viewer doesn't see this, so nothing more we can do
					//                }
					//            }
					//        }
					//    } else { //did not move, we just check distances
					//        if (viewer.CanSeeCoordinates(oldMapPoint)) {
					//            canSeeNow = viewer.CanSeeForUpdate(item);
					//            canSeeNowChecked = true;
					//            if (!canSeeNow) {
					//                if (removeFromView == null) {
					//                    removeFromView = PacketSender.NewBoundGroup();
					//                    PacketSender.PrepareRemoveFromView(thing);
					//                }
					//                Logger.WriteInfo(NetStateTracingOn, "Removing item ("+item+") from the view of "+viewer);
					//                removeFromView.SendTo(conn);
					//                continue;//viewer doesn't see this, so nothing more we can do
					//            }
					//        }
					//    }
					//}
					#endregion

					#region updateitem
					if (isOnGround || isEquippedAndVisible || isInContainer) {
						//if (!canSeeNowChecked) {
						//    canSeeNow = viewer.CanSeeForUpdate(item);
						//}
						if (viewer.CanSeeForUpdate(item)) {
							if (isOnGround) {
								if (viewer.IsPlevelAtLeast(Globals.plevelOfGM)) {
									if (allmoveItemInfo==null) {
										allmoveItemInfo=PacketSender.NewBoundGroup();
										PacketSender.PrepareItemInformation(item, MoveRestriction.Movable); //0x1a
									}
									allmoveItemInfo.SendTo(viewerConn);
								} else {
									if (pg==null) {
										pg=PacketSender.NewBoundGroup();
										PacketSender.PrepareItemInformation(item, MoveRestriction.Normal); //0x1a
									}
									pg.SendTo(viewerConn);
								}
							} else if (isEquippedAndVisible) {
								if (pg==null) {
									pg=PacketSender.NewBoundGroup();
									PacketSender.PreparePaperdollItem(item);
								}
								pg.SendTo(viewerConn);
							} else {
								if (pg==null) {
									pg=PacketSender.NewBoundGroup();
									PacketSender.PrepareItemInContainer(item);
								}
								pg.SendTo(viewerConn);
							}

							if (item.IsContainer && item.Count > 0 && 
									OpenedContainers.HasContainerOpen(viewerConn, item)) {
								if (PacketSender.PrepareContainerContents(item, viewerConn, viewer)) {
									PacketSender.SendTo(viewerConn, true);
									if (Globals.AOS && viewerConn.Version.aosToolTips) {
										foreach (AbstractItem contained in item) {
											if (viewer.CanSeeVisibility(contained)) {
												ObjectPropertiesContainer containedOpc = contained.GetProperties();
												if (containedOpc != null) {
													containedOpc.SendIdPacket(viewerConn);
												}
											}
										}
									}
								}
							}

							if (propertiesExist) {
								if (Globals.AOS && viewerConn.Version.aosToolTips) {
									if (iopc == null) {
										iopc = item.GetProperties();
										if (iopc == null) {
											propertiesExist = false;
											continue;
										}
									}
									iopc.SendIdPacket(viewerConn);
								}
							}
							item.On_BeingSentTo(viewerConn);
						}
					}
					#endregion isOnGround || isEquippedAndVisible || isInContainer
				}
			}

			if (removeFromView!=null)
				removeFromView.Dispose();
			if (pg!=null)
				pg.Dispose();
			if (allmoveItemInfo!=null) 
				allmoveItemInfo.Dispose();
		}

		private static BoundPacketGroup[] myCharInfos = new BoundPacketGroup[(int) HighlightColor.NumHighlightColors];
		private static BoundPacketGroup[] myMovings = new BoundPacketGroup[(int) HighlightColor.NumHighlightColors];

		private static BoundPacketGroup[] charInfoPackets = new BoundPacketGroup[(int) HighlightColor.NumHighlightColors];
		private void ProcessCharResend(AbstractCharacter ch) {
			Logger.WriteInfo(NetStateTracingOn, "ProcessCharResend "+ch);

			try {
				equippedItemsPropContainers.Clear();
				equippedItemsPropContainersCleared = true;
				bool propertiesExist = true;
				ObjectPropertiesContainer iopc = null;
				foreach (AbstractCharacter viewer in ch.GetMap().GetPlayersInRange(ch.X, ch.Y, Globals.MaxUpdateRange)) {
					if (viewer!=ch) {
						GameConn viewerConn = viewer.Conn;
						if (viewerConn != null) {
							if (viewer.CanSeeForUpdate(ch)) {
								int highlight = (int) ch.GetHighlightColorFor(viewer);
								if (charInfoPackets[highlight] == null) {
									charInfoPackets[highlight] = PacketSender.NewBoundGroup();
									PacketSender.PrepareCharacterInformation(ch, (HighlightColor) highlight);
								}
								charInfoPackets[highlight].SendTo(viewerConn);
								if (Globals.AOS && viewerConn.Version.aosToolTips) {
									if (propertiesExist) {
										propertiesExist = ProcessCharProperties(ch, ref iopc, viewerConn);
									}
									ProcessEquippedItemsProperties(ch, viewerConn, viewer);
								}
								ch.On_BeingSentTo(viewerConn);
								foreach (AbstractItem equippedItem in ch.visibleLayers) {
									equippedItem.On_BeingSentTo(viewerConn);
								}
							}
						}
					}
				}
			} finally {
				for (int a=0; a<charInfoPackets.Length; a++) {
					if (charInfoPackets[a]!=null) {
						charInfoPackets[a].Dispose();
						charInfoPackets[a]=null;
					}
				}
			}
		}

		private void ProcessCharUpdate(AbstractCharacter ch) {
			//TODO: party update
			//TODO: AOS stuff - name and other props (tooltips)?
			//triggers - @seenewplayer and stuff

			Logger.WriteInfo(NetStateTracingOn, "ProcessCharUpdate "+ch);
			bool invisChanged, warModeChanges;
			bool flagsChanged = GetChangedFlags(out invisChanged, out warModeChanges);
			bool highlightChanged = GetHighlightChanged();
			bool hitsChanged = GetHitpointsChanged();
			bool nameChanged = GetNameChanged();
			bool mountChanged = GetMountChanged();
			bool teleported, running, requestedStep;
			bool posChanged = GetPositionChanged(out teleported, out running, out requestedStep);
			bool directionChanged = GetDirectionChanged();
			bool basePropsChanged = GetBasePropsChanged();
			bool propertiesChanged = (changeflags & NSFlags.Property) == NSFlags.Property;
			bool propertiesExist = true;
			ObjectPropertiesContainer iopc = null;
			equippedItemsPropContainers.Clear();
			equippedItemsPropContainersCleared = true;

			Map chMap = ch.GetMap();
			ushort chX = ch.X;
			ushort chY = ch.Y;

			{
				GameConn myConn = ch.Conn;

				if (myConn != null) {
					UpdateSkills(myConn);
					if (propertiesExist && propertiesChanged) {
						if (Globals.AOS && myConn.Version.aosToolTips) {
							propertiesExist = ProcessCharProperties(ch, ref iopc, myConn);
						}
					}
					if (GetStatsChanged() || nameChanged) {
						Logger.WriteInfo(NetStateTracingOn, "Sending StatusBar to self");
						PacketSender.PrepareStatusBar(ch, StatusBarType.Me);
						PacketSender.SendTo(myConn, true);
					} else {
						bool manaChanged = GetManaChanged();
						bool staminaChanged = GetStaminaChanged();
						if (hitsChanged && manaChanged && staminaChanged) {//all 3 stats
							Logger.WriteInfo(NetStateTracingOn, "Sending Stats to self");
							PacketSender.PrepareUpdateStats(ch, true);
							PacketSender.SendTo(myConn, true);
						} else {
							if (manaChanged) {
								Logger.WriteInfo(NetStateTracingOn, "Sending Mana to self");
								PacketSender.PrepareUpdateMana(ch, true);
								PacketSender.SendTo(myConn, true);
							}
							if (staminaChanged) {
								Logger.WriteInfo(NetStateTracingOn, "Sending Stamina to self");
								PacketSender.PrepareUpdateStamina(ch, true);
								PacketSender.SendTo(myConn, true);
							}
							if (hitsChanged) {
								Logger.WriteInfo(NetStateTracingOn, "Sending Hitpoints to self");
								PacketSender.PrepareUpdateHitpoints(ch, true);
								PacketSender.SendTo(myConn, true);
							}
						}
					}
					if (flagsChanged || highlightChanged || basePropsChanged || ((directionChanged || posChanged) && (!requestedStep))) {
						Logger.WriteInfo(NetStateTracingOn, "Sending LocationInformation to self");
						PacketSender.PrepareLocationInformation(myConn);
						PacketSender.SendTo(myConn, true);
					}
					if (warModeChanges) {
						Prepared.SendWarMode(myConn, ch);
					}
					if (posChanged) {
						Map oldMap = topPoint.GetMap();
						bool mapChanged = oldMap != chMap;
						byte updateRange = ch.UpdateRange;

						if (mapChanged) {//other map. We must clear the view, and possibly change client's facet
							byte newFacet = chMap.Facet;
							if (oldMap.Facet != newFacet) {
								Prepared.SendFacetChange(myConn, newFacet);
							}
							foreach (Thing thing in oldMap.GetThingsInRange(topPoint.X, topPoint.Y, updateRange)) {
								Logger.WriteInfo(NetStateTracingOn, "Removing thing ("+thing+") from own view");
								PacketSender.PrepareRemoveFromView(thing);
								PacketSender.SendTo(myConn, true);
							}
						}
						foreach (Thing thingInRange in chMap.GetThingsInRange(chX, chY, updateRange)) {
							//I can see it now, but couldn't see it before
							if (thingInRange != ch) {//it isn't me
								if (ch.CanSeeForUpdate(thingInRange) && (mapChanged || 
								!ch.CanSeeForUpdateFrom(topPoint, thingInRange))) {
									Logger.WriteInfo(NetStateTracingOn, "Sending thing ("+thingInRange+") to self");
									AbstractCharacter newChar = thingInRange as AbstractCharacter;
									if (newChar != null) {
										PacketSender.PrepareCharacterInformation(newChar, newChar.GetHighlightColorFor(ch));
										PacketSender.SendTo(myConn, true);
										PacketSender.PrepareUpdateHitpoints(newChar, false);//may not be necessarry but oh well ;)
										PacketSender.SendTo(myConn, true);
										Server.SendCharPropertiesTo(myConn, ch, newChar);
										newChar.On_BeingSentTo(myConn);
										foreach (AbstractItem equippedItem in newChar.visibleLayers) {
											equippedItem.On_BeingSentTo(myConn);
										}
									} else {
										AbstractItem newItem = (AbstractItem) thingInRange;
										PacketSender.PrepareItemInformation(newItem);
										PacketSender.SendTo(myConn, true);
										if (Globals.AOS && myConn.Version.aosToolTips) {
											ObjectPropertiesContainer newiopc = thing.GetProperties();
											if (newiopc != null) {
												newiopc.SendIdPacket(myConn);
											}
										}
										newItem.On_BeingSentTo(myConn);
									}
								}
							}
						}
					}
					if (mountChanged) {
						SendMountChange(ch, myConn);
					}
				}
			}

			if (posChanged || directionChanged || flagsChanged || warModeChanges || highlightChanged || 
					invisChanged ||	hitsChanged || nameChanged || mountChanged || basePropsChanged) {
				int range = Globals.MaxUpdateRange;
				if (teleported) {
					RemoveFromViewIfNeeded(topPoint, new Rectangle2D(topPoint, Globals.MaxUpdateRange));
				} else if (posChanged) {
					range++;//not teleported, that means only step, so we update wider range
				}

				BoundPacketGroup rfv = null;
				BoundPacketGroup petStatus = null;
				BoundPacketGroup otherStatus = null;
				BoundPacketGroup hitsPacket = null;
				bool disposeArrays = false;

				foreach (AbstractCharacter viewer in chMap.GetPlayersInRange(chX, chY, (ushort) range)) {
					GameConn viewerConn = viewer.Conn;
					if (viewer != ch) {
						bool viewerCanSeeForUpdateAtPointChecked = false;
						bool viewerCanSeeForUpdateAtPoint = false;
						bool viewerCanSeeForUpdateChecked = false;
						bool viewerCanSeeForUpdate = false;
						if (viewerConn != null) {
							if ((!teleported) && (invisChanged || posChanged)) { //if teleported, we're already done
								if (!invisChanged) {
									viewerCanSeeForUpdateAtPoint = viewer.CanSeeForUpdateAt(topPoint, ch);
									viewerCanSeeForUpdateAtPointChecked = true;
								}
								if (invisChanged || viewerCanSeeForUpdateAtPoint) {
									viewerCanSeeForUpdate = viewer.CanSeeForUpdate(ch);
									viewerCanSeeForUpdateChecked = true;
									if (!viewerCanSeeForUpdate) {//they did see us, but now they dont. RemoveFromView.
										if (rfv == null) {
											rfv = PacketSender.NewBoundGroup();
											PacketSender.PrepareRemoveFromView(ch);
										}
										Logger.WriteInfo(NetStateTracingOn, "Removing from view of "+viewerConn);
										rfv.SendTo(viewerConn);
									}
								}
							}
							if (!viewerCanSeeForUpdateChecked) {
								viewerCanSeeForUpdate = viewer.CanSeeForUpdate(ch);
							}
							if (viewerCanSeeForUpdate) {
								bool hitsSent = false;
								bool newCharSent = false;
								if (invisChanged || posChanged) {
									if (!invisChanged) {
										if (!viewerCanSeeForUpdateAtPointChecked) {
											viewerCanSeeForUpdateAtPoint = viewer.CanSeeForUpdateAt(topPoint, ch);
										}
									}
									if (invisChanged || !viewerCanSeeForUpdateAtPoint) {
										//viewer didn't see us, but he does now - we send newchar packet
										int highlight = (int) ch.GetHighlightColorFor(viewer);
										BoundPacketGroup myCharInfo = myCharInfos[highlight];
										if (myCharInfo == null) {
											disposeArrays = true;
											myCharInfo = PacketSender.NewBoundGroup();
											PacketSender.PrepareCharacterInformation(ch, (HighlightColor) highlight);
											myCharInfos[highlight] = myCharInfo;
										}
										Logger.WriteInfo(NetStateTracingOn, "Sending new char info to "+viewerConn);
										myCharInfo.SendTo(viewerConn);
										newCharSent = true;
										if (Globals.AOS && viewerConn.Version.aosToolTips) {
											if (propertiesExist) {
												propertiesExist = ProcessCharProperties(ch, ref iopc, viewerConn);
											}
											ProcessEquippedItemsProperties(ch, viewerConn, viewer);
										}
										ch.On_BeingSentTo(viewerConn);
										foreach (AbstractItem equippedItem in ch.visibleLayers) {
											equippedItem.On_BeingSentTo(viewerConn);
										}
									}
								}
								if (!newCharSent) {
									if (propertiesChanged && propertiesExist) {
										if (Globals.AOS && viewerConn.Version.aosToolTips) {
											propertiesExist = ProcessCharProperties(ch, ref iopc, viewerConn);
										}
									}
									if (posChanged || directionChanged || flagsChanged || warModeChanges || highlightChanged || basePropsChanged) {
										int highlight = (int) ch.GetHighlightColorFor(viewer);
										BoundPacketGroup myMoving = myMovings[highlight];
										if (myMoving == null) {
											disposeArrays = true;
											myMoving = PacketSender.NewBoundGroup();
											PacketSender.PrepareMovingCharacter(ch, running, (HighlightColor) highlight);
											myMovings[highlight] = myMoving;
										}
										Logger.WriteInfo(NetStateTracingOn, "Sending moving char to "+viewerConn);
										myMoving.SendTo(viewerConn);
									}
									if (mountChanged) {
										SendMountChange(ch, viewerConn);
									}
								}
								if (nameChanged) {
									hitsSent = true;
									if (viewer.CanRename(ch)) {
										if (petStatus == null) {
											petStatus = PacketSender.NewBoundGroup();
											PacketSender.PrepareStatusBar(ch, StatusBarType.Pet);
										}
										Logger.WriteInfo(NetStateTracingOn, "Sending pet status "+viewerConn);
										petStatus.SendTo(viewerConn);
									} else {
										if (otherStatus == null) {
											otherStatus = PacketSender.NewBoundGroup();
											PacketSender.PrepareStatusBar(ch, StatusBarType.Other);
										}
										Logger.WriteInfo(NetStateTracingOn, "Sending simple status "+viewerConn);
										otherStatus.SendTo(viewerConn);
									}
								}
								if (hitsChanged && !hitsSent) {
									if (hitsPacket == null) {
										hitsPacket = PacketSender.NewBoundGroup();
										PacketSender.PrepareUpdateHitpoints(ch, false);
									}
									Logger.WriteInfo(NetStateTracingOn, "Sending pet status "+viewerConn);
									hitsPacket.SendTo(viewerConn);
								}
							}
						}
					}
				}

				if (rfv != null) rfv.Dispose();
				if (disposeArrays) {
					for (int i=0; i<(int) HighlightColor.NumHighlightColors; i++) {
						if (myCharInfos[i] != null) {
							myCharInfos[i].Dispose();
							myCharInfos[i] = null;
						}
						if (myMovings[i] != null) {
							myMovings[i].Dispose();
							myMovings[i] = null;
						}
					}
				}
				if (petStatus != null) petStatus.Dispose();
				if (otherStatus != null) otherStatus.Dispose();
				if (hitsPacket != null) hitsPacket.Dispose();

				ch.Flag_Moving=false;
			}
		}

		private static List<ObjectPropertiesContainer> equippedItemsPropContainers = new List<ObjectPropertiesContainer>();
		private static bool equippedItemsPropContainersCleared = false;

		private static bool ProcessCharProperties(AbstractCharacter target, ref ObjectPropertiesContainer iopc, GameConn viewerConn) {
			if (iopc == null) {
				iopc = target.GetProperties();
				if (iopc != null) {
					iopc.SendIdPacket(viewerConn);
					return true;
				}
			} else {
				iopc.SendIdPacket(viewerConn);
				return true;
			}
			return false;
		}

		private static void ProcessEquippedItemsProperties(AbstractCharacter target, GameConn viewerConn, AbstractCharacter viewer) {
			int i = 0;
			//these are not optimalised for existence like the props of the actual char (the propertiesExist variable)... I'm too lazy I guess, or something
			foreach (AbstractItem equippedItem in target.visibleLayers) {
				ObjectPropertiesContainer containedIopc;
				if (equippedItemsPropContainersCleared) {
					containedIopc = equippedItem.GetProperties();
					equippedItemsPropContainers.Add(containedIopc);
				} else {
					containedIopc = equippedItemsPropContainers[i];
				}
				if (containedIopc != null) {
					if (viewer.CanSeeVisibility(equippedItem)) {
						containedIopc.SendIdPacket(viewerConn);
					}
				}
				i++;
			}
			equippedItemsPropContainersCleared = false;
		}

		private void RemoveFromViewIfNeeded(IPoint4D mapPoint, Rectangle2D rect) {
			BoundPacketGroup rfv = null;
			Map oldMap = mapPoint.GetMap();

			foreach (AbstractCharacter viewer in oldMap.GetPlayersInRectangle(rect)) {
				GameConn conn = viewer.Conn;
				if (conn != null) {
					if ((viewer.CanSeeForUpdateAt(mapPoint, thing)) && (!viewer.CanSeeForUpdate(thing))) {
						if (rfv == null) {
							rfv = PacketSender.NewBoundGroup();
							PacketSender.PrepareRemoveFromView(thing);
						}
						Logger.WriteInfo(NetStateTracingOn, "Removing thing ("+thing+") from the view of "+viewer);
						rfv.SendTo(conn);
					}
				}
			}
			if (rfv!=null) rfv.Dispose();
		}

		private void SendMountChange(AbstractCharacter ch, GameConn conn) {
			AbstractCharacter myMount = ch.Mount;
			if (myMount == null) {
				Logger.WriteInfo(NetStateTracingOn, "Removing mount (#"+mountUid.ToString("x")+") from "+conn);
				PacketSender.PrepareRemoveFromView(mountUid|0x40000000);
				PacketSender.SendTo(conn, true);
			} else {
				Logger.WriteInfo(NetStateTracingOn, "Sending mount (#"+mountUid.ToString("x")+") to "+conn);
				PacketSender.PrepareMountInfo(ch);
				PacketSender.SendTo(conn, true);
			}
		}
	}
}
