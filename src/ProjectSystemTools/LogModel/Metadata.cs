namespace Microsoft.VisualStudio.ProjectSystem.Tools.LogModel
{
    internal sealed class Metadata
    {
        public string Name { get; set; }
        public string Value { get; set; }

        public override string ToString() => Name + " = " + Value;
    }
}
