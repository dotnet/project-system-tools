// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.ProjectSystem.Tools.Providers;
using Microsoft.VisualStudio.ProjectSystem.Tools.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.Model.Frontend
{
    /// <summary>
    /// Immutable Type
    /// </summary>
    public sealed class UIBuildSummary : IComparable<UIBuildSummary>
    {
        public int BuildId { get; }
        public BuildType BuildType { get; }

        public IEnumerable<string> Dimensions { get; }

        public IEnumerable<string> Targets { get; }

        public DateTime StartTime { get; }

        public TimeSpan Elapsed { get; }

        public BuildStatus Status { get; }

        public string ProjectPath { get; }

        public UIBuildSummary(BuildSummary other)
        {
            BuildId = other.BuildId;
            BuildType = other.BuildType;
            Dimensions = other.Dimensions;
            Targets = other.Targets;
            StartTime = other.StartTime;
            ProjectPath = other.ProjectPath;
            Elapsed = other.Elapsed;
            Status = other.Status;
        }

        public bool TryGetValue(string keyName, out object content)
        {
            content = keyName switch
            {
                TableKeyNames.Dimensions => Dimensions,
                TableKeyNames.Targets => Targets,
                TableKeyNames.Elapsed => Elapsed,
                TableKeyNames.BuildType => BuildType,
                TableKeyNames.Status => Status,
                StandardTableKeyNames.ProjectName => Path.GetFileNameWithoutExtension(ProjectPath),
                TableKeyNames.ProjectType => Path.GetExtension(ProjectPath),
                TableKeyNames.StartTime => StartTime,
                TableKeyNames.BuildID => BuildId,
                _ => null,
            };
            return content != null;
        }
        public int CompareTo(UIBuildSummary other)
        {
            if (ReferenceEquals(this, other))
            {
                return 0;
            }

            if (other is null)
            {
                return 1;
            }

            var startComparison = StartTime.CompareTo(other.StartTime);
            return startComparison != 0 ? startComparison : string.Compare(ProjectPath, other.ProjectPath, StringComparison.Ordinal);
        }
    }
}
