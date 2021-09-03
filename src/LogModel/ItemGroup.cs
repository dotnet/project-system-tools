// Copyright (c) Microsoft. All Rights Reserved. Licensed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem.LogModel
{
    public sealed class ItemGroup
    {
        public string Name { get; }
        public ImmutableList<Item> Items { get; }

        public ItemGroup(string name, ImmutableList<Item> items)
        {
            Name = name;
            Items = items;
        }
    }
}
