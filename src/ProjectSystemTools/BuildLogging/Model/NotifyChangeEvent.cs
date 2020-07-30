using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.Model
{
    class NotifyChangeEvent
    {
        public event EventHandler DataChanged;

        protected virtual void OnDataChanged(EventArgs e) 
        {
            EventHandler handler = DataChanged;
            handler?.Invoke(this, e);
        }
    }
}
