﻿// Copyright (c) Microsoft. All Rights Reserved. Licensed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.Build.Framework.Profiler;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.LogModel.Builder
{
    internal sealed class EvaluatedPassInfo
    {
        public EvaluationPass Pass { get; }
        public string Description { get; }
        public ImmutableArray<EvaluatedLocationInfo> Locations { get; }
        public TimeInfo Time { get; }

        public EvaluatedPassInfo(EvaluationPass pass, string description, IEnumerable<EvaluatedLocationInfo>? locations, TimeInfo time)
        {
            Pass = pass;
            Description = description;
            Locations = locations?.ToImmutableArray() ?? ImmutableArray<EvaluatedLocationInfo>.Empty;
            Time = time;
        }
    }
}
