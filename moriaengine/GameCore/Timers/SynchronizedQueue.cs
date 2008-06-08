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
using System.Threading;

namespace SteamEngine.Timers {

	//a thread-safe queue class, which uses sort of "circular" algorhitm, 
	[System.Diagnostics.DebuggerDisplay("Count = {Count}")]
	public class SynchronizedQueue<T> {
		private T[] array;
		private int headindex;
		private int tailindex;
		private int count;

		private readonly object root = new object();

		public SynchronizedQueue() {
			this.array = new T[32];
			this.headindex = 0;
			this.tailindex = 0;
		}

		public int Count {
			get {
				lock (this.root) {
					return this.count;
				}
			}
		}

		public T Peek() {
			lock (this.root) {
				if (this.count == 0) {
					throw new Exception("Attempted to get object from empty queue... This should not happen.");
				}
				return this.array[headindex];
			}
		}

		public T Dequeue() {
			lock (this.root) {
				if (this.count == 0) {
					throw new Exception("Attempted to get object from empty queue... This should not happen.");
				}
				T t = this.array[headindex];
				this.array[headindex] = default(T);
				this.headindex = ((this.headindex + 1) % this.array.Length);
				this.count--;
				return t;
			}
		}

		public void Enqueue(T obj) {
			lock (this.root) {
				if (this.count == this.array.Length) {
					this.Grow();
				}
				this.array[this.tailindex] = obj;
				this.tailindex = ((this.tailindex + 1) % this.array.Length);
				this.count++;
			}
		}

		public void Clear() {
			lock (this.root) {
				Array.Clear(this.array, 0, this.array.Length);
				this.headindex = 0;
				this.tailindex = 0;
				this.count = 0;
			}
		}

		private void Grow() {
			T[] newArray = new T[this.array.Length * 2 + 1];
			if (this.count > 0) {
				if (this.headindex < this.tailindex) {
					Array.Copy(this.array, this.headindex, newArray, 0, this.count);
				} else {
					Array.Copy(this.array, this.headindex, newArray, 0, (this.array.Length - this.headindex));
					Array.Copy(this.array, 0, newArray, (this.array.Length - this.headindex), this.tailindex);
				}
			}
			this.array = newArray;
			this.headindex = 0;
			this.tailindex = this.count;
		}
	}
}