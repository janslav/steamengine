/*
	This program is free software; you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation; either version 2 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See then
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program; if not, write to the Free Software
	Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
	Or visit http://www.gnu.org/copyleft/gpl.html
*/

using System.Collections;
using System.Collections.Generic;
using SteamEngine.Common;

namespace SteamEngine.UoData
{
	//for storing of MultiItemComponents in sectors
	internal class MultiComponentLinkedList : IEnumerable<MultiItemComponent> {
		private MultiItemComponent firstMultiComponent;
		private ushort count;

		internal void Add(MultiItemComponent multiComponent) {
			Sanity.IfTrueThrow((multiComponent.prevInList != null || multiComponent.nextInList != null),
				"'" + multiComponent + "' being added into a MultiComponentList while being in another cont already");
			var next = this.firstMultiComponent;
			this.firstMultiComponent = multiComponent;
			multiComponent.prevInList = null;
			multiComponent.nextInList = next;
			if (next != null) {
				next.prevInList = multiComponent;
			}
			multiComponent.collection = this;
			this.count++;
		}

		internal bool Remove(MultiItemComponent multiComponent) {
			if (multiComponent.collection == this) {
				if (this.firstMultiComponent == multiComponent) {
					this.firstMultiComponent = multiComponent.nextInList;
				} else {
					multiComponent.prevInList.nextInList = multiComponent.nextInList;
				}
				if (multiComponent.nextInList != null) {
					multiComponent.nextInList.prevInList = multiComponent.prevInList;
				}
				multiComponent.prevInList = null;
				multiComponent.nextInList = null;
				this.count--;
				multiComponent.collection = null;
				return true;
			}
			return false;
		}

		internal MultiItemComponent Find(int x, int y, int z, int id) {
			var mic = this.firstMultiComponent;
			while (mic != null) {
				if ((mic.X == x) && (mic.Y == y) && (mic.Z == z) && (mic.Id == id)) {
					return mic;
				}
				mic = mic.nextInList;
			}
			return null;
		}

		//internal MultiItemComponent this[int index] {
		//    get {
		//        if ((index >= this.count) || (index < 0)) {
		//            return null;
		//        }
		//        MultiItemComponent i = this.firstMultiComponent;
		//        int counter = 0;
		//        while (i != null) {
		//            if (index == counter) {
		//                return i;
		//            }
		//            i = i.nextInList;
		//            counter++;
		//        }
		//        return null;
		//    }
		//}

		public IEnumerator<MultiItemComponent> GetEnumerator() {
			return new MultiComponentListEnumerator(this);
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return new MultiComponentListEnumerator(this);
		}

		private class MultiComponentListEnumerator : IEnumerator<MultiItemComponent> {
			private readonly MultiComponentLinkedList cont;
			private MultiItemComponent current;
			private MultiItemComponent next;//this is because of the possibility 
			//that the current will be removed from the container during the enumeration

			public MultiComponentListEnumerator(MultiComponentLinkedList c) {
				this.cont = c;
				this.next = this.cont.firstMultiComponent;
			}

			public void Reset() {
				this.current = null;
				this.next = this.cont.firstMultiComponent;
			}

			public bool MoveNext() {
				this.current = this.next;
				if (this.current == null) {
					return false;
				}
				this.next = this.current.nextInList;
				return true;
			}

			public MultiItemComponent Current => this.current;

			object IEnumerator.Current => this.current;

			public void Dispose() {
			}
		}
	}
}