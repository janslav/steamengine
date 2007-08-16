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
using System.Threading;
using System.Configuration;
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
		public const uint maxTimersPerChar = 24;
		public const uint numLayers = 31;
		public const uint sentLayers = 25;//0-24
		
		//For the AnimDuration method, which returns the approximate number of seconds an anim will take.
		private static float[,] animDuration;
		private static uint[] animsAvailableTranslate;
		
		static AbstractCharacter() {
			animDuration = new float[256,256];
			for (int numFrames=0; numFrames<256; numFrames++) {
				for (int frameDelay=0; frameDelay<256; frameDelay++) {
					animDuration[numFrames, frameDelay] = SlowAnimDuration((ushort)numFrames, (byte)frameDelay);
				}
			}
			animsAvailableTranslate=new uint[32];
			
			//Fill animsAvailableTranslate with the power-of-two for each bit of animsAvailable.
			uint code=1;
			for (int anim=0; anim<32; anim++) {
				animsAvailableTranslate[anim]=code;
				code+=code;
			}
		}
		
		public override Thing Cont {
			get {
				return this;
			} set {
				throw new SanityCheckException("They obviously won't fit in your "+value.GetType()+"! (You can't give a Character a Cont)");
			}
		}
		
		public bool IsNPC { get {
			return !IsPlayer;
		} }
		
		public bool IsPC { get {
			return IsPlayer;
		} }
		
		public override bool IsPlayer { get {
			Logger.WriteInfo(CharacterTracingOn, "IsPlayer: "+((Account!=null)?"Nope":("Yep: "+Account)));
			return (Account!=null);
		} }
		
		//private float calculatedWeight;
		//private bool recalculateWeight=true;

		private AbstractAccount account = null;
		private string name = null;
		public Thing act = null;
		public Thing targ = null;
		public byte gender = 0;
		private byte direction = 0;
		private ushort model = 0;
		internal ThingLinkedList visibleLayers;//layers 0..24
		internal ThingLinkedList invisibleLayers;//layers (26..29) + (32..max)
		internal ThingLinkedList specialLayer;//layer 30
		internal AbstractItem draggingLayer = null;
		
		//In most cases, you should use Flag_* properties to set/get flags. Rarely, you may need to directly modify flags,
		//	in the SE core (not in scripts), which is why this is internal.
		//But whatever you do, NEVER set the disconnected flag (0x0001) by tweaking flags.
		protected ushort flags = 0;	//This is a ushort to save memory.
		
		//internal bool disconnected;
		
		//private ushort hits = 0;
		//private ushort maxhits = 0;
		//private ushort stam = 0;
		//private ushort maxstam = 0;
		//private ushort mana = 0;
		//private ushort maxmana = 0;
		//private ushort str = 0;
		//private ushort dex = 0;
		//private ushort intel = 0; //int is C# reserved word
		
		private static uint instances = 0;
		
		public AbstractCharacter(ThingDef myDef): base(myDef) {
			instances++;
			this.name = myDef.Name;
			this.model = myDef.Model;
			if (!MainClass.loading) {
				if (ThingDef.lastCreatedThingContOrPoint == ContOrPoint.Cont) {
					throw new SanityCheckException("You can't create a Character in a container.");
				}
			}

			Globals.lastNewChar=this;
		}

		public AbstractCharacter(AbstractCharacter copyFrom) : base(copyFrom) { //copying constuctor
			instances++;
			if (copyFrom.IsPC) {
				throw new NotSupportedException("You can not dupe a PC!");
			}
			account=copyFrom.account;
			name=copyFrom.name;
			targ=copyFrom.targ;
			flags=copyFrom.flags;
			gender=copyFrom.gender;
			direction=copyFrom.direction;
			model=copyFrom.model;

			act=copyFrom.act;
			foreach (AbstractItem item in copyFrom) {
				//no triggers, thats why we are adding it this way.
				item.Dupe(this);
			}
			
			Globals.lastNewChar=this;
		}
		
		public static uint Instances { get {
			return instances;
		} }
		
		/**
			The Direction this character is facing.
		*/
		public Direction Direction {
			get {
				return (Direction)(direction&0x7);
			} set {
				Sanity.IfTrueThrow(!Enum.IsDefined(typeof(Direction),value),"Invalid value "+value+" for "+this+"'s direction.");
				NetState.AboutToChangeDirection(this, false);
				direction=(byte)value;
			}
		}
		/**
			Direction, as a byte.
		*/
		public byte Dir {
			get {
				return ((byte)(direction&0x7));
			} set {
				Direction=(Direction) value;
			}
		}

		//Used with NetState, but at present this should be set when the character moves (walk/run/fly), and should remain
		//set for the rest of the cycle. Maybe there's a potential use for that in scripts, so this is public.
		public bool Flag_Moving {
			get {
				return (direction&0x80)==0x80;
			} set {
				if (value) {
					direction|=0x80;
				} else {
					direction&=0x7f;
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
		
		public override int Height { get {
			int defHeight = def.Height;
			if (defHeight > 0) {
				return defHeight;
			}
			return 10;
		} }
		
		public ushort MountItem {
			get {
				return (def as AbstractCharacterDef).MountItem;
			}
		}

		/**
			These are flags which specify what kind of model this is, and what anims it has.
		*/
		public uint AnimsAvailable {
			get {
				uint animsAvailable=(ThingDef.FindCharDef(Model).AnimsAvailable);
				Sanity.IfTrueThrow(animsAvailable==0, "No AnimsAvailable flags were specified in the scripts for character model "+Model+"!");
				return animsAvailable;
			}
		}
		
		public abstract AbstractCharacter Mount { get; set; }
		
		public abstract bool Flag_WarMode { get; set; }
		public abstract bool Flag_Riding { get; }
		
		public abstract string PaperdollName { get; }
		
		public override string Name {
			get {
				return name;
			} set {
				this.ChangingProperties();
				NetState.AboutToChangeName(this);
				name=value;
			}
		}
		
		public override ushort Model { get {
			return model;
		} set {
			NetState.AboutToChangeBaseProps(this);
			model=value;
		} }
		public ushort Body { get {
			return model;
		} set {
			NetState.AboutToChangeBaseProps(this);
			model=value;
		} }
		
		public AbstractAccount Account {
			get {
				return account;	//return null if _owner is null or is not an GameAccount.
			}
		}
		
		public GameConn Conn {
			get {
				if (account!=null) {
					return account.Conn;
				} else {
					return null;
				}
			}
		}
		
		//ISrc
		public Conn ConnObj {
			get {
				return Conn;
			}
		}
		
		public bool IsPlevelAtLeast(int plevel) {
			return (account!=null && account.PLevel>=plevel);
		}
		
		public bool IsMaxPlevelAtLeast(int plevel) {
			return (account!=null && account.MaxPLevel>=plevel);
		}
		
		/*
			Method: Dupe
			
				Duplicates this character.
		*/
		public sealed override Thing Dupe() {
			if (IsPC) {
				throw new NotSupportedException("You can not dupe a PC!");
			}
			return (Thing) DeepCopyFactory.GetCopy(this);
		}

		public bool IsLingering {
			get {
				return IsPC && !Flag_Disconnected && Conn==null;
			}
		}

		public override bool Flag_Disconnected {
			get {
				return ((flags&0x0001)==0x0001);
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

		//Called by GameConn
		internal bool AttemptLogIn() {
			bool success=Trigger_LogIn();
			if (success) {
				SetFlag_Disconnected(false);
			}
			return success;
		}

		/**
			Only call this on players.
			If this character is logged in, forcefully and immediately logs them out.
			If this character is lingering (in game but disconnected), logs them out fully (making them be no longer in game).
			If this character is already logged out fully, this does nothing.
		*/
		public void LogoutFully() {
			if (IsPC) {
				if (IsLingering) {
					SetFlag_Disconnected(true);
				} else if (!Flag_Disconnected) {
					Conn.Close("LogoutFully called.");
					SetFlag_Disconnected(true);
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
			if (IsPC) {
				if (IsLingering) {
					SetFlag_Disconnected(true);
				} else if (!Flag_Disconnected) {
					Conn.Close("Disconnect called.");
				}	//else already logged out fully, do nothing
			} else {
				SetFlag_Disconnected(true);
			}
		}

		/**
			Call this on NPCs only
			If this is a NPC, it simply goes out of the Disconnect containers (of map) and gets it's Flag_Disconnected set to False
		*/
		public void Reconnect() {
			if (!IsPC) {
				SetFlag_Disconnected(false);
			}
		}
		
		//Called by GameConn
		internal void LogOut() {
			Logger.WriteDebug("LogOut char "+this.Uid.ToString("x8")+": x="+this.X+", y="+this.Y+", z="+this.Z);
			Trigger_LogOut();
		}
		
		//So Character itself (or GameConn after relinking) can set the flag without warnings.
		private void SetFlag_Disconnected(bool value) {
			if (Flag_Disconnected!=value) {
				NetState.AboutToChangeFlags(this);
				flags=(ushort) (value?(flags|0x0001):(flags&~0x0001));
				if (value) {
					GetMap().Disconnected(this);
				} else {
					GetMap().Reconnected(this);
				}
			}
		}

		//is called by GameConn when relinking, which is after the Things have been loaded, but before
		//they're put into map
		internal void ReLinkToGameConn() {
			flags = (ushort) (flags &~0x0001); //we're not disconnected, the Map should know
		}

		//method: Trigger_LogIn
		//this method fires the @login trigger
		internal bool Trigger_LogIn() {
			bool cancel=false;
			cancel=TryCancellableTrigger(TriggerKey.login, null);
			if (!cancel) {
				//@login did not return 1
				cancel=On_LogIn();
			}
			return !cancel;	//return true for success
		}

		//method: Trigger_LogOut
		//this method fires the @logout trigger
		internal void Trigger_LogOut() {
			TryTrigger(TriggerKey.logout, null);
			On_LogOut();
		}

		public virtual void On_LogOut() {

		}

		public virtual bool On_LogIn() {
			return false;
		}
			
		public override void Save(SaveStream output) {
			if (account!=null) {
				output.WriteValue("account",account);
			}
			if (def.Name!=name) {
				output.WriteValue("name",name);
			}
			if (def.Model!=model) {
				output.WriteValue("model",model);
			}
			int flagsToSave = flags;
			if (this.IsPlayer) {
				flagsToSave = flagsToSave|0x0001;//add the Disconnected flag, makes no sense to save a person "connected"
			}
			if (flagsToSave!=0) {
				output.WriteValue("flags", flagsToSave);
			}
			output.WriteValue("gender",gender);
			output.WriteValue("direction",(byte)direction);
			base.Save(output);
		}
		
		//For loading.
		public void LoadAccount_Delayed(object resolvedObject, string filename, int line) {
			account = (AbstractAccount) resolvedObject;
			account.AttachCharacter(this);
			FixDisconnectedFlagOnPlayers();
			
			//check that this account knows us, it should know us by this time...
			//for (int i = 0; i < GameAccount.maxCharactersPerGameAccount; i++) {
			//	AbstractCharacter ch = acc.GetCharacterInSlot(i);
			//	if (ch == this) {
			//		//ok
			//		return;
			//	}
			//}
			
			
			//if (!acc.AttachCharacter(this)) {
			//	Logger.WriteError("The character "+this+" declares "+acc+" as it's account, but the Account doesn't know it... and the Account is already full. Deleting.");
			//	Remove();
			//} else {
			//	Logger.WriteError("The character "+this+" declares "+acc+" as it's account, but the Account doesn't know it... re-adding.");
			//}
			
			return;
		}
		
		//For loading
		public void FixDisconnectedFlagOnPlayers() {
			if (IsPlayer) {
				if (Conn==null) {
					flags=(ushort) (flags|0x0001);
				} else {	//there shouldn't actually be anyone connected when this happens, but we check just in case.
					flags=(ushort) (flags&~0x0001);
				}
			}
		}
		
		//public void Disconnect() {
		//	if (Conn!=null) {
		//		Conn.Close("The disconnect command was used.");
		//	}
		//}
		
		//For loading.
		protected override void LoadLine(string filename, int line, string prop, string value) {
			switch(prop) {
				case "account": 
					ObjectSaver.Load(value, new LoadObject(LoadAccount_Delayed), filename, line);
					break;
				case "name":
					Match ma = ConvertTools.stringRE.Match(value);
					if (ma.Success) {
						name = ma.Groups["value"].Value;
					} else {
						name = value;
					}
					break;
				case "flags":
					flags = TagMath.ParseUInt16(value);
					break;
				case "gender":
					gender = TagMath.ParseByte(value);
					break;
				case "direction":
					direction = TagMath.ParseByte(value);
					if (Flag_Moving) {
						Flag_Moving=false;
						Sanity.IfTrueSay(true, "Flag_Moving was saved! It should not have been! (At the time this sanity check was written, NetState changes should always be processed before any thought of saving occurs. NetState changes clear Flag_Moving, if it is set)");
					}
					break;
				case "dispid":
				case "model": 
				case "body":
					model = TagMath.ParseUInt16(value);
					break;
				default:
					base.LoadLine(filename, line, prop, value);
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
		public bool IsNativeConsole { get {
			return false;
		} }
		
		//ISrc implementation
		public void WriteLine(string arg) {
			SysMessage(arg);
		}
		
		//ISrc implementation
		public void WriteLine(LogStr arg) {
			SysMessage(arg.NiceString);
		}
		
		public void SysMessage(string arg) {
			ThrowIfDeleted();
			GameConn conn = this.Conn;
			if (conn != null) {
				Server.SendSystemMessage(conn, arg, 0);
			}
		}

		public void SysMessage(string arg, int color) {
			ThrowIfDeleted();
			GameConn conn = this.Conn;
			if (conn != null) {
				Server.SendSystemMessage(conn, arg, (ushort) color);
			}
		}

		public void ClilocSysMessage(uint msg, string args) {
			ThrowIfDeleted();
			GameConn conn = this.Conn;
			if (conn != null) {
				PacketSender.PrepareClilocMessage(null, msg, SpeechType.Speech, 3, 0, args);
				PacketSender.SendTo(conn, true);
			}
		}
		public void ClilocSysMessage(uint msg, params string[] args) {
			ThrowIfDeleted();
			GameConn conn = this.Conn;
			if (conn != null) {
				PacketSender.PrepareClilocMessage(null, msg, SpeechType.Speech, 3, 0, string.Join("\t", args));
				PacketSender.SendTo(conn, true);
			}
		}

		public void ClilocSysMessage(uint msg, int color, string args) {
			ThrowIfDeleted();
			GameConn conn = this.Conn;
			if (conn != null) {
				PacketSender.PrepareClilocMessage(null, msg, SpeechType.Speech, 3, (ushort) color, args);
				PacketSender.SendTo(conn, true);
			}
		}
		public void ClilocSysMessage(uint msg, int color, params string[] args) {
			ThrowIfDeleted();
			GameConn conn = this.Conn;
			if (conn != null) {
				PacketSender.PrepareClilocMessage(null, msg, SpeechType.Speech, 3, (ushort) color, string.Join("\t", args));
				PacketSender.SendTo(conn, true);
			}
		}

		//internal static MutablePoint4D GetPointAtDirection(MutablePoint4D oldPoint, Direction dir) {
		//    MutablePoint4D newPoint;
		//    switch (dir) {
		//        case Direction.North:
		//            newPoint = new MutablePoint4D(oldPoint.x, (ushort) (oldPoint.y-1), oldPoint.z, oldPoint.m);
		//            break;
		//        case Direction.NorthEast:
		//            newPoint = new MutablePoint4D((ushort) (oldPoint.x+1), (ushort) (oldPoint.y-1), oldPoint.z, oldPoint.m);
		//            break;
		//        case Direction.East:
		//            newPoint = new MutablePoint4D((ushort) (oldPoint.x+1), oldPoint.y, oldPoint.z, oldPoint.m);
		//            break;
		//        case Direction.SouthEast:
		//            newPoint = new MutablePoint4D((ushort) (oldPoint.x+1), (ushort) (oldPoint.y+1), oldPoint.z, oldPoint.m);
		//            break;
		//        case Direction.South:
		//            newPoint = new MutablePoint4D((ushort) oldPoint.x, (ushort) (oldPoint.y+1), oldPoint.z, oldPoint.m);
		//            break;
		//        case Direction.SouthWest:
		//            newPoint = new MutablePoint4D((ushort) (oldPoint.x-1), (ushort) (oldPoint.y+1), oldPoint.z, oldPoint.m);
		//            break;
		//        case Direction.West:
		//            newPoint = new MutablePoint4D((ushort) (oldPoint.x-1), oldPoint.y, oldPoint.z, oldPoint.m);
		//            break;
		//        case Direction.NorthWest:
		//            newPoint = new MutablePoint4D((ushort) (oldPoint.x-1), (ushort) (oldPoint.y-1), oldPoint.z, oldPoint.m);
		//            break;
		//        default: 
		//            newPoint = oldPoint;
		//            break;
		//    }
		//    return newPoint;
		//}

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
			GameConn conn = this.Conn;
			Flag_Moving=true;
			if (Direction==dir) {
				Point4D oldPoint = new Point4D(this.point4d);

				Map map = GetMap();
				int newZ, newY, newX;

				bool canMoveEverywhere = (this.IsPlayer && this.Plevel >= Globals.plevelOfGM);
				if (!map.CheckMovement(oldPoint, this.MovementSettings, dir, canMoveEverywhere, out newX, out newY, out newZ)) {
					return false;
				}

				MutablePoint4D newPoint = new MutablePoint4D((ushort) newX, (ushort) newY, (sbyte) newZ, this.point4d.m);
				

				//move char, and send 0x77 to players nearby

				//check valid spot, and change z
				if (!Map.IsValidPos(newPoint.x, newPoint.y, newPoint.m)) {	//off the map
					return false;
				}
				
				if (TryCancellableTrigger(TriggerKey.step, new ScriptArgs(dir, running))) {
					return false;
				}
				if (On_Step((byte)dir, running)) {
					return false;
				}
				
				//should we really be asking regions, even for npcs? -tar
				Region oldRegion = this.Region;
				Region newRegion = map.GetRegionFor(newPoint);
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
				NetState.AboutToChangePosition(this, mt);

				this.region = newRegion;	//moved this down here in case CanWalkFromTo rejects the move attempt.
				
				//P(newPoint); -this makes many operations that are already here, thus would be duped. -tar
				point4d = newPoint;
				ChangedP(oldPoint);
			} else {//just changing direction, no steps
				NetState.AboutToChangeDirection(this, requested);
				direction=(byte) dir;
			}
			return true;
		}
		
		internal override sealed void SetPosImpl(MutablePoint4D point) {
			NetState.AboutToChangePosition(this, MovementType.Teleporting);
			if (Map.IsValidPos(point.x,point.y,point.m)) {
				Region oldRegion = this.Region;
				Point4D oldP = this.P();
				Region newRegion = GetMap().GetRegionFor(point);
				if (oldRegion != newRegion) {
					Region.ExitAndEnter(oldRegion, newRegion, this);//forced exit & enter
					region = newRegion;
				}
				point4d = point;
				ChangedP(oldP);
			} else {
				throw new ArgumentOutOfRangeException("Invalid position ("+point.x+","+point.y+" on mapplane "+point.m+")");
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
			int zdiff=Math.Abs(i.Z-Z);
			return (X==i.X && Y==i.Y && zdiff>=0 && zdiff<=i.Height);
		}
		
		public bool IsStandingOn_CheckZOnly(AbstractItem i) {
			int zdiff=Math.Abs(i.Z-Z);
			Sanity.IfTrueThrow(X!=i.X || Y!=i.Y, "IsStandingOn_CheckZOnly called when the item is not actually on the same tile. "+this+" is at ("+X+","+Y+"), and the item ("+i+") is at ("+i.X+","+i.Y+").");
			return (zdiff>=0 && zdiff<=i.Height);
		}
		
		public void StepOnItems() {
			foreach (AbstractItem itm in GetMap().GetItemsInRange(this.X, this.Y, 0)) {
				if (IsStandingOn(itm)) {
					itm.Trigger_Step(this, 0);
				}
			}
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
		public void MakeBePlayer(AbstractAccount acc) {
			if (acc!=null) {
				if (acc.AttachCharacter(this)) {
					NetState.Resend(this);
					account=acc;
					GetMap().MadeIntoPlayer(this);
				} else {
					throw new SEException("That account ("+acc+") is already full.");
				}
			}
		}
		
		public void Resync() {
			if (IsPC) {
				GameConn myConn = Conn;
				if (myConn!=null) {
					using (BoundPacketGroup group = PacketSender.NewBoundGroup()) {
						//(TODO): 0xbf map change (INI flag, etc)
						//(Not To-do, or to-do much later on): 0xbf map patches (INI flag) (.. We don't need this on custom maps, and it makes it more complicated to load the maps/statics)
						PacketSender.PrepareSeasonAndCursor(Season, Cursor);
						PacketSender.PrepareLocationInformation(myConn);
						//(TODO): 0x4e and 0x4f personal and global light levels
						PacketSender.PrepareCharacterInformation(this, GetHighlightColorFor(this));
						PacketSender.PrepareWarMode(this);
						group.SendTo(myConn);
					}
					Server.SendCharPropertiesTo(myConn, this, this);
					foreach (AbstractItem equippedItem in this.visibleLayers) {
						equippedItem.On_BeingSentTo(myConn);
					}

					//send nearby characters and items
					SendNearbyStuff();
					foreach (AbstractItem con in OpenedContainers.GetOpenedContainers(myConn)) {
						if (!con.IsEmpty) {
							if (PacketSender.PrepareContainerContents(con, myConn, this)) {
								PacketSender.SendTo(myConn, true);
								if (Globals.AOS && myConn.Version.aosToolTips) {
									foreach (AbstractItem contained in con) {
										if (this.CanSeeVisibility(contained)) {
											ObjectPropertiesContainer containedOpc = contained.GetProperties();
											if (containedOpc != null) {
												containedOpc.SendIdPacket(myConn);
											}
										}
									}
								}
							}
						}
					}
					this.On_BeingSentTo(myConn);
				}
			}
		}
		
		public void SendVersions(params uint[] vals) {
			if (IsPC) {
				try {
					GameConn c = Conn;
					if (c!=null) {
						PacketSender.PrepareVersions(vals);
						PacketSender.SendTo(c, true);
					}
				} catch (FatalException) {
					throw;
				} catch (Exception) {
				}
			}
		}
		
		//Death should kill the mount and unlink it from its rider, so this will only delete mounts
		//if the rider is deleted directly.
		internal protected override sealed void BeingDeleted() {
			instances--;
			//Server._out.SendForgetCharacterPackets(this);
			foreach (AbstractItem i in this) {
				i.Trigger_Destroy();//no updates, because it will disappear entirely
			}
			GetMap().Remove(this);
			base.BeingDeleted();	//This MUST be called.
		}
		
		public void EmptyCont() {
			ThrowIfDeleted();
			foreach (AbstractItem i in this) {
				Thing.DeleteThing(i);
			}
		}
		
		public abstract AbstractItem AddBackpack();
		
		public AbstractItem Backpack { get {
			AbstractItem foundPack = null;
			if (visibleLayers != null) {
				foundPack = (AbstractItem) visibleLayers.FindByZ((int) Layers.layer_pack);
			}
			if (foundPack == null) {
				foundPack = AddBackpack();
			}
			return foundPack;
		} }
		
		public bool HasPickedUp(AbstractItem itm) {
			return ((draggingLayer != null) && draggingLayer.Equals(itm));
		}
		
		public bool HasPickedUp(int uid) {
			return ((draggingLayer != null) && (draggingLayer.Uid==uid));
		}
			
		public AbstractItem FindLayer(byte num) {
			if (num == (int) Layers.layer_dragging) {
				return draggingLayer;
			} else if (num == (int) Layers.layer_special) {
				if (specialLayer != null) {
					return (AbstractItem) specialLayer.firstThing;
				}
				return null;
			} else if (num < sentLayers) {
				if (visibleLayers != null) {
					return (AbstractItem) visibleLayers.FindByZ(num);
				}
			} else if (invisibleLayers != null) {
				return (AbstractItem) invisibleLayers.FindByZ(num);
			}
			return null;
		}
		
		public override AbstractItem FindCont(int index) {
			int counter = 0;
			int prevCounter;
			if (visibleLayers != null) {
				counter = visibleLayers.count;
			}
			if (index < counter) {
				return (AbstractItem) visibleLayers[index];
			}
			prevCounter = counter;
			if (invisibleLayers != null) {
				counter += invisibleLayers.count;
			}
			if (index < counter) {
				return (AbstractItem) invisibleLayers[index - prevCounter];
			}
			if (draggingLayer != null) {
				if (index == counter) {
					return draggingLayer;
				}
				counter ++;
			}
			prevCounter = counter;
			if (specialLayer != null) {
				counter += specialLayer.count;
			}
			if (index < counter) {
				return (AbstractItem) specialLayer[index - prevCounter];
			}
			return null;
		}
		
		internal void InternalEquip(AbstractItem i) {
			byte iLayer = i.Layer;
			if (iLayer == (byte) Layers.layer_dragging) {
				draggingLayer = i;
				i.BeingEquipped(this, iLayer);
			} else if (iLayer == (byte) Layers.layer_special) {
				if (specialLayer == null) {
					specialLayer = new ThingLinkedList(this);
				}
				specialLayer.Add(i);
				//i.cont = specialLayer;
				i.BeingEquipped(specialLayer, iLayer);
			} else if (iLayer < sentLayers) {
				if (visibleLayers == null) {
					visibleLayers = new ThingLinkedList(this);
				}
				visibleLayers.Add(i);
				//i.cont = visibleLayers;
				i.BeingEquipped(visibleLayers, iLayer);
			} else {
				if (invisibleLayers == null) {
					invisibleLayers = new ThingLinkedList(this);
				}
				invisibleLayers.Add(i);
				//i.cont = invisibleLayers;
				i.BeingEquipped(invisibleLayers, iLayer);
			}
			//IncreaseWeightBy(i.Weight);
		}
		
		public void Equip(AbstractCharacter droppingChar, AbstractItem i) {
			if (i.IsEquippable) {
				ThrowIfDeleted();
				i.ThrowIfDeleted();
				//here it can also raise an exception when i is null
				
				byte iLayer = i.Layer;
				if ((iLayer>numLayers) || (iLayer<1) || (iLayer == (byte) Layers.layer_dragging)) {
					throw new Exception("The item "+i+"("+i.def+") has it`s layer bad set.");
				}
				
				PrivatePickup(i);//we "equip" it temporarily to the dragging layer

				ScriptArgs sa = new ScriptArgs(droppingChar, this, i, true);
				
				act = i;
				TryTrigger(TriggerKey.itemEquip, sa);
				if (i == draggingLayer) {
					On_ItemEquip(droppingChar, i, true);
					if (i == draggingLayer) {
						i.TryTrigger(TriggerKey.equip, sa);
						if (i == draggingLayer) {
							i.On_Equip(droppingChar, this, true);
							if (i == draggingLayer) {
								PrivateDropItem(i);
								if (iLayer != (byte) Layers.layer_special) {
									if (i.TwoHanded) {
										//If it's two-handed, then we have to clear both hands.
										Sanity.IfTrueThrow(iLayer!=2, "Attempted to equip two-handed weapon whose layer isn't 2! (It is "+iLayer+")");
										Unequip(this, FindLayer(1)); //we're not blaming the "droppingChar" for the unequips ;)
										Unequip(this, FindLayer(2));
									} else if (iLayer==1) {
										//If it isn't two handed, but goes on layer 1, we still have to check to see if we
										//have a two-handed item equipped, and if we do, we need to unequip it.
										//If we do, then we can't possibly have anything on layer 1.
										AbstractItem l2 = FindLayer(2);
										if (l2!=null && l2.TwoHanded) {
											//We do this sanity check before unequipping the two-handed item, in case its @unequip script equips something on layer 1.
											Sanity.IfTrueThrow(FindLayer(1)!=null, "FindLayer(1) returned something, but we were holding a two-handed item in layer 2!");
											Unequip(this, l2);
										} else {
											Unequip(this, FindLayer(1));
										}
									} else {
										//If our item is on layer 2 or any other layer then we don't need to do anything special.
										Unequip(this, FindLayer(iLayer));
									}
								}
								InternalEquip(i);
								return;
							}
						}
					}
				}
			} else {
				throw new InvalidOperationException("The item ("+i+") is not equippable");
			}
		}

		public bool TryEquip(AbstractCharacter droppingChar, AbstractItem i) {
			if (i.IsEquippable) {
				ThrowIfDeleted();
				i.ThrowIfDeleted();
				//here it can also raise an exception when i is null
				byte iLayer = i.Layer;
				if ((iLayer>numLayers) || (iLayer<1) || (iLayer == (byte) Layers.layer_dragging)) {
					throw new Exception("The item "+i+"("+i.def+") has its layer set wrong.");
				}
				//check profession equip rules etc?
				if (!this.CanEquip(i)) {
					return false;
				}
				
				if (i.TwoHanded) {
					//If it's two-handed, then we have to clear both hands.
					Sanity.IfTrueThrow(iLayer!=2, "Attempted to equip two-handed weapon whose layer isn't 2! (It is "+iLayer+")");
					if (!TryUnequip(this, FindLayer(1))) return false;
					if (!TryUnequip(this, FindLayer(2))) return false;
				} else if (iLayer==1) {
					//If it isn't two handed, but goes on layer 1, we still have to check to see if we
					//have a two-handed item equipped, and if we do, we need to unequip it.
					//If we do, then we can't possibly have anything on layer 1.
					AbstractItem l2 = FindLayer(2);
					if (l2!=null && l2.TwoHanded) {
						//We do this sanity check before unequipping the two-handed item, in case its @unequip script equips something on layer 1.
						Sanity.IfTrueThrow(FindLayer(1)!=null, "FindLayer(1) returned something, but we were holding a two-handed item in layer 2!");
						if (!TryUnequip(this, l2)) return false;
					} else {
						if (!TryUnequip(this, FindLayer(1))) return false;
					}
				} else {
					//If our item is on layer 2 or any other layer then we don't need to do anything special.
					if (!TryUnequip(this, FindLayer(iLayer))) return false;
				}
				
				PrivatePickup(i);//we "equip" it temporarily to the dragging layer

				ScriptArgs sa = new ScriptArgs(droppingChar, this, i, true);

				act = i;
				//AbstractCharacter src = Globals.src;Thing origAct = src.act;src.act=this;
				if ((!TryCancellableTrigger(TriggerKey.itemEquip, sa))&&(i == draggingLayer)) {
					if ((!On_ItemEquip(droppingChar, i, false))&&(i == draggingLayer)) {
						if ((!i.TryCancellableTrigger(TriggerKey.equip, sa))&&(i == draggingLayer)) {
							if ((!i.On_Equip(droppingChar, this, true))&&(i == draggingLayer)) {
								PrivateDropItem(i);
								InternalEquip(i);
								return true;
							}
						}
					}
				}
				if (i == draggingLayer) {
					PutInPack(i);
				}
				return (i.Cont == this);
			} else {
				throw new InvalidOperationException("The item ("+i+") is not equippable");
			}
		}

		public virtual bool On_ItemEquip(AbstractCharacter droppingChar, AbstractItem i, bool forced) {
			return false;
		}

		public virtual bool On_ItemUnEquip(AbstractCharacter pickingChar, AbstractItem i, bool forced) {
			return false;
		}

		public void Unequip(AbstractCharacter pickingChar, AbstractItem i) {
			ThrowIfDeleted();
			if (i != null) {
				if (i.Cont == this) {
					DropItem(pickingChar, i);
					PutInPack(i);
				}
			}
		}

		public bool TryUnequip(AbstractCharacter pickingChar, AbstractItem i) {
			ThrowIfDeleted();
			if (i != null) {
				if (i.Cont == this) {
					if (TryDropItem(pickingChar, i)) {
						PutInPack(i);
					} else {
						return false;
					}
				}
			}
			return true;
		}
		
		internal void AddLoadedItem(AbstractItem item) {
			byte layer = (byte) item.Z;
			if ((layer != (byte) Layers.layer_special) && (FindLayer(layer) != null)) {
				PutInPack(item);
			} else {
				InternalEquip(item);
			}
		}
		
		//method: DropItem
		//drops the item out of the character, 
		//but does not place it to any particular point4d, nor does it resend to the client,
		//However, it DOES set the item's x/y to 0xffff,0xffff, which is known to the sector-changing code as "nowhere",
		//in case P is set on this later (we wouldn't want it trying to remove the item from a sector it isn't
		//in because of ghost x/y values from when it was in a container).
		//calls triggers
		internal override void DropItem(AbstractCharacter pickingChar, AbstractItem i) {
			if (i.Cont == this) {
				Thing origAct = act;
				if (draggingLayer != i) {//no triggers for "unequipping" dragged item
					ScriptArgs sa = new ScriptArgs(pickingChar, this, i, true);
					act = i;
					TryTrigger(TriggerKey.itemUnEquip, sa);
					act = i;
					On_ItemUnEquip(pickingChar, i, true);
					act = i;
					i.TryTrigger(TriggerKey.unEquip, sa);
					act = i;
					i.On_UnEquip(pickingChar, true);
				}
				if (i.Cont == this) {
					PrivateDropItem(i);
				}//else the scripts did something else to the item, we dont interfere
				act = origAct;
			}
		}
		
		internal bool TryDropItem(AbstractCharacter pickingChar, AbstractItem i) {
			if (i.Cont==this) {
				if (draggingLayer == i) {
					return false; //two people are trying to pick up the same item... 
					//the second one cant get it off the first ones hand 
				}
				ScriptArgs sa = new ScriptArgs(pickingChar, this, i, false);
				act = i;
				if (!TryCancellableTrigger(TriggerKey.itemUnEquip, sa) && (i.Cont == this)) {
					if (!On_ItemUnEquip(pickingChar, i, false) && (i.Cont == this)) {
						if (!i.TryCancellableTrigger(TriggerKey.unEquip, sa) && (i.Cont == this)) {
							if (!i.On_UnEquip(pickingChar, false) && (i.Cont == this)) {
								PrivateDropItem(i);
								return true;
							}
						}
					}
				}
				return (i.Cont != this);
			} else {
				throw new InvalidOperationException("The item ("+i+") is not in the container ("+this+"). this should not happen.");
			}
		}
		
		private void PrivateDropItem(AbstractItem i) {
			if (draggingLayer == i) {
				//DecreaseWeightBy(i.Weight);
				i.BeingDroppedFromContainer();
				draggingLayer = null;
				return;
			}
			if (i.TryRemoveFromContainer()) {
				//DecreaseWeightBy(i.Weight);
				i.BeingDroppedFromContainer();
			}
		}

		public override void AddItem(AbstractCharacter addingChar, AbstractItem i) {
			Equip(addingChar, i);
		}
		public override void AddItem(AbstractCharacter addingChar, AbstractItem i, ushort x, ushort y) {
			Equip(addingChar, i);//x,y ignored
		}
		
		public void PutInPack(AbstractItem i) {
			Backpack.AddItem(this, i);
		}
		
		public void DropHeldItem() {
			//drop
			AbstractItem i = draggingLayer;
			if (i != null) {
				//DecreaseWeightBy(i.Weight);
				i.BeingDroppedFromContainer();
				draggingLayer = null;
				i.P(this);
			}
		}

		private void SendMovingItemAnimation(Thing from, Thing to, AbstractItem i) {
			PacketSender.PrepareMovingItemAnimation(from, to, i);
			PacketSender.SendToClientsWhoCanSee(this);
		}

		//picks up item. typically called from InPackets. I am the src, the item can be anywhere.
		//CanReach checks are not considered done.
		public PickupResult PickUp(AbstractItem item, ushort amt) {
			if (CanReach(item)) {
				PickupResult result = CanPickUp(item);
				if (result == PickupResult.Succeeded) {
					if (item.IsInContainer) {
						return PickupFromContainer(item, amt);
					} else if (item.IsOnGround) {
						return PickupFromGround(item, amt);
					} else {//is equipped
						return PickupEquipped(item);
					}
				} else {
					return result;
				}
			} else {
				return PickupResult.Failed_ThatIsTooFarAway;
			}
		}

		private PickupResult PickupEquipped(AbstractItem i) {
			ThrowIfDeleted();
			i.ThrowIfDeleted();
			act = i;
			AbstractCharacter cont = (AbstractCharacter) i.Cont;//we assume it is really in container...
			if (cont.TryDropItem(this, i)) {//allowed to pick up (triggers @unequip etc.)
				NetState.ItemAboutToChange(i);
				PrivatePickup(i);
				if (cont != this) {
					SendMovingItemAnimation(cont, this, i);
				}
				return PickupResult.Succeeded;
			}
			return PickupResult.Failed_NoMessage;
		}

		private PickupResult PickupFromContainer(AbstractItem i, ushort amt) {
			ThrowIfDeleted();
			i.ThrowIfDeleted();
			AbstractItem cont = (AbstractItem) i.Cont; //we assume it is really in container...
			ScriptArgs sa = new ScriptArgs(this, i, amt);
			act = i;
			//in every step we check if the item is still in the cont... otherwise it means the scripts moved it away, and pickup fails regardless of their return value
			if (!cont.TryCancellableTrigger(TriggerKey.pickUpFrom, sa) && (cont == i.Cont)) {//@PickupFrom on container
				object[] saArgv = sa.argv;
				object amtObj = saArgv[2];
				if (!cont.On_PickupFrom(this, i, ref amtObj) && (cont == i.Cont)) {
					saArgv[2] = amtObj;
					if (!TryCancellableTrigger(TriggerKey.itemPickup_Pack, sa) && (cont == i.Cont)) { //@itemPickup_Pack on this
						amtObj = saArgv[2];
						if (!On_ItemPickup_Pack(i, ref amtObj) && (cont == i.Cont)) {
							saArgv[2] = amtObj;
							if (!i.TryCancellableTrigger(TriggerKey.pickUp_Pack, sa) && (cont == i.Cont)) { //@pickup_pack on item
								amtObj = saArgv[2];
								if (!i.On_Pickup_Pack(this, ref amtObj) && (cont == i.Cont)) {
									//operation allowed :)
									uint amountToPickup;
									try {
										amountToPickup = ConvertTools.ToUInt32(amtObj);
									} catch (Exception e) {
										Logger.WriteError("While '"+this+"' picking up '"+i+"' from container: Unconvertable value set as amount by scripts", e);
										return PickupResult.Failed_NoMessage;
									}
									if (amountToPickup > 0) {
										uint prevAmount = i.Amount;
										if (amountToPickup < prevAmount) {
											i.Amount = amountToPickup;
											AbstractItem leftbehind = (AbstractItem) i.Dupe();
											leftbehind.Amount = (uint) (prevAmount - amountToPickup);
										}
										if (!i.IsWithinCont(this)) {
											SendMovingItemAnimation(cont, this, i);
										}
										cont.DropItem(this, i);
										PrivatePickup(i);
										return PickupResult.Succeeded;
									}
								}
							}
						}
					}
				}
			}
			return PickupResult.Failed_NoMessage;
		}

		private PickupResult PickupFromGround(AbstractItem i, ushort amt) {
			ThrowIfDeleted();
			i.ThrowIfDeleted();
			ScriptArgs sa = new ScriptArgs(this, i, amt);
			act = i;
			//in every step we check if the item is still on ground... otherwise it means the scripts moved it away, and pickup fails regardless of their return value
			if (!TryCancellableTrigger(TriggerKey.itemPickup_Ground, sa) && i.IsOnGround) {
				object[] saArgv = sa.argv;
				object amtObj = saArgv[2];
				if (!On_ItemPickup_Ground(i, ref amtObj) && i.IsOnGround) {
					saArgv[2] = amtObj;
					if (!i.TryCancellableTrigger(TriggerKey.pickUp_Ground, sa) && i.IsOnGround) {
						amtObj = saArgv[2];
						if (!i.On_Pickup_Ground(this, ref amtObj) && i.IsOnGround) {
							uint amountToPickup;
							try {
								amountToPickup = ConvertTools.ToUInt32(amtObj);
							} catch (Exception e) {
								Logger.WriteError("While '"+this+"' picking up '"+i+"' from ground: Unconvertable value set as amount by scripts", e);
								return PickupResult.Failed_NoMessage;
							}
							if (amountToPickup > 0) {
								uint prevAmount = i.Amount;
								if (amountToPickup < prevAmount) {
									i.Amount = amountToPickup;
									AbstractItem leftbehind = (AbstractItem) i.Dupe();
									leftbehind.Amount = (uint) (prevAmount - amountToPickup);
								}
								SendMovingItemAnimation(i, this, i);
								NetState.ItemAboutToChange(i);
								PrivatePickup(i);
								return PickupResult.Succeeded;
							}
						}
					}
				}
			}
			return PickupResult.Failed_NoMessage;
		}

		private void PrivatePickup(AbstractItem i) {
			DropHeldItem();
			i.FreeCont(this);
			draggingLayer = i;
			i.BeingEquipped(this, (byte)Layers.layer_dragging);
		}

		//typically called from InPackets. (I am the src)
		//could be made public if needed
		public void DropItemOnContainer(AbstractItem target, ushort x, ushort y) {
			if (target.IsContainer) {
				ThrowIfDeleted();
				target.ThrowIfDeleted();
				//initial checks
				AbstractItem i = draggingLayer;
				if (i == null) {
					throw new Exception("Character '"+this+"' has no item dragged to drop on '"+target+"'");
				}
				i.ThrowIfDeleted();

				ScriptArgs sa = new ScriptArgs(this, i, target, x, y);
				object[] saArgv = sa.argv;
				act = i;
				if ((!TryCancellableTrigger(TriggerKey.itemDropon_Item, sa))&&(i == draggingLayer)) {
					object objX = saArgv[3]; object objY = saArgv[4];
					if ((!On_ItemDropon_Item(i, target, ref objX, ref objY))&&(i == draggingLayer)) {
						saArgv[3] = objX; saArgv[4] = objY;
						if ((!target.TryCancellableTrigger(TriggerKey.stackOn, sa))&&(i == draggingLayer)) {
							objX = saArgv[3]; objY = saArgv[4];
							if ((!target.On_StackOn(this, i, ref objX, ref objY))&&(i == draggingLayer)) {
								saArgv[3] = objX; saArgv[4] = objY;
								if ((!i.TryCancellableTrigger(TriggerKey.stackon_Item, sa))&&(i == draggingLayer)) {
									objX = saArgv[3]; objY = saArgv[4];
									if ((!i.On_StackOn_Item(this, target, ref objX, ref objY))&&(i == draggingLayer)) {
										try {
											x = ConvertTools.ToUInt16(objX);
											y = ConvertTools.ToUInt16(objY);
										} catch (Exception e) {
											Logger.WriteError("While '"+this+"' dropping '"+i+"' on container: Unconvertable value set as x/y coords by scripts", e);
											AbstractItem myBackPack = this.Backpack;
											if (target==myBackPack) {
												//Force it to fall on the ground then
												//We're not using DropItemOnGround because that could cause an infinite loop if scripts block both the drop on ground and on backpack.
												PrivateDropItem(i);
												SendMovingItemAnimation(this, this, i);
											} else {
												DropItemOnContainer(myBackPack, 0xffff, 0xffff);
											}
											return;
										}
										target.AddItem(this, i, x, y);
										if (!target.IsWithinCont(this)) {
											SendMovingItemAnimation(this, target, i);
										}
										return;
									}
								}
							}
						}
					}
				}
				if (draggingLayer == i) {//cancelled and not somewhere else
					AbstractItem myBackPack = this.Backpack;
					if (target==myBackPack) {
						//Force it to fall on the ground then
						//We're not using DropItemOnGround because that could cause an infinite loop if scripts block both the drop on ground and on backpack.
						SendMovingItemAnimation(this, this, i);
						PrivateDropItem(i);
					} else {
						DropItemOnContainer(myBackPack, 0xffff, 0xffff);
					}
				}
			} else {
				throw new InvalidOperationException("The item ("+target+") is not a container");
			}
		}

		//typically called from InPackets. (I am the src)
		//could be made public if needed
		public void DropItemOnItem(AbstractItem target, ushort x, ushort y) {
			ThrowIfDeleted();
			target.ThrowIfDeleted();
			AbstractItem i = draggingLayer;
			if (i == null) {
				throw new Exception("Character '"+this+"' has no item dragged to drop on '"+target+"'");
			}
			i.ThrowIfDeleted();

			ScriptArgs sa = new ScriptArgs(this, i, target, x, y);
			object[] saArgv = sa.argv;
			act = i;
			if ((!TryCancellableTrigger(TriggerKey.itemDropon_Item, sa))&&(i == draggingLayer)) {
				object objX = saArgv[3]; object objY = saArgv[4];
				if ((!On_ItemDropon_Item(i, target, ref objX, ref objY))&&(i == draggingLayer)) {
					saArgv[3] = objX; saArgv[4] = objY;
					if ((!target.TryCancellableTrigger(TriggerKey.stackOn, sa))&&(i == draggingLayer)) {
						objX = saArgv[3]; objY = saArgv[4];
						if ((!target.On_StackOn(this, i, ref objX, ref objY))&&(i == draggingLayer)) {
							saArgv[3] = objX; saArgv[4] = objY;
							if ((!i.TryCancellableTrigger(TriggerKey.stackon_Item, sa))&&(i == draggingLayer)) {
								objX = saArgv[3]; objY = saArgv[4];
								if ((!i.On_StackOn_Item(this, target, ref objX, ref objY))&&(i == draggingLayer)) {
									//first try to stack this with the item
									if (!target.StackAdd(this, i)) {//stacking didnt happen
										if (target.IsInContainer) {
											try {
												x = Convert.ToUInt16(objX);
												y = Convert.ToUInt16(objY);
											} catch (Exception e) {
												Logger.WriteError("While '"+this+"' dropping '"+i+"' on container: Unconvertable value set as x/y coords by scripts", e);
												DropItemOnContainer(Backpack, 0xffff, 0xffff);
												return;
											}
											Thing t = target.Cont;
											if (!target.IsWithinCont(this)) {
												SendMovingItemAnimation(this, t, i);
											}
											t.AddItem(this, i, x, y);
											return;
										} else {//is on ground
											i.BeingDroppedFromContainer();
											i.P(x, y, (sbyte) (target.Z+target.Height));
											return;
										}
									}
								}
							}
						}
					}
				}
			}
			if (draggingLayer == i) {//cancelled and not somewhere else
				DropItemOnGround(this.X, this.Y, this.Z);
			}
		}

		//typically called from InPackets. (I am the src)
		//could be made public if needed
		public void DropItemOnGround(ushort x, ushort y, sbyte z) {
			ThrowIfDeleted();
			AbstractItem i = draggingLayer;
			if (i == null) {
				throw new Exception("Character '"+this+"' has no item dragged to drop on ground");
			}
			i.ThrowIfDeleted();

			IPoint4D dropOnPoint = new Point4D(x, y, z, this.M);
			ScriptArgs sa = new ScriptArgs(this, i, dropOnPoint);
			object[] saArgv = sa.argv;
			act = i;
			if ((!TryCancellableTrigger(TriggerKey.itemDropon_Ground, sa))&&(i == draggingLayer)) {
				if (!ConvertPointSafely(saArgv[2], out dropOnPoint)) {
					return;
				}
				if ((!On_ItemDropon_Ground(i, dropOnPoint))&&(i == draggingLayer)) {
					saArgv[2] = dropOnPoint;
					if ((!i.TryCancellableTrigger(TriggerKey.dropon_Ground, sa))&&(i == draggingLayer)) {
						if (!ConvertPointSafely(saArgv[2], out dropOnPoint)) {
							return;
						}
						if ((!i.On_DropOn_Ground(this, dropOnPoint))&&(i == draggingLayer)) {
							i.BeingDroppedFromContainer();
							draggingLayer = null;
							i.P(dropOnPoint);
							SendMovingItemAnimation(this, i, i);
							return;
						}
					}
				}
			}
			if (draggingLayer == i) {//cancelled and not somewhere else
				DropItemOnContainer(Backpack, 0xffff, 0xffff);
			}
		}

		private bool ConvertPointSafely(object p, out IPoint4D point) {
			try {
				point = (IPoint4D) p;
				return true;
			} catch (Exception e) {
				Logger.WriteError("While '"+this+"' dropping '"+draggingLayer+"' on ground: Unconvertable value set as x/y/z coords by scripts", e);
				//prevent drop
				DropItemOnContainer(Backpack, 0xffff, 0xffff);
			}
			point = null;
			return false;
		}

		//typically called from InPackets. drops the held item on target (I am the src)
		//could be made public if needed
		public void DropItemOnChar(AbstractCharacter target) {
			ThrowIfDeleted();
			target.ThrowIfDeleted();
			AbstractItem i = draggingLayer;
			if (i == null) {
				throw new Exception("Character '"+this+"' has no item dragged to drop on '"+target+"'");
			}
			i.ThrowIfDeleted();

			if (this == target) {
				//dropping on ones own toon/paperdoll. Equip or put into backpack.
				if (i.IsEquippable) {
					byte layer = i.Layer;
					if ((layer < sentLayers)&&(this.FindLayer(layer) == null)) {
						if (TryEquip(this, i)) {
							return;
						}
					}
				}
				DropItemOnContainer(Backpack, 0xffff, 0xffff);
				return;
			} else {
				ScriptArgs sa = new ScriptArgs(this, i, target);
				act = i;
				if (CanEquipItemsOn(target)) {//checks ownership and checks to make sure the character type can hold stuff on that layer, etc.
					if ((!TryCancellableTrigger(TriggerKey.itemDropon_Char, sa)) && (i == draggingLayer)) {
						if ((!On_ItemDropon_Char(i, target)) && (i == draggingLayer)) {
							if ((!target.TryCancellableTrigger(TriggerKey.stackOn, sa)) && (i == draggingLayer)) {
								object ignored = null;
								if ((!target.On_StackOn(this, i, ref ignored, ref ignored)) && (i == draggingLayer)) {
									if ((!i.TryCancellableTrigger(TriggerKey.stackon_Char, sa)) && (i == draggingLayer)) {
										if ((!i.On_StackOn_Char(this, target)) && (i == draggingLayer)) {
											if (i.IsEquippable) {
												if (target.TryEquip(this, i)) {
													return;
												}
											} else {
												if (target != this) {
													SendMovingItemAnimation(this, target, i);
												}
												DropItemOnContainer(target.Backpack, 0xffff, 0xffff);
											}
										}
									}
								}
							}
						}
					}
				}
				if (draggingLayer == i) {//cancelled and not somewhere else
					DropItemOnContainer(Backpack, 0xffff, 0xffff);
				}
			}
		}
		
		public virtual bool On_ItemDropon_Item(AbstractItem i, AbstractItem target, ref object objX, ref object objY) {
			//I am dropping item i on item target
			return false;
		}
		
		public virtual bool On_ItemDropon_Char(AbstractItem i, AbstractCharacter target) {
			//I am dropping item i on character target
			return false;
		}
		
		public virtual bool On_ItemDropon_Ground(AbstractItem i, IPoint4D point) {
			return false;
		}
						
		public override sealed bool IsChar { get {
			return true;
		} }
		
		public override sealed bool CanContain { get {
			return true;
		} }
		
		public GumpInstance SendGump(Thing focus, Gump gump, params object[] args) {
			GameConn gc = Conn;
			if (gc != null) {
				if (gump == null) {
					throw new ArgumentNullException("gump");
				}
				GumpInstance instance = gump.InternalConstruct(focus, this, args);
				if (instance != null) {
					gc.SentGump(instance);
					PacketSender.PrepareGump(instance);	//gc.Send(new SendGump(instance));
					PacketSender.SendTo(gc, true);
					return instance;
				}
			}
			return null;
		}
		
		public void DialogClose(GumpInstance instance, int buttonId) {
			DialogClose(instance.uid, buttonId);
		}
		
		public void DialogClose(uint gumpUid, int buttonId) {
			GameConn gc = Conn;
			if (gc != null) {
				PacketSender.PrepareCloseGump(gumpUid, buttonId);
				PacketSender.SendTo(Conn, true);
			}
		}
		
		public void DialogClose(Gump def, int buttonId) {
			GameConn gc = Conn;
			if (gc != null) {
				foreach (GumpInstance gi in gc.FindGumpInstances(def)) {
					DialogClose(gi.uid, buttonId);
				}
			}
		}
		
		[Remark("Look into the dialogs dictionary and find out whether the desired one is opened"+
				"(visible) or not")]
		public bool HasOpenedDialog(Gump def) {
			GameConn gc = Conn;
			if(gc != null) {
				if(gc.FindGumpInstances(def).Count != 0) {
					//nasli jsme otevrenou instanci tohoto gumpu
					return true;
				}
			}
			return false;
		}
		
		internal override sealed bool TriggerSpecific_Click(AbstractCharacter clickingChar, ScriptArgs sa) {
			//helper method for Trigger_Click
			bool cancel=false;
			cancel=clickingChar.TryCancellableTrigger(TriggerKey.charClick, sa);
			if (!cancel) {
				clickingChar.act=this;
				cancel=clickingChar.On_CharClick(this);
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
		
		public virtual bool On_ItemPickup_Ground(AbstractItem i, ref object amount) {
			//I am picking up amt of AbstractItem i from ground
			return false;
		}

		public virtual bool On_ItemPickup_Pack(AbstractItem i, ref object amount) {
			//I am picking up amt of AbstractItem i from its container
			return false;
		}
	
		public abstract bool CanEquipItemsOn(AbstractCharacter chr);

		public abstract bool CanEquip(AbstractItem i);

		//This is called directly by InPackets if the client directly requests the paperdoll
		//(dclick with the 0x80000000 flag), so that if they use the paperdoll macro or the paperdoll button
		//while mounted, they won't dismount.
		public void ShowPaperdollTo(GameConn conn) {
			if (conn != null) {
				AbstractCharacter connChar = conn.CurCharacter;
				if (connChar != null) {
					bool canEquip = true;
					if (connChar!=this) {
						canEquip = connChar.CanEquipItemsOn(this);
					}
					PacketSender.PreparePaperdoll(this, canEquip);
					PacketSender.SendTo(conn, true);
				}
			}
		}
		
		public void ShowPaperdoll() {
			ShowPaperdollTo(Globals.SrcGameConn);
		}

		//this method fires the speech triggers
		internal void Trigger_Hear(AbstractCharacter speaker, string speech, uint clilocSpeech,	
				SpeechType type, ushort color, ushort font, string lang, int[] keywords, string[] args) {
			ScriptArgs sa = new ScriptArgs(speaker, speech, clilocSpeech, type, color, font, lang, keywords, args);
			TryTrigger(TriggerKey.hear, sa);
			object[] saArgv = sa.argv;

			speech = saArgv[1] as string;
			clilocSpeech = ConvertTools.ToUInt32(saArgv[2]);
			type = (SpeechType) ConvertTools.ToInt64(saArgv[3]);
			color = ConvertTools.ToUInt16(saArgv[4]);
			font = ConvertTools.ToUInt16(saArgv[5]);
			lang = saArgv[6] as string;
			keywords = (int[]) saArgv[7];
			args = (string[]) saArgv[8];

			On_Hear(speaker, speech, clilocSpeech, type, color, font, lang, keywords, args);
		}

		public virtual void On_Hear(AbstractCharacter speaker, string speech, uint clilocSpeech, SpeechType type, ushort color, ushort font, string lang, int[] keywords, string[] args) {
			GameConn conn = this.Conn;
			if (conn != null) {
				if (speech==null) {
					PacketSender.PrepareClilocMessage(this, clilocSpeech, type, font, color, string.Join("\t", args));
				} else {
					string sendLang=(lang==null)?(conn.lang):(lang);
					PacketSender.PrepareSpeech(speaker, speech, type, font, color, sendLang);
				}
				PacketSender.SendTo(conn, true);
			}
		}

		internal void Trigger_Say(string speech, SpeechType type, int[] keywords) {
			if (this.IsPlayer) {
				ScriptArgs sa = new ScriptArgs(speech, type, keywords);
				TryTrigger(TriggerKey.say, sa);
				On_Say(speech, type, keywords);
			}
		}

		public virtual void On_Say(string speech, SpeechType type, int[] keywords) {
		}

		internal override sealed bool TriggerSpecific_DClick(AbstractCharacter dClickingChar, ScriptArgs sa) {
			//helper method for Trigger_Click
			bool cancel=false;
			cancel=dClickingChar.TryCancellableTrigger(TriggerKey.charDClick, sa);
			if (!cancel) {
				dClickingChar.act=this;
				cancel=dClickingChar.On_CharDClick(this);
			}
			return cancel;
		}
		
		public virtual bool On_ItemDClick(AbstractItem dClicked) {
			return false;
		}
		
		public virtual bool On_CharDClick(AbstractCharacter dClicked) {
			return false;
		}


		public void EchoMessage(string msg) {
			Logger.Show(msg);
			if (Conn!=null) {
				Conn.WriteLine(msg);
			}
		}
		
		/*
			Method: CanRename
				Determines if this character can rename another character.
				GMs cannot directly rename players in the status box, but they can set their name.
				This is because, as a GM, it's too easy to accidentally rename a player by trying
				to type a command while you have a status bar open (I've done it
				I-don't-know-how-many times myself).
				-SL
			Parameters:
				to - The character to be renamed
			Returns:
				True if 'to' is an NPC with an owner, and this is its owner.
		*/
		public abstract bool CanRename(AbstractCharacter to);
		
		//The client apparently doesn't want characters' uids to be flagged with anything.
		public sealed override uint FlaggedUid {
			get {
				return (uint) Uid;
			}
		}
		
		//Commands
		
		public void Anim(int anim) {
			PacketSender.PrepareAnimation(this, (byte)anim, 6, 1, false, false, 0x01);
			PacketSender.SendToClientsWhoCanSee(this);
		}
		public void Anim(int anim, byte frameDelay) {
			PacketSender.PrepareAnimation(this, (byte)anim, 6, 1, false, false, frameDelay);
			PacketSender.SendToClientsWhoCanSee(this);
		}
		
		public void Anim(int anim, bool backwards) {
			PacketSender.PrepareAnimation(this, (byte)anim, 0, 1, backwards, false, 0x01);
			PacketSender.SendToClientsWhoCanSee(this);
		}
		public void Anim(int anim, bool backwards, bool undo) {
			PacketSender.PrepareAnimation(this, (byte)anim, 0, 1, backwards, undo, 0x01);
			PacketSender.SendToClientsWhoCanSee(this);
		}
		
		public void Anim(int anim, bool backwards, byte frameDelay) {
			PacketSender.PrepareAnimation(this, (byte)anim, 0, 1, backwards, false, frameDelay);
			PacketSender.SendToClientsWhoCanSee(this);
		}
		public void Anim(int anim, bool backwards, bool undo, byte frameDelay) {
			PacketSender.PrepareAnimation(this, (byte)anim, 0, 1, backwards, undo, frameDelay);
			PacketSender.SendToClientsWhoCanSee(this);
		}
		
		public void Anim(byte anim, ushort numBackwardsFrames, ushort numAnims, bool backwards, bool undo, byte frameDelay) {
			PacketSender.PrepareAnimation(this, anim, numBackwardsFrames, numAnims, backwards, undo, frameDelay);
			PacketSender.SendToClientsWhoCanSee(this);
		}
		/**
			Returns an estimate of how long UO should take to finish the anim.
			The formula used is (.25*numFrames)+(.1*numFrames*frameDelay), which I got by timing how long it took to do
			anim 14 (which has 7 frames) with a variety of frameDelay values. (I figured out what the base delay
			per frame was first, and then figured out how much frameDelay affected it - I rounded, by the way, to nice
			even values, because if I were programming the UO client I would have used nice even values.
			
			But the people who programmed the UO client didn't always do what I would have done, hehe. In fact,
			they often seem to have done things that seem totally bizarre to me - like having numBackwardsFrames
			and numAnims (my names, since of course I don't have a clue what they called them!) work the way they do....
			)
			
			Example for anim 14:
				Anim(14);
				conn.WriteLine("That should take "+AnimDuration(7, 0)+" seconds to draw.");
			If someone makes a script using @timer to print the message when time is up, we can see if my estimates
			are accurate. I'll probably wind up doing that later, since the whole purpose of my testing the anim packet
			and making the AnimDuration method was that I would need to be able to predict when an anim would end,
			for future scripts - the use I was pondering was making the swing speed of weapons actually be synced with
			the anim speed of the attacking anims when using that anim, and it looks like I should be able to do that :).
			
			(
				This method, by the way, uses a two-dimensional lookup-table, which is why numFrames is a byte -
				we need 256 KB of RAM for the 256x256x4b table (4bytes for each element is a float), which isn't much,
				but if numFrames were a ushort and all possible values were legal, then it would need 65536*256*4b =
				64 MB of RAM, and that would not be good. :P
				
				I figure 256 KB is an acceptable tradeoff for the speed gain from not needing the multiplications
				(unless .NET does evil things with two-dimensional tables - I know they're slower than one-dimensional
				ones, but even if it's 4x slower than a one-dimensional table, it's still 15x or so (estimating!)
				faster than doing the three multiplications instead, and also still faster than it would be to have two
				one-dimensional tables (animBaseTime[numFrames] and animAdditionalTimePerFrame[frameDelay]),
				and to multiply the additional-time-per-frame by numFrames, (then adding the two together, which takes
				very little time compared to multiplication).
			)
				
			@numFrames This is a byte, although it is possible to have more than 255 frames. If you do, you could
				call SlowAnimDuration(ushort numFrames, byte frameDelay) instead, which is slower than this method.
				Or, if your number of frames is fixed, or if frameDelay is, then you could precalculate the results
				or write a faster calculation than what SlowAnimDuration does (based on
				(.25*numFrames)+(.1*numFrames*frameDelay), just precalculate whatever you can, like .25*numFrames if you
				know numFrames, or .1*frameDelay if you know frameDelay, etc). 
				
				This is the number of frames UO would be drawing. So if you're drawing it both forward and backward
				(using 'undo'), double the # of frames. If you're drawing it backwards and limiting the # of frames,
				well, it's up to you to feed this method the actual # of frames that will be drawn.
			@frameDelay The 'frameDelay' value you passed to the Anim method. If the anim method you used didn't take
				a frameDelay parameter, it defaulted to 0.
		*/
		public static float AnimDuration(byte numFrames, byte frameDelay) {
			return animDuration[numFrames,frameDelay];
		}
		
		public static float SlowAnimDuration(ushort numFrames, byte frameDelay) {
			return (.25f*numFrames)+(.1f*numFrames*frameDelay);
		}
		
		public override sealed AbstractItem Newitem(IThingFactory arg, uint amount) {
			return Backpack.Newitem(arg, amount);
		}
		
		public void ShowStatusBarTo(GameConn conn) {
			if (this.Equals(conn.CurCharacter)) {
				PacketSender.PrepareStatusBar(this, StatusBarType.Me);
			} else if (conn.CurCharacter.CanRename(this)) {
				PacketSender.PrepareStatusBar(this, StatusBarType.Pet);
			} else {
				PacketSender.PrepareStatusBar(this, StatusBarType.Pet);
			}
			PacketSender.SendTo(conn, true);
		}
		
		public void ShowSkillsTo(GameConn conn) {
			PacketSender.PrepareAllSkillsUpdate(Skills, conn.Version.displaySkillCaps);
			PacketSender.SendTo(conn, true);
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
			} set {
				AbstractAccount a = Account;
				if (a != null) {
					a.PLevel=value;
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
		
		public abstract ISkill[] Skills { get; }
		[Summary("Gets called by the core when the player presses the skill button/runs the macro")]
		public abstract void SelectSkill(int skillId);
		//public abstract void SelectSkill(int skillId);
		
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
		public abstract long TithingPoints { get; set; }
		[Remark("Displays in client status as Physical resistance by default")]
		public abstract short ArmorClass { get; }
		[Remark("Displays in client status as Energy resistance by default")]
		public abstract short MindDefense { get; }

		//Resistances do not have effs. Negative values impart penalties.
		[Remark("Displays in client status as Fire resistance by default")]
		public abstract short ExtendedStatusNum1 { get; }
		[Remark("Displays in client status as Cold resistance by default")]
		public abstract short ExtendedStatusNum2 { get; }
		[Remark("Displays in client status as Poison resistance by default")]
		public abstract short ExtendedStatusNum3 { get; }
		[Remark("Displays in client status as Luck by default")]
		public abstract short ExtendedStatusNum5 { get; }
		[Remark("Displays in client status as MinDamage by default")]
		public abstract short ExtendedStatusNum6 { get; }
		[Remark("Displays in client status as MaxDamage by default")]
		public abstract short ExtendedStatusNum7 { get; }

		public abstract HighlightColor GetHighlightColorFor(AbstractCharacter viewer);
		
		public void CancelTarget() {
			Sanity.IfTrueThrow(Conn==null, "CancelTarget called on a non-player or non-logged-in player.");
			Packets.Prepared.SendCancelTargettingCursor(Conn);
		}
		
		public void SendNearbyStuff() {
			GameConn myConn = Conn;
			if (myConn != null) {
				Rectangle2D rect = new Rectangle2D(this, this.UpdateRange);
				Map map = this.GetMap();
				foreach (AbstractItem itm in map.GetItemsInRectangle(rect)) {
					if (CanSeeForUpdate(itm)) {
						PacketSender.PrepareItemInformation(itm);
						PacketSender.SendTo(myConn, true);
						if (Globals.AOS && myConn.Version.aosToolTips) {
							ObjectPropertiesContainer iopc = itm.GetProperties();
							if (iopc != null) {
								iopc.SendIdPacket(myConn);
							}
						}
						itm.On_BeingSentTo(myConn);
					}
				}
				foreach (AbstractCharacter chr in map.GetCharsInRectangle(rect)) {
					if (this!=chr && CanSeeForUpdate(chr)) {
						PacketSender.PrepareCharacterInformation(chr, chr.GetHighlightColorFor(this));
						PacketSender.SendTo(myConn, true);
						PacketSender.PrepareUpdateHitpoints(chr, false);
						PacketSender.SendTo(myConn, true);
						Server.SendCharPropertiesTo(myConn, this, chr);
						chr.On_BeingSentTo(myConn);
						foreach (AbstractItem equippedItem in chr.visibleLayers) {
							equippedItem.On_BeingSentTo(myConn);
						}
					}
				}
				if (myConn.curAccount.AllShow) {
					foreach (Thing thing in map.GetDisconnectsInRectangle(rect)) {
						if ((this!=thing) && CanSeeForUpdate(thing)) {
							AbstractItem itm = thing as AbstractItem;
							if (itm != null) {
								PacketSender.PrepareItemInformation((AbstractItem) thing);
								PacketSender.SendTo(myConn, true);
								if (Globals.AOS && myConn.Version.aosToolTips) {
									ObjectPropertiesContainer iopc = thing.GetProperties();
									if (iopc != null) {
										iopc.SendIdPacket(myConn);
									}
								}
								itm.On_BeingSentTo(myConn);
							} else {
								AbstractCharacter chr = (AbstractCharacter) thing;
								PacketSender.PrepareCharacterInformation(chr, chr.GetHighlightColorFor(this));
								PacketSender.SendTo(myConn, true);
								PacketSender.PrepareUpdateHitpoints(chr, false);
								PacketSender.SendTo(myConn, true);
								Server.SendCharPropertiesTo(myConn, this, chr);
								chr.On_BeingSentTo(myConn);
								foreach (AbstractItem equippedItem in chr.visibleLayers) {
									equippedItem.On_BeingSentTo(myConn);
								}
							}
						}
					}
				}
			}
		}
		
		public bool HasTwoHandedWeaponEquipped {
			get {
				AbstractItem layer2 = FindLayer(2);
				return (layer2!=null && layer2.TwoHanded);
			}
		}
		public bool HasAHandFree {
			get {
				AbstractItem layer1 = FindLayer(1);
				AbstractItem layer2 = FindLayer(2);
				return (layer2==null || (!layer2.TwoHanded && layer1==null));
			}
		}
		public bool HasBothHandsFree {
			get {
				AbstractItem layer1 = FindLayer(1);
				AbstractItem layer2 = FindLayer(2);
				return (layer2==null && layer1==null);
			}
		}
		public bool HasNoHandsFree {
			get {
				return !HasAHandFree;
			}
		}
		/*
			AnimsAvailable:
				0x80000000:	Human
				0x40000000: Animal
				0x20000000: Monster
				
				Animals and monsters use additional flags to specify which anims they have art for.
		*/
		public bool IsModelHuman() {
			return IsModelHuman(AnimsAvailable);
		}
		private bool IsModelHuman(uint animsAvailable) {
			return ((animsAvailable&0x80000000)>0);
		}
		public bool IsModelAnimal() {
			return IsModelAnimal(AnimsAvailable);
		}
		private bool IsModelAnimal(uint animsAvailable) {
			return ((animsAvailable&0x40000000)>0);
		}
		public bool IsModelMonster() {
			return IsModelMonster(AnimsAvailable);
		}
		private bool IsModelMonster(uint animsAvailable) {
			return ((animsAvailable&0x20000000)>0);
		}
		public bool CanPerformAnim(AnimalAnim anim) {
			return CanPerformAnim(anim, AnimsAvailable);
		}
		public bool CanPerformAnim(AnimalAnim anim, uint animsAvailable) {
			return (animsAvailable&(animsAvailableTranslate[(int)anim]))>0;
		}
		public bool CanPerformAnim(MonsterAnim anim) {
			return CanPerformAnim(anim, AnimsAvailable);
		}
		public bool CanPerformAnim(MonsterAnim anim, uint animsAvailable) {
			return (animsAvailable&(animsAvailableTranslate[(int)anim]))>0;
		}
		
		public AnimalAnim GetAnimalAnim(GenericAnim anim, uint animsAvailable) {
			AnimalAnim realAnim = AnimalAnim.Walk;
			switch (anim) {
				case GenericAnim.Walk: {
					realAnim=AnimalAnim.Walk;
					break;
				} case GenericAnim.Run: {
					if (CanPerformAnim(AnimalAnim.Run, animsAvailable)) {
						realAnim=AnimalAnim.Run;
					} else {
						realAnim=AnimalAnim.Walk;
					}
					break;
				} case GenericAnim.StandStill: {
					if (CanPerformAnim(AnimalAnim.StandStill, animsAvailable)) {
						realAnim=AnimalAnim.StandStill;
					} else {
						realAnim=AnimalAnim.Walk;
					}
					break;
				} case GenericAnim.RandomIdleAction: {
					double dbl = Globals.dice.NextDouble();
					if (dbl<.5 && CanPerformAnim(AnimalAnim.IdleAction, animsAvailable)) {
						realAnim=AnimalAnim.IdleAction;
					} else if (dbl<1 && CanPerformAnim(AnimalAnim.IdleAction2, animsAvailable)) {
						realAnim=AnimalAnim.IdleAction2;
					} else {
						realAnim=AnimalAnim.StandStill;
					}
					break;
				} case GenericAnim.IdleAction: {
					if (CanPerformAnim(AnimalAnim.IdleAction, animsAvailable)) {
						realAnim=AnimalAnim.IdleAction;
					} else if (CanPerformAnim(AnimalAnim.IdleAction2, animsAvailable)) {
						realAnim=AnimalAnim.IdleAction2;
					} else {
						realAnim=AnimalAnim.StandStill;
					}
					break;
				} case GenericAnim.LookAround: {
					if (CanPerformAnim(AnimalAnim.IdleAction2, animsAvailable)) {
						realAnim=AnimalAnim.IdleAction2;
					} else if (CanPerformAnim(AnimalAnim.IdleAction, animsAvailable)) {
						realAnim=AnimalAnim.IdleAction;
					} else {
						realAnim=AnimalAnim.StandStill;
					}
					break;
				} case GenericAnim.AttackSwing: {
					if (CanPerformAnim(AnimalAnim.Attack, animsAvailable)) {
						realAnim=AnimalAnim.Attack;
					} else if (CanPerformAnim(AnimalAnim.Attack2, animsAvailable)) {
						realAnim=AnimalAnim.Attack2;
					} else {
						realAnim=AnimalAnim.StandStill;
					}
					break;
				} case GenericAnim.AttackStab: {
					if (CanPerformAnim(AnimalAnim.Attack2, animsAvailable)) {
						realAnim=AnimalAnim.Attack2;
					} else if (CanPerformAnim(AnimalAnim.Attack, animsAvailable)) {
						realAnim=AnimalAnim.Attack;
					}
					break;
				} case GenericAnim.AttackOverhead: {
					if (CanPerformAnim(AnimalAnim.Attack, animsAvailable)) {
						realAnim=AnimalAnim.Attack;
					} else if (CanPerformAnim(AnimalAnim.Attack2, animsAvailable)) {
						realAnim=AnimalAnim.Attack2;
					}
					break;
				} case GenericAnim.AttackShoot: {
					if (CanPerformAnim(AnimalAnim.Attack, animsAvailable)) {
						realAnim=AnimalAnim.Attack;
					} else if (CanPerformAnim(AnimalAnim.Attack2, animsAvailable)) {
						realAnim=AnimalAnim.Attack2;
					}
					break;
				} case GenericAnim.GetHit: {
					if (CanPerformAnim(AnimalAnim.GetHit, animsAvailable)) {
						realAnim=AnimalAnim.GetHit;
					} else {
						realAnim=AnimalAnim.StandStill;
					}
					break;
				} case GenericAnim.FallBackwards: {
					if (CanPerformAnim(AnimalAnim.Die, animsAvailable)) {
						realAnim=AnimalAnim.Die;
					} else if (CanPerformAnim(AnimalAnim.Die2, animsAvailable)) {
						realAnim=AnimalAnim.Die2;
					} else if (CanPerformAnim(AnimalAnim.LieDown, animsAvailable)) {
						realAnim=AnimalAnim.LieDown;
					} else {
						realAnim=AnimalAnim.StandStill;
					}
					break;
				} case GenericAnim.FallForwards: {
					if (CanPerformAnim(AnimalAnim.Die2, animsAvailable)) {
						realAnim=AnimalAnim.Die2;
					} else if (CanPerformAnim(AnimalAnim.Die, animsAvailable)) {
						realAnim=AnimalAnim.Die;
					} else if (CanPerformAnim(AnimalAnim.LieDown, animsAvailable)) {
						realAnim=AnimalAnim.LieDown;
					} else {
						realAnim=AnimalAnim.StandStill;
					}
					break;
				} case GenericAnim.Block: {		//also represents Dodge
					double dbl = Globals.dice.NextDouble();
					if (dbl < 0.5 && CanPerformAnim(AnimalAnim.Unknown, animsAvailable)) {
						realAnim=AnimalAnim.Unknown;
					} else {
						realAnim=AnimalAnim.StandStill;
					}
					break;
				} case GenericAnim.AttackPunch: {
					if (CanPerformAnim(AnimalAnim.Attack, animsAvailable)) {
						realAnim=AnimalAnim.Attack;
					} else if (CanPerformAnim(AnimalAnim.Attack2, animsAvailable)) {
						realAnim=AnimalAnim.Attack2;
					}
					break;
				} case GenericAnim.Bow: {
					realAnim=AnimalAnim.Eat;
					break;
				} case GenericAnim.Salute: {
					realAnim=AnimalAnim.Eat;
					break;
				} case GenericAnim.Drink: {	//also represents Eat
					realAnim=AnimalAnim.Eat;
					break;
				} default: {
					throw new SanityCheckException("Unknown generic anim "+anim);
				}
			}
			return realAnim;
		}
		public MonsterAnim GetMonsterAnim(GenericAnim anim, uint animsAvailable) {
			MonsterAnim realAnim = MonsterAnim.Walk;
			switch (anim) {
				case GenericAnim.Walk: {
					realAnim=MonsterAnim.Walk;
					break;
				} case GenericAnim.Run: {
					if (CanPerformAnim(MonsterAnim.Fly, animsAvailable)) {
						realAnim=MonsterAnim.Fly;
					} else {
						realAnim=MonsterAnim.Walk;
					}
					break;
				} case GenericAnim.StandStill: {
					if (CanPerformAnim(MonsterAnim.StandStill, animsAvailable)) {
						realAnim=MonsterAnim.StandStill;
					} else {
						realAnim=MonsterAnim.Walk;
					}
					break;
				} case GenericAnim.RandomIdleAction: {
					double dbl = Globals.dice.NextDouble();
					if (dbl<.5 && CanPerformAnim(MonsterAnim.IdleAction, animsAvailable)) {
						realAnim=MonsterAnim.IdleAction;
					} else if (dbl<1 && CanPerformAnim(MonsterAnim.LookAround, animsAvailable)) {
						realAnim=MonsterAnim.LookAround;
					} else {
						realAnim=MonsterAnim.StandStill;
					}
					break;
				} case GenericAnim.IdleAction: {
					if (CanPerformAnim(MonsterAnim.IdleAction, animsAvailable)) {
						realAnim=MonsterAnim.IdleAction;
					} else if (CanPerformAnim(MonsterAnim.LookAround, animsAvailable)) {
						realAnim=MonsterAnim.LookAround;
					} else {
						realAnim=MonsterAnim.StandStill;
					}
					break;
				} case GenericAnim.LookAround: {
					if (CanPerformAnim(MonsterAnim.LookAround, animsAvailable)) {
						realAnim=MonsterAnim.LookAround;
					} else if (CanPerformAnim(MonsterAnim.IdleAction, animsAvailable)) {
						realAnim=MonsterAnim.IdleAction;
					} else {
						realAnim=MonsterAnim.StandStill;
					}
					break;
				} case GenericAnim.AttackSwing: {
					if (CanPerformAnim(MonsterAnim.Attack, animsAvailable)) {
						realAnim=MonsterAnim.Attack;
					} else if (CanPerformAnim(MonsterAnim.Attack2, animsAvailable)) {
						realAnim=MonsterAnim.Attack2;
					} else if (CanPerformAnim(MonsterAnim.Attack3, animsAvailable)) {
						realAnim=MonsterAnim.Attack3;
					} else if (CanPerformAnim(MonsterAnim.Attack4, animsAvailable)) {
						realAnim=MonsterAnim.Attack4;
					} else if (CanPerformAnim(MonsterAnim.Attack5, animsAvailable)) {
						realAnim=MonsterAnim.Attack5;
					} else if (CanPerformAnim(MonsterAnim.Attack6, animsAvailable)) {
						realAnim=MonsterAnim.Attack6;
					} else {
						realAnim=MonsterAnim.StandStill;
					}
					break;
				} case GenericAnim.AttackStab: {
					if (CanPerformAnim(MonsterAnim.Attack2, animsAvailable)) {
						realAnim=MonsterAnim.Attack2;
					} else if (CanPerformAnim(MonsterAnim.Attack, animsAvailable)) {
						realAnim=MonsterAnim.Attack;
					} else if (CanPerformAnim(MonsterAnim.Attack3, animsAvailable)) {
						realAnim=MonsterAnim.Attack3;
					} else if (CanPerformAnim(MonsterAnim.Attack4, animsAvailable)) {
						realAnim=MonsterAnim.Attack4;
					} else if (CanPerformAnim(MonsterAnim.Attack5, animsAvailable)) {
						realAnim=MonsterAnim.Attack5;
					} else if (CanPerformAnim(MonsterAnim.Attack6, animsAvailable)) {
						realAnim=MonsterAnim.Attack6;
					} else {
						realAnim=MonsterAnim.StandStill;
					}
					break;
				} case GenericAnim.AttackOverhead: {
					if (CanPerformAnim(MonsterAnim.Attack3, animsAvailable)) {
						realAnim=MonsterAnim.Attack3;
					} else if (CanPerformAnim(MonsterAnim.Attack, animsAvailable)) {
						realAnim=MonsterAnim.Attack;
					} else if (CanPerformAnim(MonsterAnim.Attack2, animsAvailable)) {
						realAnim=MonsterAnim.Attack2;
					} else if (CanPerformAnim(MonsterAnim.Attack4, animsAvailable)) {
						realAnim=MonsterAnim.Attack4;
					} else if (CanPerformAnim(MonsterAnim.Attack5, animsAvailable)) {
						realAnim=MonsterAnim.Attack5;
					} else if (CanPerformAnim(MonsterAnim.Attack6, animsAvailable)) {
						realAnim=MonsterAnim.Attack6;
					} else {
						realAnim=MonsterAnim.StandStill;
					}
					break;
				} case GenericAnim.AttackShoot: {
					if (CanPerformAnim(MonsterAnim.Attack4, animsAvailable)) {
						realAnim=MonsterAnim.Attack4;
					} else if (CanPerformAnim(MonsterAnim.Attack5, animsAvailable)) {
						realAnim=MonsterAnim.Attack5;
					} else if (CanPerformAnim(MonsterAnim.Attack, animsAvailable)) {
						realAnim=MonsterAnim.Attack;
					} else if (CanPerformAnim(MonsterAnim.Attack2, animsAvailable)) {
						realAnim=MonsterAnim.Attack2;
					} else if (CanPerformAnim(MonsterAnim.Attack3, animsAvailable)) {
						realAnim=MonsterAnim.Attack3;
					} else if (CanPerformAnim(MonsterAnim.Attack6, animsAvailable)) {
						realAnim=MonsterAnim.Attack6;
					} else {
						realAnim=MonsterAnim.StandStill;
					}
					break;
				} case GenericAnim.GetHit: {
					if (CanPerformAnim(MonsterAnim.GetHit, animsAvailable)) {
						realAnim=MonsterAnim.GetHit;
					} else if (CanPerformAnim(MonsterAnim.GetHitWhileFlying, animsAvailable)) {
						realAnim=MonsterAnim.GetHitWhileFlying;
					} else {
						realAnim=MonsterAnim.StandStill;
					}
					break;
				} case GenericAnim.FallBackwards: {
					if (CanPerformAnim(MonsterAnim.FallBackwards, animsAvailable)) {
						realAnim=MonsterAnim.FallBackwards;
					} else if (CanPerformAnim(MonsterAnim.FallForwards, animsAvailable)) {
						realAnim=MonsterAnim.FallForwards;
					} else {
						realAnim=MonsterAnim.StandStill;
					}
					break;
				} case GenericAnim.FallForwards: {
					if (CanPerformAnim(MonsterAnim.FallForwards, animsAvailable)) {
						realAnim=MonsterAnim.FallForwards;
					} else if (CanPerformAnim(MonsterAnim.FallBackwards, animsAvailable)) {
						realAnim=MonsterAnim.FallBackwards;
					} else {
						realAnim=MonsterAnim.StandStill;
					}
					break;
				} case GenericAnim.Block: {		//also represents Dodge
					double dbl = Globals.dice.NextDouble();
					if (dbl < 0.5 && CanPerformAnim(MonsterAnim.BlockLeft, animsAvailable)) {
						realAnim=MonsterAnim.BlockLeft;
					} else if (CanPerformAnim(MonsterAnim.BlockRight, animsAvailable)) {
						realAnim=MonsterAnim.BlockRight;
					} else {
						realAnim=MonsterAnim.StandStill;
					}
					break;
				} case GenericAnim.AttackPunch: {
					if (CanPerformAnim(MonsterAnim.Attack6, animsAvailable)) {
						realAnim=MonsterAnim.Attack6;
					} else if (CanPerformAnim(MonsterAnim.Attack, animsAvailable)) {
						realAnim=MonsterAnim.Attack;
					} else if (CanPerformAnim(MonsterAnim.Attack2, animsAvailable)) {
						realAnim=MonsterAnim.Attack2;
					} else if (CanPerformAnim(MonsterAnim.Attack3, animsAvailable)) {
						realAnim=MonsterAnim.Attack3;
					} else if (CanPerformAnim(MonsterAnim.Attack4, animsAvailable)) {
						realAnim=MonsterAnim.Attack4;
					} else if (CanPerformAnim(MonsterAnim.Attack5, animsAvailable)) {
						realAnim=MonsterAnim.Attack5;
					} else {
						realAnim=MonsterAnim.StandStill;
					}
					break;
				} case GenericAnim.Bow: {
					realAnim=MonsterAnim.StandStill;
					break;
				} case GenericAnim.Salute: {
					realAnim=MonsterAnim.StandStill;
					break;
				} case GenericAnim.Drink: {	//also represents Eat
					realAnim=MonsterAnim.StandStill;
					break;
				} default: {
					throw new SanityCheckException("Unknown generic anim "+anim);
				}
			}
			return realAnim;
		}
		public HumanAnim GetHumanAnim(GenericAnim anim) {
			HumanAnim realAnim = HumanAnim.WalkUnarmed;
			switch (anim) {
				case GenericAnim.Walk: {
					if (Flag_Riding) {
						realAnim=HumanAnim.MountedWalk;
					} else if (Flag_WarMode) {
						realAnim=HumanAnim.WalkWarMode;
					} else if (!HasBothHandsFree) {
						realAnim=HumanAnim.WalkArmed;	//TODO: Check this, see if it looks right with two-handed weapons, only a shield, etc.
					} else {
						realAnim=HumanAnim.WalkUnarmed;
					}
					break;
				} case GenericAnim.Run: {
					if (Flag_Riding) {
						realAnim=HumanAnim.MountedRun;
					} else if (!HasBothHandsFree) {
						realAnim=HumanAnim.RunArmed;	//TODO: Check this, see if it looks right with two-handed weapons, only a shield, etc.
					} else {
						realAnim=HumanAnim.RunUnarmed;
					}
					break;
				} case GenericAnim.StandStill: {
					if (Flag_Riding) {
						realAnim=HumanAnim.MountedStandStill;
					} else if (Flag_WarMode) {
						realAnim=HumanAnim.WarMode;
					} else {
						realAnim=HumanAnim.StandStill;
					}
					break;
				} case GenericAnim.RandomIdleAction: {
					if (Flag_Riding) {
						double dbl = Globals.dice.NextDouble();
						if (dbl<.25) {
							realAnim=HumanAnim.MountedSalute;
						} else if (dbl<.5) {
							realAnim=HumanAnim.MountedBlock;
						} else if (dbl<.75) {
							realAnim=HumanAnim.MountedGetHit;
						} else {
							if (!HasTwoHandedWeaponEquipped) {
								//This looks like slapping the horse, and that's what wolfpack calls it,
								//but if you ask me, you shouldn't use it for an idle action unless you don't
								//have a two-handed weapon equipped (or it will look like you're attacking).
								realAnim=HumanAnim.MountedAttackTwohandedOverhead;
							} else {
								//Let's see how this looks. IIRC, if you're mounted you hold two-handed weapons
								//with only one hand, so this should look like slapping the horse too.
								realAnim=HumanAnim.MountedAttackOverhead;
							}						
						}
					} else if (Flag_WarMode) {
						realAnim=HumanAnim.WarMode;	//no idle actions when in war mode.
					} else {
						double dbl = Globals.dice.NextDouble();
						if (dbl<.5) {
							realAnim=HumanAnim.LookAround;
						} else {
							realAnim=HumanAnim.LookDown;
						}
					}
					break;
				} case GenericAnim.IdleAction: {
					if (Flag_Riding) {
						double dbl = Globals.dice.NextDouble();
						if (dbl<.5) {
							realAnim=HumanAnim.MountedSalute;
						} else {
							if (!HasTwoHandedWeaponEquipped) {
								//This looks like slapping the horse, and that's what wolfpack calls it,
								//but if you ask me, you shouldn't use it for an idle action unless you don't
								//have a two-handed weapon equipped (or it will look like you're attacking).
								//Heh.
								realAnim=HumanAnim.MountedAttackTwohandedOverhead;
							} else {
								//Let's see how this looks. IIRC, if you're mounted you hold two-handed weapons
								//with only one hand, so this should look like slapping the horse too.
								realAnim=HumanAnim.MountedAttackOverhead;
							}			
						}
					} else if (Flag_WarMode) {
						realAnim=HumanAnim.WarMode;	//no idle actions when in war mode.
					} else {
						realAnim=HumanAnim.LookDown;
					}
					break;
				} case GenericAnim.LookAround: {
					if (Flag_Riding) {
						realAnim=HumanAnim.MountedSalute;
					} else if (Flag_WarMode) {
						realAnim=HumanAnim.WarMode;	//no idle actions when in war mode.
					} else {
						realAnim=HumanAnim.LookAround;
					}
					break;
				} case GenericAnim.AttackSwing: {
					if (Flag_Riding) {
						realAnim=HumanAnim.MountedAttackOverhead;
					} else if (HasTwoHandedWeaponEquipped) {
						realAnim=HumanAnim.AttackTwoHandedSwing;
					} else {
						realAnim=HumanAnim.AttackSwing;
					}
					break;
				} case GenericAnim.AttackStab: {
					if (Flag_Riding) {
						realAnim=HumanAnim.MountedAttackOverhead;
					} else if (HasTwoHandedWeaponEquipped) {
						realAnim=HumanAnim.AttackTwoHandedStab;
					} else {
						realAnim=HumanAnim.AttackStab;
					}
					break;
				} case GenericAnim.AttackOverhead: {
					if (Flag_Riding) {
						realAnim=HumanAnim.MountedAttackOverhead;
					} else if (HasTwoHandedWeaponEquipped) {
						realAnim=HumanAnim.AttackTwoHandedOverhead;
					} else {
						realAnim=HumanAnim.AttackOverhead;
					}
					break;
				} case GenericAnim.AttackShoot: {
					AbstractItem layer2 = FindLayer(2);
					if (layer2!=null && layer2.HasTriggerGroup(TriggerGroup.Get("t_weapon_xbow"))) {
						if (Flag_Riding) {
							realAnim=HumanAnim.MountedFireCrossbow;
						} else {
							realAnim=HumanAnim.FireCrossbow;
						}
					} else {
						if (Flag_Riding) {
							realAnim=HumanAnim.MountedFireBow;
						} else {
							realAnim=HumanAnim.FireBow;
						}
					}
					break;
				} case GenericAnim.GetHit: {
					if (Flag_Riding) {
						realAnim=HumanAnim.MountedGetHit;
					} else {
						realAnim=HumanAnim.GetHit;
					}
					break;
				} case GenericAnim.FallBackwards: {
					realAnim=HumanAnim.FallBackwards;
					break;
				} case GenericAnim.FallForwards: {
					realAnim=HumanAnim.FallForwards;
					break;
				} case GenericAnim.Block: {		//also represents Dodge
					if (Flag_Riding) {
						realAnim=HumanAnim.MountedBlock;
					} else {
						realAnim=HumanAnim.Block;
					}
					break;
				} case GenericAnim.AttackPunch: {
					if (Flag_Riding) {
						realAnim=HumanAnim.MountedBlock;
					} else {
						realAnim=HumanAnim.AttackPunch;
					}
					break;
				} case GenericAnim.Bow: {
					if (Flag_Riding) {
						realAnim=HumanAnim.MountedBlock;
					} else {
						realAnim=HumanAnim.Bow;
					}
					break;
				} case GenericAnim.Salute: {
					if (Flag_Riding) {
						realAnim=HumanAnim.MountedSalute;
					} else {
						realAnim=HumanAnim.Salute;
					}
					break;
				} case GenericAnim.Drink: {	//also represents Eat
					if (Flag_Riding) {
						realAnim=HumanAnim.MountedSalute;
					} else {
						realAnim=HumanAnim.Drink;
					}
					break;
				} default: {
					throw new SanityCheckException("Unknown generic anim "+anim);
				}
			}
			return realAnim;
		}
		public byte TranslateAnim(GenericAnim anim) {
			byte realAnim = 0;
			if ((byte)anim>=(byte)HumanAnim.NumAnims) {
				Sanity.IfTrueThrow((byte)anim>0xff, "Cannot perform anim '"+anim+"', that number is too high.");
				realAnim = (byte)anim;
			} else {
				uint animsAvailable = AnimsAvailable;
				if (IsModelHuman(animsAvailable)) {
					realAnim=(byte) GetHumanAnim(anim);
				} else if (IsModelMonster(animsAvailable)) {
					realAnim=(byte) GetMonsterAnim(anim, animsAvailable);
				} else if (IsModelAnimal(animsAvailable)) {
					realAnim=(byte) GetAnimalAnim(anim, animsAvailable);
				}
			}
			Logger.WriteDebug("Translated "+anim+" to "+realAnim);
			return realAnim;
		}
		public void PerformAnim(GenericAnim anim) {
			Anim(TranslateAnim(anim));
		}
		public void PerformAnim(GenericAnim anim, bool backwards) {
			Anim(TranslateAnim(anim), backwards);
		}
		public void PerformAnim(GenericAnim anim, bool backwards, bool undo) {
			Anim(TranslateAnim(anim), 0, 1, backwards, undo, 0x01);
		}
		
		public void PerformAnim(GenericAnim anim, bool backwards, byte frameDelay) {
			Anim(TranslateAnim(anim), 0, 1, backwards, false, frameDelay);
		}
		public void PerformAnim(GenericAnim anim, bool backwards, bool undo, byte frameDelay) {
			Anim(TranslateAnim(anim), 0, 1, backwards, undo, frameDelay);
		}
		
		public void PerformAnim(GenericAnim anim, ushort numBackwardsFrames, ushort numAnims, bool backwards, bool undo, byte frameDelay) {
			Anim(TranslateAnim(anim), numBackwardsFrames, numAnims, backwards, undo, frameDelay);
		}

		//ISrc implementation
		public AbstractCharacter Character { get {
			return this;
		} }
		
		public int VisibleCount { get {
			if (visibleLayers == null) {
				return 0;
			} else {
				return visibleLayers.count;
			}
		} }

		public IEnumerable<Thing> GetVisibleEquip() {
			if (visibleLayers == null) {
				return EmptyEnumerator<Thing>.instance;
			} else {
				return visibleLayers;
			}
		}
		
		public int InvisibleCount { get {
			int count = 0;
			if (invisibleLayers != null) {
				count =  invisibleLayers.count;
			}
			if (specialLayer != null) {
				count +=  specialLayer.count;
			}
			if (draggingLayer != null) {
				count++;
			}
			return count;
		} }

		public override sealed System.Collections.IEnumerator GetEnumerator() {
			ThrowIfDeleted();
			return new EquipsEnumerator(this);
		}
	}
	public class EquipsEnumerator: System.Collections.IEnumerator {
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
    	
    	public AbstractItem Current { get {
    	      return current;
    	} }
    	
    	object IEnumerator.Current { get {
    	      return current;
    	} }
	}
}
