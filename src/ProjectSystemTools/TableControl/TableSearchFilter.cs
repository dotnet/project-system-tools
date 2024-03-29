﻿// Copyright (c) Microsoft. All Rights Reserved. Licensed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableControl;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.TableControl
{
    internal class TableSearchFilter : IEntryFilter
    {
        // NOTE methods on this type are invoked on a worker thread

        private readonly IEnumerable<IVsSearchToken> _searchTokens;
        private readonly IReadOnlyList<ITableColumnDefinition> _visibleColumns;

        public TableSearchFilter(IVsSearchQuery searchQuery, IWpfTableControl control)
        {
            _searchTokens = SearchUtilities.ExtractSearchTokens(searchQuery) ?? Array.Empty<IVsSearchToken>();

            var newVisibleColumns = control.ColumnStates
                .Where(c => c.IsVisible || (c as ColumnState2)?.GroupingPriority > 0)
                .Select(c => control.ColumnDefinitionManager.GetColumnDefinition(c.Name))
                .Where(definition => definition != null).ToList();

            _visibleColumns = newVisibleColumns;
        }

        public bool Match(ITableEntryHandle entry)
        {
            var cachedColumnValues = new string[_visibleColumns.Count + 1];

#pragma warning disable VSTHRD010 // Invoke single-threaded types on Main thread
            return _searchTokens.Where(searchToken => searchToken is not IVsSearchFilterToken)
                .All(searchToken => AtLeastOneColumnOrDetailsContentMatches(entry, searchToken,
                    cachedColumnValues));
#pragma warning restore VSTHRD010 // Invoke single-threaded types on Main thread
        }

        private bool AtLeastOneColumnOrDetailsContentMatches(ITableEntryHandle entry, IVsSearchToken searchToken, string[] cachedColumnValues)
        {
            if (cachedColumnValues[0] == null)
            {
                cachedColumnValues[0] = GetDetailsContentAsString(entry);
            }

            var detailsContent = cachedColumnValues[0];
            if (detailsContent != null && Match(detailsContent, searchToken))
            {
                return true;
            }

            for (var i = 0; i < _visibleColumns.Count; i++)
            {
                if (cachedColumnValues[i + 1] == null)
                {
                    cachedColumnValues[i + 1] = GetColumnValueAsString(entry, _visibleColumns[i]);
                }

                var columnValue = cachedColumnValues[i + 1];
                System.Diagnostics.Debug.Assert(columnValue != null);

                if (columnValue != null && Match(columnValue, searchToken))
                {
                    // Found match in this column
                    return true;
                }
            }

            // No match found in this entry
            return false;
        }

        private static string GetColumnValueAsString(ITableEntryHandle entry, ITableColumnDefinition column) =>
            entry.TryCreateStringContent(column, truncatedText: false, singleColumnView: false, content: out string columnValue) && (columnValue != null)
                ? columnValue : string.Empty;

        private static string GetDetailsContentAsString(ITableEntryHandle entry)
        {
            string? detailsString = null;

            if (entry.CanShowDetails && entry is IWpfTableEntry wpfEntry)
            {
                wpfEntry.TryCreateDetailsStringContent(out detailsString);
            }

            return detailsString ?? string.Empty;
        }

        private static bool Match(string columnValue, IVsSearchToken searchToken)
        {
#pragma warning disable VSTHRD010 // Invoke single-threaded types on Main thread
            return
                columnValue != null &&
                columnValue.IndexOf(searchToken.ParsedTokenText, StringComparison.OrdinalIgnoreCase) >= 0;
#pragma warning restore VSTHRD010 // Invoke single-threaded types on Main thread
        }
    }
}
