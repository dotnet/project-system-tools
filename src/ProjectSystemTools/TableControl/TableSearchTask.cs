﻿// Copyright (c) Microsoft. All Rights Reserved. Licensed under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableControl;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.TableControl
{
    internal class TableSearchTask : VsSearchTask
    {
        private readonly IWpfTableControl _control;

        public TableSearchTask(uint dwCookie, IVsSearchQuery pSearchQuery, IVsSearchCallback pSearchCallback, IWpfTableControl control)
            : base(dwCookie, pSearchQuery, pSearchCallback)
        {
            _control = control;
        }

        protected override void OnStartSearch()
        {
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                _control.SetFilter(TableToolWindow.SearchFilterKey, new TableSearchFilter(SearchQuery, _control));

                SearchCallback.ReportComplete(this, dwResultsFound: 0);
            });
        }

        protected override void OnStopSearch()
        {
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                
                _control.SetFilter(TableToolWindow.SearchFilterKey, null);
            });
        }
    }
}
