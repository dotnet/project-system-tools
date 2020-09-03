using System;

namespace Microsoft.VisualStudio.ProjectSystemTools.FrontEnd
{
    public class Message
    {
        public DateTime Timestamp { get; }
        public string Text { get; }

        public Message(DateTime timestamp, string text)
        {
            Timestamp = timestamp;
            Text = text;
        }
    }
}
