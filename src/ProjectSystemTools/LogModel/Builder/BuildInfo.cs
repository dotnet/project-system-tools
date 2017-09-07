// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.LogModel.Builder
{
    internal sealed class BuildInfo
    {
        public DateTime StartTime;
        public DateTime EndTime;
        public ImmutableDictionary<string, string> Environment;
        public bool Succeeded;
        public readonly Dictionary<int, EvaluatedProjectInfo> EvaluatedProjects = new Dictionary<int, EvaluatedProjectInfo>();
        public List<string> Messages = new List<string>();
    }
}
