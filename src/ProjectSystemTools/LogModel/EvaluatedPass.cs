// Copyright (c) Microsoft. All Rights Reserved. Licensed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.Build.Framework.Profiler;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.LogModel
{
    internal sealed class EvaluatedPass
    {
        public EvaluationPass Pass { get; }
        public string Description { get; }
        public ImmutableArray<EvaluatedLocation> Locations { get; }
        public Time Time { get; }

        public EvaluatedPass(EvaluationPass pass, string description, ImmutableArray<EvaluatedLocation> locations, Time time)
        {
            Pass = pass;
            Description = description;
            Locations = locations;
            Time = time;
        }
    }
}
