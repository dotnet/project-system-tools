using System;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.LogModel
{
    internal sealed class Diagnostic
    {
        public bool IsError { get; set; }
        public DateTime Timestamp { get; set; }
        public string Code { get; set; }
        public int ColumnNumber { get; set; }
        public int EndColumnNumber { get; set; }
        public int EndLineNumber { get; set; }
        public string File { get; set; }
        public int LineNumber { get; set; }
        public string ProjectFile { get; set; }
        public string Subcategory { get; set; }
        public string Text { get; set; }

        public override string ToString()
        {
            File = File ?? "";

            var position = "";
            if (LineNumber != 0 || ColumnNumber != 0)
            {
                position = $"({LineNumber},{ColumnNumber}):";
            }

            var code = "";
            if (!string.IsNullOrWhiteSpace(Code))
            {
                code = $" {this.GetType().Name.ToLowerInvariant()} {Code}:";
            }

            var text = Text;
            if (File.Length + position.Length + code.Length > 0)
            {
                text = " " + text;
            }

            var projectFile = "";
            if (!string.IsNullOrWhiteSpace(ProjectFile))
            {
                projectFile = $" [{ProjectFile}]";
            }

            return $"{File}{position}{code}{text}{projectFile}";
        }
    }
}
