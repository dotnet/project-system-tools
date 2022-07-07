// Copyright (c) Microsoft. All Rights Reserved. Licensed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.LogModel
{
    internal sealed class Build : Node
    {
        public Project? Project { get; }

        public ImmutableDictionary<string, string>? Environment { get; }

        public Build(Project? project, ImmutableDictionary<string, string>? environment, ImmutableList<Message> messages, DateTime startTime, DateTime endTime, Result result)
            : base(messages, startTime, endTime, result)
        {
            Project = project;
            Environment = environment;
        }
    }
}
