// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.Model.RpcContracts;
using Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.UI;
using Microsoft.VisualStudio.RpcContracts.FileSystem;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.ServiceBroker;
using Microsoft.VisualStudio.Shell.TableManager;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.Model.FrontEnd
{
    [Export(typeof(IFrontEndBuildTableDataSource))]
    internal sealed class FrontEndBuildTableDataSource : ITableEntriesSnapshotFactory, IFrontEndBuildTableDataSource, IDisposable
    {
        private const string BuildDataSourceDisplayName = "Build Data Source";
        private const string BuildTableDataSourceIdentifier = nameof(BuildTableDataSourceIdentifier);
        private const string BuildTableDataSourceSourceTypeIdentifier = nameof(BuildTableDataSourceSourceTypeIdentifier);

        private readonly object _gate = new object();

        private ITableDataSink _tableDataSink;
        private BuildTableEntriesSnapshot _lastSnapshot;
        private ImmutableList<UIBuildSummary> _entries = ImmutableList<UIBuildSummary>.Empty;

        private readonly IServiceProvider _serviceProvider;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private IBuildLoggerService _loggerServiceReference;

        public string SourceTypeIdentifier => BuildTableDataSourceSourceTypeIdentifier;

        public string Identifier => BuildTableDataSourceIdentifier;

        public string DisplayName => BuildDataSourceDisplayName;

        public bool SupportRoslynLogging { get; private set; }

        public int CurrentVersionNumber { get; private set; }

        public FrontEndBuildTableDataSource()
        {
            _serviceProvider = ProjectSystemToolsPackage.ServiceProvider;
            _cancellationTokenSource = new CancellationTokenSource();

            ThreadHelper.JoinableTaskFactory.Run(() =>
            {
                return UseLoggerServiceAsync(async (loggerService, token) =>
                {
                    if (loggerService != null)
                    {
                        SupportRoslynLogging = await loggerService.SupportsRoslynLoggingAsync(token);
                    }
                });
            });
        }

        public Task<bool> IsLoggingAsync()
        {
            return UseLoggerServiceAsync(async (loggerService, token) =>
            {
                if (loggerService != null)
                {
                    return await loggerService.IsLoggingAsync(token);
                }
                else
                {
                    throw new InvalidOperationException("Logging service is null. Most likely the client is not connected to the server yet.");
                }
            });
        }

        public void Start()
        {
            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                IServiceBroker sb = GetServiceBroker();
                (_loggerServiceReference as IDisposable)?.Dispose();
                _loggerServiceReference = await sb.GetProxyAsync<IBuildLoggerService>(RpcDescriptors.LoggerServiceDescriptor);
                Assumes.Present(_loggerServiceReference);
                _loggerServiceReference.DataChanged += C_DataChanged;
                await _loggerServiceReference.StartAsync(_cancellationTokenSource.Token);
            });
        }

        /// <summary>
        /// Start must be called before Stop
        /// </summary>
        public void Stop()
        {
            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                Assumes.Present(_loggerServiceReference);
                await _loggerServiceReference.StopAsync(_cancellationTokenSource.Token);
                _loggerServiceReference.DataChanged -= C_DataChanged;
            });
            UpdateEntries();
        }
        void C_DataChanged(object sender, EventArgs e)
        {
            UpdateEntries();
        }

        public void Clear()
        {
            ThreadHelper.JoinableTaskFactory.Run(() =>
            {
                return UseLoggerServiceAsync(async (loggerService, token) =>
                {
                    Assumes.Present(loggerService);
                    await loggerService.ClearAsync(token);
                    _entries = ImmutableList<UIBuildSummary>.Empty;
                    NotifyChange();
                });
            });
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
            (_loggerServiceReference as IDisposable)?.Dispose();
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

        public async Task<string> GetLogForBuildAsync(int buildID)
        {
            string filePath = await UseLoggerServiceAsync(async (loggerService, token) =>
            {
                Assumes.Present(loggerService);
                return await loggerService.GetLogForBuildAsync(buildID, token);
            });

            if (filePath == null)
            {
                return null;
            }

            IServiceBroker sb = GetServiceBroker();
            IFileSystemProvider fileSystemService = await sb.GetProxyAsync<IFileSystemProvider>(VisualStudioServices.VS2019_7.FileSystem);
            try
            {
                Assumes.Present(fileSystemService);
                Uri fileUri = new Uri(filePath);
                string clientFilePath = Path.Combine(Path.GetTempPath(), Path.GetFileName(filePath));
                Uri clientUri = new Uri(clientFilePath);
                if (!fileUri.Equals(clientUri))
                {
                    await fileSystemService.CopyAsync(fileUri, clientUri, true, null, _cancellationTokenSource.Token);
                }
                return clientFilePath;
            }
            finally
            {
                (fileSystemService as IDisposable)?.Dispose();
            }
        }

        private void UpdateEntries()
        {
            ThreadHelper.JoinableTaskFactory.Run(() =>
            {
                return UseLoggerServiceAsync(async (loggerService, token) =>
                {
                    Assumes.Present(loggerService);
                    _entries = (await loggerService.GetAllBuildsAsync(token))
                            .Select(summary => new UIBuildSummary(summary))
                            .ToImmutableList();

                    NotifyChange();
                });
            });
        }

        private IServiceBroker GetServiceBroker()
        {
            IBrokeredServiceContainer serviceContainer = _serviceProvider.GetService<SVsBrokeredServiceContainer, IBrokeredServiceContainer>();
            Assumes.Present(serviceContainer);
            return serviceContainer.GetFullAccessServiceBroker();
        }

        private async Task UseLoggerServiceAsync(Func<IBuildLoggerService, CancellationToken, Task> func)
        {
            IServiceBroker sb = GetServiceBroker();
            IBuildLoggerService loggerService = await sb.GetProxyAsync<IBuildLoggerService>(RpcDescriptors.LoggerServiceDescriptor);
            try
            {
                await func(loggerService, _cancellationTokenSource.Token);
            }
            finally
            {
                (loggerService as IDisposable)?.Dispose();
            }
        }

        private async Task<T> UseLoggerServiceAsync<T>(Func<IBuildLoggerService, CancellationToken, Task<T>> func)
        {
            IServiceBroker sb = GetServiceBroker();
            IBuildLoggerService loggerService = await sb.GetProxyAsync<IBuildLoggerService>(RpcDescriptors.LoggerServiceDescriptor);
            try
            {
                return await func(loggerService, _cancellationTokenSource.Token);
            }
            finally
            {
                (loggerService as IDisposable)?.Dispose();
            }
        }
    }
}
