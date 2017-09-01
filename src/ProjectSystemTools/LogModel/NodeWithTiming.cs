using System;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.LogModel
{
    internal abstract class NodeWithTiming : NodeWithName
    {
        public int Id { get; set; }
        public int NodeId { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration => EndTime - StartTime;

        public string DurationText
        {
            get
            {
                var result = Duration.ToString(@"s\.fff");
                return result == "0.000" ? "" : $" ({result}s)";
            }
        }
    }
}
