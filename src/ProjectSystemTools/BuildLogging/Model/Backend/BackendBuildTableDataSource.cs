// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.ComponentModel.Composition;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Framework;
using Microsoft.VisualStudio.ProjectSystem.Tools.Providers;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.Model
{

    [Export(typeof(IBackendBuildTableDataSource))]
    internal sealed class BackendBuildTableDataSource : IBackendBuildTableDataSource // ITableEntriesSnapshotFactory, 
    {
        private const string BuildDataSourceDisplayName = "Build Data Source";
        private const string BuildTableDataSourceIdentifier = nameof(BuildTableDataSourceIdentifier);
        private const string BuildTableDataSourceSourceTypeIdentifier = nameof(BuildTableDataSourceSourceTypeIdentifier);

        private readonly EvaluationLogger _evaluationLogger;
        private readonly RoslynLogger _roslynLogger;

        private ImmutableList<Build> _entries = ImmutableList<Build>.Empty;

        public string SourceTypeIdentifier => BuildTableDataSourceSourceTypeIdentifier;

        public string Identifier => BuildTableDataSourceIdentifier;

        public string DisplayName => BuildDataSourceDisplayName;

        public bool SupportRoslynLogging => _roslynLogger.Supported;

        public bool IsLogging { get; private set; }

        public bool SupportsRoslynLogging => _roslynLogger.Supported;

        private NotifyCallback NotifyUI { get; set; }

        public BackendBuildTableDataSource()
        {
            _evaluationLogger = new EvaluationLogger(this);
            _roslynLogger = new RoslynLogger(this);
        }

        public void Start(NotifyCallback notifyCallback)
        {
            NotifyUI = notifyCallback;

            IsLogging = true;
            ProjectCollection.GlobalProjectCollection.RegisterLogger(_evaluationLogger);
            _roslynLogger.Start();
        }

        public void Stop()
        {
            NotifyUI = null;

            IsLogging = false;
            ProjectCollection.GlobalProjectCollection.UnregisterAllLoggers();
            _roslynLogger.Stop();
        }

        public void Clear()
        {
            foreach (var build in _entries)
            {
                build.Dispose();
            }
            _entries = ImmutableList<Build>.Empty;
        }

        public ILogger CreateLogger(bool isDesignTime) => new ProjectLogger(this, isDesignTime);

        /// <summary>
        /// (Temporary) return log path on server for a given build
        /// If buildID cannot be found, will return null
        /// </summary>
        /// <param name="buildID">ID to return build for</param>
        /// <returns>(Temporary) returns filepath to log path (on server)</returns>
        public string RetrieveLogForBuild(int buildID)
        {
            Build match = findBuildByID(buildID);
            return match.LogPath;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="buildID">ID to return build for</param>
        /// <returns>Build matching the given ID, if no match, return null</returns>
        private Build findBuildByID(int buildID)
        {
            return _entries.Find(x => x.BuildSummary.BuildID == buildID);
        }

        public ImmutableList<IBuildSummary> RetrieveAllBuilds()
        {
            ImmutableList<IBuildSummary> buildSummaries = ImmutableList<IBuildSummary>.Empty;
            IEnumerator<Build> builds = _entries.GetEnumerator();
            while (builds.MoveNext())
            {
                Build current = builds.Current;
                buildSummaries = buildSummaries.Add(current.BuildSummary);
            }
            return buildSummaries;
        }

        public void Dispose()
        {
            Clear();
        }

        public void NotifyChange()
        {
            if (NotifyUI != null) 
            {
                NotifyUI();
            }
        }

        public void AddEntry(Build build)
        {
            _entries = _entries.Add(build);
            NotifyChange();
        }
    }
}
