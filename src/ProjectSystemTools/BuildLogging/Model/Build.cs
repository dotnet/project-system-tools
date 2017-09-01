// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.UI;
using Microsoft.VisualStudio.Shell.TableManager;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.Model
{
    internal sealed class Build : IComparable<Build>, IDisposable
    {
        public bool DesignTime { get; }

        public IEnumerable<string> Dimensions { get; }

        public IDictionary<string, TargetInfo> Targets { get; }

        public DateTime StartTime { get; }

        public TimeSpan Elapsed { get; private set; }

        public BuildStatus Status { get; private set; }

        public string Project { get; }

        public string LogPath { get; private set; }

        public string Filename => $"{Project}_{Dimensions.Aggregate((c, n) => string.IsNullOrEmpty(n) ? c : $"{c}_{n}")}_{(DesignTime ? "design" : "")}_{StartTime:s}.binlog".Replace(':', '_');

        public Build(string project, IEnumerable<string> dimensions, IEnumerable<string> targets, bool designTime, DateTime startTime)
        {
            Project = project;
            Dimensions = dimensions.ToArray();
            Targets = targets
                ?.Select(t => new KeyValuePair<string, TargetInfo>(t, new TargetInfo(t)))
                    .ToDictionary(x => x.Key, x => x.Value)
                ?? new Dictionary<string, TargetInfo>();
            DesignTime = designTime;
            StartTime = startTime;
        }

        public void BuildFinish(bool succeeded, DateTime time, string logPath)
        {
            if (Status != BuildStatus.Running)
            {
                throw new InvalidOperationException();
            }

            Status = succeeded ? BuildStatus.Finished : BuildStatus.Failed;
            Elapsed = time - StartTime;
            LogPath = logPath;
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

                case TableKeyNames.DesignTime:
                    content = DesignTime;
                    break;

                case TableKeyNames.Status:
                    content = Status;
                    break;

                case StandardTableKeyNames.ProjectName:
                    content = Project;
                    break;

                case TableKeyNames.StartTime:
                    content = StartTime;
                    break;

                case TableKeyNames.LogPath:
                    content = LogPath;
                    break;

                case TableKeyNames.Filename:
                    content = Filename;
                    break;

                default:
                    content = null;
                    break;
            }

            return content != null;
        }

        internal void TargetCompleted(string targetName, string targetFile, DateTime timestamp)
        {
            if (Targets.TryGetValue(targetName, out var targetInfo) && targetInfo.TargetFile == targetFile)
            {
                targetInfo.Elapsed = timestamp - targetInfo.StartTime;
            }
        }

        internal void TargetStarted(string targetName, string targetFile, DateTime timestamp)
        {
            if (Targets.TryGetValue(targetName, out var targetInfo))
            {
                targetInfo.TargetFile = targetFile;
                targetInfo.StartTime = timestamp;
            }
            else
            {
                Targets[targetName] = new TargetInfo(targetName, targetFile, timestamp);
            }
        }

        public int CompareTo(Build other)
        {
            if (ReferenceEquals(this, other))
            {
                return 0;
            }

            if (ReferenceEquals(null, other))
            {
                return 1;
            }

            var startComparison = StartTime.CompareTo(other.StartTime);
            return startComparison != 0 ? startComparison : string.Compare(Project, other.Project, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (ReferenceEquals(obj, null))
            {
                return false;
            }

            if (obj is Build build)
            {
                return CompareTo(build) == 0;
            }

            return false;
        }

        public static bool operator ==(Build left, Build right)
        {
            if (ReferenceEquals(left, null))
            {
                return ReferenceEquals(right, null);
            }

            return left.Equals(right);
        }

        public static bool operator !=(Build left, Build right)
            => !(left == right);

        public static bool operator <(Build left, Build right)
            => ReferenceEquals(left, null) ? !ReferenceEquals(right, null) : left.CompareTo(right) < 0;

        public static bool operator <=(Build left, Build right)
            => ReferenceEquals(left, null) || left.CompareTo(right) <= 0;

        public static bool operator >(Build left, Build right)
            => !ReferenceEquals(left, null) && left.CompareTo(right) > 0;

        public static bool operator >=(Build left, Build right)
            => ReferenceEquals(left, null)
                ? ReferenceEquals(right, null)
                : left.CompareTo(right) >= 0;

        public override int GetHashCode()
        {
            var hashCode = -617317777;
            hashCode = hashCode * -1521134295 + StartTime.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Project);
            return hashCode;
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
