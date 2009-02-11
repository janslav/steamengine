using System;
using System.Collections.Generic;
using SteamEngine.Common;

namespace SteamEngine {
	[Summary("A Dictionary that forgets entries that it receieved if they haven't been used in the last 'maxQueueCount' usages of the dictionary. "
		+ "The maxQueueLength number should typically be pretty big, in thousands or more.")]
	public class CacheDictionary<TKey, TValue> : IDictionary<TKey, TValue> {
		private Dictionary<TKey, CacheDictionaryKeyEntry> dict;
		private LinkedList<TKey> linkedList = new LinkedList<TKey>();
		public readonly int maxCacheItems;
		public readonly bool disposeOnRemove;

		public CacheDictionary(int maxCacheItems, bool disposeOnRemove) {
			if (maxCacheItems < 1) {
				throw new SEException("maxQueueCount must be higher than 0");
			}
			this.dict = new Dictionary<TKey, CacheDictionaryKeyEntry>();
			this.maxCacheItems = maxCacheItems;
			this.disposeOnRemove = disposeOnRemove;
		}

		public CacheDictionary(int maxCacheItems, bool disposeOnRemove, IEqualityComparer<TKey> comparer) {
			if (maxCacheItems < 1) {
				throw new SEException("maxQueueCount must be higher than 0");
			}
			this.dict = new Dictionary<TKey, CacheDictionaryKeyEntry>(comparer);
			this.maxCacheItems = maxCacheItems;
			this.disposeOnRemove = disposeOnRemove;
		}

		private struct CacheDictionaryKeyEntry {
			internal TValue value;
			internal LinkedListNode<TKey> node;
			internal CacheDictionaryKeyEntry(TValue value, LinkedListNode<TKey> node) {
				this.value = value;
				this.node = node;
			}
		}

		#region IDictionary<TKey, TValue> Members
		public void Add(TKey key, TValue value) {
			if (this.dict.ContainsKey(key)) {
				throw new SEException("Adding duplicate");
			} else {
				this.linkedList.AddFirst(key);
				dict.Add(key, new CacheDictionaryKeyEntry(value, this.linkedList.First));
				this.PurgeLastIfNeeded();
			}
		}

		private void PurgeLastIfNeeded() {
			if (this.linkedList.Count > this.maxCacheItems) {
				LinkedListNode<TKey> node = this.linkedList.Last;
				this.linkedList.RemoveLast();
				if (this.disposeOnRemove) {
					IDisposable disposable = this.dict[node.Value] as IDisposable;
					if (disposable != null) {
						disposable.Dispose();
					}
				}
				this.dict.Remove(node.Value);
			}
		}

		public bool ContainsKey(TKey key) {
			CacheDictionaryKeyEntry valueEntry;
			if (dict.TryGetValue(key, out valueEntry)) {
				this.linkedList.Remove(valueEntry.node);
				this.linkedList.AddFirst(valueEntry.node);//put our node on the first place
				return true;
			}
			return false;
		}

		public bool Remove(TKey key) {
			CacheDictionaryKeyEntry valueEntry;
			if (dict.TryGetValue(key, out valueEntry)) {
				this.linkedList.Remove(valueEntry.node);
				dict.Remove(key);
				if (this.disposeOnRemove) {
					IDisposable disposable = valueEntry.value as IDisposable;
					if (disposable != null) {
						disposable.Dispose();
					}
				}
				return true;
			}
			return false;
		}

		public bool TryGetValue(TKey key, out TValue value) {
			CacheDictionaryKeyEntry valueEntry;
			if (dict.TryGetValue(key, out valueEntry)) {
				this.linkedList.Remove(valueEntry.node);
				this.linkedList.AddFirst(valueEntry.node);//put our node on the first place
				value = valueEntry.value;
				return true;
			}
			value = default(TValue);
			return false;
		}

		public TValue this[TKey key] {
			get {
				return dict[key].value;
			}
			set {
				CacheDictionaryKeyEntry valueEntry;
				if (dict.TryGetValue(key, out valueEntry)) {
					this.linkedList.Remove(valueEntry.node);
					this.linkedList.AddFirst(valueEntry.node);//put our node on the first place
					dict[key] = new CacheDictionaryKeyEntry(value, valueEntry.node);
				} else {
					this.linkedList.AddFirst(key);
					dict.Add(key, new CacheDictionaryKeyEntry(value, this.linkedList.First));
					this.PurgeLastIfNeeded();
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
				throw new SEException("The method or operation is not implemented.");
			}
		}
		#endregion

		#region ICollection<KeyValuePair<TKey,TValue>> Members

		public void Add(KeyValuePair<TKey, TValue> keyValuePair) {
			this.Add(keyValuePair.Key, keyValuePair.Value);
		}

		public void Clear() {
			dict.Clear();
			linkedList.Clear();
		}

		public bool Contains(KeyValuePair<TKey, TValue> item) {
			return this.ContainsKey(item.Key);
		}

		public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) {
			throw new SEException("The method or operation is not implemented.");
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
			throw new SEException("The method or operation is not implemented.");
		}

		#endregion

		#region IEnumerable<KeyValuePair<TKey,TValue>> Members

		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() {
			throw new SEException("The method or operation is not implemented.");
		}

		#endregion

		#region IEnumerable Members

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
			throw new SEException("The method or operation is not implemented.");
		}

		#endregion
	}
}