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
using System.Text.RegularExpressions;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using SteamEngine.Common;
using SteamEngine.Packets;
using System.Diagnostics;
using System.Configuration;
using SteamEngine.Persistence;
using SteamEngine.Regions;
using SteamEngine.Networking;
using SteamEngine.Communication;
using SteamEngine.Communication.TCP;

namespace SteamEngine {
	internal interface ObjectWithUid {
		int Uid { get; set; }
	}

	public abstract partial class Thing : PluginHolder, IPoint4D, IEnumerable<AbstractItem>, ObjectWithUid {
		public static bool ThingTracingOn = TagMath.ParseBoolean(ConfigurationManager.AppSettings["Thing Trace Messages"]);
		public static bool WeightTracingOn = TagMath.ParseBoolean(ConfigurationManager.AppSettings["Weight Trace Messages"]);

		private DateTime createdAt;//Server time of creation
		private ushort color;
		private ushort model;
		internal ThingDef def; //tis is changed even from outside the constructor in case of dupeitems...
		internal readonly MutablePoint4D point4d = new MutablePoint4D(0xffff, 0xffff, 0, 0); //made this internal because SetPosImpl is now abstract... -tar
		private int uid = -2;
		//internal Region region;
		//internal NetState netState;//No one is to touch this but the NetState class itself!

		internal object contOrTLL; //internal cos of ThingLinkedList

		private static int savedCharacters = 0;
		private static int savedItems = 0;
		private static List<bool> alreadySaved = new List<bool>();

		private static int loadedCharacters;
		private static int loadedItems;

		internal Thing nextInList = null;
		internal Thing prevInList = null;

		private static List<TriggerGroup> registeredTGs = new List<TriggerGroup>();

		private static UIDArray<Thing> things = new UIDArray<Thing>();
		private static int uidBeingLoaded = -1;
		public static TagKey weightTag = TagKey.Get("_weight_");


		public sealed class ThingSaveCoordinator : IBaseClassSaveCoordinator {
			public static readonly Regex thingUidRE = new Regex(@"^\s*#(?<value>(0x)?[\da-f]+)\s*$",
				RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

			public string FileNameToSave {
				get { return "things"; }
			}

			public void StartingLoading() {
				loadedCharacters = 0;
				loadedItems = 0;
			}

			public void SaveAll(SaveStream output) {
				Logger.WriteDebug("Saving Things.");
				output.WriteComment("Things");
				output.WriteLine();
				savedCharacters = 0;
				savedItems = 0;
				alreadySaved = new List<bool>(things.HighestElement);
				foreach (Thing t in things) {
					SaveThis(output, t.TopObj());//each thing should recursively save it's contained items
				}
				Logger.WriteDebug(string.Format(
												"Saved {0} things: {1} items and {2} characters.",
												savedCharacters + savedItems, savedItems, savedCharacters));

				alreadySaved = null;
			}

			public void LoadingFinished() {
				//this means real end of file(s)
				uidBeingLoaded = -1;
				things.LoadingFinished();
				Logger.WriteDebug(string.Format(
												"Loaded {0} things: {1} items and {2} characters.",
												loadedItems + loadedCharacters, loadedItems, loadedCharacters));
				foreach (Thing t in things) {
					try {
						t.On_AfterLoad();
					} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
				}
			}

			public Type BaseType {
				get { return typeof(Thing); }
			}

			public string GetReferenceLine(object value) {
				return "#" + ((Thing) value).uid;
			}

			public Regex ReferenceLineRecognizer {
				get { return thingUidRE; }
			}

			public object Load(Match m) {
				int uid = int.Parse(m.Groups["value"].Value, NumberStyles.Integer);
				Thing thing = Thing.UidGetThing(uid);
				if (thing != null) {
					return thing;
				} else {
					throw new NonExistingObjectException("There is no thing with uid " + LogStr.Number(uid) + " to load.");
				}
			}
		}

		//still needs to be put into the world
		protected Thing(ThingDef myDef) {
			this.def = myDef;
			this.model = myDef.Model;
			this.color = myDef.Color;
			if (uidBeingLoaded == -1) {
				things.Add(this);//sets uid
				this.Resend();
			} else {
				uid = uidBeingLoaded;
				this.point4d = new MutablePoint4D((ushort) Globals.dice.Next(0, 20), (ushort) Globals.dice.Next(0, 20), 0, 0);
			}
			Globals.lastNew = this;
		}

		protected Thing(Thing copyFrom)
			: base(copyFrom) { //copying constuctor

			this.Resend();
			things.Add(this);//sets uid
			point4d = new MutablePoint4D(copyFrom.point4d);
			def = copyFrom.def;
			color = copyFrom.color;
			model = copyFrom.model;
			//SetSectorPoint4D();
			Globals.lastNew = this;
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

		int ObjectWithUid.Uid {
			get {
				return uid;
			}
			set {
				uid = value;
			}
		}

		public virtual uint FlaggedUid {
			get {
				return (uint) Uid;
			}
		}

		public abstract byte FlagsToSend { get; }
		//Normal = 0x00,
		//Unknown = 0x01, (also warmode?)
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
			SetPosImpl(x, y, this.Z, this.M);
		}

		public void P(Point2D point) {
			SetPosImpl(point.x, point.y, this.Z, this.M);
		}

		public void P(IPoint2D point) {
			SetPosImpl(point.X, point.Y, this.Z, this.M);
		}

		public void P(ushort x, ushort y, sbyte z) {
			SetPosImpl(x, y, z, M);
		}

		public void P(Point3D point) {
			SetPosImpl(point.x, point.y, point.z, this.M);
		}

		public void P(IPoint3D point) {
			SetPosImpl(point.X, point.Y, point.Z, this.M);
		}

		public void P(ushort x, ushort y, sbyte z, byte m) {
			SetPosImpl(x, y, z, m);
		}

		public void P(Point4D point) {
			SetPosImpl(point.x, point.y, point.z, point.m);
		}

		public void P(IPoint4D point) {
			SetPosImpl(point.X, point.Y, point.Z, point.M);
		}

		public virtual Thing Cont {
			get {
				return this;
			}
			set {
				throw new SanityCheckException("You can't give a Character a Cont");
			}
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
			sbyte tmpZ = Z;
			try {
				tmpZ = checked((sbyte) (tmpZ + amt));
				Z = tmpZ;
			} catch (OverflowException) {
				//OverheadMessage("This cannot be nudged that much (It would make its z coordinate too high).");
				Z = -128;
			}
		}

		public void NudgeDown(short amt) {
			sbyte tmpZ = Z;
			try {
				tmpZ = checked((sbyte) (tmpZ - amt));
				Z = tmpZ;
			} catch (OverflowException) {
				//OverheadMessage("This cannot be nudged that much (It would make its z coordinate too low).");
				Z = 127;
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
				if (value >= 0 && (value & ~0xc000) <= 0xbb6) {
					if (IsItem) {
						ItemSyncQueue.AboutToChange((AbstractItem) this);
					} else {
						CharSyncQueue.AboutToChangeBaseProps((AbstractCharacter) this);
					}
					color = value;
				}
			}
		}

		public ushort Model {
			get {
				return model;
			}
			set {
				if (IsItem) {
					ItemSyncQueue.AboutToChange((AbstractItem) this);
				} else {
					CharSyncQueue.AboutToChangeBaseProps((AbstractCharacter) this);
				}
				model = value;
			}
		}

		public DateTime CreatedAt {
			get {
				return createdAt;
			}
		}

		public ThingDef Def { get { return def; } }

		public virtual bool IsPlayer { get { return false; } }

		public abstract int Height { get; }

		public override bool IsDeleted { get { return (uid == -1); } }

		//------------------------
		//Static Thing methods

		//method: DeleteThing
		//Call this to delete a thing


		public static int UidClearFlags(int uid) {
			return (int) (((uint) uid) & ~0xc0000000);			//0x4*, 0x8*, * meaning zeroes padded to 8 digits total
		}

		public static int UidClearFlags(uint uid) {
			return (int) (uid & ~0xc0000000);			//0x4*, 0x8*, * meaning zeroes padded to 8 digits total
		}

		public static AbstractCharacter UidGetCharacter(int uid) {
			return things.Get(UidClearFlags(uid)) as AbstractCharacter;
		}

		public static AbstractCharacter UidGetCharacter(uint uid) {
			return things.Get(UidClearFlags(uid)) as AbstractCharacter;
		}

		public static AbstractItem UidGetContainer(int uid) {
			AbstractItem i = things.Get(UidClearFlags(uid)) as AbstractItem;
			if ((i != null) && (i.IsContainer)) {
				return i;
			}
			return null;
		}

		public static AbstractItem UidGetItem(int uid) {
			return things.Get(UidClearFlags(uid)) as AbstractItem;
		}

		public static AbstractItem UidGetItem(uint uid) {
			return things.Get(UidClearFlags(uid)) as AbstractItem;
		}

		public static Thing UidGetThing(int uid) {
			return things.Get(UidClearFlags(uid));
		}

		public static Thing UidGetThing(uint uid) {
			return things.Get(UidClearFlags(uid));
		}

		public static void RegisterTriggerGroup(TriggerGroup tg) {
			if (!registeredTGs.Contains(tg)) {
				registeredTGs.Add(tg);
			}
		}

		public override void Trigger(TriggerKey td, ScriptArgs sa) {
			ThrowIfDeleted();
			for (int i = 0, n = registeredTGs.Count; i < n; i++) {
				TriggerGroup tg = registeredTGs[i];
				tg.Run(this, td, sa);
			}
			base.Trigger(td, sa);
			def.Trigger(this, td, sa);
		}

		public override void TryTrigger(TriggerKey td, ScriptArgs sa) {
			ThrowIfDeleted();
			for (int i = 0, n = registeredTGs.Count; i < n; i++) {
				TriggerGroup tg = registeredTGs[i];
				tg.TryRun(this, td, sa);
			}
			base.TryTrigger(td, sa);
			def.TryTrigger(this, td, sa);
		}

		public override bool CancellableTrigger(TriggerKey td, ScriptArgs sa) {
			ThrowIfDeleted();
			for (int i = 0, n = registeredTGs.Count; i < n; i++) {
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
				return def.CancellableTrigger(this, td, sa);
			}
		}

		public override bool TryCancellableTrigger(TriggerKey td, ScriptArgs sa) {
			ThrowIfDeleted();
			for (int i = 0, n = registeredTGs.Count; i < n; i++) {
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
				return def.TryCancellableTrigger(this, td, sa);
			}
		}

		//------------------------
		//Public methods

		public override abstract string Name { get; set; }

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

		public abstract bool IsNotVisible { get; }

		public virtual Region Region {
			get {
				return Map.GetMap(this.point4d.m).GetRegionFor(point4d);
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

		public abstract AbstractItem FindCont(int index);

		IEnumerator IEnumerable.GetEnumerator() {
			return this.GetEnumerator();
		}

		public abstract IEnumerator<AbstractItem> GetEnumerator();

		//------------------------
		//Private & internal stuff

		[LoadSection]
		public static Thing Load(PropsSection input) {
			//an Exception propagated away from this method is usually considered critical - load failed

			//is called for each separate section

			//Console.WriteLine("["+input.headerType+" "+input.headerName+"]");
			//no exception, ignore case.

			//we need defname and p(x,y, z, m)  to construct the thing.

			ThingDef thingDef = ThingDef.Get(input.headerName) as ThingDef;

			if (thingDef == null) {
				Logger.WriteError(input.filename, input.headerLine, "Defname '" + LogStr.Ident(input.headerName) + "' not found. Thing loading interrupted.");
				return null;
			}

			int _uid;
			PropsLine prop = input.TryPopPropsLine("uid");
			if (prop == null) {
				prop = input.TryPopPropsLine("serial");
			}
			if (prop != null) {
				if (!TagMath.TryParseInt32(prop.value, out _uid)) {
					Logger.WriteError(input.filename, prop.line, "Unrecognized UID property format. Thing loading interrupted.");
					return null;
				}
			} else {
				Logger.WriteError(input.filename, input.headerLine, "UID property not found. Thing loading interrupted.");
				return null;
			}
			_uid = UidClearFlags(_uid);

			Thing.uidBeingLoaded = _uid;//the constructor should set this as Uid
			Thing constructed = thingDef.CreateWhenLoading(1, 1, 0, 0);//let's hope the P gets loaded properly later ;)

			Thing.things.AddLoaded(constructed, _uid);

			//now load the rest of the properties
			try {
				constructed.On_Load(input);
			} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }

			constructed.LoadSectionLines(input);
			if (constructed.IsChar) loadedCharacters++;
			if (constructed.IsItem) loadedItems++;

			return constructed;
		}


		[Summary("This is called after this object was is being loaded.")]
		public virtual void On_AfterLoad() {
		}

		[Summary("This is called when this object is being loaded, before the LoadLine calls. It exists because of ClassTemplate and it's autogenerating of the Save method itself. With this, it's possible to implement user save code...")]
		public virtual void On_Load(PropsSection output) {
		}

		public override void LoadLine(string filename, int line, string valueName, string valueString) {
			ThrowIfDeleted();
			switch (valueName) {
				case "p":
					object o = ObjectSaver.OptimizedLoad_SimpleType(valueString, typeof(Point4D));
					Point4D asPoint = o as Point4D;
					if (asPoint != null) {
						point4d.SetP(asPoint);
					} else {
						MutablePoint4D.Parse(this.point4d, (string) o);
					}
					//it will be put in world later by map or Cont
					break;
				case "color":
					color = TagMath.ParseUInt16(valueString);
					break;
				case "dispid":
				case "model":
				case "body":
					model = TagMath.ParseUInt16(valueString);
					break;
				case "createdat":
					this.createdAt = (DateTime) ObjectSaver.OptimizedLoad_SimpleType(valueString, typeof(DateTime));
					break;
				default:
					base.LoadLine(filename, line, valueName, valueString);
					break;
			}
		}

		[Summary("Sets all uids to lowest possible value. Always save & restart after doing this.")]
		public static void ResetAllUids() {
			things.ReIndexAll();
		}

		public static uint GetFakeUid() {
			return (uint) things.GetFakeUid();
		}

		public static uint GetFakeItemUid() {
			return (uint) things.GetFakeUid() | 0x40000000;
		}

		public static void DisposeFakeUid(int uid) {
			things.DisposeFakeUid(Thing.UidClearFlags(uid));
		}

		public static void DisposeFakeUid(uint uid) {
			things.DisposeFakeUid(Thing.UidClearFlags(uid));
		}

		internal static void SaveThis(SaveStream output, Thing t) {
			if (t.IsDeleted) {
				Logger.WriteError("Thing " + LogStr.Ident(t) + " is already deleted.");
				return;
			}
			t.SaveWithHeader(output);
		}

		[Save]
		public void SaveWithHeader(SaveStream output) {
			while (alreadySaved.Count <= this.uid) {
				alreadySaved.Add(false);
			}
			if (!alreadySaved[this.uid]) {
				alreadySaved[this.uid] = true;
				if (this.IsChar) {
					savedCharacters++;
				} else {
					savedItems++;
				}

				output.WriteSection(this.GetType().Name, this.Def.PrettyDefname);
				this.Save(output);
				output.WriteLine();
				ObjectSaver.FlushCache(output);
				if (this.CanContain) {
					foreach (AbstractItem i in this) {
						SaveThis(output, i);
					}
				}
			}
		}

		public override void Save(SaveStream output) {
			ThrowIfDeleted();
			output.WriteValue("uid", uid);

			output.WriteValue("p", this.P());

			if (color != 0) {
				output.WriteValue("color", color);
			}

			if (model != def.Model) {
				output.WriteValue("model", model);
			}

			output.WriteValue("createdat", createdAt);
			try {
				this.On_Save(output);
			} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
			base.Save(output);//tagholder save
		}

		[Summary("This is called when this object is being saved. It exists because of ClassTemplate and it's autogenerating of the Save method itself. With this, it's possible to implement custom save code...")]
		public virtual void On_Save(SaveStream output) {

		}

		public virtual Thing TopObj() {
			return this;
		}


		/*
			Method: Dupe
			
				Duplicates this character, possibly more than once.
			
			Parameters:
				ntimes - The number of times to duplicate this thing.
		 */
		public void Dupe(int ntimes) {
			ThrowIfDeleted();
			//precondition sanity checks
			Sanity.IfTrueThrow(ntimes <= 0, "Dupe called with " + ntimes + " times - Only values above 0 are valid.");

			while (ntimes > 0) {
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

		internal protected abstract void AdjustWeight(float adjust);

		//internal virtual void DecreaseWeightBy(float adjust) {
		//}
		//internal virtual void IncreaseWeightBy(float adjust) {
		//}


		public override void Delete() {
			if (this.IsDeleted) {
				return;
			}
			if (IsPlayer) {
				Globals.SrcWriteLine("Unable to remove player characters with Delete command. Use DeletePlayer().");
				return;
			}
			this.InternalDelete();
		}

		public void DeletePlayer() {
			this.InternalDelete();
		}

		internal void InternalDelete() {
			if (this.IsDeleted) {
				return;
			}
			this.RemoveFromView();
			this.InternalDeleteNoRFV();
		}

		internal void InternalDeleteNoRFV() {
			if (this.IsDeleted) {
				return;
			}
			base.Delete();
			this.Trigger_Destroy();

			things.RemoveAt(uid);
			uid = -1;

		}

		//fires the @destroy trigger on this Thing, after that removes the item from world. 
		internal virtual void Trigger_Destroy() {
			this.TryTrigger(TriggerKey.destroy, null);
			try {
				this.On_Destroy();
			} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
		}

		//method:On_Destroy
		//Thing`s implementation of trigger @Destroy,
		//does actually nothing, because finalizing of core Thing subclasses is done elsewhere
		public virtual void On_Destroy() {
			//this does nothing by default, because core classes use BeingDeleted (for safety against evil scripts)
		}

		public void Click() {
			this.Trigger_Click(Globals.SrcCharacter);
		}

		//method: Trigger_Click
		//fires the @itemClick / @charclick and @click triggers where this is the Thing being clicked on.
		public void Trigger_AosClick(AbstractCharacter clicker) {
			this.ThrowIfDeleted();
			if (clicker == null)
				return;

			GameState clickerState = clicker.GameState;
			if (clickerState != null) {
				TCPConnection<GameState> clickerConn = clickerState.Conn;
				if (!clicker.CanSeeForUpdate(this)) {
					PacketSequences.SendRemoveFromView(clickerConn, this.FlaggedUid);
				} else {
					bool cancel = false;
					ScriptArgs sa = new ScriptArgs(clicker, clickerState, clickerConn, this);
					clicker.act = this;
					cancel = this.TryCancellableTrigger(TriggerKey.aosClick, sa);
					if (!cancel) {
						//@aosClick on thing did not return 1
						try {
							this.On_AosClick(clicker, clickerState, clickerConn);
						} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
					}
				}
			}
		}

		//method: Trigger_Click
		//fires the @itemClick / @charclick and @click triggers where this is the Thing being clicked on.
		public void Trigger_Click(AbstractCharacter clicker) {
			this.ThrowIfDeleted();
			if (clicker == null)
				return;

			GameState clickerState = clicker.GameState;
			if (clickerState != null) {
				TCPConnection<GameState> clickerConn = clickerState.Conn;
				if (!clicker.CanSeeForUpdate(this)) {
					PacketSequences.SendRemoveFromView(clickerConn, this.FlaggedUid);
				} else {
					bool cancel = false;
					ScriptArgs sa = new ScriptArgs(clicker, this);
					clicker.act = this;
					cancel = this.Trigger_SpecificClick(clicker, sa);
					if (!cancel) {
						//@itemclick or @charclick on src did not return 1
						cancel = this.TryCancellableTrigger(TriggerKey.click, sa);
						if (!cancel) {
							//@click on item did not return 1
							try {
								this.On_Click(clicker, clickerState, clickerConn);
							} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
						}
					}
				}
			}
		}

		internal abstract bool Trigger_SpecificClick(AbstractCharacter clickingChar, ScriptArgs sa);

		//method: On_Click
		//Thing`s implementation of trigger @Click,
		//sends the name (DecoratedName) of this Thing as plain overheadmessage
		public virtual void On_Click(AbstractCharacter clicker, GameState clickerState, TCPConnection<GameState> clickerConn) {
			PacketSequences.SendNameFrom(clicker.GameState.Conn, this,
				this.Name, 0);
		}

		public virtual void On_AosClick(AbstractCharacter clicker, GameState clickerState, TCPConnection<GameState> clickerConn) {
			//aos client basically only clicks on incoming characters and corpses
			AOSToolTips toolTips = this.GetAOSToolTips();
			PacketSequences.SendClilocNameFrom(clicker.GameState.Conn, this,
				toolTips.FirstId, 0, toolTips.FirstArgument);
		}

		public void DClick() {
			this.Trigger_DClick(Globals.SrcCharacter);
		}

		public void Use() {
			this.Trigger_DClick(Globals.SrcCharacter);
		}

		//method: Trigger_DClick
		//fires the @itemDClick / @charDClick and @dclick triggers where this is the Thing being doubleclicked.
		public void Trigger_DClick(AbstractCharacter dclicker) {
			this.ThrowIfDeleted();
			if (dclicker == null)
				return;

			//deny triggers first
			DenyClickArgs denyArgs = new DenyClickArgs(dclicker, this);
			bool cancel = false;
			bool isItem = this.IsItem;
			if (isItem) {
				cancel = dclicker.TryCancellableTrigger(TriggerKey.denyItemDClick, denyArgs);
				if (!cancel) {
					try {
						cancel = dclicker.On_DenyItemDClick(denyArgs);
					} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
				}
			} else {
				cancel = dclicker.TryCancellableTrigger(TriggerKey.denyItemDClick, denyArgs);
				if (!cancel) {
					try {
						cancel = dclicker.On_DenyCharDClick(denyArgs);
					} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
				}
			}

			if (!cancel) {
				cancel = this.TryCancellableTrigger(TriggerKey.denyDClick, denyArgs);
				if (!cancel) {
					try {
						cancel = this.On_DenyDClick(denyArgs);
					} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
				}
			}

			DenyResult result = denyArgs.Result;

			if (result != DenyResult.Allow) {
				GameState dclickerState = dclicker.GameState;
				if (dclickerState != null) {
					PacketSequences.SendDenyResultMessage(dclickerState.Conn, this, result);
				}
			} else {//action triggers
				ScriptArgs sa = new ScriptArgs(dclicker, this);

				if (isItem) {
					dclicker.TryTrigger(TriggerKey.itemDClick, sa);
					try {
						dclicker.On_ItemDClick((AbstractItem) this);
					} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
				} else {
					dclicker.TryTrigger(TriggerKey.charDClick, sa);
					try {
						dclicker.On_CharDClick((AbstractCharacter) this);
					} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
				}

				this.TryTrigger(TriggerKey.dClick, sa);
				try {
					this.On_DClick(dclicker);
				} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
			}
		}

		//method:On_DClick
		//Thing`s implementation of trigger @DClick,
		//does nothing.
		public virtual void On_DClick(AbstractCharacter dclicker) {
		}

		public virtual bool On_DenyDClick(DenyClickArgs args) {
			return false;
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
			if (this.def != null) {
				return this.Name + " (0x" + Uid.ToString("x") + ")";
			} else {
				return "incomplete Thing (0x" + Uid.ToString("x") + ")";
			}
		}

		public override int GetHashCode() {
			return Uid;
		}

		public override bool Equals(Object obj) {
			if (obj is Thing) {
				Thing tobj = (Thing) obj;
				return (tobj.Uid == this.Uid);
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

		//public virtual bool CanPutItemsOn {
		//    get {
		//        return this.CanContain;
		//    }
		//}

		public bool IsMulti {
			get {
				return Def.multiData != null;
			}
		}

		public virtual bool CanContain {
			get {
				//i.e. if this is either char or container
				return this.IsContainer;
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
			return NewItem(factory, 1);
		}

		public abstract AbstractItem NewItem(IThingFactory factory, uint amount);

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
			DeleteObjectOutPacket p = Pool<DeleteObjectOutPacket>.Acquire();
			p.Prepare(this);
			GameServer.SendToClientsWhoCanSee(this, p);
		}

		public abstract void Resend();

		public AOSToolTips GetAOSToolTips() {
			AOSToolTips toolTips = AOSToolTips.GetFromCache(this);
			if (toolTips != null) {
				return toolTips;
			}

			toolTips = Pool<AOSToolTips>.Acquire();

			uint id;
			string argument;
			this.GetNameCliloc(out id, out argument);
			toolTips.AddLine(id, argument);

			this.BuildAOSToolTips(toolTips);
			toolTips.InitDone(this);

			return toolTips;//new or changed
		}

		public virtual void GetNameCliloc(out uint id, out string argument) {
			id = 1042971;
			argument = this.Name;
		}

		public virtual void BuildAOSToolTips(AOSToolTips opc) {
		}

		public virtual void InvalidateProperties() {
			AOSToolTips.RemoveFromCache(this);
		}

		//public Gump Dialog(string gumpName) {
		//	GumpDef GumpDef = GumpDef.Get(gumpName);
		//	if (GumpDef == null) {
		//		throw new SEException("There is no gump named "+LogStr.Ident(gumpName));
		//	} else {
		//		return Globals.src.Character.SendGump(this, GumpDef, null);
		//	}
		//}

		public Gump Dialog(GumpDef gump) {
			return Dialog(Globals.SrcCharacter, gump);
		}

		//volani primo s parametry (jak se s parametry nalozi, to zavisi na tom kterem dialogu)
		public Gump Dialog(GumpDef gump, params object[] paramArr) {
			return Dialog(Globals.SrcCharacter, gump, new DialogArgs(paramArr));
		}

		public Gump Dialog(GumpDef gump, DialogArgs args) {
			return Dialog(Globals.SrcCharacter, gump, args);
		}

		//jen presmerujeme volani jinam (bez argumentu ovsem)
		public Gump Dialog(AbstractCharacter sendTo, GumpDef gump) {
			return Dialog(sendTo, gump, new DialogArgs());
		}

		//volani primo s parametry (jak se s parametry nalozi, to zavisi na tom kterem dialogu)
		public Gump Dialog(AbstractCharacter sendTo, GumpDef gump, params object[] paramArr) {
			return Dialog(sendTo, gump, new DialogArgs(paramArr));
		}

		public Gump Dialog(AbstractCharacter sendTo, GumpDef gump, DialogArgs args) {
			if (sendTo != null) {
				return sendTo.SendGump(this, gump, args);
			}
			return null;
		}

		public void Message(string arg) {
			this.Message(arg, 0);
		}

		public void Message(string arg, ushort color) {
			this.ThrowIfDeleted();
			PacketSequences.SendOverheadMessageFrom(Globals.SrcTCPConnection, this, arg, color);
		}

		public void ClilocMessage(uint msg, string args) {
			this.ThrowIfDeleted();
			PacketSequences.SendClilocMessageFrom(Globals.SrcTCPConnection, this, msg, 0, args);
		}

		public void ClilocMessage(uint msg, params string[] args) {
			this.ThrowIfDeleted();
			PacketSequences.SendClilocMessageFrom(Globals.SrcTCPConnection, this, msg, 0, args);
		}

		public void ClilocMessage(uint msg, ushort color, string args) {
			this.ThrowIfDeleted();
			PacketSequences.SendClilocMessageFrom(Globals.SrcTCPConnection, this, msg, color, args);
		}

		public void ClilocMessage(uint msg, ushort color, params string[] args) {
			this.ThrowIfDeleted();
			PacketSequences.SendClilocMessageFrom(Globals.SrcTCPConnection, this, msg, color, args);
		}

		//public void SoundTo(ushort soundId, GameState toClient) {
		//    if (soundId != 0xffff) {
		//        if (toClient != null) {
		//            PacketSender.PrepareSound(this, soundId);
		//            PacketSender.SendTo(toClient, true);
		//        }
		//    }
		//}

		public void SoundTo(ushort soundId, AbstractCharacter toPlayer) {
			if (soundId != 0xffff) {
				if (toPlayer != null) {
					GameState state = toPlayer.GameState;
					if (state != null) {
						PlaySoundEffectOutPacket p = Pool<PlaySoundEffectOutPacket>.Acquire();
						p.Prepare(this, soundId);
						state.Conn.SendSinglePacket(p);
					}
				}
			}
		}

		public void Sound(ushort soundId) {
			this.Sound(soundId, Globals.MaxUpdateRange);
		}

		public void Sound(ushort soundId, int range) {
			if (soundId != 0xffff) {
				Thing top = this.TopObj();
				PlaySoundEffectOutPacket p = Pool<PlaySoundEffectOutPacket>.Acquire();
				p.Prepare(top, soundId);
				GameServer.SendToClientsInRange(top, range, p);
			}
		}

		/**
			Makes this thing say something to all clients who can hear it.
			
			@param arg The text to say
		 */
		public void Say(string arg) {
			if (!string.IsNullOrEmpty(arg)) {
				this.Speech(arg, 0, SpeechType.Speech, -1, 3, null, null);
			}
		}

		/**
			Makes this thing say something to all clients who can hear it.
			
			@param arg The text to say
			@param color The color the text should be
		 */
		public void Say(string arg, ushort color) {
			if (!string.IsNullOrEmpty(arg)) {
				this.Speech(arg, 0, SpeechType.Speech, color, 3, null, null);
			}
		}

		/**
			Makes this thing say something to all clients who can hear it.
			
			@param arg The text to say
			@param color The color the text should be
			@param font The font to use for the text (The default is 3)
		 */
		public void Say(string arg, ushort color, byte font) {
			if (!string.IsNullOrEmpty(arg)) {
				this.Speech(arg, 0, SpeechType.Speech, color, font, null, null);
			}
		}

		/**
			Makes this thing say something to all clients who can hear it.
			
			@param arg The cliloc entry # to say
			@param args Additional args needed for the cliloc entry, if any.
		 */
		public void ClilocSay(uint arg, params string[] args) {
			this.Speech(null, arg, SpeechType.Speech, -1, 3, null, args);
		}

		/**
			Makes this thing say something to all clients who can hear it.
			
			@param arg The cliloc entry # to say
			@param color The color the text should be
			@param args Additional args needed for the cliloc entry, if any.
		 */
		public void ClilocSay(uint arg, ushort color, params string[] args) {
			this.Speech(null, arg, SpeechType.Speech, color, 3, null, args);
		}

		/**
			Makes this thing say something to all clients who can hear it.
			
			@param arg The cliloc entry # to say
			@param color The color the text should be
			@param font The font to use for the text (The default is 3)
			@param args Additional args needed for the cliloc entry, if any.
		 */
		public void ClilocSay(uint arg, ushort color, byte font, params string[] args) {
			this.Speech(null, arg, SpeechType.Speech, color, font, null, args);
		}

		/**
			Makes this thing yell something to all clients who can hear it.
			
			@param arg The text to yell
			@param color The color the text should be
		 */
		public void Yell(string arg, ushort color) {
			if (!string.IsNullOrEmpty(arg)) {
				this.Speech(arg, 0, SpeechType.Yell, color, 3, null, null);
			}
		}

		/**
			Makes this thing yell something to all clients who can hear it.
			
			@param arg The cliloc entry # to yell
			@param color The color the text should be
			@param args Additional args needed for the cliloc entry, if any.
		 */
		public void ClilocYell(uint arg, ushort color, params string[] args) {
			this.Speech(null, arg, SpeechType.Yell, color, 3, null, args);
		}

		/**
			Makes this thing send a whisper to all clients who can hear it.
			
			@param arg The text to whisper
			@param color  The color the text should be
		 */
		public void Whisper(string arg, ushort color) {
			if (!string.IsNullOrEmpty(arg)) {
				this.Speech(arg, 0, SpeechType.Whisper, color, 3, null, null);
			}
		}

		/**
			Makes this thing send a whisper to all clients who can hear it.
			
			@param arg The cliloc entry # to whisper
			@param color The color the text should be
			@param args Additional args needed for the cliloc entry, if any.
		 */
		public void ClilocWhisper(uint arg, ushort color, params string[] args) {
			this.Speech(null, arg, SpeechType.Whisper, color, 3, null, args);
		}

		/**
			Makes this thing send an emote to all clients in range.
			
			@param arg The text to emote
			@param color  The color the text should be
		 */
		public void Emote(string arg, ushort color) {
			if (!string.IsNullOrEmpty(arg)) {
				this.Speech(arg, 0, SpeechType.Emote, color, 3, null, null);
			}
		}

		/**
			Makes this thing send an emote to all clients in range.
			
			@param arg The cliloc entry # to emote
			@param color The color the text should be
			@param args Additional args needed for the cliloc entry, if any.
		 */
		public void ClilocEmote(uint arg, ushort color, params string[] args) {
			this.Speech(null, arg, SpeechType.Emote, color, 3, null, args);
		}

		/**
			Makes this thing yell something to all clients who can hear it.
			
			@param arg The text to yell
		 */
		public void Yell(string arg) {
			this.Yell(arg, 0);
		}

		/**
			Makes this thing yell something to all clients who can hear it.
			
			@param arg The cliloc entry # to yell
			@param args Additional args needed for the cliloc entry, if any.
		 */
		public void ClilocYell(uint arg, params string[] args) {
			this.ClilocYell(arg, 0);
		}

		/**
			Makes this thing send a whisper to all clients who can hear it.
			
			@param arg The text to whisper
		 */
		public void Whisper(string arg) {
			this.Whisper(arg, 0);
		}
		/**
			Makes this thing send a whisper to all clients who can hear it.
			
			@param arg The cliloc entry # to whisper
			@param args Additional args needed for the cliloc entry, if any.
		 */
		public void ClilocWhisper(uint arg, params string[] args) {
			this.ClilocWhisper(arg, 0);
		}

		/**
			Makes this thing send an emote to all clients in range.
			
			@param arg The text to emote
		 */
		public void Emote(string arg) {
			this.Emote(arg, 0x22);
		}
		/**
			Makes this thing send an emote to all clients in range.
			
			@param arg The cliloc entry # to emote
			@param args Additional args needed for the cliloc entry, if any.
		 */
		public void ClilocEmote(uint arg, params string[] args) {
			this.ClilocEmote(arg, 0x22);
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
			if (this.TopObj() != this) {
				throw new SEException("Can't use the Move command for something not on ground.");
			}

			int x = this.point4d.x;
			int y = this.point4d.y;

			switch (dir) {
				case ("n"):
					y -= amount;
					break;
				case ("ne"):
					y -= amount;
					x += amount;
					break;
				case ("e"):
					x += amount;
					break;
				case ("se"):
					y += amount;
					x += amount;
					break;
				case ("s"):
					y += amount;
					break;
				case ("sw"):
					y += amount;
					x -= amount;
					break;
				case ("w"):
					x -= amount;
					break;
				case ("nw"):
					y -= amount;
					x -= amount;
					break;
				default:
					throw new SEException(dir + " isn't a direction I'll accept. Use n, nw, ne, w, e, s, sw, or se.");

			}

			this.P((ushort) x, (ushort) y, this.Z);	//This won't change our Z coordinate, whereas P(x,y) would.
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
		public void Speech(string speech, uint clilocSpeech, SpeechType type, int color, ushort font, int[] keywords, string[] args) {
			AbstractCharacter self = this as AbstractCharacter;

			string language = "enu";
			if (self != null) {
				GameState state = self.GameState;
				if (state != null) {
					language = state.Language;
				}
			}
			this.Speech(speech, clilocSpeech, type, color, font, language, keywords, args);
		}

		public void Speech(string speech, uint clilocSpeech, SpeechType type, int color, ushort font, string language, int[] keywords, string[] args) {
			this.ThrowIfDeleted();

			AbstractCharacter self = this as AbstractCharacter;

			bool selfIsPlayer = ((self != null) && (self.IsPlayer));
			if (selfIsPlayer) {
				bool cancel = self.Trigger_Say(speech, type, keywords);
				if (cancel) {
					return;
				}
			}

			int dist = 0;
			switch (type) {
				case SpeechType.Speech:
					dist = Globals.speechDistance;
					break;
				case SpeechType.Emote:
					dist = Globals.emoteDistance;
					break;
				case SpeechType.Whisper:
					dist = Globals.whisperDistance;
					break;
				case SpeechType.Yell:
					dist = Globals.yellDistance;
					break;
				default:
					dist = Globals.speechDistance;
					//type = SpeechType.Speech; //or should we? could this cause something bad? -tar
					break;
			}

			Map map = this.GetMap();
			ushort x = this.point4d.x;
			ushort y = this.point4d.y;

			if (selfIsPlayer) {
				foreach (AbstractCharacter chr in map.GetCharsInRange(x, y, dist)) {
					chr.Trigger_Hear(self, speech, clilocSpeech, type, color, font, language, keywords, args);
				}
			} else {//item/npc is speaking... no triggers fired, just send it to players
				PacketGroup pg = null;
				foreach (TCPConnection<GameState> conn in map.GetConnectionsInRange(x, y, dist)) {
					if (pg == null) {
						pg = PacketGroup.AcquireMultiUsePG();
						if (speech == null) {
							pg.AcquirePacket<ClilocMessageOutPacket>().Prepare(this, clilocSpeech, this.Name, type, font, color,
								args == null ? null : string.Join("\t", args));
						} else {
							pg.AddPacket(PacketSequences.PrepareMessagePacket(
								this, speech, this.Name, type, font, color, language));
						}
					}
					conn.SendPacketGroup(pg);
				}
				if (pg != null) {
					pg.Dispose();
				}
			}
		}

		///*
		//    Method: CheckForInvalidCoords
		//        This function only exists in the debug build, and is not run in non-debug builds.

		//        Checks the coords to see if they are outside the map. Since maps start at 0,0 and coordinates
		//        are unsigned, X and Y are only checked against the max X and Y for the map, and not against 0,0.

		//        This will definitely trip (Throw a SanityCheckException) if the item never got real coordinates
		//        after BeingDroppedFromContainer was called, so this is called as a sanity check by methods which
		//        call BeingDroppedFromContainer but rely on other methods to give it a real location
		//        (after the other methods have been called).

		//        This will not trip if the item is in a container (or equipped), since invalid coordinates ARE
		//        used for equipped items (Specifically, X=7000).

		//        This does not check Z.
		// */
		//[Conditional("DEBUG")]
		//internal void CheckForInvalidCoords() {
		//    bool throwExcep = false;
		//    if (!IsDeleted && IsOnGround) {
		//        if (X > Server.MaxX(M)) {
		//            throwExcep = true;
		//        } else if (Y > Server.MaxY(M)) {
		//            throwExcep = true;
		//        }
		//        if (throwExcep) {
		//            throw new SanityCheckException("Invalid coordinates detected: (" + X + "," + Y + "," + Z + "," + M + ")");
		//        }
		//    }
		//}
	}

	public class DenyClickArgs : DenyTriggerArgs {
		public readonly AbstractCharacter clickingChar;
		public readonly Thing target;

		public DenyClickArgs(AbstractCharacter clickingChar, Thing target)
			: base(DenyResult.Allow, clickingChar, target) {

			this.clickingChar = clickingChar;
			this.target = target;
		}
	}
}