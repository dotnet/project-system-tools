﻿// Copyright (c) Microsoft. All Rights Reserved. Licensed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.UI
{
    [Export(typeof(ITableControlEventProcessorProvider))]
    [Name(BuildLog)]
    [VisualStudio.Utilities.Order(After = Priority.Default, Before = StandardTableControlEventProcessors.Default)]
    [ManagerType(BuildLoggingToolWindow.BuildLogging)]
    [DataSourceType(StandardTableDataSources.AnyDataSource)]
    [DataSource(StandardTableDataSources.AnyDataSource)]
    internal class BuildLogTableEventProcessorProvider : ITableControlEventProcessorProvider
    {
        public const string BuildLog = "Build Log Table Control Event Processor";

        public ITableControlEventProcessor GetAssociatedEventProcessor(IWpfTableControl tableControl)
        {
            Assumes.Present(ProjectSystemToolsPackage.Instance);

            return new BuildLogTableEventProcessor(ProjectSystemToolsPackage.Instance.BuildLoggingToolWindow);
        }
    }
}
