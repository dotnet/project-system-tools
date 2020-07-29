// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Tools.Providers;
using Microsoft.VisualStudio.ProjectSystem.Tools.Providers.RpcContracts;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.Model
{
    /// <summary>
    /// Implements IBuildLoggerService that interacts with Codespaces
    /// </summary>
    [Export(typeof(IBuildLoggerService))]
    internal sealed class BuildLoggerService : IBuildLoggerService
    {
        IBackendBuildTableDataSource _dataSource;

        [ImportingConstructor]
        public BuildLoggerService(IBackendBuildTableDataSource dataSource) {
            _dataSource = dataSource;
        }

        Task<bool> IBuildLoggerService.IsLogging()
        {
            return Task.FromResult(_dataSource.IsLogging);
        }

        Task<bool> IBuildLoggerService.Start()
        {
            _dataSource.Start();
            return Task.FromResult(true);
        }

        Task<bool> IBuildLoggerService.Stop()
        {
            _dataSource.Stop();
            return Task.FromResult(true);
        }

        Task<bool> IBuildLoggerService.Clear()
        {
            _dataSource.Clear();
            return Task.FromResult(true);
        }
    }
}
