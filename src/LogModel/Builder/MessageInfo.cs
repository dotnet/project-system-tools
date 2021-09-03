// Copyright (c) Microsoft. All Rights Reserved. Licensed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Microsoft.VisualStudio.ProjectSystem.LogModel.Builder
{
    internal class MessageInfo
    {
        public DateTime Timestamp { get; }
        public string Text { get; }

        public MessageInfo(string text, DateTime timestamp)
        {
            Text = text;
            Timestamp = timestamp;
        }
    }
}
