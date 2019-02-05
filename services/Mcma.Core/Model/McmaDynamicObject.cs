using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq.Expressions;

namespace Mcma.Core
{
    public abstract class McmaDynamicObject : IDictionary<string, object>, IMcmaObject, IDynamicMetaObjectProvider
    {
        protected McmaDynamicObject()
        {
            Type = GetType().Name;
        }

        public string Type { get; set; }

        public T Get<T>(string key) => PropertyDictionary.ContainsKey(key) ? (T)PropertyDictionary[key] : default(T);
        
        public bool TryGet<T>(string key, out T value)
        {
            value = default(T);
            if (PropertyDictionary.ContainsKey(key))
            {
                value = (T)PropertyDictionary[key];
                return true;
            }
            Console.WriteLine($"Failed to find key '{key}' in dynamic object of type {GetType().Name}. Existing keys are {string.Join(", ", PropertyDictionary.Keys)}.");
            return false;
        }

        #region Dictionary & Dynamic Implementations

        private ExpandoObject ExpandoObject { get; } = new ExpandoObject();

        private IDictionary<string, object> PropertyDictionary => ExpandoObject;

        public object this[string key] { get => PropertyDictionary[key]; set => PropertyDictionary[key] = value; }

        public ICollection<string> Keys => PropertyDictionary.Keys;

        public ICollection<object> Values => PropertyDictionary.Values;

        public int Count => PropertyDictionary.Count;

        public bool IsReadOnly => PropertyDictionary.IsReadOnly;

        public void Add(string key, object value) => PropertyDictionary.Add(key, value);

        public void Add(KeyValuePair<string, object> item) => PropertyDictionary.Add(item);
        
        public void Clear() => PropertyDictionary.Clear();

        public bool Contains(KeyValuePair<string, object> item) => PropertyDictionary.Contains(item);

        public bool ContainsKey(string key) => PropertyDictionary.ContainsKey(key);

        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex) => PropertyDictionary.CopyTo(array, arrayIndex);

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => PropertyDictionary.GetEnumerator();

        public bool Remove(string key) => PropertyDictionary.Remove(key);

        public bool Remove(KeyValuePair<string, object> item) => PropertyDictionary.Remove(item);

        public bool TryGetValue(string key, out object value) => PropertyDictionary.TryGetValue(key, out value);

        IEnumerator IEnumerable.GetEnumerator() => PropertyDictionary.GetEnumerator();

        public DynamicMetaObject GetMetaObject(Expression parameter) => ((IDynamicMetaObjectProvider)ExpandoObject).GetMetaObject(parameter);

        #endregion
    }
}