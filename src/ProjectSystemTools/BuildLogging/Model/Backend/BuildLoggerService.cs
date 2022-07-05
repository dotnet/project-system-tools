// Copyright (c) Microsoft. All Rights Reserved. Licensed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.Model.RpcContracts;

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

        public bool IsLogging => _loggingController.IsLogging;

        public bool SupportsRoslynLogging => _dataSource.SupportsRoslynLogging;

        public void Start(Action notifyCallback)
        {
            _dataSource.Start(notifyCallback);
        }

        public void Stop()
        {
            _dataSource.Stop();
        }

        public void Clear()
        {
            _dataSource.Clear();
        }

        public string? GetLogForBuild(int buildId)
        {
            return _dataSource.GetLogForBuild(buildId);
        }

        public ImmutableArray<BuildSummary> GetAllBuilds()
        {
            return _dataSource.GetAllBuilds();
        }
    }
}
