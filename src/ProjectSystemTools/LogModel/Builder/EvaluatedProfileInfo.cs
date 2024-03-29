﻿// Copyright (c) Microsoft. All Rights Reserved. Licensed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.LogModel.Builder
{
    internal sealed class EvaluatedProfileInfo
    {
        public ImmutableArray<EvaluatedPassInfo> Passes { get; }
        public TimeInfo EvaluationTimeInfo { get; }

        public EvaluatedProfileInfo(IEnumerable<EvaluatedPassInfo> passes, TimeInfo evalutionTimeInfo)
        {
            Passes = passes.ToImmutableArray();
            EvaluationTimeInfo = evalutionTimeInfo;
        }
    }
}
