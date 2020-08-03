// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.Build.Framework;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.Providers
{
    public interface IBackendBuildTableDataSource
    {
        bool IsLogging { get; }
        bool SupportsRoslynLogging { get; }
        void Start(NotifyCallback notifyCallback);

        void Stop();

        void Clear();

        ILogger CreateLogger(bool isDesignTime);

        string GetLogForBuild(int buildID);

        ImmutableList<BuildSummary> GetAllBuilds();
    }
}
