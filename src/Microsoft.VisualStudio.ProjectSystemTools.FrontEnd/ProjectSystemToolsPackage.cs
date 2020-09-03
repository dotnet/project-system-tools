﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;
using System.ComponentModel.Design;
using Microsoft.Internal.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.ProjectSystem.Tools.BinaryLogEditor;
using Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.Tools
{
    [PackageRegistration(AllowsBackgroundLoading = true, UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    [Guid(PackageGuidString)]
    [ProvideMenuResource("Menus.ctmenu", 2)]
    [ProvideToolWindow(typeof(BuildLoggingToolWindow), Style = VsDockStyle.Tabbed, Window = ToolWindowGuids.Outputwindow)]
    [ProvideToolWindow(typeof(MessageListToolWindow), Style = VsDockStyle.Tabbed, Window = ToolWindowGuids.Outputwindow)]
    [ProvideEditorExtension(typeof(BinaryLogEditorFactory), ".binlog", 0x50, NameResourceID = 113)]
    [ProvideEditorLogicalView(typeof(BinaryLogEditorFactory), LogicalViewID.Designer)]
    [ProvideEditorFactory(typeof(BinaryLogEditorFactory), 113)]
    internal sealed class ProjectSystemToolsPackage : AsyncPackage
    {
        public const string PackageGuidString = "e3bfb509-b8fd-4692-b4c4-4b2f6ed62bc7";

        public static readonly Guid CommandSetGuid = new Guid("cf0c6f43-4716-4419-93d0-2c246c8eb5ee");

        public const int BuildLoggingCommandId = 0x0100;
        public const int StartLoggingCommandId = 0x0101;
        public const int StopLoggingCommandId = 0x0102;
        public const int ClearCommandId = 0x0103;
        public const int SaveLogsCommandId = 0x0104;
        public const int OpenLogsCommandId = 0x0105;
        public const int BuildTypeComboCommandId = 0x0106;
        public const int BuildTypeComboGetListCommandId = 0x0107;
        public const int MessageListCommandId = 0x010b;
        public const int OpenLogsExternalCommandId = 0x010c;

        public const int LogRoslynWorkspaceStructureCommandId = 0x0200;

        public static readonly Guid UIGuid = new Guid("629080DF-2A44-40E5-9AF4-371D4B727D16");

        public const int BuildLoggingToolbarMenuId = 0x0100;
        public const int BuildLoggingContextMenuId = 0x0102;
        public const int MessageListToolbarMenuId = 0x0106;

        public const string BinaryLogEditorFactoryGuidString = "C5A2E7ED-F7E7-4199-BD68-17668AA2F2D4";
        public static readonly Guid BinaryLogEditorFactoryGuid = new Guid(BinaryLogEditorFactoryGuidString);

        public const string BinaryLogEditorUIContextGuidString = "6B0A6B53-F2AA-41A6-AE25-7C7E8F2D2CAE";
        public static readonly Guid BinaryLogEditorUIContextGuid = new Guid(BinaryLogEditorUIContextGuidString);

        private BuildLoggingToolWindow _buildLoggingToolWindow;

        public static IServiceProvider ServiceProvider { get; private set; }

        public BuildLoggingToolWindow BuildLoggingToolWindow => _buildLoggingToolWindow ??= (BuildLoggingToolWindow)FindToolWindow(typeof(BuildLoggingToolWindow), 0, true);

        public static IVsUIShell VsUIShell { get; private set; }

        public static IVsFindManager VsFindManager { get; private set; }

        public static ProjectSystemToolsPackage Instance;

        internal static ITableManagerProvider TableManagerProvider { get; private set; }
        public static IWpfTableControlProvider TableControlProvider { get; private set; }

        private static JoinableTaskCollection PackageTaskCollection { get; set; }
        public static JoinableTaskFactory PackageTaskFactory { get; private set; }

        public static bool IsDisposed { get; private set; }

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            ServiceProvider = this;

            PackageTaskCollection = ThreadHelper.JoinableTaskContext.CreateCollection();
            PackageTaskFactory = ThreadHelper.JoinableTaskContext.CreateFactory(PackageTaskCollection);

            VsUIShell = await GetServiceAsync(typeof(IVsUIShell)) as IVsUIShell;
            VsFindManager = await GetServiceAsync(typeof(SVsFindManager)) as IVsFindManager;

            var componentModel = await GetServiceAsync(typeof(SComponentModel)) as IComponentModel;
            TableControlProvider = componentModel?.GetService<IWpfTableControlProvider>();
            TableManagerProvider = componentModel?.GetService<ITableManagerProvider>();

            var mcs = await GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            mcs?.AddCommand(new MenuCommand(ShowBuildLoggingToolWindow, new CommandID(CommandSetGuid, BuildLoggingCommandId)));
            mcs?.AddCommand(new MenuCommand(ShowMessageListToolWindow, new CommandID(CommandSetGuid, MessageListCommandId)));
            mcs?.AddCommand(new MenuCommand(LogRoslynWorkspaceStructure, new CommandID(CommandSetGuid, LogRoslynWorkspaceStructureCommandId)));

            RegisterEditorFactory(new BinaryLogEditorFactory());

            Instance = this;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                IsDisposed = true;
            }

            base.Dispose(disposing);
        }

        public static void UpdateQueryStatus()
        {
            // Force the shell to refresh the QueryStatus for all the command since some of them may have been flagged as
            // not supported (because the host had focus but the view did not) and switching focus from the zoom control
            // back to the view will not automatically force the shell to requery for the command status.
            ThreadHelper.ThrowIfNotOnUIThread();
            VsUIShell?.UpdateCommandUI(0);
        }

        private void ShowBuildLoggingToolWindow(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var windowFrame = (IVsWindowFrame)BuildLoggingToolWindow.Frame;
            ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }

        private void ShowMessageListToolWindow(object sender, EventArgs e)
        {
            var window = FindToolWindow(typeof(MessageListToolWindow), 0, true);
            if (window?.Frame == null)
            {
                throw new NotSupportedException("Cannot create tool window");
            }
            ThreadHelper.ThrowIfNotOnUIThread();
            var windowFrame = (IVsWindowFrame)window.Frame;
            ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }

        private void LogRoslynWorkspaceStructure(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            RoslynLogging.RoslynWorkspaceStructureLogger.ShowSaveDialogAndLog(ServiceProvider);
        }

    }
}
