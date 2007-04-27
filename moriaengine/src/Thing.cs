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
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using SteamEngine.Common;
using SteamEngine.Packets;
using System.Diagnostics;
using System.Configuration;
using SteamEngine.Persistence;

namespace SteamEngine {
	public abstract class Thing : TagHolder, IPoint4D, IEnumerable {
		public static bool ThingTracingOn = TagMath.ParseBoolean(ConfigurationManager.AppSettings["Thing Trace Messages"]);
		public static bool WeightTracingOn = TagMath.ParseBoolean(ConfigurationManager.AppSettings["Weight Trace Messages"]);

		private long createdAt = Globals.TimeInTicks;//Server time of creation
		private ushort color;
		internal ThingDef _def; //tis is changed even from outside the constructor in case of dupeitems...
		internal MutablePoint4D point4d; //made this internal because SetPosImpl is now abstract... -tar
		private int uid=-2;
		internal Region region;
		internal NetState netState;//No one is to touch this but the NetState class itself!

		internal object contOrTLL; //internal cos of ThingLinkedList

		private static int savedCharacters=0;
		private static int savedItems=0;
		private static ArrayList alreadySaved=new ArrayList();

		internal Thing nextInList = null;
		internal Thing prevInList = null;

		private static int loadedCharacters;
		private static int loadedItems;

		private static List<TriggerGroup> registeredTGs = new List<TriggerGroup>();

		private static UIDArray<Thing> things = new UIDArray<Thing>();
		private static int uidBeingLoaded=-1;
		public static TagKey weightTag = TagKey.Get("_weight");

		//still needs to be put into the world
		protected Thing(ThingDef myDef) {
			this._def=myDef;
			if (uidBeingLoaded==-1) {
				uid = things.Add(this);
				NetState.Resend(this);
				//we give this either to cont or to coords.
				if (ThingDef.lastCreatedThingContOrPoint == ContOrPoint.Point) {
					this.point4d = new MutablePoint4D(ThingDef.lastCreatedIPoint);
					Map.GetMap(point4d.m).Add(this);
				}
			} else {
				uid=uidBeingLoaded;
				this.point4d=new MutablePoint4D((ushort) Globals.dice.Next(0, 256), (ushort) Globals.dice.Next(0, 256), 0, 0);
			}
			Globals.lastNew=this;
		}

		protected Thing(Thing copyFrom)
				: base(copyFrom) { //copying constuctor
			NetState.Resend(this);
			uid=things.Add(this);
			if (uid<0 && uid>=things.Count) {	//If this isn't true, then something's wrong with GetFreeThingSlot.
				throw new ServerException("Something is wrong with GetFreeThingSlot! Free uid returned="+Uid+" and things' index should be >=0 and <"+things.Count);
			}
			point4d=new MutablePoint4D(copyFrom.point4d);
			_def=copyFrom._def;
			color=copyFrom.color;
			//SetSectorPoint4D();
			Globals.lastNew=this;
			On_Dupe(copyFrom);
		}

		//Property: Enumerable
		//An IEnumerable for the Things represented by this class
		public static IEnumerable AllThings {
			get {
				return things;
			}
		}

		//Property: uid
		//An identification number that no other Thing (AbstractItem, AbstractCharacter, etc) has.
		public int Uid {
			get {
				return uid;
			}
		}

		public virtual uint FlaggedUid {
			get {
				return (uint) Uid;
			}
		}

		public abstract byte FlagsToSend { get; }
		//Normal = 0x00,
		//Unknown = 0x01,
		//CanAlterPaperdoll = 0x02,
		//Poisoned = 0x04,
		//GoldenHealth = 0x08,
		//Unknown2 = 0x10,
		//Unknown3 = 0x20,
		//WarMode = 0x40, - used as such in scripts
		//Hidden = 0x80

		//Property: x
		//The x coordinate of this Thing. To change something's position, it's generally recommended that you set P
		//or use Go instead.
		public ushort X {
			get {
				ThrowIfDeleted();
				return point4d.x;
			}
			set {
				P(value, Y, Z, M);	//maybe the compiler will optimize this for us... I hope so! -SL
			}
		}
		public ushort Y {
			get {
				ThrowIfDeleted();
				return point4d.y;
			}
			set {
				P(X, value, Z, M);	//maybe the compiler will optimize this for us... I hope so! -SL
			}
		}
		public sbyte Z {
			get {
				ThrowIfDeleted();
				return point4d.z;
			}
			set {
				P(X, Y, value, M);	//maybe the compiler will optimize this for us... I hope so! -SL
			}
		}

		//public byte mapplane { get { return m; } set { m = value; } }
		public byte M {
			get {
				ThrowIfDeleted();
				return point4d.m;
			}
			set {
				P(X, Y, Z, value);	//maybe the compiler will optimize this for us... I hope so! -SL
			}
		}

		public Point4D P() {
			return new Point4D(point4d);
		}


		public IPoint4D TopPoint {
			get {
				return this.TopObj();
			}
		}

		IPoint3D IPoint3D.TopPoint {
			get {
				return this.TopObj();
			}
		}

		IPoint2D IPoint2D.TopPoint {
			get {
				return this.TopObj();
			}
		}

		public abstract bool Flag_Disconnected { get; set; }

		public void P(ushort x, ushort y) {
			P(x, y, Z, M);	//maybe the compiler will optimize this for us... I hope so! -SL
		}

		public void P(Point2D point) {
			P(point.X, point.Y, Z, M);
		}

		public void P(IPoint2D point) {
			P(point.X, point.Y, Z, M);
		}

		public void P(ushort x, ushort y, sbyte z) {
			P(x, y, z, M);	//maybe the compiler will optimize this for us... I hope so! -SL
		}

		public void P(Point3D point) {
			P(point.X, point.Y, point.Z, M);
		}

		public void P(IPoint3D point) {
			P(point.X, point.Y, point.Z, M);
		}

		public void P(ushort x, ushort y, sbyte z, byte m) {
			SetPosImpl(new MutablePoint4D(x, y, z, m));
		}

		public void P(Point4D point) {
			P(point.X, point.Y, point.Z, point.M);
		}

		public void P(IPoint4D point) {
			P(point.X, point.Y, point.Z, point.M);
		}

		public abstract Thing Cont {
			get;
			set;
		}

		//This verifies that the coordinate specified is inside the appropriate map (by asking Map if it's a valid pos),
		//and then sets
		//CheckVisibility itself only if the character turns instead of moving (Since P isn't updated in that case).
		//this is completely overriden in AbstractCharacter
		internal abstract void SetPosImpl(MutablePoint4D point);


		//Changes layer ('z') silently, without triggering resync or sector update code
		protected void SetLayerSilently(byte layer) {
			point4d.z=(sbyte) layer;
		}

		public Map GetMap() {
			return Map.GetMap(M);
		}

		//There was a Resync method here which called Resend, but Resync is supposed to be a character-only
		//command, its purpose being to resend everything to that client only
		//(NOT to resend this thing to every client who can see it, which is what Resend does).
		//So I've deleted the Resync method which was here. (SL)

		public void NudgeUp() {
			NudgeUp(1);
		}
		public void NudgeDown() {
			NudgeDown(1);
		}

		public void NudgeUp(short amt) {
			sbyte tmpZ=Z;
			try {
				tmpZ=checked((sbyte) (tmpZ+amt));
				Z=tmpZ;
			} catch (OverflowException) {
				//OverheadMessage("This cannot be nudged that much (It would make its z coordinate too high).");
				Z=-128;
			}
		}
		public void NudgeDown(short amt) {
			sbyte tmpZ=Z;
			try {
				tmpZ=checked((sbyte) (tmpZ-amt));
				Z=tmpZ;
			} catch (OverflowException) {
				//OverheadMessage("This cannot be nudged that much (It would make its z coordinate too low).");
				Z=127;
			}
		}

		public void Update() {
			Resend();
		}
		public void UpdateX() {
			RemoveFromView();
			Resend();
		}

		public ushort Color {
			get {
				return color;
			}
			set {
				if (value>=0 && (value&~0xc000)<=0xbb6) {
					if (IsItem) {
						NetState.ItemAboutToChange((AbstractItem) this);
					} else {
						NetState.AboutToChangeBaseProps((AbstractCharacter) this);
					}
					color=value;
				}
			}
		}

		public long CreatedAt {
			get {
				return createdAt;
			}
		}

		public ThingDef def { get { return _def; } }

		public virtual bool IsPlayer { get { return false; } }

		public abstract int Height { get; }

		public override bool IsDeleted { get { return (uid==-1); } }
		public bool IsBeingCreated { get { return (uid==-2); } }

		//------------------------
		//Static Thing methods

		//method: DeleteThing
		//Call this to delete a thing
		internal static void DeleteThing(Thing t) {
			if (t==null || t.IsDeleted) {
				//throw new ServerException("Attempt to delete item which is already deleted.");
				return;
			}
			t.RemoveFromView();
			t.Trigger_Destroy();
			//this should not be done inside the BeingDeleted,
			//so that its possible to delete some items without update just by calling BeingDeleted
		}

		//call this to delete a thing without update packets being sent to clients.
		//AbstractItem and AbstractCharacter remove themselves from the map when BeingDeleted is called, but this is done separately
		//by them instead of here because AbstractItem doesn't remove itself from the map if it was in a container
		//(it removes itself from that instead) - by the time we get to Thing's BeingDeleted, the item is already
		//removed from its container, so we can't check here. Although, we COULD check if the x/y coords are
		//0xffff,0xffff, which is what they're set to when removed from a container (until they are put somewhere new,
		//which won't happen if they're being deleted).
		internal protected override void BeingDeleted() {
			things.RemoveAt(uid);
			uid=-1;
			base.BeingDeleted();
		}

		public static int UidClearFlags(int uid) {
			return (int) (((uint) uid)&~0xc0000000);			//0x4*, 0x8*, * meaning zeroes padded to 8 digits total
		}

		public static bool UidIsValid(int uid) {
			return things.IsValid(uid);
		}

		public static AbstractCharacter UidGetCharacter(int uid) {
			return things.Get(uid) as AbstractCharacter;
		}

		public static AbstractItem UidGetContainer(int uid) {
			AbstractItem i = things.Get(uid) as AbstractItem;
			if ((i != null) && (i.IsContainer)) {
				return i;
			}
			return null;
		}

		public static AbstractItem UidGetItem(int uid) {
			if (!UidIsValid(uid)) return null;
			return things.Get(uid) as AbstractItem;
		}

		public static Thing UidGetThing(int uid) {
			return things.Get(uid);
		}

		public static void UidDelete(int uid) {
			Thing t = things.Get(uid);
			if (t != null) {
				DeleteThing(t);
			}
		}

		public static void RegisterTriggerGroup(TriggerGroup tg) {
			if (!registeredTGs.Contains(tg)) {
				registeredTGs.Add(tg);
			}
		}

		public override void Trigger(TriggerKey td, ScriptArgs sa) {
			ThrowIfDeleted();
			for (int i = 0, n = registeredTGs.Count; i<n; i++) {
				TriggerGroup tg = registeredTGs[i];
				tg.Run(this, td, sa);
			}
			base.Trigger(td, sa);
			_def.Trigger(this, td, sa);
		}

		public override void TryTrigger(TriggerKey td, ScriptArgs sa) {
			ThrowIfDeleted();
			for (int i = 0, n = registeredTGs.Count; i<n; i++) {
				TriggerGroup tg = registeredTGs[i];
				tg.TryRun(this, td, sa);
			}
			base.TryTrigger(td, sa);
			_def.TryTrigger(this, td, sa);
		}

		public override bool CancellableTrigger(TriggerKey td, ScriptArgs sa) {
			ThrowIfDeleted();
			for (int i = 0, n = registeredTGs.Count; i<n; i++) {
				TriggerGroup tg = registeredTGs[i];
				object retVal = tg.Run(this, td, sa);
				try {
					int retInt = Convert.ToInt32(retVal);
					if (retInt == 1) {
						return true;
					}
				} catch (Exception) {
				}
			}
			if (base.CancellableTrigger(td, sa)) {
				return true;
			} else {
				return _def.CancellableTrigger(this, td, sa);
			}
		}

		public override bool TryCancellableTrigger(TriggerKey td, ScriptArgs sa) {
			ThrowIfDeleted();
			for (int i = 0, n = registeredTGs.Count; i<n; i++) {
				TriggerGroup tg = registeredTGs[i];
				object retVal = tg.TryRun(this, td, sa);
				try {
					int retInt = Convert.ToInt32(retVal);
					if (retInt == 1) {
						return true;
					}
				} catch (Exception) {
				}
			}
			if (base.TryCancellableTrigger(td, sa)) {
				return true;
			} else {
				return _def.TryCancellableTrigger(this, td, sa);
			}
		}

		//------------------------
		//Public methods

		public override abstract string Name { get; set; }

		public abstract ushort Model { get; set; }

		public virtual bool IsEquipped {
			get {
				return false;
			}
		}

		public virtual bool IsOnGround {
			get {
				return true;
			}
		}

		public virtual bool IsInContainer {
			get {
				return false;
			}
		}

		public abstract bool IsInvisible { get; }

		public virtual Region Region {
			get {
				if (region == null) {
					region = GetMap().GetRegionFor(point4d);
				}
				return region;
			}
		}

		///**
		//	This is called by NetState once updates have been sent out for this thing. This simply
		//	instructs the Thing to reset netStateChanged to false. (The NetState object is discarded
		//	shortly after this is called; It is passed here only so that this Thing can verify that it
		//	is in fact the proper NetState object)
		// */
		//internal void NetStateSyncDone(NetState stateObj) {
		//	Sanity.IfTrueSay(netStateChanged==false, "NetStateSynchronized was called, but netStateChanged is false.");
		//	Sanity.IfTrueSay(stateObj==null, "NetStateSynchronized was called, but stateObj is null.");
		//	Sanity.IfTrueSay(stateObj==null, "NetStateSynchronized was called, but stateObj is null.");
		//	if (netStateChanged==true && stateObj.Thing==this) {
		//		netStateChanged=false;
		//	}
		//}


		//[Summary("This should be called immediately before anything is changed which will require sending of some packets\
		//	to inform clients of the change.")] 
		//[Remarks("NetState will determine what has changed, what to send, and sending it.\
		//	All you have to do it call this BEFORE making a change.\
		//	This should be called by the setters for things like Model, Color, etc, it is not intended to be called\
		//	from scripts or anything like that - anything they change which will result in needed resending should\
		//	itself call this.\
		//	Calling this more than once per cycle is harmless and perfectly OK to do.")]
		//public void AboutToChange(NSFlags flags) {
		//	CheckDeleted();
		//	NetState.AboutToChange(this, flags);
		//}
		//
		//[Summary("This should be called immediately before a skill is changed. which will require sending \
		//	of some packets to inform clients of the change")]
		//[Remarks("NetState will determine what has changed, what to send, and sending it.\
		//	All you have to do it call this BEFORE making a change.\
		//	This should be called by the setters of ISkill.\
		//	Calling this more than once per cycle is harmless and perfectly OK to do.")]
		//public void AboutToChangeSkill(ushort skillId) {
		//	CheckDeleted();
		//	NetState.AboutToChangeSkill(this, skillId);
		//}


		//internal void NSMessage(NSMsg msg, params object[] args) {
		//	Sanity.IfTrueThrow(!netStateChanged, "You can only use NSMessage once AboutToChange has been used this cycle.");
		//	NetState.Message(this, msg, args);
		//}

		public virtual void AddItem(AbstractCharacter addingChar, AbstractItem iToAdd) {
			throw new InvalidOperationException("The thing ("+this+") can not contain items");
		}

		public virtual void AddItem(AbstractCharacter addingChar, AbstractItem iToAdd, ushort x, ushort y) {
			throw new InvalidOperationException("The thing ("+this+") can not contain items");
		}

		internal virtual void DropItem(AbstractCharacter pickingChar, AbstractItem i) {
			throw new InvalidOperationException("The thing ("+this+") can not contain items");
		}

		public abstract AbstractItem FindCont(int index);

		public abstract IEnumerator GetEnumerator();

		//------------------------
		//Private & internal stuff


		internal static void StartingLoading() {
			loadedCharacters=0;
			loadedItems=0;
		}

		internal static void Load(PropsSection input) {
			//an Exception propagated away from this method is usually considered critical - load failed

			//is called for each separate section

			//Console.WriteLine("["+input.headerType+" "+input.headerName+"]");
			//no exception, ignore case.

			//we need defname and p(x,y, z, m)  to construct the thing.

			ThingDef thingDef = ThingDef.Get(input.headerName) as ThingDef;


			if (thingDef == null) {
				Logger.WriteError(input.filename, input.headerLine, "Defname '"+LogStr.Ident(input.headerName)+"' not found. Thing loading interrupted.");
				return;
			}
			Type type = ThingDef.GetDefTypeByThingName(input.headerType);
			if (thingDef.GetType() != type) {
				Logger.WriteWarning("Saved thing declares wrong class ("+LogStr.Ident(type)+"). Using "+LogStr.Ident(thingDef.GetType()));
			}

			int _uid;
			PropsLine prop = input.TryPopPropsLine("uid");
			if (prop==null) {
				prop=input.TryPopPropsLine("serial");
			}
			if (prop!=null) {
				if (!TagMath.TryParseInt32(prop.value, out _uid)) {
					Logger.WriteError(input.filename, prop.line, "Unrecognized UID property format. Thing loading interrupted.");
					return;
				}
			} else {
				Logger.WriteError(input.filename, input.headerLine, "UID property not found. Thing loading interrupted.");
				return;
			}
			_uid = UidClearFlags(_uid);

			Thing.uidBeingLoaded = _uid;//the constructor should set this as Uid
			Thing constructed = thingDef.CreateWhenLoading(1, 1, 0, 0);//let's hope the P gets loaded properly later ;)
			Thing.things.Add(constructed, _uid);

			//now load the rest of the properties
			constructed.On_Load(input);
			constructed.LoadSectionLines(input);
			if (constructed.IsChar) loadedCharacters++;
			if (constructed.IsItem) loadedItems++;
		}

		internal static void LoadingFinished() {
			//this means real end of file(s)
			uidBeingLoaded = -1;
			things.LoadingFinished();
			Logger.WriteDebug(string.Format(
											"Loaded {0} things: {1} items and {2} characters.",
											loadedItems+loadedCharacters, loadedItems, loadedCharacters));
			foreach (Thing t in things) {
				t.On_AfterLoad();
			}

			return;
		}

		[Summary("This is called after this object was is being loaded.")]
		public virtual void On_AfterLoad() {
		}

		[Summary("This is called when this object is being loaded, before the LoadLine calls. It exists because of ClassTemplate and it's autogenerating of the Save method itself. With this, it's possible to implement user save code...")]
		public virtual void On_Load(PropsSection output) {
		}

		protected override void LoadLine(string filename, int line, string prop, string value) {
			ThrowIfDeleted();
			switch (prop) {
				case "p":
					point4d = MutablePoint4D.Parse(value);
					//it will be put in world later by map or Cont
					break;
				case "color":
					color = TagMath.ParseUInt16(value);
					break;
				case "createdat":
					createdAt = TagMath.ParseInt64(value);
					break;
				default:
					//here should come some skill loader, but since skills
					//are not yet implemented, isnt this implemented too
					base.LoadLine(filename, line, prop, value);
					break;
			}
		}


		public static void SaveAll(SaveStream output) {
			Logger.WriteDebug("Saving Things.");
			output.WriteComment("Textual SteamEngine save");
			output.WriteComment("Things");
			output.WriteLine();
			savedCharacters=0;
			savedItems=0;
			alreadySaved=new ArrayList(things.HighestElement);
			foreach (Thing t in things) {
				SaveThis(output, t.TopObj());//each thing should recursively save it's contained items
			}
			output.WriteLine("[EOF]");
			Logger.WriteDebug(string.Format(
											"Saved {0} things: {1} items and {2} characters.",
											savedCharacters+savedItems, savedItems, savedCharacters));
			alreadySaved = null;
		}

		internal static void SaveThis(SaveStream output, Thing t) {
			if (t.IsDeleted) {
				Logger.WriteError("Thing "+LogStr.Ident(t)+" is already deleted.");
				return;
			}
			while (alreadySaved.Count<=t.Uid) {
				alreadySaved.Add(null);
			}
			if (alreadySaved[t.Uid]==null) {
				alreadySaved[t.Uid]=true;
				if (t.IsChar) {
					savedCharacters++;
				} else if (t.IsItem) {
					savedItems++;
				} else {
					Logger.WriteError("Unknown Thing type. uid="+LogStr.Ident(t.Uid));
					return;
				}
				output.WriteSection(t.GetType().Name, t.def.PrettyDefname);
				t.Save(output);
				output.WriteLine();
				ObjectSaver.FlushCache(output);
				if (t.CanContain) {
					foreach (AbstractItem i in t) {
						SaveThis(output, i);
					}
				}
			}
		}

		public override void Save(SaveStream output) {
			ThrowIfDeleted();
			output.WriteValue("uid", uid);
			if (M==0) {
				if (Z==0) {
					output.WriteValue("p", X+","+Y);
				} else {
					output.WriteValue("p", X+","+Y+","+Z);
				}
			} else {
				output.WriteValue("p", X+","+Y+","+Z+","+M);
			}
			if (Color!=0) {
				output.WriteValue("color", color);
			}
			output.WriteValue("createdat", createdAt);
			On_Save(output);
			base.Save(output);//tagholder save
		}

		[Summary("This is called when this object is being saved. It exists because of ClassTemplate and it's autogenerating of the Save method itself. With this, it's possible to implement custom save code...")]
		public virtual void On_Save(SaveStream output) {

		}

		[Summary("This is called when this object is being duped. It exists because of ClassTemplate and it's autogenerating of the copy constructor itself. With this, it's possible to implement custom copy code...")]
		public virtual void On_Dupe(Thing copyFrom) {

		}

		public virtual Thing TopObj() {
			return this;
		}

		public abstract Thing Dupe();


		/*
			Method: Dupe
			
				Duplicates this character, possibly more than once.
			
			Parameters:
				ntimes - The number of times to duplicate this character.
		 */
		public void Dupe(int ntimes) {
			ThrowIfDeleted();
			//precondition sanity checks
			Sanity.IfTrueThrow(ntimes<=0, "Dupe called with "+ntimes+" times - Only values above 0 are valid.");

			while (ntimes>0) {
				Dupe();
				ntimes--;
			}
		}

		/*
			Member: weight
			The weight of this thing. If you're going to change the weight on an item (instead of changing
			the weight on the itemdef/chardef to change the weight for all items with that def), always do it by
			setting this property. This ensures that the stuff this thing is in (containers, characters, etc)
			updates weight properly.
			
			well obviously I disable dsetting the weight. I may re-enable it once I need it, but probably not at this level -tar
		 */

		public abstract float Weight { get; }
		//	get {
		//		CheckDeleted();
		//		if (HasTag(weightTag)) {
		//			double wtag = GetTag0(weightTag);
		//			return (float) wtag;
		//		}
		//		return def.Weight;
		//	} set {
		//		CheckDeleted();
		//		float oldWeight = Weight;
		//		SetTag(weightTag,value);
		//		if (Cont!=null) {
		//			float weightChange = oldWeight - value;
		//			Cont.DecreaseWeightBy(weightChange);
		//		}
		//	}
		//}


		public abstract void FixWeight();

		public abstract void AdjustWeight(float adjust);

		//internal virtual void DecreaseWeightBy(float adjust) {
		//}
		//internal virtual void IncreaseWeightBy(float adjust) {
		//}


		//fires the @destroy trigger on this Thing, after that calls BeingDeleted(),
		//which removes the item from world. If you want to remove a Thing without it being refreshed for clients,
		//use this method instead of Thing.DeleteThing()
		internal void Trigger_Destroy() {
			//in fact the src is quite undefined, it can also be null...
			//it`s here just to keep it the same for all triggers, as usual.
			//ScriptArgs sa = new ScriptArgs();
			TryTrigger(TriggerKey.Destroy, null);
			On_Destroy();

			BeingDeleted();
		}

		//method:On_Destroy
		//Thing`s implementation of trigger @Destroy,
		//does actually nothing, because finalizing of core Thing subclasses is done elsewhere
		public virtual void On_Destroy() {
			//this does nothing by default, because core classes use BeingDeleted (for safety against evil scripts)
		}

		public void Click() {
			ThrowIfDeleted();
			Trigger_Click(Globals.SrcCharacter);
		}

		//method: Trigger_Click
		//fires the @itemClick / @charclick and @click triggers where this is the Thing being clicked on.
		public void Trigger_AosClick(AbstractCharacter clicker) {
			ThrowIfDeleted();
			if (clicker==null)
				return;
			if (!clicker.CanSeeForUpdate(this)) {
				if (clicker.Conn!=null) {
					PacketSender.PrepareRemoveFromView(this);
					PacketSender.SendTo(clicker.Conn, true);
				}
			} else {
				bool cancel = false;
				ScriptArgs sa = new ScriptArgs(clicker, this);
				clicker.act=this;
				cancel=TryCancellableTrigger(TriggerKey.aosClick, sa);
				if (!cancel) {
					//@aosClick on thing did not return 1
					On_AosClick(clicker);
				}
			}
		}

		//method: Trigger_Click
		//fires the @itemClick / @charclick and @click triggers where this is the Thing being clicked on.
		public void Trigger_Click(AbstractCharacter clicker) {
			ThrowIfDeleted();
			if (clicker==null)
				return;
			if (!clicker.CanSeeForUpdate(this)) {
				if (clicker.Conn!=null) {
					PacketSender.PrepareRemoveFromView(this);
					PacketSender.SendTo(clicker.Conn, true);
				}
			} else {
				bool cancel = false;
				ScriptArgs sa = new ScriptArgs(clicker, this);
				clicker.act=this;
				cancel=TriggerSpecific_Click(clicker, sa);
				if (!cancel) {
					//@itemclick or @charclick on src did not return 1
					cancel=TryCancellableTrigger(TriggerKey.click, sa);
					if (!cancel) {
						//@click on item did not return 1
						On_Click(clicker);
					}
				}
			}
		}

		internal abstract bool TriggerSpecific_Click(AbstractCharacter src, ScriptArgs sa);

		public virtual void GetNameCliloc(out uint id, out string argument) {
			id = 1042971;
			argument = this.Name;
		}

		public virtual void AddProperties(ObjectPropertiesContainer opc) {

		}

		public ObjectPropertiesContainer GetProperties() {
			ObjectPropertiesContainer opc = ObjectPropertiesContainer.Get(this);
			if (opc != null) {
				if (opc.Frozen) {
					return opc;//unchanged
				}
			}
			if (opc == null) {
				opc = new ObjectPropertiesContainer(this);
			}

			uint id;
			string argument;
			GetNameCliloc(out id, out argument);
			opc.AddLine(id, argument);

			AddProperties(opc);
			opc.Freeze();

			return opc;//new or changed
		}

		//method: On_Click
		//Thing`s implementation of trigger @Click,
		//sends the name (DecoratedName) of this Thing as plain overheadmessage
		public virtual void On_Click(AbstractCharacter clicker) {
			Server.SendNameFrom(clicker.Conn, this,
				this.Name, 0);
		}

		public virtual void On_AosClick(AbstractCharacter clicker) {
			//does this ever happen? :)
			ObjectPropertiesContainer opc = this.GetProperties();
			Server.SendClilocNameFrom(clicker.Conn, this,
				opc.FirstId, 0, opc.FirstArgument);
		}

		public void ChangingProperties() {
			NetState.AboutToChangeProperty(this);
			ObjectPropertiesContainer opc = ObjectPropertiesContainer.Get(this);
			if (opc != null) {
				opc.Unfreeze();
			}
		}

		public void DClick() {
			Trigger_DClick(Globals.SrcCharacter);
		}

		public void Use() {
			Trigger_DClick(Globals.SrcCharacter);
		}

		//method: Trigger_DClick
		//fires the @itemDClick / @charDClick and @dclick triggers where this is the Thing being doubleclicked.
		public void Trigger_DClick(AbstractCharacter dclicker) {
			ThrowIfDeleted();
			if (dclicker==null)
				return;
			if (!dclicker.CanSeeForUpdate(this)) {
				if (dclicker.Conn!=null) {
					PacketSender.PrepareRemoveFromView(this);
					PacketSender.SendTo(dclicker.Conn, true);
				}
			} else {
				bool cancel=false;
				ScriptArgs sa = new ScriptArgs(dclicker, this);
				dclicker.act=this;
				cancel=TriggerSpecific_DClick(dclicker, sa);
				if (!cancel) {
					//@itemDClick or @charDClick on src did not return 1
					cancel=TryCancellableTrigger(TriggerKey.DClick, sa);
					if (!cancel) {
						//@DClick on item did not return 1
						On_DClick(dclicker);
					}
				}
			}
		}

		internal virtual bool TriggerSpecific_DClick(AbstractCharacter src, ScriptArgs sa) {
			throw new NotSupportedException(this+" is nor AbstractItem nor Character?");
		}

		//method:On_DClick
		//Thing`s implementation of trigger @DClick,
		//does nothing.
		public virtual void On_DClick(AbstractCharacter from) {

		}

		public virtual bool On_StackOn(AbstractCharacter stackingCharacter, AbstractItem i, ref object objX, ref object objY) {
			//stackingCharacter is dropping/stacking item i on/with me
			return false;
		}

		//method:On_Create
		//Thing`s implementation of trigger @create,
		//does nothing.
		public virtual void On_Create() {

		}

		public override string ToString() {
			return Name+" (0x"+Uid.ToString("x")+")";
		}

		public override int GetHashCode() {
			return Uid;
		}

		public override bool Equals(Object obj) {
			if (obj is Thing) {
				Thing tobj = (Thing) obj;
				return (tobj.Uid==this.Uid);
			} else {
				return false;
			}
		}

		public virtual bool IsItem {
			get {
				return false;
			}
		}

		public virtual bool IsContainer {
			get {
				return false;
			}
		}

		public bool IsMulti {
			get {
				return def.multiData != null;
			}
		}

		public virtual bool CanContain {
			get {
				//i.e. if this implements IContainer
				return false;
			}
		}

		public virtual bool IsEquippable {
			get {
				return false;
			}
		}

		public virtual bool IsChar {
			get {
				return false;
			}
		}

		public AbstractItem Newitem(IThingFactory factory) {
			return Newitem(factory, 1);
		}

		public virtual AbstractItem Newitem(IThingFactory factory, uint amount) {
			ThrowIfDeleted();
			Thing t = factory.Create(point4d.x, point4d.y, point4d.z, point4d.m);
			AbstractItem i = t as AbstractItem;
			if (i != null) {
				if (i.IsStackable) {
					i.Amount=amount;
				}
				return i;
			}
			if (t != null) {
				DeleteThing(t);//we created a character, wtf? :)
			}
			throw new SEException(factory+" did not create an item.");
		}

		public virtual AbstractCharacter Newnpc(IThingFactory factory) {
			ThrowIfDeleted();
			Thing t = factory.Create(point4d.x, point4d.y, point4d.z, point4d.m);
			AbstractCharacter c = t as AbstractCharacter;
			if (c != null) {
				return c;
			}
			if (t != null) {
				DeleteThing(t);//we created an item, wtf? :)
			}
			throw new SEException(factory+" did not create a character.");
		}

		public override void Delete() {
			if (IsPlayer) {
				Message("Unable to remove player characters with Delete unless you use remove('n'): 'n' must be a number greater than 0.");
				return;
			}
			DeleteThing(this);
		}


		public void DeletePlayer() {
			DeleteThing(this);
		}

		public void Move(string dir) {
			Move(dir, 1);
		}

		public static void ClearAll() {
			foreach (Thing t in things) {
				t.uid = -1;
			}
			things.Clear();
		}

		public void RemoveFromView() {
			PacketSender.PrepareRemoveFromView(this);
			PacketSender.SendToClientsWhoCanSee(this);
		}

		public void Resend() {
			NetState.Resend(this);
		}


		//public GumpInstance Dialog(string gumpName) {
		//	Gump Gump = Gump.Get(gumpName);
		//	if (Gump == null) {
		//		throw new SEException("There is no gump named "+LogStr.Ident(gumpName));
		//	} else {
		//		return Globals.src.Character.SendGump(this, Gump, null);
		//	}
		//}

		public GumpInstance Dialog(Gump gump) {
			return Dialog(Globals.SrcCharacter, gump);
		}

		public GumpInstance Dialog(Gump gump, params object[] args) {
			return Dialog(Globals.SrcCharacter, gump, args);
		}

		public GumpInstance Dialog(AbstractCharacter sendTo, Gump gump, params object[] args) {
			if (sendTo != null) {
				return sendTo.SendGump(this, gump, args);
			}
			return null;
		}

		public void Message(string arg) {
			Message(arg, 0);
		}

		public void Message(string arg, int color) {
			ThrowIfDeleted();
			Server.SendOverheadMessageFrom(Globals.SrcGameConn, this, arg, (ushort) color);
		}

		public void ClilocMessage(uint msg, string args) {
			ThrowIfDeleted();
			PacketSender.PrepareClilocMessage(this, msg, SpeechType.Speech, 3, 0, args);
			PacketSender.SendTo(Globals.SrcGameConn, true);
		}
		public void ClilocMessage(uint msg, params string[] args) {
			ThrowIfDeleted();
			PacketSender.PrepareClilocMessage(this, msg, SpeechType.Speech, 3, 0, string.Join("\t", args));
			PacketSender.SendTo(Globals.SrcGameConn, true);
		}


		public void ClilocMessage(uint msg, int color, string args) {
			ThrowIfDeleted();
			PacketSender.PrepareClilocMessage(this, msg, SpeechType.Speech, 3, (ushort) color, args);
			PacketSender.SendTo(Globals.SrcGameConn, true);
		}
		public void ClilocMessage(uint msg, int color, params string[] args) {
			ThrowIfDeleted();
			PacketSender.PrepareClilocMessage(this, msg, SpeechType.Speech, 3, (ushort) color, string.Join("\t", args));
			PacketSender.SendTo(Globals.SrcGameConn, true);
		}


		public void SoundTo(SoundFX soundId, GameConn toClient) {
			if (soundId!=SoundFX.None) {
				if (toClient != null) {
					PacketSender.PrepareSound(this, soundId);
					PacketSender.SendTo(toClient, true);
				}
			}
		}

		public void SoundTo(SoundFX soundId, AbstractCharacter toPlayer) {
			if (soundId!=SoundFX.None) {
				if (toPlayer != null) {
					GameConn conn = toPlayer.Conn;
					if (conn != null) {
						PacketSender.PrepareSound(this, soundId);
						PacketSender.SendTo(conn, true);
					}
				}
			}
		}

		public void Sound(SoundFX soundId) {
			if (soundId!=SoundFX.None) {
				PacketSender.PrepareSound(this, soundId);
				PacketSender.SendToClientsInRange(this.TopPoint, Globals.MaxUpdateRange);
			}
		}
		public void Sound(SoundFX soundId, ushort range) {
			if (soundId!=SoundFX.None) {
				PacketSender.PrepareSound(this, soundId);
				PacketSender.SendToClientsInRange(this.TopPoint, range);
			}
		}
		public void Sfx(SoundFX soundId) {
			Sound(soundId);
		}

		/**
			Makes this thing say something to all clients who can hear it.
			
			@param arg The text to say
		 */
		public void Say(string arg) {
			Sanity.IfTrueThrow(arg==null, "arg cannot be null in Say.");
			Speech(arg, 0, SpeechType.Speech, 0, 3, null, null);
		}

		/**
			Makes this thing say something to all clients who can hear it.
			
			@param arg The text to say
			@param color The color the text should be
		 */
		public void Say(string arg, ushort color) {
			Sanity.IfTrueThrow(arg==null, "arg cannot be null in Say.");
			Speech(arg, 0, SpeechType.Speech, color, 3, null, null);
		}

		/**
			Makes this thing say something to all clients who can hear it.
			
			@param arg The text to say
			@param color The color the text should be
			@param font The font to use for the text (The default is 3)
		 */
		public void Say(string arg, ushort color, byte font) {
			Sanity.IfTrueThrow(arg==null, "arg cannot be null in Say.");
			Speech(arg, 0, SpeechType.Speech, color, font, null, null);
		}

		/**
			Makes this thing say something to all clients who can hear it.
			
			@param arg The cliloc entry # to say
			@param args Additional args needed for the cliloc entry, if any.
		 */
		public void ClilocSay(uint arg, params string[] args) {
			Speech(null, arg, SpeechType.Speech, 0, 3, null, args);
		}

		/**
			Makes this thing say something to all clients who can hear it.
			
			@param arg The cliloc entry # to say
			@param color The color the text should be
			@param args Additional args needed for the cliloc entry, if any.
		 */
		public void ClilocSay(uint arg, ushort color, params string[] args) {
			Speech(null, arg, SpeechType.Speech, color, 3, null, args);
		}

		/**
			Makes this thing say something to all clients who can hear it.
			
			@param arg The cliloc entry # to say
			@param color The color the text should be
			@param font The font to use for the text (The default is 3)
			@param args Additional args needed for the cliloc entry, if any.
		 */
		public void ClilocSay(uint arg, ushort color, byte font, params string[] args) {
			Speech(null, arg, SpeechType.Speech, color, font, null, args);
		}

		/**
			Makes this thing yell something to all clients who can hear it.
			
			@param arg The text to yell
			@param color The color the text should be
		 */
		public void Yell(string arg, ushort color) {
			ThrowIfDeleted();
			Sanity.IfTrueThrow(arg==null, "arg cannot be null in Yell.");
			Speech(arg, 0, SpeechType.Yell, color, 3, null, null);
		}

		/**
			Makes this thing yell something to all clients who can hear it.
			
			@param arg The cliloc entry # to yell
			@param color The color the text should be
			@param args Additional args needed for the cliloc entry, if any.
		 */
		public void ClilocYell(uint arg, ushort color, params string[] args) {
			ThrowIfDeleted();
			Speech(null, arg, SpeechType.Yell, color, 3, null, args);
		}

		/**
			Makes this thing send a whisper to all clients who can hear it.
			
			@param arg The text to whisper
			@param color  The color the text should be
		 */
		public void Whisper(string arg, ushort color) {
			ThrowIfDeleted();
			Sanity.IfTrueThrow(arg==null, "arg cannot be null in Whisper.");
			Speech(arg, 0, SpeechType.Whisper, color, 3, null, null);
		}

		/**
			Makes this thing send a whisper to all clients who can hear it.
			
			@param arg The cliloc entry # to whisper
			@param color The color the text should be
			@param args Additional args needed for the cliloc entry, if any.
		 */
		public void ClilocWhisper(uint arg, ushort color, params string[] args) {
			ThrowIfDeleted();
			Speech(null, arg, SpeechType.Whisper, color, 3, null, args);
		}

		/**
			Makes this thing send an emote to all clients in range.
			
			@param arg The text to emote
			@param color  The color the text should be
		 */
		public void Emote(string arg, ushort color) {
			ThrowIfDeleted();
			Sanity.IfTrueThrow(arg==null, "arg cannot be null in Emote.");
			Speech(arg, 0, SpeechType.Emote, color, 3, null, null);
		}

		/**
			Makes this thing send an emote to all clients in range.
			
			@param arg The cliloc entry # to emote
			@param color The color the text should be
			@param args Additional args needed for the cliloc entry, if any.
		 */
		public void ClilocEmote(uint arg, ushort color, params string[] args) {
			ThrowIfDeleted();
			Speech(null, arg, SpeechType.Emote, color, 3, null, args);
		}

		/**
			Makes this thing yell something to all clients who can hear it.
			
			@param arg The text to yell
		 */
		public void Yell(string arg) {
			Yell(arg, 0);
		}

		/**
			Makes this thing yell something to all clients who can hear it.
			
			@param arg The cliloc entry # to yell
			@param args Additional args needed for the cliloc entry, if any.
		 */
		public void ClilocYell(uint arg, params string[] args) {
			ClilocYell(arg, 0);
		}

		/**
			Makes this thing send a whisper to all clients who can hear it.
			
			@param arg The text to whisper
		 */
		public void Whisper(string arg) {
			Whisper(arg, 0);
		}
		/**
			Makes this thing send a whisper to all clients who can hear it.
			
			@param arg The cliloc entry # to whisper
			@param args Additional args needed for the cliloc entry, if any.
		 */
		public void ClilocWhisper(uint arg, params string[] args) {
			ClilocWhisper(arg, 0);
		}

		/**
			Makes this thing send an emote to all clients in range.
			
			@param arg The text to emote
		 */
		public void Emote(string arg) {
			Emote(arg, 0x22);
		}
		/**
			Makes this thing send an emote to all clients in range.
			
			@param arg The cliloc entry # to emote
			@param args Additional args needed for the cliloc entry, if any.
		 */
		public void ClilocEmote(uint arg, params string[] args) {
			ClilocEmote(arg, 0x22);
		}

		public void Fix() {
			sbyte oldZ = this.Z;
			sbyte newZ;
			GetMap().GetFixedZ(this, out newZ);
			if (oldZ != newZ) {
				this.Z = newZ;
			}
		}

		public virtual Season Season {
			get {
				return Season.Spring;
			}
		}

		public virtual CursorType Cursor {
			get {
				return CursorType.Gold;
			}
		}

		public void Move(string dir, int amount) {
			switch (dir) {
				case ("n"): {
						int y=Y-amount;
						if (y<0) {
							y=0;
						} else if (y>Server.MaxY(M)) {
							y=Server.MaxY(M);
						}
						Y=(ushort) y;
						break;
					}
				case ("ne"): {
						int y=Y-amount;
						int x=X+amount;
						if (y<0) {
							y=0;
						} else if (y>Server.MaxY(M)) {
							y=Server.MaxY(M);
						}
						if (x<0) {
							x=0;
						} else if (x>Server.MaxX(M)) {
							x=Server.MaxX(M);
						}
						P((ushort) x, (ushort) y, Z);	//This won't change our Z coordinate, whereas P(x,y) would.
						break;
					}
				case ("e"): {
						int x=X+amount;
						if (x<0) {
							x=0;
						} else if (x>Server.MaxX(M)) {
							x=Server.MaxX(M);
						}
						X=(ushort) x;
						break;
					}
				case ("se"): {
						int y=Y+amount;
						int x=X+amount;
						if (y<0) {
							y=0;
						} else if (y>Server.MaxY(M)) {
							y=Server.MaxY(M);
						}
						if (x<0) {
							x=0;
						} else if (x>Server.MaxX(M)) {
							x=Server.MaxX(M);
						}
						P((ushort) x, (ushort) y, Z);	//This won't change our Z coordinate, whereas P(x,y) would.
						break;
					}
				case ("s"): {
						int y=Y+amount;
						if (y<0) {
							y=0;
						} else if (y>Server.MaxY(M)) {
							y=Server.MaxY(M);
						}
						Y=(ushort) y;
						break;
					}
				case ("sw"): {
						int y=Y+amount;
						int x=X-amount;
						if (y<0) {
							y=0;
						} else if (y>Server.MaxY(M)) {
							y=Server.MaxY(M);
						}
						if (x<0) {
							x=0;
						} else if (x>Server.MaxX(M)) {
							x=Server.MaxX(M);
						} else {
							x=(ushort) X;
						}
						P((ushort) x, (ushort) y, Z);
						break;
					}
				case ("w"): {
						int x=X-amount;
						if (x<0) {
							x=0;
						} else if (x>Server.MaxX(M)) {
							x=Server.MaxX(M);
						}
						X=(ushort) x;
						break;
					}
				case ("nw"): {
						int y=Y-amount;
						int x=X-amount;
						if (y<0) {
							y=0;
						} else if (y>Server.MaxY(M)) {
							y=Server.MaxY(M);
						}
						if (x<0) {
							x=0;
						} else if (y>Server.MaxX(M)) {
							y=Server.MaxX(M);
						}
						P((ushort) x, (ushort) y, Z);
						break;
					}
				default: {
						throw new SEException(dir+" isn't a direction I'll accept. Use n, nw, ne, w, e, s, sw, or se.");
					}
			}
		}

		internal void ChangedP(Point4D oldP) {
			Map.ChangedP(this, oldP);
			AbstractCharacter self = this as AbstractCharacter;//why are you "creating" a src? why dont you take the Globals.src? -tar
			if (self!=null) {
				self.StepOnItems();
			}
		}

		/*
			Method: Speech
				Does the work of sending speech (of any type) to everyone within range, calling
				Trigger_Hear on NPCs, etc.
				If the speaker is hidden, they will be unhidden, unless they are actually invisible.
				Speech distance is calculated in here based on the speech type, based on your
				INI settings for speechDistance, whisperDistance, emoteDistance, and yellDistance.
				
				At present, NPCs only hear characters, not items.
				
			Parameters:
				speech - What will be said
				type - The type of the speech. Valid values are Speech, Yell, Emote, and Whisper. If anything
					else is sent, the type is treated as Speech. (The other types are Name and Server, which
					shouldn't be spoken)
				font - The font to speak in. 3 is the default, and is used by everything that doesn't specify
					a font. If this is not 3, then the speech will be sent in ASCII instead of Unicode, because
					Unicode doesn't support special fonts like runic.
		 */
		public void Speech(string speech, uint clilocSpeech, SpeechType type, ushort color, ushort font, int[] keywords, string[] args) {
			//todo: speech triggers?
			AbstractCharacter self = this as AbstractCharacter;

			string lang=null;
			if (self != null) {
				if (self.IsPlayer) {
					GameConn conn = self.Conn;
					if (conn!=null) {
						lang=conn.lang;
					}
					self.Trigger_Say(speech, type, keywords);
				}
			}

			uint dist=0;
			switch (type) {
				case SpeechType.Speech:
					dist=Globals.speechDistance;
					break;
				case SpeechType.Emote:
					dist=Globals.emoteDistance;
					break;
				case SpeechType.Whisper:
					dist=Globals.whisperDistance;
					break;
				case SpeechType.Yell:
					dist=Globals.yellDistance;
					break;
				default:
					dist=Globals.speechDistance;
					type=SpeechType.Speech;
					break;
			}

			Map map = this.GetMap();
			ushort x = this.point4d.x;
			ushort y = this.point4d.y;

			if ((self != null) && (self.IsPlayer)) {
				foreach (AbstractCharacter chr in map.GetCharsInRange(x, y, (ushort) dist)) {
					chr.Trigger_Hear(self, speech, clilocSpeech, type, color, font, lang, keywords, args);
				}
			} else {//item is speaking... no triggers fired, just send it to players
				bool packetPrepared=false;
				foreach (GameConn conn in map.GetClientsInRange(x, y, (ushort) dist)) {
					if (!packetPrepared) {
						if (speech==null) {
							PacketSender.PrepareClilocMessage(this, clilocSpeech, type, font, color, args==null?null:string.Join("\t", args));
						} else {
							PacketSender.PrepareSpeech(this, speech, type, font, color, lang);
						}
						packetPrepared=true;
					}
					PacketSender.SendTo(conn, false);
				}
				if (packetPrepared) {
					PacketSender.DiscardLastPacket();
				}
			}
		}

		/*
			Method: CheckForInvalidCoords
				This function only exists in the debug build, and is not run in non-debug builds.
				
				Checks the coords to see if they are outside the map. Since maps start at 0,0 and coordinates
				are unsigned, X and Y are only checked against the max X and Y for the map, and not against 0,0.
				
				This will definitely trip (Throw a SanityCheckException) if the item never got real coordinates
				after BeingDroppedFromContainer was called, so this is called as a sanity check by methods which
				call BeingDroppedFromContainer but rely on other methods to give it a real location
				(after the other methods have been called).
				
				This will not trip if the item is in a container (or equipped), since invalid coordinates ARE
				used for equipped items (Specifically, X=7000).
				
				This does not check Z.
		 */
		[Conditional("DEBUG")]
		internal void CheckForInvalidCoords() {
			bool throwExcep=false;
			if (!IsDeleted && IsOnGround) {
				if (X>Server.MaxX(M)) {
					throwExcep=true;
				} else if (Y>Server.MaxY(M)) {
					throwExcep=true;
				}
				if (throwExcep) {
					throw new SanityCheckException("Invalid coordinates detected: ("+X+","+Y+","+Z+","+M+")");
				}
			}
		}
	}
}