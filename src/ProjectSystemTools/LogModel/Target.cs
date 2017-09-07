using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.LogModel
{
    internal sealed class Target : Node
    {
        public Target(ImmutableList<string> messages, DateTime startTime, DateTime endTime, bool succeeded, int nodeId, string name, string dependsOnTargets, string sourceFilePath, ImmutableList<Item> outputItems) : base(messages, startTime, endTime, succeeded)
        {
            NodeId = nodeId;
            Name = name;
            DependsOnTargets = dependsOnTargets;
            SourceFilePath = sourceFilePath;
            OutputItems = outputItems;
        }

        public int NodeId { get; }
        public string Name { get; set; }
        public string DependsOnTargets { get; set; }
        public string SourceFilePath { get; internal set; }
        public ImmutableList<Item> OutputItems { get; set; }

    }
}
