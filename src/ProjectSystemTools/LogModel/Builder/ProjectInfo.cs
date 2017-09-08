// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.LogModel.Builder
{
    internal sealed class ProjectInfo
    {
        public string Name;
        public int NodeId;
        public int ParentProject;
        public DateTime StartTime;
        public DateTime EndTime;
        public string ProjectFile;
        public ImmutableDictionary<string, string> GlobalProperties;
        public ImmutableDictionary<string, string> Properties;
        public List<ItemGroupInfo> ItemGroups;
        public List<TargetInfo> SkippedTargets = new List<TargetInfo>();
        public bool Succeeded;
        public List<string> Messages = new List<string>();
        public string[] TargetNames;
        public string ToolsVersion;
        public Dictionary<int, TargetInfo> TargetInfos = new Dictionary<int, TargetInfo>();
    }
}
