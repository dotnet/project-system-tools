// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.Model.RpcContracts;
using Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.UI;
using Microsoft.VisualStudio.Shell.TableManager;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.Model.Frontend
{
    [Export(typeof(IFrontendBuildTableDataSource))]
    internal sealed class FrontendBuildTableDataSource : ITableEntriesSnapshotFactory, IFrontendBuildTableDataSource
    {
        private const string BuildDataSourceDisplayName = "Build Data Source";
        private const string BuildTableDataSourceIdentifier = nameof(BuildTableDataSourceIdentifier);
        private const string BuildTableDataSourceSourceTypeIdentifier = nameof(BuildTableDataSourceSourceTypeIdentifier);

        private readonly object _gate = new object();

        private ITableManager _manager;
        private ITableDataSink _tableDataSink;
        private BuildTableEntriesSnapshot _lastSnapshot;
        private ImmutableList<UIBuildSummary> _entries = ImmutableList<UIBuildSummary>.Empty;

        private readonly IBuildLoggerService _loggerService;

        public string SourceTypeIdentifier => BuildTableDataSourceSourceTypeIdentifier;

        public string Identifier => BuildTableDataSourceIdentifier;

        public string DisplayName => BuildDataSourceDisplayName;

        public bool SupportRoslynLogging { get; }

        public int CurrentVersionNumber { get; private set; }

        public ITableManager Manager
        {
            get => _manager;
            set
            {
                _manager?.RemoveSource(this);
                _manager = value;
                _manager?.AddSource(this);
            }
        }

        [ImportingConstructor]
        public FrontendBuildTableDataSource(IBuildLoggerService loggerService)
        {
            _loggerService = loggerService;
            SupportRoslynLogging = _loggerService.SupportsRoslynLogging();
        }

        public bool IsLogging
        {
            get
            {
                return _loggerService.IsLogging();
            }
        }

        public void Start()
        {
            _loggerService.Start(UpdateEntries);
        }

        public void Stop()
        {
            _loggerService.Stop();
        }

        public void Clear()
        {
            _loggerService.Clear();
            _entries = ImmutableList<UIBuildSummary>.Empty;
            CurrentVersionNumber++;
            NotifyChange();
        }

        public IDisposable Subscribe(ITableDataSink sink)
        {
            _tableDataSink = sink;

            _tableDataSink.AddFactory(this, removeAllFactories: true);
            _tableDataSink.IsStable = true;

            return this;
        }

        public void Dispose()
        {
            _entries = ImmutableList<UIBuildSummary>.Empty;
            Manager = null;
        }

        public void NotifyChange()
        {
            CurrentVersionNumber++;
            _tableDataSink.FactorySnapshotChanged(this);
        }

        public ITableEntriesSnapshot GetCurrentSnapshot()
        {
            lock (_gate)
            {
                if (_lastSnapshot?.VersionNumber != CurrentVersionNumber)
                {
                    _lastSnapshot = new BuildTableEntriesSnapshot(_entries, CurrentVersionNumber);
                }

                return _lastSnapshot;
            }
        }

        public ITableEntriesSnapshot GetSnapshot(int versionNumber)
        {
            lock (_gate)
            {
                if (_lastSnapshot?.VersionNumber == versionNumber)
                {
                    return _lastSnapshot;
                }

                if (versionNumber == CurrentVersionNumber)
                {
                    return GetCurrentSnapshot();
                }
            }

            // We didn't have this version.  Notify the sinks that something must have changed
            // so that they call back into us with the latest version.
            NotifyChange();
            return null;
        }

        public string RetrieveLogForBuild(int buildID)
        {
            return _loggerService.GetLogForBuild(buildID);
        }

        private void UpdateEntries()
        {
            _entries = _loggerService
                .GetAllBuilds()
                .Select(summary => new UIBuildSummary(summary))
                .ToImmutableList();
        }
    }
}
