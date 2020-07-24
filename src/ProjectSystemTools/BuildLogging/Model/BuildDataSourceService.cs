// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Framework;
using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.UI;
using Microsoft.VisualStudio.ProjectSystem.Tools.Providers;
using Microsoft.VisualStudio.ProjectSystem.Tools.Providers.RpcContracts;
using Microsoft.VisualStudio.Shell.TableManager;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.Model
{
    /// <summary>
    /// Implements the IBuildDataSourceService that interacts with Codespaces
    /// </summary>
    [Export(typeof(IBuildTableDataSource))]
    internal sealed class BuildDataSourceService : IBuildDataSourceService
    {
        IBuildTableDataSource _dataSource;
        
        public BuildDataSourceService(IBuildTableDataSource dataSource) {
            _dataSource = dataSource;
        }

        Task<bool> IBuildDataSourceService.IsLogging()
        {
            return Task.FromResult(_dataSource.IsLogging);
        }

        Task<bool> IBuildDataSourceService.Start()
        {
            throw new NotImplementedException();
        }

        Task<bool> IBuildDataSourceService.Stop()
        {
            throw new NotImplementedException();
        }

        Task<bool> IBuildDataSourceService.Clear()
        {
            throw new NotImplementedException();
        }
    }
}
