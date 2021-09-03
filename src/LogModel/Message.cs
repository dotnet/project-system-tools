// Copyright (c) Microsoft. All Rights Reserved. Licensed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Microsoft.VisualStudio.ProjectSystem.LogModel
{
    public class Message
    {
        public DateTime Timestamp { get; }
        public string Text { get; }

        public Message(DateTime timestamp, string text)
        {
            Timestamp = timestamp;
            Text = text;
        }
    }
}
