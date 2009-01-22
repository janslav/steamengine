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
using SteamEngine.Timers; //SimpleQueue :D

namespace SteamEngine {
	internal class UIDArray<ElementType> : IEnumerable<ElementType> where ElementType : ObjectWithUid {
		private ElementType[] array;
		private SimpleQueue<int> freeSlots = new SimpleQueue<int>();
		private int highestElement;
		private int count;
		private bool loadingFinished;

		private List<bool> fakeSlots = new List<bool>(minimalLength);
		private SimpleQueue<int> freeFakeSlots = new SimpleQueue<int>();

		private const int minimalLength = 1000;
		private const int uidOffset = 1; //low uids (particularly 0) have been found to cause problems in client.
		private const int queueMaxCount = 1000;

		private const int highestUid = 0x00ffffff - 1;

		internal int HighestElement {
			get {
				return highestElement;
			}
		}

		internal int Count {
			get {
				return count;
			}
		}

		internal UIDArray() {
			highestElement = 0;	//we don't have any elements yet
			count = 0;
			loadingFinished = false;
			array = new ElementType[minimalLength];
		}

		internal void Clear() {
			freeSlots.Clear();
			freeFakeSlots.Clear();
			fakeSlots.Clear();
			highestElement = 0;
			count = 0;
			array = new ElementType[array.Length];
			loadingFinished = false;
		}

		internal void AddLoaded(ElementType o, int loadedUid) {
			int index = loadedUid - uidOffset;
			if (loadingFinished) {
				throw new ApplicationException("Add(object,index) disabled after LoadingFinished");
			}
			if (index < 0) {
				throw new IndexOutOfRangeException("Object with non-positive UID " + index + " found");
			}
			if (index >= array.Length) { //index is too high, make the array bigger!
				ResizeArray(index);
			}
			if (array[index] != null) {
				throw new IndexOutOfRangeException("Two objects attempted to take the same UID");
			}
			if (index > highestElement) {
				highestElement = index;
			}
			array[index] = o;
			count++;
		}

		internal void Add(ElementType o) {
			if (freeSlots.Count == 0) {	//no indexes in the freeSlots queue
				FillFreeSlotQueue();
			}
			int index = freeSlots.Dequeue();
			int uid = index + uidOffset;
			if (uid >= (highestUid - fakeSlots.Count)) {
				throw new FatalException("We're out of UIDs. This is baaaad.");
			}
			o.Uid = uid;
			array[index] = o;
			count++;

			if (uid > highestElement) {
				highestElement = uid;
			}
		}

		private void FillFreeSlotQueue() {
			freeSlots.Clear();

			int count = 0;
			for (int i = 0, n = array.Length; i < n; i++) {
				if (array[i] == null) {
					freeSlots.Enqueue(i);
					count++;
					if (count >= queueMaxCount) {
						break;
					}
				}
			}
			if (count == 0) {
				ResizeArray();
				FillFreeSlotQueue();//try again
			}
		}

		private void ResizeArray() {
			ResizeImpl(array.Length * 2);
		}

		private void ResizeArray(int highestElement) {
			int newSize = Math.Max(highestElement + minimalLength, array.Length * 2);
			ResizeImpl(newSize);
		}

		private void ResizeImpl(int newSize) {
			ElementType[] temp = new ElementType[newSize];
			Array.Copy(this.array, temp, array.Length);
			array = temp;
		}

		internal void LoadingFinished() {
			loadingFinished = true;
		}

		internal ElementType Get(int uid) { //may return a null object
			int index = uid - uidOffset;
			if (index < array.Length && index >= 0) {
				return array[index];
			}
			return default(ElementType);
		}

		internal void RemoveAt(int uid) {
			int index = uid - uidOffset;
			if (!Object.Equals(array[index], default(ElementType))) { //only add to queue if not already null
				array[index] = default(ElementType);
				if (index == highestElement) {
					highestElement--;
				} else {
					freeSlots.Enqueue(index);
				}
				count--;
			}
		}

		internal void ReIndexAll() {
			ElementType[] origArray = this.array;
			int n = origArray.Length;
			ElementType[] newArray = new ElementType[n];

			for (int i = 0, newI = 0; i < n; i++) {
				ElementType elem = origArray[i];
				if (!Object.Equals(elem, default(ElementType))) {
					newArray[newI] = elem;
					elem.Uid = newI + uidOffset;
					newI++;
				}
			}
		}

		internal int GetFakeUid() {
			if (freeFakeSlots.Count == 0) {	//no indexes in the freeSlots queue
				FillFreeFakeSlotQueue();
			}
			int index = freeFakeSlots.Dequeue();
			fakeSlots[index] = true;
			return highestUid - index;
		}

		private void FillFreeFakeSlotQueue() {
			freeFakeSlots.Clear();

			int count = 0;
			for (int i = 0, n = fakeSlots.Capacity; i < n; i++) {
				if (i >= fakeSlots.Count) {
					fakeSlots.Add(false);
				}
				if (!fakeSlots[i]) {
					freeFakeSlots.Enqueue(i);
					count++;
					if (count >= queueMaxCount) {
						break;
					}
				}
			}
			if (count == 0) {
				fakeSlots.Capacity = fakeSlots.Capacity * 2;
				FillFreeFakeSlotQueue();//try again
			}
		}

		internal void DisposeFakeUid(int uid) {
			int index = highestUid - uid;
			if (index < fakeSlots.Count) {
				if (fakeSlots[index]) {
					fakeSlots[index] = false;
					freeFakeSlots.Enqueue(index);
				}
			}
		}

		public IEnumerator<ElementType> GetEnumerator() {
			return new Enumerator(this);
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return new Enumerator(this);
		}


		private class Enumerator : IEnumerator<ElementType> {
			private ElementType current;
			private int currIdx;
			private UIDArray<ElementType> source;

			internal Enumerator(UIDArray<ElementType> source) {
				this.source = source;
				Reset();
			}

			public void Reset() {
				currIdx = 1;
				current = default(ElementType);
			}

			public ElementType Current {
				get { return current; }
			}

			public void Dispose() {
			}

			object IEnumerator.Current {
				get { return current; }
			}

			public bool MoveNext() {
				while (currIdx <= source.HighestElement) {
					current = source.Get(currIdx);
					currIdx++;
					if (!Object.Equals(current, default(ElementType))) {
						return true;
					}
				}
				return false;
			}
		}
	}
}