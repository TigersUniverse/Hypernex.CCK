using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Hypernex.CCK.Unity.Internals
{
    [Serializable]
    public class SerializedDictionary<TKey, TValue> : ISerializationCallbackReceiver, IEnumerable<KeyValuePair<TKey, TValue>>
    {
        private Dictionary<TKey, TValue> dic = new Dictionary<TKey, TValue>();
        [SerializeField] private List<TKey> keys = new List<TKey>();
        [SerializeField] private List<TValue> values = new List<TValue>();

        public SerializedDictionary(){}
        public SerializedDictionary(IDictionary<TKey, TValue> i) => dic = new Dictionary<TKey, TValue>(i);

        public SerializedDictionary(IList<TKey> k, IList<TValue> v)
        {
            if (k.Count != v.Count)
            {
                Debug.LogWarning("IKeys and IValues have different counts!");
                return;
            }
            for(int i = 0; i < k.Count; i++)
                dic.Add(k.ElementAt(i), v.ElementAt(i));
        }

        public TValue this[TKey i]
        {
            get => dic[i];
            set => dic[i] = value;
        }

        public Dictionary<TKey, TValue>.KeyCollection Keys => dic.Keys;
        public Dictionary<TKey, TValue>.ValueCollection Values => dic.Values;

        public void Add(TKey key, TValue value) => dic.Add(key, value);
        public void Remove(TKey key) => dic.Remove(key);
        public bool ContainsKey(TKey key) => dic.ContainsKey(key);
        public bool ContainsValue(TValue value) => dic.ContainsValue(value);
        
        public void OnBeforeSerialize()
        {
            keys.Clear();
            values.Clear();
            foreach (KeyValuePair<TKey,TValue> keyValuePair in dic)
            {
                keys.Add(keyValuePair.Key);
                values.Add(keyValuePair.Value);
            }
        }

        public void OnAfterDeserialize()
        {
            dic = new Dictionary<TKey, TValue>();
            if (keys.Count != values.Count)
                throw new Exception("Keys and Values have different counts!");
            for(int i = 0; i < keys.Count; i++)
                dic.Add(keys.ElementAt(i), values.ElementAt(i));
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => dic.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}