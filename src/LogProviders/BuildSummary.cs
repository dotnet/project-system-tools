// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.ProjectSystem.Tools.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.Model
{
    /// <summary>
    /// Immutable Type
    /// </summary>
    public sealed class BuildSummary : IComparable<BuildSummary>
    {
        public int BuildID { get; }
        public BuildType BuildType { get; }

        public IEnumerable<string> Dimensions { get; }

        public IEnumerable<string> Targets { get; }

        public DateTime StartTime { get; }

        public TimeSpan Elapsed { get; }

        public BuildStatus Status { get; }

        public string ProjectPath { get; }

        public BuildSummary(int buildID, string projectPath, IEnumerable<string> dimensions, IEnumerable<string> targets, BuildType buildType, DateTime startTime)
        {
            BuildID = buildID;
            ProjectPath = projectPath;
            Dimensions = dimensions.ToArray();
            Targets = targets?.ToArray() ?? Enumerable.Empty<string>();
            BuildType = buildType;
            StartTime = startTime;
            Status = BuildStatus.Running;
        }
        public BuildSummary(BuildSummary other, BuildStatus status, TimeSpan elapsed) {
            BuildID = other.BuildID;
            BuildType = other.BuildType;
            // TODO: Check if this needs deep copying
            Dimensions = other.Dimensions;
            Targets = other.Targets;
            StartTime = other.StartTime;
            ProjectPath = other.ProjectPath;

            Elapsed = elapsed;
            Status = status;
        }

        public bool TryGetValue(string keyName, out object content)
        {
            switch (keyName)
            {
                case TableKeyNames.Dimensions:
                    content = Dimensions;
                    break;

                case TableKeyNames.Targets:
                    content = Targets;
                    break;

                case TableKeyNames.Elapsed:
                    content = Elapsed;
                    break;

                case TableKeyNames.BuildType:
                    content = BuildType;
                    break;

                case TableKeyNames.Status:
                    content = Status;
                    break;

                case StandardTableKeyNames.ProjectName:
                    content = Path.GetFileNameWithoutExtension(ProjectPath);
                    break;

                case TableKeyNames.ProjectType:
                    content = Path.GetExtension(ProjectPath);
                    break;

                case TableKeyNames.StartTime:
                    content = StartTime;
                    break;

                // TODO: Need new approach for LogPath
                //case TableKeyNames.LogPath:
                //    content = LogPath;
                //    break;

                default:
                    content = null;
                    break;
            }

            return content != null;
        }

        public int CompareTo(BuildSummary other)
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
