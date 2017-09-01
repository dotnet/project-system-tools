namespace Microsoft.VisualStudio.ProjectSystem.Tools.LogModel
{
    internal sealed class Item : NodeWithName
    {
        public string Text { get; set; }

        public string NameAndEquals => string.IsNullOrWhiteSpace(Name) ? "" : Name + " = ";

        public override string ToString()
        {
            return string.IsNullOrWhiteSpace(Name) ? Text : NameAndEquals + Text;
        }
    }
}
