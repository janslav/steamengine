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
using SteamEngine.Regions;
using SteamEngine.Networking;

namespace SteamEngine {

	public partial class Thing {
		public virtual void On_Dupe(Thing model) {

		}

		public Thing Dupe() {
			if (IsPlayer) {
				throw new SEException("You can not dupe a PC!");
			}
			Thing copy = (Thing) DeepCopyFactory.GetCopy(this);

			AbstractItem copyAsItem = copy as AbstractItem;
			if (copyAsItem != null) {
				Thing cont = this.Cont;
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

			Trigger_Dupe(this);

			return copy;
		}

		//duplicating some code here, but hey... who will know ;)
		private AbstractItem Dupe(Thing cont) {
			AbstractItem copy = (AbstractItem) DeepCopyFactory.GetCopy(this);

			AbstractItem contItem = cont as AbstractItem;
			if (contItem == null) {
				MarkAsLimbo(copy);
				copy.Trigger_EnterChar((AbstractCharacter) cont, (byte) this.point4d.z);
			} else {
				MarkAsLimbo(copy);
				copy.Trigger_EnterItem(contItem, this.point4d.x, this.point4d.y);
			}

			Trigger_Dupe(this);

			return copy;
		}

		private void Trigger_Dupe(Thing model) {
			this.TryTrigger(TriggerKey.dupe, new ScriptArgs(model));
			try {
				this.On_Dupe(model);
			} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
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
		internal abstract void SetPosImpl(int x, int y, int z, byte m);
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
					throw new SEException("value is null");
				}

				AbstractItem contItem = value as AbstractItem;
				if (contItem == null) {
					if (this.IsEquippable) {
						byte layer = this.Layer;
						if (layer > 0) {
							this.MakeLimbo();
							Trigger_EnterChar((AbstractCharacter) value, layer);
						} else {
							throw new SEException("Item '" + this + "' is equippable, but has Layer not set.");
						}
					} else {
						throw new SEException("Item '" + this + "' is not equippable.");
					}
				} else if (contItem.IsContainer) {
					this.MakeLimbo();
					int x, y;
					contItem.GetRandomXYInside(out x, out y);
					this.Trigger_EnterItem(contItem, x, y);

					contItem.TryStackToAnyInside(this);
				} else {
					throw new SEException("Item '" + value + "' is not a container, it can't contain items.");
				}
			}
		}

		internal override void MakeLimbo() {
			if (!this.IsLimbo) {
				OpenedContainers.SetContainerClosed(this);
				this.RemoveFromView();
				ItemSyncQueue.AboutToChange(this);

				Thing cont = this.Cont;
				if (cont == null) {
					this.Trigger_LeaveGround();
				} else {
					AbstractItem contItem = cont as AbstractItem;
					if (contItem == null) {
						this.Trigger_LeaveChar((AbstractCharacter) cont);
					} else {
						this.Trigger_LeaveItem(contItem);
					}
				}
				base.MakeLimbo();
			}
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
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

		private void ReturnIntoItemIfNeeded(AbstractItem originalCont, int x, int y) {
			if (this.Cont != originalCont) {
				Logger.WriteWarning(this + " has been moved in the implementation of one of the @LeaveItem/@EnterItem triggers. Don't do this. Putting back.");
				this.MakeLimbo();
				this.Trigger_EnterItem(originalCont, x, y);
			}
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		private void Trigger_LeaveChar(AbstractCharacter cont) {
			Sanity.IfTrueThrow(cont != this.Cont, "this not in cont.");

			byte layer = (byte) this.point4d.z;
			ItemInCharArgs args = new ItemInCharArgs(this, cont, layer);

			if (this.IsEquippable && this.Layer == layer) {
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
			} else {
				this.TryTrigger(TriggerKey.unEquip, args);
				ReturnIntoCharIfNeeded(cont, layer);
				try {
					this.On_UnEquip(args);
				} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
				ReturnIntoCharIfNeeded(cont, layer);
				cont.TryTrigger(TriggerKey.itemUnEquip, args);
				ReturnIntoCharIfNeeded(cont, layer);
				try {
					cont.On_ItemUnEquip(args);
				} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
				ReturnIntoCharIfNeeded(cont, layer);
			}

			cont.ItemMakeLimbo(this);
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual void On_UnEquip(ItemInCharArgs args) {
		}

		private void ReturnIntoCharIfNeeded(AbstractCharacter originalCont, byte layer) {
			if ((this.Cont != originalCont) || this.point4d.z != layer) {
				Logger.WriteWarning(this + " has been moved in the implementation of one of the @LeaveChar triggers. Don't do this. Putting back.");
				this.MakeLimbo();
				this.Trigger_EnterChar(originalCont, layer);
			}
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		private void Trigger_LeaveGround() {
			Sanity.IfTrueThrow(this.Cont != null, "this not on ground.");
			
			if (Map.IsValidPos(this)) {
				Point4D point = new Point4D(this.point4d);
				Map map = Map.GetMap(point.M);
				Region region = map.GetRegionFor(point.X, point.Y);
				ItemOnGroundArgs args = new ItemOnGroundArgs(this, region, point);

				this.TryTrigger(TriggerKey.leaveRegion, args);
				ReturnOnGroundIfNeeded(point);
				try {
					this.On_LeaveRegion(args);
				} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
				ReturnOnGroundIfNeeded(point);
				Region.Trigger_ItemLeave(args);

				map.Remove(this);
			} //else what?
		}

		private void ReturnOnGroundIfNeeded(Point4D point) {
			if ((this.Cont != null) || (!point.Equals(this))) {
				Logger.WriteWarning(this + " has been moved in the implementation of one of the @Leave/EnterGround triggers. Don't do this. Putting back.");
				this.MakeLimbo();
				this.Trigger_EnterRegion(point.X, point.Y, point.Z, point.M);
			}
		}

		//we expect this to be in limbo, and cont to be really container
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		internal void Trigger_EnterItem(AbstractItem cont, int x, int y) {
			Sanity.IfTrueThrow(!this.IsLimbo, "this not in Limbo");
			Sanity.IfTrueThrow(!cont.IsContainer, "cont is no Container");
#if TRACE
			{
				int minX, minY, maxX, maxY;
				cont.GetContainerGumpBoundaries(out minX, out minY, out maxX, out maxY);
				Sanity.IfTrueThrow(x < minX, "x < minX");
				Sanity.IfTrueThrow(x > maxX, "x > maxX");
				Sanity.IfTrueThrow(y < minY, "y < minY");
				Sanity.IfTrueThrow(y > maxY, "y > maxY");
			}
#endif

			cont.InternalItemEnter(this);
			this.point4d.x = (ushort) x;
			this.point4d.y = (ushort) y;

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
			this.InvalidateProperties();
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
		internal bool TryStackToAnyInside(AbstractItem toStack) {
			ThingLinkedList tll = this.contentsOrComponents as ThingLinkedList;
			if (tll != null) {
				AbstractItem stackWith = (AbstractItem) tll.firstThing;
				while (stackWith != null) {
					if (stackWith != toStack) {
						if (this.Trigger_StackInCont(toStack, stackWith)) {
							return true;
						}
					}
					stackWith = (AbstractItem) stackWith.nextInList;
				}
			}
			return false;
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
		public static bool CouldBeStacked(AbstractItem a, AbstractItem b) {
			return ((a.IsStackable && b.IsStackable) &&
					(a.def == b.def) &&
					(a.Color == b.Color) &&
					(a.Model == b.Model));
		}

		//add "toStack" to this stack, if possible
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		internal bool Trigger_StackInCont(AbstractItem toStack, AbstractItem waitingStack) {
			Sanity.IfTrueThrow(this != toStack.Cont, "toStack not in this.");
			Sanity.IfTrueThrow(this != waitingStack.Cont, "waitingStack not in this.");

			if (CouldBeStacked(toStack, waitingStack)) {
				//Amount overflow checking:
				int tmpAmount = waitingStack.Amount;
				try {
					tmpAmount = checked(tmpAmount + toStack.Amount);
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

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		internal static bool Trigger_StackOnGround(AbstractItem toStack, AbstractItem waitingStack) {
			Sanity.IfTrueThrow(null != toStack.Cont, "toStack not on ground.");
			Sanity.IfTrueThrow(null != waitingStack.Cont, "waitingStack not on ground.");

			if (CouldBeStacked(toStack, waitingStack)) {

				//Amount overflow checking:
				int tmpAmount = waitingStack.Amount;
				try {
					tmpAmount = checked(tmpAmount + toStack.Amount);
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
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		internal void Trigger_EnterChar(AbstractCharacter cont, byte layer) {
			Sanity.IfTrueThrow(!this.IsLimbo, "this not in limbo");
			Sanity.IfTrueThrow(((layer != (byte) LayerNames.Dragging) && (!this.IsEquippable)), "this not equippable");
			Sanity.IfTrueThrow(layer == 0, "layer == 0");

			cont.InternalItemEnter(this, layer);

			ItemInCharArgs args = new ItemInCharArgs(this, cont, layer);

			//do we really want this?
			if (this.IsEquippable && this.Layer == layer) {
				this.TryTrigger(TriggerKey.equip, args);
				this.ReturnIntoCharIfNeeded(cont, layer);
				try {
					this.On_Equip(args);
				} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
				this.ReturnIntoCharIfNeeded(cont, layer);
				cont.TryTrigger(TriggerKey.itemEquip, args);
				this.ReturnIntoCharIfNeeded(cont, layer);
				try {
					cont.On_ItemEquip(args);
				} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
				ReturnIntoCharIfNeeded(cont, layer);
			} else {
				this.TryTrigger(TriggerKey.enterChar, args);
				this.ReturnIntoCharIfNeeded(cont, layer);
				try {
					this.On_EnterChar(args);
				} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
				this.ReturnIntoCharIfNeeded(cont, layer);
				cont.TryTrigger(TriggerKey.itemEnter, args);
				this.ReturnIntoCharIfNeeded(cont, layer);
				try {
					cont.On_ItemEnter(args);
				} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
				ReturnIntoCharIfNeeded(cont, layer);
			}

		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual void On_Equip(ItemInCharArgs args) {
		}

		//we expect this to be in limbo
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		internal void Trigger_EnterRegion(int x, int y, int z, byte m) {
			Sanity.IfTrueThrow(!this.IsLimbo, "Item is supposed to be in Limbo state when Trigger_Enter* called");

			this.point4d.SetP(x, y, z, m);

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

		internal override sealed void SetPosImpl(int x, int y, int z, byte m) {
			if (Map.IsValidPos(x, y, m)) {
				MakeLimbo();
				Trigger_EnterRegion(x, y, z, m);
			} else {
				throw new SEException("Invalid position (" + x + "," + y + " on mapplane " + m + ")");
			}
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual void On_LeaveItem(ItemInItemArgs args) {
			//I am the manipulated item
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual void On_ItemLeave(ItemInItemArgs args) {
			//I am the container
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual void On_LeaveChar(ItemInCharArgs args) {
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual void On_LeaveRegion(ItemOnGroundArgs args) {
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual void On_ItemEnter(ItemInItemArgs args) {
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual void On_EnterItem(ItemInItemArgs args) {
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual bool On_ItemStackOn(ItemStackArgs args) {
			return false;
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual bool On_StackOnItem(ItemStackArgs args) {
			return false;
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual void On_EnterChar(ItemInCharArgs args) {
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual void On_EnterRegion(ItemOnGroundArgs args) {

		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "0#"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "1#")]
		public void GetRandomXYInside(out int x, out int y) {
			//todo?: nonconstant bounds for this? or virtual?
			int minX, minY, maxX, maxY;
			GetContainerGumpBoundaries(out minX, out minY, out maxX, out maxY);
			x = Globals.dice.Next(minX, maxX);
			y = Globals.dice.Next(minY, maxY);
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "2#"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "3#"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "0#"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "1#")]
		public virtual void GetContainerGumpBoundaries(out int minX, out int minY, out int maxX, out int maxY) {
			minX = 20;
			minY = 20;
			maxX = 120;
			maxY = 120;
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
			this.InvalidateProperties();//changing Count
			this.AdjustWeight(-i.Weight);
		}

		public void MoveInsideContainer(int x, int y) {
			AbstractItem i = this.Cont as AbstractItem;
			if (i != null) {
				int minX, minY, maxX, maxY;
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
					OpenedContainers.SetContainerClosed(this);//if I am a container too, I get closed also
					ItemSyncQueue.AboutToChange(this);
					point4d.x = (ushort) x;
					point4d.y = (ushort) y;
				}
			}//else throw? Probably not so important...
		}

		private void MakePositionInContLegal() {
			AbstractItem i = this.Cont as AbstractItem;
			if (i != null) {
				MutablePoint4D p = this.point4d;

				int minX, minY, maxX, maxY;
				i.GetContainerGumpBoundaries(out minX, out minY, out maxX, out maxY);
				if (p.x < minX) {
					p.x = (ushort) minX;
				} else if (p.x > maxX) {
					p.x = (ushort) maxX;
				}
				if (p.y < minY) {
					p.y = (ushort) minY;
				} else if (p.y > maxY) {
					p.y = (ushort) maxY;
				}
			}
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public void LoadCont_Delayed(object resolvedObj, string filename, int line) {
			if (Uid == -1) {
				//I was probably cleared because of failed load. Let me get deleted by garbage collector.
				return;
			}
			if (Cont == null) {
				Thing t = resolvedObj as Thing;
				if (t != null) {
					if ((t.IsChar) && (this.IsEquippable)) {
						((AbstractCharacter) t).AddLoadedItem(this);
						return;
					} else if (t.IsContainer) {
						((AbstractItem) t).InternalItemEnter(this);
						this.MakePositionInContLegal();
						return;
					}
				}
				//contOrTLL=null;
				Logger.WriteWarning("The saved cont object (" + resolvedObj + ") for item '" + this.ToString() + "' is not a valid container. Removing.");
				this.InternalDeleteNoRFV();
				return;
			}
			Logger.WriteWarning("The object (" + resolvedObj + ") is being loaded as cont for item '" + this.ToString() + "', but it already does have it's cont. This should not happen.");
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member"), Summary("Someone is trying to pick me up")]
		public virtual bool On_DenyPickup(DenyPickupArgs args) {
			return false;
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member"), Summary("Someone is trying to pick up item that is contained in me")]
		public virtual bool On_DenyPickupItemFrom(DenyPickupArgs args) {
			return false;
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member"), Summary("Someone is trying to put me on ground")]
		public virtual bool On_DenyPutOnGround(DenyPutOnGroundArgs args) {
			return false;
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member"), Summary("Someone is trying to put me in a container")]
		public virtual bool On_DenyPutInItem(DenyPutInItemArgs args) {
			return false;
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member"), Summary("Someone is trying to put an item in me (I am a container)")]
		public virtual bool On_DenyPutItemIn(DenyPutInItemArgs args) {
			return false;
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member"), Summary("I am being put on another item")]
		public virtual bool On_PutOnItem(ItemOnItemArgs args) {
			return false;
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member"), Summary("Another item is being put on me")]
		public virtual bool On_PutItemOn(ItemOnItemArgs args) {
			return false;
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member"), Summary("I am being put on a character other than the person wielding me")]
		public virtual bool On_PutOnChar(ItemOnCharArgs args) {
			return false;
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member"), Summary("Iam being equipped on a character")]
		public virtual bool On_DenyEquip(DenyEquipArgs args) {
			return false;
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
					Sanity.IfTrueThrow(this.FindLayer(layer) != null, "Layer " + layer + " is supposed to be empty after it's contents was removed.");
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

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
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

		//
		public void TryEquip(AbstractItem i) {
			i.Cont = this;
		}

		[Summary("Tries to have the char drop the item it is dragging. First it tries to put it in backpack, then on ground.")]
		[Return("True if it is dragging no item after the method passes.")]
		public bool TryGetRidOfDraggedItem() {
			AbstractItem i = draggingLayer;
			if (i != null) {
				DenyResult result = this.TryPutItemOnItem(this.Backpack);
				if (result != DenyResult.Allow && this.draggingLayer == i) {//can't put item in his own pack? unprobable but possible.
					GameState state = this.GameState;
					SteamEngine.Communication.TCP.TCPConnection<GameState> conn = null;
					if (state != null) {
						conn = state.Conn;
						PacketSequences.SendDenyResultMessage(conn, i, result);
					}

					MutablePoint4D point = this.point4d;
					result = this.TryPutItemOnGround(point.x, point.y, point.z);

					if (result != DenyResult.Allow && this.draggingLayer == i) {//can't even put item on ground?
						if (state != null) {
							PacketSequences.SendDenyResultMessage(conn, i, result);
						}
						//The player is not allowed to put the item anywhere. 
						//Too bad we can't tell the client
					}
				}
			}

			return this.draggingLayer == null;
		}

		private void SendMovingItemAnimation(IPoint4D from, IPoint4D to, AbstractItem i) {
			DraggingOfItemOutPacket p = Pool<DraggingOfItemOutPacket>.Acquire();
			p.Prepare(from, to, i);
			GameServer.SendToClientsWhoCanSee(this, p);
		}

		#region client
		//picks up item. typically called from InPackets. I am the src, the item can be anywhere.
		//will run the @deny triggers
		//CanReach checks are not considered done.
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		public DenyResult TryPickupItem(AbstractItem item, int amt) {
			this.ThrowIfDeleted();
			item.ThrowIfDeleted();

			if (!this.TryGetRidOfDraggedItem()) {
				return DenyResult.Deny_YouAreAlreadyHoldingAnItem;
			}

			//@deny triggers
			DenyPickupArgs args = new DenyPickupArgs(this, item, amt);

			bool cancel = this.TryCancellableTrigger(TriggerKey.denyPickupItem, args);
			if (!cancel) {
				try {
					cancel = this.On_DenyPickupItem(args);
				} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
				if (!cancel) {
					cancel = item.TryCancellableTrigger(TriggerKey.denyPickup, args);
					if (!cancel) {
						try {
							cancel = item.On_DenyPickup(args);
						} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }

						if (!cancel) {
							Thing c = item.Cont;
							if (c != null) {
								AbstractItem contItem = c as AbstractItem;
								if (contItem != null) {
									cancel = contItem.TryCancellableTrigger(TriggerKey.denyPickupItemFrom, args);
									if (!cancel) {
										try {
											cancel = contItem.On_DenyPickupItemFrom(args);
										} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
									}
								} else {
									AbstractCharacter contChar = (AbstractCharacter) c;
									cancel = contChar.TryCancellableTrigger(TriggerKey.denyPickupItemFrom, args);
									if (!cancel) {
										try {
											cancel = contChar.On_DenyPickupItemFrom(args);
										} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
									}
								}
							} else {
								MutablePoint4D p = item.point4d;
								Region region = Map.GetMap(p.m).GetRegionFor(p.x, p.y);
								cancel = Region.Trigger_DenyPickupItemFrom(args);
							}
						}
					}
				}
			}

			DenyResult retVal = args.Result;

			//default implementation
			if ((!cancel) && (retVal == DenyResult.Allow)) {
				retVal = this.CanReach(item);
			}

			//equip into dragging layer
			if (retVal == DenyResult.Allow) {
				IPoint4D oldPoint = item.TopObj();
				if (oldPoint != this) {
					if (oldPoint == item) {
						oldPoint = item.P();
					}
				} else {
					oldPoint = null;
				}

				int amountToPick = args.Amount;
				int amountSum = item.Amount;
				if (!item.IsEquipped && amountToPick < amountSum) {
					AbstractItem dupedItem = (AbstractItem) item.Dupe();
					dupedItem.Amount = (amountSum - amountToPick);
					item.Amount = amountToPick;
				}

				item.MakeLimbo();
				item.Trigger_EnterChar(this, (byte) LayerNames.Dragging);
				if (oldPoint != null) {
					this.SendMovingItemAnimation(oldPoint, this, item);
				}
			}
			return retVal;
		}

		//typically called from InPackets. (I am the src)
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		public DenyResult TryPutItemInItem(AbstractItem targetCont, int x, int y, bool tryStacking) {
			this.ThrowIfDeleted();
			targetCont.ThrowIfDeleted();

			AbstractItem item = draggingLayer;
			if (item == null) {
				throw new SEException("Character '" + this + "' has no item dragged to drop on '" + targetCont + "'");
			}
			item.ThrowIfDeleted();

			if (targetCont.IsContainer) {
				DenyPutInItemArgs args = new DenyPutInItemArgs(this, item, targetCont);

				bool cancel = this.TryCancellableTrigger(TriggerKey.denyPutItemInItem, args);
				if (!cancel) {
					try {
						cancel = this.On_DenyPutItemInItem(args);
					} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
					if (!cancel) {
						cancel = item.TryCancellableTrigger(TriggerKey.denyPutInItem, args);
						if (!cancel) {
							try {
								cancel = item.On_DenyPutInItem(args);
							} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
							if (!cancel) {
								cancel = targetCont.TryCancellableTrigger(TriggerKey.denyPutItemIn, args);
								if (!cancel) {
									try {
										cancel = targetCont.On_DenyPutItemIn(args);
									} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
								}
							}
						}
					}
				}

				DenyResult retVal = args.Result;

				if ((!cancel) && (retVal == DenyResult.Allow)) {
					retVal = OpenedContainers.HasContainerOpen(this, targetCont);

					if (retVal != DenyResult.Allow) {//we don't have it open, let's check if we could
						retVal = this.CanOpenContainer(targetCont);//we check if targetCont could be openable (it also does canreach checks)
					}
				}

				if (retVal == DenyResult.Allow) {
					int minX, minY, maxX, maxY;
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

					item.MakeLimbo();
					item.Trigger_EnterItem(targetCont, x, y);
					if (tryStacking) {
						targetCont.TryStackToAnyInside(item);
					}
					if (targetCont.TopObj() != this) {
						this.SendMovingItemAnimation(this, targetCont, item);
					}
				}
				return retVal;
			} else {
				throw new SEException("The item (" + targetCont + ") is not a container");
			}
		}

		//typically called from InPackets. (I am the src)
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		public DenyResult TryPutItemOnItem(AbstractItem target) {
			ThrowIfDeleted();
			target.ThrowIfDeleted();
			AbstractItem item = draggingLayer;
			if (item == null) {
				throw new SEException("Character '" + this + "' has no item dragged to drop on '" + target + "'");
			}
			item.ThrowIfDeleted();

			if (target.IsContainer) {
				int x, y;
				target.GetRandomXYInside(out x, out y);
				return this.TryPutItemInItem(target, x, y, true);
			} else {
				Thing cont = target.Cont;
				if (cont != null) {
					AbstractItem contAsItem = cont as AbstractItem;
					if (contAsItem != null) {
						MutablePoint4D point = target.point4d;
						DenyResult result = this.TryPutItemInItem(contAsItem, point.x, point.y, false);
						if (result == DenyResult.Allow) {
							if (!contAsItem.Trigger_StackInCont(item, target)) {
								//couldn't stack, let's see if there's some script for the stacking
								goto trigger;
							}
						}
						return result;
					} else {
						//we're stacking something on an equipped item? that's pretty weird.
						return DenyResult.Deny_NoMessage;
					}
				} else {
					MutablePoint4D point = target.point4d;
					DenyResult result = this.TryPutItemOnGround(point.x, point.y, point.z);
					if (result == DenyResult.Allow) {
						if (!AbstractItem.Trigger_StackOnGround(item, target)) {
							//couldn't stack, let's see if there's some script for the stacking
							goto trigger;
						}
					}
					return result;
				}
			}

		trigger:
			ItemOnItemArgs args = new ItemOnItemArgs(this, item, target);
			bool cancel = item.TryCancellableTrigger(TriggerKey.putOnItem, args);
			if (!cancel) {
				try {
					cancel = item.On_PutOnItem(args);
				} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
				if (!cancel) {
					cancel = target.TryCancellableTrigger(TriggerKey.putItemOn, args);
					if (!cancel) {
						try {
							cancel = target.On_PutItemOn(args);
							//we do nothing...
						} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
					}
				}
			}

			return DenyResult.Allow;
		}

		//typically called from InPackets. (I am the src)
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		public DenyResult TryPutItemOnGround(int x, int y, int z) {
			ThrowIfDeleted();
			AbstractItem item = draggingLayer;
			if (item == null) {
				throw new SEException("Character '" + this + "' has no item dragged to drop on ground");
			}
			item.ThrowIfDeleted();

			byte m = this.M;
			IPoint4D point = new Point4D(x, y, z, m);
			DenyPutOnGroundArgs args = new DenyPutOnGroundArgs(this, item, point);

			bool cancel = this.TryCancellableTrigger(TriggerKey.denyPutItemOnGround, args);
			if (!cancel) {
				try {
					cancel = this.On_DenyPutItemOnGround(args);
				} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
				if (!cancel) {
					cancel = item.TryCancellableTrigger(TriggerKey.denyPutOnGround, args);
					if (!cancel) {
						try {
							cancel = item.On_DenyPutOnGround(args);
						} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }

						if (!cancel) {
							Region region = Map.GetMap(m).GetRegionFor(x, y);
							cancel = Region.Trigger_DenyPutItemOn(args);
						}
					}
				}
			}

			DenyResult retVal = args.Result;
			point = args.Point.TopPoint;

			//default implementation
			if ((!cancel) && (retVal == DenyResult.Allow)) {
				retVal = this.CanReachCoordinates(point);
			}


			if (retVal == DenyResult.Allow) {
				item.MakeLimbo();
				item.Trigger_EnterRegion(point.X, point.Y, point.Z, point.M);
				this.SendMovingItemAnimation(this, point, item);
			}

			return retVal;
		}

		//typically called from InPackets. drops the held item on target (I am the src)
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		public DenyResult TryPutItemOnChar(AbstractCharacter target) {
			ThrowIfDeleted();
			target.ThrowIfDeleted();
			AbstractItem item = draggingLayer;
			if (item == null) {
				throw new SEException("Character '" + this + "' has no item dragged to drop on '" + target + "'");
			}
			item.ThrowIfDeleted();

			if (target == this) {
				return this.TryPutItemOnItem(this.Backpack);
			} else {
				ItemOnCharArgs args = new ItemOnCharArgs(this, item, target);

				bool cancel = this.TryCancellableTrigger(TriggerKey.putItemOnChar, args);
				if (!cancel) {
					try {
						cancel = this.On_PutItemOnChar(args);
					} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
					if (!cancel) {
						cancel = item.TryCancellableTrigger(TriggerKey.putOnChar, args);
						if (!cancel) {
							try {
								cancel = item.On_PutOnChar(args);
							} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
							if (!cancel) {
								cancel = target.TryCancellableTrigger(TriggerKey.putItemOn, args);
								if (!cancel) {
									try {
										cancel = target.On_PutItemOn(args);
									} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
								}
							}
						}
					}
				}

				if (!cancel) {
					return this.TryPutItemOnItem(target.Backpack);
				}
			}

			return DenyResult.Allow;
		}

		//typically called from InPackets. drops the held item on target (I am the src)
		//dropping on ones own paperdoll
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		public DenyResult TryEquipItemOnChar(AbstractCharacter target) {
			ThrowIfDeleted();
			target.ThrowIfDeleted();
			AbstractItem item = this.draggingLayer;
			if (item == null) {
				throw new SEException("Character '" + this + "' has no item dragged to drop on '" + target + "'");
			}
			item.ThrowIfDeleted();

			if (item.IsEquippable) {
				byte layer = item.Layer;
				if ((layer < sentLayers) && (layer > 0)) {
					bool succeededUnequipping = true;
					if (layer != (byte) LayerNames.Special) {
						if ((layer == (byte) LayerNames.Hand1) || (layer == (byte) LayerNames.Hand2)) {
							bool twoHanded = item.IsTwoHanded;
							AbstractItem h1 = this.FindLayer(LayerNames.Hand1);
							if (h1 != null) {
								if (twoHanded || (layer == (byte) LayerNames.Hand1) || h1.IsTwoHanded) {
									succeededUnequipping = this.TryUnequip(h1);
								}
							}
							AbstractItem h2 = this.FindLayer(LayerNames.Hand2);
							if (h2 != null) {
								if (twoHanded || (layer == (byte) LayerNames.Hand2) || h2.IsTwoHanded) {
									succeededUnequipping = succeededUnequipping && this.TryUnequip(h2);
								}
							}
						} else {
							//unequip what's in there and throw it on ground.
							AbstractItem prevItem = this.FindLayer(layer);
							if (prevItem != null) {
								succeededUnequipping = this.TryUnequip(prevItem);
							}
						}
					}

					if (!succeededUnequipping) {
						return DenyResult.Deny_YouAreAlreadyHoldingAnItem;
					}

					DenyEquipArgs args = new DenyEquipArgs(this, item, target, layer);

					bool cancel = this.TryCancellableTrigger(TriggerKey.denyEquipOnChar, args);
					if (!cancel) {
						try {
							cancel = this.On_DenyEquipOnChar(args);
						} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
						if (!cancel) {
							cancel = target.TryCancellableTrigger(TriggerKey.denyEquip, args);
							if (!cancel) {
								try {
									cancel = target.On_DenyEquip(args);
								} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
								if (!cancel) {
									cancel = item.TryCancellableTrigger(TriggerKey.denyEquip, args);
									if (!cancel) {
										try {
											cancel = item.On_DenyEquip(args);
										} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
									}
								}
							}
						}
					}

					DenyResult result = args.Result;

					if (result == DenyResult.Allow) {
						if (this != target) {
							result = this.CanReach(target);
						}
					}

					if (result == DenyResult.Allow) {
						item.MakeLimbo();
						item.Trigger_EnterChar(target, layer);
						if (this != target) {
							this.SendMovingItemAnimation(this, target, item);
						}
					}

					return result;
				}
			}

			return this.TryPutItemOnChar(target);
		}

		private bool TryUnequip(AbstractItem i) {
			DenyResult dr = this.TryPickupItem(i, 1);
			if (dr == DenyResult.Allow) {
				return this.TryGetRidOfDraggedItem();
			}
			return false;
		}

		#endregion client trigger_deny methods


		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual void On_ItemEnter(ItemInCharArgs args) {
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual void On_ItemEquip(ItemInCharArgs args) {
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual void On_ItemUnEquip(ItemInCharArgs args) {

		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual bool On_DenyPutItemOnGround(DenyPutOnGroundArgs args) {
			return false;
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual bool On_DenyPickupItem(DenyPickupArgs args) {
			return false;
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual bool On_DenyPutItemInItem(DenyPutInItemArgs args) {
			return false;
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual bool On_DenyPickupItemFrom(DenyPickupArgs args) {
			return false;
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual bool On_PutItemOn(ItemOnCharArgs args) {
			return false;
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual bool On_PutItemOnChar(ItemOnCharArgs args) {
			return false;
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual bool On_DenyEquip(DenyEquipArgs args) {
			return false;
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual bool On_DenyEquipOnChar(DenyEquipArgs args) {
			return false;
		}
	}

	public class ItemStackArgs : ScriptArgs {
		public readonly Thing manipulatedItem;
		public readonly Thing waitingStack;

		public ItemStackArgs(Thing manipulatedItem, Thing waitingStack)
			: base(manipulatedItem, waitingStack) {
			this.manipulatedItem = manipulatedItem;
			this.waitingStack = waitingStack;
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

	public class DenyTriggerArgs : ScriptArgs {
		public DenyTriggerArgs(params object[] argv)
			: base(argv) {
			Sanity.IfTrueThrow(!(argv[0] is DenyResult), "argv[0] is not DenyResult");
		}

		public DenyResult Result {
			get {
				return (DenyResult) Convert.ToInt32(argv[0]);
			}
			set {
				argv[0] = value;
			}
		}
	}

	public class DenyPickupArgs : DenyTriggerArgs {
		public readonly AbstractCharacter pickingChar;
		public readonly AbstractItem manipulatedItem;

		public DenyPickupArgs(AbstractCharacter pickingChar, AbstractItem manipulatedItem, int amount)
			: base(DenyResult.Allow, pickingChar, manipulatedItem, amount) {
			this.pickingChar = pickingChar;
			this.manipulatedItem = manipulatedItem;
		}

		public int Amount {
			get {
				return Convert.ToInt32(this.argv[3]);
			}
			set {
				argv[3] = value;
			}
		}
	}

	public class DenyPutInItemArgs : DenyTriggerArgs {
		public readonly AbstractCharacter pickingChar;
		public readonly AbstractItem manipulatedItem;
		public readonly AbstractItem container;

		public DenyPutInItemArgs(AbstractCharacter pickingChar, AbstractItem manipulatedItem, AbstractItem container)
			: base(DenyResult.Allow, pickingChar, manipulatedItem, container) {
			this.pickingChar = pickingChar;
			this.manipulatedItem = manipulatedItem;
			this.container = container;
		}
	}

	public class DenyPutOnGroundArgs : DenyTriggerArgs {
		public readonly AbstractCharacter puttingChar;
		public readonly AbstractItem manipulatedItem;

		public DenyPutOnGroundArgs(AbstractCharacter puttingChar, AbstractItem manipulatedItem, IPoint4D point)
			: base(DenyResult.Allow, puttingChar, manipulatedItem, point) {
			this.puttingChar = puttingChar;
			this.manipulatedItem = manipulatedItem;
		}

		public IPoint4D Point {
			get {
				return (IPoint4D) argv[3];
			}
			set {
				argv[3] = value;
			}
		}
	}

	public class ItemOnItemArgs : ScriptArgs {
		public readonly AbstractCharacter puttingChar;
		public readonly AbstractItem manipulatedItem;
		public readonly AbstractItem waitingItem;

		public ItemOnItemArgs(AbstractCharacter puttingChar, AbstractItem manipulatedItem, AbstractItem waitingItem)
			: base(puttingChar, manipulatedItem, waitingItem) {
			this.puttingChar = puttingChar;
			this.manipulatedItem = manipulatedItem;
			this.waitingItem = waitingItem;
		}
	}


	public class ItemOnCharArgs : ScriptArgs {
		public readonly AbstractCharacter puttingChar;
		public readonly AbstractCharacter cont;
		public readonly AbstractItem manipulatedItem;

		public ItemOnCharArgs(AbstractCharacter puttingChar, AbstractItem manipulatedItem, AbstractCharacter cont)
			: base(puttingChar, manipulatedItem, cont) {
			this.puttingChar = puttingChar;
			this.cont = cont;
			this.manipulatedItem = manipulatedItem;
		}
	}

	public class DenyEquipArgs : DenyTriggerArgs {
		public readonly AbstractCharacter puttingChar;
		public readonly AbstractCharacter cont;
		public readonly AbstractItem manipulatedItem;
		public readonly byte layer;

		public DenyEquipArgs(AbstractCharacter puttingChar, AbstractItem manipulatedItem, AbstractCharacter cont, byte layer)
			: base(DenyResult.Allow, puttingChar, manipulatedItem, cont, layer) {

			this.puttingChar = puttingChar;
			this.cont = cont;
			this.manipulatedItem = manipulatedItem;
			this.layer = layer;
		}
	}
}