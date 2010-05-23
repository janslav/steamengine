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
	internal class UIDArray<T> : IEnumerable<T> where T : ObjectWithUid {
		private T[] array = new T[minimalLength];
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
				return this.highestElement;
			}
		}

		internal int Count {
			get {
				return this.count;
			}
		}

		internal void Clear() {
			this.freeSlots.Clear();
			this.freeFakeSlots.Clear();
			this.fakeSlots.Clear();
			this.highestElement = 0;
			this.count = 0;
			this.array = new T[this.array.Length];
			this.loadingFinished = false;
		}

		internal void AddLoaded(T o, int loadedUid) {
			int index = loadedUid - uidOffset;
			if (this.loadingFinished) {
				throw new SEException("Add(object,index) disabled after LoadingFinished");
			}
			if (index < 0) {
				throw new SEException("Object with non-positive UID " + index + " found");
			}
			if (index >= this.array.Length) { //index is too high, make the array bigger!
				this.ResizeArray(index);
			}
			if (this.array[index] != null) {
				throw new SEException("Two objects attempted to take the same UID");
			}
			if (index > this.highestElement) {
				this.highestElement = index;
			}
			this.array[index] = o;
			this.count++;
		}

		internal void Add(T o) {
			if (this.freeSlots.Count == 0) {	//no indexes in the freeSlots queue
				this.FillFreeSlotQueue();
			}
			int index = this.freeSlots.Dequeue();
			int uid = index + uidOffset;
			if (uid >= (highestUid - this.fakeSlots.Count)) {
				throw new SEException("We're out of UIDs. This is baaaad.");
			}
			o.Uid = uid;
			this.array[index] = o;
			this.count++;

			if (uid > this.highestElement) {
				this.highestElement = uid;
			}
		}

		private void FillFreeSlotQueue() {
			this.freeSlots.Clear();

			int counter = 0;
			for (int i = 0, n = this.array.Length; i < n; i++) {
				if (this.array[i] == null) {
					this.freeSlots.Enqueue(i);
					counter++;
					if (counter >= queueMaxCount) {
						break;
					}
				}
			}
			if (counter == 0) {
				this.ResizeArray();
				this.FillFreeSlotQueue();//try again
			}
		}

		private void ResizeArray() {
			this.ResizeImpl(this.array.Length * 2);
		}

		private void ResizeArray(int newHighestElement) {
			int newSize = Math.Max(newHighestElement + minimalLength, this.array.Length * 2);
			this.ResizeImpl(newSize);
		}

		private void ResizeImpl(int newSize) {
			T[] temp = new T[newSize];
			Array.Copy(this.array, temp, this.array.Length);
			this.array = temp;
		}

		internal void LoadingFinished() {
			this.loadingFinished = true;
		}

		internal T Get(int uid) { //may return a null object
			int index = uid - uidOffset;
			if (index < this.array.Length && index >= 0) {
				return this.array[index];
			}
			return default(T);
		}

		internal void RemoveAt(int uid) {
			int index = uid - uidOffset;
			if (!Object.Equals(this.array[index], default(T))) { //only add to queue if not already null
				this.array[index] = default(T);
				if (index == this.highestElement) {
					this.highestElement--;
				} else {
					this.freeSlots.Enqueue(index);
				}
				this.count--;
			}
		}

		internal void ReIndexAll() {
			T[] origArray = this.array;
			int n = origArray.Length;
			T[] newArray = new T[n];

			for (int i = 0, newI = 0; i < n; i++) {
				T elem = origArray[i];
				if (!Object.Equals(elem, default(T))) {
					newArray[newI] = elem;
					elem.Uid = newI + uidOffset;
					newI++;
				}
			}
		}

		internal int GetFakeUid() {
			if (this.freeFakeSlots.Count == 0) {	//no indexes in the freeSlots queue
				this.FillFreeFakeSlotQueue();
			}
			int index = this.freeFakeSlots.Dequeue();
			this.fakeSlots[index] = true;
			return highestUid - index;
		}

		private void FillFreeFakeSlotQueue() {
			this.freeFakeSlots.Clear();

			int counter = 0;
			for (int i = 0, n = this.fakeSlots.Capacity; i < n; i++) {
				if (i >= this.fakeSlots.Count) {
					this.fakeSlots.Add(false);
				}
				if (!this.fakeSlots[i]) {
					this.freeFakeSlots.Enqueue(i);
					counter++;
					if (counter >= queueMaxCount) {
						break;
					}
				}
			}
			if (counter == 0) {
				this.fakeSlots.Capacity = this.fakeSlots.Capacity * 2;
				this.FillFreeFakeSlotQueue();//try again
			}
		}

		internal void DisposeFakeUid(int uid) {
			int index = highestUid - uid;
			if (index < this.fakeSlots.Count) {
				if (this.fakeSlots[index]) {
					this.fakeSlots[index] = false;
					this.freeFakeSlots.Enqueue(index);
				}
			}
		}

		public IEnumerator<T> GetEnumerator() {
			return new Enumerator(this);
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return new Enumerator(this);
		}

		public IEnumerable<T> AsEnumerable() {
			return new Enumerator(this);
		}

		private class Enumerator : IEnumerator<T>, IEnumerable<T> {
			private T current;
			private int currIdx;
			private UIDArray<T> source;

			internal Enumerator(UIDArray<T> source) {
				this.source = source;
				this.Reset();
			}

			public void Reset() {
				this.currIdx = 1;
				this.current = default(T);
			}

			public T Current {
				get {
					return this.current;
				}
			}

			public void Dispose() {
			}

			object IEnumerator.Current {
				get {
					return this.current;
				}
			}

			public bool MoveNext() {
				while (this.currIdx <= this.source.HighestElement) {
					this.current = this.source.Get(this.currIdx);
					this.currIdx++;
					if (!Object.Equals(this.current, default(T))) {
						return true;
					}
				}
				return false;
			}

			#region IEnumerable<T> Members

			IEnumerator<T> IEnumerable<T>.GetEnumerator() {
				return this;
			}

			IEnumerator IEnumerable.GetEnumerator() {
				return this;
			}

			#endregion
		}
	}
}