﻿// Copyright (c) Microsoft. All Rights Reserved. Licensed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Windows;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.TableControl
{
    [Export(typeof(ITableColumnDefinition))]
    [Name(TableColumnNames.ProjectType)]
    internal sealed class ProjectTypeColumnDefinition : TableColumnDefinitionBase
    {
        public override string Name => TableColumnNames.ProjectType;

        public override string DisplayName => TableControlResources.ProjectTypeHeaderLabel;

        public override StringComparer Comparer => StringComparer.Ordinal;

        public override double MinWidth => 50.0;

        public override TextWrapping TextWrapping => TextWrapping.NoWrap;

        public override bool TryCreateStringContent(ITableEntryHandle entry, bool truncatedText, bool singleColumnView, out string? content)
        {
            if (entry.TryGetValue(TableKeyNames.ProjectType, out var value) && value is string projectType)
            {
                content = projectType;
                return true;
            }

            content = null;
            return false;
        }
    }
}
