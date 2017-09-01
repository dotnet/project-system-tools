using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.LogModel
{
    internal sealed class Build : NodeWithTiming
    {
        public bool Succeeded { get; set; }

        public ImmutableDictionary<string, string> Environment { get; set; }

        public override string ToString() => "Build " + (Succeeded ? "succeeded" : "failed");
    }
}
