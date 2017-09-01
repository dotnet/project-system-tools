using System;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.LogModel
{
    internal sealed class ChildrenList : List<object>
    {
        public ChildrenList() : base(1)
        {
        }

        private Dictionary<ChildrenCacheKey, object> _childrenCache;

        public T FindNode<T>(string name) where T : NodeWithName
        {
            EnsureCacheCreated();

            var key = new ChildrenCacheKey(typeof(T), name);
            if (_childrenCache.TryGetValue(key, out var result))
            {
                return (T) result;
            }

            for (var i = 0; i < Count; i++)
            {
                if (!(this[i] is T t) || t.LookupKey != name)
                {
                    continue;
                }

                _childrenCache[key] = t;
                return t;
            }

            return null;
        }

        private void EnsureCacheCreated()
        {
            if (_childrenCache == null)
            {
                _childrenCache = new Dictionary<ChildrenCacheKey, object>();
            }
        }

        public void OnAdded(NodeWithName child)
        {
            if (child?.LookupKey == null)
            {
                return;
            }

            EnsureCacheCreated();

            var key = new ChildrenCacheKey(child.GetType(), child.LookupKey);
            _childrenCache[key] = child;
        }

        private struct ChildrenCacheKey
        {
            private readonly Type _type;
            private readonly string _name;
            private readonly int _hashCode;

            public ChildrenCacheKey(Type type, string name)
            {
                _type = type;
                _name = name;
                _hashCode = unchecked((_type.GetHashCode() * 397) ^ _name.ToLowerInvariant().GetHashCode());
            }

            private bool Equals(ChildrenCacheKey other) => _type == other._type && string.Equals(_name, other._name, StringComparison.OrdinalIgnoreCase);

            public override bool Equals(object obj) => obj is ChildrenCacheKey key && Equals(key);

            public override int GetHashCode() => _hashCode;
        }
    }
}
