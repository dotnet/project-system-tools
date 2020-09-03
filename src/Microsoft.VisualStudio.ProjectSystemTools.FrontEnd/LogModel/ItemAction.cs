// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.ProjectSystemTools.FrontEnd
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
