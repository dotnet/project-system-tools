// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell.TableManager;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.Providers
{
    public interface IFrontendBuildTableDataSource : ITableDataSource
    {
        ITableManager Manager { get; set; }

        bool IsLogging();

        void Start();

        void Stop();

        void Clear();
    }
}
