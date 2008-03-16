using System;
using System.Collections;
using System.Collections.Generic;

namespace SteamEngine.Common {
	public class HashSet<T> : ICollection<T> {
		private int[] buckets;
		private int count;
		private Entry[] entries;
		private int freeCount;
		private int freeList;
		private int version;

		private struct Entry {
			public int hashCode;
			public int next;
			public T key;
		}

		public void Add(T value) {
			int index;
			if (value == null) {
				throw new ArgumentNullException("value");
			}
			if (this.buckets == null) {
				this.Initialize(0);
			}
			int num = this.GetHashCode(value) & 2147483647;
			for (int i = this.buckets[num % this.buckets.Length]; i >= 0; i = this.entries[i].next) {
				if ((this.entries[i].hashCode == num) && this.Equals(this.entries[i].key, value)) {
					this.version++;
					return;
				}
			}
			if (this.freeCount > 0) {
				index = this.freeList;
				this.freeList = this.entries[index].next;
				this.freeCount--;
			} else {
				if (this.count == this.entries.Length) {
					this.Resize();
				}
				index = this.count;
				this.count++;
			}
			int num4 = num % this.buckets.Length;
			this.entries[index].hashCode = num;
			this.entries[index].next = this.buckets[num4];
			this.entries[index].key = value;
			this.buckets[num4] = index;
			this.version++;
		}

		public int Count {
			get {
				return (this.count - this.freeCount);
			}
		}

		public bool Remove(T key) {
			if (key == null) {
				throw new ArgumentNullException("value");
			}
			if (this.buckets != null) {
				int num = this.GetHashCode(key) & 2147483647;
				int index = num % this.buckets.Length;
				int num3 = -1;
				for (int i = this.buckets[index]; i >= 0; i = this.entries[i].next) {
					if ((this.entries[i].hashCode == num) && this.Equals(this.entries[i].key, key)) {
						if (num3 < 0) {
							this.buckets[index] = this.entries[i].next;
						} else {
							this.entries[num3].next = this.entries[i].next;
						}
						this.entries[i].hashCode = -1;
						this.entries[i].next = this.freeList;
						this.entries[i].key = default(T);
						this.freeList = i;
						this.freeCount++;
						this.version++;
						return true;
					}
					num3 = i;
				}
			}
			return false;
		}

		public bool Contains(T value) {
			return (this.FindEntry(value) >= 0);
		}

		public void Clear() {
			if (this.count > 0) {
				for (int i = 0; i < this.buckets.Length; i++) {
					this.buckets[i] = -1;
				}
				Array.Clear(this.entries, 0, this.count);
				this.freeList = -1;
				this.count = 0;
				this.freeCount = 0;
				this.version++;
			}
		}

		private int FindEntry(T value) {
			if (value == null) {
				throw new ArgumentNullException("value");
			}
			if (this.buckets != null) {
				int num = this.GetHashCode(value) & 2147483647;
				for (int i = this.buckets[num % this.buckets.Length]; i >= 0; i = this.entries[i].next) {
					if ((this.entries[i].hashCode == num) && this.Equals(this.entries[i].key, value)) {
						return i;
					}
				}
			}
			return -1;
		}

		private void Resize() {
			int prime = PrimeNumbers.GetPrime(this.count * 2);
			int[] numArray = new int[prime];
			for (int i = 0; i < numArray.Length; i++) {
				numArray[i] = -1;
			}
			Entry[] destinationArray = new Entry[prime];
			Array.Copy(this.entries, 0, destinationArray, 0, this.count);
			for (int j = 0; j < this.count; j++) {
				int index = destinationArray[j].hashCode % prime;
				destinationArray[j].next = numArray[index];
				numArray[index] = j;
			}
			this.buckets = numArray;
			this.entries = destinationArray;
		}

		private void Initialize(int capacity) {
			int prime = PrimeNumbers.GetPrime(capacity);
			this.buckets = new int[prime];
			for (int i = 0; i < this.buckets.Length; i++) {
				this.buckets[i] = -1;
			}
			this.entries = new Entry[prime];
			this.freeList = -1;
		}

		private int GetHashCode(T obj) {
			if (obj == null) {
				return 0;
			}
			return obj.GetHashCode();
		}

		private bool Equals(T x, T y) {
			if (x != null) {
				if (y != null) {
					return x.Equals(y);
				}
				return false;
			}
			if (y != null) {
				return false;
			}
			return true;
		}

		private static class PrimeNumbers {
			// Fields
			private static readonly int[] primes = new int[] { 
				3, 7, 11, 17, 23, 29, 37, 47, 59, 71, 89, 107, 131, 163, 197, 239, 
				293, 353, 431, 521, 631, 761, 919, 1103, 1327, 1597, 1931, 2333, 2801, 3371, 4049, 4861, 
				5839, 7013, 8419, 10103, 12143, 14591, 17519, 21023, 25229, 30293, 36353, 43627, 52361, 62851, 75431, 90523, 
				108631, 130363, 156437, 187751, 225307, 270371, 324449, 389357, 467237, 560689, 672827, 807403, 968897, 1162687, 1395263, 1674319, 
				2009191, 2411033, 2893249, 3471899, 4166287, 4999559, 5999471, 7199369
			 };

			internal static int GetPrime(int min) {
				if (min < 0) {
					throw new ArgumentException();
				}
				for (int i = 0; i < primes.Length; i++) {
					int num2 = primes[i];
					if (num2 >= min) {
						return num2;
					}
				}
				for (int j = min | 1; j < 2147483647; j += 2) {
					if (IsPrime(j)) {
						return j;
					}
				}
				return min;
			}

			private static bool IsPrime(int candidate) {
				if ((candidate & 1) == 0) {
					return (candidate == 2);
				}
				int num = (int) Math.Sqrt((double) candidate);
				for (int i = 3; i <= num; i += 2) {
					if ((candidate % i) == 0) {
						return false;
					}
				}
				return true;
			}
		}

		#region IEnumerable<T> Members

		public IEnumerator<T> GetEnumerator() {
			return new Enumerator(this);
		}

		#endregion

		#region IEnumerable Members

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
			return new Enumerator(this);
		}

		#endregion

		internal class Enumerator : IEnumerator<T>, IEnumerator {
			private HashSet<T> dictionary;
			private int version;
			private int index;
			private T current;

			internal Enumerator(HashSet<T> dictionary) {
				this.dictionary = dictionary;
				this.version = dictionary.version;
				this.index = 0;
				this.current = default(T);
			}

			#region IEnumerator<T> Members

			T IEnumerator<T>.Current {
				get {
					return this.current;
				}
			}

			#endregion

			#region IEnumerator Members

			object System.Collections.IEnumerator.Current {
				get {
					return this.current;
				}
			}

			bool System.Collections.IEnumerator.MoveNext() {
				if (this.version != this.dictionary.version) {
					throw new InvalidOperationException("Don't touch the HashSet while enumerating it.");
				}
				while (this.index < this.dictionary.count) {
					if (this.dictionary.entries[this.index].hashCode >= 0) {
						this.current = this.dictionary.entries[this.index].key;
						this.index++;
						return true;
					}
					this.index++;
				}
				this.index = this.dictionary.count + 1;
				this.current = default(T);
				return false;
			}

			void System.Collections.IEnumerator.Reset() {
				if (this.version != this.dictionary.version) {
					throw new InvalidOperationException("Don't touch the HashSet while enumerating it.");
				}
				this.index = 0;
				this.current = default(T);

			}

			#endregion

			#region IDisposable Members

			void IDisposable.Dispose() {
			}

			#endregion
		}

		#region ICollection<T> Members


		public void CopyTo(T[] array, int arrayIndex) {
			foreach (T entry in this) {
				array[arrayIndex] = entry;
				arrayIndex++;
			}
		}

		public bool IsReadOnly {
			get { return false; }
		}

		#endregion
	}
}