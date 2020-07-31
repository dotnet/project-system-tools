// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.Build.Framework;
using Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.Model;
//using Microsoft.VisualStudio.Shell.TableManager;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.Providers
{
    public interface IBackendBuildTableDataSource // : ITableDataSource
    {
        bool IsLogging { get; }
        bool SupportsRoslynLogging { get; }
        void Start(NotifyCallback notifyCallback);

        void Stop();

        void Clear();

        ILogger CreateLogger(bool isDesignTime);

        string RetrieveLogForBuild(int buildID);

        ImmutableList<IBuildSummary> RetrieveAllBuilds();
    }
}
