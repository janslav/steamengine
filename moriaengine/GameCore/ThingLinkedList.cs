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

namespace SteamEngine {

	internal class ThingLinkedList : IEnumerable<Thing> {
		private readonly object cont;
		internal Thing firstThing;
		internal ushort count;

		internal ThingLinkedList(object cont) {
			this.cont = cont;
		}

		internal Thing ContAsThing {
			get {
				return cont as Thing;
			}
		}

		internal void Add(Thing thing) {
			Sanity.IfTrueThrow((thing.prevInList != null || thing.nextInList != null),
				"'" + thing + "' being added into a ThingLinkedList while being in another cont already");
			Thing next = firstThing;
			firstThing = thing;
			thing.prevInList = null;
			thing.nextInList = next;
			if (next != null) {
				next.prevInList = thing;
			}
			thing.contOrTLL = this;
			count++;
		}

		internal bool Remove(Thing thing) {
			if (thing.contOrTLL == this) {
				if (firstThing == thing) {
					firstThing = thing.nextInList;
				} else {
					thing.prevInList.nextInList = thing.nextInList;
				}
				if (thing.nextInList != null) {
					thing.nextInList.prevInList = thing.prevInList;
				}
				thing.prevInList = null;
				thing.nextInList = null;
				count--;
				return true;
			}
			return false;
		}

		internal Thing FindByZ(byte z) {//usd by findlayer
			Thing i = firstThing;
			while (i != null) {
				if (i.Z == z) {
					return i;
				}
				i = i.nextInList;
			}
			return null;
		}

		internal Thing this[int index] {
			get {
				if ((index >= count) || (index < 0)) {
					return null;
				}
				Thing i = firstThing;
				int counter = 0;
				while (i != null) {
					if (index == counter) {
						return i;
					}
					i = i.nextInList;
					counter++;
				}
				return null;
			}
		}

		internal void Empty() {
			Thing i = firstThing;
			while (i != null) {
				Thing next = i.nextInList;
				i.InternalDelete();
				i = next;
			}
		}

		internal void BeingDeleted() {
			Thing i = firstThing;
			while (i != null) {
				Thing next = i.nextInList;
				i.InternalDeleteNoRFV();
				i = next;
			}
		}

		IEnumerator<Thing> IEnumerable<Thing>.GetEnumerator() {
			return new ThingLinkedListEnumerator(this);
		}

		public IEnumerator<AbstractItem> GetItemEnumerator() {
			return new ThingLinkedListEnumerator(this);
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return new ThingLinkedListEnumerator(this);
		}

		private class ThingLinkedListEnumerator : IEnumerator<Thing>, IEnumerator<AbstractItem> {
			ThingLinkedList cont;
			Thing current;
			Thing next;//this is because of the possibility 
			//that the current will be removed from the container during the enumeration
			public ThingLinkedListEnumerator(ThingLinkedList c) {
				cont = c;
				current = null;
				next = cont.firstThing;
			}

			public void Reset() {
				current = null;
				next = cont.firstThing;
			}

			public bool MoveNext() {
				current = next;
				if (current == null) {
					return false;
				}
				next = current.nextInList;
				return true;
			}

			Thing IEnumerator<Thing>.Current {
				get {
					return current;
				}
			}

			object IEnumerator.Current {
				get {
					return current;
				}
			}

			AbstractItem IEnumerator<AbstractItem>.Current {
				get {
					return (AbstractItem) current;
				}
			}

			public void Dispose() {
			}
		}
	}
}