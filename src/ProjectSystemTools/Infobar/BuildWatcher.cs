// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging;
using Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.Model;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.Infobar
{
    internal class BuildWatcher
    {
        // TODO read this value from  the remote control file
        private const double ThreshHold = 0.3;
        private readonly IInfoBarService _infobarService;
        private readonly IBuildTableDataSource _buildTableDataSource;

        private static bool IsInfoBarShowing = false;

        private static readonly InfoBarUI OpenToolWindowButton = new InfoBarUI("Show Me", InfoBarUI.UIKind.Button, async () => await LaunchToolWindowAsync());
        private static readonly InfoBarUI IgnoreButton = new InfoBarUI("Ignore", InfoBarUI.UIKind.Button, () => DisableWatcherForSession());
        private static readonly InfoBarUI IgnoreForeverButton = new InfoBarUI("Ignore", InfoBarUI.UIKind.Button, () => DisableWatcherForAllSessions());
        private static readonly InfoBarUI CloseButton = new InfoBarUI(string.Empty, InfoBarUI.UIKind.Close, () => IsInfoBarShowing = false);

        [ImportingConstructor]
        public BuildWatcher(IInfoBarService infobarService, IBuildTableDataSource buildTableDataSource)
        {
            _infobarService = infobarService;
            _buildTableDataSource = buildTableDataSource;
        }

        public void StartListening()
        {
            _buildTableDataSource.OnBuildCompleted += OnBuildCompleted;
            _buildTableDataSource.Start();
        }

        public void StopListening()
        {
            _buildTableDataSource.OnBuildCompleted -= OnBuildCompleted;
            _buildTableDataSource.Stop();
        }

        private void OnBuildCompleted(object sender, BuildCompletedEventArgs e)
        {
            // run work on the threadpool
            Task.Run(async () =>
            {
                var totalBuildTime = e.Build.Elapsed;
                var percentage = 0.0;
                string targetName = null;
                foreach (var target in e.Build.Targets.Values)
                {
                    if (IsKnowTarget(target.Name))
                    {
                        continue;
                    }

                    var targetElapsedTime = target.Elapsed;
                    var newPercentage = targetElapsedTime.TotalMilliseconds / totalBuildTime.TotalMilliseconds;
                    if (newPercentage > ThreshHold && newPercentage > percentage)
                    {
                        targetName = target.Name;
                        percentage = newPercentage;
                    }
                }

                if (targetName != null)
                {
                    await ThreadHelper.JoinableTaskFactory.RunAsync(() => LaunchInfobarAsync(targetName, percentage));
                }
            });

        }

        private async Task LaunchInfobarAsync(string targetName, double percentage)
        {
            // check if infobar is already up
            if (IsInfoBarShowing)
            {
                return;
            }
            IsInfoBarShowing = true;

            await _infobarService.ShowInfoBarInGlobalViewAsync(
                $"Target {targetName} is taking up {percentage:P} of the build time.",
                OpenToolWindowButton,
                IgnoreButton,
                CloseButton);
        }

        private static async Task LaunchToolWindowAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var window = ProjectSystemToolsPackage.Instance?.FindToolWindow(typeof(BuildLoggingToolWindow), 0, true);
            if (window?.Frame == null)
            {
                throw new NotSupportedException("Cannot create tool window");
            }

            var windowFrame = (IVsWindowFrame)window.Frame;
            ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }

        private static void DisableWatcherForAllSessions()
        {
            // TODO  enable per session disable/enable
            IsInfoBarShowing = false;
        }

        private static void DisableWatcherForSession()
        {
            // TODO  enable per session disable/enable
            IsInfoBarShowing = false;
        }

        private bool IsKnowTarget(string name)
        {
            // TODO  get list of known targets from  the remote control file
            return false;
        }
    }
}
