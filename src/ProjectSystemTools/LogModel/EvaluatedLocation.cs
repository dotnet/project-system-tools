// Copyright (c) Microsoft. All Rights Reserved. Licensed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.Build.Framework.Profiler;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.LogModel
{
    internal sealed class EvaluatedLocation
    {
        public string ElementName { get; }
        public string ElementDescription { get; }
        public EvaluationLocationKind Kind { get; }
        public string File { get; }
        public int? Line { get; }
        public ImmutableArray<EvaluatedLocation> Children { get; }
        public Time Time { get; }

        public EvaluatedLocation(string elementName, string elementDescription, EvaluationLocationKind kind, string file, int? line, IEnumerable<EvaluatedLocation> children, Time time)
        {
            ElementName = elementName;
            ElementDescription = elementDescription;
            Kind = kind;
            File = file;
            Line = line;
            Children = children.ToImmutableArray();
            Time = time;
        }
    }
}
