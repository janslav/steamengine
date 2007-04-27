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

//the queue has been made generic
//the original file still most probably lies in the 3dParty directory

using System;
using System.Collections;

namespace SteamEngine.CompiledScripts {

	public class PriorityQueue<TNode, TPriority>
			where TPriority : IComparable<TPriority> {

		private int count;
		private int capacity;
		private int version;
		private Node<TNode, TPriority>[] heap;

		struct Node<TNNode, TNPriority> {
			internal TNNode value;
			internal TNPriority priority;
		}

		public PriorityQueue() {
			capacity = 15;
			heap = new Node<TNode, TPriority>[capacity];
		}
		
		public void Clear() {
			Array.Clear(heap, 0, count);
			count = 0;
		}

		public TNode Dequeue() {
			if (count == 0) {
				throw new InvalidOperationException();
			}
			TNode result = heap[0].value;
			count--;
			TrickleDown(0, heap[count]);
			//heap[count] = default(TNode);
			version++;
			return result;
		}
		
		private void RemoveAt(int i) {
			count--;
			TrickleDown(i, heap[count]);
			//heap[count] = default(TNode);
			version++;
		}
		
		public TNode Peek() {
			if (count == 0) {
				throw new InvalidOperationException();
			}
			return heap[0].value;
		}

		public void Enqueue(TNode nodeValue, TPriority priority) {
			if (count == capacity) {
				Grow();
			}
			count++;
			Node<TNode, TPriority> node = new Node<TNode, TPriority>();
			node.value = nodeValue;
			node.priority = priority;
			BubbleUp(count - 1, node);
			version++;
		}

		private void BubbleUp(int index, Node<TNode, TPriority> node) {
			int parent = GetParent(index);
			// note: (index > 0) means there is a parent

			while ((index > 0) && 
					(heap[parent].priority.CompareTo(node.priority) > 0)) {
				Node<TNode, TPriority> parentAStarNode = heap[parent];
				heap[index] = parentAStarNode;
				index = parent;
				parent = GetParent(index);
			}
			heap[index] = node;
		}

		private int GetLeftChild(int index) {
			return (index * 2) + 1;
		}

		private int GetParent(int index) {
			return (index - 1) / 2;
		}

		private void Grow() {
			capacity = (capacity * 2) + 1;
			Node<TNode, TPriority>[] newHeap = new Node<TNode, TPriority>[capacity];
			System.Array.Copy(heap, 0, newHeap, 0, count);
			heap = newHeap;
		}

		private void TrickleDown(int index, Node<TNode, TPriority> node) {
			int child = GetLeftChild(index);
			while (child < count) {
				if (((child + 1) < count) && 
						(heap[child].priority.CompareTo(heap[child + 1].priority) > 0)) {
					child++;
				}
				Node<TNode, TPriority> childAStarNode = heap[child];
				heap[index] = childAStarNode;
				index = child;
				child = GetLeftChild(index);
			}
			BubbleUp(index, node);
		}
        
		public IEnumerator GetEnumerator() {
			int startVersion = this.version;
			for (int i = 0; i<count; i++) {
				if (startVersion != this.version) {
					throw new InvalidOperationException();
				}
				yield return heap[i];
			}
		}

		public int Count { get {
			return count;
		} }
	}
}
