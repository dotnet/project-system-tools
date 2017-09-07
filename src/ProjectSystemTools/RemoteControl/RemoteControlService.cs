// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.RemoteControl;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.RemoteControl
{
    internal class RemoteControlService : IRemoteControlService
    {
        public IRemoteControlClient CreateClient(string hostId, string serverPath, int pollingMinutes)
        {
            // BaseUrl provided by the VS RemoteControl client team.  This is URL we are supposed
            // to use to publish and access data from.
            const string BaseUrl = "https://az700632.vo.msecnd.net/pub";

            return new RemoteControlClient(hostId, BaseUrl, serverPath, pollingMinutes);
        }
    }
}
