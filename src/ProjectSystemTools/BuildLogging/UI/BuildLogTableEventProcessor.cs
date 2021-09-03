// Copyright (c) Microsoft. All Rights Reserved. Licensed under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.TableControl;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.UI
{
    internal class BuildLogTableEventProcessor : TableControlEventProcessorBase
    {
        private readonly BuildLoggingToolWindow _toolWindow;

        public BuildLogTableEventProcessor(BuildLoggingToolWindow toolWindow)
        {
            _toolWindow = toolWindow;
        }

        public override void PostprocessNavigate(ITableEntryHandle entryHandle, TableEntryNavigateEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            _toolWindow.OpenLog(entryHandle);
        }

        public override void PreprocessSelectionChanged(TableSelectionChangedEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            ProjectSystemToolsPackage.UpdateQueryStatus();

            base.PreprocessSelectionChanged(e);
        }
    }
}
