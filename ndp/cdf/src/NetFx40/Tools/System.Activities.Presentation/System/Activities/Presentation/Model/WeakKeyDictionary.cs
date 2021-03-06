//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------
namespace System.Activities.Presentation.Model
{

    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Text;
    using System.Threading;
    using System.Runtime.InteropServices;

    internal class WeakKeyDictionary<K, V> : IDictionary<K, V> 
    {
        private Dictionary<WeakKey, V> _internalDictionary;
        private object _sync = new object();
        private bool _finalized;

        internal WeakKeyDictionary() 
        {
            _internalDictionary = new Dictionary<WeakKey, V>(new WeakComparer());
        }

        public WeakKeyDictionary(IEqualityComparer<K> comparer)  
        {
            _internalDictionary = new Dictionary<WeakKey, V>(new WeakComparer(comparer));
        }

        // FXCop: this is not empty; we need to mark this so we know if a key
        // still has an active dictionary at its finalization.
        [SuppressMessage("Microsoft.Performance", "CA1821:RemoveEmptyFinalizers")]
        ~WeakKeyDictionary() 
        {
            _finalized = true;
        }

        public ICollection<K> Keys 
        {
            get 
            {
                List<K> list = new List<K>();
                lock (_sync) 
                {
                    foreach (WeakKey key in _internalDictionary.Keys) 
                    {
                        object k = key.Target;
                        if (k != null)
                        {
                            list.Add((K)k);
                        }
                    }
                }
                return list;
            }
        }

        public ICollection<V> Values 
        {
            get {
                lock (_sync) {
                    // make a copy of the values, so the during GC, the returned collection does not change.
                    return new List<V>(_internalDictionary.Values);
                }
            }
        }

        public int Count 
        {
            get 
            {
                // Ensure a fairly accurate count.
                ScavangeLostKeys();
                lock (_sync) 
                {
                    return _internalDictionary.Count;
                }
            }
        }

        public bool IsReadOnly 
        {
            get {
                return false;
            }
        }

        [SuppressMessage("Microsoft.Usage", "CA1806:DoNotIgnoreMethodResults", Justification = "LostKeyFinder's purpose is to get garbage collected as soon as posible")]
        public V this[K key] 
        {
            get {
                lock (_sync) {
                    return _internalDictionary[new WeakKey(key)];
                }
            }
            set 
            {
                WeakKey k = new WeakKey(key);
                lock (_sync) 
                {
                    _internalDictionary[k] = value;
                }
                // This looks a bit weird but the purpose of the lost key finder is to execute
                // code in some future garbage collection phase so we immediately create some garbage.
                new LostKeyFinder(this, k);
            }
        }

        public bool TryGetValue(K key, out V value) 
        {
            WeakKey k = new WeakKey(key);
            lock (_sync) 
            {
                return _internalDictionary.TryGetValue(k, out value);
            }
        }

        [SuppressMessage("Microsoft.Usage", "CA1806:DoNotIgnoreMethodResults", Justification = "LostKeyFinder's purpose is to get garbage collected as soon as posible")]
        public void Add(K key, V value) 
        {
            WeakKey k = new WeakKey(key);
            lock (_sync) 
            {
                _internalDictionary.Add(k, value);
            }
            // This looks a bit weird but the purpose of the lost key finder is to execute
            // code in some future garbage collection phase so we immediately create some garbage.
            new LostKeyFinder(this, k);

        }

        public bool ContainsKey(K key) 
        {
            return _internalDictionary.ContainsKey(new WeakKey(key));
        }

        public bool Remove(K key) 
        {
            lock (_sync) 
            {
                return _internalDictionary.Remove(new WeakKey(key));
            }
        }

        public void Add(KeyValuePair<K, V> item) 
        {
            Add(item.Key, item.Value);
        }

        public void Clear() 
        {
            lock (_sync) 
            {
                _internalDictionary.Clear();
            }
        }

        public bool Contains(KeyValuePair<K, V> item) 
        {
            V value;
            bool result;
            lock (_sync) 
            {
                result = _internalDictionary.TryGetValue(new WeakKey(item.Key), out value);
            }
            if (result)
            {
                return value.Equals(item.Value);
            }
            else
            {
                return false;
            }
        }

        public void CopyTo(KeyValuePair<K, V>[] array, int arrayIndex) 
        {
            lock (_sync) 
            {
                foreach (KeyValuePair<WeakKey, V> item in _internalDictionary) 
                {
                    KeyValuePair<K, V> kv = new KeyValuePair<K, V>((K)item.Key.Target, item.Value);
                    array[arrayIndex] = kv;
                    arrayIndex++;
                }
            }
        }

        public bool Remove(KeyValuePair<K, V> item) 
        {
            WeakKey key = new WeakKey(item.Key);
            lock (_sync) 
            {
                return _internalDictionary.Remove(key);
            }
        }





        public IEnumerator<KeyValuePair<K, V>> GetEnumerator() 
        {
            List<WeakKey> lostKeys = null;
            lock (_sync) 
            {
                foreach (KeyValuePair<WeakKey, V> item in _internalDictionary) 
                {
                    object k = item.Key.Target;
                    if (k != null)
                    {
                        yield return new KeyValuePair<K, V>((K)k, item.Value);
                    }
                    else 
                    {
                        if (lostKeys == null)
                        {
                            lostKeys = new List<WeakKey>();
                        }
                        lostKeys.Add(item.Key);
                    }
                }
            }
            // Recover any lost keys.
            if (lostKeys != null)
            {
                lock (_sync) 
                {
                    foreach (WeakKey key in lostKeys)
                    {
                        _internalDictionary.Remove(key);
                    }
                }
            }
        }




        IEnumerator IEnumerable.GetEnumerator() 
        {
            return GetEnumerator();
        }



        private void ScavangeLostKeys() 
        {
            List<WeakKey> lostKeys = null;
            lock (_sync) 
            {
                foreach (WeakKey key in _internalDictionary.Keys)
                {
                    if (!key.IsAlive) 
                    {
                        if (lostKeys == null)
                        {
                            lostKeys = new List<WeakKey>();
                        }
                        lostKeys.Add(key);
                    }
                }
            }
            if (lostKeys != null)
            {
                lock (_sync) 
                {
                    foreach (WeakKey key in lostKeys)
                    {
                        _internalDictionary.Remove(key);
                    }
                }
            }
        }

        private class WeakKey : WeakReference 
        {
            private int _hashCode;
            // private GCHandle _gcHandle;

            public WeakKey(K key)
                : base(key, true) 
            {
                _hashCode = key.GetHashCode();
                // Keep the key alive until it is explicitly collected
                // _gcHandle = GCHandle.Alloc(this);
            }

            internal void Release() 
            {
                // _gcHandle.Free();
            }

            public override int GetHashCode() 
            {
                return _hashCode;
            }

            public override bool Equals(object obj) 
            {
                if (obj == null)
                {
                    return false;
                }
                if (obj.GetHashCode() != _hashCode)
                {
                    return false;
                }
                if (obj != this && (!IsAlive || !obj.Equals(Target)))
                {
                    return false;
                }
                return true;
            }
        }

        private class WeakComparer : IEqualityComparer<WeakKey> 
        {

            private IEqualityComparer<K> _comparer;
            public WeakComparer() 
            {
            }

            public WeakComparer(IEqualityComparer<K> comparer) 
            {
                _comparer = comparer;
            }

            public bool Equals(WeakKey x, WeakKey y) 
            {
                if (x.GetHashCode() != y.GetHashCode())
                {
                    return false;
                }
                if (object.ReferenceEquals(x, y))
                {
                    return true;
                }
                object ref1 = x.Target;
                if (ref1 == null)
                {
                    return false;
                }
                object ref2 = y.Target;
                if (ref2 == null)
                {
                    return false;
                }

                if (_comparer != null) 
                {
                    return _comparer.Equals((K)ref1, (K)ref2);
                }
                else 
                {
                    return ref1.Equals(ref2);
                }
            }

            public int GetHashCode(WeakKey obj) 
            {
                return obj.GetHashCode();
            }
        }

        private class LostKeyFinder 
        {
            WeakKeyDictionary<K, V> _dictionary;
            WeakKey _key;

            public LostKeyFinder(WeakKeyDictionary<K, V> dictionary, WeakKey key) 
            {
                _dictionary = dictionary;
                _key = key;
            }

            ~LostKeyFinder() 
            {
                if (_dictionary._finalized || _key == null) 
                {
                    if (_key != null) 
                    {
                        _key.Release();
                        _key = null;
                    }
                    return;
                }
                // if (!_key.IsAlive) {
                if (_key.Target == null) 
                {
                    bool locked = false;
                    try
                    {
                        locked = Monitor.TryEnter(_dictionary._sync);
                        _dictionary._internalDictionary.Remove(_key);
                    }
                    finally
                    {
                        if (locked)
                        {
                            Monitor.Exit(_dictionary._sync);
                        }
                    }

                    if (locked)
                    {
                        _key.Release();
                        _key = null;
                    }
                    else
                    {
                        GC.ReRegisterForFinalize(this);
                    }
                }
                else if (_dictionary._internalDictionary.ContainsKey(_key))
                {
                    GC.ReRegisterForFinalize(this);
                }
            }
        }
    }
}
