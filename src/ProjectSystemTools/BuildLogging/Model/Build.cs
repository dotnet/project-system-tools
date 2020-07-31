// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.Model.Backend;
using Microsoft.VisualStudio.ProjectSystem.Tools.Providers;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.Model
{
    // server side data (deals with log files)
    internal sealed class Build : IComparable<Build>, IDisposable
    {
        // TODO: If needed, make an issue: implement more robust ID system (that doesn't use ints)
        public static int SharedBuildID { get; private set; }
        public BuildSummary BuildSummary { get; private set; }
        public string LogPath { get; private set; }

        public Build(string projectPath, IEnumerable<string> dimensions, IEnumerable<string> targets, BuildType buildType, DateTime startTime)
        {
            BuildSummary = new BuildSummary(SharedBuildID, projectPath, dimensions, targets, buildType, startTime);
            SharedBuildID++;
        }

        public void Finish(bool succeeded, DateTime time)
        {
            if (BuildSummary.Status != BuildStatus.Running)
            {
                throw new InvalidOperationException();
            }

            BuildStatus newStatus = succeeded ? BuildStatus.Finished : BuildStatus.Failed;
            var elapsedTime = time - BuildSummary.StartTime;
            BuildSummary = new BuildSummary(BuildSummary, newStatus, elapsedTime);
        }

        public void SetLogPath(string logPath)
        {
            LogPath = logPath;
        }

        public int CompareTo(Build other)
        {
            if (ReferenceEquals(this, other))
            {
                return 0;
            }

            if (other is null)
            {
                return 1;
            }

            return BuildSummary.CompareTo(other.BuildSummary);
        }

        public void Dispose()
        {
            if (LogPath == null)
            {
                return;
            }

            var logPath = LogPath;
            LogPath = null;
            try
            {
                File.Delete(logPath);
            }
            catch
            {
                // If it fails, it fails...
            }
        }
    }
}
