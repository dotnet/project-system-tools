﻿// Copyright (c) Microsoft. All Rights Reserved. Licensed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.LogModel
{
    internal sealed class Project : Node
    {
        public int NodeId { get; }

        public string Name { get; }

        public string ProjectFile { get; }

        public ImmutableDictionary<string, string> GlobalProperties { get; }

        public ImmutableDictionary<string, string> Properties { get; }

        public ImmutableList<ItemGroup> ItemGroups { get; }

        public ImmutableList<Target> Targets { get; }

        public string ToolsVersion { get; }

        public Project(int nodeId, string name, string projectFile, ImmutableDictionary<string, string> globalProperties, ImmutableDictionary<string, string> properties, ImmutableList<ItemGroup> itemGroups, ImmutableList<Target> targets, string toolsVersion, ImmutableList<Message> messages, DateTime startTime, DateTime endTime, Result result)
            : base(messages, startTime, endTime, result)
        {
            NodeId = nodeId;
            Name = name;
            ProjectFile = projectFile;
            GlobalProperties = globalProperties;
            ItemGroups = itemGroups;
            Properties = properties;
            ToolsVersion = toolsVersion;
            Targets = targets;
        }
    }
}
