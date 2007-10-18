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
using SteamEngine.Packets;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Collections;
using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.Persistence;

namespace SteamEngine {
	public abstract class AbstractItem : Thing, PacketSender.ICorpseEquipInfo {
		private uint amount;
		private string name;
		//Important: cont should only be changed through calls to BeingDroppedFromContainer or BeingPutInContainer,
		//and coords of an item inside a container should only be changed through MoveInsideContainer
		
		private TriggerGroup type;

		internal object contentsOrComponents = null;

		public AbstractItemDef TypeDef {
			get {
				return (AbstractItemDef) Def;
			}
		}
		
		//Don't set the disconnected flag (0x01) by tweaking flags.
		protected byte flags;
		/* Flags:
		 * 0x0001: Stolen (Drop on death)
		 * 0x0002: Decay
		 * 0x0004: Newbied
		 * 0x0008: Always Movable
		 * 0x0010: Never Movable
		 * 0x0020: Magic
		 * 0x0040: Static (For moving stuff to the statics MULs, perhaps?)
		 * 0x0080: Invisible
		 * 0x0100: Ignored by NearbyItems
		 * 0x0200: Blocks LOS
		 * 0x0400: Provides partial cover (Doesn't block LOS but does assess combat penalties)
		 * */
		//private byte netChangeFlags = 0;
		
		private static uint instances = 0;
		private static ArrayList registeredTGs = new ArrayList();
		public TriggerGroup Type {
			get {
				return type;
			} set {
				type=value;
			}
		}
		
		//On ground
		//public AbstractItem(ThingDef myDef, ushort x, ushort y, sbyte z, byte m) : base(myDef, x, y,z,m) {
		//	instances++;
		//	this.cont=null;
		//	this.model = myDef.Model;
		//	this.flags=0;
		//	this.amount=1;
		//	this.name=-1;
		//}
		
		public AbstractItem(ThingDef myDef) : base(myDef) {
			instances++;
			this.name=null;
			this.type = ((AbstractItemDef) myDef).Type;
			this.flags=0;
			this.amount=1;
			if (!MainClass.loading) {
				if (ThingDef.lastCreatedThingContOrPoint == ContOrPoint.Cont) {
					//we are to be placed in a container
					Thing ci = ThingDef.lastCreatedIPoint as AbstractItem;
					if (ci == null) {
						if (this.IsEquippable) {
							ci = (AbstractCharacter) ThingDef.lastCreatedIPoint;
						} else {//when creating a new backpack, it must be equippable, otherwise StackOverflow here :)
							ci = ((AbstractCharacter) ThingDef.lastCreatedIPoint).Backpack;
						}
					}
					this.point4d = new MutablePoint4D(0, 0, 0, 0);
					MoveInsideContainer((ushort) Globals.dice.Next(20,100),(ushort) Globals.dice.Next(20,100));
					if (ci.IsItem) {
						((AbstractItem) ci).InternalAddItem(this);//also resends
					} else {
						((AbstractCharacter) ci).AddLoadedItem(this);
					}
				}
			}

			//MoveInsideContainer((ushort) Globals.dice.Next(20,100),(ushort) Globals.dice.Next(20,100));
			//cont.InternalAddItem(this);//also resends
			Globals.lastNewItem=this;
		}
		
		public AbstractItem(AbstractItem copyFrom) : base(copyFrom) { //copying constuctor
			instances++;
			name=copyFrom.name;
			flags=copyFrom.flags;
			amount=copyFrom.amount;

			if (this.IsContainer) {
				foreach (AbstractItem i in copyFrom) {
					i.Dupe(this);	//dupe the item & put it in our container
				}
			}
			Globals.lastNewItem=this;
		}
		
		public static uint Instances { 
			get {
				return instances;
			} 
		}
		
		public bool IsStackable { 
			get { 
				return ((AbstractItemDef) Def).IsStackable;
			} 
		}

		public int Count {
			get {
				ThingLinkedList tll = contentsOrComponents as ThingLinkedList;
				if (tll == null) {
					return 0;
				}
				return tll.count;
			}
		}
		public bool IsEmpty {
			get {
				return (Count==0);
			}
		}		
		public override int Height { 
			get {
				int defHeight = Def.Height;
				if (defHeight > 0) {
					return defHeight;
				}
				if (this.IsContainer) {
					return 4;
				}
				ItemDispidInfo idi=ItemDispidInfo.Get(this.Model);
				if (idi == null) {
					return 1;
				}
				return idi.height;
			} 
		}
		
		public override Thing Cont { 
			get {
				ThingLinkedList list = contOrTLL as ThingLinkedList;
				if ((list != null) && (list.ContAsThing != null)) {
					return list.ContAsThing;
				}
				return contOrTLL as Thing;
			} set {
				value.AddItem(Globals.SrcCharacter, this);
			} 
		}
		
		public uint Amount { 
			get {
				return amount;
			} 
			set {
				this.ChangingProperties();
				NetState.ItemAboutToChange(this);
				amount = value;
			} 
		}
		public ushort ShortAmount {
			get {
				return (amount>50000?(ushort)50000:(ushort)amount);
			}
		}
		public virtual bool TwoHanded {
			get { 
				return false;
			}
		}
		
		public sealed override uint FlaggedUid { 
			get {
				return (uint) (Uid|0x40000000);
			} 
		}
		
		public override bool Flag_Disconnected {
			get {
				return ((flags&0x01)==0x01);
			} set {
				if (Flag_Disconnected!=value) {
					NetState.ItemAboutToChange(this);
					flags=(byte) (value?(flags|0x01):(flags&~0x01));
					if (value) {
						GetMap().Disconnected(this);
					} else {
						GetMap().Reconnected(this);
					}
				}
			}
		}
		
		public bool IsInVisibleLayer { get {
			if (point4d.x == 7000) {
				return (point4d.z < 26);
			}
			return true;
		} } 
		
		public sealed override Thing Dupe() {
			Thing cont = this.Cont;
			return Dupe(cont);
		}
		
		internal AbstractItem Dupe(Thing putIn) {
			AbstractItem copy = (AbstractItem) DeepCopyFactory.GetCopy(this);
			if (putIn!=null) {
				if (putIn.IsContainer) {
					((AbstractItem) putIn).InternalAddItem(copy);
				} else {
					//it is a character
					((AbstractCharacter) putIn).AddLoadedItem(copy);
				}
			} else {//on ground, we add it to map
				copy.GetMap().Add(copy);
			}
			return copy;
		}
		
		public virtual byte Layer { 
			get {
				return (int) Layers.layer_none;
			} 
		}
		
		public virtual ushort Gump { 
			get {
				return 0;
			} 
		}
		
		//commands:
		public override AbstractItem FindCont(int index) {
			ThingLinkedList tll = contentsOrComponents as ThingLinkedList;
			if (tll == null) {
				return null;
			} else {
				return (AbstractItem) tll[index];
			}
		}
		
		public void OpenTo(AbstractCharacter viewer) {
			if (IsContainer) {
				if (viewer != null && viewer.IsPC) {
					ThrowIfDeleted();
					viewer.ThrowIfDeleted();
					GameConn viewerConn = viewer.Conn;
					if (viewerConn!=null) {
						//send container
						bool sendProperties = false;
						OpenedContainers.SetContainerOpened(viewerConn, this);
						using (BoundPacketGroup packets = PacketSender.NewBoundGroup()) {
							PacketSender.PrepareOpenContainer(this);
							if (Count>0) {
								if (PacketSender.PrepareContainerContents(this, viewerConn, viewer)) {
									sendProperties = true;
								}
							}
							packets.SendTo(viewerConn);
						}
						if (sendProperties) {
							if (Globals.AOS && viewerConn.Version.aosToolTips) {
								foreach (AbstractItem contained in this) {
									if (viewer.CanSeeVisibility(contained)) {
										ObjectPropertiesContainer containedOpc = contained.GetProperties();
										if (containedOpc != null) {
											containedOpc.SendIdPacket(viewerConn);
										}
									}
								}
							}
						}
						On_ContainerOpen(viewerConn);
					}
				}
			} else {
				throw new InvalidOperationException("The item ("+this+") is not a container");
			}
		}

		public virtual void On_ContainerOpen(GameConn viewerConn) {

		}

		public ushort GetRandomXInside() {
			//todo?: nonconstant bounds for this? or virtual?
			return (ushort) Globals.dice.Next(20,128);
		}
		
		public ushort GetRandomYInside() {
			return (ushort) Globals.dice.Next(20,128);
		}
		
		public virtual bool On_PickupFrom(AbstractCharacter pickingChar, AbstractItem i, ref object amount) {
			//pickingChar is picking up amount of AbstractItem i from this
			return false;
		}

		public override void AddItem(AbstractCharacter addingChar, AbstractItem i) {
			if (IsContainer) {
				if (!TryToStack(addingChar, i)) {
					AddItem(addingChar, i, GetRandomXInside(), GetRandomYInside());
				}
			} else {
				throw new InvalidOperationException("The item ("+this+") is not a container");
			}
		}

		public override void AddItem(AbstractCharacter addingChar, AbstractItem i, ushort x, ushort y) {
			if (IsContainer) {
				ThrowIfDeleted();
				i.ThrowIfDeleted();
				if (this.IsWithinCont(i)) {
					throw new InvalidOperationException("You can not add item ("+i+") to it's own subcontainer ("+this+").");
				}
				NetState.ItemAboutToChange(i);
				
				if ((x==0xffff) || (y==0xffff)) {
					//add with stacking
					if (TryToStack(addingChar, i)) {
						return;
					} else {
						x = GetRandomXInside();
						y = GetRandomYInside();
					}
				} else if ((x==0) || (y==0)) {
					x = GetRandomXInside();
					y = GetRandomYInside();
				}
				if (i.Cont != this) {
					i.FreeCont(addingChar);
					InternalAddItem(i);
				}
				i.MoveInsideContainer(x, y);
				i.PlayDropSound(addingChar);
			} else {
				throw new InvalidOperationException("The item ("+this+") is not a container");
			}
		}
		
		internal void InternalAddItem(AbstractItem i) {
			this.ChangingProperties();
			ThingLinkedList tll = contentsOrComponents as ThingLinkedList;
			if (tll == null) {
				tll = new ThingLinkedList(this);
				contentsOrComponents = tll;
			}
			//IncreaseWeightBy(i.Weight);
			i.BeingPutInContainer(tll);
			tll.Add(i);
			//i.cont = contents;
		}
		
		//try to add item to some stack inside
		private bool TryToStack(AbstractCharacter stackingChar, AbstractItem toStack) {
			ThingLinkedList tll = contentsOrComponents as ThingLinkedList;
			if (tll != null) {
				return tll.TryToStack(stackingChar, toStack);
			}
			return false;
		}

		//add i to this stack, if possible
		public bool StackAdd(AbstractCharacter stackingChar, AbstractItem i) {
			if (this.IsStackable && i.IsStackable) {
				if (this.Def.Equals(i.Def)) {
					ThrowIfDeleted();
					i.ThrowIfDeleted();
					
					//Amount overflow checking:
					if (Amount==0) Amount=1;
					if (i.Amount==0) i.Amount=1;
					uint tmpAmount=Amount;
					try {
						tmpAmount=checked (tmpAmount+i.Amount);
					} catch (OverflowException) {
						return false;
					}
					Amount=tmpAmount;

					PlayDropSound(stackingChar);
					Thing.DeleteThing(i);
					return true;
				}
			}
			return false;
		}
		
		/**
			Drop this item on the ground.
		*/
		public void Drop() {
			ThrowIfDeleted();
			if (Cont!=null) {
				IPoint4D tp = TopPoint;
				Cont.DropItem(Globals.SrcCharacter, this);
				P(tp);
			}
			//Perhaps call Fix too? Adjust Z so we aren't in the exact same place as the cont?
			//Fix should probably do that, I suppose.
		}
		
		//is not public because it leaves the item in "illegal" state...
		internal override void DropItem(AbstractCharacter pickingChar, AbstractItem i) {
			ThingLinkedList tll = contentsOrComponents as ThingLinkedList;
			if (tll != null) {
				if (i.contOrTLL == tll) {
					tll.Remove(i);
					this.ChangingProperties();//changing Count
					//DecreaseWeightBy(i.Weight);
					i.BeingDroppedFromContainer();
				}
			}
		}

		/**
			Changes x/y without affecting sector coordinates.
			If you are moving an item around inside a container, this is what you should call.
		 */
		public void MoveInsideContainer(ushort x, ushort y) {
			NetState.ItemAboutToChange(this);
			region = null;
			point4d.x=x;
			point4d.y=y;
		}
		
		//called from PrivatePickup
		internal void FreeCont(AbstractCharacter droppingChar) {
			if (this.point4d.x == 0xffff && this.point4d.y == 0xffff) {
				return;
			}
			Thing c = this.Cont;
			if (c != null) {
				c.DropItem(droppingChar, this);
			} else {
				BeingPickedUpFromGround();
			}
		}

		internal void BeingDroppedFromContainer() {
			//Console.WriteLine("BeingDroppedFromContainer()");
			OpenedContainers.SetContainerClosed(this);//if we are a container, moving us makes us closed
			RemoveFromView();
			Cont.AdjustWeight(-this.Weight);
			//NetState.AboutToChange(this);
			contOrTLL=null;
			region = null;
			//Invalid coordinates, to mark this as being in transition between a container and somewhere else.
			point4d.x=0xffff;
			point4d.y=0xffff;
		}

		internal void BeingPickedUpFromGround() {
			region = null;
			byte m = this.M;
			if (Map.IsValidPos(point4d.x, point4d.y, m)) {
				Map map = Map.GetMap(m);
				OpenedContainers.SetContainerClosed(this);
				RemoveFromView();
				map.Remove(this);
			}

			//Invalid coordinates, to mark this as being in transition between a container and somewhere else.
			point4d.x=0xffff;
			point4d.y=0xffff;
		}
		
		internal void BeingEquipped(object container, byte layer) {
			//Console.WriteLine("BeingPutInContainer(object, byte)");
			NetState.ItemAboutToChange(this);
			contOrTLL=container;
			this.point4d.x = 7000;
			this.point4d.y = 0;
			this.point4d.z = (sbyte) layer;
			Cont.AdjustWeight(this.Weight);
		}

		internal void BeingPutInContainer(object container) {
			//Console.WriteLine("BeingPutInContainer(object)");
			NetState.ItemAboutToChange(this);
			contOrTLL=container;
			Cont.AdjustWeight(this.Weight);
		}

		internal bool TryRemoveFromContainer() {
			ThingLinkedList list = this.contOrTLL as ThingLinkedList;
			if ((list != null) && (list.ContAsThing != null)) {
				list.Remove(this);
				return true;
			}
			Logger.WriteWarning("cont is "+contOrTLL+", a "+contOrTLL.GetType()+".");
			return false;
		}

		internal protected override sealed void BeingDeleted() {
			instances--;
			ThingLinkedList tll = contentsOrComponents as ThingLinkedList;
			if (tll != null) {
				tll.BeingDeleted();
			}
			Thing c = Cont;
			if (c!=null) {
				c.DropItem(null, this);	//Does not place it on the ground, which is good when it's being deleted.
			} else if (Map.IsValidPos(this)) {
				GetMap().Remove(this);
			}
			base.BeingDeleted();	//This MUST be called.
		}
		
		public void EmptyCont() {
			ThingLinkedList tll = contentsOrComponents as ThingLinkedList;
			if (tll != null) {
				tll.Empty();
			}
		}
		
		public override string Name { 
			get {
				if (name==null) {
					return Amount>1?TypeDef.PluralName:TypeDef.SingularName;
				} else {
					return name;
				}
			} 
			set {
				this.ChangingProperties();
				if (!string.IsNullOrEmpty(value)) {
					name=String.Intern(value);
				} else {
					name=null;
				}
			} 
		}
	
		public override void On_Create() {
			AbstractItemDef aidef = (AbstractItemDef) def;
			AbstractItemDef dupe = aidef.DupeItem;
			if (dupe!=null) {
				def = dupe;
			}
			Type = aidef.Type;
			base.On_Create();
		}
				
		public void Flip() {
			Model = TypeDef.GetNextFlipModel(Model);
		}
		
		public static new void RegisterTriggerGroup(TriggerGroup tg) {
			if (!registeredTGs.Contains(tg)) {
				registeredTGs.Add(tg);
			}
		}
		
		public override void Trigger(TriggerKey td, ScriptArgs sa) {
			ThrowIfDeleted();
			for (int i = 0, n = registeredTGs.Count; i<n; i++) {
				TriggerGroup tg = (TriggerGroup) registeredTGs[i];
				tg.Run(this, td, sa);
			}
			base.TryTrigger(td, sa);
			if (type!=null) {
				type.Run(this, td, sa);
			}
		}
		
		public override void TryTrigger(TriggerKey td, ScriptArgs sa) {
			ThrowIfDeleted();
			for (int i = 0, n = registeredTGs.Count; i<n; i++) {
				TriggerGroup tg = (TriggerGroup) registeredTGs[i];
				tg.TryRun(this, td, sa);
			}
			base.TryTrigger(td, sa);
			if (type!=null) {
				type.TryRun(this, td, sa);
			}
		}

		internal override sealed void SetPosImpl(MutablePoint4D point) {
			if (IsInContainer) {
				Logger.WriteWarning(LogStr.Ident(this)+" is inside a container, but its P is being set. If it is being taken out of the container, it should be dropped first, and if it is just being moved inside the container, then MoveInsideContainer should be used instead of setting its coordinates directly. (We are now going to call MoveInsideContainer for you)");
				MoveInsideContainer(point.x, point.y);
				return;
			}
			if (Map.IsValidPos(point.x,point.y,point.m)) {
				NetState.ItemAboutToChange(this);
				region = null;
				Point4D oldP = this.P();
				point4d = point;
				ChangedP(oldP);
			} else {
				throw new ArgumentOutOfRangeException("Invalid position ("+point.x+","+point.y+" on mapplane "+point.m+")");
			}
		}
		
		public override bool CancellableTrigger(TriggerKey td, ScriptArgs sa) {
			ThrowIfDeleted();
			for (int i = 0, n = registeredTGs.Count; i<n; i++) {
				TriggerGroup tg = (TriggerGroup) registeredTGs[i];
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
				if (type!=null) {
					object retVal = type.Run(this, td, sa);
					try {
						int retInt = Convert.ToInt32(retVal);
						if (retInt == 1) {
							return true;
						}
					} catch (Exception) {
					}
				}
			}
			return false;
		}
		
		public override bool TryCancellableTrigger(TriggerKey td, ScriptArgs sa) {
			ThrowIfDeleted();
			for (int i = 0, n = registeredTGs.Count; i<n; i++) {
				TriggerGroup tg = (TriggerGroup) registeredTGs[i];
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
				if (type!=null) {
					object retVal = type.TryRun(this, td, sa);
					try {
						int retInt = Convert.ToInt32(retVal);
						if (retInt == 1) {
							return true;
						}
					} catch (Exception) {
					}
				}
			}
			return false;
		}
		
		internal override sealed bool TriggerSpecific_Click(AbstractCharacter clickingChar, ScriptArgs sa) {
			//helper method for Trigger_Click
			bool cancel=false;
			cancel=clickingChar.TryCancellableTrigger(TriggerKey.itemClick, sa);
			if (!cancel) {
				clickingChar.act=this;
				cancel=clickingChar.On_ItemClick(this);
			}
			return cancel;
		}

		public virtual bool On_DropOn_Ground(AbstractCharacter droppingChar, IPoint4D point) {
			//src is dropping on ground at x y z point4d
			return false;
		}

		public virtual bool On_Pickup_Ground(AbstractCharacter pickingChar, ref object amount) {
			//pickingChar is picking me up from ground
			return false;
		}

		public virtual bool On_Pickup_Pack(AbstractCharacter pickingChar, ref object amount) {
			//pickingChar is picking me up from ground
			return false;
		}

		public virtual bool On_StackOn_Item(AbstractCharacter droppingChar, AbstractItem target, ref object objX, ref object objY) {
			//droppingChar is dropping this on (not into!) item target
			return false;
		}

		public virtual bool On_StackOn_Char(AbstractCharacter droppingChar, AbstractCharacter target) {
			return false;
		}

		public virtual bool On_Equip(AbstractCharacter droppingChar, AbstractCharacter target, bool forced) {
			return false;
		}

		public virtual bool On_UnEquip(AbstractCharacter pickingChar, bool forced) {
			return false;
		}
		
		internal override sealed bool TriggerSpecific_DClick(AbstractCharacter dClickingChar, ScriptArgs sa) {
			//helper method for Trigger_Click
			bool cancel=false;
			cancel=dClickingChar.TryCancellableTrigger(TriggerKey.itemDClick, sa);
			if (!cancel) {
				dClickingChar.act=this;
				cancel=dClickingChar.On_ItemDClick(this);
			}
			return cancel;
		}
		
		public void Trigger_Step(AbstractCharacter steppingChar, int repeated) {
			ThrowIfDeleted();
			bool cancel = false;
			bool rep = (repeated!=0);
			ScriptArgs sa = new ScriptArgs(steppingChar, this, repeated);
			steppingChar.act=this;
			cancel=steppingChar.TryCancellableTrigger(TriggerKey.itemStep, sa);
			if (!cancel) {
				//@item/charStep on src did not return 1
				cancel=steppingChar.On_ItemStep(this, rep);//sends true if repeated=1
				if (!cancel) {
					cancel=TryCancellableTrigger(TriggerKey.step, sa);
					if (!cancel) {
						On_Step(steppingChar, rep);
					}
				}
			}
		}
		
		public virtual void On_Step(AbstractCharacter stepping, bool repeated) {
			//Globals.src is stepping/standing on this
		}
		
		public override void Save(SaveStream output) {
			base.Save(output);
			AbstractItemDef def = this.TypeDef;
			if ((name != null) && (!def.Name.Equals(name))) {
				output.WriteValue("name", name);
			}
			Thing c = this.Cont;
			if (c != null) {
				output.WriteValue("cont", c);
			}
			if (amount != 1) {
				output.WriteValue("amount", amount);
			}
			if (flags != 0) {
				output.WriteValue("flags", flags);
			}
			if ((type != null) && (type!= def.Type)) {
				output.WriteLine("type="+type.Defname);
			}
		}

		public override void LoadLine(string filename, int line, string prop, string value) {
			switch(prop) {
				case "cont":
					ObjectSaver.Load(value, new LoadObject(LoadCont_Delayed), filename, line);
					break;
				case "name":
					Match ma = TagMath.stringRE.Match(value);
					if (ma.Success) {
						name = String.Intern(ma.Groups["value"].Value);
					} else {
						name = String.Intern(value);
					}
					break;
				case "amount":
					amount = TagMath.ParseUInt16(value);
					break;
				case "flags":
					flags = TagMath.ParseByte(value);
					break;
				case "type":
					type = TriggerGroup.Get(value);
					break;
				default:
					base.LoadLine(filename, line, prop, value);
					break;
			}
		}
		
		public void LoadCont_Delayed(object resolvedObj, string filename, int line) {
			if (Uid == -1) {
				//I was probably cleared because of failed load. Let me get deleted by garbage collector.
				return;
			}
			if (Cont == null) {
				Thing t = resolvedObj as Thing;
				if (t != null) {
					if ((t.IsChar)&&(this.IsEquippable)) {
						((AbstractCharacter) t).AddLoadedItem(this);
						return;
					} else if (t.IsContainer) {
						((AbstractItem) t).InternalAddItem(this);
						return;
					}
				}
				//contOrTLL=null;
				Logger.WriteWarning("The saved cont object ("+resolvedObj+") for item '"+this.ToString()+"' is not a valid container. Removing.");
				Thing.DeleteThing(this);
				return;
			}
			Logger.WriteWarning("The object ("+resolvedObj+") is being loaded as cont for item '"+this.ToString()+"', but it already does have it's cont. This should not happen.");
		}
		
		public void PlayDropSound(AbstractCharacter droppingChar) {
			ScriptArgs sa = new ScriptArgs(droppingChar);
			if (!TryCancellableTrigger(TriggerKey.playDropSound, sa)) {
				On_DropSound(droppingChar);
			}
		}

		public virtual void On_DropSound(AbstractCharacter droppingChar) {
			this.SoundTo(this.TypeDef.DropSound, droppingChar);
		}
		
		public override sealed bool IsItem { 
			get {
				return true;
			} 
		}
		
		public override sealed bool IsEquipped { 
			get {
				Thing t = Cont;
				return (t!=null && t.IsChar);
			} 
		}
		
		public override sealed bool IsOnGround { 
			get {
				return (Cont==null);
			} 
		}
		
		public override sealed bool IsInContainer { 
			get {
				Thing c = Cont;
				return (c != null && c.IsItem);
			} 
		}
		
		public override sealed Thing TopObj() {
			Thing c = Cont;
			if (c != null) {
				return c.TopObj();
			} else {
				return this;
			}
		}

		public virtual Direction Direction {
			get {
				return (Direction) 0;
			}
			set {
				throw new NotSupportedException("You can't set Direction to "+this.GetType());
			}
		}
		
		//returns true if this is in given container or its subcontainers
		public bool IsWithinCont(Thing container) {
			Thing myCont = Cont;
			if (myCont == container) {
				return true;
			} else if (myCont != null) {
				if (myCont.IsItem) {
					return ((AbstractItem) myCont).IsWithinCont(container);
				}
			}
			return false;
		}
				
		public override sealed IEnumerator GetEnumerator() {
			ThingLinkedList tll = contentsOrComponents as ThingLinkedList;
			if (tll == null) {
				return EmptyEnumerator<AbstractItem>.instance;
			} else {
				return tll.GetEnumerator();
			}
		}
	}

	public class EmptyEnumerator<T> : IEnumerator<T>, IEnumerable<T>, IEnumerator, IEnumerable {
		public static readonly EmptyEnumerator<T> instance = new EmptyEnumerator<T>();

		private EmptyEnumerator() {
		}

		public void Reset() {
		}

		public bool MoveNext() {
			return false;
		}

		public object Current { 
			get {
				throw new Exception("this should not happen");
			} 
		}
		
		public IEnumerator GetEnumerator() {
			return this;
		}

		T IEnumerator<T>.Current {
			get { throw new Exception("this should not happen"); }
		}

		public void Dispose() {
		}

		IEnumerator<T> IEnumerable<T>.GetEnumerator() {
			return this;
		}
	}
}