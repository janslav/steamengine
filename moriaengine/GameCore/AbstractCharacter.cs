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
using System.Globalization;
using System.Text.RegularExpressions;
using SteamEngine.Packets;
using SteamEngine.Common;
using SteamEngine.Persistence;
using SteamEngine.Regions;
using System.Threading;
using System.Configuration;
using SteamEngine.Networking;
using SteamEngine.Communication;
using SteamEngine.Communication.TCP;
#if TESTRUNUO
using RunUO_Compression = Server.Network.Compression;
#endif

/*
	Unused (potential) flags:
	In 'direction':
		0x08, 0x10, 0x20, and 0x40 (0x80 is Flag_Moving, which is used to signal information about
		movement to NetState). Note that anyone checking the Direction property, or dir, or facing,
		will not see the flags, since they are hidden by those properties.
	'gender':
		This is currently a byte with valid values being 0 or 1, but probably should be eliminated
		and made into a flag on some other variable. One argument for leaving it as a byte would
		be if scripters want to script races with more than two genders (or two genders plus genderless).
		If we do leave it as a byte, then bits 2-7 (0x02 to 0x80) are available for use as flags, but
		we should use the higher ones first in case a scripter does decide to increase the # of genders
		for a specific race. Also, if we do add flags here, gender would have to be hidden behind a
		masking property, like direction is.
*/
using SteamEngine.CompiledScripts;

namespace SteamEngine {

	/**
		A character.
	*/
	public abstract partial class AbstractCharacter : Thing, ISrc {
		public static bool CharacterTracingOn = TagMath.ParseBoolean(ConfigurationManager.AppSettings["Character Trace Messages"]);

		public const uint numLayers = 31;
		public const uint sentLayers = 25;//0-24

		public AbstractCharacterDef TypeDef {
			get {
				return (AbstractCharacterDef) base.def;
			}
		}

		public override bool IsPlayer {
			get {
				Logger.WriteInfo(CharacterTracingOn, "IsPlayer: " + ((Account != null) ? "Nope" : ("Yep: " + Account)));
				return (this.Account != null);
			}
		}

		private AbstractAccount account = null;
		private string name = null;
		public Thing act = null;
		public Thing targ = null;
		private byte direction = 0;
		internal ThingLinkedList visibleLayers;//layers 0..24
		internal ThingLinkedList invisibleLayers;//layers (26..29) + (32..max)
		internal ThingLinkedList specialLayer;//layer 30
		internal AbstractItem draggingLayer = null;

		//In most cases, you should use Flag_* properties to set/get flags. Rarely, you may need to directly modify flags,
		//	in the SE core (not in scripts), which is why this is internal.
		//But whatever you do, NEVER set the disconnected flag (0x0001) by tweaking flags.
		protected ushort flags = 0;	//This is a ushort to save memory.
		private static uint instances = 0;

		internal CharSyncQueue.CharState syncState; //don't touch

		public AbstractCharacter(ThingDef myDef)
			: base(myDef) {
			instances++;
			this.name = myDef.Name;
			Globals.lastNewChar = this;
		}

		public AbstractCharacter(AbstractCharacter copyFrom)
			: base(copyFrom) { //copying constuctor
			instances++;
			this.account = copyFrom.account;
			this.name = copyFrom.name;
			this.targ = copyFrom.targ;
			this.flags = copyFrom.flags;
			this.direction = copyFrom.direction;
			this.act = copyFrom.act;
			Globals.lastNewChar = this;
			Map.GetMap(this.point4d.m).Add(this);
		}

		public static uint Instances {
			get {
				return instances;
			}
		}

		/**
			The Direction this character is facing.
		*/
		public Direction Direction {
			get {
				return (Direction) (direction & 0x7);
			}
			set {
				byte dir = (byte) value;
				if (dir != direction) {
					Sanity.IfTrueThrow(!Enum.IsDefined(typeof(Direction), value), "Invalid value " + value + " for " + this + "'s direction.");
					CharSyncQueue.AboutToChangeDirection(this, false);
					direction = dir;
				}
			}
		}
		/**
			Direction, as a byte.
		*/
		public byte Dir {
			get {
				return ((byte) (direction & 0x7));
			}
			set {
				Direction = (Direction) value;
			}
		}

		public abstract bool IsFemale { get; }
		public abstract bool IsMale { get; }

		//Used with NetState, but at present this should be set when the character moves (walk/run/fly), and should remain
		//set for the rest of the cycle. Maybe there's a potential use for that in scripts, so this is public.
		public bool Flag_Moving {
			get {
				return (direction & 0x80) == 0x80;
			}
			set {
				if (value) {
					direction |= 0x80;
				} else {
					direction &= 0x7f;
				}
			}
		}

		//public override float Weight {
		//	get {
		//		if (recalculateWeight) FixWeight();
		//		return calculatedWeight;
		//	} set {
		//		base.Weight=value;
		//	}
		//}

		public override int Height {
			get {
				int defHeight = Def.Height;
				if (defHeight > 0) {
					return defHeight;
				}
				return 10;
			}
		}

		public ushort MountItem {
			get {
				return this.TypeDef.MountItem;
			}
		}

		public abstract AbstractCharacter Mount { get; set; }

		public abstract bool Flag_WarMode { get; set; }
		public abstract bool Flag_Riding { get; }

		public abstract string PaperdollName { get; }

		public override string Name {
			get {
				return name;
			}
			set {
				this.InvalidateProperties();
				CharSyncQueue.AboutToChangeName(this);
				name = value;
			}
		}

		public AbstractAccount Account {
			get {
				return account;
			}
		}

		public GameConn Conn {
			get {
				if (this.account != null) {
					return this.account.Conn;
				} else {
					return null;
				}
			}
		}

		public GameState GameState {
			get {
				if (this.account != null) {
					return this.account.GameState;
				} else {
					return null;
				}
			}
		}

		public bool IsOnline {
			get {
				if (this.account != null) {
					return this.account.IsOnline;
				} else {
					return false;
				}
			}
		}

		public bool IsPlevelAtLeast(int plevel) {
			return (this.account != null && this.account.PLevel >= plevel);
		}

		public bool IsMaxPlevelAtLeast(int plevel) {
			return (this.account != null && this.account.MaxPLevel >= plevel);
		}

		public bool IsLingering {
			get {
				return this.IsPlayer && !this.Flag_Disconnected && this.GameState == null;
			}
		}

		public override bool Flag_Disconnected {
			get {
				return ((flags & 0x0001) == 0x0001);
			}
			set {
				throw new InvalidOperationException("Can't set Flag_Disconnected directly on a character");
			}
			//    Sanity.IfTrueThrow(IsPlayer, "It is NOT safe to set Flag_Disconnected on a player.");
			//    if (Flag_Disconnected!=value) {
			//        NetState.AboutToChangeFlags(this);
			//        flags=(ushort) (value?(flags|0x0001):(flags&~0x0001));
			//        if (value) {
			//            GetMap().Disconnected(this);
			//        } else {
			//            GetMap().Reconnected(this);
			//        }
			//    }
			//}
		}

		public override void Resend() {
			CharSyncQueue.Resend(this);
		}

		/**
			Only call this on players.
			If this character is logged in, forcefully and immediately logs them out.
			If this character is lingering (in game but disconnected), logs them out fully (making them be no longer in game).
			If this character is already logged out fully, this does nothing.
		*/
		public void LogoutFully() {
			if (this.IsPlayer) {
				if (this.IsLingering) {
					this.SetFlag_Disconnected(true);
				} else if (!this.Flag_Disconnected) {
					this.GameState.Conn.Close("LogoutFully called.");
					this.SetFlag_Disconnected(true);
				}	//else already logged out fully, do nothing
			}
		}

		/**
			Call this on both players and NPCs
			If this character is logged in, Close their connection, but they stay lingering
			If this character is lingering (in game but disconnected), logs them out fully (making them be no longer in game).
			If this character is already logged out fully, this does nothing.
			If this is a NPC, it simply goes to the Disconnect containers (of map) and gets it's Flag_Disconnected set to True
		*/
		public void Disconnect() {
			if (this.IsPlayer) {
				if (this.IsLingering) {
					this.SetFlag_Disconnected(true);
				} else if (!this.Flag_Disconnected) {
					this.GameState.Conn.Close("Disconnect called.");
				}	//else already logged out fully, do nothing
			} else {
				this.SetFlag_Disconnected(true);
			}
		}

		/**
			Call this on NPCs only
			If this is a NPC, it simply goes out of the Disconnect containers (of map) and gets it's Flag_Disconnected set to False
		*/
		public void Reconnect() {
			if (!this.IsPlayer) {
				this.SetFlag_Disconnected(false);
			}
		}

		//So Character itself (or GameConn after relinking) can set the flag without warnings.
		private void SetFlag_Disconnected(bool value) {
			if (this.Flag_Disconnected != value) {
				CharSyncQueue.AboutToChangeVisibility(this);
				this.flags = (ushort) (value ? (this.flags | 0x0001) : (this.flags & ~0x0001));
				if (value) {
					this.GetMap().Disconnected(this);
				} else {
					this.GetMap().Reconnected(this);
				}
			}
		}

		//is called by GameConn when relinking, which is after the Things have been loaded, but before
		//they're put into map
		internal void ReLinkToGameState() {
			this.flags = (ushort) (this.flags & ~0x0001); //we're not disconnected, the Map needs to know
		}

		//Called by GameConn
		internal bool TryLogIn() {
			bool success = this.Trigger_LogIn();
			if (success) {
				this.SetFlag_Disconnected(false);
			}
			return success;
		}

		//method: Trigger_LogIn
		//this method fires the @login trigger
		internal bool Trigger_LogIn() {
			bool cancel = false;
			cancel = this.TryCancellableTrigger(TriggerKey.login, null);
			if (!cancel) {
				//@login did not return 1
				try {
					cancel = this.On_LogIn();
				} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
			}
			return !cancel;	//return true for success
		}

		//method: Trigger_LogOut
		//this method fires the @logout trigger
		internal void Trigger_LogOut() {
			this.TryTrigger(TriggerKey.logout, null);
			try {
				this.On_LogOut();
			} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
		}

		public virtual void On_LogOut() {

		}

		public virtual bool On_LogIn() {
			return false;
		}

		public override void Save(SaveStream output) {
			if (this.account != null) {
				output.WriteValue("account", this.account);
			}
			if (!this.Def.Name.Equals(this.name)) {
				output.WriteValue("name", this.name);
			}
			int flagsToSave = this.flags;
			if (this.IsPlayer) {
				flagsToSave = flagsToSave | 0x0001;//add the Disconnected flag, makes no sense to save a person "connected"
			}
			if (flagsToSave != 0) {
				output.WriteValue("flags", flagsToSave);
			}
			output.WriteValue("direction", (byte) this.direction);
			base.Save(output);
		}

		//For loading.
		public void LoadAccount_Delayed(object resolvedObject, string filename, int line) {
			AbstractAccount acc = (AbstractAccount) resolvedObject;

			int slot;
			if (acc.AttachCharacter(this, out slot)) {
				this.account = acc;

				FixDisconnectedFlagOnPlayers();
			} else {
				Logger.WriteError("The character " + this + " declares " + acc + " as it's account, but the Account is already full. Deleting.");
				this.Delete();
			}
		}

		//For loading
		public void FixDisconnectedFlagOnPlayers() {
			if (IsPlayer) {
				if (this.GameState == null) {
					flags = (ushort) (flags | 0x0001);
				} else {	//there shouldn't actually be anyone connected when this happens, but we check just in case.
					flags = (ushort) (flags & ~0x0001);
				}
			}
		}

		//public void Disconnect() {
		//	if (Conn!=null) {
		//		Conn.Close("The disconnect command was used.");
		//	}
		//}

		//For loading.
		public override void LoadLine(string filename, int line, string valueName, string valueString) {
			switch (valueName) {
				case "account":
					ObjectSaver.Load(valueString, new LoadObject(LoadAccount_Delayed), filename, line);
					break;
				case "name":
					Match ma = ConvertTools.stringRE.Match(valueString);
					if (ma.Success) {
						name = ma.Groups["value"].Value;
					} else {
						name = valueString;
					}
					break;
				case "flags":
					flags = TagMath.ParseUInt16(valueString);
					break;
				case "direction":
					direction = TagMath.ParseByte(valueString);
					if (Flag_Moving) {
						Flag_Moving = false;
						Sanity.IfTrueSay(true, "Flag_Moving was saved! It should not have been! (At the time this sanity check was written, NetState changes should always be processed before any thought of saving occurs. NetState changes clear Flag_Moving, if it is set)");
					}
					break;
				default:
					base.LoadLine(filename, line, valueName, valueString);
					break;
			}
		}

		//TODO: abstract Damage methods

		/*
			Method: FixWeight
			Call to recalculate the weight of this container. Anything holding this is corrected as well.
		*/
		//public override void FixWeight() {
		//	calculatedWeight=CalculateWeight();
		//	recalculateWeight=false;
		//}
		//
		//internal override void IncreaseWeightBy(float adjust) {
		//	calculatedWeight+=adjust;
		//	Logger.WriteInfo(WeightTracingOn, "IncreaseWeightBy("+adjust+"), changed calculatedweight to "+calculatedWeight+".");
		//	Sanity.StackTraceIf(WeightTracingOn);
		//	recalculateWeight=false;
		//}
		//
		//internal override void DecreaseWeightBy(float adjust) {
		//	calculatedWeight-=adjust;
		//	Logger.WriteInfo(WeightTracingOn, "DecreaseWeightBy("+adjust+"), changed calculatedweight to "+calculatedWeight+".");
		//	Sanity.StackTraceIf(WeightTracingOn);
		//	recalculateWeight=false;
		//}

		/*
			Method: CalculateWeight
			Returns the weight of this, including contents.
		*/
		//public float CalculateWeight() {
		//	float w=0;
		//	foreach (AbstractItem i in this) {
		//		if (i!=null) {
		//			w+=i.Weight;
		//		}
		//	}
		//	w+=base.Weight;
		//	return w;
		//}

		//ISrc implementation
		public void WriteLine(string arg) {
			this.SysMessage(arg);
		}

		public void SysMessage(string arg) {
			GameState state = this.GameState;
			if (state != null) {
				PacketSequences.SendSystemMessage(state.Conn, arg, Globals.serverMessageColor);
			}
		}

		public void SysMessage(string arg, int color) {
			GameState state = this.GameState;
			if (state != null) {
				PacketSequences.SendSystemMessage(state.Conn, arg, (ushort) color);
			}
		}

		public void ClilocSysMessage(uint msg, string args) {
			GameState state = this.GameState;
			if (state != null) {
				PacketSequences.SendClilocSysMessage(state.Conn, msg, Globals.serverMessageColor, args);
			}
		}
		public void ClilocSysMessage(uint msg, params string[] args) {
			GameState state = this.GameState;
			if (state != null) {
				PacketSequences.SendClilocSysMessage(state.Conn, msg, Globals.serverMessageColor, args);
			}
		}

		public void ClilocSysMessage(uint msg, int color, string args) {
			GameState state = this.GameState;
			if (state != null) {
				PacketSequences.SendClilocSysMessage(state.Conn, msg, (ushort) color, args);
			}
		}

		public void ClilocSysMessage(uint msg, int color, params string[] args) {
			GameState state = this.GameState;
			if (state != null) {
				PacketSequences.SendClilocSysMessage(state.Conn, msg, (ushort) color, args);
			}
		}

		private class DefaultMovementSettings : IMovementSettings {
			public bool CanCrossLand { get { return true; } }
			public bool CanSwim { get { return false; } }
			public bool CanCrossLava { get { return false; } }
			public bool CanFly { get { return false; } }
			public bool IgnoreDoors { get { return false; } }
			public int ClimbPower { get { return 2; } } //max positive difference in 1 step
		}

		private static readonly IMovementSettings defaultMovement = new DefaultMovementSettings();

		public virtual IMovementSettings MovementSettings {
			get {
				return defaultMovement;
			}
			set {
				throw new NotSupportedException();
			}
		}

		//player or npc walking
		public bool WalkRunOrFly(Direction dir, bool running, bool requested) {
			ThrowIfDeleted();
			Flag_Moving = true;
			if (Direction == dir) {
				Point4D oldPoint = new Point4D(this.point4d);

				Map map = GetMap();
				int newZ, newY, newX;

				bool canMoveEverywhere = (this.IsPlayer && this.Plevel >= Globals.plevelOfGM);
				if (!map.CheckMovement(oldPoint, this.MovementSettings, dir, canMoveEverywhere, out newX, out newY, out newZ)) {
					return false;
				}

				//move char, and send 0x77 to players nearby

				//check valid spot, and change z
				if (!Map.IsValidPos(newX, newY, oldPoint.m)) {	//off the map
					return false;
				}

				if (this.TryCancellableTrigger(TriggerKey.step, new ScriptArgs(dir, running))) {
					return false;
				}

				bool onStepCancelled = false;
				try {
					this.On_Step((byte) dir, running);
				} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
				if (onStepCancelled) {
					return false;
				}

				//should we really be asking regions, even for npcs? -tar
				Region oldRegion = this.Region;
				Region newRegion = map.GetRegionFor(newX, newY);
				if (oldRegion != newRegion) {
					if (!Region.TryExitAndEnter(oldRegion, newRegion, this)) {
						return false;
					}
				}

				MovementType mt = MovementType.Walking;
				if (running) {
					mt = MovementType.Running;
				}
				if (requested) {
					mt |= MovementType.RequestedStep;
				}
				CharSyncQueue.AboutToChangePosition(this, mt);

				point4d.SetP((ushort) newX, (ushort) newY, (sbyte) newZ);
				ChangedP(oldPoint);
			} else {//just changing direction, no steps
				CharSyncQueue.AboutToChangeDirection(this, requested);
				direction = (byte) dir;
			}
			return true;
		}

		internal override sealed void SetPosImpl(ushort x, ushort y, sbyte z, byte m) {
			CharSyncQueue.AboutToChangePosition(this, MovementType.Teleporting);
			if (Map.IsValidPos(x, y, m)) {
				Point4D oldP = this.P();
				Region oldRegion = Map.GetMap(oldP.m).GetRegionFor(oldP.x, oldP.y);
				Region newRegion = Map.GetMap(m).GetRegionFor(x, y);
				if (oldRegion != newRegion) {
					Region.ExitAndEnter(oldRegion, newRegion, this);//forced exit & enter
				}
				point4d.x = x;
				point4d.y = y;
				point4d.z = z;
				point4d.m = m;
				ChangedP(oldP);
			} else {
				throw new ArgumentOutOfRangeException("Invalid position (" + x + "," + y + " on mapplane " + m + ")");
			}
		}

		public virtual bool On_Step(byte direction, bool running) {
			return false;
		}

		public override void On_Create() {
			AbstractItem ignoreTheWarning = Backpack; //mono compiler knows we dont use the variable ;)
			base.On_Create();
		}

		//		//The code for each of these SendUpdate* methods is identical except for the method which is called
		//		//in PacketSender by each. It would be more elegant if only one method did the actual work, but
		//		//the only efficient way I can think of to do that, would be to have a 'static MethodInfo[] updateMethods'
		//		//on Character, holding one MW for each update method, with the MWs pointing to the appropriate method in
		//		//PacketSender. The real implementation would be in a new SendUpdateFor(UpdateType) method, with UpdateType being
		//		//an enum containing Stats, Hitpoints, Mana, and Stamina. To call the prepare-packet method, it would
		//		//call updateMethods[(int) updateType].Invoke(this, true); or (this, false);
		//		
		//		/*
		//			Method: SendUpdateStats
		//				This sends the current and maximum hitpoints, mana, and stamina, of this character,
		//				to all clients who can see this character and are close enough for this character to be in
		//				their update range.
		//				
		//				For sending to everyone but this character and GMs (with plevel >= plevelToSeeRealStats),
		//				the stats are scaled to [0-255] to hide their real values.
		//				
		//				If only need to update only one of hitpoints, mana, or stamina, use SendUpdateHitpoints,
		//				SendUpdateMana, or SendUpdateStamina. If you would need to call two or more of those methods,
		//				then this method (SendUpdateStas) is actually more efficient.
		//		*/
		//		public void SendUpdateStats() {
		//			PacketGroup groupScaled = null;
		//			PacketGroup groupReal = null;
		//			
		//			foreach (AbstractCharacter viewer in PlayersInRange(Globals.MaxUpdateRange)) {
		//				if (viewer.Conn!=null && (viewer==this || viewer.CanSee(this))) {
		//					if (viewer==this || viewer.plevel>=Globals.plevelToSeeRealStats) {
		//						if (groupReal==null) {
		//							groupReal = new PacketGroup();
		//							PacketSender.PrepareUpdateStats(this, true);
		//						}
		//						groupReal.SendTo(viewer.Conn);
		//					} else {
		//						if (groupScaled==null) {
		//							groupScaled = new PacketGroup();
		//							PacketSender.PrepareUpdateStats(this, false);
		//						}
		//						groupScaled.SendTo(viewer.Conn);
		//					}
		//				}
		//			}
		//			if (groupReal!=null) groupReal.Dispose();
		//			if (groupScaled!=null) groupScaled.Dispose();
		//		}
		//		
		//		/*
		//			Method: SendUpdateHitpoints
		//				This sends the current and maximum hitpoints of this character,
		//				to all clients who can see this character and are close enough for the character to be in
		//				their update range.
		//				
		//				For sending to everyone but this character and GMs (with plevel >= plevelToSeeRealStats),
		//				the stats are scaled to [0-255] to hide their real values.
		//				
		//				If you need to update more than just hitpoints and maxhitpoints, you should use SendUpdateStats,
		//				which would be more efficient than calling this method plus another.
		//		*/
		//		public void SendUpdateHitpoints() {
		//			PacketGroup groupScaled = null;
		//			PacketGroup groupReal = null;
		//			
		//			foreach (AbstractCharacter viewer in PlayersInRange(Globals.MaxUpdateRange)) {
		//				if (viewer.Conn!=null && (viewer==this || viewer.CanSee(this))) {
		//					if (viewer==this || viewer.plevel>=Globals.plevelToSeeRealStats) {
		//						if (groupReal==null) {
		//							groupReal = new PacketGroup();
		//							PacketSender.PrepareUpdateHitpoints(this, true);
		//						}
		//						groupReal.SendTo(viewer.Conn);
		//					} else {
		//						if (groupScaled==null) {
		//							groupScaled = new PacketGroup();
		//							PacketSender.PrepareUpdateHitpoints(this, false);
		//						}
		//						groupScaled.SendTo(viewer.Conn);
		//					}
		//				}
		//			}
		//			if (groupReal!=null) groupReal.Dispose();
		//			if (groupScaled!=null) groupScaled.Dispose();
		//		}
		//		
		//		/*
		//			Method: SendUpdateMana
		//				This sends the current and maximum mana of this character,
		//				to all clients who can see this character and are close enough for the character to be in
		//				their update range.
		//				
		//				For sending to everyone but this character and GMs (with plevel >= plevelToSeeRealStats),
		//				the stats are scaled to [0-255] to hide their real values.
		//				
		//				If you need to update more than just mana and maxmana, you should use SendUpdateStats,
		//				which would be more efficient than calling this method plus another.
		//		*/
		//		public void SendUpdateMana() {
		//			PacketGroup groupScaled = null;
		//			PacketGroup groupReal = null;
		//			
		//			foreach (AbstractCharacter viewer in PlayersInRange(Globals.MaxUpdateRange)) {
		//				if (viewer.Conn!=null && (viewer==this || viewer.CanSee(this))) {
		//					if (viewer==this || viewer.plevel>=Globals.plevelToSeeRealStats) {
		//						if (groupReal==null) {
		//							groupReal = new PacketGroup();
		//							PacketSender.PrepareUpdateMana(this, true);
		//						}
		//						groupReal.SendTo(viewer.Conn);
		//					} else {
		//						if (groupScaled==null) {
		//							groupScaled = new PacketGroup();
		//							PacketSender.PrepareUpdateMana(this, false);
		//						}
		//						groupScaled.SendTo(viewer.Conn);
		//					}
		//				}
		//			}
		//			if (groupReal!=null) groupReal.Dispose();
		//			if (groupScaled!=null) groupScaled.Dispose();
		//		}
		//		
		//		/*
		//			Method: SendUpdateStamina
		//				This sends the current and maximum stamina of this character,
		//				to all clients who can see this character and are close enough for the character to be in
		//				their update range.
		//				
		//				For sending to everyone but this character and GMs (with plevel >= plevelToSeeRealStats),
		//				the stats are scaled to [0-255] to hide their real values.
		//				
		//				If you need to update more than just stamina and maxstamina, you should use SendUpdateStats,
		//				which would be more efficient than calling this method plus another.
		//		*/
		//		public void SendUpdateStamina() {
		//			PacketGroup groupScaled = null;
		//			PacketGroup groupReal = null;
		//			
		//			foreach (AbstractCharacter viewer in PlayersInRange(Globals.MaxUpdateRange)) {
		//				if (viewer.Conn!=null && (viewer==this || viewer.CanSee(this))) {
		//					if (viewer==this || viewer.plevel>=Globals.plevelToSeeRealStats) {
		//						if (groupReal==null) {
		//							groupReal = new PacketGroup();
		//							PacketSender.PrepareUpdateStamina(this, true);
		//						}
		//						groupReal.SendTo(viewer.Conn);
		//					} else {
		//						if (groupScaled==null) {
		//							groupScaled = new PacketGroup();
		//							PacketSender.PrepareUpdateStamina(this, false);
		//						}
		//						groupScaled.SendTo(viewer.Conn);
		//					}
		//				}
		//			}
		//			if (groupReal!=null) groupReal.Dispose();
		//			if (groupScaled!=null) groupScaled.Dispose();
		//		}

		public bool IsStandingOn(AbstractItem i) {
			int zdiff = Math.Abs(i.Z - Z);
			return (X == i.X && Y == i.Y && zdiff >= 0 && zdiff <= i.Height);
		}

		public bool IsStandingOn_CheckZOnly(AbstractItem i) {
			int zdiff = Math.Abs(i.Z - Z);
			Sanity.IfTrueThrow(X != i.X || Y != i.Y, "IsStandingOn_CheckZOnly called when the item is not actually on the same tile. " + this + " is at (" + X + "," + Y + "), and the item (" + i + ") is at (" + i.X + "," + i.Y + ").");
			return (zdiff >= 0 && zdiff <= i.Height);
		}

		public void Trigger_NewPosition() {
			this.TryTrigger(TriggerKey.newPosition, null);
			try {
				this.On_NewPosition();
			} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }

			foreach (AbstractItem itm in GetMap().GetItemsInRange(this.X, this.Y, 0)) {
				if (IsStandingOn(itm)) {
					itm.Trigger_Step(this, 0);
				}
			}
		}

		public virtual void On_NewPosition() {
		}

		public virtual bool On_ItemStep(AbstractItem i, bool repeated) {
			return false;
		}

		/**
			Attaches this character to an account and makes them a player.
			This could be used by the 'control' command.
			Used by playercreation script
				
			@param acc The account to attach them to.
		*/
		public int MakeBePlayer(AbstractAccount acc) {
			int slot;
			if (acc.AttachCharacter(this, out slot)) {
				CharSyncQueue.Resend(this);
				this.account = acc;
				GetMap().MadeIntoPlayer(this);
			} else {
				throw new SEException("That account (" + acc + ") is already full.");
			}
			return slot;
		}

		public void Resync() {
			GameState state = this.GameState;
			if (state != null) {
				TCPConnection<GameState> conn = state.Conn;

				PacketGroup pg = PacketGroup.AcquireSingleUsePG();

				pg.AcquirePacket<SeasonalInformationOutPacket>().Prepare(this.Season, this.Cursor);
				pg.AcquirePacket<SetFacetOutPacket>().Prepare(this.GetMap().Facet);
				pg.AcquirePacket<DrawGamePlayerOutPacket>().Prepare(state, this);
				pg.AcquirePacket<ClientViewRangeOutPacket>().Prepare(state.UpdateRange);
				//(Not To-do, or to-do much later on): 0xbf map patches (INI flag) (.. We don't need this on custom maps, and it makes it more complicated to load the maps/statics)
				//(TODO): 0x4e and 0x4f personal and global light levels
				conn.SendPacketGroup(pg);

				PreparedPacketGroups.SendWarMode(conn, this.Flag_WarMode);

				PacketSequences.SendCharInfoWithPropertiesTo(this, state, conn, this);

				this.SendNearbyStuffTo(state, conn);

				foreach (AbstractItem con in OpenedContainers.GetOpenedContainers(this)) {
					if (!con.IsEmpty) {
						PacketSequences.SendContainerContentsWithPropertiesTo(this, state, conn, con);
					}
				}
			}
		}

		internal override void Trigger_Destroy() {
			AbstractAccount acc = this.Account;
			if (acc != null) {
				GameState state = acc.GameState;
				if (state != null) {
					state.Conn.Close("Character being deleted");
				}

				acc.DetachCharacter(this);
			}

			foreach (AbstractItem i in this) {
				i.InternalDeleteNoRFV();//no updates, because it will disappear entirely
			}

			base.Trigger_Destroy();
			instances--;
			this.GetMap().Remove(this);
			Thing.MarkAsLimbo(this);
		}

		public void EmptyCont() {
			ThrowIfDeleted();
			foreach (AbstractItem i in this) {
				i.InternalDelete();
			}
		}

		public abstract AbstractItem AddBackpack();

		public AbstractItem Backpack {
			get {
				AbstractItem foundPack = null;
				if (visibleLayers != null) {
					foundPack = (AbstractItem) visibleLayers.FindByZ((int) LayerNames.Pack);
				}
				if (foundPack == null) {
					foundPack = AddBackpack();
				}
				return foundPack;
			}
		}

		public override sealed bool IsChar {
			get {
				return true;
			}
		}

		public override sealed bool CanContain {
			get {
				return true;
			}
		}

		public Gump SendGump(Thing focus, GumpDef gumpDef, DialogArgs args) {
			GameState state = this.GameState;
			if (state != null) {
				Gump instance = gumpDef.InternalConstruct(focus, this, args);
				if (instance != null) {
					state.SentGump(instance);
					SendGumpMenuDialogPacket p = Pool<SendGumpMenuDialogPacket>.Acquire();
					p.Prepare(instance);
					state.Conn.SendSinglePacket(p);
					return instance;
				}
			}
			return null;
		}

		public void DialogClose(Gump instance, int buttonId) {
			this.DialogClose(instance.uid, buttonId);
		}

		public void DialogClose(uint gumpUid, int buttonId) {
			GameState state = this.GameState;
			if (state != null) {
				CloseGenericGumpOutPacket p = Pool<CloseGenericGumpOutPacket>.Acquire();
				p.Prepare(gumpUid, buttonId);
				state.Conn.SendSinglePacket(p);
			}
		}

		public void DialogClose(GumpDef def, int buttonId) {
			GameState state = this.GameState;
			if (state != null) {
				foreach (Gump gi in state.FindGumpInstances(def)) {
					this.DialogClose(gi.uid, buttonId);
				}
			}
		}

		[Summary("Look into the dialogs dictionary and find out whether the desired one is opened" +
				"(visible) or not")]
		public bool HasOpenedDialog(GumpDef def) {
			GameState state = this.GameState;
			if (state != null) {
				if (state.FindGumpInstances(def).Count != 0) {
					//nasli jsme otevrenou instanci tohoto gumpu
					return true;
				}
			}
			return false;
		}

		internal override sealed bool Trigger_SpecificClick(AbstractCharacter clickingChar, ScriptArgs sa) {
			//helper method for Trigger_Click
			bool cancel = false;
			cancel = clickingChar.TryCancellableTrigger(TriggerKey.charClick, sa);
			if (!cancel) {
				clickingChar.act = this;
				try {
					cancel = clickingChar.On_CharClick(this);
				} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
			}
			return cancel;
		}

		public virtual bool On_ItemClick(AbstractItem clickedOn) {
			//I clicked an item
			return false;
		}

		public virtual bool On_CharClick(AbstractCharacter clickedOn) {
			//I clicked a char
			return false;
		}

		//This is called directly by InPackets if the client directly requests the paperdoll
		//(dclick with the 0x80000000 flag), so that if they use the paperdoll macro or the paperdoll button
		//while mounted, they won't dismount.
		//public void ShowPaperdollTo(GameConn conn) {
		//    if (conn != null) {
		//        AbstractCharacter connChar = conn.CurCharacter;
		//        if (connChar != null) {
		//            bool canEquip = true;
		//            if (connChar != this) {
		//                canEquip = connChar.CanEquipItemsOn(this);
		//            }
		//            PacketSender.PreparePaperdoll(this, canEquip);
		//            PacketSender.SendTo(conn, true);
		//        }
		//    }
		//}


		public void ShowPaperdollToSrc() {
			this.ShowPaperdollTo(Globals.SrcCharacter);
		}

		public void ShowPaperdollTo(AbstractCharacter viewer) {
			if (viewer != null) {
				GameState state = viewer.GameState;
				if (state != null) {
					this.ShowPaperdollTo(viewer, state.Conn);
				}
			}
		}

		public void ShowPaperdollTo(AbstractCharacter viewer, TCPConnection<GameState> conn) {
			bool canEquip = true;
			if (viewer != this) {
				canEquip = viewer.CanEquipItemsOn(this);
			}
			OpenPaperdollOutPacket packet = Pool<OpenPaperdollOutPacket>.Acquire();
			packet.Prepare(this, canEquip);
			conn.SendSinglePacket(packet);
		}

		public virtual bool CanEquipItemsOn(AbstractCharacter targetChar) {
			return true;
		}

		//this method fires the [speech] triggers
		internal void Trigger_Hear(AbstractCharacter speaker, string speech, uint clilocSpeech,
				SpeechType type, ushort color, ushort font, string lang, int[] keywords, string[] args) {

			ScriptArgs sa = new ScriptArgs(speaker, speech, clilocSpeech, type, color, font, lang, keywords, args);
			this.TryTrigger(TriggerKey.hear, sa);
			object[] saArgv = sa.argv;

			speech = ConvertTools.ToString(saArgv[1]);
			clilocSpeech = ConvertTools.ToUInt32(saArgv[2]);
			type = (SpeechType) ConvertTools.ToInt64(saArgv[3]);
			color = ConvertTools.ToUInt16(saArgv[4]);
			font = ConvertTools.ToUInt16(saArgv[5]);
			lang = ConvertTools.ToString(saArgv[6]);
			keywords = (int[]) saArgv[7];
			args = (string[]) saArgv[8];

			try {
				this.On_Hear(speaker, speech, clilocSpeech, type, color, font, lang, keywords, args);
			} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }

			GameState state = this.GameState;
			if (state != null) {
				if (speech == null) {
					ClilocMessageOutPacket packet = Pool<ClilocMessageOutPacket>.Acquire();
					packet.Prepare(speaker, clilocSpeech, speaker.Name, type, font, color, string.Join("\t", args));
					state.Conn.SendSinglePacket(packet);
				} else {
					PacketSequences.InternalSendMessage(state.Conn, speaker, speech, speaker.Name, type, font, color, lang);
				}
			}
		}

		public virtual void On_Hear(AbstractCharacter speaker, string speech, uint clilocSpeech, SpeechType type, ushort color, ushort font, string lang, int[] keywords, string[] args) {

		}

		internal void Trigger_Say(string speech, SpeechType type, int[] keywords) {
			if (this.IsPlayer) {
				ScriptArgs sa = new ScriptArgs(speech, type, keywords);
				this.TryTrigger(TriggerKey.say, sa);
				try {
					this.On_Say(speech, type, keywords);
				} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
			}
		}

		public virtual void On_Say(string speech, SpeechType type, int[] keywords) {
		}

		public virtual void On_ItemDClick(AbstractItem dClicked) {
		}

		public virtual void On_CharDClick(AbstractCharacter dClicked) {
		}

		public virtual bool On_DenyItemDClick(DenyClickArgs args) {
			DenyResult result = args.clickingChar.CanReach(args.target);
			args.Result = result;
			return result != DenyResult.Allow;
		}

		public virtual bool On_DenyCharDClick(DenyClickArgs args) {
			//default implementation only for item... char can be dclicked even if outa range (paperdoll)
			return false;
		}

		public abstract void Trigger_PlayerAttackRequest(AbstractCharacter target);

		/*
			Method: CanRename
				Determines if this character can rename another character.
			Parameters:
				to - The character to be renamed
			Returns:
				True if 'to' is an NPC with an owner, and this is its owner.
		*/
		public abstract bool CanRename(AbstractCharacter to);

		//The client apparently doesn't want characters' uids to be flagged with anything.
		public sealed override uint FlaggedUid {
			get {
				return (uint) this.Uid;
			}
		}

		//Commands

		public void Anim(int anim) {
			this.Anim(anim, 1, false, false, 0x01);
		}
		public void Anim(int anim, byte frameDelay) {
			this.Anim(anim, 1, false, false, frameDelay);
		}

		public void Anim(int anim, bool backwards) {
			this.Anim(anim, 1, backwards, false, 0x01);
		}

		public void Anim(int anim, bool backwards, bool undo) {
			this.Anim(anim, 1, backwards, undo, 0x01);
		}

		public void Anim(int anim, bool backwards, byte frameDelay) {
			this.Anim(anim, 1, backwards, false, frameDelay);
		}

		public void Anim(int anim, bool backwards, bool undo, byte frameDelay) {
			this.Anim(anim, 1, backwards, undo, frameDelay);
		}

		public void Anim(int anim, ushort numAnims, bool backwards, bool undo, byte frameDelay) {
			CharacterAnimationOutPacket p = Pool<CharacterAnimationOutPacket>.Acquire();
			p.Prepare(this, (ushort) anim, numAnims, backwards, undo, frameDelay);
			GameServer.SendToClientsWhoCanSee(this, p);
		}

		//[Obsolete("Use the alternative from Networking namespace", false)]
		//public void ShowStatusBarTo(GameConn conn) {
		//    if (this.Equals(conn.CurCharacter)) {
		//        PacketSender.PrepareStatusBar(this, StatusBarType.Me);
		//    } else if (conn.CurCharacter.CanRename(this)) {
		//        PacketSender.PrepareStatusBar(this, StatusBarType.Pet);
		//    } else {
		//        PacketSender.PrepareStatusBar(this, StatusBarType.Other);
		//    }
		//    PacketSender.SendTo(conn, true);
		//}

		//[Obsolete("Use the alternative from Networking namespace", false)]
		//public void ShowSkillsTo(GameConn conn) {
		//    PacketSender.PrepareAllSkillsUpdate(Skills, conn.Version.displaySkillCaps);
		//    PacketSender.SendTo(conn, true);
		//}

		public void ShowStatusBarTo(AbstractCharacter viewer, TCPConnection<GameState> viewerConn) {
			StatusBarInfoOutPacket packet = Pool<StatusBarInfoOutPacket>.Acquire();

			if (this == viewer) {
				packet.Prepare(this, StatusBarType.Me);
			} else if (viewer.CanRename(this)) {
				packet.Prepare(this, StatusBarType.Pet);
			} else {
				packet.Prepare(this, StatusBarType.Other);
			}

			viewerConn.SendSinglePacket(packet);
		}

		public void ShowSkillsTo(TCPConnection<GameState> viewerConn, GameState viewerState) {
			SendSkillsOutPacket packet = Pool<SendSkillsOutPacket>.Acquire();
			packet.PrepareMultipleSkillsUpdate(this.Skills, viewerState.Version.displaySkillCaps);
			viewerConn.SendSinglePacket(packet);
		}

		//ISrc implementation
		public byte Plevel {
			get {
				AbstractAccount a = Account;
				if (a != null) {
					return a.PLevel;
				} else {
					return 0;
				}
			}
			set {
				AbstractAccount a = Account;
				if (a != null) {
					a.PLevel = value;
				}//else ?
			}
		}
		public byte MaxPlevel {
			get {
				AbstractAccount a = Account;
				if (a != null) {
					return a.MaxPLevel;
				} else {
					return 0;
				}
			}
		}

		//public abstract ISkill[] Skills { get; }
		public abstract IEnumerable<ISkill> Skills { get; }
		public abstract void SetSkill(int id, ushort value);
		public abstract void SetSkillLockType(int id, SkillLockType type);
		public abstract ISkill GetSkillObject(int id);
		public abstract ushort GetSkill(int id);

		[Summary("Gets called by the core when the player presses the skill button/runs the macro")]
		public abstract void SelectSkill(AbstractSkillDef skillDef);

		public abstract byte StatLockByte { get; }
		public abstract StatLockType StrLock { get; set; }
		public abstract StatLockType DexLock { get; set; }
		public abstract StatLockType IntLock { get; set; }

		public abstract short Hits { get; set; }
		public abstract short MaxHits { get; set; }
		public abstract short Stam { get; set; }
		public abstract short MaxStam { get; set; }
		public abstract short Mana { get; set; }
		public abstract short MaxMana { get; set; }
		public abstract short Str { get; set; }
		public abstract short Dex { get; set; }
		public abstract short Int { get; set; }
		public abstract ulong Gold { get; }
		public abstract short Experience { get; set; }
		//Tithing points can be reduced if you do something to displease your god, like murdering innocents.
		//They can even go negative (but not from using powers, which work only with enough points).
		//You only gain points by sacrificing gold (that balances this with other magic classes since this has no reagent costs).
		public abstract short TithingPoints { get; set; }
		[Summary("Displays in client status as Physical resistance by default")]
		public abstract short StatusArmorClass { get; }
		[Summary("Displays in client status as Energy resistance by default")]
		public abstract short StatusMindDefense { get; }

		//Resistances do not have effs. Negative values impart penalties.
		[Summary("Displays in client status as Fire resistance by default")]
		public abstract short ExtendedStatusNum01 { get; }
		[Summary("Displays in client status as Cold resistance by default")]
		public abstract short ExtendedStatusNum02 { get; }
		[Summary("Displays in client status as Poison resistance by default")]
		public abstract short ExtendedStatusNum03 { get; }
		[Summary("Displays in client status as Luck by default")]
		public abstract short ExtendedStatusNum04 { get; }
		[Summary("Displays in client status as Min damage by default")]
		public abstract short ExtendedStatusNum05 { get; }
		[Summary("Displays in client status as Max damage by default")]
		public abstract short ExtendedStatusNum06 { get; }

		[Summary("Displays in client status as Current pet count by default")]
		public abstract byte ExtendedStatusNum07 { get; }
		[Summary("Displays in client status as Max pet count by default")]
		public abstract byte ExtendedStatusNum08 { get; }
		[Summary("Displays in client status as Stat cap by default")]
		public abstract ushort ExtendedStatusNum09 { get; }

		public abstract HighlightColor GetHighlightColorFor(AbstractCharacter viewer);

		public void CancelTarget() {
			GameState state = this.GameState;
			if (state != null) {
				PreparedPacketGroups.SendCancelTargettingCursor(state.Conn);
			}
		}

		public void SendNearbyStuff() {
			GameState state = this.GameState;
			if (state != null) {
				TCPConnection<GameState> conn = state.Conn;
				this.SendNearbyStuffTo(state, conn);
			}
		}

		private void SendNearbyStuffTo(GameState state, TCPConnection<GameState> conn) {
			ImmutableRectangle rect = new ImmutableRectangle(this, this.UpdateRange);
			Map map = this.GetMap();

			foreach (AbstractItem item in map.GetItemsInRectangle(rect)) {
				if (this.CanSeeForUpdate(item)) {
					item.GetOnGroundUpdater().SendTo(this, state, conn);

					PacketSequences.TrySendPropertiesTo(state, conn, item);
				}
			}
			foreach (AbstractCharacter ch in map.GetCharsInRectangle(rect)) {
				if ((this != ch) && this.CanSeeForUpdate(ch)) {
					PacketSequences.SendCharInfoWithPropertiesTo(this, state, conn, ch);

					UpdateCurrentHealthOutPacket packet = Pool<UpdateCurrentHealthOutPacket>.Acquire();
					packet.Prepare(ch.FlaggedUid, ch.Hits, ch.MaxHits, false);
					conn.SendSinglePacket(packet);
				}
			}
			if (state.AllShow) {
				foreach (Thing thing in map.GetDisconnectsInRectangle(rect)) {
					if ((this != thing) && this.CanSeeForUpdate(thing)) {
						AbstractItem item = thing as AbstractItem;
						if (item != null) {
							item.GetOnGroundUpdater().SendTo(this, state, conn);

							PacketSequences.TrySendPropertiesTo(state, conn, item);
						} else {
							AbstractCharacter ch = (AbstractCharacter) thing;

							PacketSequences.SendCharInfoWithPropertiesTo(this, state, conn, ch);

							UpdateCurrentHealthOutPacket packet = Pool<UpdateCurrentHealthOutPacket>.Acquire();
							packet.Prepare(ch.FlaggedUid, ch.Hits, ch.MaxHits, false);
							conn.SendSinglePacket(packet);
						}
					}
				}

			}
		}

		//ISrc implementation
		public AbstractCharacter Character {
			get {
				return this;
			}
		}

		public int VisibleCount {
			get {
				if (visibleLayers == null) {
					return 0;
				} else {
					return visibleLayers.count;
				}
			}
		}

		public IEnumerable<Thing> GetVisibleEquip() {
			if (visibleLayers == null) {
				return EmptyReadOnlyGenericCollection<Thing>.instance;
			} else {
				return visibleLayers;
			}
		}

		public override void InvalidateProperties() {
			CharSyncQueue.PropertiesChanged(this);
			base.InvalidateProperties();
		}

		public int InvisibleCount {
			get {
				int count = 0;
				if (invisibleLayers != null) {
					count = invisibleLayers.count;
				}
				if (specialLayer != null) {
					count += specialLayer.count;
				}
				if (draggingLayer != null) {
					count++;
				}
				return count;
			}
		}

		public override sealed IEnumerator<AbstractItem> GetEnumerator() {
			ThrowIfDeleted();
			return new EquipsEnumerator(this);
		}
	}

	public class EquipsEnumerator : IEnumerator<AbstractItem> {
		private const int STATE_VISIBLES = 0;
		private const int STATE_DRAGGING = 1;
		private const int STATE_SPECIAL = 2;
		private const int STATE_OTHERS = 3;
		private int state;
		AbstractCharacter cont;
		AbstractItem current;
		AbstractItem next;

		public EquipsEnumerator(AbstractCharacter c) {
			cont = c;
			state = STATE_VISIBLES;
			current = null;
			next = null;
		}

		public void Reset() {
			state = STATE_VISIBLES;
			current = null;
			next = null;
		}

		public bool MoveNext() {
			switch (state) {
				case STATE_VISIBLES:
					if (cont.visibleLayers != null) {
						if (current == null) {//it just started
							current = (AbstractItem) cont.visibleLayers.firstThing;
							if (current != null) {
								next = (AbstractItem) current.nextInList;
								return true;
							} else {
								next = null;
							}
						} else {
							if (next != null) {
								current = next;
								next = (AbstractItem) current.nextInList;
								return true;
							} else {
								current = null;//continue on next state
							}
						}
					}
					state = STATE_DRAGGING;
					goto case STATE_DRAGGING;
				case STATE_DRAGGING:
					if ((next == null) && (current == null)) {//first time
						if (cont.draggingLayer != null) {
							current = cont.draggingLayer;
							if (current != null) {
								return true;
							}
						}
					} else {
						current = null;//continue on next state
					}
					state = STATE_SPECIAL;
					goto case STATE_SPECIAL;
				case STATE_SPECIAL:
					if (cont.specialLayer != null) {
						if (current == null) {//it just started
							current = (AbstractItem) cont.specialLayer.firstThing;
							if (current != null) {
								next = (AbstractItem) current.nextInList;
								return true;
							} else {
								next = null;
							}
						} else {
							if (next != null) {
								current = next;
								next = (AbstractItem) current.nextInList;
								return true;
							} else {
								current = null;//continue on next state
							}
						}
					}
					state = STATE_OTHERS;
					goto case STATE_OTHERS;
				case STATE_OTHERS:
					if (cont.invisibleLayers != null) {
						if (current == null) {//it just started
							current = (AbstractItem) cont.invisibleLayers.firstThing;
							if (current != null) {
								next = (AbstractItem) current.nextInList;
								return true;
							} else {
								next = null;
							}
						} else {
							if (next != null) {
								current = next;
								next = (AbstractItem) current.nextInList;
								return true;
							}
						}
					}
					break;
			}
			return false;
		}

		public void Dispose() {
		}

		public AbstractItem Current {
			get {
				return current;
			}
		}

		object IEnumerator.Current {
			get {
				return current;
			}
		}
	}
}
