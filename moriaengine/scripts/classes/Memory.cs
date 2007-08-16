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
using SteamEngine;
using SteamEngine.Common;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {
	
	[SaveableClass]
	public class Memory : PluginHolder {
		internal Memory prev;
		internal Memory next;
		internal MemoryCollection cont;
		
		private int flags = 0;
		private Thing link = null;
		private bool isDeleted = false;
		private MemoryDef def = null;

		private static List<TriggerGroup> registeredTGs = new List<TriggerGroup>();
		
		[LoadingInitializer]
		public Memory() {
		}

		public Memory(MemoryDef def) {
			this.def = def;
			Globals.lastNew = this;
		}

		//copies with the entire collection
		internal Memory(Memory copyFrom)
				: this(copyFrom.def) {
			if (copyFrom.isDeleted) {
				throw new Exception("Cannot copy a deleted Memory instance.");
			}
			flags = copyFrom.flags;
			cont = copyFrom.cont;
			if (copyFrom.next != null) {
				next = copyFrom.next.Dupe();
				next.prev = this;
			}
		}

		internal virtual Memory Dupe() {
			Sanity.IfTrueThrow((this.GetType() != typeof(Memory)), "Dupe() needs to be overriden by subclasses");
			return new Memory(this);
		}
		
		public MemoryDef Def { get {
			return def;
		} }

		[SaveableData]
		public int Flags { 
			get {
				ThrowIfDeleted();
				return flags;
			}
			set {
				ThrowIfDeleted();
				flags = value;
			}
		}
		public int Color { //another name for Flags - for sphere compatibility
			get {
				ThrowIfDeleted();
				return flags;
			}
			set {
				ThrowIfDeleted();
				flags = value;
			}
		}
		
		[SaveableData]
		public Thing Link {
			get {
				ThrowIfDeleted();
				return link;
			}
			set {
				ThrowIfDeleted();
				link = value;
			}
		}
		
		public Character Cont {
			get {
				ThrowIfDeleted();
				if (cont != null) {
					return cont.cont;
				}
				return null;
			}
		}
		
		public override bool IsDeleted {
			get {
				return isDeleted;
			}
		}
		
		public void Remove() {
			if (!isDeleted) {
				BeingDeleted();
			}
		}
		
		protected override void BeingDeleted() {
			if (Cont != null) {
				Cont.RemoveMemory(this);
			}
			TryTrigger(TriggerKey.destroy, null);
			isDeleted = true;
			base.BeingDeleted();
		}
		
		[Save]
		public override void Save(SaveStream output) {
			ThrowIfDeleted();

			Character c = Cont;
			if (c != null) {
				output.WriteValue("cont", c);
			}
			base.Save(output);
		}
		
		[LoadLine]//can't be directly on LoadLine because it is not public
		public void Call_LoadLine(string filename, int line, string prop, string value) {
			LoadLine(filename, line, prop, value);
		}


		protected override void LoadLine(string filename, int line, string prop, string value) {
			switch(prop) {
				case "cont":
					ObjectSaver.Load(value, new LoadObject(LoadCont_Delayed), filename, line);
					break;
				default:
					base.LoadLine(filename, line, prop, value);
					break;
			}
		}
		
		public void LoadCont_Delayed(object resolvedObj, string filename, int line) {
			if (cont == null) {
				Character ch = resolvedObj as Character;
				if (ch != null) {
					ch.AddLoadedMemory(this);
					return;
				}
				cont=null;
				Logger.WriteWarning("The saved cont object ("+resolvedObj+") for item '"+this.ToString()+"' is not a valid Character. Removing.");
				Remove();
			}
		}

		public virtual void On_Equip(Character ch) {
		}
		
		public virtual void On_UnEquip(Character ch) {
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
	}
	
	public class MemoryCollection : IEnumerable {
		internal readonly Character cont;
		internal Memory first;
		internal ushort count;
		
		internal MemoryCollection(Character cont) {
			this.cont = cont;
		}
		
		internal MemoryCollection(MemoryCollection copyFrom) {
			if (copyFrom.first != null) {
				//recursively copies all the memories
				first = copyFrom.first.Dupe();
			}
			this.cont = copyFrom.cont;
			this.count = copyFrom.count;
		}
		
		internal void Add(Memory m) {
			Memory next=first;
			first=m;
			m.prev=null;
			m.next=next;
			if (next!=null) {
				next.prev=m;
			}
			m.cont=this;
			count++;
		}
		
		internal bool Remove(Memory m) {
			if (m.cont == this) {
				if (first == m) {
					first = m.next;
				} else {
					m.prev.next = m.next;
				}
				if (m.next != null) {
					m.next.prev=m.prev;
				}
				m.prev=null;
				m.next=null;
				count--;
				return true;
			}
			return false;
		}
		
		internal Memory FindByDef(MemoryDef def) {
			Memory m = first;
			while (m != null) {
				if (m.Def == def) {
					return m;
				}
				m = m.next;
			}
			return null;
		}
		
		internal Memory FindByLink(Thing link) {
			Memory m = first;
			while (m != null) {
				if (m.Link == link) {
					return m;
				}
				m = m.next;
			}
			return null;
		}
		
		internal Memory FindByFlag(int flag) {
			Memory m = first;
			while (m != null) {
				if ((m.Flags&flag)>0) {
					return m;
				}
				m = m.next;
			}
			return null;
		}
		
		internal void SaveMemories() {
			Memory m = first;
			while (m != null) {
				ObjectSaver.Save(m);
				m = m.next;
			}
		}

		internal Memory this[int index] { 
			get {
				if ((index >= count) || (index < 0)) {
					return null;
				}
				Memory m = first;
				int counter = 0;
				while (m != null) {
					if (index == counter) {
						return m;
					}
					m = m.next;
					counter++;
				}
				return null;
			}
		}
		
		internal void Empty() {
			Memory m = first;
			while (m != null) {
				Memory next = m.next;
				m.Remove();
				m = next;
			}
		}
		
		internal void BeingDeleted() {
			Memory m = first;
			while (m != null) {
				Memory next = m.next;
				m.Remove();
				m = next;
			}
		}
		
		public IEnumerator GetEnumerator() {
			return new MemoryCollectionEnumerator(this);
		}
		
		private class MemoryCollectionEnumerator : IEnumerator {
			MemoryCollection cont;
			Memory current;
			Memory next;//this is because of the possibility 
			//that the current will be removed from the container during the enumeration
			public MemoryCollectionEnumerator(MemoryCollection c) {
				cont = c;
				current = null;
				next = cont.first;
			}
   	 	
			public void Reset() {
				current = null;
				next = cont.first;
			}
   	 	
			public bool MoveNext() {
				current=next;
				if (current==null) {
					return false;
				}
				next=current.next;
				return true;
			}
   	 	
			public Memory Current { 
				get {
					return current;
				} 
			}
   	 	
			object IEnumerator.Current { 
				get {
					return current;
				} 
			}
		}
	}
}