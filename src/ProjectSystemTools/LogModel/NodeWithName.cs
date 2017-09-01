using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.LogModel
{
    internal abstract class NodeWithName : Node
    {
        private IList<object> _children;

        public string Name { get; set; }

        public virtual string LookupKey => Name;

        public bool HasChildren => _children != null && _children.Count > 0;

        public IList<object> Children => _children ?? (_children = new ChildrenList());

        public virtual void AddChild(object child)
        {
            if (_children == null)
            {
                _children = new ChildrenList();
            }

            _children.Add(child);
            if (child is NodeWithName named)
            {
                ((ChildrenList)_children).OnAdded(named);
            }

            if (child is Node treeNode)
            {
                treeNode.Parent = this;
            }
        }

        public T GetOrCreateNodeWithName<T>(string name) where T : NodeWithName, new()
        {
            var node = FindChild<T>(name);
            if (node != null)
            {
                return node;
            }

            var newNode = new T { Name = name };
            AddChild(newNode);
            return newNode;
        }

        public virtual T FindChild<T>(string name) where T : NodeWithName
        {
            if (Children is ChildrenList list)
            {
                return list.FindNode<T>(name);
            }

            return FindChild<T>(c => string.Equals(c.LookupKey, name, StringComparison.OrdinalIgnoreCase));
        }

        public virtual T FindChild<T>(Predicate<T> predicate)
        {
            if (!HasChildren)
            {
                return default(T);
            }

            foreach (var t in Children)
            {
                if (t is T child && predicate(child))
                {
                    return child;
                }
            }

            return default(T);
        }

        public virtual T FindFirstInSubtreeIncludingSelf<T>(Predicate<T> predicate = null)
        {
            if (this is T && (predicate == null || predicate((T)(object)this)))
            {
                return (T)(object)this;
            }

            return FindFirstDescendant(predicate);
        }

        public virtual T FindFirstDescendant<T>(Predicate<T> predicate = null)
        {
            if (!HasChildren)
            {
                return default(T);
            }

            foreach (var child in Children)
            {
                switch (child)
                {
                    case NodeWithName treeNode:
                        var found = treeNode.FindFirstInSubtreeIncludingSelf(predicate);
                        if (found != null)
                        {
                            return found;
                        }
                        break;
                    case T _ when predicate == null || predicate((T)child):
                        return (T)child;
                }
            }

            return default(T);
        }

        public virtual T FindLastInSubtreeIncludingSelf<T>(Predicate<T> predicate = null)
        {
            var child = FindLastDescendant(predicate);
            if (child != null)
            {
                return child;
            }

            if (this is T && (predicate == null || predicate((T)(object)this)))
            {
                return (T)(object)this;
            }

            return default(T);
        }

        public virtual T FindLastChild<T>()
        {
            if (HasChildren)
            {
                for (var i = Children.Count - 1; i >= 0; i--)
                {
                    if (Children[i] is T t)
                    {
                        return t;
                    }
                }
            }

            return default(T);
        }

        public virtual T FindLastDescendant<T>(Predicate<T> predicate = null)
        {
            if (HasChildren)
            {
                foreach (var child in Children.Reverse())
                {
                    switch (child)
                    {
                        case NodeWithName treeNode:
                            var found = treeNode.FindLastInSubtreeIncludingSelf(predicate);
                            if (found != null)
                            {
                                return found;
                            }
                            break;
                        case T _ when predicate == null || predicate((T)child):
                            return (T)child;
                    }
                }
            }

            return default(T);
        }

        public int FindChildIndex(object child)
        {
            if (HasChildren)
            {
                for (var i = 0; i < Children.Count; i++)
                {
                    if (Children[i] == child)
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        public T FindPreviousChild<T>(object currentChild, Predicate<T> predicate = null)
        {
            var i = FindChildIndex(currentChild);
            if (i == -1)
            {
                return default(T);
            }

            for (var j = i - 1; j >= 0; j--)
            {
                if (Children[j] is T && (predicate == null || predicate((T)Children[j])))
                {
                    return (T)Children[j];
                }
            }

            return default(T);
        }

        public T FindNextChild<T>(object currentChild, Predicate<T> predicate = null)
        {
            var i = FindChildIndex(currentChild);
            if (i == -1)
            {
                return default(T);
            }

            for (var j = i + 1; j < Children.Count; j++)
            {
                if (Children[j] is T && (predicate == null || predicate((T)Children[j])))
                {
                    return (T)Children[j];
                }
            }

            return default(T);
        }

        public T FindPreviousInTraversalOrder<T>(Predicate<T> predicate = null)
        {
            if (Parent == null)
            {
                return default(T);
            }

            var current = Parent.FindPreviousChild<T>(this);

            while (current != null)
            {
                var last = current;

                var treeNode = current as NodeWithName;
                if (treeNode != null)
                {
                    last = treeNode.FindLastInSubtreeIncludingSelf(predicate);
                }

                if (last != null)
                {
                    return last;
                }

                if (Parent != null)
                {
                    current = Parent.FindPreviousChild<T>(current);
                }
                else
                {
                    // no parent and no previous; we must be at the top
                    return default(T);
                }
            }

            if (Parent is T && (predicate == null || predicate((T)(object)Parent)))
            {
                return (T)(object)Parent;
            }

            return Parent.FindPreviousInTraversalOrder(predicate);
        }

        public T FindNextInTraversalOrder<T>(Predicate<T> predicate = null)
        {
            if (Parent == null)
            {
                return default(T);
            }

            var current = Parent.FindNextChild<T>(this);

            while (current != null)
            {
                var first = current;

                var treeNode = current as NodeWithName;
                if (treeNode != null)
                {
                    first = treeNode.FindFirstInSubtreeIncludingSelf(predicate);
                }

                if (first != null)
                {
                    return first;
                }

                if (Parent != null)
                {
                    current = Parent.FindNextChild<T>(current);
                }
                else
                {
                    return default(T);
                }
            }

            if (Parent != null)
            {
                return Parent.FindNextInTraversalOrder(predicate);
            }

            return default(T);
        }

        public void VisitAllChildren<T>(Action<T> processor, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            if (this is T)
            {
                processor((T)(object)this);
            }

            if (HasChildren)
            {
                foreach (var child in Children)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    switch (child)
                    {
                        case NodeWithName node:
                            node.VisitAllChildren(processor, cancellationToken);
                            break;
                        case T _:
                            processor((T)child);
                            break;
                    }
                }
            }
        }

        public override string ToString() => Name;
    }
}
