// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.Infobar
{
    public interface IInfoBarService
    {
        /// <summary>
        /// Show an info bar in the current active view.
        ///
        /// Different hosts can have different definitions on what active view means.
        /// </summary>
        Task ShowInfoBarInActiveViewAsync(string message, params InfoBarUI[] items);

        /// <summary>
        /// Show global info bar
        /// </summary>
        Task ShowInfoBarInGlobalViewAsync(string message, params InfoBarUI[] items);
    }
}
