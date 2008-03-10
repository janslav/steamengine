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

namespace SteamEngine {
	internal interface ObjectWithUid {
		int Uid { get; set; }
	}

	public abstract partial class Thing : PluginHolder, IPoint4D, IEnumerable, ObjectWithUid {
		public static bool ThingTracingOn = TagMath.ParseBoolean(ConfigurationManager.AppSettings["Thing Trace Messages"]);
		public static bool WeightTracingOn = TagMath.ParseBoolean(ConfigurationManager.AppSettings["Weight Trace Messages"]);

		private DateTime createdAt;//Server time of creation
		private ushort color;
		private ushort model;
		internal ThingDef def; //tis is changed even from outside the constructor in case of dupeitems...
		internal readonly MutablePoint4D point4d = new MutablePoint4D(0xffff, 0xffff, 0, 0); //made this internal because SetPosImpl is now abstract... -tar
		private int uid=-2;
		//internal Region region;
		internal NetState netState;//No one is to touch this but the NetState class itself!

		internal object contOrTLL; //internal cos of ThingLinkedList

		private static int savedCharacters=0;
		private static int savedItems=0;
		private static List<bool> alreadySaved = new List<bool>();

		private static int loadedCharacters;
		private static int loadedItems;

		internal Thing nextInList = null;
		internal Thing prevInList = null;

		private static List<TriggerGroup> registeredTGs = new List<TriggerGroup>();

		private static UIDArray<Thing> things = new UIDArray<Thing>();
		private static int uidBeingLoaded=-1;
		public static TagKey weightTag = TagKey.Get("_weight_");


		public sealed class ThingSaveCoordinator : IBaseClassSaveCoordinator {
			public static readonly Regex thingUidRE = new Regex(@"^\s*#(?<value>(0x)?[\da-f]+)\s*$",
				RegexOptions.IgnoreCase|RegexOptions.CultureInvariant|RegexOptions.Compiled);

			public string FileNameToSave {
				get { return "things"; }
			}

			public void StartingLoading() {
				loadedCharacters=0;
				loadedItems=0;
			}

			public void SaveAll(SaveStream output) {
				Logger.WriteDebug("Saving Things.");
				output.WriteComment("Things");
				output.WriteLine();
				savedCharacters=0;
				savedItems=0;
				alreadySaved = new List<bool>(things.HighestElement);
				foreach (Thing t in things) {
					SaveThis(output, t.TopObj());//each thing should recursively save it's contained items
				}
				Logger.WriteDebug(string.Format(
												"Saved {0} things: {1} items and {2} characters.",
												savedCharacters+savedItems, savedItems, savedCharacters));
												
				alreadySaved = null;
			}

			public void LoadingFinished() {
				//this means real end of file(s)
				uidBeingLoaded = -1;
				things.LoadingFinished();
				Logger.WriteDebug(string.Format(
												"Loaded {0} things: {1} items and {2} characters.",
												loadedItems+loadedCharacters, loadedItems, loadedCharacters));
				foreach (Thing t in things) {
					t.On_AfterLoad();
				}
			}

			public Type BaseType {
				get { return typeof(Thing); }
			}

			public string GetReferenceLine(object value) {
				return "#"+((Thing) value).uid;
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
					throw new NonExistingObjectException("There is no thing with uid "+LogStr.Number(uid)+" to load.");
				}
			}
		}

		//still needs to be put into the world
		protected Thing(ThingDef myDef) {
			this.def=myDef;
			this.model = myDef.Model;
			this.color = myDef.Color;
			if (uidBeingLoaded==-1) {
				things.Add(this);//sets uid
				NetState.Resend(this);
			} else {
				uid=uidBeingLoaded;
				this.point4d=new MutablePoint4D((ushort) Globals.dice.Next(0, 256), (ushort) Globals.dice.Next(0, 256), 0, 0);
			}
			Globals.lastNew=this;
		}

		protected Thing(Thing copyFrom)
				: base(copyFrom) { //copying constuctor
			NetState.Resend(this);
			things.Add(this);//sets uid
			point4d=new MutablePoint4D(copyFrom.point4d);
			def=copyFrom.def;
			color=copyFrom.color;
			model=copyFrom.model;
			//SetSectorPoint4D();
			Globals.lastNew=this;
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

		public ushort Model {
			get {
				return model;
			}
			set {
				if (IsItem) {
					NetState.ItemAboutToChange((AbstractItem) this);
				} else {
					NetState.AboutToChangeBaseProps((AbstractCharacter) this);
				}
				model=value;
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

		public override bool IsDeleted { get { return (uid==-1); } }

		//------------------------
		//Static Thing methods

		//method: DeleteThing
		//Call this to delete a thing


		public static int UidClearFlags(int uid) {
			return (int) (((uint) uid)&~0xc0000000);			//0x4*, 0x8*, * meaning zeroes padded to 8 digits total
		}

		public static int UidClearFlags(uint uid) {
			return (int) (uid&~0xc0000000);			//0x4*, 0x8*, * meaning zeroes padded to 8 digits total
		}

		public static AbstractCharacter UidGetCharacter(int uid) {
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

		public static Thing UidGetThing(int uid) {
			return things.Get(UidClearFlags(uid));
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
			def.Trigger(this, td, sa);
		}

		public override void TryTrigger(TriggerKey td, ScriptArgs sa) {
			ThrowIfDeleted();
			for (int i = 0, n = registeredTGs.Count; i<n; i++) {
				TriggerGroup tg = registeredTGs[i];
				tg.TryRun(this, td, sa);
			}
			base.TryTrigger(td, sa);
			def.TryTrigger(this, td, sa);
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
				return def.CancellableTrigger(this, td, sa);
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

		public abstract IEnumerator GetEnumerator();

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
				Logger.WriteError(input.filename, input.headerLine, "Defname '"+LogStr.Ident(input.headerName)+"' not found. Thing loading interrupted.");
				return null;
			}

			int _uid;
			PropsLine prop = input.TryPopPropsLine("uid");
			if (prop==null) {
				prop=input.TryPopPropsLine("serial");
			}
			if (prop!=null) {
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
			constructed.On_Load(input);
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
					string v = o as string;
					if (v != null) {
						MutablePoint4D.Parse(this.point4d, valueString);
					} else {
						point4d.SetP((Point4D) o);
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

		[Remark("Sets all uids to lowest possible value. Always save & restart after doing this.")]
		public static void ResetAllUids() {
			things.ReIndexAll();
		}

		public static uint GetFakeUid() {
			return (uint) things.GetFakeUid();
		}

		public static uint GetFakeItemUid() {
			return (uint) things.GetFakeUid()|0x40000000;
		}

		public static void DisposeFakeUid(int uid) {
			things.DisposeFakeUid(Thing.UidClearFlags(uid));
		}

		public static void DisposeFakeUid(uint uid) {
			things.DisposeFakeUid(Thing.UidClearFlags(uid));
		}

		internal static void SaveThis(SaveStream output, Thing t) {
			if (t.IsDeleted) {
				Logger.WriteError("Thing "+LogStr.Ident(t)+" is already deleted.");
				return;
			}
			t.SaveWithHeader(output);
		}

		[Save]
		public void SaveWithHeader(SaveStream output) {
			while (alreadySaved.Count<=this.uid) {
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

			if (color!=0) {
				output.WriteValue("color", color);
			}

			if (model != def.Model) {
				output.WriteValue("model", model);
			}

			output.WriteValue("createdat", createdAt);
			On_Save(output);
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
			uid=-1;

		}

		//fires the @destroy trigger on this Thing, after that removes the item from world. 
		internal virtual void Trigger_Destroy() {
			TryTrigger(TriggerKey.destroy, null);
			On_Destroy();
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

		internal abstract bool TriggerSpecific_Click(AbstractCharacter clickingChar, ScriptArgs sa);

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
			//aos client basically only clicks on incoming characters and corpses
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
					cancel=TryCancellableTrigger(TriggerKey.dClick, sa);
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

		//volani primo s parametry (jak se s parametry nalozi, to zavisi na tom kterem dialogu)
		public GumpInstance Dialog(Gump gump, params object[] paramArr) {
			return Dialog(Globals.SrcCharacter, gump, new DialogArgs(paramArr));
		}

		public GumpInstance Dialog(Gump gump, DialogArgs args) {
			return Dialog(Globals.SrcCharacter, gump, args);
		}

		//jen presmerujeme volani jinam (bez argumentu ovsem)
		public GumpInstance Dialog(AbstractCharacter sendTo, Gump gump) {
			return Dialog(sendTo, gump, new DialogArgs());
		}

		//volani primo s parametry (jak se s parametry nalozi, to zavisi na tom kterem dialogu)
		public GumpInstance Dialog(AbstractCharacter sendTo, Gump gump, params object[] paramArr) {
			return Dialog(sendTo, gump, new DialogArgs(paramArr));
		}

		public GumpInstance Dialog(AbstractCharacter sendTo, Gump gump, DialogArgs args) {
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


		public void SoundTo(ushort soundId, GameConn toClient) {
			if (soundId != 0xffff) {
				if (toClient != null) {
					PacketSender.PrepareSound(this, soundId);
					PacketSender.SendTo(toClient, true);
				}
			}
		}

		public void SoundTo(ushort soundId, AbstractCharacter toPlayer) {
			if (soundId != 0xffff) {
				if (toPlayer != null) {
					GameConn conn = toPlayer.Conn;
					if (conn != null) {
						PacketSender.PrepareSound(this, soundId);
						PacketSender.SendTo(conn, true);
					}
				}
			}
		}

		public void Sound(ushort soundId) {
			if (soundId != 0xffff) {
				PacketSender.PrepareSound(this, soundId);
				PacketSender.SendToClientsInRange(this.TopPoint, Globals.MaxUpdateRange);
			}
		}

		public void Sound(ushort soundId, ushort range) {
			if (soundId != 0xffff) {
				PacketSender.PrepareSound(this, soundId);
				PacketSender.SendToClientsInRange(this.TopPoint, range);
			}
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
			AbstractCharacter self = this as AbstractCharacter;
			if (self!=null) {
				self.Trigger_NewPosition();
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

		public virtual void On_BeingSentTo(GameConn viewerConn) {
			//Console.WriteLine(this+" being sent to "+viewerConn.CurCharacter);
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