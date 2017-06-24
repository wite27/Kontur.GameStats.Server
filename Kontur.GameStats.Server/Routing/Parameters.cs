using System.Collections;
using System.Collections.Generic;

namespace Kontur.GameStats.Server.Routing
{
    public class Parameters : IDictionary<string, object>
    {
        private Dictionary<string, object> map;
        public Dictionary<string, object> Map
        {
            get { return map; }
            private set { map = value; }
        }
        public Parameters()
        {
            this.map = new Dictionary<string, object>();
        }
        public Parameters(string token, object value) : this()
        {
            Add(token, value);
        }
        public Parameters(Dictionary<string, object> map) : this()
        {
            foreach (var pair in map)
            {
                Add(pair);
            }
        }
        #region IDictionary<string, object> implementation

        public ICollection<string> Keys
        {
            get
            {
                return ((IDictionary<string, object>)Map).Keys;
            }
        }

        public ICollection<object> Values
        {
            get
            {
                return ((IDictionary<string, object>)Map).Values;
            }
        }

        public int Count
        {
            get
            {
                return ((IDictionary<string, object>)Map).Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return ((IDictionary<string, object>)Map).IsReadOnly;
            }
        }

        public object this[string key]
        {
            get
            {
                return ((IDictionary<string, object>)Map)[key];
            }

            set
            {
                ((IDictionary<string, object>)Map)[key] = value;
            }
        }

        public bool ContainsKey(string key)
        {
            return ((IDictionary<string, object>)Map).ContainsKey(key);
        }

        public void Add(string key, object value)
        {
            ((IDictionary<string, object>)Map).Add(key, value);
        }

        public bool Remove(string key)
        {
            return ((IDictionary<string, object>)Map).Remove(key);
        }

        public bool TryGetValue(string key, out object value)
        {
            return ((IDictionary<string, object>)Map).TryGetValue(key, out value);
        }

        public void Add(KeyValuePair<string, object> item)
        {
            ((IDictionary<string, object>)Map).Add(item);
        }

        public void Clear()
        {
            ((IDictionary<string, object>)Map).Clear();
        }

        public bool Contains(KeyValuePair<string, object> item)
        {
            return ((IDictionary<string, object>)Map).Contains(item);
        }

        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            ((IDictionary<string, object>)Map).CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<string, object> item)
        {
            return ((IDictionary<string, object>)Map).Remove(item);
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return ((IDictionary<string, object>)Map).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IDictionary<string, object>)Map).GetEnumerator();
        }

        #endregion
    }
}
