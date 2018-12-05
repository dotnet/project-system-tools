// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using System.Threading;
using EnvDTE;
using EnvDTE80;
using Microsoft.Internal.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogExplorer;
using Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging;
using Microsoft.VisualStudio.ProjectSystem.Tools.ProjectQuery;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Threading;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.ProjectSystem.Tools
{
    [PackageRegistration(AllowsBackgroundLoading = true, UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    [Guid(PackageGuidString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideToolWindow(typeof(BuildLoggingToolWindow), Style = VsDockStyle.Tabbed, Window = ToolWindowGuids.Outputwindow)]
    [ProvideToolWindow(typeof(BuildLogExplorerToolWindow), Style = VsDockStyle.MDI)]
    [ProvideToolWindow(typeof(MessageListToolWindow), Style = VsDockStyle.Tabbed, Window = ToolWindowGuids.Outputwindow)]
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
        public const int BuildLogExplorerCommandId = 0x0108;
        public const int ExploreLogsCommandId = 0x0109;
        public const int AddLogCommandId = 0x010a;
        public const int MessageListCommandId = 0x010b;
        public const int QueryMSBuildPropertyCommandId = 0x010c;
        public const int QueryOutputGroupCommandId = 0x010d;

        public static readonly Guid UIGuid = new Guid("629080DF-2A44-40E5-9AF4-371D4B727D16");

        public const int BuildLoggingToolbarMenuId = 0x0100;
        public const int BuildLoggingContextMenuId = 0x0102;
        public const int BuildLogExplorerToolbarMenuId = 0x0104;
        public const int MessageListToolbarMenuId = 0x0106;

        private BuildLoggingToolWindow _buildLoggingToolWindow;
        private BuildLogExplorerToolWindow _buildLogExplorerToolWindow;

        private OutputWindowPane _projectQueryOutputWindowPane;

        public static IServiceProvider ServiceProvider { get; private set; }

        public BuildLoggingToolWindow BuildLoggingToolWindow => _buildLoggingToolWindow ?? (_buildLoggingToolWindow = (BuildLoggingToolWindow)FindToolWindow(typeof(BuildLoggingToolWindow), 0, true));
        public BuildLogExplorerToolWindow BuildLogExplorerToolWindow => _buildLogExplorerToolWindow ?? (_buildLogExplorerToolWindow = (BuildLogExplorerToolWindow)FindToolWindow(typeof(BuildLogExplorerToolWindow), 0, true));

        public OutputWindowPane ProjectQueryOutputPane => _projectQueryOutputWindowPane ?? (_projectQueryOutputWindowPane = (DTE.ToolWindows.OutputWindow.OutputWindowPanes.Add("Project Query")));

        public static IVsUIShell VsUIShell { get; private set; }

        public static IVsFindManager VsFindManager { get; private set; }

        private static DTE2 DTE { get; set; }

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

            VsUIShell = GetService(typeof(IVsUIShell)) as IVsUIShell;
            VsFindManager = GetService(typeof(SVsFindManager)) as IVsFindManager;
            DTE = GetService(typeof(SDTE)) as DTE2;

            var componentModel = GetService(typeof(SComponentModel)) as IComponentModel;
            TableControlProvider = componentModel?.GetService<IWpfTableControlProvider>();
            TableManagerProvider = componentModel?.GetService<ITableManagerProvider>();

            var mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            mcs?.AddCommand(new MenuCommand(ShowBuildLoggingToolWindow, new CommandID(CommandSetGuid, BuildLoggingCommandId)));
            mcs?.AddCommand(new MenuCommand(ShowBuildLogExplorerToolWindow, new CommandID(CommandSetGuid, BuildLogExplorerCommandId)));
            mcs?.AddCommand(new MenuCommand(ShowMessageListToolWindow, new CommandID(CommandSetGuid, MessageListCommandId)));
            mcs?.AddCommand(new MenuCommand(QueryMSBuildProperty, new CommandID(CommandSetGuid, QueryMSBuildPropertyCommandId)));
            mcs?.AddCommand(new MenuCommand(QueryOutputGroup, new CommandID(CommandSetGuid, QueryOutputGroupCommandId)));

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
            VsUIShell?.UpdateCommandUI(0);
        }

        private void ShowBuildLoggingToolWindow(object sender, EventArgs e)
        {
            var windowFrame = (IVsWindowFrame)BuildLoggingToolWindow.Frame;
            ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }

        private void ShowBuildLogExplorerToolWindow(object sender, EventArgs e)
        {
            var windowFrame = (IVsWindowFrame)BuildLogExplorerToolWindow.Frame;
            ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }

        private void ShowMessageListToolWindow(object sender, EventArgs e)
        {
            var window = FindToolWindow(typeof(MessageListToolWindow), 0, true);
            if (window?.Frame == null)
            {
                throw new NotSupportedException("Cannot create tool window");
            }

            var windowFrame = (IVsWindowFrame)window.Frame;
            ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }

        private void QueryMSBuildProperty(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var dialog = new ProjectQueryDialog("Get MSBuild Property", "Retrieves the value of an MSBuild property via the project's IVsBuildPropertyStorage implementation.");

            var retrieveProperty = dialog.ShowModal();
            if (!retrieveProperty.HasValue
                || !retrieveProperty.Value)
            {
                return;
            }

            ProjectQueryOutputPane.Activate();
            ShowOutputWindow();

            var propertyName = dialog.InputField.Text;

            var solution = GetService(typeof(SVsSolution)) as IVsSolution;
            if (solution == null)
            {
                return;
            }

            var guid = Guid.Empty;
            if (solution.GetProjectEnum((uint)__VSENUMPROJFLAGS.EPF_LOADEDINSOLUTION, ref guid, out IEnumHierarchies hierarchies) == VSConstants.S_OK)
            {
                var hierarchyArray = new IVsHierarchy[1];
                while (hierarchies.Next((uint)hierarchyArray.Length, hierarchyArray, out uint fetched) == 0
                        && fetched > 0)
                {
                    if (hierarchyArray[0] is IVsBuildPropertyStorage buildPropertyStorage
                        && buildPropertyStorage.GetPropertyValue(propertyName, null, (uint)_PersistStorageType.PST_PROJECT_FILE, out string propertyValue) == VSConstants.S_OK
                        && hierarchyArray[0].GetProperty((uint)VSConstants.VSITEMID.Root, (int)__VSHPROPID.VSHPROPID_Name, out object projectNameObject) == VSConstants.S_OK)
                    {
                        var projectName = (string)projectNameObject;
                        ProjectQueryOutputPane.OutputString($"$({propertyName}) in {projectName} = {propertyValue}\r\n");
                    }
                }
            }
        }

        private void QueryOutputGroup(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var dialog = new ProjectQueryDialog("Get Output Group", "Retrieves the list of items in the specified output group.");

            var retrieveOutputGroup = dialog.ShowModal();
            if (!retrieveOutputGroup.HasValue
                || !retrieveOutputGroup.Value)
            {
                return;
            }

            ProjectQueryOutputPane.Activate();
            ShowOutputWindow();

            var outputGroupName = dialog.InputField.Text;
            foreach (Project project in DTE.Solution.Projects)
            {
                var configurationManager = project.ConfigurationManager;
                if (configurationManager == null)
                {
                    return;
                }

                var activeConfiguration = configurationManager.ActiveConfiguration;
                if (activeConfiguration == null)
                {
                    return;
                }

                var outputGroups = activeConfiguration.OutputGroups;
                if (outputGroups == null)
                {
                    return;
                }

                var outputGroup = outputGroups.Item(outputGroupName);
                if (outputGroup == null)
                {
                    return;
                }

                ProjectQueryOutputPane.OutputString($"Output group '{outputGroupName}' in project {project.Name}:\r\n");
                Array fileNames = (Array)outputGroup.FileNames;
                foreach (string fileName in fileNames)
                {
                    ProjectQueryOutputPane.OutputString($"  {fileName}\r\n");
                }
            };
        }

        private void ShowOutputWindow()
        {
            var outputWindow = DTE.Windows.Item(EnvDTE.Constants.vsWindowKindOutput);
            outputWindow.Activate();
        }
    }
}
