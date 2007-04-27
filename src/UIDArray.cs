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

namespace SteamEngine {
	//the first uid should be 1, because clients show "hidden error" behaviour with a client of uid 1...
	internal class UIDArray<ElementType> : IEnumerable<ElementType> {
		private ElementType[] array;
		private Queue<int> freeSlots;
		private int highestElement;
		private int count;
		private bool loadingFinished;

		internal int HighestElement { get {
			return highestElement;
		} }

		internal int Count { get {
			return count;
		} }

		internal UIDArray() {
			highestElement=0;	//we don't have any elements yet
			count=0;
			loadingFinished=false;
			array=new ElementType[1000];
		}

		//private class UIDIterator : IEnumerator {

		//    private object current;
		//    private int currIdx;
		//    private UIDArray src;

		//    internal UIDIterator(UIDArray src) {	
		//        currIdx=1;
		//        this.src=src;
		//        current=null;
		//    }

		//    public object Current { get { 
		//        return current;
		//    } }

		//    public bool MoveNext() {
		//        while (currIdx<=src.HighestElement) {
		//            current = src.Get(currIdx);
		//            currIdx++;
		//            if (current != null) {
		//                return true;
		//            }
		//        }
		//        return false;
		//    }

		//    public void Reset() {
		//        currIdx=1;
		//        current=null;
		//    }
		//}

		public IEnumerator<ElementType> GetEnumerator() {
		    //return new UIDIterator(this);
			int currIdx=1;
			int highestElement = this.HighestElement;
			while (currIdx<=highestElement) {
				ElementType o = array[currIdx];
				if (!Object.ReferenceEquals(o, default(ElementType))) {
					yield return o;
				}
				currIdx++;
			}
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}

		internal void Clear() {
			freeSlots=new Queue<int>();
			highestElement=0;
			count=0;
			array=new ElementType[array.Length];
			loadingFinished = false;
		}

		internal int Add(ElementType o) {
			if (freeSlots.Count==0) {	//no indexes in the freeSlots queue
				if (highestElement==array.Length-1) { //if highestElement is the size of the array let's move everything into a bigger array
					ResizeArray(array.Length*2);
				} //stick the object at the end as the new highestElement
				highestElement++;
				array[highestElement]=o;
				count++;
				return highestElement;
			} else { //we have freeSlots available
				int num=(int)freeSlots.Dequeue();
				array[num]=o;
				count++;
				return num;
			}
		}

		internal int NextFreeSlot() {
			if (freeSlots.Count==0) { //no indexes in the freeSlots queue
				if (highestElement==array.Length-1) { //if highestElement is the size of the array let's move everything into a bigger array
					ResizeArray(array.Length*2);
				} //stick the object at the end as the new highestElement
				return highestElement+1;
			} else { //we have freeSlots available
				int num=freeSlots.Peek();
				return num;
			}
		}

		private void ResizeArray(int newSize) {
			ElementType[] temp=new ElementType[newSize];
			for (int i=0; i<array.Length; i++) {
				temp[i]=array[i];
			}
			array=temp;
		}

		internal void LoadingFinished() {
			loadingFinished=true;
			freeSlots=new Queue<int>();
			for (int i=1; i<highestElement; i++) {
				if (array[i]==null) {
					freeSlots.Enqueue(i);
				}
			}
		}

		internal void Add(ElementType o, int index) {
			if (loadingFinished) {
				throw new ApplicationException("Add(object,index) disabled after LoadingFinished");
			}
			if (index<1) {
				throw new IndexOutOfRangeException("Object with non-positive UID "+index+" found");
			}
			if (index>array.Length) { //index is too high, make the array bigger!
				if (index>array.Length*2) { //index will be bigger than if we double the array
					ResizeArray(index+1000);
				} else {
					ResizeArray(array.Length*2);
				}
			}
			if (array[index]!=null) {
				throw new IndexOutOfRangeException("Two objects attempted to take the same UID");
			}
			if (index>highestElement) {
				highestElement=index;
			}
			array[index]=o;
			count++;
		}

		internal ElementType Get(int index) { //may return a null object
			if (index<array.Length && index>0) {
				return array[index];
			}
			return default(ElementType);
		}

		internal void RemoveAt(int index) {
			if (array[index]!=null) { //only add to queue if not already null
				array[index]=default(ElementType);
				if (index==highestElement) {
					highestElement--;
				} else {
					freeSlots.Enqueue(index);
				}
				count--;
			}
		}

		internal bool IsValid(int uid) {
			return (uid>0 && uid<array.Length && array[uid]!=null);
		}

		//internal bool Is(int uid, Type t) {
		//    if (!IsValid(uid)) return false;
		//    return t.IsInstanceOfType(array[uid]);
		//}
	}
}