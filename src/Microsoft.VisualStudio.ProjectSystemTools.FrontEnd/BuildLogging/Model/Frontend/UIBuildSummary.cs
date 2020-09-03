// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.VisualStudio.ProjectSystem.Tools.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.Model.FrontEnd
{
    /// <summary>
    /// Immutable Type
    /// 
    /// A wrapper class for the BuildSummary class,
    /// used only on the client side.
    /// This has some extra behavior to give the UI
    /// access to data on this UIBuildSummary, and sorting order.
    /// </summary>
    public sealed class UIBuildSummary : IComparable<UIBuildSummary>
    {
        private readonly BuildSummary buildSummary;

        public UIBuildSummary(BuildSummary other)
        {
            buildSummary = other;
        }

        public bool TryGetValue(string keyName, out object content)
        {
            content = keyName switch
            {
                TableKeyNames.Dimensions => buildSummary.Dimensions,
                TableKeyNames.Targets => buildSummary.Targets,
                TableKeyNames.Elapsed => buildSummary.Elapsed,
                TableKeyNames.BuildType => buildSummary.BuildType,
                TableKeyNames.Status => buildSummary.Status,
                StandardTableKeyNames.ProjectName => Path.GetFileNameWithoutExtension(buildSummary.ProjectName),
                TableKeyNames.ProjectType => Path.GetExtension(buildSummary.ProjectName),
                TableKeyNames.StartTime => buildSummary.StartTime,
                TableKeyNames.BuildID => buildSummary.BuildId,
                _ => null,
            };
            return content != null;
        }
        public int CompareTo(UIBuildSummary other)
        {
            if (ReferenceEquals(this, other))
            {
                return 0;
            }

            if (other is null)
            {
                return 1;
            }

            var startComparison = (int)(buildSummary.StartTime?.CompareTo(other.buildSummary.StartTime));
            return startComparison != 0 ? startComparison : string.Compare(buildSummary.ProjectName, other.buildSummary.ProjectName, StringComparison.Ordinal);
        }
    }
}
