namespace Microsoft.VisualStudio.ProjectSystem.Tools.LogModel
{
    internal abstract class Node
    {
        private NodeWithName _parent;

        public NodeWithName Parent
        {
            get => _parent;
            set
            {
#if DEBUG
                if (_parent != null)
                {
                    throw new System.InvalidOperationException("A node is being reparented");
                }
#endif

                _parent = value;
            }
        }

        public T GetNearestParent<T>() where T : Node
        {
            var current = this;
            while (current.Parent != null)
            {
                current = current.Parent;
                if (current is T value)
                {
                    return value;
                }
            }

            return null;
        }
    }
}
