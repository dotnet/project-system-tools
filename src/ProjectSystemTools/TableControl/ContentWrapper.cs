﻿// Copyright (c) Microsoft. All Rights Reserved. Licensed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.TableControl
{
    internal class ContentWrapper : Border
    {
        private readonly int _contextMenuId;

        internal ContentWrapper(int contextMenuId)
        {
            _contextMenuId = contextMenuId;
        }

        protected override void OnPreviewMouseRightButtonUp(MouseButtonEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            OpenContextMenu();
        }

        internal static bool PreProcessMessage(ref Message m, IOleCommandTarget cmdTarget)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            return m.Msg == 0x007B &&
                   ErrorHandler.Succeeded(cmdTarget.Exec(VSConstants.VSStd2K, (uint)VSConstants.VSStd2KCmdID.SHOWCONTEXTMENU, 0, IntPtr.Zero, IntPtr.Zero));
        }

        internal void OpenContextMenu()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (_contextMenuId == -1)
            {
                return;
            }

            var guidContextMenu = ProjectSystemToolsPackage.UIGuid;
            var location = GetContextMenuLocation();
            var locationPoints = new[] { new POINTS { x = (short)location.X, y = (short)location.Y } };

            // Show context menu blocks, so we need to yield out of this method
            // for e.Handled to be noticed by WPF

            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                Assumes.Present(ProjectSystemToolsPackage.VsUIShell);

                ProjectSystemToolsPackage.VsUIShell.ShowContextMenu(0, ref guidContextMenu, _contextMenuId,
                    locationPoints, pCmdTrgtActive: null);
            });
        }

        // Default to the bottom-left corner of the control for the position of context menu invoked from keyboard
        private Point GetKeyboardContextMenuAnchorPoint() => PointToScreen(new Point(0, RenderSize.Height));

        // Get the current mouse position and convert it to screen coordinates as the shell expects a screen position
        private Point GetContextMenuLocation() =>
            InputManager.Current.MostRecentInputDevice is KeyboardDevice
                ? GetKeyboardContextMenuAnchorPoint()
                : PointToScreen(Mouse.GetPosition(this));
    }
}
