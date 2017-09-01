using System;
using System.Text.RegularExpressions;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.LogModel
{
    internal sealed class Message : NodeWithName
    {
        public string Text { get; set; }

        public override string LookupKey => Text;

        private static readonly Regex PropertyReassignment = new Regex(@"^Property reassignment: \$\(\w+\)=.+ \(previous value: .*\) at (?<File>.*) \((?<Line>\d+),(\d+)\)$", RegexOptions.Compiled);

        public string SourceFilePath
        {
            get
            {
                var match = GetSourceFileMatch();
                return match.Success ? match.Groups["File"].Value : null;
            }
        }

        public override string ToString() => base.ToString() + " " + Text;

        private Match GetSourceFileMatch() => PropertyReassignment.Match(Text);

        // These are recalculated and not stored because storage in this class is incredibly expensive
        // There are millions of Message instances in a decent size log
        public int? LineNumber
        {
            get
            {
                var match = GetSourceFileMatch();
                if (match.Success)
                {
                    return int.Parse(match.Groups["Line"].Value);
                }

                return null;
            }
        }
    }
}
