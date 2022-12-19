// Copyright (c) Microsoft. All Rights Reserved. Licensed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.Model;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.TableControl
{
    [Export(typeof(ITableColumnDefinition))]
    [Name(TableColumnNames.Elapsed)]
    internal sealed class ElapsedColumnDefinition : TableColumnDefinitionBase
    {
        public override string Name => TableColumnNames.Elapsed;

        public override string DisplayName => TableControlResources.ElapsedHeaderLabel;

        public override StringComparer Comparer => StringComparer.Ordinal;

        public override double MinWidth => 60.0;

        public override GridLength ColumnDefinition => new(60);

        public override TextWrapping TextWrapping => TextWrapping.NoWrap;

        public override bool TryCreateColumnContent(ITableEntryHandle entry, bool singleColumnView, out FrameworkElement? content)
        {
            if (TryCreateStringContent(entry, false, singleColumnView, out string? text))
            {
                content = new TextBlock
                {
                    Text = text,
                    TextAlignment = TextAlignment.Right
                };
                return true;
            }

            content = null;
            return false;
        }

        public override bool TryCreateStringContent(ITableEntryHandle entry, bool truncatedText, bool singleColumnView, out string? content)
        {
            if (entry.TryGetValue(TableKeyNames.Status, out var status) && status is BuildStatus and not BuildStatus.Running &&
                entry.TryGetValue(TableKeyNames.Elapsed, out var value) && value is TimeSpan timeSpan)
            {
                content = timeSpan.TotalSeconds.ToString("N3");
                return true;
            }

            content = null;
            return false;
        }
    }
}
