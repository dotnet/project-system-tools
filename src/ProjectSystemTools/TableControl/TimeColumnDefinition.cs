﻿// Copyright (c) Microsoft. All Rights Reserved. Licensed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Windows;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.TableControl
{
    [Export(typeof(ITableColumnDefinition))]
    [Name(TableColumnNames.Time)]
    internal sealed class TimeColumnDefinition : TableColumnDefinitionBase
    {
        public override string Name => TableColumnNames.Time;

        public override string DisplayName => TableControlResources.TimeHeaderLabel;

        public override StringComparer Comparer => StringComparer.Ordinal;

        public override double MinWidth => 100.0;

        public override TextWrapping TextWrapping => TextWrapping.NoWrap;

        public override bool TryCreateStringContent(ITableEntryHandle entry, bool truncatedText, bool singleColumnView, out string? content)
        {
            if (entry.TryGetValue(TableKeyNames.Time, out var value) && value is DateTime time)
            {
                content = time.ToString("s");
                return true;
            }

            content = null;
            return false;
        }
    }
}
