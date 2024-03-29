﻿// Copyright (c) Microsoft. All Rights Reserved. Licensed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.LogModel.Builder
{
    internal abstract class BaseInfo
    {
        private List<MessageInfo>? _messages;

        public IEnumerable<MessageInfo>? Messages => _messages;

        public void AddMessage(MessageInfo message)
        {
            _messages ??= new List<MessageInfo>();

            _messages.Add(message);
        }

        public void AddMessage(string message, DateTime timestamp) =>
            AddMessage(new MessageInfo(message, timestamp));
    }
}
