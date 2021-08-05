// Copyright (c) Microsoft. All Rights Reserved. Licensed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.Model
{
    [Flags]
    public enum BuildType
    {
        None = 0x0,
        Build = 0x1,
        DesignTimeBuild = 0x2,
        Evaluation = 0x4,
        Roslyn = 0x8,
        All = Build | DesignTimeBuild | Evaluation | Roslyn
    }
}
