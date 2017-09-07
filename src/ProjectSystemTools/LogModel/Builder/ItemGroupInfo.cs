// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.LogModel.Builder
{
    internal sealed class ItemGroupInfo
    {
        public string Name;
        public ItemGroupType Type;
        public List<ItemInfo> Items = new List<ItemInfo>();
    }
}
