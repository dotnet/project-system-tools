using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.LogModel
{
    internal sealed class Target : NodeWithTiming
    {
        public bool Succeeded { get; internal set; }
        public string DependsOnTargets { get; set; }
        public Project Project => GetNearestParent<Project>();
        public string SourceFilePath { get; internal set; }
        public ImmutableList<Item> OutputItems { get; set; }

        public Target()
        {
            Id = -1;
        }

        public void TryAddTarget(Target target)
        {
            if (target.Parent == null)
            {
                AddChild(target);
            }
        }

        public Task GetTaskById(int taskId)
        {
            return Children.OfType<Task>().First(t => t.Id == taskId);
        }

        public override string ToString()
        {
            return $"Target Name={Name} Project={Path.GetFileName(Project?.ProjectFile)}";
        }
    }
}
