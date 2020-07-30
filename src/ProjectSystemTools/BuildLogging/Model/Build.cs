// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.ProjectSystem.Tools.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.Model
{
    // server side data (deals with log files)
    internal sealed class Build : IComparable<Build>, IDisposable
    {
        public static int SharedBuildID { get; }
        public int BuildID { get; }
        public BuildSummary BuildSummary { get; private set; }
        public string LogPath { get; private set; }

        public Build(string projectPath, IEnumerable<string> dimensions, IEnumerable<string> targets, BuildType buildType, DateTime startTime)
        {
            BuildID = SharedBuildID;
            BuildSummary = new BuildSummary(projectPath, dimensions, targets, buildType, startTime);
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

        public bool TryGetValue(string keyName, out object content)
        {
            switch (keyName)
            {
                case TableKeyNames.Dimensions:
                    content = BuildSummary.Dimensions;
                    break;

                case TableKeyNames.Targets:
                    content = BuildSummary.Targets;
                    break;

                case TableKeyNames.Elapsed:
                    content = BuildSummary.Elapsed;
                    break;

                case TableKeyNames.BuildType:
                    content = BuildSummary.BuildType;
                    break;

                case TableKeyNames.Status:
                    content = BuildSummary.Status;
                    break;

                case StandardTableKeyNames.ProjectName:
                    content = Path.GetFileNameWithoutExtension(BuildSummary.ProjectPath);
                    break;

                case TableKeyNames.ProjectType:
                    content = Path.GetExtension(BuildSummary.ProjectPath);
                    break;

                case TableKeyNames.StartTime:
                    content = BuildSummary.StartTime;
                    break;

                case TableKeyNames.LogPath:
                    content = LogPath;
                    break;

                default:
                    content = null;
                    break;
            }

            return content != null;
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
            //var startComparison = BuildSummary.StartTime.CompareTo(other.BuildSummary.StartTime);
            //return startComparison != 0 ? startComparison : string.Compare(BuildSummary.ProjectPath, other.BuildSummary.ProjectPath, StringComparison.Ordinal);
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
