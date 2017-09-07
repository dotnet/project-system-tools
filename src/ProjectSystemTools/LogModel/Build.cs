using System;
using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.LogModel
{
    internal sealed class Build : Node
    {
        public Project Project { get; }

        public ImmutableDictionary<string, string> Environment { get; }

        public ImmutableList<EvaluatedProject> EvaluatedProjects { get; }

        public Build(ImmutableList<string> messages, DateTime startTime, DateTime endTime, Project project, bool succeeded, ImmutableDictionary<string, string> environment, ImmutableList<EvaluatedProject> evaluatedProjects)
            : base(messages, startTime, endTime, succeeded)
        {
            Project = project;
            Environment = environment;
            EvaluatedProjects = evaluatedProjects;
        }
    }
}
