// Copyright (c) Microsoft. All Rights Reserved. Licensed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Microsoft.VisualStudio.ProjectSystem.LogModel
{
    public sealed class ItemAction
    {
        public bool IsAddition { get; }
        public ItemGroup ItemGroup { get; }
        public DateTime Time { get; }

        public ItemAction(bool isAddition, ItemGroup itemGroup, DateTime time)
        {
            IsAddition = isAddition;
            ItemGroup = itemGroup;
            Time = time;
        }
    }
}
