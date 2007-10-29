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
		public virtual void On_Dupe(DupeArgs args) {

		}

		public Thing Dupe() {
			if (IsPlayer) {
				throw new NotSupportedException("You can not dupe a PC!");
			}
			Thing copy = (Thing) DeepCopyFactory.GetCopy(this);
			DupeArgs args = new DupeArgs(this, copy);

			AbstractItem copyAsItem = copy as AbstractItem;
			if (copyAsItem != null) {
				Thing cont = copyAsItem.Cont;
				if (cont == null) {
					MarkAsLimbo(copyAsItem);
					copyAsItem.Trigger_EnterRegion(this.point4d.x, this.point4d.y, this.point4d.z, this.point4d.m);
				} else {
					AbstractItem contItem = cont as AbstractItem;
					if (contItem == null) {
						MarkAsLimbo(copyAsItem);
						copyAsItem.Trigger_EnterChar((AbstractCharacter) cont, (byte) this.point4d.z);
					} else {
						MarkAsLimbo(copyAsItem);
						copyAsItem.Trigger_EnterItem(contItem, this.point4d.x, this.point4d.y);
					}
				}
			}

			//copy.def.Trigger_Create(copy); nevolame pri dupovani...?
			this.On_Dupe(args);
			copy.On_Dupe(args);


			foreach (AbstractItem i in this) {
				i.Dupe(copy);
			}

			return copy;
		}

		//duplicating some code here, but hey... who will know ;)
		private AbstractItem Dupe(Thing cont) {
			AbstractItem copy = (AbstractItem) DeepCopyFactory.GetCopy(this);
			DupeArgs args = new DupeArgs(this, copy);

			AbstractItem contItem = cont as AbstractItem;
			if (contItem == null) {
				MarkAsLimbo(copy);
				copy.Trigger_EnterChar((AbstractCharacter) cont, (byte) this.point4d.z);
			} else {
				MarkAsLimbo(copy);
				copy.Trigger_EnterItem(contItem, this.point4d.x, this.point4d.y);
			}

			//copy.def.Trigger_Create(copy); nevolame pri dupovani...?
			this.On_Dupe(args);
			copy.On_Dupe(args);

			foreach (AbstractItem i in this) {
				i.Dupe(copy);
			}

			return copy;
		}

		internal override bool IsLimbo {
			get {
				return this.point4d.x == 0xffff;
			}
		}

		internal virtual void MakeLimbo() {
			MarkAsLimbo(this);
		}

		static internal void MarkAsLimbo(Thing t) {
			t.point4d.x = 0xffff;
			t.point4d.y = 0xffff;
		}

		internal abstract void ItemMakeLimbo(AbstractItem i);


		//This verifies that the coordinate specified is inside the appropriate map (by asking Map if it's a valid pos),
		//and then sets
		//CheckVisibility itself only if the character turns instead of moving (Since P isn't updated in that case).
		//this is completely overriden in AbstractCharacter
		internal abstract void SetPosImpl(ushort x, ushort y, sbyte z, byte m);
	}

	public partial class AbstractItem {

		public override Thing Cont {
			get {
				ThrowIfDeleted();
				ThingLinkedList list = contOrTLL as ThingLinkedList;
				if ((list != null) && (list.ContAsThing != null)) {
					return list.ContAsThing;
				}
				return contOrTLL as Thing;
			}
			set {
				ThrowIfDeleted();
				if (value == null) {
					throw new ArgumentNullException("value");
				}
				
				AbstractItem contItem = value as AbstractItem;
				if (contItem == null) {
					if (this.IsEquippable) {
						byte layer = this.Layer;
						if (layer > 0) {
							this.MakeLimbo();
							Trigger_EnterChar((AbstractCharacter) value, layer);
						} else {
							throw new SEException("Item '"+ this + "' is equippable, but has Layer not set.");
						}
					} else {
						throw new SEException("Item '"+ this + "' is not equippable.");
					}
				} else if (contItem.IsContainer) {
					this.MakeLimbo();
					ushort x, y;
					contItem.GetRandomXYInside(out x, out y);
					Trigger_EnterItem(contItem, x, y);

					contItem.TryStackToAnyInside(this);
				} else {
					throw new SEException("Item '"+ value + "' is not a container, it can't contain items.");
				}
			}
		}

		internal override void MakeLimbo() {
			if (!IsLimbo) {
				OpenedContainers.SetContainerClosed(this);
				this.RemoveFromView();
				NetState.ItemAboutToChange(this);

				Thing cont = this.Cont;
				if (cont == null) {
					Trigger_LeaveGround();
				} else {
					AbstractItem contItem = cont as AbstractItem;
					if (contItem == null) {
						Trigger_LeaveChar((AbstractCharacter) cont);
					} else {
						Trigger_LeaveItem(contItem);
					}
				}
				base.MakeLimbo();
			}
		}

		private void Trigger_LeaveItem(AbstractItem cont) {
			Sanity.IfTrueThrow(cont != this.Cont, "this not in cont.");

			ItemInItemArgs args = new ItemInItemArgs(this, cont);
			ushort x = this.point4d.x;
			ushort y = this.point4d.y;
			this.TryTrigger(TriggerKey.leaveItem, args);
			ReturnIntoItemIfNeeded(cont, x, y);
			try {
				this.On_LeaveItem(args);
			} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
			ReturnIntoItemIfNeeded(cont, x, y);
			cont.TryTrigger(TriggerKey.itemLeave, args);
			ReturnIntoItemIfNeeded(cont, x, y);
			try {
				cont.On_ItemLeave(args);
			} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
			ReturnIntoItemIfNeeded(cont, x, y);

			cont.ItemMakeLimbo(this);
		}

		private void ReturnIntoItemIfNeeded(AbstractItem originalCont, ushort x, ushort y) {
			if (this.Cont != originalCont) {
				Logger.WriteWarning(this+" has been moved in the implementation of one of the @LeaveItem/@EnterItem triggers. Don't do this. Putting back.");
				this.MakeLimbo();
				this.Trigger_EnterItem(originalCont, x, y);
			}
		}

		private void Trigger_LeaveChar(AbstractCharacter cont) {
			Sanity.IfTrueThrow(cont != this.Cont, "this not in cont.");

			byte layer = (byte) this.point4d.z;
			ItemInCharArgs args = new ItemInCharArgs(this, cont, layer);

			this.TryTrigger(TriggerKey.leaveChar, args);
			ReturnIntoCharIfNeeded(cont, layer);
			try {
				this.On_LeaveChar(args);
			} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
			ReturnIntoCharIfNeeded(cont, layer);
			cont.TryTrigger(TriggerKey.itemLeave, args);
			ReturnIntoCharIfNeeded(cont, layer);
			try {
				cont.On_ItemLeave(args);
			} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
			ReturnIntoCharIfNeeded(cont, layer);

			cont.ItemMakeLimbo(this);
		}

		private void ReturnIntoCharIfNeeded(AbstractCharacter originalCont, byte layer) {
			if ((this.Cont != originalCont) || this.point4d.z != layer) {
				Logger.WriteWarning(this+" has been moved in the implementation of one of the @LeaveChar triggers. Don't do this. Putting back.");
				this.MakeLimbo();
				this.Trigger_EnterChar(originalCont, layer);
			}
		}

		private void Trigger_LeaveGround() {
			Sanity.IfTrueThrow(this.Cont != null, "this not on ground.");

			Point4D point = new Point4D(this.point4d);
			Map map = Map.GetMap(point.m);
			Region region = map.GetRegionFor(point.x, point.y);
			ItemOnGroundArgs args = new ItemOnGroundArgs(this, region, point);

			this.TryTrigger(TriggerKey.leaveRegion, args);
			ReturnOnGroundIfNeeded(point);
			try {
				this.On_LeaveRegion(args);
			} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
			ReturnOnGroundIfNeeded(point);
			Region.Trigger_ItemLeave(args);

			map.Remove(this);
		}

		private void ReturnOnGroundIfNeeded(Point4D point) {
			if ((this.Cont != null) || (!point.Equals(this))) {
				Logger.WriteWarning(this+" has been moved in the implementation of one of the @Leave/EnterGround triggers. Don't do this. Putting back.");
				this.MakeLimbo();
				this.Trigger_EnterRegion(point.x, point.y, point.z, point.m);
			}
		}

		//we expect this to be in limbo, and cont to be really container
		internal void Trigger_EnterItem(AbstractItem cont, ushort x, ushort y) {
			Sanity.IfTrueThrow(!this.IsLimbo, "this not in Limbo");
			Sanity.IfTrueThrow(!cont.IsContainer, "cont is no Container");
#if TRACE
			{
				ushort minX, minY, maxX, maxY;
				cont.GetContainerGumpBoundaries(out minX, out minY, out maxX, out maxY);
				Sanity.IfTrueThrow(x < minX, "x < minX");
				Sanity.IfTrueThrow(x > maxX, "x > maxX");
				Sanity.IfTrueThrow(y < minY, "y < minY");
				Sanity.IfTrueThrow(y > maxY, "y > maxY");
			}
#endif

			NetState.ItemAboutToChange(this);
			cont.InternalItemEnter(this);
			this.point4d.x = x;
			this.point4d.y = y;

			ItemInItemArgs args = new ItemInItemArgs(this, cont);
			this.TryTrigger(TriggerKey.enterItem, args);
			ReturnIntoItemIfNeeded(cont, x, y);
			try {
				this.On_EnterItem(args);
			} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
			ReturnIntoItemIfNeeded(cont, x, y);
			cont.TryTrigger(TriggerKey.itemEnter, args);
			ReturnIntoItemIfNeeded(cont, x, y);
			try {
				cont.On_ItemEnter(args);
			} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
			ReturnIntoItemIfNeeded(cont, x, y);
		}

		internal void InternalItemEnter(AbstractItem i) {
			this.ChangingProperties();
			ThingLinkedList tll = this.contentsOrComponents as ThingLinkedList;
			if (tll == null) {
				tll = new ThingLinkedList(this);
				this.contentsOrComponents = tll;
			}
			tll.Add(i);
			i.contOrTLL = tll;
			this.AdjustWeight(i.Weight);
		}

		//try to add item to some stack inside
		private bool TryStackToAnyInside(AbstractItem toStack) {
			ThingLinkedList tll = contentsOrComponents as ThingLinkedList;
			if (tll != null) {
				AbstractItem stackWith = (AbstractItem) tll.firstThing;
				while (stackWith != null) {
					if (stackWith != toStack) {
						if (this.Trigger_StackInCont(toStack, stackWith)) {
							return true;
						}
					}
					stackWith=(AbstractItem) stackWith.nextInList;
				}
			}
			return false;
		}

		//add "toStack" to this stack, if possible
		internal bool Trigger_StackInCont(AbstractItem toStack, AbstractItem waitingStack) {
			Sanity.IfTrueThrow(this != toStack.Cont, "toStack not in this.");
			Sanity.IfTrueThrow(this != waitingStack.Cont, "waitingStack not in this.");

			if ((waitingStack.IsStackable && toStack.IsStackable) && 
					(waitingStack.def == toStack.def) &&
					(waitingStack.Color == toStack.Color) &&
					(waitingStack.Model == toStack.Model)) {

				
				//Amount overflow checking:
				uint tmpAmount = waitingStack.Amount;
				try {
					tmpAmount=checked(tmpAmount+toStack.Amount);
				} catch (OverflowException) {
					return false;
				}

				ushort toStackX = toStack.point4d.x;
				ushort toStackY = toStack.point4d.y;
				ItemStackArgs args = new ItemStackArgs(toStack, waitingStack);

				bool cancel = toStack.TryCancellableTrigger(TriggerKey.stackOnItem, args);
				toStack.ReturnIntoItemIfNeeded(this, toStackX, toStackY);
				if (!cancel && waitingStack.Cont == this) {
					try {
						cancel = toStack.On_StackOnItem(args);
					} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
					toStack.ReturnIntoItemIfNeeded(this, toStackX, toStackY);
					if (!cancel && waitingStack.Cont == this) {
						cancel = waitingStack.TryCancellableTrigger(TriggerKey.itemStackOn, args);
						toStack.ReturnIntoItemIfNeeded(this, toStackX, toStackY);
						if (!cancel && waitingStack.Cont == this) {
							try {
								cancel = waitingStack.On_ItemStackOn(args);
							} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
							if (!cancel && waitingStack.Cont == this) {
								toStack.InternalDelete();
								waitingStack.Amount = tmpAmount;
								return true;
							} else {
								toStack.ReturnIntoItemIfNeeded(this, toStackX, toStackY);
							}
						}
					}
				}
			}
			return false;
		}

		internal static bool Trigger_StackOnGround(AbstractItem toStack, AbstractItem waitingStack) {
			Sanity.IfTrueThrow(null != toStack.Cont, "toStack not on ground.");
			Sanity.IfTrueThrow(null != waitingStack.Cont, "waitingStack not on ground.");

			if ((waitingStack.IsStackable && toStack.IsStackable) && 
					(waitingStack.def == toStack.def) &&
					(waitingStack.Color == toStack.Color) &&
					(waitingStack.Model == toStack.Model)) {


				//Amount overflow checking:
				uint tmpAmount = waitingStack.Amount;
				try {
					tmpAmount=checked(tmpAmount+toStack.Amount);
				} catch (OverflowException) {
					return false;
				}

				Point4D toStackPoint = toStack.P();
				ItemStackArgs args = new ItemStackArgs(toStack, waitingStack);

				bool cancel = toStack.TryCancellableTrigger(TriggerKey.stackOnItem, args);
				toStack.ReturnOnGroundIfNeeded(toStackPoint);
				if (!cancel && waitingStack.Cont == null) {
					try {
						cancel = toStack.On_StackOnItem(args);
					} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
					toStack.ReturnOnGroundIfNeeded(toStackPoint);
					if (!cancel && waitingStack.Cont == null) {
						cancel = waitingStack.TryCancellableTrigger(TriggerKey.itemStackOn, args);
						toStack.ReturnOnGroundIfNeeded(toStackPoint);
						if (!cancel && waitingStack.Cont == null) {
							try {
								cancel = waitingStack.On_ItemStackOn(args);
							} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
							if (!cancel && waitingStack.Cont == null) {
								toStack.InternalDelete();
								waitingStack.Amount = tmpAmount;
								return true;
							} else {
								toStack.ReturnOnGroundIfNeeded(toStackPoint);
							}
						}
					}
				}
			}
			return false;
		}


		//we expect this to be in limbo and equippable
		internal void Trigger_EnterChar(AbstractCharacter cont, byte layer) {
			Sanity.IfTrueThrow(!this.IsLimbo, "this not in limbo");
			Sanity.IfTrueThrow(((layer != (byte) LayerNames.Dragging) && (!this.IsEquippable)), "this not equippable");
			Sanity.IfTrueThrow(layer == 0, "layer == 0");

			cont.InternalItemEnter(this, layer);

			ItemInCharArgs args = new ItemInCharArgs(this, cont, layer);

			this.TryTrigger(TriggerKey.enterChar, args);
			this.ReturnIntoCharIfNeeded(cont, layer);
			try {
				this.On_EnterChar(args);
			} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
			this.ReturnIntoCharIfNeeded(cont, layer);
			cont.TryTrigger(TriggerKey.itemLeave, args);
			this.ReturnIntoCharIfNeeded(cont, layer);
			try {
				cont.On_ItemEnter(args);
			} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
			ReturnIntoCharIfNeeded(cont, layer);
		}

		//we expect this to be in limbo
		internal void Trigger_EnterRegion(ushort x, ushort y, sbyte z, byte m) {
			Sanity.IfTrueThrow(!this.IsLimbo, "Item is supposed to be in Limbo state when Trigger_Enter* called");

			this.point4d.x = x;
			this.point4d.y = y;
			this.point4d.z = z;
			this.point4d.m = m;

			Map map = Map.GetMap(m);
			Region region = map.GetRegionFor(x, y);
			Point4D point = new Point4D(x, y, z, m);
			ItemOnGroundArgs args = new ItemOnGroundArgs(this, region, point);
			map.Add(this);

			this.TryTrigger(TriggerKey.enterRegion, args);
			this.ReturnOnGroundIfNeeded(point);
			try {
				this.On_EnterRegion(args);
			} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
			this.ReturnOnGroundIfNeeded(point);
			Region.Trigger_ItemEnter(args);
		}

		internal override sealed void SetPosImpl(ushort x, ushort y, sbyte z, byte m) {
			MakeLimbo();
			Trigger_EnterRegion(x, y, z, m);
		}

		public virtual void On_LeaveItem(ItemInItemArgs args) {
			//I am the manipulated item
		}

		public virtual void On_ItemLeave(ItemInItemArgs args) {
			//I am the container
		}

		public virtual void On_LeaveChar(ItemInCharArgs args) {
		}

		public virtual void On_LeaveRegion(ItemOnGroundArgs args) {
		}

		public virtual void On_ItemEnter(ItemInItemArgs args) {
		}

		public virtual void On_EnterItem(ItemInItemArgs args) {
		}

		public virtual bool On_ItemStackOn(ItemStackArgs args) {
			return false;
		}

		public virtual bool On_StackOnItem(ItemStackArgs args) {
			return false;
		}

		public virtual void On_EnterChar(ItemInCharArgs args) {
		}

		public virtual void On_EnterRegion(ItemOnGroundArgs args) {

		}

		public void GetRandomXYInside(out ushort x, out ushort y) {
			//todo?: nonconstant bounds for this? or virtual?
			ushort minX, minY, maxX, maxY;
			GetContainerGumpBoundaries(out minX, out minY, out maxX, out maxY);
			x = (ushort) Globals.dice.Next(minX, maxX);
			y = (ushort) Globals.dice.Next(minY, maxY);
		}

		public virtual void GetContainerGumpBoundaries(out ushort minX, out ushort minY, out ushort maxX, out ushort maxY) {
			minX = 20;
			minY = 20;
			maxX = 120;
			maxY = 120;
		}

		public virtual bool On_PickupFrom(AbstractCharacter pickingChar, AbstractItem i, ref object amount) {
			//pickingChar is picking up amount of AbstractItem i from this
			return false;
		}

		/**
			Drop this item on the ground.
		*/
		public void Drop() {
			Thing cont = this.Cont;
			if (cont != null) {
				this.P(cont.TopPoint);
			}
		}

		//is not public because it leaves the item in "illegal" state...
		internal override sealed void ItemMakeLimbo(AbstractItem i) {
			Sanity.IfTrueThrow(this != i.Cont, "i not in this.");

			ThingLinkedList tll = (ThingLinkedList) i.contOrTLL;
			tll.Remove(i);
			this.ChangingProperties();//changing Count
			AdjustWeight(-i.Weight);
		}

		public void MoveInsideContainer(ushort x, ushort y) {
			AbstractItem i = this.Cont as AbstractItem;
			if (i != null) {
				ushort minX, minY, maxX, maxY;
				i.GetContainerGumpBoundaries(out minX, out minY, out maxX, out maxY);
				if (x < minX) {
					x = minX;
				} else if (x > maxX) {
					x = maxX;
				}
				if (y < minY) {
					y = minY;
				} else if (y > maxY) {
					y = maxY;
				}

				if ((this.point4d.x != x) || (this.point4d.y != y)) {
					NetState.ItemAboutToChange(this);
					point4d.x = x;
					point4d.y = y;
				}
			}//else throw? Probably not so important...
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
						((AbstractItem) t).InternalItemEnter(this);
						return;
					}
				}
				//contOrTLL=null;
				Logger.WriteWarning("The saved cont object ("+resolvedObj+") for item '"+this.ToString()+"' is not a valid container. Removing.");
				this.InternalDeleteNoRFV();
				return;
			}
			Logger.WriteWarning("The object ("+resolvedObj+") is being loaded as cont for item '"+this.ToString()+"', but it already does have it's cont. This should not happen.");
		}
	}

	public partial class AbstractCharacter {

		internal override sealed void ItemMakeLimbo(AbstractItem i) {
			Sanity.IfTrueThrow(this != i.Cont, "i not in this.");

			if (draggingLayer == i) {
				draggingLayer = null;
			} else {
				ThingLinkedList tll = (ThingLinkedList) i.contOrTLL;
				tll.Remove(i);
			}

			i.contOrTLL = null;
			this.AdjustWeight(-i.Weight);
		}

		internal void InternalItemEnter(AbstractItem i, byte layer) {
			Sanity.IfTrueThrow(!i.IsLimbo, "i not in limbo");
			Sanity.IfTrueThrow(layer == 0, "layer == 0");

			if (layer != (byte) LayerNames.Special) {
				if ((layer == (byte) LayerNames.Hand1) || (layer == (byte) LayerNames.Hand2)) {
					bool twoHanded = i.IsTwoHanded;
					AbstractItem h1 = this.FindLayer(LayerNames.Hand1);
					if (h1 != null) {
						if (twoHanded || (layer == (byte) LayerNames.Hand1) || h1.IsTwoHanded) {
							MutablePoint4D p = this.point4d;
							h1.P(p.x, p.y, p.z, p.m);
						}
					}
					AbstractItem h2 = this.FindLayer(LayerNames.Hand2);
					if (h2 != null) {
						if (twoHanded || (layer == (byte) LayerNames.Hand2) || h2.IsTwoHanded) {
							MutablePoint4D p = this.point4d;
							h2.P(p.x, p.y, p.z, p.m);
						}
					}
				} else {
					//unequip what's in there and throw it on ground.
					AbstractItem prevItem = this.FindLayer(layer);
					if (prevItem != null) {
						MutablePoint4D p = this.point4d;
						prevItem.P(p.x, p.y, p.z, p.m);
					}
					Sanity.IfTrueThrow(this.FindLayer(layer) != null, "Layer "+layer+" is supposed to be empty after it's contents was removed.");
				}
			}

			if (layer == (byte) LayerNames.Dragging) {
				draggingLayer = i;
				i.contOrTLL = this;
			} else if (layer == (byte) LayerNames.Special) {
				if (specialLayer == null) {
					specialLayer = new ThingLinkedList(this);
				}
				specialLayer.Add(i);
				i.contOrTLL = specialLayer;
			} else if (layer < sentLayers) {
				if (visibleLayers == null) {
					visibleLayers = new ThingLinkedList(this);
				}
				visibleLayers.Add(i);
				i.contOrTLL = visibleLayers;
			} else {
				if (invisibleLayers == null) {
					invisibleLayers = new ThingLinkedList(this);
				}
				invisibleLayers.Add(i);
				i.contOrTLL = invisibleLayers;
			}

			i.point4d.x = 7000;
			i.point4d.y = 0;
			i.point4d.z = (sbyte) layer;
			this.AdjustWeight(i.Weight);
		}

		internal void AddLoadedItem(AbstractItem item) {
			byte layer = (byte) item.Z;
			Thing.MarkAsLimbo(item);
			InternalItemEnter(item, layer);
		}

		public virtual void On_ItemLeave(ItemInCharArgs args) {

		}

		public bool HasPickedUp(AbstractItem itm) {
			return draggingLayer == itm;
		}

		public bool HasPickedUp(int uid) {
			return ((draggingLayer != null) && (draggingLayer.Uid == uid));
		}

		public AbstractItem FindLayer(LayerNames layer) {
			return FindLayer((byte) layer);
		}

		public AbstractItem FindLayer(byte num) {
			if (num == (int) LayerNames.Dragging) {
				return draggingLayer;
			} else if (num == (int) LayerNames.Special) {
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

		public void Equip(AbstractItem i) {
			i.Cont = this;
		}

		public void DropHeldItem() {
			AbstractItem i = draggingLayer;
			if (i != null) {
				i.P(this);
			}
		}

		private void SendMovingItemAnimation(Thing from, Thing to, AbstractItem i) {
			PacketSender.PrepareMovingItemAnimation(from, to, i);
			PacketSender.SendToClientsWhoCanSee(this);
		}

		//picks up item. typically called from InPackets. I am the src, the item can be anywhere.
		//CanReach checks are not considered done.
		public TryReachResult PickUp(AbstractItem item, ushort amt) {
			this.ThrowIfDeleted();
			item.ThrowIfDeleted();

			TryReachResult retVal = CanReach(item);
			if (retVal == TryReachResult.Succeeded) {
				item.MakeLimbo();
				item.Trigger_EnterChar(this, (byte) LayerNames.Dragging);
			}
			return retVal;
		}

		//typically called from InPackets. (I am the src)
		//could be made public if needed
		public void DropItemOnContainer(AbstractItem targetCont, ushort x, ushort y) {
			this.ThrowIfDeleted();
			targetCont.ThrowIfDeleted();

			AbstractItem i = draggingLayer;
			if (i == null) {
				throw new Exception("Character '"+this+"' has no item dragged to drop on '"+targetCont+"'");
			}
			i.ThrowIfDeleted();

			if (targetCont.IsContainer) {
				ushort minX, minY, maxX, maxY;
				targetCont.GetContainerGumpBoundaries(out minX, out minY, out maxX, out maxY);
				if (x < minX) {
					x = minX;
				} else if (x > maxX) {
					x = maxX;
				}
				if (y < minY) {
					y = minY;
				} else if (y > maxY) {
					y = maxY;
				}
				i.MakeLimbo();
				i.Trigger_EnterItem(targetCont, x, y);
			} else {
				throw new InvalidOperationException("The item ("+targetCont+") is not a container");
			}
		}

		//typically called from InPackets. (I am the src)
		//could be made public if needed
		public void DropItemOnItem(AbstractItem target) {
			ThrowIfDeleted();
			target.ThrowIfDeleted();
			AbstractItem i = draggingLayer;
			if (i == null) {
				throw new Exception("Character '"+this+"' has no item dragged to drop on '"+target+"'");
			}
			i.ThrowIfDeleted();

			Thing cont = i.Cont;
			if (cont != null) {
				AbstractItem contAsItem = cont as AbstractItem;
				if (contAsItem != null) {
					i.MakeLimbo();
					i.Trigger_EnterItem(contAsItem, i.point4d.x, i.point4d.y);
					contAsItem.Trigger_StackInCont(i, target);
				} else {
					//stacking with an equipped item? huh. Put in pack.
					i.Cont = ((AbstractCharacter) cont).Backpack;
				}
			} else {
				MutablePoint4D targP = target.point4d;
				sbyte newZ = (sbyte) Math.Min(sbyte.MaxValue, (targP.z + target.Height));

				i.MakeLimbo();
				i.Trigger_EnterRegion(targP.x, targP.y, newZ, targP.m);
				AbstractItem.Trigger_StackOnGround(i, target);
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

			i.MakeLimbo();
			i.Trigger_EnterRegion(x, y, z, this.point4d.m);
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

			i.Cont = target.Backpack;
		}

		public void EquipItemOnChar(AbstractCharacter target) {
			ThrowIfDeleted();
			target.ThrowIfDeleted();
			AbstractItem i = draggingLayer;
			if (i == null) {
				throw new Exception("Character '"+this+"' has no item dragged to drop on '"+target+"'");
			}
			i.ThrowIfDeleted();

			//dropping on ones own toon/paperdoll. Equip or put into backpack.
			if (i.IsEquippable) {
				byte layer = i.Layer;
				if ((layer < sentLayers) && (layer > 0)) {
					i.MakeLimbo();
					i.Trigger_EnterChar(target, layer);
					return;
				}
			}
			i.Cont = target.Backpack;
		}

		public virtual void On_ItemEnter(ItemInCharArgs args) {
		}
	}

	public class DupeArgs : ScriptArgs {
		public readonly Thing copy;
		public readonly Thing model;

		public DupeArgs(Thing model, Thing copy)
				: base(model, copy) {
			this.model = model;
			this.copy = copy;
		}
	}

	public class ItemStackArgs : ScriptArgs {
		public readonly Thing manipulatedItem;
		public readonly Thing waitingStack ;

		public ItemStackArgs(Thing manipulatedItem, Thing waitingStack)
				: base(manipulatedItem, waitingStack ) {
			this.manipulatedItem = manipulatedItem;
			this.waitingStack  = waitingStack ;
		}
	}

	public class ItemOnGroundArgs : ScriptArgs {
		public readonly AbstractItem manipulatedItem;
		public readonly Region region;
		public readonly Point4D point;

		public ItemOnGroundArgs(AbstractItem manipulatedItem, Region region, Point4D point)
			: base(manipulatedItem, region, point) {
			this.manipulatedItem = manipulatedItem;
			this.region = region;
			this.point = point;
		}
	}

	public class ItemInItemArgs : ScriptArgs {
		public readonly AbstractItem manipulatedItem;
		public readonly AbstractItem container;

		public ItemInItemArgs(AbstractItem manipulatedItem, AbstractItem container)
			: base(manipulatedItem, container) {
			this.manipulatedItem = manipulatedItem;
			this.container = container;
		}
	}

	public class ItemInCharArgs : ScriptArgs {
		public readonly AbstractItem manipulatedItem;
		public readonly AbstractCharacter cont;
		public readonly byte layer;

		public ItemInCharArgs(AbstractItem manipulatedItem, AbstractCharacter cont, byte layer)
			: base(manipulatedItem, cont, layer) {
			this.manipulatedItem = manipulatedItem;
			this.cont = cont;
			this.layer = layer;
		}
	}
}