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

//**********************************************************
//* PriorityQueue                                          *
//* Copyright (c) Julian M Bucknall 2004                   *
//* All rights reserved.                                   *
//* This code can be used in your applications, providing  *
//*    that this copyright comment box remains as-is       *
//**********************************************************
//* .NET priority queue class (heap algorithm)             *
//**********************************************************

//the queue has been specialized for use with our Timer class
//the original file still most probably lies in the 3dParty directory

using System;
using System.Collections;

namespace SteamEngine.Timers {
	public class TimerPriorityQueue {
		private int count;
		private int capacity;
		private int version;
		private Timer[] heap;

		public TimerPriorityQueue() {
			capacity = 15;
			heap = new Timer[capacity];
		}
		
		public void Clear() {
			Array.Clear(heap, 0, heap.Length);
			count = 0;
		}

		public Timer Dequeue() {
			if (count == 0) {
				throw new InvalidOperationException();
			}
			Timer result = heap[0];
			count--;
			TrickleDown(0, heap[count]);
			heap[count] = null;
			version++;
			result.index = -1;
			return result;
		}
		
		public void Remove(Timer timer) {
			if (timer.index > -1) {
				RemoveAt(timer.index);
				timer.index = -1;
			}
		}
		
		private void RemoveAt(int i) {
			count--;
			TrickleDown(i, heap[count]);
			heap[count] = null;
			version++;
		}
		
		public Timer Peek() {
			if (count == 0) {
				throw new InvalidOperationException();
			}
			return heap[0];
		}

		public void Enqueue(Timer timer) {
			if (count == capacity) {
				Grow();
			}
			count++;
			BubbleUp(count - 1, timer);
			version++;
		}

		private void BubbleUp(int index, Timer timer) {
			int parent = GetParent(index);
			// note: (index > 0) means there is a parent
			while ((index > 0) && 
					(heap[parent].fireAt > timer.fireAt)) {
						//original line: (heap[parent].Priority.CompareTo(he.Priority) < 0)
						//is true when parent priority is lower than our current priority - in case of timers means it higher time
				Timer parentTimer = heap[parent];
				parentTimer.index = index;
				heap[index] = parentTimer;
				index = parent;
				parent = GetParent(index);
			}
			timer.index = index;
			heap[index] = timer;
		}

		private int GetLeftChild(int index) {
			return (index * 2) + 1;
		}

		private int GetParent(int index) {
			return (index - 1) / 2;
		}

		private void Grow() {
			capacity = (capacity * 2) + 1;
			Timer[] newHeap = new Timer[capacity];
			System.Array.Copy(heap, 0, newHeap, 0, count);
			heap = newHeap;
		}

		private void TrickleDown(int index, Timer timer) {
			int child = GetLeftChild(index);
			while (child < count) {
				if (((child + 1) < count) && 
						(heap[child].fireAt > heap[child + 1].fireAt)) {
					//orig. line: (heap[child].Priority.CompareTo(heap[child + 1].Priority) < 0))
					//is true when left child has lower priority than right one
					child++;
				}
				Timer childTimer = heap[child];
				childTimer.index = index;
				heap[index] = childTimer;
				index = child;
				child = GetLeftChild(index);
			}
			BubbleUp(index, timer);
		}
        
		public IEnumerator GetEnumerator() {
			return new PriorityQueueEnumerator(this);
		}

		public int Count { get {
			return count;
		} }

		private class PriorityQueueEnumerator : IEnumerator {
			private int index;
			private TimerPriorityQueue pq;
			private int version;

			public PriorityQueueEnumerator(TimerPriorityQueue pq) {
				this.pq = pq;
				Reset();
			}

			private void checkVersion() {
				if (version != pq.version)
					throw new InvalidOperationException();
			}

			public void Reset() {
				index = -1;
				version = pq.version;
			}

			public object Current {
				get { 
					checkVersion();
					return pq.heap[index]; 
				}
			}

			public bool MoveNext() {
				checkVersion();
				if (index + 1 == pq.count) 
					return false;
				index++;
				return true;
			}
		}
	}
}
