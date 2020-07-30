// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        public BackendBuildTableDataSource()
        {
            _evaluationLogger = new EvaluationLogger(this);
            _roslynLogger = new RoslynLogger(this);
        }

        public void Start()
        {
            IsLogging = true;
            ProjectCollection.GlobalProjectCollection.RegisterLogger(_evaluationLogger);
            _roslynLogger.Start();
        }

        public void Stop()
        {
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

        // TODO: removing Subscribe() breaks the ITableDataSource interface, is this needed?
        //public IDisposable Subscribe(ITableDataSink sink)
        //{
        //    _tableDataSink = sink;

        //    _tableDataSink.AddFactory(this, removeAllFactories: true);
        //    _tableDataSink.IsStable = true;

        //    return this;
        //}

        public void Dispose()
        {
            Clear();
        }

        public void NotifyChange()
        {
            // TODO: Loggers need NotifyChange(), maybe include this in the interface?
            //CurrentVersionNumber++;
            //_tableDataSink.FactorySnapshotChanged(this);
            /*NotifyChangeEventFire();*/
        }

        public void AddEntry(Build build)
        {
            _entries = _entries.Add(build);
            NotifyChange();
        }

        public ImmutableList<BuildSummary> RetrieveAllBuilds()
        {
            ImmutableList<BuildSummary> buildSummaries = ImmutableList<BuildSummary>.Empty;
            IEnumerator<Build> builds = _entries.GetEnumerator();
            while (builds.MoveNext()) 
            {
                Build current = builds.Current;
                buildSummaries = buildSummaries.Add(current.BuildSummary);
            }
            return buildSummaries;
        }
    }
}
