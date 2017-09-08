// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.RemoteControl
{
    internal interface IProjectSystemToolsSetttingsService
    {
        bool TryGetSetting<T>(string name, out T value);
        Task UpdateContinuouslyAsync(string serverPath, CancellationToken token);
    }
}
