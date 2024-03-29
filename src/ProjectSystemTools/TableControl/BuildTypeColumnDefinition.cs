﻿// Copyright (c) Microsoft. All Rights Reserved. Licensed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Windows;
using Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.Model;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.TableControl
{
    [Export(typeof(ITableColumnDefinition))]
    [Name(TableColumnNames.BuildType)]
    internal sealed class BuildTypeColumnDefinition : TableColumnDefinitionBase
    {
        public override string Name => TableColumnNames.BuildType;

        public override string DisplayName => TableControlResources.BuildTypeHeaderLabel;

        public override StringComparer Comparer => StringComparer.Ordinal;

        public override double MinWidth => 100.0;

        public override TextWrapping TextWrapping => TextWrapping.NoWrap;

        public override bool TryCreateStringContent(ITableEntryHandle entry, bool truncatedText, bool singleColumnView, out string? content)
        {
            if (entry.TryGetValue(TableKeyNames.BuildType, out var value) && value is BuildType buildType)
            {
                content = buildType.ToString();
                return true;
            }

            content = null;
            return false;
        }
    }
}
