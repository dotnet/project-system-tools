// Copyright (c) Microsoft. All Rights Reserved. Licensed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;

using Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.Model.BackEnd;
using Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.UI;
using Microsoft.VisualStudio.Shell.TableManager;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.Model.FrontEnd
{
    [Export(typeof(IFrontEndBuildTableDataSource))]
    internal sealed class FrontEndBuildTableDataSource : ITableEntriesSnapshotFactory, IFrontEndBuildTableDataSource
    {
        private const string BuildDataSourceDisplayName = "Build Data Source";
        private const string BuildTableDataSourceIdentifier = nameof(BuildTableDataSourceIdentifier);
        private const string BuildTableDataSourceSourceTypeIdentifier = nameof(BuildTableDataSourceSourceTypeIdentifier);

        private readonly object _gate = new object();

        private ITableDataSink? _tableDataSink;
        private BuildTableEntriesSnapshot? _lastSnapshot;
        private ImmutableArray<UIBuildSummary> _entries = ImmutableArray<UIBuildSummary>.Empty;

        private readonly ILoggingDataSource _loggerService;
        private readonly ILoggingController _loggingController;

        public string SourceTypeIdentifier => BuildTableDataSourceSourceTypeIdentifier;

        public string Identifier => BuildTableDataSourceIdentifier;

        public string DisplayName => BuildDataSourceDisplayName;

        public int CurrentVersionNumber { get; private set; }

        [ImportingConstructor]
        public FrontEndBuildTableDataSource(ILoggingDataSource loggerService, ILoggingController loggingController)
        {
            _loggerService = loggerService;
            _loggingController = loggingController;

            // cache this value to avoid redundant work
            SupportRoslynLogging = loggerService.SupportsRoslynLogging;
        }

        public bool IsLogging => _loggingController.IsLogging;

        public bool SupportRoslynLogging { get; }

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
            _entries = ImmutableArray<UIBuildSummary>.Empty;
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
            _entries = ImmutableArray<UIBuildSummary>.Empty;
        }

        public void NotifyChange()
        {
            CurrentVersionNumber++;

            Assumes.NotNull(_tableDataSink);
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

        public ITableEntriesSnapshot? GetSnapshot(int versionNumber)
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

        public string? GetLogForBuild(int buildId)
        {
            Assumes.NotNull(_loggerService);

            return _loggerService.GetLogForBuild(buildId);
        }

        private void UpdateEntries()
        {
            _entries = _loggerService.GetAllBuilds()
                .Select(summary => new UIBuildSummary(summary))
                .ToImmutableArray();

            NotifyChange();
        }
    }
}
