using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace WeSay.Data
{
	public enum SortBy
	{
		KeyPrimaryValueSecondary,
		KeySecondaryValuePrimary
	}


	public class KeyValueComparer<Key, Value> : IComparer<KeyValuePair<Key, Value>>
	{
		private IComparer<Key> _keySorter;
		private IComparer<Value> _valueSorter;
		private SortBy _sortBy;

		public KeyValueComparer(IComparer<Key> keySorter, IComparer<Value> valueSorter, SortBy sortBy)
		{
			_keySorter = keySorter;
			_valueSorter = valueSorter;
			_sortBy = sortBy;
		}

		public KeyValueComparer(IComparer<Key> keySorter, IComparer<Value> valueSorter)
			: this(keySorter, valueSorter, SortBy.KeyPrimaryValueSecondary)
		{

		}

		#region IComparer<KeyValuePair<Key,Value>> Members

		public int Compare(KeyValuePair<Key, Value> x, KeyValuePair<Key, Value> y)
		{
			if (_sortBy == SortBy.KeyPrimaryValueSecondary)
			{
				int result = _keySorter.Compare(x.Key, y.Key);
				if (result == 0)
				{
					result = _valueSorter.Compare(x.Value, y.Value);
				}
				return result;
			}
			else
			{
				int result = _valueSorter.Compare(x.Value, y.Value);
				if (result == 0)
				{
					result = _keySorter.Compare(x.Key, y.Key);
				}
				return result;

			}
		}

		#endregion
	};


	public class CachedSortedDb4oList<K, T>: IBindingList, IEnumerable<K>, IDisposable where T : class, new()
	{
		public delegate K KeyProvider(T item);

		List<KeyValuePair<K, long>> _keyIdMap;
		List<KeyValuePair<K, long>> _idKeyMap;
		Db4oRecordList<T> _masterRecordList;
		string _cachePath;
		KeyValueComparer<K, long> _keyIdSorter;
		KeyValueComparer<K, long> _idKeySorter;
		ISortHelper<K, T> _sortHelper;

		public CachedSortedDb4oList(Db4oRecordListManager recordListManager, ISortHelper<K, T> sortHelper)
		{
			if (recordListManager == null)
			{
				Dispose();
				throw new ArgumentNullException("recordListManager");
			}
			if (sortHelper == null)
			{
				Dispose();
				throw new ArgumentNullException("sortHelper");
			}
			_sortHelper = sortHelper;
			_cachePath = recordListManager.CachePath;
			_keyIdSorter = new KeyValueComparer<K, long>(sortHelper.KeyComparer, Comparer<long>.Default, SortBy.KeyPrimaryValueSecondary);
			_idKeySorter = new KeyValueComparer<K, long>(sortHelper.KeyComparer, Comparer<long>.Default, SortBy.KeySecondaryValuePrimary);
			_masterRecordList = (Db4oRecordList<T>)recordListManager.GetListOfType<T>();

			Deserialize();
			if(_keyIdMap == null)
			{
				_keyIdMap = _sortHelper.GetKeyIdPairs();
				_idKeyMap = _sortHelper.GetKeyIdPairs();
				Sort();
			}

			_masterRecordList.ListChanged += new ListChangedEventHandler(OnMasterRecordListListChanged);
			_masterRecordList.DeletingRecord += new EventHandler<RecordListEventArgs<T>>(OnMasterRecordListDeletingRecord);
			_masterRecordList.ContentOfItemInListChanged += new ListChangedEventHandler(OnMasterRecordListContentChanged);
		}

		private string CacheFilePath
		{
			get
			{
				return Path.Combine(_cachePath, CacheHelper.EscapeFileNameString(_sortHelper.Name) + ".cache");
			}
		}

		private void Serialize()
		{
			try
			{
				if (!Directory.Exists(_cachePath))
				{
					Directory.CreateDirectory(_cachePath);
				}
				using (FileStream fs = File.Create(CacheFilePath))
				{
					BinaryFormatter formatter = new BinaryFormatter();
					try
					{
						formatter.Serialize(fs, GetDatabaseLastModified());
						formatter.Serialize(fs, GetSorterHashCode());
						formatter.Serialize(fs, _keyIdMap);
					}
					finally
					{
						fs.Close();
					}
				}
			}
			catch
			{
			}
		}

		private int GetSorterHashCode() {
			byte[] bytes = _keyIdSorter.GetType().GetMethod("Compare").GetMethodBody().GetILAsByteArray();
			int hashCode = 0;
			for (int i = 0; i < bytes.Length; i++)
			{
				byte b = bytes[i];
				hashCode ^= b;
			}
			return hashCode;
		}

		private void Deserialize()
		{
			_keyIdMap = null;
			_idKeyMap = null;
			if (File.Exists(CacheFilePath))
			{
				using (FileStream fs = File.Open(CacheFilePath, FileMode.Open))
				{
					BinaryFormatter formatter = new BinaryFormatter();
					try
					{
						DateTime databaseLastModified = (DateTime)formatter.Deserialize(fs);
						if (databaseLastModified == GetDatabaseLastModified())
						{
							int filterHashCode = (int)formatter.Deserialize(fs);
							if (filterHashCode == GetSorterHashCode())
							{
								_keyIdMap = (List<KeyValuePair<K, long>>)formatter.Deserialize(fs);
								_idKeyMap = new List<KeyValuePair<K, long>>(_keyIdMap);
								_idKeyMap.Sort(_idKeySorter);

							}
						}
					}
					catch{}
					finally
					{
						fs.Close();
					}
				}
			}
		}

		private DateTime GetDatabaseLastModified()
		{
			return _masterRecordList.GetDatabaseLastModified();
		}

		void OnMasterRecordListContentChanged(object sender, ListChangedEventArgs e)
		{
			IRecordList<T> masterRecordList = (IRecordList<T>)sender;
			Update(masterRecordList[e.NewIndex]);
		}


		void OnMasterRecordListDeletingRecord(object sender, RecordListEventArgs<T> e)
		{
			Remove(e.Item);
		}

		void OnMasterRecordListListChanged(object sender, ListChangedEventArgs e)
		{
			IRecordList<T> masterRecordList = (IRecordList<T>)sender;
			switch (e.ListChangedType)
			{
				case ListChangedType.ItemAdded:
					Add(masterRecordList[e.NewIndex]);
					break;
				case ListChangedType.ItemChanged:
					//Update(masterRecordList[e.NewIndex]);
					break;
				case ListChangedType.ItemDeleted:
					break;
				case ListChangedType.Reset:
					Sort();
					break;
			}
			//Serialize();
		}

		private void Add(T item)
		{
			long itemId = this._masterRecordList.GetId(item);
			foreach (K key in _sortHelper.GetKeys(item))
			{
				int index = AddKeyId(key, itemId);
				OnItemAdded(index);
			}
		}

		private int AddKeyId(K key, long itemId)
		{
			int index;
			KeyValuePair<K, long> keyIdPair = new KeyValuePair<K, long>(key, itemId);


			// insert into id key map
			int idKeyIndex = this._idKeyMap.BinarySearch(keyIdPair, this._idKeySorter);
			if (idKeyIndex < 0) // not found, index is bitwise complement of the place to insert
			{
				idKeyIndex = ~idKeyIndex;
			}
			this._idKeyMap.Insert(idKeyIndex, keyIdPair);

			// insert into key id map
			index = this._keyIdMap.BinarySearch(keyIdPair, this._keyIdSorter);
			if (index < 0) // not found, index is bitwise complement of the place to insert
			{
				index = ~index;
			}
			this._keyIdMap.Insert(index, keyIdPair);
			return index;
		}

		private void Update(T item)
		{
			long itemId = _masterRecordList.GetId(item);

			List<int> indexesOfAddedItems = new List<int>();
			List<int> indexesOfDeletedItems = RemoveId(itemId);

			foreach (K key in _sortHelper.GetKeys(item))
			{
				int index = AddKeyId(key, itemId);
				//if this got inserted before our other indexes, we need to bump their values up
				for (int i = 0; i < indexesOfAddedItems.Count; i++)
				{
					if (indexesOfAddedItems[i] >= index)
					{
						indexesOfAddedItems[i]++;
					}
				}
				indexesOfAddedItems.Add(index);
			}

			for (int i = indexesOfAddedItems.Count - 1; i >= 0; i--)
			{
				int index = indexesOfAddedItems[i];
				if (indexesOfDeletedItems.Contains(index))
				{
					OnItemChanged(index);
					indexesOfDeletedItems.Remove(index);
					indexesOfAddedItems.RemoveAt(i);
				}
			}

			int itemsMoved = Math.Min(indexesOfAddedItems.Count, indexesOfDeletedItems.Count);

			indexesOfAddedItems.Sort();

			for (int i = 0; i < itemsMoved; i++)
			{
				OnItemMoved(indexesOfAddedItems[0],
							indexesOfDeletedItems[0]);
				indexesOfAddedItems.RemoveAt(0);
				indexesOfDeletedItems.RemoveAt(0);
			}

			foreach (int index in indexesOfAddedItems)
			{
				OnItemAdded(index);
			}

			foreach (int index in indexesOfDeletedItems)
			{
				OnItemDeleted(index);
			}
		}

		private List<int> RemoveId(long itemId) {
			List<int> indexesOfDeletedItems = new List<int>();

			int idKeyMapIndex;
			do
			{
				int keyIdMapIndex;
				GetKeyValueIndexes(itemId, out keyIdMapIndex, out idKeyMapIndex);
				if (keyIdMapIndex >=0)
				{
					this._idKeyMap.RemoveAt(idKeyMapIndex);
					indexesOfDeletedItems.Add(keyIdMapIndex);
				}
			}
			while (idKeyMapIndex >= 0);

			indexesOfDeletedItems.Sort();

			for (int i = indexesOfDeletedItems.Count - 1; i >= 0; i--)
			{
				_keyIdMap.RemoveAt(indexesOfDeletedItems[i]);
			}

			return indexesOfDeletedItems;
		}

		private void GetKeyValueIndexes(long itemId, out int keyIdMapIndex, out int idKeyMapIndex) {
			idKeyMapIndex = GetIdKeyMapIndex(itemId);

			keyIdMapIndex = -1;

			// get the index of the keyIdPair from the keyIdMap
			if (idKeyMapIndex >= 0)
			{
				KeyValuePair<K, long> keyIdPair = this._idKeyMap[idKeyMapIndex];
				keyIdMapIndex = this._keyIdMap.IndexOf(keyIdPair);
			}
		}

		private int GetIdKeyMapIndex(long itemId)
		{
			int idKeyMapIndex;
			KeyValuePair<K, long> keyIdPair = new KeyValuePair<K, long>(default(K), itemId);
			idKeyMapIndex = this._idKeyMap.BinarySearch(keyIdPair, this._idKeySorter);
			if (idKeyMapIndex < 0 && ~idKeyMapIndex != this._idKeyMap.Count)
			{
				if (this._idKeyMap[~idKeyMapIndex].Value == itemId)
				{
					idKeyMapIndex = ~idKeyMapIndex;
				}
			}
			return idKeyMapIndex;
		}

		private void Remove(T item)
		{
			if (_masterRecordList.Contains(item))
			{
				long itemId = _masterRecordList.GetId(item);
				foreach (int index in RemoveId(itemId))
				{
					OnItemDeleted(index);
				}
			}
		}

		private void Sort()
		{
			// The reset event can be raised when items are cleared and also when sorting or filtering occurs
			if (_masterRecordList.Count == 0)
			{
				_keyIdMap.Clear();
				_idKeyMap.Clear();
			}
			else
			{
				_keyIdMap.Sort(_keyIdSorter);
				_idKeyMap.Sort(_idKeySorter);
			}
			OnListReset();
			if (!this._masterRecordList.DelayWritingCachesUntilDispose)
			{
				Serialize();
			}
		}

		#region IBindingList Members

		public void AddIndex(PropertyDescriptor property)
		{
			VerifyNotDisposed();
			throw new NotSupportedException();
		}

		public object AddNew()
		{
			VerifyNotDisposed();
			throw new NotSupportedException();
		}

		public bool AllowEdit
		{
			get
			{
				VerifyNotDisposed();
				return false;
			}
		}

		public bool AllowNew
		{
			get
			{
				VerifyNotDisposed();
				return false;
			}
		}

		public bool AllowRemove
		{
			get
			{
				VerifyNotDisposed();
				return false;
			}
		}

		public void ApplySort(PropertyDescriptor property, ListSortDirection direction)
		{
			VerifyNotDisposed();
			throw new NotSupportedException();
		}

		public int Find(PropertyDescriptor property, object key)
		{
			VerifyNotDisposed();
			throw new NotSupportedException();
		}

		/// <summary>
		/// Returns the first row where the key is equal
		/// </summary>
		/// <param name="key"></param>
		/// <returns>The zero based index of item if found; otherwise a negative number
		/// that is the bitwise complement of the index of the next element that is larger than item
		/// or if there is no larger element, the bitwise complement of Count.</returns>
		public int BinarySearch(K key)
		{
			VerifyNotDisposed();
			KeyValuePair<K, long> keyIdPair = new KeyValuePair<K, long>(key, 0);

			int index = _keyIdMap.BinarySearch(keyIdPair, _keyIdSorter);
			if (index < 0 && ~index !=_keyIdMap.Count)
			{
				if (_sortHelper.KeyComparer.Compare(_keyIdMap[~index].Key, key) == 0)
				{
					index = ~index;
				}
			}

			return index;
		}

		public bool IsSorted
		{
			get
			{
				VerifyNotDisposed();
				return true;
			}
		}

		protected virtual void OnItemAdded(int newIndex)
		{
			Debug.Assert(this[newIndex] != null);
			OnListChanged(new ListChangedEventArgs(ListChangedType.ItemAdded, newIndex));
		}
		protected virtual void OnItemMoved(int newIndex, int oldIndex)
		{
			OnListChanged(new ListChangedEventArgs(ListChangedType.ItemMoved, newIndex, oldIndex));
		}
		protected virtual void OnItemChanged(int newIndex)
		{
			OnListChanged(new ListChangedEventArgs(ListChangedType.ItemChanged, newIndex));
		}

		protected virtual void OnItemDeleted(int oldIndex)
		{
			OnListChanged(new ListChangedEventArgs(ListChangedType.ItemDeleted, oldIndex));
		}

		protected virtual void OnListReset()
		{
			OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
		}

		protected virtual void OnListChanged(ListChangedEventArgs e)
		{
			ListChanged(this, e);
		}

		public event ListChangedEventHandler ListChanged = delegate
														   {
														   };

		public void RemoveIndex(PropertyDescriptor property)
		{
			VerifyNotDisposed();
			throw new NotSupportedException();
		}

		public void RemoveSort()
		{
			VerifyNotDisposed();
			throw new NotSupportedException();
		}

		public ListSortDirection SortDirection
		{
			get
			{
				VerifyNotDisposed();
				throw new NotSupportedException();
			}
		}

		public PropertyDescriptor SortProperty
		{
			get
			{
				VerifyNotDisposed();
				throw new NotSupportedException();
			}
		}

		public bool SupportsChangeNotification
		{
			get
			{
				VerifyNotDisposed();
				return true;
			}
		}

		public bool SupportsSearching
		{
			get
			{
				VerifyNotDisposed();
				return false;
			}
		}

		public bool SupportsSorting
		{
			get
			{
				VerifyNotDisposed();
				return false;
			}
		}

		#endregion

		#region IList Members

		public int Add(object value)
		{
			VerifyNotDisposed();
			return ((IList)_masterRecordList).Add(value);
		}

		public void Clear()
		{
			VerifyNotDisposed();
			throw new NotSupportedException();
		}

		public bool Contains(object value)
		{
			VerifyNotDisposed();
			throw new NotSupportedException();
		}

		public int IndexOf(object value)
		{
			VerifyNotDisposed();

			if (!(value is T))
			{
				throw new ArgumentException("value must be of type T");
			}

			long itemId = this._masterRecordList.GetId((T)value);

			int idKeyMapIndex;
			int keyIdMapIndex;
			GetKeyValueIndexes(itemId, out keyIdMapIndex, out idKeyMapIndex);
			return keyIdMapIndex;
		}

		public IList<long> GetIds()
		{
			List<long> result = new List<long>();
			foreach (KeyValuePair<K, long> pair in _keyIdMap)
			{
				result.Add(pair.Value);
			}
			return result;
		}

		public void Insert(int index, object value)
		{
			VerifyNotDisposed();
			throw new NotSupportedException();
		}

		public bool IsFixedSize
		{
			get
			{
				VerifyNotDisposed();
				return false;
			}
		}

		public bool IsReadOnly
		{
			get
			{
				VerifyNotDisposed();
				return true;
			}
		}

		public void Remove(object value)
		{
			VerifyNotDisposed();
			((IList)_masterRecordList).Remove(value);
		}

		public void RemoveAt(int index)
		{
			VerifyNotDisposed();
			((IList)_masterRecordList).Remove(GetValue(index));
		}


		// returns the key string. to get the value object use GetValue
		// this is so bindinglistgrid will display the right thing.
		public object this[int index]
		{
			get
			{
				VerifyNotDisposed();
				return GetKey(index);
			}
			set
			{
				VerifyNotDisposed();
				throw new NotSupportedException();
			}
		}

		public K GetKey(int index)
		{
			VerifyNotDisposed();
			return _keyIdMap[index].Key;
		}

		public K GetKeyFromId(long id)
		{
			VerifyNotDisposed();

			int idKeyMapIndex = GetIdKeyMapIndex(id);

			if (idKeyMapIndex < 0)
			{
				throw new ArgumentOutOfRangeException();
			}

			return _idKeyMap[idKeyMapIndex].Key;
		}

		public T GetValue(int index)
		{
			VerifyNotDisposed();
			return _masterRecordList[_masterRecordList.GetIndexFromId(_keyIdMap[index].Value)];
		}

		public long GetId(int index)
		{
			VerifyNotDisposed();
			return _keyIdMap[index].Value;
		}

		#endregion

		#region ICollection Members

		public void CopyTo(Array array, int index)
		{
			VerifyNotDisposed();
			throw new NotSupportedException();
		}

		public int Count
		{
			get
			{
				VerifyNotDisposed();
				return _keyIdMap.Count;
			}
		}

		public bool IsSynchronized
		{
			get
			{
				VerifyNotDisposed();
				return false;
			}
		}

		public object SyncRoot
		{
			get
			{
				VerifyNotDisposed();
				throw new NotSupportedException();
			}
		}

		#endregion

		#region IEnumerable Members        #region IEnumerable Members

		#region Enumerator

		public struct Enumerator : IEnumerator<K>
		{
			CachedSortedDb4oList<K, T> _collection;
			int _index;
			bool _isDisposed;

			public Enumerator(CachedSortedDb4oList<K, T> collection)
			{
				if (collection == null)
				{
					throw new ArgumentNullException();
				}
				_collection = collection;
				_index = -1;
				_isDisposed = false;
			}

			private void CheckValidIndex(int validMinimum)
			{
				if (_index < validMinimum || _index >= _collection.Count)
				{
					throw new InvalidOperationException();
				}
			}

			private void CheckCollectionUnchanged()
			{
				if (!_collection._isEnumerating)
				{
					throw new InvalidOperationException();
				}
			}

			private void VerifyNotDisposed()
			{
				if (_isDisposed)
				{
					throw new ObjectDisposedException("CachedSortedDb4oList<K, T>.Enumerator");
				}
			}

			#region IEnumerator<T> Members

			public K Current
			{
				get
				{
					VerifyNotDisposed();
					CheckValidIndex(0);
					CheckCollectionUnchanged();
					return _collection.GetKey(_index);
				}
			}

			#endregion

			#region IDisposable Members

			void IDisposable.Dispose()
			{
				_collection = null;
			}

			#endregion

			#region IEnumerator Members

			object IEnumerator.Current
			{
				get
				{
					VerifyNotDisposed();
					return Current;
				}
			}

			public bool MoveNext()
			{
				VerifyNotDisposed();
				CheckValidIndex(-1);
				CheckCollectionUnchanged();
				return (++_index < _collection.Count);
			}

			public void Reset()
			{
				VerifyNotDisposed();
				CheckCollectionUnchanged();
				_index = -1;
			}

			#endregion
		}

		private bool _isEnumerating;

		#endregion

		public Enumerator GetEnumerator()
		{
			VerifyNotDisposed();
			_isEnumerating = true;
			return new Enumerator(this);
		}


		IEnumerator IEnumerable.GetEnumerator()
		{
			VerifyNotDisposed();
			return GetEnumerator();
		}


		IEnumerator<K> IEnumerable<K>.GetEnumerator()
		{
			VerifyNotDisposed();
			int count = _keyIdMap.Count;
			for (int i = 0; i < count; i++)
			{
				yield return _keyIdMap[i].Key;
			}
		}
		#endregion
		#region IDisposable Members
#if DEBUG
		~CachedSortedDb4oList()
		{
			if (!this._disposed)
			{
				throw new ApplicationException("Disposed not explicitly called on CachedSortedDb4oList.");
			}
		}
#endif

		private bool _disposed = false;

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!this._disposed)
			{
				if (disposing)
				{
					// dispose-only, i.e. non-finalizable logic
					if (_masterRecordList != null)
					{
						_masterRecordList.ListChanged -= OnMasterRecordListListChanged;
						_masterRecordList.DeletingRecord -= OnMasterRecordListDeletingRecord;
					}
				   Serialize();
				}

				// shared (dispose and finalizable) cleanup logic
				this._disposed = true;
			}
		}

		protected void VerifyNotDisposed()
		{
			if (this._disposed)
			{
				throw new ObjectDisposedException("CachedSortedDb4oList");
			}
		}
		#endregion
	}
}
