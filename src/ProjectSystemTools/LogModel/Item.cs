// Copyright (c) Microsoft. All Rights Reserved. Licensed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.LogModel
{
    internal sealed class Item
    {
        public string Name { get; }

        public ImmutableDictionary<string, string> Metadata { get; }

        public Item(string name, ImmutableDictionary<string, string> metadata)
        {
            Name = name;
            Metadata = metadata;
        }
    }
}
