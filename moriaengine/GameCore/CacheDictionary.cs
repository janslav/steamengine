using System;
using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.Timers;

namespace SteamEngine {
	[Remark("A Dictionary that forgets entries that it receieved if they haven't been used in the last 'maxQueueCount' usages of the dictionary. "
		+"The maxQueueLength number should typically be pretty big, in thousands or more.")]
	public class CacheDictionary<TKey, TValue> : IDictionary<TKey, TValue> {
		private Dictionary<TKey, CacheDictionaryKeyEntry> dict;
		private SimpleQueue<TKey> queue = new SimpleQueue<TKey>();
		private int maxQueueCount;

		public CacheDictionary(int maxQueueLength) {
			if (maxQueueLength < 1) {
				throw new ArgumentException("maxQueueCount must be higher than 0");
			}
			this.dict = new Dictionary<TKey, CacheDictionaryKeyEntry>();
			this.maxQueueCount = maxQueueLength;
		}

		public CacheDictionary(int maxQueueLength, IEqualityComparer<TKey> comparer) {
			if (maxQueueLength < 1) {
				throw new ArgumentException("maxQueueCount must be higher than 0");
			}
			this.dict = new Dictionary<TKey, CacheDictionaryKeyEntry>(comparer);
			this.maxQueueCount = maxQueueLength;
		}

		public int MaxQueueCount { get {
			return maxQueueCount;
		} }

		private struct CacheDictionaryKeyEntry {
			internal TValue value;
			internal int usageCounter;
			internal CacheDictionaryKeyEntry(TValue value, int usageCounter) {
				this.value = value;
				this.usageCounter = usageCounter;
			}
		}

		#region IDictionary<TKey, TValue> Members
		public void Add(TKey key, TValue value) {
			CacheDictionaryKeyEntry valueEntry;
			if (dict.TryGetValue(key, out valueEntry)) {
				dict.Add(key, new CacheDictionaryKeyEntry(value, valueEntry.usageCounter+1));
			} else {
				dict.Add(key, new CacheDictionaryKeyEntry(value, 1));
			}
			EnqueueAndOrPurge(key);
		}

		public bool ContainsKey(TKey key) {
			CacheDictionaryKeyEntry valueEntry;
			if (dict.TryGetValue(key, out valueEntry)) {
				valueEntry.usageCounter++;
				dict[key] = valueEntry;//it's a struct we must re-enter it :\
				EnqueueAndOrPurge(key);
				return true;
			}
			return false;
		}

		public bool Remove(TKey key) {
			return dict.Remove(key);
		}

		public bool TryGetValue(TKey key, out TValue value) {
			CacheDictionaryKeyEntry valueEntry;
			if (dict.TryGetValue(key, out valueEntry)) {
				valueEntry.usageCounter++;
				dict[key] = valueEntry;//it's a struct we must re-enter it :\
				value = valueEntry.value;
				EnqueueAndOrPurge(key);
				return true;
			}
			value = default(TValue);
			return false;
		}

		public TValue this[TKey key] {
			get {
				CacheDictionaryKeyEntry valueEntry = dict[key];
				valueEntry.usageCounter++;
				dict[key] = valueEntry;//it's a struct we must re-enter it :\
				EnqueueAndOrPurge(key);
				return valueEntry.value;
			}
			set {
				CacheDictionaryKeyEntry valueEntry;
				if (dict.TryGetValue(key, out valueEntry)) {
					dict[key] = new CacheDictionaryKeyEntry(value, valueEntry.usageCounter+1);
				} else {
					dict[key] = new CacheDictionaryKeyEntry(value, 1);
				}
				EnqueueAndOrPurge(key);
			}
		}

		private void EnqueueAndOrPurge(TKey key) {
			queue.Enqueue(key);
			if (queue.Count > maxQueueCount) {
				TKey removedKey = queue.Dequeue();
				CacheDictionaryKeyEntry valueEntry;
				if (dict.TryGetValue(key, out valueEntry)) {
					valueEntry.usageCounter--;
					if (valueEntry.usageCounter < 1) {
						dict.Remove(removedKey);
					} else {
						dict[key] = valueEntry;//it's a struct we must re-enter it :\
					}
				}
			}
		}

		public ICollection<TKey> Keys {
			get {
				return dict.Keys;
			}
		}

		public ICollection<TValue> Values {
			get {
				throw new NotImplementedException("The method or operation is not implemented.");
			}
		}
		#endregion

		#region ICollection<KeyValuePair<TKey,TValue>> Members

		public void Add(KeyValuePair<TKey, TValue> keyValuePair) {
			this.Add(keyValuePair.Key, keyValuePair.Value);
		}

		public void Clear() {
			dict.Clear();
			queue.Clear();
		}

		public bool Contains(KeyValuePair<TKey, TValue> item) {
			throw new NotImplementedException("The method or operation is not implemented.");
		}

		public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) {
			throw new NotImplementedException("The method or operation is not implemented.");
		}

		public int Count {
			get {
				return dict.Count;
			}
		}

		public bool IsReadOnly {
			get { 
				return false; 
			}
		}

		public bool Remove(KeyValuePair<TKey, TValue> item) {
			throw new NotImplementedException("The method or operation is not implemented.");
		}

		#endregion

		#region IEnumerable<KeyValuePair<TKey,TValue>> Members

		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() {
			throw new NotImplementedException("The method or operation is not implemented.");
		}

		#endregion

		#region IEnumerable Members

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
			throw new NotImplementedException("The method or operation is not implemented.");
		}

		#endregion
	}
}