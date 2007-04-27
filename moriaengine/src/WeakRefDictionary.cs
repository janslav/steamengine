using System;
using System.Collections.Generic;
using SteamEngine.Common;

namespace SteamEngine {
	internal struct WeakRefDictionaryKeyEntry {
		internal int hashCode;
		internal WeakReference weakKey;
		internal WeakRefDictionaryKeyEntry(object key) {
			this.hashCode = key.GetHashCode();
			this.weakKey = new WeakReference(key);
		}
	}

	public interface IPurgable {
		void Purge();
	}

	[Remark("We purge the caches when saving world")]
	public class WeakRefDictionaryUtils {
		internal static List<WeakReference> allCaches = new List<WeakReference>();

		public static void PurgeAll() {
			List<WeakReference> aliveCaches = new List<WeakReference>(allCaches.Count);
			foreach (WeakReference weakCache in allCaches) {
				IPurgable cache = weakCache.Target as IPurgable;
				if (cache != null) {
					cache.Purge();
					aliveCaches.Add(weakCache);
				}
			}
			allCaches = aliveCaches;
		}
	}

	[Remark("Dictionary of weakly referenced keys and values. "
	+"That means you can put inside stuff and then later it may or may not be still inside :)"
	+"Depending on whether it has been deleted meanwhile (if it's IDeletable) or eaten by system garbage collection.")]
	public class WeakRefDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IPurgable {
		private Dictionary<WeakRefDictionaryKeyEntry, WeakReference> dict;
		private IEqualityComparer<TKey> comparer;

		public WeakRefDictionary() 
			: this(null)
		{
		}

		public WeakRefDictionary(IEqualityComparer<TKey> comparer) {
			dict = new Dictionary<WeakRefDictionaryKeyEntry, WeakReference>(new CacheComparer<TKey, TValue>(this));
			WeakRefDictionaryUtils.allCaches.Add(new WeakReference(this));
			if (comparer == null) {
				this.comparer = EqualityComparer<TKey>.Default;
			} else {
				this.comparer = comparer;
			}
		}

		private class CacheComparer<TCKey, TCValue> : IEqualityComparer<WeakRefDictionaryKeyEntry> {
			private WeakRefDictionary<TCKey, TCValue> cache;

			internal CacheComparer(WeakRefDictionary<TCKey, TCValue> cache) {
				this.cache = cache;
			}

			public bool Equals(WeakRefDictionaryKeyEntry entryA, WeakRefDictionaryKeyEntry entryB) {
				TCKey keyA = (TCKey) entryA.weakKey.Target;
				TCKey keyB = (TCKey) entryB.weakKey.Target;
				if ((keyA != null) && (keyB != null)) {
					IDeletable deletable = keyA as IDeletable;
					if (deletable != null) {
						if (deletable.IsDeleted) {
							return false;
						}
					}
					deletable = keyB as IDeletable;
					if (deletable != null) {
						if (deletable.IsDeleted) {
							return false;
						}
					}
					return cache.comparer.Equals(keyA, keyB);
				}
				return false;
			}

			public int GetHashCode(WeakRefDictionaryKeyEntry entry) {
				Object key = entry.weakKey.Target;
				if (key != null) {
					return key.GetHashCode();
				}
				return -1;
			}
		}

		#region IPurgable Members
		[Remark("Clears out the entries where either the key or the value have become deleted (if they're IDeletable) or have been memory-collected")]
		public void Purge() {
			List<KeyValuePair<WeakRefDictionaryKeyEntry, WeakReference>> aliveEntries = new List<KeyValuePair<WeakRefDictionaryKeyEntry, WeakReference>>(dict.Count);
			foreach (KeyValuePair<WeakRefDictionaryKeyEntry, WeakReference> pair in dict) {
				if (IsAlive(pair.Key.weakKey) && IsAlive(pair.Value)) {
					aliveEntries.Add(pair);
				}
			}
			if (aliveEntries.Count < dict.Count) { //something was deleted, we must rebuild
				dict.Clear();
				foreach (KeyValuePair<WeakRefDictionaryKeyEntry, WeakReference> pair in aliveEntries) {
					this.dict.Add(pair.Key, pair.Value);
				}
			}
		}

		private bool IsAlive(WeakReference weak) {
			object o = weak.Target;
			if (o != null) {
				IDeletable deletable = o as IDeletable;
				if (deletable != null) {
					if (deletable.IsDeleted) {
						return false;
					}
				}
				return true;
			}
			return false;
		}
		#endregion

		#region IDictionary<TKey, TValue> Members
		public void Add(TKey key, TValue value) {
			if (key == null) {
				throw new ArgumentNullException("key");
			}
			if (value == null) {
				throw new ArgumentNullException("value");
			}
			dict.Add(new WeakRefDictionaryKeyEntry(key),
				new WeakReference(value));
		}

		public bool ContainsKey(TKey key) {
			return dict.ContainsKey(new WeakRefDictionaryKeyEntry(key));
		}

		public bool Remove(TKey key) {
			return dict.Remove(new WeakRefDictionaryKeyEntry(key));
		}

		public bool TryGetValue(TKey key, out TValue value) {
			WeakReference weakWal;
			value = default(TValue);
			if (dict.TryGetValue(new WeakRefDictionaryKeyEntry(key), out weakWal)) {
				value = (TValue) weakWal.Target;
				if (value != null) {
					IDeletable deletable = value as IDeletable;
					if (deletable != null) {
						if (deletable.IsDeleted) {
							return false;
						}
					}
					return true;
				}
			}
			return false;
		}

		public TValue this[TKey key] {
			get {
				WeakReference weakWal = dict[new WeakRefDictionaryKeyEntry(key)];
				TValue value = (TValue) weakWal.Target;
				if (value != null) {
					IDeletable deletable = value as IDeletable;
					if (deletable != null) {
						if (deletable.IsDeleted) {
							return default(TValue);
						}
					}
					return value;
				}
				return default(TValue);
			}
			set {
				if (key == null) {
					throw new ArgumentNullException("key");
				}
				if (value == null) {
					throw new ArgumentNullException("value");
				}
				dict[new WeakRefDictionaryKeyEntry(key)] = new WeakReference(value);
			}
		}

		private KeysCollection<TKey, TValue> keys;
		public ICollection<TKey> Keys {
			get {
				if (keys == null) {
					keys = new KeysCollection<TKey, TValue>(this);
				}
				return keys;
			}
		}

		private ValuesCollection<TKey, TValue> values;
		public ICollection<TValue> Values {
			get {
				if (values == null) {
					values = new ValuesCollection<TKey, TValue>(this);
				}
				return values;
			}
		}

		private class KeysCollection<TKKey,TKValue> : ICollection<TKKey> {
			private WeakRefDictionary<TKKey,TKValue> cache;

			internal KeysCollection(WeakRefDictionary<TKKey,TKValue> cache) {
				this.cache = cache;
			}

			public void Add(TKKey item) {
				throw new NotSupportedException("readonly");
			}

			public void Clear() {
				throw new NotSupportedException("readonly");
			}

			public bool Remove(TKKey item) {
				throw new NotSupportedException("readonly");
			}

			public bool Contains(TKKey key) {
				return cache.dict.ContainsKey(new WeakRefDictionaryKeyEntry(key));
			}

			public void CopyTo(TKKey[] array, int arrayIndex) {
				cache.Purge();
				foreach (KeyValuePair<WeakRefDictionaryKeyEntry, WeakReference> pair in cache.dict) {
					array[arrayIndex] = (TKKey) pair.Key.weakKey.Target;
					arrayIndex++;
				}
			}

			public int Count {
				get {
					return cache.Count;
				}
			}

			public bool IsReadOnly {
				get {
					return true;
				}
			}

			public IEnumerator<TKKey> GetEnumerator() {
				cache.Purge();
				foreach (KeyValuePair<WeakRefDictionaryKeyEntry, WeakReference> pair in cache.dict) {
					yield return (TKKey) pair.Key.weakKey.Target;
				}
			}

			System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
				cache.Purge();
				foreach (KeyValuePair<WeakRefDictionaryKeyEntry, WeakReference> pair in cache.dict) {
					yield return pair.Key.weakKey.Target;
				}
			}
		}

		private class ValuesCollection<TKKey, TKValue> : ICollection<TKValue> {
			private WeakRefDictionary<TKKey, TKValue> cache;

			internal ValuesCollection(WeakRefDictionary<TKKey, TKValue> cache) {
				this.cache = cache;
			}

			public void Add(TKValue item) {
				throw new NotSupportedException("readonly");
			}

			public void Clear() {
				throw new NotSupportedException("readonly");
			}

			public bool Remove(TKValue item) {
				throw new NotSupportedException("readonly");
			}

			public bool Contains(TKValue value) {
				return cache.dict.ContainsValue(new WeakReference(value));
			}

			public void CopyTo(TKValue[] array, int arrayIndex) {
				cache.Purge();
				foreach (KeyValuePair<WeakRefDictionaryKeyEntry, WeakReference> pair in cache.dict) {
					array[arrayIndex] = (TKValue) pair.Value.Target;
					arrayIndex++;
				}
			}

			public int Count {
				get {
					return cache.Count;
				}
			}

			public bool IsReadOnly {
				get {
					return true;
				}
			}

			public IEnumerator<TKValue> GetEnumerator() {
				cache.Purge();
				foreach (KeyValuePair<WeakRefDictionaryKeyEntry, WeakReference> pair in cache.dict) {
					yield return (TKValue) pair.Value.Target;
				}
			}

			System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
				cache.Purge();
				foreach (KeyValuePair<WeakRefDictionaryKeyEntry, WeakReference> pair in cache.dict) {
					yield return pair.Value.Target;
				}
			}
		}

		#endregion

		#region ICollection<KeyValuePair<TKey,TValue>> Members

		public void Add(KeyValuePair<TKey, TValue> keyValuePair) {
			this.Add(keyValuePair.Key, keyValuePair.Value);
		}

		public void Clear() {
			dict.Clear();
		}

		public bool Contains(KeyValuePair<TKey, TValue> item) {
			throw new NotImplementedException("The method or operation is not implemented.");
		}

		public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) {
			Purge();
			foreach (KeyValuePair<WeakRefDictionaryKeyEntry, WeakReference> pair in dict) {
				array[arrayIndex] = new KeyValuePair<TKey, TValue>(
					(TKey) pair.Key.weakKey.Target, (TValue) pair.Value.Target);
				arrayIndex++;
			}
		}

		public int Count {
			get {
				Purge();
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
			Purge();
			foreach (KeyValuePair<WeakRefDictionaryKeyEntry, WeakReference> pair in dict) {
				yield return new KeyValuePair<TKey, TValue>(
					(TKey) pair.Key.weakKey.Target, (TValue) pair.Value.Target);
			}
		}

		#endregion

		#region IEnumerable Members

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
			Purge();
			foreach (KeyValuePair<WeakRefDictionaryKeyEntry, WeakReference> pair in dict) {
				yield return new KeyValuePair<TKey, TValue>(
					(TKey) pair.Key.weakKey.Target, (TValue) pair.Value.Target);
			}
		}

		#endregion
	}
}