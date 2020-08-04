// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.Model.RpcContracts;
using Microsoft.VisualStudio.ProjectSystem.Tools.Providers;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.Model.BackEnd
{
    /// <summary>
    /// Implements IBuildLoggerService that interacts with Codespaces
    /// </summary>
    [Export(typeof(IBuildLoggerService))]
    internal sealed class BuildLoggerService : IBuildLoggerService
    {
        private readonly ILoggingDataSource _dataSource;
        private readonly ILoggingController _loggingController;

        [ImportingConstructor]
        public BuildLoggerService(ILoggingDataSource dataSource, ILoggingController loggingController)
        {
            _dataSource = dataSource;
            _loggingController = loggingController;
        }

        public Task<bool> IsLogging()
        {
            return Task.FromResult(_loggingController.IsLogging);
        }

        public Task<bool> SupportsRoslynLogging()
        {
            return Task.FromResult(_dataSource.SupportsRoslynLogging);
        }

        public Task Start(Action notifyCallback)
        {
            _dataSource.Start(notifyCallback);
            return Task.CompletedTask;
        }

        public Task Stop()
        {
            _dataSource.Stop();
            return Task.CompletedTask;
        }

        public Task Clear()
        {
            _dataSource.Clear();
            return Task.CompletedTask;
        }

        public Task<string> GetLogForBuild(int buildID)
        {
            return Task.FromResult(_dataSource.GetLogForBuild(buildID));
        }

        public Task<ImmutableList<BuildSummary>> GetAllBuilds()
        {
            return Task.FromResult(_dataSource.GetAllBuilds());
        }
    }
}
