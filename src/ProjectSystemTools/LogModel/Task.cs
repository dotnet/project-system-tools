namespace Microsoft.VisualStudio.ProjectSystem.Tools.LogModel
{
    internal class Task : NodeWithTiming
    {
        public string FromAssembly { get; set; }
        public string CommandLineArguments { get; set; }
        public string SourceFilePath { get; set; }

        public override string ToString() => $"{Name}";
    }
}
