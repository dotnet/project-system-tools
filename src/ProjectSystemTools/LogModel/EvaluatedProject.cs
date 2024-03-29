﻿// Copyright (c) Microsoft. All Rights Reserved. Licensed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.LogModel
{
    internal sealed class EvaluatedProject : Node
    {
        public string Name { get; }
        public EvaluatedProfile? EvaluationProfile { get; }

        public EvaluatedProject(string name, EvaluatedProfile? evaluationProfile, DateTime startTime, DateTime endTime, ImmutableList<Message> messages) :
            base(messages, startTime, endTime, Result.Succeeded)
        {
            Name = name;
            EvaluationProfile = evaluationProfile;
        }
    }
}
