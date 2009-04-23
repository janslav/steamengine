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
using SteamEngine.CompiledScripts;

namespace SteamEngine {

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

	/**
		A character.
	*/
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix")]
	public abstract partial class AbstractCharacter : Thing, ISrc {
		internal const int sentLayers = 25;//0-24

		//In most cases, you should use Flag_* properties to set/get flags. Rarely, you may need to directly modify flags,
		//	in the SE core (not in scripts), which is why this is internal.
		//But whatever you do, NEVER set the disconnected flag by tweaking flags.
		private enum DirectionAndFlag : short {
			Zero = 0,
			DirectionMask = 0x0007,
			DirFlagDisconnected = 0x0008,
			DirFlagMoving = 0x0010,
			DirFlag1 = 0x0020,
			DirFlag2 = 0x0040,
			DirFlag3 = 0x0080,
			DirFlag4 = 0x0100,
			DirFlag5 = 0x0200,
			DirFlag6 = 0x0400
		}

		private AbstractAccount account;
		private string name;
		private DirectionAndFlag directionAndFlags = DirectionAndFlag.Zero;
		internal ThingLinkedList visibleLayers;//layers 0..24
		internal ThingLinkedList invisibleLayers;//layers (26..29) + (32..max)
		internal ThingLinkedList specialLayer;//layer 30
		internal AbstractItem draggingLayer;

		private static int instances;

		internal CharSyncQueue.CharState syncState; //don't touch

		protected AbstractCharacter(ThingDef myDef)
			: base(myDef) {
			instances++;
			this.name = myDef.Name;
			Globals.LastNewChar = this;
		}

		protected AbstractCharacter(AbstractCharacter copyFrom)
			: base(copyFrom) { //copying constuctor
			instances++;
			this.account = copyFrom.account;
			this.name = copyFrom.name;
			this.directionAndFlags = copyFrom.directionAndFlags;
			Globals.LastNewChar = this;
			Map.GetMap(this.point4d.m).Add(this);
		}

		public static int Instances {
			get {
				return instances;
			}
		}

		[Summary("The Direction this character is facing.")]
		public override sealed Direction Direction {
			get {
				return (Direction) this.DirectionByte;
			}
			set {
				this.DirectionByte = (byte) value;
			}
		}

		[Summary("Direction, as a byte.")]
		internal byte DirectionByte {
			get {
				return (byte) (this.directionAndFlags & DirectionAndFlag.DirectionMask);
			}
			set {
				DirectionAndFlag newValue = (this.directionAndFlags & ~DirectionAndFlag.DirectionMask) | ((DirectionAndFlag) value & DirectionAndFlag.DirectionMask);
				if (this.directionAndFlags != newValue) {
					CharSyncQueue.AboutToChangeDirection(this, false);
					this.directionAndFlags = newValue;
				}
			}
		}

		public AbstractCharacterDef TypeDef {
			get {
				return (AbstractCharacterDef) base.def;
			}
		}

		public override bool IsPlayer {
			get {
				return (this.Account != null);
			}
		}

		public abstract bool IsFemale { get; }
		public abstract bool IsMale { get; }

		//Used with NetState, but at present this should be set when the character moves (walk/run/fly), and should remain
		//set for the rest of the cycle. Maybe there's a potential use for that in scripts, so this is public.
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public bool Flag_Moving {
			get {
				return (this.directionAndFlags & DirectionAndFlag.DirFlagMoving) == DirectionAndFlag.DirFlagMoving;
			}
			set {
				if (value) {
					this.directionAndFlags |= DirectionAndFlag.DirFlagMoving;
				} else {
					this.directionAndFlags &= ~DirectionAndFlag.DirFlagMoving;
				}
			}
		}

		protected bool ProtectedFlag1 {
			get {
				return ((this.directionAndFlags & DirectionAndFlag.DirFlag1) == DirectionAndFlag.DirFlag1);
			}
			set {
				if (value) {
					this.directionAndFlags |= DirectionAndFlag.DirFlag1;
				} else {
					this.directionAndFlags &= ~DirectionAndFlag.DirFlag1;
				}
			}
		}

		protected bool ProtectedFlag2 {
			get {
				return ((this.directionAndFlags & DirectionAndFlag.DirFlag2) == DirectionAndFlag.DirFlag2);
			}
			set {
				if (value) {
					this.directionAndFlags |= DirectionAndFlag.DirFlag2;
				} else {
					this.directionAndFlags &= ~DirectionAndFlag.DirFlag2;
				}
			}
		}

		protected bool ProtectedFlag3 {
			get {
				return ((this.directionAndFlags & DirectionAndFlag.DirFlag3) == DirectionAndFlag.DirFlag3);
			}
			set {
				if (value) {
					this.directionAndFlags |= DirectionAndFlag.DirFlag3;
				} else {
					this.directionAndFlags &= ~DirectionAndFlag.DirFlag3;
				}
			}
		}

		protected bool ProtectedFlag4 {
			get {
				return ((this.directionAndFlags & DirectionAndFlag.DirFlag4) == DirectionAndFlag.DirFlag4);
			}
			set {
				if (value) {
					this.directionAndFlags |= DirectionAndFlag.DirFlag4;
				} else {
					this.directionAndFlags &= ~DirectionAndFlag.DirFlag4;
				}
			}
		}

		protected bool ProtectedFlag5 {
			get {
				return ((this.directionAndFlags & DirectionAndFlag.DirFlag5) == DirectionAndFlag.DirFlag5);
			}
			set {
				if (value) {
					this.directionAndFlags |= DirectionAndFlag.DirFlag5;
				} else {
					this.directionAndFlags &= ~DirectionAndFlag.DirFlag5;
				}
			}
		}

		protected bool ProtectedFlag6 {
			get {
				return ((this.directionAndFlags & DirectionAndFlag.DirFlag6) == DirectionAndFlag.DirFlag6);
			}
			set {
				if (value) {
					this.directionAndFlags |= DirectionAndFlag.DirFlag6;
				} else {
					this.directionAndFlags &= ~DirectionAndFlag.DirFlag6;
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

		public int MountItem {
			get {
				return this.TypeDef.MountItem;
			}
		}

		public abstract AbstractCharacter Mount { get; set; }

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public abstract bool Flag_WarMode { get; set; }
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public abstract bool Flag_Riding { get; }

		public abstract string PaperdollName { get; }

		public override string Name {
			get {
				return this.name;
			}
			set {
				this.InvalidateProperties();
				CharSyncQueue.AboutToChangeName(this);
				this.name = value;
			}
		}

		public AbstractAccount Account {
			get {
				return this.account;
			}
		}

		//public GameConn Conn {
		//    get {
		//        if (this.account != null) {
		//            return this.account.Conn;
		//        } else {
		//            return null;
		//        }
		//    }
		//}

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

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public abstract bool Flag_Insubst {
			get;
			set;
		}

		public override sealed void Resend() {
			CharSyncQueue.Resend(this);
		}

		public override bool Flag_Disconnected {
			get {
				return ((this.directionAndFlags & DirectionAndFlag.DirFlagDisconnected) == DirectionAndFlag.DirFlagDisconnected);
			}
		}

		private void SetFlag_Disconnected(bool value) {
			DirectionAndFlag newValue = (value ? (this.directionAndFlags | DirectionAndFlag.DirFlagDisconnected) : (this.directionAndFlags & ~DirectionAndFlag.DirFlagDisconnected));

			if (this.directionAndFlags != newValue) {
				CharSyncQueue.AboutToChangeVisibility(this);
				this.directionAndFlags = newValue;
				if (value) {
					this.GetMap().Disconnected(this);
				} else {
					this.GetMap().Reconnected(this);
				}
			}
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

		//is called by GameConn when relinking, which is after the Things have been loaded, but before
		//they're put into map
		internal void ReLinkToGameState() {
			this.directionAndFlags &= ~DirectionAndFlag.DirFlagDisconnected; //we're not disconnected, the Map needs to know
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
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
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
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		internal void Trigger_LogOut() {
			this.TryTrigger(TriggerKey.logout, null);
			try {
				this.On_LogOut();
			} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual void On_LogOut() {

		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual bool On_LogIn() {
			return false;
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
		public override void Save(SaveStream output) {
			if (this.account != null) {
				output.WriteValue("account", this.account);
			}
			if (!this.Def.Name.Equals(this.name)) {
				output.WriteValue("name", this.name);
			}
			DirectionAndFlag flagsToSave = this.directionAndFlags;
			if (this.IsPlayer) {
				flagsToSave = flagsToSave | DirectionAndFlag.DirFlagDisconnected;//add the Disconnected flag, makes no sense to save a person "connected"
			}
			if (flagsToSave != 0) {
				output.WriteValue("directionAndFlags", flagsToSave);
			}
			base.Save(output);
		}

		//For loading.
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1720:AvoidTypeNamesInParameters", MessageId = "0#")]
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
			if (this.IsPlayer) {
				if (this.GameState == null) {
					this.directionAndFlags |= DirectionAndFlag.DirFlagDisconnected;
				} else {	//there shouldn't actually be anyone connected when this happens, but we check just in case.
					this.directionAndFlags &= ~DirectionAndFlag.DirFlagDisconnected;
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
						this.name = ma.Groups["value"].Value;
					} else {
						this.name = valueString;
					}
					break;
				case "directionandflags":
					this.directionAndFlags = (DirectionAndFlag) TagMath.ParseByte(valueString);
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
		public void WriteLine(string line) {
			this.SysMessage(line);
		}

		public Language Language {
			get {
				GameState state = this.GameState;
				if (state != null) {
					return state.Language;
				}
				return Language.Default;
			}
		}

		public void AnnounceBug() {
			this.ClilocSysMessage(501634, 0x21);	//Error!  This is a bug, please report it!
			//0x21 = red, hopefully
		}

		public void SysMessage(string arg) {
			GameState state = this.GameState;
			if (state != null) {
				PacketSequences.SendSystemMessage(state.Conn, arg, -1);
			}
		}

		public void SysMessage(string arg, int color) {
			GameState state = this.GameState;
			if (state != null) {
				PacketSequences.SendSystemMessage(state.Conn, arg, color);
			}
		}

		public void ClilocSysMessage(int msg, string args) {
			GameState state = this.GameState;
			if (state != null) {
				PacketSequences.SendClilocSysMessage(state.Conn, msg, -1, args);
			}
		}
		public void ClilocSysMessage(int msg, params string[] args) {
			GameState state = this.GameState;
			if (state != null) {
				PacketSequences.SendClilocSysMessage(state.Conn, msg, -1, args);
			}
		}

		public void ClilocSysMessage(int msg, int color) {
			GameState state = this.GameState;
			if (state != null) {
				PacketSequences.SendClilocSysMessage(state.Conn, msg, color, "");
			}
		}

		public void ClilocSysMessage(int msg, int color, string args) {
			GameState state = this.GameState;
			if (state != null) {
				PacketSequences.SendClilocSysMessage(state.Conn, msg, color, args);
			}
		}

		public void ClilocSysMessage(int msg, int color, params string[] args) {
			GameState state = this.GameState;
			if (state != null) {
				PacketSequences.SendClilocSysMessage(state.Conn, msg, color, args);
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
				throw new SEException("Not supported");
			}
		}

		//player or npc walking
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		public bool WalkRunOrFly(Direction dir, bool running, bool requested) {
			ThrowIfDeleted();
			this.Flag_Moving = true;
			dir = dir & Direction.Mask;

			if (this.Direction == dir) { //no dir change = step forward
				Point4D oldPoint = new Point4D(this.point4d);

				Map map = this.GetMap();
				int newZ, newY, newX;

				bool canMoveEverywhere = (this.IsPlayer && this.Plevel >= Globals.PlevelOfGM);
				if (!map.CheckMovement(oldPoint, this.MovementSettings, dir, canMoveEverywhere, out newX, out newY, out newZ)) {
					return false;
				}

				//move char, and send 0x77 to players nearby

				if (this.TryCancellableTrigger(TriggerKey.step, new ScriptArgs(dir, running))) {
					return false;
				}

				bool onStepCancelled = false;
				try {
					this.On_Step(dir, running);
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

				MovementType mt;
				if (running) {
					mt = MovementType.Running;
				} else {
					mt = MovementType.Walking;
				}
				if (requested) {
					mt |= MovementType.RequestedStep;
				}
				CharSyncQueue.AboutToChangePosition(this, mt);

				this.point4d.SetP(newX, newY, newZ);
				ChangedP(oldPoint);
			} else { //just changing direction, no steps
				CharSyncQueue.AboutToChangeDirection(this, requested);
				this.Direction = dir;
			}
			return true;
		}

		internal override sealed void SetPosImpl(int x, int y, int z, byte m) {
			if (Map.IsValidPos(x, y, m)) {
				CharSyncQueue.AboutToChangePosition(this, MovementType.Teleporting);
				Point4D oldP = this.P();
				Region oldRegion;
				if (Map.IsValidPos(oldP)) {
					oldRegion = Map.GetMap(oldP.M).GetRegionFor(oldP.X, oldP.Y);
				} else {
					oldRegion = StaticRegion.WorldRegion;
				}
				Region newRegion = Map.GetMap(m).GetRegionFor(x, y);
				if (oldRegion != newRegion) {
					Region.ExitAndEnter(oldRegion, newRegion, this);//forced exit & enter
				}
				this.point4d.SetP(x, y, z, m);
				ChangedP(oldP);
			} else {
				throw new SEException("Invalid position (" + x + "," + y + " on mapplane " + m + ")");
			}
		}

		private void ChangedP(Point4D oldP) {
			Map.ChangedP(this, oldP);
			AbstractCharacter self = this as AbstractCharacter;
			if (self != null) {
				self.Trigger_NewPosition(oldP);
			}
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual bool On_Step(Direction direction, bool running) {
			return false;
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
		public bool IsStandingOn(AbstractItem i) {
			int zdiff = Math.Abs(i.Z - Z);
			return (X == i.X && Y == i.Y && zdiff >= 0 && zdiff <= i.Height);
		}

		//public bool IsStandingOnCheckZOnly(AbstractItem i) {
		//    int zdiff = Math.Abs(i.Z - Z);
		//    Sanity.IfTrueThrow(X != i.X || Y != i.Y, "IsStandingOn_CheckZOnly called when the item is not actually on the same tile. " + this + " is at (" + X + "," + Y + "), and the item (" + i + ") is at (" + i.X + "," + i.Y + ").");
		//    return (zdiff >= 0 && zdiff <= i.Height);
		//}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public void Trigger_NewPosition(Point4D oldP) {
			this.TryTrigger(TriggerKey.newPosition, new ScriptArgs(oldP));
			try {
				this.On_NewPosition();
			} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }

			foreach (AbstractItem itm in this.GetMap().GetItemsInRange(this.X, this.Y, 0)) {
				if (IsStandingOn(itm)) {
					itm.Trigger_Step(this, false);
				}
			}
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual void On_NewPosition() {
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual bool On_ItemStep(AbstractItem i, bool repeated) {
			return false;
		}

		/**
			Attaches this character to an account and makes them a player.
			This could be used by the 'control' command.
			Used by playercreation script
				
			@param acc The account to attach them to.
		*/
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
		public int MakeBePlayer(AbstractAccount acc) {
			int slot;
			if (acc.AttachCharacter(this, out slot)) {
				CharSyncQueue.Resend(this);
				this.account = acc;
				this.GetMap().MadeIntoPlayer(this);
			} else {
				throw new SEException("That account (" + acc + ") is already full.");
			}
			return slot;
		}

		public void Resync() {
			GameState state = this.GameState;
			if (state != null) {
				TcpConnection<GameState> conn = state.Conn;

				//PacketGroup pg = PacketGroup.AcquireSingleUsePG();

				//pg.AcquirePacket<SeasonalInformationOutPacket>().Prepare(this.Season, this.Cursor);
				//pg.AcquirePacket<SetFacetOutPacket>().Prepare(this.GetMap().Facet);
				//pg.AcquirePacket<DrawGamePlayerOutPacket>().Prepare(state, this);
				//pg.AcquirePacket<ClientViewRangeOutPacket>().Prepare(state.UpdateRange);
				////(Not To-do, or to-do much later on): 0xbf map patches (INI flag) (.. We don't need this on custom maps, and it makes it more complicated to load the maps/statics)
				////(TODO): 0x4e and 0x4f personal and global light levels
				//conn.SendPacketGroup(pg);

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

		internal sealed override void Trigger_Destroy() {
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

			if (!this.IsLimbo) {
				this.GetMap().Remove(this);
				Thing.MarkAsLimbo(this);
			}
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
		public abstract AbstractItem GetBackpack();

		public void EmptyCont() {
			ThrowIfDeleted();
			foreach (AbstractItem i in this) {
				i.InternalDelete();
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
			this.DialogClose(instance.Uid, buttonId);
		}

		public void DialogClose(int gumpUid, int buttonId) {
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
					this.DialogClose(gi.Uid, buttonId);
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

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		internal override sealed bool Trigger_SpecificClick(AbstractCharacter clickingChar, ScriptArgs sa) {
			//helper method for Trigger_Click
			bool cancel = false;
			cancel = clickingChar.TryCancellableTrigger(TriggerKey.charClick, sa);
			if (!cancel) {
				try {
					cancel = clickingChar.On_CharClick(this);
				} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
			}
			return cancel;
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual bool On_ItemClick(AbstractItem clickedOn) {
			//I clicked an item
			return false;
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual bool On_CharClick(AbstractCharacter clickedOn) {
			//I clicked a char
			return false;
		}

		public void ShowPaperdollToSrc() {
			this.ShowPaperdollTo(Globals.SrcCharacter);
		}

		public void ShowPaperdollTo(AbstractCharacter viewer) {
			if (viewer != null) {
				GameState state = viewer.GameState;
				if (state != null) {
					this.ShowPaperdollTo(viewer, state, state.Conn);
				}
			}
		}

		public void ShowPaperdollTo(AbstractCharacter viewer, GameState viewerState, TcpConnection<GameState> viewerConn) {
			bool canEquip = true;
			if (viewer != this) {
				canEquip = viewer.CanEquipItemsOn(this);
			}
			OpenPaperdollOutPacket packet = Pool<OpenPaperdollOutPacket>.Acquire();
			packet.Prepare(this, canEquip);
			viewerConn.SendSinglePacket(packet);

			if (Globals.UseAosToolTips && viewerState.Version.AosToolTips) {
				foreach (AbstractItem equipped in this.VisibleEquip) {
					AosToolTips toolTips = equipped.GetAosToolTips();
					if (toolTips != null) {
						toolTips.SendIdPacket(viewerState, viewerConn);
					}
				}
			}
		}

		public virtual bool CanEquipItemsOn(AbstractCharacter target) {
			return true;
		}

		//this method fires the [speech] triggers
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		internal void Trigger_Hear(AbstractCharacter speaker, string speech, int clilocSpeech,
				SpeechType type, int color, ClientFont font, string lang, int[] keywords, string[] args) {

			ScriptArgs sa = new ScriptArgs(speaker, speech, clilocSpeech, type, color, font, lang, keywords, args);
			this.TryTrigger(TriggerKey.hear, sa);
			object[] saArgv = sa.Argv;

			speech = ConvertTools.ToString(saArgv[1]);
			clilocSpeech = ConvertTools.ToInt32(saArgv[2]);
			type = (SpeechType) ConvertTools.ToInt32(saArgv[3]);
			color = ConvertTools.ToInt32(saArgv[4]);
			font = (ClientFont) ConvertTools.ToInt32(saArgv[5]);
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

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual void On_Hear(AbstractCharacter speaker, string speech, int clilocSpeech, SpeechType type, int color, ClientFont font, string lang, int[] keywords, string[] args) {

		}

		//cancellable because of things like Guild speech, etc.
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		internal bool Trigger_Say(string speech, SpeechType type, int[] keywords) {
			bool cancel = false;
			if (this.IsPlayer) {
				ScriptArgs sa = new ScriptArgs(speech, type, keywords);
				cancel = this.TryCancellableTrigger(TriggerKey.say, sa);
				if (!cancel) {
					try {
						cancel = this.On_Say(speech, type, keywords);
					} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
				}
			}
			return cancel;
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual bool On_Say(string speech, SpeechType type, int[] keywords) {
			return false;
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual void On_ItemDClick(AbstractItem dClicked) {
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual void On_CharDClick(AbstractCharacter dClicked) {
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual bool On_DenyItemDClick(DenyClickArgs args) {
			DenyResult result = args.ClickingChar.CanReach(args.Target);
			args.Result = result;
			return result != DenyResult.Allow;
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual bool On_DenyCharDClick(DenyClickArgs args) {
			//default implementation only for item... char can be dclicked even if outa range (paperdoll)
			return false;
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
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
		[CLSCompliant(false)]
		public sealed override uint FlaggedUid {
			get {
				return (uint) this.Uid;
			}
		}

		//Commands
		public void Anim(int animId) {
			this.Anim(animId, 1, false, false, 0x01);
		}
		public void Anim(int animId, byte frameDelay) {
			this.Anim(animId, 1, false, false, frameDelay);
		}

		public void Anim(int animId, bool backwards) {
			this.Anim(animId, 1, backwards, false, 0x01);
		}

		public void Anim(int animId, bool backwards, bool undo) {
			this.Anim(animId, 1, backwards, undo, 0x01);
		}

		public void Anim(int animId, bool backwards, byte frameDelay) {
			this.Anim(animId, 1, backwards, false, frameDelay);
		}

		public void Anim(int animId, bool backwards, bool undo, byte frameDelay) {
			this.Anim(animId, 1, backwards, undo, frameDelay);
		}

		public void Anim(int animId, int numAnims, bool backwards, bool undo, byte frameDelay) {
			CharacterAnimationOutPacket p = Pool<CharacterAnimationOutPacket>.Acquire();
			p.Prepare(this, animId, numAnims, backwards, undo, frameDelay);
			GameServer.SendToClientsWhoCanSee(this, p);
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
		public void ShowStatusBarTo(AbstractCharacter viewer, TcpConnection<GameState> viewerConn) {
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

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
		public void ShowSkillsTo(TcpConnection<GameState> viewerConn, GameState viewerState) {
			SendSkillsOutPacket packet = Pool<SendSkillsOutPacket>.Acquire();
			packet.PrepareAllSkillsUpdate(this.Skills, viewerState.Version.DisplaySkillCaps);
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
		public abstract void SetSkill(int id, int value);
		public abstract void SetSkillLockType(int id, SkillLockType type);
		public abstract ISkill GetSkillObject(int id);
		public abstract int GetSkill(int id);

		[Summary("Gets called by the core when the player presses the skill button/runs the macro")]
		public abstract void SelectSkill(AbstractSkillDef skillDef);

		[Summary("Gets called by the core when the player presses the spell icon/runs the macro")]
		public abstract void TryCastSpellFromBook(int spellid);

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
		public abstract long Gold { get; }

		//Tithing points can be reduced if you do something to displease your god, like murdering innocents.
		//They can even go negative (but not from using powers, which work only with enough points).
		//You only gain points by sacrificing gold (that balances this with other magic classes since this has no reagent costs).
		public abstract short TithingPoints { get; set; }
		[Summary("Displays in client status as Physical resistance by default")]
		public virtual short StatusArmorClass { get { return 0; } }
		[Summary("Displays in client status as Energy resistance by default")]
		public virtual short StatusMindDefense { get { return 0; } }

		//Resistances do not have effs. Negative values impart penalties.
		[Summary("Displays in client status as Fire resistance by default")]
		public virtual short ExtendedStatusNum01 { get { return 0; } }
		[Summary("Displays in client status as Cold resistance by default")]
		public virtual short ExtendedStatusNum02 { get { return 0; } }
		[Summary("Displays in client status as Poison resistance by default")]
		public virtual short ExtendedStatusNum03 { get { return 0; } }
		[Summary("Displays in client status as Luck by default")]
		public virtual short ExtendedStatusNum04 { get { return 0; } }
		[Summary("Displays in client status as Min damage by default")]
		public virtual short ExtendedStatusNum05 { get { return 0; } }
		[Summary("Displays in client status as Max damage by default")]
		public virtual short ExtendedStatusNum06 { get { return 0; } }
		[Summary("Displays in client status as Current pet count by default")]
		public virtual byte ExtendedStatusNum07 { get { return 0; } }
		[Summary("Displays in client status as Max pet count by default")]
		public virtual byte ExtendedStatusNum08 { get { return 0; } }
		[Summary("Displays in client status as Stat cap by default")]
		public virtual short ExtendedStatusNum09 { get { return 0; } }

		public abstract HighlightColor GetHighlightColorFor(AbstractCharacter viewer);

		public virtual ICollection<AbstractCharacter> PartyMembers {
			get {
				return EmptyReadOnlyGenericCollection<AbstractCharacter>.instance;
			}
		}


		public void CancelTarget() {
			GameState state = this.GameState;
			if (state != null) {
				PreparedPacketGroups.SendCancelTargettingCursor(state.Conn);
			}
		}

		public void SendNearbyStuff() {
			GameState state = this.GameState;
			if (state != null) {
				TcpConnection<GameState> conn = state.Conn;
				this.SendNearbyStuffTo(state, conn);
			}
		}

		private void SendNearbyStuffTo(GameState state, TcpConnection<GameState> conn) {
			ImmutableRectangle rect = new ImmutableRectangle(this, this.UpdateRange);
			Map map = this.GetMap();

			foreach (Thing t in map.GetThingsInRectangle(rect)) {
				this.ProcessSendNearbyThing(state, conn, t);
			}

			if (state.AllShow) {
				foreach (Thing thing in map.GetDisconnectsInRectangle(rect)) {
					this.ProcessSendNearbyThing(state, conn, thing);
				}
			}
		}

		private void ProcessSendNearbyThing(GameState state, TcpConnection<GameState> conn, Thing t) {
			AbstractItem item = t as AbstractItem;
			if (item != null) {
				if (this.CanSeeForUpdate(item)) {
					item.GetOnGroundUpdater().SendTo(this, state, conn);
					PacketSequences.TrySendPropertiesTo(state, conn, item);
				}
			} else {
				AbstractCharacter ch = (AbstractCharacter) t;
				if ((this != ch) && this.CanSeeForUpdate(ch)) {
					PacketSequences.SendCharInfoWithPropertiesTo(this, state, conn, ch);
					UpdateCurrentHealthOutPacket packet = Pool<UpdateCurrentHealthOutPacket>.Acquire();
					packet.Prepare(ch.FlaggedUid, ch.Hits, ch.MaxHits, false);
					conn.SendSinglePacket(packet);
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
				if (this.visibleLayers == null) {
					return 0;
				} else {
					return this.visibleLayers.count;
				}
			}
		}

		public IEnumerable<Thing> VisibleEquip {
			get {
				if (this.visibleLayers == null) {
					return EmptyReadOnlyGenericCollection<Thing>.instance;
				} else {
					return this.visibleLayers;
				}
			}
		}

		public sealed override void InvalidateProperties() {
			CharSyncQueue.PropertiesChanged(this);
			base.InvalidateProperties();
		}

		public int InvisibleCount {
			get {
				int count = 0;
				if (this.invisibleLayers != null) {
					count = this.invisibleLayers.count;
				}
				if (this.specialLayer != null) {
					count += this.specialLayer.count;
				}
				if (this.draggingLayer != null) {
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

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly")]
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
			this.cont = c;
			//this.state = STATE_VISIBLES;
		}

		public void Reset() {
			this.state = STATE_VISIBLES;
			this.current = null;
			this.next = null;
		}

		public bool MoveNext() {
			switch (this.state) {
				case STATE_VISIBLES:
					if (this.cont.visibleLayers != null) {
						if (this.current == null) {//it just started
							this.current = (AbstractItem) this.cont.visibleLayers.firstThing;
							if (this.current != null) {
								this.next = (AbstractItem) this.current.nextInList;
								return true;
							} else {
								this.next = null;
							}
						} else {
							if (this.next != null) {
								this.current = this.next;
								this.next = (AbstractItem) current.nextInList;
								return true;
							} else {
								this.current = null;//continue on next state
							}
						}
					}
					this.state = STATE_DRAGGING;
					goto case STATE_DRAGGING;
				case STATE_DRAGGING:
					if ((this.next == null) && (this.current == null)) {//first time
						if (this.cont.draggingLayer != null) {
							this.current = this.cont.draggingLayer;
							if (this.current != null) {
								return true;
							}
						}
					} else {
						this.current = null;//continue on next state
					}
					this.state = STATE_SPECIAL;
					goto case STATE_SPECIAL;
				case STATE_SPECIAL:
					if (this.cont.specialLayer != null) {
						if (this.current == null) {//it just started
							this.current = (AbstractItem) this.cont.specialLayer.firstThing;
							if (this.current != null) {
								this.next = (AbstractItem) this.current.nextInList;
								return true;
							} else {
								this.next = null;
							}
						} else {
							if (this.next != null) {
								this.current = this.next;
								this.next = (AbstractItem) this.current.nextInList;
								return true;
							} else {
								this.current = null;//continue on next state
							}
						}
					}
					this.state = STATE_OTHERS;
					goto case STATE_OTHERS;
				case STATE_OTHERS:
					if (this.cont.invisibleLayers != null) {
						if (this.current == null) {//it just started
							this.current = (AbstractItem) this.cont.invisibleLayers.firstThing;
							if (this.current != null) {
								this.next = (AbstractItem) this.current.nextInList;
								return true;
							} else {
								this.next = null;
							}
						} else {
							if (this.next != null) {
								this.current = this.next;
								this.next = (AbstractItem) this.current.nextInList;
								return true;
							}
						}
					}
					break;
			}
			return false;
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly")]
		public void Dispose() {
		}

		public AbstractItem Current {
			get {
				return this.current;
			}
		}

		object IEnumerator.Current {
			get {
				return this.current;
			}
		}
	}
}
