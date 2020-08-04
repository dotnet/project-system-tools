// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.Build.Framework;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.Providers
{
    public interface ILoggingController
    {
        bool IsLogging { get; }
        ILogger CreateLogger(bool isDesignTime);
    }
}
