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

using System.IO;
using System;
using System.Reflection;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;


namespace SteamEngine.Common {
	public class EmptyReadOnlyCollection : IEnumerable, IEnumerator, IList, ICollection, IDictionary, IDictionaryEnumerator {
		public static readonly EmptyReadOnlyCollection instance = new EmptyReadOnlyCollection();

		public void CopyTo(Array array, int index) {
		}

		public int Count {
			get { return 0; }
		}

		public bool IsSynchronized {
			get { return true; }
		}

		public object SyncRoot {
			get { return this; }
		}

		public IEnumerator GetEnumerator() {
			return this;
		}

		public int Add(object value) {
			throw new SEException("Not supported");
		}

		public void Clear() {
		}

		public bool Contains(object value) {
			return false;
		}

		public int IndexOf(object value) {
			return -1;
		}

		public void Insert(int index, object value) {
			throw new SEException("Not supported");
		}

		public bool IsFixedSize {
			get {
				return true;
			}
		}

		public bool IsReadOnly {
			get { return true; }
		}

		public void Remove(object value) {
		}

		public void RemoveAt(int index) {
		}

		public object this[int index] {
			get {
				throw new SEException("Not supported");
			}
			set {
				throw new SEException("Not supported");
			}
		}

		public object Current {
			get { throw new SEException("Not supported"); }
		}

		public bool MoveNext() {
			return false;
		}

		public void Reset() {
		}

		public void Add(object key, object value) {
			throw new SEException("Not supported");
		}

		IDictionaryEnumerator IDictionary.GetEnumerator() {
			return this;
		}

		public ICollection Keys {
			get { return this; }
		}

		public ICollection Values {
			get { return this; }
		}

		public object this[object key] {
			get {
				return null;
			}
			set {
				throw new Exception("The method or operation is not implemented.");
			}
		}

		public DictionaryEntry Entry {
			get {
				throw new SEException("Not supported");
			}
		}

		public object Key {
			get { throw new SEException("Not supported"); ; }
		}

		public object Value {
			get { throw new SEException("Not supported"); ; }
		}
	}

	public class EmptyReadOnlyGenericCollection<T> : EmptyReadOnlyCollection, IEnumerable<T>, IEnumerator<T>, ICollection<T>, IList<T> {
		public static new readonly EmptyReadOnlyGenericCollection<T> instance = new EmptyReadOnlyGenericCollection<T>();

		public void Add(T item) {
			throw new SEException("Not supported");
		}

		public bool Contains(T item) {
			return false;
		}

		public void CopyTo(T[] array, int arrayIndex) {
		}

		public bool Remove(T item) {
			return false;
		}

		public new IEnumerator<T> GetEnumerator() {
			return this;
		}

		public new T Current {
			get { throw new SEException("Not supported"); }
		}

		public void Dispose() {
		}

		public int IndexOf(T item) {
			return -1;
		}

		public void Insert(int index, T item) {
			throw new SEException("Not supported");
		}

		public new T this[int index] {
			get {
				throw new SEException("Not supported");
			}
			set {
				throw new SEException("Not supported");
			}
		}
	}

	public class EmptyReadOnlyDictionary<TKey, TValue> : EmptyReadOnlyGenericCollection<KeyValuePair<TKey, TValue>>, IDictionary<TKey, TValue> {
		public static new readonly EmptyReadOnlyDictionary<TKey, TValue> instance = new EmptyReadOnlyDictionary<TKey, TValue>();

		public void Add(TKey key, TValue value) {
			throw new SEException("Not supported");
		}

		public bool ContainsKey(TKey key) {
			return false;
		}

		public new ICollection<TKey> Keys {
			get { return EmptyReadOnlyGenericCollection<TKey>.instance; }
		}

		public bool Remove(TKey key) {
			return false;
		}

		public bool TryGetValue(TKey key, out TValue value) {
			value = default(TValue);
			return false;
		}

		public new ICollection<TValue> Values {
			get { return EmptyReadOnlyGenericCollection<TValue>.instance; }
		}

		public TValue this[TKey key] {
			get {
				throw new SEException("Not supported");
			}
			set {
				throw new SEException("Not supported");
			}
		}
	}
}