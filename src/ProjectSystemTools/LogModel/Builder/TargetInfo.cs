// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.LogModel.Builder
{
    internal sealed class TargetInfo
    {
        public string Name;
        public string SourceFilePath;
        public int NodeId;
        public int ParentProject;
        public string ParentTarget;
        public DateTime StartTime;
        public DateTime EndTime;
        public bool Succeeded;
        public ItemGroupInfo OutputItems;
        public List<ItemGroupInfo> ItemGroups = new List<ItemGroupInfo>();
        public List<KeyValuePair<string, string>> Properties = new List<KeyValuePair<string, string>>();
        public List<string> Messages = new List<string>();
        public Dictionary<int, TaskInfo> TaskInfos = new Dictionary<int, TaskInfo>();
    }
}
