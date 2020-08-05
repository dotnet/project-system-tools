﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.Model.RpcContracts;
using Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.UI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.Model.FrontEnd
{
    [Export(typeof(IFrontEndBuildTableDataSource))]
    internal sealed class FrontEndBuildTableDataSource : ITableEntriesSnapshotFactory, IFrontEndBuildTableDataSource
    {
        private const string BuildDataSourceDisplayName = "Build Data Source";
        private const string BuildTableDataSourceIdentifier = nameof(BuildTableDataSourceIdentifier);
        private const string BuildTableDataSourceSourceTypeIdentifier = nameof(BuildTableDataSourceSourceTypeIdentifier);

        private readonly object _gate = new object();

        private ITableDataSink _tableDataSink;
        private BuildTableEntriesSnapshot _lastSnapshot;
        private ImmutableList<UIBuildSummary> _entries = ImmutableList<UIBuildSummary>.Empty;

        private readonly IBuildLoggerService _loggerService;

        public string SourceTypeIdentifier => BuildTableDataSourceSourceTypeIdentifier;

        public string Identifier => BuildTableDataSourceIdentifier;

        public string DisplayName => BuildDataSourceDisplayName;

        public bool SupportRoslynLogging { get; private set; }

        public int CurrentVersionNumber { get; private set; }

        [ImportingConstructor]
        public FrontEndBuildTableDataSource(IBuildLoggerService loggerService)
        {
            _loggerService = loggerService;
            
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                SupportRoslynLogging = await _loggerService.SupportsRoslynLogging();
            });
        }

        public Task<bool> IsLogging
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

        public string GetLogForBuild(int buildID)
        {
            string result;
            result = ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                string result = await _loggerService.GetLogForBuild(buildID);
                return result;
            });
            return result;
        }

        private void UpdateEntries()
        {
            _entries = _loggerService
                .GetAllBuilds()
                .Select(summary => new UIBuildSummary(summary))
                .ToImmutableList();

            NotifyChange();
        }
    }
}
