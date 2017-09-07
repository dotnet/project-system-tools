using System;
using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.LogModel
{
    internal class Task : Node
    {
        public Task(ImmutableList<string> messages, DateTime startTime, DateTime endTime, bool succeeded, int nodeId, string name, string fromAssembly, string commandLineArguments, string sourceFilePath) : base(messages, startTime, endTime, succeeded)
        {
            NodeId = nodeId;
            Name = name;
            FromAssembly = fromAssembly;
            CommandLineArguments = commandLineArguments;
            SourceFilePath = sourceFilePath;
        }

        public int NodeId { get; }
        public string Name { get; set; }
        public string FromAssembly { get; set; }
        public string CommandLineArguments { get; set; }
        public string SourceFilePath { get; set; }

        public override string ToString() => $"{Name}";
    }
}
