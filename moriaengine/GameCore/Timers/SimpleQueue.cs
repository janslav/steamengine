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
	
	//a very simple queue class, which uses sort of "circular" algorhitm, 
	[System.Diagnostics.DebuggerDisplay("Count = {Count}")]
	internal class SimpleQueue<T> {
		private T[] array;
		private int headindex;
		private int tailindex;
		private int count;
	
		internal SimpleQueue() {
			array = new T[32];
			headindex = 0;
			tailindex = 0;
		}
	
		internal int Count { get {
			return count;
		} }
	
		internal T Peek() {
			if (count == 0) {
				throw new Exception("Attempted to get object from empty queue... This should not happen.");
			}
			return array[headindex];
		}
	
		internal T Dequeue() {
			if (count == 0) {
				throw new Exception("Attempted to get object from empty queue... This should not happen.");
			}
			T t = array[headindex];
			array[headindex] = default(T);
			headindex = ((headindex + 1) % array.Length);
			count = (count - 1);
			return t;   
		}
	
		internal void Enqueue(T obj) {
			if (count == array.Length) {
				Grow();
			}
			array[tailindex] = obj;
			tailindex = ((tailindex + 1) % array.Length);
			count = (count + 1);
		}
	
		internal void Clear() {
			Array.Clear(array, 0, array.Length);
			headindex = 0;
			tailindex = 0;
			count = 0;
		}
	
		private void Grow() {
			T[] newArray = new T[array.Length*2+1];
			if (count > 0) {
				if (headindex < tailindex) {
					Array.Copy(array, headindex, newArray , 0, count);
				}
				else {
					Array.Copy(array, headindex, newArray , 0, (array.Length - headindex));
					Array.Copy(array, 0, newArray , (array.Length - headindex), tailindex);
				}
			}
			array = newArray ;
			headindex = 0;
			tailindex = count;
		}
	}
}