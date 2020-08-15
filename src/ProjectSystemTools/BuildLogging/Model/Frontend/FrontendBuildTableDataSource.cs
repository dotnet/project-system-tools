// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.Model.RpcContracts;
using Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.UI;
using Microsoft.VisualStudio.RpcContracts.FileSystem;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.ServiceBroker;
using Microsoft.VisualStudio.Shell.TableManager;

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

        public string SourceTypeIdentifier => BuildTableDataSourceSourceTypeIdentifier;

        public string Identifier => BuildTableDataSourceIdentifier;

        public string DisplayName => BuildDataSourceDisplayName;

        public bool SupportRoslynLogging { get; private set; }

        public int CurrentVersionNumber { get; private set; }

        private static FrontEndBuildTableDataSource temp { get; set; }


        public FrontEndBuildTableDataSource()
        {
            _serviceProvider = ProjectSystemToolsPackage.ServiceProvider;
            temp = this;

            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                IBrokeredServiceContainer serviceContainer = _serviceProvider.GetService<SVsBrokeredServiceContainer, IBrokeredServiceContainer>();
                Assumes.Present(serviceContainer);
                IServiceBroker sb = serviceContainer.GetFullAccessServiceBroker();
                IBuildLoggerService _loggerService = await sb.GetProxyAsync<IBuildLoggerService>(RpcDescriptors.LoggerServiceDescriptor);

                try
                {
                    if (_loggerService != null)
                    {
                        SupportRoslynLogging = await _loggerService.SupportsRoslynLoggingAsync();
                    }
                }
                finally
                {
                    (_loggerService as IDisposable)?.Dispose();
                }
            });
        }
        static void c_DataChanged(object sender, DataChangedEventArgs e)
        {
            bool receive = e.Test;
            //temp.UpdateEntries();
        }

        public async Task<bool> IsLoggingAsync()
        {
            IBrokeredServiceContainer serviceContainer = _serviceProvider.GetService<SVsBrokeredServiceContainer, IBrokeredServiceContainer>();
            Assumes.Present(serviceContainer);
            IServiceBroker sb = serviceContainer.GetFullAccessServiceBroker();
            IBuildLoggerService _loggerService = await sb.GetProxyAsync<IBuildLoggerService>(RpcDescriptors.LoggerServiceDescriptor);

            try
            {
                if (_loggerService != null)
                {
                    return await _loggerService.IsLoggingAsync();
                }
                else
                {
                    throw new InvalidOperationException("IsLoggingAsync");
                }
            }
            finally
            {
                (_loggerService as IDisposable)?.Dispose();
            }
        }

        public void Start()
        {
            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                IBrokeredServiceContainer serviceContainer = _serviceProvider.GetService<SVsBrokeredServiceContainer, IBrokeredServiceContainer>();
                Assumes.Present(serviceContainer);
                IServiceBroker sb = serviceContainer.GetFullAccessServiceBroker();
                IBuildLoggerService _loggerService = await sb.GetProxyAsync<IBuildLoggerService>(RpcDescriptors.LoggerServiceDescriptor);

                try
                {
                    if (_loggerService != null)
                    {
                        _loggerService.DataChanged += c_DataChanged;
                        await _loggerService.StartAsync();
                    }
                    else
                    {
                        throw new InvalidOperationException("StartAsync");
                    }
                }
                finally
                {
                    (_loggerService as IDisposable)?.Dispose();
                }
            });
        }

        public void Stop()
        {
            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                IBrokeredServiceContainer serviceContainer = _serviceProvider.GetService<SVsBrokeredServiceContainer, IBrokeredServiceContainer>();
                Assumes.Present(serviceContainer);
                IServiceBroker sb = serviceContainer.GetFullAccessServiceBroker();
                IBuildLoggerService _loggerService = await sb.GetProxyAsync<IBuildLoggerService>(RpcDescriptors.LoggerServiceDescriptor);

                try
                {
                    if (_loggerService != null)
                    {
                        await _loggerService.StopAsync();
                    }
                    else
                    {
                        throw new InvalidOperationException("StopAsync");
                    }
                }
                finally
                {
                    (_loggerService as IDisposable)?.Dispose();
                }
            });
            UpdateEntries(); // TODO: Remove once events is implemented
        }

        public void Clear()
        {
            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                IBrokeredServiceContainer serviceContainer = _serviceProvider.GetService<SVsBrokeredServiceContainer, IBrokeredServiceContainer>();
                Assumes.Present(serviceContainer);
                IServiceBroker sb = serviceContainer.GetFullAccessServiceBroker();
                IBuildLoggerService _loggerService = await sb.GetProxyAsync<IBuildLoggerService>(RpcDescriptors.LoggerServiceDescriptor);
                try
                {
                    if (_loggerService != null)
                    {
                        await _loggerService.ClearAsync();
                        _entries = ImmutableList<UIBuildSummary>.Empty;
                        NotifyChange();
                    }
                    else
                    {
                        throw new InvalidOperationException("ClearAsync");
                    }
                }
                finally
                {
                    (_loggerService as IDisposable)?.Dispose();
                }
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
            string filePath;

            IBrokeredServiceContainer serviceContainer = _serviceProvider.GetService<SVsBrokeredServiceContainer, IBrokeredServiceContainer>();
            Assumes.Present(serviceContainer);
            IServiceBroker sb = serviceContainer.GetFullAccessServiceBroker();
            IBuildLoggerService _loggerService = await sb.GetProxyAsync<IBuildLoggerService>(RpcDescriptors.LoggerServiceDescriptor);
            try
            {
                if (_loggerService != null)
                {
                    filePath = await _loggerService.GetLogForBuildAsync(buildID);
                }
                else
                {
                    (_loggerService as IDisposable)?.Dispose();
                    throw new InvalidOperationException("GetLogForBuildAsync");
                }
            }
            finally
            {
                (_loggerService as IDisposable)?.Dispose();
            }

            IFileSystemProvider fileSystemService = await sb.GetProxyAsync<IFileSystemProvider>(VisualStudioServices.VS2019_6.FileSystem);
            try
            {
                if (fileSystemService != null)
                {
                    Uri fileUri = new Uri(filePath);
                    Pipe pipe = new Pipe();
                    await fileSystemService.ReadFileAsync(fileUri, pipe.Writer, cancellationToken: default);
                    Stream readStream = pipe.Reader.AsStream();
                    string clientFilePath = Path.Combine(Path.GetTempPath(), Path.GetFileName(filePath));
                    using (FileStream outStream = new FileStream(clientFilePath, FileMode.Create))
                    {
                        await readStream.CopyToAsync(outStream);
                    }

                    return clientFilePath;
                }
                else
                {
                    throw new InvalidOperationException("GetLogForBuildAsync");
                }
            }
            finally
            {
                (fileSystemService as IDisposable)?.Dispose();
            }
        }

        private void UpdateEntries()
        {
            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                IBrokeredServiceContainer serviceContainer = _serviceProvider.GetService<SVsBrokeredServiceContainer, IBrokeredServiceContainer>();
                Assumes.Present(serviceContainer);
                IServiceBroker sb = serviceContainer.GetFullAccessServiceBroker();
                IBuildLoggerService _loggerService = await sb.GetProxyAsync<IBuildLoggerService>(RpcDescriptors.LoggerServiceDescriptor);

                try
                {
                    if (_loggerService != null)
                    {
                        _entries = (await _loggerService.GetAllBuildsAsync())
                        .Select(summary => new UIBuildSummary(summary))
                        .ToImmutableList();

                        NotifyChange();
                    }
                    else
                    {
                        throw new InvalidOperationException("UpdateEntries");
                    }
                }
                finally
                {
                    (_loggerService as IDisposable)?.Dispose();
                }
            });
        }
    }
}
