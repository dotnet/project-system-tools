// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.LogModel.Builder
{
    internal sealed class TaskInfo
    {
        public string Name;
        public int Id;
        public int NodeId;
        public int ParentProject;
        public int ParentTarget;
        public DateTime EndTime;
        public DateTime StartTime;
        public string FromAssembly;
        public string SourceFilePath;
        public List<ItemGroupInfo> OutputItems = new List<ItemGroupInfo>();
        public Dictionary<string, string> OutputProperties = new Dictionary<string, string>();
        public List<ParameterInfo> Parameters = new List<ParameterInfo>();
        public string CommandLineArguments;
        public List<ParameterInfo> Results = new List<ParameterInfo>();
        public List<ParameterInfo> Inputs = new List<ParameterInfo>();
        public List<PropertyGroupInfo> PropertyGroups = new List<PropertyGroupInfo>();
        public bool Succeeded;
        public List<string> Messages = new List<string>();
    }
}
