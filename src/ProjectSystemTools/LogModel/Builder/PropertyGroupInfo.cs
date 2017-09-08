﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.LogModel.Builder
{
    internal sealed class PropertyGroupInfo
    {
        public string Name;
        public Dictionary<string, string> Properties = new Dictionary<string, string>();
    }
}
