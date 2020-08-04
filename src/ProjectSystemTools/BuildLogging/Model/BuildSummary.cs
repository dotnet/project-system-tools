// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.Model
{
    /// <summary>
    /// Immutable Type
    /// </summary>
    public sealed class BuildSummary
    {
        public int BuildId { get; }

        public BuildType BuildType { get; }

        public ImmutableArray<string> Dimensions { get; }

        public ImmutableArray<string> Targets { get; }

        public DateTime StartTime { get; }

        public TimeSpan Elapsed { get; }

        public BuildStatus Status { get; }

        public string ProjectName { get; }

        public BuildSummary(int buildId, string projectPath, IEnumerable<string> dimensions, IEnumerable<string> targets, BuildType buildType, DateTime startTime)
        {
            BuildId = buildId;
            ProjectName = Path.GetFileName(projectPath);
            Dimensions = dimensions.ToImmutableArray();
            Targets = targets?.ToImmutableArray() ?? ImmutableArray<string>.Empty;
            BuildType = buildType;
            StartTime = startTime;
            Status = BuildStatus.Running;
        }
        public BuildSummary(BuildSummary other, BuildStatus status, TimeSpan elapsed) {
            BuildId = other.BuildId;
            BuildType = other.BuildType;
            Dimensions = other.Dimensions;
            Targets = other.Targets;
            StartTime = other.StartTime;
            ProjectName = other.ProjectName;
            Elapsed = elapsed;
            Status = status;
        }
    }
}
