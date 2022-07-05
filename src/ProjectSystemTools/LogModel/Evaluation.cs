// Copyright (c) Microsoft. All Rights Reserved. Licensed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.LogModel
{
    internal sealed class Evaluation
    {
        public ImmutableList<Message> Messages { get; }

        public ImmutableList<EvaluatedProject> EvaluatedProjects { get; }

        public Evaluation(ImmutableList<Message> messages, ImmutableList<EvaluatedProject> evaluatedProjects)
        {
            Messages = messages;
            EvaluatedProjects = evaluatedProjects;
        }
    }
}
