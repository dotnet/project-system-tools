// Copyright (c) Microsoft. All Rights Reserved. Licensed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Microsoft.VisualStudio.ProjectSystem.LogModel.Builder
{
    internal sealed class ItemActionInfo : BaseInfo
    {
        public bool IsAddition { get; }
        public ItemGroupInfo ItemGroup { get; }
        public DateTime Time { get; }

        public ItemActionInfo(bool isAddition, ItemGroupInfo itemGroup, DateTime time)
        {
            IsAddition = isAddition;
            ItemGroup = itemGroup;
            Time = time;
        }
    }
}
