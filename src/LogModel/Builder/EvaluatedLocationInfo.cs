// Copyright (c) Microsoft. All Rights Reserved. Licensed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.Build.Framework.Profiler;

namespace Microsoft.VisualStudio.ProjectSystem.LogModel.Builder
{
    internal sealed class EvaluatedLocationInfo
    {
        public string ElementName { get; }
        public string ElementDescription { get; }
        public EvaluationLocationKind Kind { get; }
        public string File { get; }
        public int? Line { get; }
        public ImmutableArray<EvaluatedLocationInfo> Children { get; }
        public TimeInfo Time { get; }

        public EvaluatedLocationInfo(string elementName, string elementDescription, EvaluationLocationKind kind, string file, int? line, IEnumerable<EvaluatedLocationInfo>? children, TimeInfo time)
        {
            ElementName = elementName;
            ElementDescription = elementDescription;
            Kind = kind;
            File = file;
            Line = line;
            Children = children?.ToImmutableArray() ?? ImmutableArray<EvaluatedLocationInfo>.Empty;
            Time = time;
        }
    }
}
