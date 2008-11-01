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
using SteamEngine.Networking;
using SteamEngine.Communication.TCP;

namespace SteamEngine {
	public interface ICorpseEquipInfo {
		uint FlaggedUid { get; }
		byte Layer { get; }
		ushort Color { get; }
		ushort Model { get; }
	}

	public abstract partial class AbstractItem : Thing, ICorpseEquipInfo {
		private uint amount;
		private string name;
		//Important: cont should only be changed through calls to BeingDroppedFromContainer or BeingPutInContainer,
		//and coords of an item inside a container should only be changed through MoveInsideContainer

		internal ItemSyncQueue.SyncFlags syncFlags;

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
			}
			set {
				type = value;
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

		public AbstractItem(ThingDef myDef)
			: base(myDef) {
			instances++;
			this.name = null;
			this.type = ((AbstractItemDef) myDef).Type;
			this.flags = 0;
			this.amount = 1;

			//MoveInsideContainer((ushort) Globals.dice.Next(20,100),(ushort) Globals.dice.Next(20,100));
			//cont.InternalAddItem(this);//also resends
			Globals.lastNewItem = this;
		}

		public AbstractItem(AbstractItem copyFrom)
			: base(copyFrom) { //copying constuctor
			instances++;
			name = copyFrom.name;
			flags = copyFrom.flags;
			amount = copyFrom.amount;
			Globals.lastNewItem = this;
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
				return (Count == 0);
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
				ItemDispidInfo idi = ItemDispidInfo.Get(this.Model);
				if (idi == null) {
					return 1;
				}
				return idi.height;
			}
		}

		public uint Amount {
			get {
				return amount;
			}
			set {
				if (value != amount) {
					this.InvalidateProperties();
					ItemSyncQueue.AboutToChange(this);
					Thing c = this.Cont;
					if (c != null) {
						c.AdjustWeight(this.def.Weight * (value - amount));
					}
					amount = value;
				}
			}
		}

		public ushort ShortAmount {
			get {
				return (ushort) Math.Min(ushort.MaxValue, this.amount);
			}
		}

		public virtual bool IsTwoHanded {
			get {
				return false;
			}
		}

		public sealed override uint FlaggedUid {
			get {
				return (uint) (Uid | 0x40000000);
			}
		}

		public override bool Flag_Disconnected {
			get {
				return ((flags & 0x01) == 0x01);
			}
			set {
				if (this.Flag_Disconnected != value) {
					if (value) {
						OpenedContainers.SetContainerClosed(this);
						this.RemoveFromView();
					}
					ItemSyncQueue.AboutToChange(this);
					flags = (byte) (value ? (flags | 0x01) : (flags & ~0x01));
					if (value) {
						GetMap().Disconnected(this);
					} else {
						GetMap().Reconnected(this);
					}
				}
			}
		}

		public bool IsInVisibleLayer {
			get {
				if (point4d.x == 7000) {
					return (point4d.z < 26);
				}
				return true;
			}
		}

		public virtual byte Layer {
			get {
				return (int) LayerNames.None;
			}
		}

		public virtual ushort Gump {
			get {
				return 0;
			}
		}

		//commands:
		public override void Resend() {
			ItemSyncQueue.Resend(this);
		}

		public override AbstractItem FindCont(int index) {
			ThingLinkedList tll = contentsOrComponents as ThingLinkedList;
			if (tll == null) {
				return null;
			} else {
				return (AbstractItem) tll[index];
			}
		}

		public void OpenTo(AbstractCharacter viewer) {
			GameState viewerState = viewer.GameState;
			this.OpenTo(viewer, viewerState, viewerState.Conn);
		}

		public void OpenTo(AbstractCharacter viewer, GameState viewerState, TCPConnection<GameState> viewerConn) {
			if (this.IsContainer) {
				this.ThrowIfDeleted();
				viewer.ThrowIfDeleted();

				//send container
				OpenedContainers.SetContainerOpened(viewer, this);
				DrawContainerOutPacket packet = Pool<DrawContainerOutPacket>.Acquire();
				packet.PrepareContainer(this.FlaggedUid, this.Gump);
				viewerConn.SendSinglePacket(packet);

				PacketSequences.SendContainerContentsWithPropertiesTo(viewer, viewerState, viewerConn, this);

				this.Trigger_ContainerOpen(viewer, viewerState, viewerConn);
			} else {
				throw new InvalidOperationException("The item (" + this + ") is not a container");
			}
		}

		private void Trigger_ContainerOpen(AbstractCharacter viewer, GameState viewerState, TCPConnection<GameState> viewerConn) {
			ScriptArgs sa = new ScriptArgs(viewer, viewerState, viewerConn);
			this.TryTrigger(TriggerKey.containerOpen, sa);
			try {
				this.On_ContainerOpen(viewer, viewerState, viewerConn);
			} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
		}

		public virtual void On_ContainerOpen(AbstractCharacter viewer, GameState viewerState, TCPConnection<GameState> viewerConn) {
		}

		[Obsolete("Use the alternative from Networking namespace", false)]
		public virtual void On_ContainerOpen(GameConn viewerConn) {

		}

		public virtual ItemOnGroundUpdater GetOnGroundUpdater() {
			ItemOnGroundUpdater iogu = ItemOnGroundUpdater.GetFromCache(this);
			if (iogu == null) {
				iogu = new ItemOnGroundUpdater(this);
			}
			return iogu;
		}

		internal override void Trigger_Destroy() {
			ThingLinkedList tll = contentsOrComponents as ThingLinkedList;
			if (tll != null) {
				tll.BeingDeleted();
			}
			base.Trigger_Destroy();
			this.MakeLimbo();
			instances--;
		}

		public void EmptyCont() {
			ThingLinkedList tll = contentsOrComponents as ThingLinkedList;
			if (tll != null) {
				tll.Empty();
			}
		}

		public override string Name {
			get {
				if (name == null) {
					return Amount > 1 ? TypeDef.PluralName : TypeDef.SingularName;
				} else {
					return name;
				}
			}
			set {
				this.InvalidateProperties();
				if (!string.IsNullOrEmpty(value)) {
					name = String.Intern(value);
				} else {
					name = null;
				}
			}
		}

		public override void On_Create() {
			AbstractItemDef aidef = (AbstractItemDef) def;
			AbstractItemDef dupe = aidef.DupeItem;
			if (dupe != null) {
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
			for (int i = 0, n = registeredTGs.Count; i < n; i++) {
				TriggerGroup tg = (TriggerGroup) registeredTGs[i];
				tg.Run(this, td, sa);
			}
			base.TryTrigger(td, sa);
			if (type != null) {
				type.Run(this, td, sa);
			}
		}

		public override void TryTrigger(TriggerKey td, ScriptArgs sa) {
			ThrowIfDeleted();
			for (int i = 0, n = registeredTGs.Count; i < n; i++) {
				TriggerGroup tg = (TriggerGroup) registeredTGs[i];
				tg.TryRun(this, td, sa);
			}
			base.TryTrigger(td, sa);
			if (type != null) {
				type.TryRun(this, td, sa);
			}
		}

		public override bool CancellableTrigger(TriggerKey td, ScriptArgs sa) {
			ThrowIfDeleted();
			for (int i = 0, n = registeredTGs.Count; i < n; i++) {
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
				if (type != null) {
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
			for (int i = 0, n = registeredTGs.Count; i < n; i++) {
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
				if (type != null) {
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

		internal override sealed bool Trigger_SpecificClick(AbstractCharacter clickingChar, ScriptArgs sa) {
			//helper method for Trigger_Click
			bool cancel = false;
			cancel = clickingChar.TryCancellableTrigger(TriggerKey.itemClick, sa);
			if (!cancel) {
				clickingChar.act = this;
				cancel = clickingChar.On_ItemClick(this);
			}
			return cancel;
		}

		public void Trigger_Step(AbstractCharacter steppingChar, int repeated) {
			ThrowIfDeleted();
			bool cancel = false;
			bool rep = (repeated != 0);
			ScriptArgs sa = new ScriptArgs(steppingChar, this, repeated);
			steppingChar.act = this;
			cancel = steppingChar.TryCancellableTrigger(TriggerKey.itemStep, sa);
			if (!cancel) {
				//@item/charStep on src did not return 1
				cancel = steppingChar.On_ItemStep(this, rep);//sends true if repeated=1
				if (!cancel) {
					cancel = TryCancellableTrigger(TriggerKey.step, sa);
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
			if ((type != null) && (type != def.Type)) {
				output.WriteLine("type=" + type.Defname);
			}
		}

		public override void LoadLine(string filename, int line, string valueName, string valueString) {
			switch (valueName) {
				case "cont":
					ObjectSaver.Load(valueString, new LoadObject(LoadCont_Delayed), filename, line);
					break;
				case "p":
					base.LoadLine(filename, line, valueName, valueString);//loads the position
					if (this.IsInContainer) {
						this.MakePositionInContLegal();
					}
					break;

				case "name":
					Match ma = TagMath.stringRE.Match(valueString);
					if (ma.Success) {
						name = String.Intern(ma.Groups["value"].Value);
					} else {
						name = String.Intern(valueString);
					}
					break;
				case "amount":
					amount = TagMath.ParseUInt16(valueString);
					break;
				case "flags":
					flags = TagMath.ParseByte(valueString);
					break;
				case "type":
					type = TriggerGroup.Get(valueString);
					break;
				default:
					base.LoadLine(filename, line, valueName, valueString);
					break;
			}
		}


		public override sealed bool IsItem {
			get {
				return true;
			}
		}

		public override sealed bool IsEquipped {
			get {
				return (this.Cont is AbstractCharacter);
			} 
		}

		public override sealed bool IsOnGround {
			get {
				return (Cont == null);
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
				throw new NotSupportedException("You can't set Direction to " + this.GetType());
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

		public override sealed IEnumerator<AbstractItem> GetEnumerator() {
			this.ThrowIfDeleted();
			ThingLinkedList tll = contentsOrComponents as ThingLinkedList;
			if (tll == null) {
				return EmptyReadOnlyGenericCollection<AbstractItem>.instance;
			} else {
				return tll.GetItemEnumerator();
			}
		}

		public override void InvalidateProperties() {
			ItemSyncQueue.PropertiesChanged(this);
			base.InvalidateProperties();
		}
	}
}
