using System;
using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.LogModel
{
    internal sealed class EvaluatedProject
    {
        public string Name { get; }

        public ImmutableList<string> Messages { get; }

        public EvaluatedProject(string name, ImmutableList<string> messages)
        {
            Name = name;
            Messages = messages;
        }
    }
}
