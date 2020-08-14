using System;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.Model
{
    public class DataChangedEventArgs : EventArgs
    {
        public bool Test;

        public DataChangedEventArgs(bool test)
        {
            Test = test;
        }
    }
}
