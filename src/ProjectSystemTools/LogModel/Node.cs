using System;
using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.LogModel
{
    internal abstract class Node
    {
        public DateTime StartTime { get; }
        public DateTime EndTime { get; }
        public TimeSpan Duration => EndTime - StartTime;

        public ImmutableList<string> Messages { get; }

        public bool Succeeded { get; }

        protected Node(ImmutableList<string> messages, DateTime startTime, DateTime endTime, bool succeeded)
        {
            Messages = messages;
            StartTime = startTime;
            EndTime = endTime;
            Succeeded = succeeded;
        }

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
