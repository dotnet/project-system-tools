// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.LogModel;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.Providers.RpcContracts
{
    public interface IBuildTableDataSourceService
    {
        Task<bool> IsLogging();

        Task<bool> Start();  // Should this be void return type?

        Task<bool> Stop();  // Should this be void return type?

        Task<bool> Clear();  // Would it be a good idea to have this? Technically the client could probably handle that logic and just clear the UI

        Task<BuildHandle> NotifyBuildStart();

        Task<bool> NotifyBuildFinished();  // Should this be void return type?

        Task<Log> RetrieveLogForBuild(BuildHandle handle);

        Task<bool> SaveBuildLogToServer();  // Would it be a good idea to have this?
    }
}
