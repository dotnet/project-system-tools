// Copyright (c) Microsoft. All Rights Reserved. Licensed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.LogModel
{
    internal sealed class EvaluatedProfile
    {
        public ImmutableArray<EvaluatedPass> Passes { get; }
        public Time EvaluationTime { get; }

        public EvaluatedProfile(ImmutableArray<EvaluatedPass> passes, Time evaluationTime)
        {
            Passes = passes;
            EvaluationTime = evaluationTime;
        }
    }
}
