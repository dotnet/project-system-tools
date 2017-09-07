using System;
using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.LogModel
{
    internal sealed class Project : Node
    {
        public int NodeId { get; }

        public string Name { get; }

        public string ProjectFile { get; }

        public ImmutableDictionary<string, string> GlobalProperties { get; }

        public ImmutableDictionary<string, string> Properties { get; }

        public ImmutableList<ItemGroup> ItemGroups { get; }

        public ImmutableArray<string> TargetNames { get; }

        public string ToolsVersion { get; }

        public Project(int nodeId, string name, string projectFile, ImmutableDictionary<string, string> globalProperties, ImmutableDictionary<string, string> properties, ImmutableList<ItemGroup> itemGroups, ImmutableArray<string> targetNames, string toolsVersion, ImmutableList<string> messages, DateTime startTime, DateTime endTime, bool succeeded)
            : base(messages, startTime, endTime, succeeded)
        {
            NodeId = nodeId;
            Name = name;
            ProjectFile = projectFile;
            GlobalProperties = globalProperties;
            ItemGroups = itemGroups;
            Properties = properties;
            TargetNames = targetNames;
            ToolsVersion = toolsVersion;
        }
    }
}
