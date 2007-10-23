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

namespace SteamEngine {

	public partial class Thing {
		public virtual void AddItem(AbstractCharacter addingChar, AbstractItem iToAdd) {
			throw new InvalidOperationException("The thing ("+this+") can not contain items");
		}

		public virtual void AddItem(AbstractCharacter addingChar, AbstractItem iToAdd, ushort x, ushort y) {
			throw new InvalidOperationException("The thing ("+this+") can not contain items");
		}

		internal virtual void DropItem(AbstractCharacter pickingChar, AbstractItem i) {
			throw new InvalidOperationException("The thing ("+this+") can not contain items");
		}
	}

	public partial class AbstractItem {

		public override Thing Cont {
			get {
				ThingLinkedList list = contOrTLL as ThingLinkedList;
				if ((list != null) && (list.ContAsThing != null)) {
					return list.ContAsThing;
				}
				return contOrTLL as Thing;
			}
			set {
				value.AddItem(Globals.SrcCharacter, this);
			}
		}

		internal override sealed void SetPosImpl(MutablePoint4D point) {
			if (Map.IsValidPos(point.x, point.y, point.m)) {
				NetState.ItemAboutToChange(this);

				Thing c = this.Cont;
				if (c!= null) {
					AbstractCharacter contAsChar = c as AbstractCharacter;
					if (contAsChar != null) {
						contAsChar.DropItem(contAsChar, this);
					} else {
						c.DropItem(null, this);
					}
				}
				region = null;
				Point4D oldP = this.P();
				point4d = point;
				ChangedP(oldP);
			} else {
				throw new ArgumentOutOfRangeException("Invalid position ("+point.x+","+point.y+" on mapplane "+point.m+")");
			}
		}

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


		public ushort GetRandomXInside() {
			//todo?: nonconstant bounds for this? or virtual?
			return (ushort) Globals.dice.Next(20, 128);
		}

		public ushort GetRandomYInside() {
			return (ushort) Globals.dice.Next(20, 128);
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
						tmpAmount=checked(tmpAmount+i.Amount);
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
	}

	public partial class AbstractCharacter {


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
				counter++;
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
					throw new Exception("The item "+i+"("+i.Def+") has it`s layer bad set.");
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
					throw new Exception("The item "+i+"("+i.Def+") has its layer set wrong.");
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
		internal override sealed void DropItem(AbstractCharacter pickingChar, AbstractItem i) {
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
			i.BeingEquipped(this, (byte) Layers.layer_dragging);
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
										if (!target.IsOnGround) {
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

	}
}