﻿// Copyright (c) Microsoft. All Rights Reserved. Licensed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.LogModel
{
    internal class Task : Node
    {
        public int NodeId { get; }
        public string Name { get; }
        public string? FromAssembly { get; }
        public string? CommandLineArguments { get; }
        public string? SourceFilePath { get; }
        public ImmutableList<Project> ChildProjects { get; }
        public ImmutableList<ItemGroup> ParameterItems { get; }
        public ImmutableDictionary<string, string> ParameterProperties { get; }
        public ImmutableList<ItemGroup> OutputItems { get; }
        public ImmutableDictionary<string, string> OutputProperties { get; }

        public Task(int nodeId, string name, string? fromAssembly, string? commandLineArguments, string? sourceFilePath, ImmutableList<Project> childProjects, ImmutableList<ItemGroup> parameterItems, ImmutableDictionary<string, string> parameterProperties, ImmutableList<ItemGroup> outputItems, ImmutableDictionary<string, string> outputProperties, DateTime startTime, DateTime endTime, ImmutableList<Message> messages, Result result)
            : base(messages, startTime, endTime, result)
        {
            NodeId = nodeId;
            Name = name;
            FromAssembly = fromAssembly;
            CommandLineArguments = commandLineArguments;
            SourceFilePath = sourceFilePath;
            ChildProjects = childProjects;
            ParameterItems = parameterItems;
            ParameterProperties = parameterProperties;
            OutputItems = outputItems;
            OutputProperties = outputProperties;
        }
    }
}
