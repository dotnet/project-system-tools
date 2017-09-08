using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.LogModel
{
    internal sealed class ItemGroup
    {
        public string Name { get; }
        public ItemGroupType Type { get; }
        public ImmutableList<Item> Items { get; }

        public ItemGroup(string name, ItemGroupType type, ImmutableList<Item> items)
        {
            Name = name;
            Type = type;
            Items = items;
        }
    }
}
