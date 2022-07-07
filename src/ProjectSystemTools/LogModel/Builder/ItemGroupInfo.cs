// Copyright (c) Microsoft. All Rights Reserved. Licensed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.LogModel.Builder
{
    internal sealed class ItemGroupInfo
    {
        public string Name { get; }
        public ImmutableList<ItemInfo> Items { get; }

        public ItemGroupInfo(string name, ImmutableList<ItemInfo> items)
        {
            Name = name;
            Items = items;
        }
    }
}
