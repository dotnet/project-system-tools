﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using System.Linq;
using Microsoft.Build.Framework;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.Model.Backend
{
    internal abstract class LoggerBase : ILogger
    {
        protected readonly BackendBuildTableDataSource DataSource;

        public LoggerVerbosity Verbosity { get => LoggerVerbosity.Diagnostic; set { } }

        public string Parameters { get; set; }

        protected LoggerBase(BackendBuildTableDataSource dataSource)
        {
            DataSource = dataSource;
        }

        protected string GetLogPath(Build build)
        {
            var dimensionsString =
                build.BuildSummary.Dimensions.Any() ? $"{build.BuildSummary.Dimensions.Aggregate((c, n) => string.IsNullOrEmpty(n) ? c : $"{c}_{n}")}_" : string.Empty;

            var filename = $"{Path.GetFileNameWithoutExtension(build.BuildSummary.ProjectPath)}_{dimensionsString}{build.BuildSummary.BuildType}_{build.BuildSummary.StartTime:o}.binlog".Replace(':', '_');

            return Path.Combine(Path.GetTempPath(), filename);
        }

        protected void Copy(string from, string to)
        {
            File.Copy(from, to, overwrite: true);
        }

        public abstract void Initialize(IEventSource eventSource);

        public virtual void Shutdown()
        {
        }
    }
}